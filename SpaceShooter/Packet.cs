using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text.Json;
using System.Net;


namespace SpaceShooter
{
    public static class Packet
    {
        public static async Task SendStateAsync(NetworkStream stream, State state)
        {
            if (stream == null || state == null) return;
            try
            {
                // State 객체를 JSON 문자열로 직렬화
                var json = JsonSerializer.Serialize(state); // 어떤건 string이고 어떤건 byte[]인데 그래서 var로 입력 
                byte[] data = Encoding.UTF8.GetBytes(json);
                // 데이터 길이를 먼저 전송 (4바이트)
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
                await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                // 실제 데이터 전송
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"패킷 전송 오류: {ex.Message}");
            }
        }

        public static async Task<State> ReceiveStateAsync(NetworkStream stream)
        {
            // 여기서 파싱..? 
        }

    }
}
