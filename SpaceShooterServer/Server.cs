using Newtonsoft.Json;
using SpaceShooterShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpaceShooterServer
{
    public class Server
    {
        private readonly int port = 8080;
        private TcpListener server = null;
        private volatile bool isRunning = false;

        // ClientState 리스트: State(게임상태) + TcpClient 보관
        private readonly List<ClientState> clients = new List<ClientState>();
        private readonly object clientsLock = new object();
        private int nextRole = 1;

        // TcpClient별 쓰기 락 (동시 쓰기 방지)
        private readonly Dictionary<TcpClient, SemaphoreSlim> clientSemaphores = new();
        private readonly object semLock = new();

        // 내부용 클래스: State + TcpClient
        private class ClientState
        {
            public State State { get; set; }
            public TcpClient Client { get; set; }
        }

        public Server() { }

        // 서버 시작
        public void Start()
        {
            IPEndPoint localAddress = new IPEndPoint(IPAddress.Any, port);
            server = new TcpListener(localAddress);
            server.Start();
            isRunning = true;

            Console.WriteLine($"서버시작, 포트넘버: {port}");

            var acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true
            };
            acceptThread.Start();
        }

        // 서버 종료
        public void Stop()
        {
            isRunning = false;
            try { server?.Stop(); } catch { }

            ClientState[] snapshot;
            lock (clientsLock)
            {
                snapshot = clients.ToArray();
                clients.Clear();
            }

            foreach (var c in snapshot)
            {
                try { c.Client.Close(); } catch { }
                RemoveSemaphoreForClient(c.Client); // 세마포어 정리
            }

            Console.WriteLine("서버종료");
        }

        // Accept loop (블로킹 AcceptTcpClient)
        private void AcceptLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient tcpClient = server.AcceptTcpClient();
                    Console.WriteLine($"클라이언트 접속: {((IPEndPoint)tcpClient.Client.RemoteEndPoint).ToString()}");

                    var clientThread = new Thread(() => HandleClient(tcpClient))
                    {
                        IsBackground = true
                    };
                    clientThread.Start();
                }
                catch (SocketException) when (!isRunning)
                {
                    // Stop()으로 중단되면 정상적으로 빠져나옴
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AcceptLoop 오류: {ex.Message}");
                }
            }
        }

        // 각 클라이언트 처리 (길이+JSON 프로토콜)
        private void HandleClient(TcpClient tcpClient)
        {
            int assignedRole;
            ClientState myClientState = null;

            try
            {
                NetworkStream stream = tcpClient.GetStream();

                // 1) 접속 순서대로 Role 할당 및 초기 State 생성/저장
                lock (clientsLock)
                {
                    assignedRole = nextRole++;
                    var initialState = new State
                    {
                        Role = assignedRole,
                        Player = new Player { X = 50, Y = 50 }, // 초기 위치(필요시 조정)
                        Enemies = new List<Enemy>()
                    };

                    myClientState = new ClientState
                    {
                        State = initialState,
                        Client = tcpClient
                    };

                    clients.Add(myClientState);
                }

                Console.WriteLine($"클라이언트에 Role {myClientState.State.Role} 할당");

                // 2) 할당된 초기 State를 클라이언트로 전송(길이+JSON)
                // fire-and-forget: 전송 실패는 내부에서 처리
                Task.Run(async () =>
                {
                    var sem = GetSemaphoreForClient(tcpClient);
                    await sem.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        await SendStateAsyncToClient(stream, myClientState.State).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"초기 State 전송 실패(Role {assignedRole}): {ex.Message}");
                    }
                    finally
                    {
                        sem.Release();
                    }
                });

                // 3) 읽기 루프: length(4) -> payload(length) -> JSON Deserialize(State)
                byte[] lenBuf = new byte[4];
                while (isRunning && tcpClient.Connected)
                {
                    // 길이 읽기
                    if (!ReadExactlySync(stream, lenBuf, 0, 4)) break;
                    int netLen = BitConverter.ToInt32(lenBuf, 0);
                    int payloadLen = IPAddress.NetworkToHostOrder(netLen);

                    if (payloadLen <= 0 || payloadLen > 10_000_000)
                    {
                        Console.WriteLine($"비정상 payload 길이: {payloadLen}. 연결 종료.");
                        break;
                    }

                    // payload 읽기
                    byte[] payload = new byte[payloadLen];
                    if (!ReadExactlySync(stream, payload, 0, payloadLen)) break;

                    // JSON -> State 역직렬화
                    string json = Encoding.UTF8.GetString(payload);
                    State incoming = null;
                    try
                    {
                        incoming = JsonConvert.DeserializeObject<State>(json);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"JSON 파싱 실패: {ex.Message}");
                        continue; // 오류시 다음 메시지 대기
                    }
                    if (incoming == null) continue;

                    // 4) 들어온 순서대로 할당한 Role을 덮어쓰기 (보안/정합성)
                    incoming.Role = myClientState.State.Role;

                    // 5) 서버 내 저장된 State 업데이트 (원하면 필드별 머지 가능)
                    // 여기서는 전체 State를 갱신(간단화). 필요시 일부 필드만 덮어쓸 것.
                    lock (clientsLock)
                    {
                        myClientState.State = incoming;
                    }

                    Console.WriteLine($"수신(Role {incoming.Role}): Player X={incoming.Player?.X}, Y={incoming.Player?.Y}");

                    // 6) 다른 클라이언트에게 중계 (각 클라이언트로 길이+JSON 전송)
                    ClientState[] snapshot;
                    lock (clientsLock)
                    {
                        snapshot = clients.ToArray();
                    }

                    foreach (var other in snapshot)
                    {
                        // 자신에게 보내지 않음
                        if (other.Client == tcpClient) continue;
                        if (!other.Client.Connected) continue;

                        // 각 타겟에 대해 비동기 전송(쓰기 동시성 제어)
                        Task.Run(async () =>
                        {
                            var sem = GetSemaphoreForClient(other.Client);
                            await sem.WaitAsync().ConfigureAwait(false);
                            try
                            {
                                // 다른 클라이언트에게는 incoming(State)을 그대로 보냄
                                await SendStateAsyncToClient(other.Client.GetStream(), incoming).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"전달 실패(to Role {other.State?.Role}): {ex.Message}");
                                // 실패 시 연결 정리(선택사항)
                                // lock (clientsLock) { clients.RemoveAll(c => c.Client == other.Client); }
                            }
                            finally
                            {
                                sem.Release();
                            }
                        });
                    }
                } // while
            }
            catch (Exception ex)
            {
                Console.WriteLine($"클라이언트 처리 중 오류: {ex.Message}");
            }
            finally
            {
                // 연결 종료 정리: clients 리스트에서 제거, 세마포어 해제, TcpClient 닫기
                if (myClientState != null)
                {
                    lock (clientsLock)
                    {
                        clients.RemoveAll(c => ReferenceEquals(c.Client, myClientState.Client));
                    }
                }
                else
                {
                    // myClientState이 null인 경우(초기화 실패 등), tcpClient가 리스트에 남아있을 수 있으니 안전하게 제거 시도
                    lock (clientsLock)
                    {
                        clients.RemoveAll(c => ReferenceEquals(c.Client, tcpClient));
                    }
                }

                RemoveSemaphoreForClient(tcpClient);
                try { tcpClient.Close(); } catch { }

                Console.WriteLine($"클라이언트 연결 종료(Role {(myClientState?.State?.Role.ToString() ?? "Unknown")})");
            }
        }

        // TcpClient에 대응하는 SemaphoreSlim 반환(없으면 생성)
        private SemaphoreSlim GetSemaphoreForClient(TcpClient client)
        {
            lock (semLock)
            {
                if (!clientSemaphores.TryGetValue(client, out var sem))
                {
                    sem = new SemaphoreSlim(1, 1);
                    clientSemaphores[client] = sem;
                }
                return sem;
            }
        }

        // TcpClient 정리 시 Semaphore 제거
        private void RemoveSemaphoreForClient(TcpClient client)
        {
            lock (semLock)
            {
                if (clientSemaphores.TryGetValue(client, out var sem))
                {
                    clientSemaphores.Remove(client);
                    try { sem.Dispose(); } catch { }
                }
            }
        }

        // 스트림 닫지 않고 State 전송 (길이(prefix) + JSON)
        private async Task SendStateAsyncToClient(NetworkStream stream, State state)
        {
            string json = JsonConvert.SerializeObject(state);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lenBuf = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
            await stream.WriteAsync(lenBuf, 0, lenBuf.Length).ConfigureAwait(false);
            await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        // 동기적으로 정확히 count 바이트 읽기 (원래 코드 재사용)
        private bool ReadExactlySync(NetworkStream stream, byte[] buffer, int offset, int count)
        {
            int read = 0;
            try
            {
                while (read < count)
                {
                    int n = stream.Read(buffer, offset + read, count - read);
                    if (n == 0) return false;
                    read += n;
                }
                return true;
            }
            catch (IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
    }
}
