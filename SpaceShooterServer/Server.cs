using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SpaceShooterShared;

namespace SpaceShooterServer
{
    public class Server
    {
        private readonly int port = 8080;
        private TcpListener server = null;
        // private bool isRunning = false; 였는데 volatile이 뭐람.?
        private volatile bool isRunning = false;

        // 멀티쓰레드로 변경하면서 변수가 늘었음. 
        //private readonly List<TcpClient> clients = new();
        //private readonly object clientsLock = new();

        // role -> TcpClient 매핑 (예: role 1 -> 클라이언트A, role 2 -> 클라이언트B)
        // 이거 우선 수정해야하는데 붙여넣었음. 접속 순서대로 1p,2p 하든지 해야함.
        //private readonly Dictionary<int, TcpClient> roleClients = new();
        //private readonly object roleLock = new();
        private List<ClientState> clients = new List<ClientState>();
        private int nextRole = 1;
        private readonly object clientsLock = new object(); 

        // 각 TcpClient별 쓰기 락 (동시 쓰기 방지)
        private readonly Dictionary<TcpClient, SemaphoreSlim> clientSemaphores = new();
        private readonly object semLock = new();

        // 클라이언트 상태 + 스트림 
        private class ClientState
        {
            public State State { get; set; }
            public TcpClient Client { get; set; }
        }
        // 생성자
        public Server()
        {
        }
        // 서버 시작 메서드
        public void Start()
        {
            IPEndPoint localAddress = new IPEndPoint(IPAddress.Any, port);

            server = new TcpListener(localAddress);
            server.Start();
            isRunning = true;

            Console.WriteLine($"서버시작, 포트넘버: {port}");

            // AcceptLoop(); 를 했더니 너무 직접 호출했다고 지피티가 바꾸라함. 
            // 이게 왜 여기 들어가는지 모르겠음. 
            var acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            acceptThread.Start();
        }

        // 맨 마지막에 들어가는 서버 종료, 메서드 공부 해야함
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
            }

            Console.WriteLine("서버종료");
        }
        // AcceptTcpClient 메서드
        private void AcceptLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient(); // 블로킹 호출
                    Console.WriteLine($"클라이언트 접속 : " +
                        $"{((IPEndPoint)client.Client.RemoteEndPoint).ToString()}");
                    // 클라이언트 리스트에 추가

                    var clientThread = new Thread(() => HandleClient(client))
                    {
                        IsBackground = true
                    };
                    clientThread.Start();

                }
                catch (SocketException) when (!isRunning)
                {
                    // Stop() 으로 server 닫히면 여기로 들어오면서 정상종료 됨. 
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AcceptLoop 오류: {ex.Message}");
                }
            }
        }
        
        private void HandleClient(TcpClient client)
        {
            int assignedRole;
            ClientState myClientState = null;
            try
            {
                NetworkStream stream = client.GetStream();

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
                        Client = client
                    };

                    clients.Add(myClientState);
                }

                Console.WriteLine($"클라이언트에 Role {myClientState.State.Role} 할당");
                Console.WriteLine($"클라이언트 처리 시작: Role - {assignedRole}");


                // 2) 할당된 초기 State를 클라이언트로 전송(길이+JSON)
                // fire-and-forget: 전송 실패는 내부에서 처리
                Task.Run(async () =>
                {
                    var sem = GetSemaphoreForClient(client);
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
                while (isRunning && client.Connected)
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
                        if (other.Client == client) continue;
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
                                Console.WriteLine($"전달 성공(to Role {other.State?.Role})");
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
                Console.WriteLine($"클라이언트 처리 중 오류 발생: {ex.Message}");
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
                        clients.RemoveAll(c => ReferenceEquals(c.Client, client));
                    }
                }

                RemoveSemaphoreForClient(client);
                try { client.Close(); } catch { }

                Console.WriteLine($"클라이언트 연결 종료(Role {(myClientState?.State?.Role.ToString() ?? "Unknown")})");
            }
        }
        // 10.15 오전 9:48 추가
        // 스트림 닫지 않고 비동기 전송(중계용)
        private async Task SendStateAsyncToClient(NetworkStream stream, State state)
        {
            string json = JsonConvert.SerializeObject(state);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] lenBuf = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
            await stream.WriteAsync(lenBuf, 0, lenBuf.Length).ConfigureAwait(false);
            await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
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
        // 동기적으로 정확히 count 바이트 읽기
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
