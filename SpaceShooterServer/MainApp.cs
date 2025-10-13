using System;


namespace SpaceShooterServer
{
    public class MainApp
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();

            Console.WriteLine("서버가 시작되었습니다. 종료하려면 Enter 키를 누르세요...");
            Console.ReadLine();

            server.Stop();
        }
    }
}


