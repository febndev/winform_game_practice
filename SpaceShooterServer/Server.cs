
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
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
        // private bool isRunning = false; 였는데 volatile이 뭐람.?
        private volatile bool isRunning = false;
        // 멀티쓰레드로 변경하면서 변수가 늘었음. 
        private readonly List<TcpClient> clients = new();
        private readonly object clientsLock = new();
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
            var acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true
            };
            acceptThread.Start();
        }
        // 맨 마지막에 들어가는 서버 종료 메서드
        public void Stop()
        {
            isRunning = false;
            server.Stop();
            Console.WriteLine("서버종료");
        }
        // AcceptTcpClient 메서드
        private void AcceptLoop()
        {
            while (isRunning)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine($"클라이언트 접속 : " +
                    $"{((IPEndPoint)client.Client.RemoteEndPoint).ToString()}");
                // 클라이언트 리스트에 추가
                lock (clientsLock) clients.Add(client);

                var clientThread = new Thread(() => HandleClient(client))
                {
                    IsBackground = true
                };
                clientThread.Start();
            }
        }
        
        private void HandleClient(TcpClient client)
        {
            Console.WriteLine("클라이언트 처리 시작");
            NetworkStream stream = null;

            try
            {
                //stream.close(); 하기 쉽게 try문 안에서 GetStream() 호출
                stream = client.GetStream();
            
                while(true)
                {
                    //패킷 처리
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"클라이언트 처리 중 오류 발생: {ex.Message}");
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
                
                if (client != null)
                {
                    client.Close();
                }
                Console.WriteLine("클라이언트 연결 종료");
            }
        }

    }
}
