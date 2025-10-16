using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;
using SpaceShooterShared;

namespace SpaceShooter
{
    public partial class FormMain : Form
    {
        WindowsMediaPlayer gameMedia; // 백그라운드 미디어 
        WindowsMediaPlayer shootgMedia; // 총알 발사시 사운드
        WindowsMediaPlayer explosionMedia; // 적 파괴될 때 사운드 

        // --- 게임 오브젝트 (패널별) ---
        // 디자이너에 올려둔 player1, player2 존재 (Name: player1, player2)
        private Panel myPanel;
        private Panel opponentPanel;

        private PictureBox myPlayer;         // 내 화면의 플레이어 (디자이너의 player1/2 중 하나)
        private PictureBox opponentPlayer;   // 상대 화면의 플레이어

        private Dictionary<int, PictureBox> myEnemies = new Dictionary<int, PictureBox>();
        private Dictionary<int, PictureBox> opponentEnemies = new Dictionary<int, PictureBox>();

        private PictureBox[] myMunitions;
        private PictureBox[] opponentMunitions;

        private PictureBox[] myStars;
        private PictureBox[] opponentStars;

        // (선택) 적이 발사하는 총알(패널별)
        private PictureBox[] myEnemiesMunition;
        private PictureBox[] opponentEnemiesMunition;

        // --- 게임 파라미터 ---
        int enemiesMunitionSpeed = 4;
        int backgroundspeed = 4;
        int playerSpeed = 4;
        int munitionSpeed = 20;
        int enemiSpeed = 4;

        Random rnd;

        int score = 0;
        int level = 1;
        int difficulty = 9;
        bool pause;
        bool gameIsOver = false;

        // --- 네트워크 --- 
        private readonly Client client;
        private int role = 1;
        private bool focusSet = false;
       

        public FormMain(Client clientInstance, int assignedRole)
        {
            InitializeComponent();

            client = clientInstance;
            role = assignedRole;
            // 폼에서 키 이벤트 받기
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            // 최소 초기값 세팅 
            rnd = new Random();

            this.Load += Form1_Load;
            this.FormClosing += FormMain_FormClosing;

            // 연결 후 수신 루프 시작, 중복이라 삭제 그래도 여기서 이렇게 호출할 수 있다는걸 알려고 남겨둠.
            // _ = Task.Run(() => StartReceivingLoop());
        }
        private void Form1_Load(object sender, EventArgs e)
        {

            // myPanel / opponentPanel 결정 (role 기준)
            myPanel = (role == 1) ? splitContainer1.Panel1 : splitContainer1.Panel2;
            opponentPanel = (role == 1) ? splitContainer1.Panel2 : splitContainer1.Panel1;
            myPanel.Font = this.Font;

            // 디자이너에 올려둔 player1/player2의 Parent를 적절히 설정 (이미 Form 디자이너에 존재)
            myPlayer = (role == 1) ? player1 : player2;
            opponentPlayer = (role == 1) ? player2 : player1;

            // 부모 패널로 배치 (안정적: Load 이벤트에서 진행)
            myPlayer.Parent = myPanel;
            opponentPlayer.Parent = opponentPanel;

            // 미디어 초기화 (파일 경로는 프로젝트에 맞게 조정)
            InitMediaPlayers();

            // 패널별 게임 오브젝트 초기화 (총알, 별, 적 총알)
            InitGameObjects(myPanel, opponentPanel);

            // 상대 패널을 반투명하게 덮기
            AddDarkOverlay(opponentPanel);

            // 점수 레이블, 버튼 등 myPanel에 띄우기 
            AttachHudTo(myPanel);

            // 포커스 한 번 설정
            myPanel.Focus();
            focusSet = true;

            // 수신 루프 시작 (폼이 완전히 로드된 뒤 시작)
            //if (client != null)
            //    _ = Task.Run(() => StartReceivingLoop());

            // --- 변경: Client에서 수신(읽기)을 담당하므로, 여기선 이벤트 등록만 함 ---
            if (client != null)
            {
                // 안전하게 UI 스레드로 마샬링하여 UpdateUI 호출
                client.OnStateReceived += (state) =>
                {
                    if (this.IsDisposed) return;
                    if (this.InvokeRequired)
                        this.BeginInvoke(new Action(() => UpdateUI(state)));
                    else
                        UpdateUI(state);
                };
            }
        }
        private void InitMediaPlayers()
        {
            gameMedia = new WindowsMediaPlayer();
            shootgMedia = new WindowsMediaPlayer();
            explosionMedia = new WindowsMediaPlayer();

            gameMedia.URL = "songs\\GameSong.mp3";
            shootgMedia.URL = "songs\\shoot.mp3";
            explosionMedia.URL = "songs\\boom.mp3";

            gameMedia.settings.setMode("loop", true);
            gameMedia.settings.volume = 5;
            shootgMedia.settings.volume = 1;
            explosionMedia.settings.volume = 6;

            gameMedia.controls.play(); 
        }

