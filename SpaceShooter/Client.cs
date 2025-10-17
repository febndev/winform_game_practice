using SpaceShooterShared;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpaceShooter
{
    public class Client : IDisposable
    {
        private TcpClient client;
        private NetworkStream stream;

        private readonly string serverIP = "10.10.21.101";
        private readonly int port = 8080;

        // 마지막으로 받은 상태
        public State CurrentState { get; private set; }

        // 서버에서 State를 받을 때 발생 (구독자는 UI 스레드로 마샬링할 것)
        public event Action<State> OnStateReceived;

        public NetworkStream Stream => stream; // 공개 프로퍼티
        public bool IsConnected => client?.Connected ?? false;

        //[시작] Form1_Load보다 먼저 실행되게끔, 서버로부터 클라이언트 Role 부여 먼저 받게끔 하기위해서 선언 
        private TaskCompletionSource<State> _initialStateTcs;
        private CancellationTokenSource _receiveLoopCts;

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
                client.NoDelay = true;
                //Console.WriteLine("서버에 연결 성공!");
                stream = client.GetStream();

                // 초기 상태 수신을 위한 TCS 초기화 (재시도 시 초기화)
                _initialStateTcs = new TaskCompletionSource<State>(TaskCreationOptions.RunContinuationsAsynchronously);

                // 수신 루프용 CancellationTokenSource 준비 (이미 있으면 새로 만듦)
                _receiveLoopCts?.Cancel();
                _receiveLoopCts = new CancellationTokenSource();

                // 연결 후 바로 수신 루프 시작 
                _ = Task.Run(() => StartReceivingLoop(_receiveLoopCts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
                try { client?.Close(); } catch { }
                client = null;
                stream = null;
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                _receiveLoopCts?.Cancel();
                _receiveLoopCts?.Dispose();
                _receiveLoopCts = null;
            }
            catch { }

            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }

            stream = null;
            client = null;
        }

        // 기존 ConnectAsync 를 유지하고, 아래 메서드는 연결 후 수신루프 시작 + 초기 state를 기다림
        public async Task<State> ConnectAndWaitInitialStateAsync(int timeoutMs = 5000)
        {
            // 1) 연결 시작 (ConnectAsync 내부에서 _initialStateTcs 초기화 및 StartReceivingLoop 시작)
            await ConnectAsync().ConfigureAwait(false);

            // 2) 초기 state 를 기다림 (타임아웃 처리)
            var tcsTask = _initialStateTcs?.Task;
            if (tcsTask == null)
                throw new InvalidOperationException("_initialStateTcs가 초기화되지 않았습니다.");

            var completed = await Task.WhenAny(tcsTask, Task.Delay(timeoutMs)).ConfigureAwait(false);
            if (completed == tcsTask)
            {
                return await tcsTask.ConfigureAwait(false); // 성공적으로 받은 State 반환
            }
            else
            {
                throw new TimeoutException($"초기 State 수신 타임아웃({timeoutMs}ms)");
            }
        }


        private async Task StartReceivingLoop(CancellationToken token)
        {
            try
            {
                var localStream = this.stream; // 안전하게 캡처
                if (localStream == null) return;

                while (!token.IsCancellationRequested && client != null && client.Connected)
                {
                    State state = null;
                    try
                    {
                        // 기존 Packet.ReceiveStateAsync 사용 (예: 길이 읽고 payload 읽기)
                        state = await Packet.ReceiveStateAsync(localStream).ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        // 스트림이 닫히면 종료
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Receive loop error: " + ex.Message);
                        break;
                    }

                    if (state == null)
                    {
                        // 연결이 끊기거나 잘못된 메시지
                        Console.WriteLine("Received null state -> breaking receive loop");
                        break;
                    }

                    // CurrentState 갱신 (필요하면 프로퍼티를 노출)
                    this.CurrentState = state;

                    // 최초 상태면 TCS를 완료시킨다 (안전하게 TrySetResult)
                    if (_initialStateTcs != null && !_initialStateTcs.Task.IsCompleted)
                    {
                        _initialStateTcs.TrySetResult(state);
                    }

                    // 외부 이벤트 핸들러 호출 (UI 등에서 구독)
                    try
                    {
                        OnStateReceived?.Invoke(state);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("OnStateReceived handler threw: " + ex.Message);
                    }
                }
            }
            finally
            {
                // 루프 종료 시 _initialStateTcs 가 아직 완료되지 않았다면 취소 또는 예외 처리
                if (_initialStateTcs != null && !_initialStateTcs.Task.IsCompleted)
                {
                    _initialStateTcs.TrySetCanceled();
                }
            }
        }
        public void Dispose()
        {
            Disconnect();
        }

    }
}