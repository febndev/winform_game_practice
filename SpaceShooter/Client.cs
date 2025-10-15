using SpaceShooterShared;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        public NetworkStream Stream => stream; // 공개 프로퍼티
        public bool IsConnected => client?.Connected ?? false;

        public Client() { }

        // 비동기 연결
        public async Task ConnectAsync()
        {
            if (IsConnected)
                return;
            client = new TcpClient();
            try
            {
                await client.ConnectAsync(serverIP, port).ConfigureAwait(false); // 비동기

                //Console.WriteLine("서버에 연결 성공!");
                stream = client.GetStream();

                // 연결 후 바로 수신 루프 시작 
                _ = Task.Run(() => StartReceivingLoop());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
            }
            catch // 이건 뭔진 알겠는데 일단 놔두고 다시 한번 보기 
            {
                try {  client?.Close(); } catch { }
                client = null;
                stream = null;
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                try { stream?.Close(); } catch { }
                try { client?.Close(); } catch { }
                client = null;
                stream = null;
            }
            catch { }
        }


        private async Task StartReceivingLoop()
        {
            if (stream == null) return;

            while (client.Connected)
            {
                try
                {
                    // Packet.cs의 ReceiveStateAsync 사용
                    State state = await Packet.ReceiveStateAsync(stream).ConfigureAwait(false);

                    if (state == null)
                    {
                        // null 상태 수신 시 연결 종료
                        Disconnect();
                        break;
                    }
                    CurrentState = state;

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
                    // 수신 중 예외 발생하면 연결 종료 
                    System.Diagnostics.Debug.WriteLine("네트워크 스트림이 닫혔습니다.");
                    Disconnect();
                    break;
                }
            }
        }
        public void Dispose()
        {
            Disconnect();
        }

    }
}