        // [시작] 게임 UI 컨트롤 생성 
        private void InitGameObjects(Panel myPanel, Panel opponentPanel)
        {
            // --- 별들 (배경) ---
            myStars = new PictureBox[15];
            opponentStars = new PictureBox[15];
            for (int i = 0; i < 15; i++)
            {
                myStars[i] = new PictureBox
                {
                    BorderStyle = BorderStyle.None,
                    Location = new Point(rnd.Next(20, 570), rnd.Next(-10, 400)),
                    BackColor = (i % 2 == 0) ? Color.DarkGray : Color.Wheat,
                    Size = (i % 2 == 0) ? new Size(3, 3) : new Size(2, 2),
                    Image = SpaceShooter.Properties.Resources.star2,
                    Parent = myPanel
                };
                opponentStars[i] = new PictureBox
                {
                    BorderStyle = BorderStyle.None,
                    Location = new Point(rnd.Next(20, 570), rnd.Next(-10, 400)),
                    BackColor = (i % 2 == 0) ? Color.DarkGray : Color.Wheat,
                    Size = (i % 2 == 0) ? new Size(3, 3) : new Size(2, 2),
                    Image = SpaceShooter.Properties.Resources.star2,
                    Parent = opponentPanel
                };
            }


            // --- 내/상대 총알 ---
            myMunitions = new PictureBox[3];
            opponentMunitions = new PictureBox[3];
            for (int i = 0; i < 3; i++)
            {
                myMunitions[i] = new PictureBox
                {
                    Size = new Size(8, 8),
                    Image = SpaceShooter.Properties.Resources.munition,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.None,
                    Visible = false,
                    Parent = myPanel
                };
                opponentMunitions[i] = new PictureBox
                {
                    Size = new Size(8, 8),
                    Image = SpaceShooter.Properties.Resources.munition,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.None,
                    Visible = false,
                    Parent = opponentPanel
                };
            }

            // --- 내 패널의 적비행기 --- 
            myEnemies = new Dictionary<int, PictureBox>();
            opponentEnemies = new Dictionary<int, PictureBox>();
            for (int i = 0; i < 10; i++)
            {
                var myEnemy = new PictureBox
                {
                    Size = new Size(40, 40),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.None,
                    Visible = false,
                    Location = new Point((i + 1) * 50, -50),
                    Parent = myPanel,
                    Tag = i
                };
                myEnemies[i] = myEnemy;
            }
            myEnemies[0].Image = SpaceShooter.Properties.Resources.Boss1;
            myEnemies[1].Image = SpaceShooter.Properties.Resources.E2;
            myEnemies[2].Image = SpaceShooter.Properties.Resources.E3;
            myEnemies[3].Image = SpaceShooter.Properties.Resources.E3;
            myEnemies[4].Image = SpaceShooter.Properties.Resources.E1;
            myEnemies[5].Image = SpaceShooter.Properties.Resources.E3;
            myEnemies[6].Image = SpaceShooter.Properties.Resources.E2;
            myEnemies[7].Image = SpaceShooter.Properties.Resources.E3;
            myEnemies[8].Image = SpaceShooter.Properties.Resources.E2;
            myEnemies[9].Image = SpaceShooter.Properties.Resources.Boss2;

            // --- 상대 패널의 적비행기 ---
            for (int i = 0; i < 10; i++)
            {
                var oppEnemy = new PictureBox
                {
                    Size = new Size(40, 40),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.None,
                    Visible = false,
                    Location = new Point((i + 1) * 50, -50),
                    Parent = opponentPanel,
                    Tag = i
                };
                opponentEnemies[i] = oppEnemy;
            }
            opponentEnemies[0].Image = SpaceShooter.Properties.Resources.Boss1;
            opponentEnemies[1].Image = SpaceShooter.Properties.Resources.E2;
            opponentEnemies[2].Image = SpaceShooter.Properties.Resources.E3;
            opponentEnemies[3].Image = SpaceShooter.Properties.Resources.E3;
            opponentEnemies[4].Image = SpaceShooter.Properties.Resources.E1;
            opponentEnemies[5].Image = SpaceShooter.Properties.Resources.E3;
            opponentEnemies[6].Image = SpaceShooter.Properties.Resources.E2;
            opponentEnemies[7].Image = SpaceShooter.Properties.Resources.E3;
            opponentEnemies[8].Image = SpaceShooter.Properties.Resources.E2;
            opponentEnemies[9].Image = SpaceShooter.Properties.Resources.Boss2;


            //// 이코드랑 밑에 코드랑 둘중택일해야함 
            //myEnemiesMunition = new PictureBox[10];

            //for (int i = 0; i < myEnemiesMunition.Length; i++)
            //{
            //    myEnemiesMunition[i] = new PictureBox();
            //    myEnemiesMunition[i].Size = new Size(2, 25);
            //    myEnemiesMunition[i].Visible = false;
            //    myEnemiesMunition[i].BackColor = Color.Yellow;
            //    int x = rnd.Next(0, 10);
            //    myEnemiesMunition[i].Location = new Point(myEnemies[x].Location.X, myEnemies[x].Location.Y - 20);
            //    // 스플릿 컨테이너 하기 전 코드
            //    // this.Controls.Add(enemiesMunition[i]);
            //    // 스플릿 컨테이너 후 코드
            //    splitContainer1.Panel1.Controls.Add(myEnemiesMunition[i]);
            //}
            //---적 총알(선택)-- -
            myEnemiesMunition = new PictureBox[10];
            //opponentEnemiesMunition = new PictureBox[10];
            for (int i = 0; i < 10; i++)
            {
                int x = rnd.Next(0, 10);
                myEnemiesMunition[i] = new PictureBox
                {
                    Size = new Size(2, 25),
                    BackColor = Color.Yellow,
                    Visible = false,
                    Location = new Point(myEnemies[x].Location.X, myEnemies[x].Location.Y - 20),//여기써야함
                    Parent = myPanel
                };
            }
                //opponentEnemiesMunition[i] = new PictureBox
                //{
                //    Size = new Size(2, 25),
                //    BackColor = Color.Yellow,
                //    Visible = false,
                //    Parent = opponentPanel
                //};
                //}
        }
        // [끝] 게임 UI 컨트롤 생성 

