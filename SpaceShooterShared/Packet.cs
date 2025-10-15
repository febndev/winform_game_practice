using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Net;


namespace SpaceShooterShared
{
    public static class Packet
    {
        public static async Task SendStateAsync(NetworkStream stream, State state)
        {
            if (stream == null || state == null) return;
            try
            {
                // State 객체를 JSON 문자열로 직렬화
                var json = JsonConvert.SerializeObject(state); // 어떤건 string이고 어떤건 byte[]인데 그래서 var로 입력 
                byte[] data = Encoding.UTF8.GetBytes(json);
                // 데이터 길이를 먼저 전송 (4바이트)
                int len = data.Length;
                byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len));
                await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                // 실제 데이터 전송
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync().ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"패킷 전송 오류: {ex.Message}");
                throw;
            }
        }

        // helper: 정확히 count 바이트 읽기
        private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int count)
        {
            byte[] buf = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n = await stream.ReadAsync(buf, read, count - read);
                if (n == 0) throw new Exception("원격에서 연결을 닫음");
                read += n;
            }
            return buf;
        }

        public static async Task<State> ReceiveStateAsync(NetworkStream stream)
        {
            var lenBuf = await ReadExactlyAsync(stream, 4);
            int netLen = BitConverter.ToInt32(lenBuf, 0);
            int len = IPAddress.NetworkToHostOrder(netLen);
            var payload = await ReadExactlyAsync(stream, len);
            var json = Encoding.UTF8.GetString(payload);
            var state = JsonConvert.DeserializeObject<State>(json);
            return state;
        }

    }
}
