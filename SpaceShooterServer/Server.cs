using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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

        // role -> TcpClient 매핑 (예: role 1 -> 클라이언트A, role 2 -> 클라이언트B)
        // 이거 우선 수정해야하는데 붙여넣었음. 접속 순서대로 1p,2p 하든지 해야함.
        private readonly Dictionary<int, TcpClient> roleClients = new();
        private readonly object roleLock = new();


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
            var acceptThread = new Thread(AcceptLoop);

            // true 값일때 이 쓰레드를 기다리지 않고 같이 종료. 
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        // 맨 마지막에 들어가는 서버 종료, 메서드 공부 해야함
        public void Stop()
        {
            isRunning = false;
            try { server?.Stop(); } catch { }

            TcpClient[] snapshot;
            lock (clientsLock)
            {
                snapshot = clients.ToArray();
                clients.Clear();
            }
            foreach (var c in snapshot)
            {
                try { c.Close(); } catch { }
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
            Console.WriteLine("클라이언트 처리 시작");

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];

                    while (isRunning)
                    {
                        int read;
                        try
                        {
                            read = stream.Read(buffer, 0, buffer.Length); // 블로킹 읽기
                        }
                        catch (IOException)
                        {
                            break; // 타임아웃 / 연결문제 발생시 종료. 
                        }
                        catch (ObjectDisposedException) { break; } // ← 추가

                        if (read == 0) break;

                        // 패킷 파싱, 처리 
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"클라이언트 처리 중 오류 발생: {ex.Message}");
            }
            finally
            {
                lock (clientsLock) clients.Remove(client); // 누수 방지, 리스트에서 제거
                Console.WriteLine("클라이언트 연결 종료");
            }
        }

    }
}
