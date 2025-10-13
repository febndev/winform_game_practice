using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SpaceShooter
{
    public class Client
    {
        private TcpClient client;
        private NetworkStream stream;

        private readonly string serverIP = "127.0.0.1";
        private readonly int port = 8080;

        // 비동기 연결
        public async Task ConnectAsync()
        {
            try
            {
                client = new TcpClient();
                //Console.WriteLine("서버에 연결 시도 중...");
                await client.ConnectAsync(serverIP, port); // 비동기

                //Console.WriteLine("서버에 연결 성공!");
                stream = client.GetStream();

                // 테스트 메시지 전송
                //string msg = "Hello Server!";
                //byte[] data = Encoding.UTF8.GetBytes(msg);
                //await stream.WriteAsync(data, 0, data.Length);
                //Console.WriteLine("테스트 메시지 전송 완료.");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"서버 연결 실패: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            try
            {
                stream?.Close();
                client?.Close();
                //Console.WriteLine("클라이언트 연결 종료.");
            }
            catch { }
        }
    }
}