        // [시작] 패널 ui 어둡게 
        private void AddDarkOverlay(Panel targetPanel)
        {
            Panel overlay = new Panel
            {
                BackColor = Color.FromArgb(30, 0, 0, 0), // 반투명 검정색 (120=불투명도)
                Dock = DockStyle.Fill,
                Enabled = false // 클릭 등 이벤트 통과하게 함
            };
            targetPanel.Controls.Add(overlay);
            overlay.BringToFront();
        }
        // [끝] 패널 ui 어둡게 


        // Timer_Tick 은 맨 밑으로 이동시키자 
        // [시작] 내 비행기 충돌시 로직
        private void Collision()
        {
            foreach (var enemy in myEnemies)
            {
                var key = enemy.Key;         // 키
                var pb = enemy.Value;        // 값 

                if (myMunitions[0].Bounds.IntersectsWith(pb.Bounds)
                    || myMunitions[1].Bounds.IntersectsWith(pb.Bounds)
                    || myMunitions[2].Bounds.IntersectsWith(pb.Bounds))
                {
                    explosionMedia.controls.play();

                    score += 1;
                    scorelbl.Text = (score < 10) ? "0" + score.ToString() : score.ToString();

                    if (score % 30 == 0)
                    {
                        level += 1;
                        levellbl.Text = (level < 10) ? "0" + level.ToString() : level.ToString();
                        
                        // 난이도 조절 
                        if (enemiSpeed <= 10 && enemiesMunitionSpeed <= 10 && difficulty >= 0)
                        {
                            difficulty--;
                            enemiSpeed++;
                            enemiesMunitionSpeed++;
                        }
                        // 10레벨 되면 이기면서 게임 끝 
                        if (level == 10)
                        {
                            GameOver("YOU WIN!");
                        }
                    }
                    // 여기서도 myEnemies의 좌표값을 조절하네.. ?
                    pb.Location = new Point((key + 1) * 50, -100);
                }

                if (myPlayer.Bounds.IntersectsWith(pb.Bounds))
                {
                    explosionMedia.settings.volume = 30;
                    explosionMedia.controls.play();
                    myPlayer.Visible = false;
                    GameOver("GameOver");
                }
            }
        }

