using SpaceShooterShared;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SpaceShooter
{
    public class Client : IDisposable
    {
        private TcpClient client;
        private NetworkStream stream;

        private readonly string serverIP = "127.0.0.1";
        private readonly int port = 8080;

        // 마지막으로 받은 상태
        public State CurrentState { get; private set; }

        // 서버에서 State를 받을 때 발생 (구독자는 UI 스레드로 마샬링할 것)
        public event Action<State> OnStateReceived;

        public NetworkStream Stream => stream;
        public bool IsConnected => client?.Connected ?? false;

        public Client() { }

        // 서버에 연결하고 수신 루프 시작
        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            client = new TcpClient();
            try
            {
                await client.ConnectAsync(serverIP, port).ConfigureAwait(false);
                stream = client.GetStream();

                // 단순 수신 루프 시작 (백그라운드)
                _ = Task.Run(() => StartReceivingLoop());
            }
            catch
            {
                try { client?.Close(); } catch { }
                client = null;
                stream = null;
                throw;
            }
        }

        // 안전한 연결 해제
        public void Disconnect()
        {
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }

            stream = null;
            client = null;
        }

        // 단순화된 수신 루프 (CancellationToken 없음)
        private async Task StartReceivingLoop()
        {
            if (stream == null) return;

            while (client != null && client.Connected)
            {
                try
                {
                    // 프로젝트 내 Packet.ReceiveStateAsync 사용 (길이+JSON 처리 담당)
                    State state = await Packet.ReceiveStateAsync(stream).ConfigureAwait(false);

                    if (state == null)
                    {
                        // null이면 연결에 문제가 있는 것으로 간주하고 종료
                        Disconnect();
                        break;
                    }

                    CurrentState = state;

                    // 이벤트 발생 (구독자 측에서 Invoke로 UI 스레드 진입)
                    try
                    {
                        OnStateReceived?.Invoke(state);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"OnStateReceived handler error: {ex}");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 스트림/소켓이 닫히면 루프 종료
                    Disconnect();
                    break;
                }
                catch (Exception ex)
                {
                    // 수신 중 예외 발생하면 연결 종료
                    System.Diagnostics.Debug.WriteLine($"StartReceivingLoop error: {ex}");
                    Disconnect();
                    break;
                }
            }
        }

        // 서버로 State 전송(프로젝트의 기존 전송 유틸 사용)
        public async Task SendStateAsync(State state)
        {
            if (!IsConnected || stream == null) throw new InvalidOperationException("Not connected");
            await Packet.SendStateAsync(stream, state).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
