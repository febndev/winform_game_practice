using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaceShooter
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Client client = new Client();
            // client.Connect();

            // 폼 생성
            Form1 form = new Form1(client);

            // 폼이 닫힐 때 클라이언트 정리
            form.FormClosed += (sender, e) =>
            {
                client.Disconnect();
                Console.WriteLine("클라이언트 연결 종료됨.");
            };
            // 비동기 연결을 UI 스레드를 막지 않고 실행
            Task.Run(async () => await client.ConnectAsync());

            // WinForm 실행
            Application.Run(form);

        }
    }
}
