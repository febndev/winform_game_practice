using System;


namespace SpaceShooterServer
{
    public class MainApp
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();

        }
    }
}


