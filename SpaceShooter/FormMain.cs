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
       
        // 예전 생성자 코드 
        //public FormMain()
        //{
        //    InitializeComponent();

        //    // 폼에서 키 이벤트 받기
        //    this.KeyPreview = true;
        //    this.KeyDown += Form1_KeyDown;
        //    this.KeyUp += Form1_KeyUp;

        //    // 패널 포커스 허용
        //    splitContainer1.Panel1.TabStop = true;
        //    splitContainer1.Panel2.TabStop = true;

        //}
        // 새로 추가한 생성자
        public FormMain(Client clientInstance, int assignedRole)
        {
            InitializeComponent();

            client = clientInstance;
            role = assignedRole;
            // 폼에서 키 이벤트 받기
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            // 패널 포커스 허용
            splitContainer1.Panel1.TabStop = true;
            splitContainer1.Panel2.TabStop = true;

            // 최소 초기값 세팅 
            rnd = new Random();

            this.Load += Form1_Load;
            this.FormClosing += FormMain_FormClosing;

            // 연결 후 수신 루프 시작
            // _ = Task.Run(() => StartReceivingLoop());
        }
        private void Form1_Load(object sender, EventArgs e)
        {

            // myPanel / opponentPanel 결정 (role 기준)
            Panel myPanel = (role == 1) ? splitContainer1.Panel1 : splitContainer1.Panel2;
            Panel opponentPanel = (role == 1) ? splitContainer1.Panel2 : splitContainer1.Panel1;

            // 디자이너에 올려둔 player1/player2의 Parent를 적절히 설정 (이미 Form 디자이너에 존재)
            // player1, player2 컨트롤 이름은 디자이너와 동일해야 합니다.
            // (Designer에서 `player1` `player2` 가 존재하지 않으면 NullRef 발생)
            myPlayer = (role == 1) ? player1 : player2;
            opponentPlayer = (role == 1) ? player2 : player1;

            // 부모 패널로 배치 (안정적: Load 이벤트에서 진행)
            myPlayer.Parent = myPanel;
            opponentPlayer.Parent = opponentPanel;

            // 미디어 초기화 (파일 경로는 프로젝트에 맞게 조정)
            InitMediaPlayers();

            // 패널별 게임 오브젝트 초기화 (총알, 별, 적 총알)
            InitGameObjects(myPanel, opponentPanel);

            // 포커스 한 번 설정
            myPanel.Focus();
            focusSet = true;

            // 수신 루프 시작 (폼이 완전히 로드된 뒤 시작)
            if (client != null)
                _ = Task.Run(() => StartReceivingLoop());
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
            // 이코드랑 밑에 코드랑 둘중택일해야함 
            myEnemiesMunition = new PictureBox[10];

            for (int i = 0; i < myEnemiesMunition.Length; i++)
            {
                myEnemiesMunition[i] = new PictureBox();
                myEnemiesMunition[i].Size = new Size(2, 25);
                myEnemiesMunition[i].Visible = false;
                myEnemiesMunition[i].BackColor = Color.Yellow;
                int x = rnd.Next(0, 10);
                myEnemiesMunition[i].Location = new Point(enemies[x].Location.X, enemies[x].Location.Y - 20);
                // 스플릿 컨테이너 하기 전 코드
                // this.Controls.Add(enemiesMunition[i]);
                // 스플릿 컨테이너 후 코드
                splitContainer1.Panel1.Controls.Add(myEnemiesMunition[i]);
            }
            // --- 적 총알 (선택) ---
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
                    Location = new Point()//여기써야함

                    Parent = myPanel
                };

                //opponentEnemiesMunition[i] = new PictureBox
                //{
                //    Size = new Size(2, 25),
                //    BackColor = Color.Yellow,
                //    Visible = false,
                //    Parent = opponentPanel
                //};
            }




























            //Load images
            Image munition = SpaceShooter.Properties.Resources.munition;

            // Load images for enemies 
            Image enemi1 = SpaceShooter.Properties.Resources.E1;
            Image enemi2 = SpaceShooter.Properties.Resources.E2;
            Image enemi3 = SpaceShooter.Properties.Resources.E3;
            Image boss1 = SpaceShooter.Properties.Resources.Boss1;
            Image boss2 = SpaceShooter.Properties.Resources.Boss2;

            enemies = new PictureBox[10];

            // 적 비행기 초기화 
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i] = new PictureBox();
                enemies[i].Size = new Size(40, 40);
                enemies[i].SizeMode = PictureBoxSizeMode.Zoom;
                enemies[i].BorderStyle = BorderStyle.None;
                enemies[i].Visible = false; // 게임하는 사람이 처음부터 볼 필요 없으니 우선 false로 한다고 함. 
                // 스플릿 컨테이너 하기 전 코드
                // this.Controls.Add(enemies[i]);
                // 스플릿 컨테이너 후 코드
                splitContainer1.Panel1.Controls.Add(enemies[i]);
                enemies[i].Location = new Point((i + 1) * 50, -50); // 이 위치는 화면 밖 위 쪽에서 시작. 
            }

            enemies[0].Image = boss1;
            enemies[1].Image = enemi2;
            enemies[2].Image = enemi3;
            enemies[3].Image = enemi3;
            enemies[4].Image = enemi1;
            enemies[5].Image = enemi3;
            enemies[6].Image = enemi2;
            enemies[7].Image = enemi3;
            enemies[8].Image = enemi2;
            enemies[9].Image = boss2;


            for (int i = 0; i<munitions.Length; i++)
            {
                munitions[i] = new PictureBox();
                munitions[i].Size = new Size(8, 8);
                munitions[i].Image = munition;
                munitions[i].SizeMode = PictureBoxSizeMode.Zoom;
                munitions[i].BorderStyle = BorderStyle.None;
                // 스플릿 컨테이너 하기 전 코드
                // this.Controls.Add(munitions[i]);
                // 스플릿 컨테이너 후 코드
                splitContainer1.Panel1.Controls.Add(munitions[i]);
            }
            //Create WMP
            gameMedia = new WindowsMediaPlayer();
            shootgMedia = new WindowsMediaPlayer();
            explosionMedia = new WindowsMediaPlayer(); 

            // Load all songs 
            gameMedia.URL = "songs\\GameSong.mp3";
            shootgMedia.URL = "songs\\shoot.mp3";
            explosionMedia.URL = "songs\\boom.mp3"; 

            //Setup Songs settings 
            gameMedia.settings.setMode("loop", true);
            gameMedia.settings.volume = 5;
            shootgMedia.settings.volume = 1;
            explosionMedia.settings.volume = 6;

            stars = new PictureBox[15];
            rnd = new Random();

            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = new PictureBox();
                stars[i].BorderStyle = BorderStyle.None;
                stars[i].Location = new Point(rnd.Next(20, 580), rnd.Next(-10, 400));
                if (i % 2 == 1)
                {
                    stars[i].Size = new Size(2, 2);
                    stars[i].BackColor = Color.Wheat;
                }
                else
                {
                    stars[i].Size = new Size(3, 3);
                    stars[i].BackColor = Color.DarkGray;
                }
                // 스플릿 컨테이너 하기 전 코드 
                // this.Controls.Add(stars[i]);
                // 스플릿 컨테이너 후 코드
                splitContainer1.Panel1.Controls.Add(stars[i]);
            }

            enemiesMunition = new PictureBox[10];

            for (int i = 0; i < enemiesMunition.Length; i++)
            {
                enemiesMunition[i] = new PictureBox();
                enemiesMunition[i].Size = new Size(2, 25);
                enemiesMunition[i].Visible = false;
                enemiesMunition[i].BackColor = Color.Yellow;
                int x = rnd.Next(0, 10);
                enemiesMunition[i].Location = new Point(enemies[x].Location.X, enemies[x].Location.Y - 20);
                // 스플릿 컨테이너 하기 전 코드
                // this.Controls.Add(enemiesMunition[i]);
                // 스플릿 컨테이너 후 코드
                splitContainer1.Panel1.Controls.Add(enemiesMunition[i]);
            }

            // 배경음악 실행 
            gameMedia.controls.play();
        }

        private void MoveBgTimer_Tick(object sender, EventArgs e)
        {
            for(int i = 0; i < stars.Length/2; i++)
            {
                stars[i].Top += backgroundspeed;

                if (stars[i].Top >= this.Height)
                {
                    stars[i].Top = -stars[i].Height;
                }
            }

            for(int i = stars.Length/2; i < stars.Length; i++)
            {
                stars[i].Top += backgroundspeed - 2;

                if (stars[i].Top >= this.Height)
                {
                    stars[i].Top = -stars[i].Height;
                }
            }
        }

        private void LeftMoveTimer_Tick(object sender, EventArgs e)
        {
            if (player1.Left > 10)
            {
                player1.Left -= playerSpeed;
            }
        }

        private void RightMoveTimer_Tick(object sender, EventArgs e)
        {
            if (player1.Right < 580)
            {
                player1.Left += playerSpeed;
            }
        }

        private void DownMoveTimer_Tick(object sender, EventArgs e)
        {
            if (player1.Top < 400)
            {
                player1.Top += playerSpeed;
            }
        }

        private void UpMoveTimer_Tick(object sender, EventArgs e)
        {
            if (player1.Top > 10)
            {
                player1.Top -= playerSpeed;
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
                    if(pause)
                    {
                        StartTimers();
                        label1.Visible = false;
                        gameMedia.controls.play();
                        pause = false;
                    }
                    else
                    {
                        label1.Location = new Point(this.Width/2 - 120, 150);
                        label1.Text = "PAUSED";
                        label1.Visible = true;
                        gameMedia.controls.pause();
                        StopTimers();
                        pause = true;
                    }
                }
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // 게임오버 됐을때에는 player 움직이지 않도록 해야하니까 pause 체크 
            if (!pause)
            {
                if (role == 1) // player1
                {
                    if (e.KeyCode == Keys.Right)
                        RightMoveTimer.Start();
                    if (e.KeyCode == Keys.Left)
                    {
                        LeftMoveTimer.Start();
                    }
                    if (e.KeyCode == Keys.Up)
                    {
                        UpMoveTimer.Start();
                    }
                    if (e.KeyCode == Keys.Down)
                    {
                        DownMoveTimer.Start();
                    }
                }
                else if (role == 2) // player2
                {
                    if (e.KeyCode == Keys.Right)
                    {
                        RightMoveTimer.Start();
                    }
                    if (e.KeyCode == Keys.Left)
                    {
                        LeftMoveTimer.Start();
                    }
                    if (e.KeyCode == Keys.Up)
                    {
                        UpMoveTimer.Start();
                    }
                    if (e.KeyCode == Keys.Down)
                    {
                        DownMoveTimer.Start();
                    }
                }
            }
        }

        private void MoveMunitionTimer_Tick(object sender, EventArgs e)
        {
            shootgMedia.controls.play();
            for (int i = 0; i < munitions.Length; i++)
            {
                if (munitions[i].Top > 0)
                {
                    munitions[i].Visible = true;
                    munitions[i].Top -= munitionSpeed;

                    Collision();
                }
                else
                {
                    munitions[i].Visible = false;
                    munitions[i].Location = new Point(player1.Location.X + 20, player1.Location.Y - i * 30);
                }
            }
        }

        private void MoveEnemiesTimer_Tick(object sender, EventArgs e)
        {
            MoveEnemies(enemies, enemiSpeed);

        }
        private void MoveEnemies(PictureBox[] array, int speed)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Visible = true;
                array[i].Top += speed;

                if (array[i].Top > this.Height)
                {
                    array[i].Location = new Point((i + 1) * 50, -200);
                }
            }
        }
        private void Collision()
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (munitions[0].Bounds.IntersectsWith(enemies[i].Bounds)
                    || munitions[1].Bounds.IntersectsWith(enemies[i].Bounds)
                    || munitions[2].Bounds.IntersectsWith(enemies[i].Bounds))
                {
                    explosionMedia.controls.play();

                    score += 1;
                    scorelbl.Text = (score < 10) ? "0" + score.ToString() : score.ToString();

                    if (score % 30 == 0)
                    {
                        level += 1;
                        levellbl.Text = (level < 10) ? "0" + level.ToString() : level.ToString();

                        if (enemiSpeed <= 10 && enemiesMunitionSpeed <= 10 && difficulty >= 0)
                        {
                            difficulty--;
                            enemiSpeed++;
                            enemiesMunitionSpeed++;
                        }
                        if (level == 10)
                        {
                            GameOver("YOU WIN!");
                        }
                    }

                    enemies[i].Location = new Point((i + 1) * 50, -100);
                }

                if (player1.Bounds.IntersectsWith(enemies[i].Bounds))
                {
                    explosionMedia.settings.volume = 30;
                    explosionMedia.controls.play();
                    player1.Visible = false;
                    GameOver("GameOver");
                }
            }
        }
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

        private void EnemiesMunitionTimer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < (enemiesMunition.Length - difficulty); i++)
            {
                if (enemiesMunition[i].Top < this.Height)
                {
                    enemiesMunition[i].Visible = true;
                    enemiesMunition[i].Top += enemiesMunitionSpeed;

                    CollisionWithEnemiesMunition();
                }
                else
                {
                    enemiesMunition[i].Visible = false;
                    int x = rnd.Next(0, 10);
                    enemiesMunition[i].Location = new Point(enemies[x].Location.X + 20, enemies[x].Location.Y + 30);
                }
            }
        }

        private void CollisionWithEnemiesMunition()
        {
            for (int i = 0; i < enemiesMunition.Length; i++)
            {
                if (enemiesMunition[i].Bounds.IntersectsWith(player1.Bounds))
                {
                    enemiesMunition[i].Visible = false;
                    explosionMedia.settings.volume = 30;
                    explosionMedia.controls.play();
                    player1.Visible = false;
                    GameOver("Game Over");
                }
            }
        }

        private async void ReplayBtn_Click(object sender, EventArgs e)
        {
            this.Controls.Clear();
            InitializeComponent();
            Form1_Load(e, e);
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private async void SendStateTimer_Tick(object sender, EventArgs e)
        {
            if (client == null || client.Stream == null) return;

            // 플레이어 상태 생성
            var playerState = new Player
            {
                X = player1.Left,
                Y = player1.Top,
                Health = 100 // 예시, 필요하면 실제 체력으로
            };

            // 적 상태 생성
            var enemyStates = new List<Enemy>();
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].Visible)
                {
                    enemyStates.Add(new Enemy
                    {
                        Id = i,
                        X = enemies[i].Left,
                        Y = enemies[i].Top
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
            await Packet.SendStateAsync(client.Stream, state);
        }


        // 서버에서 받은 State를 UI에 반영
        public void UpdatePictureBox(State state)
        {
            //if (pictureBox1.InvokeRequired)
            //{
            //    pictureBox1.Invoke(new Action(() => UpdatePictureBox(state)));
            //    return;
            //}

            //// 예: 플레이어 좌표
            //pictureBox1.Left = state.PlayerX;
            //pictureBox1.Top = state.PlayerY;

            // 필요 시 적 좌표도 반영 가능
            // pictureBoxEnemy.Left = state.EnemyX;
            // pictureBoxEnemy.Top = state.EnemyY;
        }

        private async Task StartReceivingLoop()
        {
            while (true)
            {
                try
                {
                    State state = await Packet.ReceiveStateAsync(client.Stream);
                    if (state != null)
                        UpdatePictureBox(state);
                }
                catch
                {
                    break;
                }
            }
        }
    }
}