        // 적 비행기 "총알" 보여지는 Timer 이벤트 핸들러 
        private void EnemiesMunitionTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < (myEnemiesMunition.Length - difficulty); i++)
            {
                if (myEnemiesMunition[i].Top < this.Height)
                {
                    myEnemiesMunition[i].Visible = true;
                    myEnemiesMunition[i].Top += enemiesMunitionSpeed;

                    CollisionWithEnemiesMunition();
                }
                else
                {
                    myEnemiesMunition[i].Visible = false;
                    int x = rnd.Next(0, 10);
                    myEnemiesMunition[i].Location = new Point(myEnemies[x].Location.X + 20, myEnemies[x].Location.Y + 30);
                }
            }
        }

        private void CollisionWithEnemiesMunition()
        {
            for (int i = 0; i < myEnemiesMunition.Length; i++)
            {
                if (myEnemiesMunition[i].Bounds.IntersectsWith(myPlayer.Bounds))
                {
                    myEnemiesMunition[i].Visible = false;
                    explosionMedia.settings.volume = 30;
                    explosionMedia.controls.play();
                    myPlayer.Visible = false;
                    GameOver("Game Over");
                }
            }
        }
        //[끝] 내 비행기 충돌시 로직 
        private void GameOver(String str)
        {
            label1.Text = str;
            label1.Location = new Point(120, 120);
            label1.Visible = true;
            ReplayBtn.Visible = true;
            ExitBtn.Visible = true;

            gameMedia.controls.stop();
            StopTimers();
        }
        // Stop Timers
        private void StopTimers()
        {
            MoveBgTimer.Stop();
            MoveEnemiesTimer.Stop();
            MoveMunitionTimer.Stop();
            EnemiesMunitionTimer.Stop();
        }

        //Start Timers 
        private void StartTimers()
        {
            MoveBgTimer.Start();
            MoveEnemiesTimer.Start();
            MoveMunitionTimer.Start();
            EnemiesMunitionTimer.Start();
        }

        // HUD를 내가 조종하는 패널로 이동
        private void AttachHudTo(Panel myPanel)
        {
            // 디자이너 컨트롤들을 한 묶음으로
            Control[] hudAlwaysOn = { scoreTitle, scorelbl, levelTitle, levellbl };
            Control[] hudGameOver = { label1, ReplayBtn, ExitBtn }; // 게임오버 때만 표시

            foreach (var c in hudAlwaysOn)
            {
                c.Parent = myPanel;
                c.Visible = true;
                c.BringToFront();
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }

            foreach (var c in hudGameOver)
            {
                c.Parent = myPanel;
                c.Visible = false;            // ← 기본 숨김
                c.BringToFront();
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }

            // 패널이 바뀌면 좌표계도 바뀌니, 필요한 경우 위치를 한 번 정렬
            // (원래 패널에 맞춘 절대 좌표라면 건드릴 필요 없음)
            PositionHud(myPanel);
        }

        // HUD 배치(원하는 위치로 간단히 정리)
        private void PositionHud(Panel myPanel)
        {
            // 예: 좌측 상단 정리 배치 (원하던 기존 위치가 있으면 그 좌표 그대로 써도 됨)
            scoreTitle.Location = new Point(19, 10);
            scorelbl.Location = new Point(109, scoreTitle.Top);

            levelTitle.Location = new Point(456, 10);
            levellbl.Location = new Point(536, levelTitle.Top);

            // 중앙 안내 라벨/버튼(게임오버/리플레이용)도 내 패널 기준으로
            label1.Location = new Point(myPanel.Width / 2 - 80, myPanel.Height / 2 - 40);
            ReplayBtn.Location = new Point(175, 211);
            ExitBtn.Location = new Point(175, 279);
        }

        private async void SendStateTimer_Tick(object sender, EventArgs e)
        {
            if (client == null || client.Stream == null) return;
            var playerState = new Player();
            // 플레이어 상태 생성
            if (myPlayer == player1)
            {
                playerState.X = player1.Left;
                playerState.Y = player1.Top;
                playerState.Health = 100; // 실제 체력 값으로 바꿔야함. 
            }
            else
            {
                playerState.X = player2.Left;
                playerState.Y = player2.Top;
                playerState.Health = 100; // 실제 체력 값으로 바꿔야함. 
            }    

            // 적 상태 생성
            var enemyStates = new List<Enemy>();
            foreach (var enemy in myEnemies)
            {
                var pb = enemy.Value;
                if (pb.Visible)
                {
                    enemyStates.Add(new Enemy
                    {
                        Id = (int)pb.Tag,
                        X = pb.Left,
                        Y = pb.Top
                    });
                    
                }
            }

            // 전체 State 객체 생성
            var state = new State
            {
                Role = role,           // 1 또는 2
                Player = playerState,
                Enemies = enemyStates
            };

            // 서버로 전송
            await Packet.SendStateAsync(client.Stream, state).ConfigureAwait(false);
        }
        // [끝] 내 패널의 State 를 서버로 전송 

        // [시작] 서버에서 받은 State, UI에 반영
        private async Task StartReceivingLoop()
        {
            while (true)
            {
                try
                {
                    State state = await Packet.ReceiveStateAsync(client.Stream).ConfigureAwait(false);
                    if (state != null)
                        UpdateUI(state);
                }
                catch
                {
                    break;
                }
            }
        }

        public void UpdateUI(State state)
        {
            if (state == null || this.IsDisposed) return;

            // 항상 UI 스레드에서 실행되도록 보장
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateUI(state)));
                return;
            }
            // Form1_Load 와 중복 선언중이어서 class 멤버로 선언하고 Form1_Load에서 초기화하는걸로 변경 
            //Panel myPanel = (role == 1) ? splitContainer1.Panel1 : splitContainer1.Panel2;
            //Panel opponentPanel = (role == 1) ? splitContainer1.Panel2 : splitContainer1.Panel1;

            // 포커스 설정 1회
            if (!focusSet)
            {
                myPanel.Focus();
                focusSet = true;
            }

            if (state.Role == role)
            {
                // --- 내 화면 업데이트 (내 플레이어, 내 적들) ---
                myPlayer.Left = state.Player.X;
                myPlayer.Top = state.Player.Y;

                // Enemies: 서버가 보내는 내 적들(게임 내 적들)을 동기화
                SyncEnemiesDictionary(myEnemies, state.Enemies, myPanel);
            }
            else
            {
                // --- 상대 화면 업데이트 (상대 플레이어, 상대 적들) ---
                opponentPlayer.Left = state.Player.X;
                opponentPlayer.Top = state.Player.Y;

                SyncEnemiesDictionary(opponentEnemies, state.Enemies, opponentPanel);
            }
        }

        // 엔티티 딕셔너리 동기화: 존재하지 않으면 생성, 존재하면 위치 갱신, 서버에 없으면 제거
        private void SyncEnemiesDictionary(Dictionary<int, PictureBox> dict, List<Enemy> states, Panel parentPanel)
        {
            var existingIds = new HashSet<int>(dict.Keys);

            foreach (var e in states)
            {
                if (dict.ContainsKey(e.Id))
                {
                    var pb = dict[e.Id];
                    pb.Left = e.X;
                    pb.Top = e.Y;
                    pb.Visible = true;
                    existingIds.Remove(e.Id);
                }
                else
                {
                    var pb = new PictureBox
                    {
                        Width = 40,
                        Height = 40,
                        Left = e.X,
                        Top = e.Y,
                        BackColor = Color.Red,
                        Tag = e.Id,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Parent = parentPanel,
                        Visible = true
                    };
                    dict[e.Id] = pb;
                }
            }
            // 서버에 없는 적 제거
            foreach (var id in existingIds)
            {
                var pb = dict[id];
                if (pb != null)
                {
                    parentPanel.Controls.Remove(pb);
                    pb.Dispose();
                }
                dict.Remove(id);
            }
        }
        // [끝] 서버에서 받은 State, UI에 반영


        // [시작] Timer_Tick 이벤트 핸들러 모음 
        private void MoveBgTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < myStars.Length / 2; i++)
            {
                myStars[i].Top += backgroundspeed;

                if (myStars[i].Top >= this.Height)
                {
                    myStars[i].Top = -myStars[i].Height;
                }
            }

            for (int i = myStars.Length / 2; i < myStars.Length; i++)
            {
                myStars[i].Top += backgroundspeed - 2;

                if (myStars[i].Top >= this.Height)
                {
                    myStars[i].Top = -myStars[i].Height;
                }
            }
        }

        private void LeftMoveTimer_Tick(object sender, EventArgs e)
        {
            if (myPlayer.Left > 10)
            {
                myPlayer.Left -= playerSpeed;
            }
        }

        private void RightMoveTimer_Tick(object sender, EventArgs e)
        {
            if (myPlayer.Right < 580)
            {
                myPlayer.Left += playerSpeed;
            }
        }

        private void DownMoveTimer_Tick(object sender, EventArgs e)
        {
            if (myPlayer.Top < 400)
            {
                myPlayer.Top += playerSpeed;
            }
        }

        private void UpMoveTimer_Tick(object sender, EventArgs e)
        {
            if (myPlayer.Top > 10)
            {
                myPlayer.Top -= playerSpeed;
            }
        }
        // [끝] Timer_Tick 이벤트 핸들러 모음 

        // [시작] 키입력 로직
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // 게임오버 됐을때에는 player 움직이지 않도록 해야하니까 pause 체크 
            if (!pause)
            {
                if (e.KeyCode == Keys.Right)
                    RightMoveTimer.Start();
                if (e.KeyCode == Keys.Left)
                    LeftMoveTimer.Start();
                if (e.KeyCode == Keys.Up)
                    UpMoveTimer.Start();
                if (e.KeyCode == Keys.Down)
                    DownMoveTimer.Start();
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            RightMoveTimer.Stop();
            LeftMoveTimer.Stop();
            UpMoveTimer.Stop();
            DownMoveTimer.Stop();

            if (e.KeyCode == Keys.Space)
            {
                if (!gameIsOver)
                {
                    if (pause)
                    {
                        StartTimers();
                        label1.Visible = false;
                        gameMedia.controls.play();
                        pause = false;
                    }
                    else
                    {
                        // label1.Location = new Point(this.Width / 2 - 120, 150);
                        label1.Text = "PAUSED";
                        label1.Visible = true;
                        gameMedia.controls.pause();
                        StopTimers();
                        pause = true;
                    }
                }
            }
        }
        // [끝] 키입력 로직 

        // 플레이어에서 총알 나감. 
        private void MoveMunitionTimer_Tick(object sender, EventArgs e)
        {
            shootgMedia.controls.play();
            for (int i = 0; i < myMunitions.Length; i++)
            {
                if (myMunitions[i].Top > 0)
                {
                    myMunitions[i].Visible = true;
                    myMunitions[i].Top -= munitionSpeed;

                    Collision();
                }
                else
                {
                    myMunitions[i].Visible = false;
                    myMunitions[i].Location = new Point(myPlayer.Location.X + 20, myPlayer.Location.Y - i * 30);
                }
            }


        }

        private void MoveEnemiesTimer_Tick(object sender, EventArgs e)
        {
            MoveEnemies(myEnemies, enemiSpeed);
        }

        private void MoveEnemies(Dictionary<int, PictureBox> dict, int speed)
        {
            foreach (var kvp in dict) // key-value 쌍 순회
            {
                PictureBox enemy = kvp.Value; // PictureBox만 꺼냄
                enemy.Visible = true;
                enemy.Top += speed;

                if (enemy.Top > this.Height)
                {
                    // 키 값(kvp.Key)을 이용해서 새 위치 계산 가능
                    enemy.Location = new Point((kvp.Key + 1) * 50, -200);
                }
            }
        }
        // ----------------------------
        // 폼 닫기 시 정리
        // ----------------------------
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (client != null)
                {
                    // client.Close/Disconnect 등 필요시 호출
                }
            }
            catch { }
        }

        private void ReplayBtn_Click_1(object sender, EventArgs e)
        {
            // 상태값 초기화
            score = 0;
            level = 1;
            difficulty = 9;
            gameIsOver = false;
            pause = false;

            label1.Visible = false;
            ReplayBtn.Visible = false;
            ExitBtn.Visible = false;

            // 플레이어/적 재배치
            myPlayer.Visible = true;
            myPlayer.Location = new Point(250, 400);

            foreach (var kvp in myEnemies)
            {
                var idx = kvp.Key;
                var pb = kvp.Value;
                pb.Location = new Point((idx + 1) * 50, -100);
                pb.Visible = true;
            }

            // 총알/별 재초기화(필요한 범위만)
            for (int i = 0; i < myMunitions.Length; i++)
            {
                myMunitions[i].Visible = false;
                myMunitions[i].Location = new Point(myPlayer.Left + 20, myPlayer.Top - i * 30);
            }

            // 타이머/사운드 재개
            StartTimers();
            gameMedia.controls.play();

            // 혹시 포커스 잃었으면 다시 주기
            var myPanel = (role == 1) ? splitContainer1.Panel1 : splitContainer1.Panel2;
            myPanel.Focus();
        }

        private void ExitBtn_Click_1(object sender, EventArgs e)
        {
            try
            {
                client?.Disconnect(); // 서버연결을 끊고 
            }
            catch { }

            Application.Exit(); // 프로그램 정상 종료 
            // 아래 기존 코드 , 강제종료하는 코드였음. 
            // Environment.Exit(1);
        }

        //private async void ReplayBtn_Click(object sender, EventArgs e)
        //{
        //    // 게임 관련 변수 초기화
        //    score = 0;
        //    level = 1;
        //    difficulty = 9;
        //    gameIsOver = false;
        //    pause = false;

        //    label1.Visible = false;
        //    ReplayBtn.Visible = false;
        //    ExitBtn.Visible = false;

        //    // 플레이어 및 적 초기화
        //    myPlayer.Visible = true;
        //    myPlayer.Location = new Point(250, 400);
        //    foreach (var enemy in myEnemies.Values)
        //    {
        //        enemy.Location = new Point((int)enemy.Tag * 50, -100);
        //        enemy.Visible = true;
        //    }

        //    // 타이머 재시작
        //    StartTimers();
        //    gameMedia.controls.play();
        //}

        //// [시작] 내 패널의 State 를 서버로 전송 
        //private void ExitBtn_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        client?.Disconnect(); // 서버연결을 끊고 
        //    }
        //    catch { }

        //    Application.Exit(); // 프로그램 정상 종료 
        //    // 아래 기존 코드 , 강제종료하는 코드였음. 
        //    // Environment.Exit(1);
        //}

    }
}


