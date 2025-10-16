using SpaceShooterShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Client client = new Client();
            try
            {
                // 서버에 연결하고 초기 State(역할 포함)를 기다림
                State initialState = await client.ConnectAndWaitInitialStateAsync(5000);

                int assignedRole = initialState?.Role ?? 1;
                Console.WriteLine($"초기 State 수신: assignedRole = {assignedRole}");

                Application.Run(new FormMain(client, assignedRole));
            }
            catch (TimeoutException tex)
            {
                MessageBox.Show("초기 State 수신 타임아웃: 기본 역할(1)로 실행합니다.\n" + tex.Message, "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Run(new FormMain(client, 1));
            }
            catch (Exception ex)
            {
                MessageBox.Show("서버 연결 중 오류가 발생했습니다:\n" + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

