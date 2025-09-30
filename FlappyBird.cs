using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace FlappyBird
{
    // Ana Program
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FlappyBirdGame());
        }
    }

    // Zorluk seviyeleri
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public class DifficultySettings
    {
        public int PipeGap { get; set; }
        public int PipeSpacing { get; set; }
        public float PipeSpeed { get; set; }
        public float SecondsPerSpeedup { get; set; }
        public float SpeedupDelta { get; set; }

        public static DifficultySettings For(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy:
                    return new DifficultySettings { PipeGap = 190, PipeSpacing = 320, PipeSpeed = 2.2f, SecondsPerSpeedup = 12f, SpeedupDelta = 0.15f };
                case Difficulty.Hard:
                    return new DifficultySettings { PipeGap = 140, PipeSpacing = 260, PipeSpeed = 3.2f, SecondsPerSpeedup = 8f, SpeedupDelta = 0.25f };
                default:
                    return DefaultNormal();
            }
        }

        public static DifficultySettings DefaultNormal()
        {
            return new DifficultySettings { PipeGap = 160, PipeSpacing = 280, PipeSpeed = 2.5f, SecondsPerSpeedup = 10f, SpeedupDelta = 0.2f };
        }
    }

    public static class DifficultyExtensions
    {
        public static DifficultySettings GetSettings(this Difficulty d)
        {
            return DifficultySettings.For(d);
        }
    }

    // Kuş Kostümleri
    public enum BirdCostume
    {
        Classic,
        Red,
        Blue,
        Rainbow,
        Golden,
        Green,
        Purple
    }

    // Kuş Sınıfı
    public class Bird
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Velocity { get; set; }
        public float Gravity { get; set; } = 0.6f; // Eski tek değer (artık kullanılmıyor ama geriye uyum için tutuluyor)
        public float RiseGravity { get; set; } = 0.45f; // Yükselirken uygulanan yerçekimi
        public float FallGravity { get; set; } = 0.8f;  // Düşerken uygulanan yerçekimi
        public float Drag { get; set; } = 0.995f;       // Hava direnci (hızı hafifçe azaltır)
        public float JumpCutFactor { get; set; } = 0.6f; // Space bırakıldığında yükselişi kesme
        public float JumpPower { get; set; } = -10f;
        public float MaxVelocity { get; set; } = 8f;
        public int Size { get; set; } = 35;
        public BirdCostume Costume { get; set; } = BirdCostume.Classic;
        public float WingAnimation { get; set; } = 0;
        public float RotationDeg { get; private set; } = 0f; // Hıza bağlı görsel açı

        public Bird(float x, float y)
        {
            X = x;
            Y = y;
            Velocity = 0;
        }

        public void Update(bool isJumpHeld)
        {
            // Geliştirilmiş fizik: yükselirken daha düşük, düşerken daha yüksek yerçekimi
            float gravityToApply = Velocity < 0 ? RiseGravity : FallGravity;

            // Jump cut: Yükselirken Space bırakılırsa daha hızlı aşağı çek
            if (Velocity < 0 && !isJumpHeld)
            {
                gravityToApply += FallGravity * (1f - JumpCutFactor);
            }

            Velocity += gravityToApply;
            Velocity *= Drag; // Hava direnci uygula
            Velocity = Math.Max(-12f, Math.Min(Velocity, MaxVelocity)); // Yukarı doğru da makul sınır
            Y += Velocity;
            
            // Kanat animasyonu
            WingAnimation += 0.3f;

            // Görsel dönüş açısı: hız arttıkça aşağı doğru bak
            // Velocity -12..+8 aralığını -20°..+90° aralığına haritala
            float t = (Velocity + 12f) / (8f + 12f);
            t = Math.Max(0f, Math.Min(1f, t));
            RotationDeg = -20f + t * (90f + 20f);
        }

        public void Jump()
        {
            Velocity = JumpPower;
            WingAnimation = 0; // Zıpladığında kanat animasyonunu sıfırla
        }

        public void Draw(Graphics g)
        {
            // Kuş gövdesi (kostüm rengine göre)
            Color bodyColor = GetBodyColor();
            Color wingColor = GetWingColor();
            Color beakColor = GetBeakColor();
            
            // Dönüş için merkez etrafında transform kullan
            PointF center = new PointF(X + Size / 2f, Y + Size / 2f);
            GraphicsState state = g.Save();
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(RotationDeg);
            g.TranslateTransform(-center.X, -center.Y);

            // Gölge efekti
            g.FillEllipse(Brushes.DarkGray, X + 2, Y + 2, Size, Size);

            // Kuş gövdesi
            using (SolidBrush bodyBrush = new SolidBrush(bodyColor))
            {
                g.FillEllipse(bodyBrush, X, Y, Size, Size);
            }

            // Kuş gözü
            g.FillEllipse(Brushes.White, X + 22, Y + 10, 8, 8);
            g.FillEllipse(Brushes.Black, X + 24, Y + 12, 4, 4);

            // Kuş gagası
            using (SolidBrush beakBrush = new SolidBrush(beakColor))
            {
                g.FillPolygon(beakBrush, new PointF[]
                {
                    new PointF(X + Size, Y + 15),
                    new PointF(X + Size + 10, Y + 18),
                    new PointF(X + Size, Y + 21)
                });
            }

            // Animasyonlu kanatlar
            DrawWings(g, wingColor);

            // Özel efektler
            DrawSpecialEffects(g);

            g.Restore(state);
        }

        private void DrawWings(Graphics g, Color wingColor)
        {
            float wingOffset = (float)Math.Sin(WingAnimation) * 3;
            
            using (SolidBrush wingBrush = new SolidBrush(wingColor))
            {
                // Sol kanat
                g.FillEllipse(wingBrush, X + 5, Y + 8 + wingOffset, 18, 18);
                // Sağ kanat
                g.FillEllipse(wingBrush, X + 12, Y + 15 + wingOffset, 15, 12);
            }
        }

        private void DrawSpecialEffects(Graphics g)
        {
            switch (Costume)
            {
                case BirdCostume.Rainbow:
                    // Gökkuşağı efekti
                    using (LinearGradientBrush rainbowBrush = new LinearGradientBrush(
                        new RectangleF(X, Y, Size, Size),
                        Color.Red, Color.Purple, 45f))
                    {
                        g.FillEllipse(rainbowBrush, X - 2, Y - 2, Size + 4, Size + 4);
                    }
                    break;
                    
                case BirdCostume.Golden:
                    // Altın parıltı efekti
                    using (LinearGradientBrush goldBrush = new LinearGradientBrush(
                        new RectangleF(X, Y, Size, Size),
                        Color.Yellow, Color.Orange, 45f))
                    {
                        g.FillEllipse(goldBrush, X - 1, Y - 1, Size + 2, Size + 2);
                    }
                    break;
            }
        }

        private Color GetBodyColor()
        {
            return Costume switch
            {
                BirdCostume.Red => Color.Red,
                BirdCostume.Blue => Color.Blue,
                BirdCostume.Classic => Color.Yellow,
                BirdCostume.Rainbow => Color.White,
                BirdCostume.Golden => Color.Gold,
                BirdCostume.Green => Color.Green,
                BirdCostume.Purple => Color.MediumPurple,
                _ => Color.Yellow
            };
        }

        private Color GetWingColor()
        {
            return Costume switch
            {
                BirdCostume.Red => Color.DarkRed,
                BirdCostume.Blue => Color.DarkBlue,
                BirdCostume.Classic => Color.Orange,
                BirdCostume.Rainbow => Color.White,
                BirdCostume.Golden => Color.Orange,
                BirdCostume.Green => Color.DarkGreen,
                BirdCostume.Purple => Color.Purple,
                _ => Color.Orange
            };
        }

        private Color GetBeakColor()
        {
            return Costume switch
            {
                BirdCostume.Red => Color.Orange,
                BirdCostume.Blue => Color.Orange,
                BirdCostume.Classic => Color.Orange,
                BirdCostume.Rainbow => Color.Orange,
                BirdCostume.Golden => Color.Yellow,
                BirdCostume.Green => Color.Orange,
                BirdCostume.Purple => Color.Orange,
                _ => Color.Orange
            };
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(X + 5, Y + 5, Size - 10, Size - 10); // Daha hassas çarpışma
        }
    }

    // Boru Sınıfı
    public class Pipe
    {
        public float X { get; set; }
        public float TopHeight { get; set; }
        public float BottomY { get; set; }
        public int Width { get; set; } = 70;
        public int Gap { get; set; } = 160;
        public bool Scored { get; set; } = false;
        public float Speed { get; set; } = 3f;

        public Pipe(float x, int windowHeight, Random random)
        {
            X = x;
            TopHeight = random.Next(120, Math.Max(121, windowHeight - Gap - 120));
            BottomY = TopHeight + Gap;
        }

        public void Update()
        {
            X -= Speed;
        }

        public void Draw(Graphics g)
        {
            // Boru gölgeleri
            g.FillRectangle(Brushes.DarkGreen, X + 3, 0, Width, TopHeight);
            g.FillRectangle(Brushes.DarkGreen, X + 3, BottomY, Width, 1000);
            
            // Ana boru gövdesi
            using (LinearGradientBrush pipeBrush = new LinearGradientBrush(
                new RectangleF(X, 0, Width, TopHeight),
                Color.LimeGreen, Color.Green, 0f))
            {
                g.FillRectangle(pipeBrush, X, 0, Width, TopHeight);
                g.FillRectangle(pipeBrush, X, BottomY, Width, 1000);
            }
            
            // Boru kenarları (daha kalın)
            g.FillRectangle(Brushes.DarkGreen, X - 8, TopHeight - 25, Width + 16, 25);
            g.FillRectangle(Brushes.DarkGreen, X - 8, BottomY, Width + 16, 25);
            
            // Boru detayları
            g.DrawRectangle(Pens.DarkGreen, X, 0, Width, TopHeight);
            g.DrawRectangle(Pens.DarkGreen, X, BottomY, Width, 1000);
            
            // Boru içi detayları
            for (int i = 0; i < TopHeight; i += 20)
            {
                g.DrawLine(Pens.LightGreen, X + 10, i, X + Width - 10, i);
            }
            for (int i = (int)BottomY; i < BottomY + 500; i += 20)
            {
                g.DrawLine(Pens.LightGreen, X + 10, i, X + Width - 10, i);
            }
        }

        public RectangleF GetTopBounds()
        {
            return new RectangleF(X + 5, 0, Width - 10, TopHeight);
        }

        public RectangleF GetBottomBounds()
        {
            return new RectangleF(X + 5, BottomY, Width - 10, 1000);
        }

        public bool IsOffScreen(int windowWidth)
        {
            return X + Width < -50;
        }
    }

    // Boru Yöneticisi Sınıfı
    public class PipeManager
    {
        private List<Pipe> pipes;
        private Random random;
        private float basePipeSpeed = 2.5f;
        private float currentPipeSpeed;
        private int pipeSpacing = 280;
        private int windowWidth;
        private int windowHeight;
        private int score = 0;
        private float speedIncreaseTimer = 0;
        private readonly DifficultySettings settings;

        public PipeManager(DifficultySettings settings)
        {
            pipes = new List<Pipe>();
            random = new Random();
            windowWidth = 800;
            windowHeight = 600;
            this.settings = settings ?? DifficultySettings.DefaultNormal();
            basePipeSpeed = settings.PipeSpeed;
            currentPipeSpeed = basePipeSpeed;
            pipeSpacing = settings.PipeSpacing;
            InitializePipes();
        }

        private void InitializePipes()
        {
            pipes.Clear();
            for (int i = 0; i < 4; i++)
            {
                Pipe p = new Pipe(windowWidth + (i * pipeSpacing), windowHeight, random);
                p.Gap = settings.PipeGap;
                pipes.Add(p);
            }
        }

        public void Update()
        {
            // Hız artışı (zamanla oyun zorlaşır) – ayarlardan
            speedIncreaseTimer += 0.016f; // ~60 FPS varsayımı
            if (speedIncreaseTimer >= settings.SecondsPerSpeedup)
            {
                currentPipeSpeed += settings.SpeedupDelta;
                speedIncreaseTimer = 0;
            }

            foreach (var pipe in pipes)
            {
                pipe.Speed = currentPipeSpeed;
                pipe.Update();
            }

            // Ekrandan çıkan boruları kaldır
            pipes.RemoveAll(pipe => pipe.IsOffScreen(windowWidth));

            // Yeni borular ekle
            if (pipes.Count == 0 || pipes.Last().X < windowWidth - pipeSpacing)
            {
                Pipe p = new Pipe(windowWidth + 50, windowHeight, random);
                p.Gap = settings.PipeGap;
                pipes.Add(p);
            }
        }

        public void Draw(Graphics g)
        {
            foreach (var pipe in pipes)
            {
                pipe.Draw(g);
            }
        }

        public bool CheckCollision(Bird bird)
        {
            RectangleF birdBounds = bird.GetBounds();

            foreach (var pipe in pipes)
            {
                RectangleF topBounds = pipe.GetTopBounds();
                RectangleF bottomBounds = pipe.GetBottomBounds();

                if (birdBounds.IntersectsWith(topBounds) || birdBounds.IntersectsWith(bottomBounds))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetScore()
        {
            int newScore = 0;
            foreach (var pipe in pipes)
            {
                if (pipe.X + pipe.Width < 120 && !pipe.Scored)
                {
                    pipe.Scored = true;
                    newScore++;
                }
            }
            score += newScore;
            return score;
        }

        public void SetWindowSize(int width, int height)
        {
            windowWidth = width;
            windowHeight = height;
        }

        public void Reset()
        {
            score = 0;
            currentPipeSpeed = basePipeSpeed;
            speedIncreaseTimer = 0;
            InitializePipes();
        }
    }

    // Oyun Durumları
    public enum GameState
    {
        MainMenu,
        DifficultySelection,
        CostumeSelection,
        Playing,
        GameOver,
        Paused
    }

    // Ana Oyun Formu
    public class FlappyBirdGame : Form
    {
        private Timer gameTimer;
        private Bird bird;
        private PipeManager pipeManager;
        private GameState gameState = GameState.MainMenu;
        private int score = 0;
        private int highScore = 0;
        private BirdCostume selectedCostume = BirdCostume.Classic;
        private Difficulty selectedDifficulty = Difficulty.Normal;
        private float backgroundOffset = 0;
        private Font titleFont;
        private Font menuFont;
        private string scoreFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "highscore.txt");
        private string scoresListFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scores.txt");
        private List<int> topScores = new List<int>();

        // UI buton alanları (tıklama için)
        private Rectangle btnMainStart;
        private Rectangle btnMainExit;
        private Rectangle btnPauseResume;
        private Rectangle btnPauseRestart;
        private Rectangle btnPauseMenu;
        private Rectangle btnGameOverRestart;
        private Rectangle btnGameOverMenu;

        // Çarpışma efektleri
        private int screenShakeFrames = 0;
        private readonly Random fxRandom = new Random();
        private readonly List<Particle> particles = new List<Particle>();

        public FlappyBirdGame()
        {
            InitializeComponent();
            InitializeGame();
            LoadHighScore();
            LoadTopScores();
        }

        private void InitializeComponent()
        {
            this.Text = "Flappy Bird - Gelişmiş Versiyon";
            this.Size = new Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.SkyBlue;
            this.KeyPreview = true;
            this.DoubleBuffered = true;
            this.KeyDown += FlappyBirdGame_KeyDown;
            this.Paint += FlappyBirdGame_Paint;
            this.Resize += FlappyBirdGame_Resize;
            this.MouseDown += FlappyBirdGame_MouseDown;
            
            // Fontları başlat
            titleFont = new Font("Arial", 36, FontStyle.Bold);
            menuFont = new Font("Arial", 18, FontStyle.Bold);
        }

        private void InitializeGame()
        {
            bird = new Bird(150, 300);
            pipeManager = new PipeManager(selectedDifficulty.GetSettings());
            pipeManager.SetWindowSize(this.ClientSize.Width, this.ClientSize.Height);
            
            gameTimer = new Timer();
            gameTimer.Interval = 16; // 60 FPS
            gameTimer.Tick += GameTimer_Tick;
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            switch (gameState)
            {
                case GameState.Playing:
                    bird.Update(isJumpHeld);
                    pipeManager.Update();
                    backgroundOffset += 0.5f;

                    // Çarpışma kontrolü
                    if (pipeManager.CheckCollision(bird) || bird.Y < 0 || bird.Y > this.ClientSize.Height - 80)
                    {
                        gameState = GameState.GameOver;
                        gameTimer.Stop();
                        PlayHit();
                        StartScreenShake();
                        SpawnParticles(bird);
                        
                        // Yüksek skor güncelle
                        if (score > highScore)
                        {
                            highScore = score;
                            SaveHighScore();
                        }

                        // Top skorları güncelle
                        UpdateTopScores(score);
                    }

                    // Skor güncelleme
                    {
                        int old = score;
                        score = pipeManager.GetScore();
                        if (score > old)
                        {
                            PlayScore();
                        }
                    }
                    break;
                    
                case GameState.MainMenu:
                case GameState.CostumeSelection:
                case GameState.DifficultySelection:
                case GameState.GameOver:
                    backgroundOffset += 0.2f;
                    break;
            }

            // Ekran titreme süre sayacı ve parçacık güncelle
            if (screenShakeFrames > 0)
                screenShakeFrames--;
            UpdateParticles();

            this.Invalidate();
        }

        private bool isJumpHeld = false;
        private void FlappyBirdGame_KeyDown(object sender, KeyEventArgs e)
        {
            switch (gameState)
            {
                case GameState.MainMenu:
                    if (e.KeyCode == Keys.Enter)
                    {
                        gameState = GameState.DifficultySelection;
                    }
                    break;
                    
                case GameState.DifficultySelection:
                    if (e.KeyCode == Keys.D1)
                    {
                        selectedDifficulty = Difficulty.Easy;
                        gameState = GameState.CostumeSelection;
                    }
                    else if (e.KeyCode == Keys.D2)
                    {
                        selectedDifficulty = Difficulty.Normal;
                        gameState = GameState.CostumeSelection;
                    }
                    else if (e.KeyCode == Keys.D3)
                    {
                        selectedDifficulty = Difficulty.Hard;
                        gameState = GameState.CostumeSelection;
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        gameState = GameState.MainMenu;
                    }
                    break;
                    
                case GameState.CostumeSelection:
                    switch (e.KeyCode)
                    {
                        case Keys.D1:
                            selectedCostume = BirdCostume.Classic;
                            StartGame();
                            break;
                        case Keys.D2:
                            selectedCostume = BirdCostume.Red;
                            StartGame();
                            break;
                        case Keys.D3:
                            selectedCostume = BirdCostume.Blue;
                            StartGame();
                            break;
                        case Keys.D4:
                            selectedCostume = BirdCostume.Rainbow;
                            StartGame();
                            break;
                        case Keys.D5:
                            selectedCostume = BirdCostume.Golden;
                            StartGame();
                            break;
                        case Keys.D6:
                            selectedCostume = BirdCostume.Green;
                            StartGame();
                            break;
                        case Keys.D7:
                            selectedCostume = BirdCostume.Purple;
                            StartGame();
                            break;
                        case Keys.Escape:
                            gameState = GameState.MainMenu;
                            break;
                    }
                    break;
                    
                case GameState.Playing:
                    if (e.KeyCode == Keys.Space)
                    {
                        bird.Jump();
                        isJumpHeld = true;
                        PlayJump();
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        gameState = GameState.Paused;
                        gameTimer.Stop();
                    }
                    break;
                    
                case GameState.GameOver:
                    if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
                    {
                        RestartGame();
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        gameState = GameState.MainMenu;
                    }
                    break;
                    
                case GameState.Paused:
                    if (e.KeyCode == Keys.Escape)
                    {
                        gameState = GameState.Playing;
                        gameTimer.Start();
                    }
                    else if (e.KeyCode == Keys.R)
                    {
                        RestartGame();
                    }
                    else if (e.KeyCode == Keys.M)
                    {
                        gameState = GameState.MainMenu;
                    }
                    break;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.KeyCode == Keys.Space)
            {
                isJumpHeld = false;
            }
        }

        private void StartGame()
        {
            bird = new Bird(150, 300);
            bird.Costume = selectedCostume;
            pipeManager = new PipeManager(selectedDifficulty.GetSettings());
            pipeManager.SetWindowSize(this.ClientSize.Width, this.ClientSize.Height);
            score = 0;
            gameState = GameState.Playing;
            gameTimer.Start();
        }

        private void RestartGame()
        {
            StartGame();
        }

        private void FlappyBirdGame_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            // Ekran titremesi için küçük ofset uygula
            PointF shake = GetShakeOffset();
            g.TranslateTransform(shake.X, shake.Y);

            // Arka plan gradyanı
            DrawBackground(g);
            
            // Yer çizgisi
            DrawGround(g);
            
            // Oyun durumuna göre içerik çiz
            switch (gameState)
            {
                case GameState.MainMenu:
                    DrawMainMenu(g);
                    break;
                case GameState.DifficultySelection:
                    DrawDifficultySelection(g);
                    break;
                case GameState.CostumeSelection:
                    DrawCostumeSelection(g);
                    break;
                case GameState.Playing:
                    DrawGame(g);
                    break;
                case GameState.GameOver:
                    DrawGameOver(g);
                    break;
                case GameState.Paused:
                    DrawPaused(g);
                    break;
            }
        }

        private void DrawBackground(Graphics g)
        {
            // Gökyüzü gradyanı
            using (LinearGradientBrush skyBrush = new LinearGradientBrush(
                new PointF(0, 0), new PointF(0, this.ClientSize.Height),
                Color.LightBlue, Color.SkyBlue))
            {
                g.FillRectangle(skyBrush, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }
            
            // Bulutlar
            for (int i = 0; i < 5; i++)
            {
                float cloudX = (i * 200 + backgroundOffset) % (this.ClientSize.Width + 100) - 50;
                float cloudY = 50 + (i % 3) * 80;
                DrawCloud(g, cloudX, cloudY);
            }
            // Parçacıklar (çarpışma sonrası)
            DrawParticles(g);
        }

        // Basit parçacık efekti
        private class Particle
        {
            public float X;
            public float Y;
            public float VX;
            public float VY;
            public float Life;
            public Color Color;
        }

        private void SpawnParticles(Bird b)
        {
            for (int i = 0; i < 20; i++)
            {
                particles.Add(new Particle
                {
                    X = b.X + b.Size / 2f,
                    Y = b.Y + b.Size / 2f,
                    VX = (float)(fxRandom.NextDouble() * 6 - 3),
                    VY = (float)(fxRandom.NextDouble() * -6),
                    Life = 1.0f,
                    Color = Color.FromArgb(255, fxRandom.Next(180, 255), fxRandom.Next(0, 100), 0)
                });
            }
        }

        private void UpdateParticles()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.X += p.VX;
                p.Y += p.VY;
                p.VY += 0.3f;
                p.Life -= 0.03f;
                particles[i] = p;
                if (p.Life <= 0)
                    particles.RemoveAt(i);
            }
        }

        private void DrawParticles(Graphics g)
        {
            foreach (var p in particles)
            {
                int alpha = (int)(Math.Max(0, Math.Min(1, p.Life)) * 255);
                using (SolidBrush br = new SolidBrush(Color.FromArgb(alpha, p.Color)))
                {
                    g.FillEllipse(br, p.X, p.Y, 6, 6);
                }
            }
        }

        // Ekran titremesi
        private void StartScreenShake()
        {
            screenShakeFrames = 20; // ~0.3 sn
        }

        private PointF GetShakeOffset()
        {
            if (screenShakeFrames <= 0) return new PointF(0, 0);
            float amp = 4f;
            float ox = (float)(fxRandom.NextDouble() * 2 - 1) * amp;
            float oy = (float)(fxRandom.NextDouble() * 2 - 1) * amp;
            return new PointF(ox, oy);
        }

        // Ses efektleri (basit beep fallback)
        private void PlayJump()
        {
            SystemSounds.Asterisk.Play();
        }
        private void PlayScore()
        {
            SystemSounds.Beep.Play();
        }
        private void PlayHit()
        {
            SystemSounds.Hand.Play();
        }

        // Fare tıklama işlemleri
        private void FlappyBirdGame_MouseDown(object sender, MouseEventArgs e)
        {
            if (gameState == GameState.MainMenu)
            {
                if (btnMainStart.Contains(e.Location))
                {
                    gameState = GameState.DifficultySelection;
                    return;
                }
                if (btnMainExit.Contains(e.Location))
                {
                    this.Close();
                    return;
                }
            }
            else if (gameState == GameState.Paused)
            {
                if (btnPauseResume.Contains(e.Location))
                {
                    gameState = GameState.Playing;
                    gameTimer.Start();
                    return;
                }
                if (btnPauseRestart.Contains(e.Location))
                {
                    RestartGame();
                    return;
                }
                if (btnPauseMenu.Contains(e.Location))
                {
                    gameState = GameState.MainMenu;
                    return;
                }
            }
            else if (gameState == GameState.GameOver)
            {
                if (btnGameOverRestart.Contains(e.Location))
                {
                    RestartGame();
                    return;
                }
                if (btnGameOverMenu.Contains(e.Location))
                {
                    gameState = GameState.MainMenu;
                    return;
                }
            }
        }

        private void DrawCloud(Graphics g, float x, float y)
        {
            g.FillEllipse(Brushes.White, x, y, 40, 25);
            g.FillEllipse(Brushes.White, x + 20, y - 10, 35, 30);
            g.FillEllipse(Brushes.White, x + 40, y, 30, 25);
            g.FillEllipse(Brushes.White, x + 15, y + 10, 25, 20);
        }

        private void DrawGround(Graphics g)
        {
            // Yer gradyanı
            using (LinearGradientBrush groundBrush = new LinearGradientBrush(
                new PointF(0, this.ClientSize.Height - 80), new PointF(0, this.ClientSize.Height),
                Color.Green, Color.DarkGreen))
            {
                g.FillRectangle(groundBrush, 0, this.ClientSize.Height - 80, this.ClientSize.Width, 80);
            }
            
            // Yer detayları
            for (int i = 0; i < this.ClientSize.Width; i += 30)
            {
                g.DrawLine(Pens.DarkGreen, i, this.ClientSize.Height - 60, i + 15, this.ClientSize.Height - 70);
            }
        }

        private void DrawMainMenu(Graphics g)
        {
            // Başlık
            string title = "FLAPPY BIRD";
            SizeF titleSize = g.MeasureString(title, titleFont);
            float titleX = (this.ClientSize.Width - titleSize.Width) / 2;
            float titleY = 150;
            
            // Başlık gölgesi
            g.DrawString(title, titleFont, Brushes.Black, titleX + 3, titleY + 3);
            g.DrawString(title, titleFont, Brushes.Yellow, titleX, titleY);
            
            // Butonlar (Başlat / Çıkış)
            string startText = "BAŞLAT (Enter)";
            string exitText = "ÇIKIŞ";
            SizeF startSize = g.MeasureString(startText, menuFont);
            SizeF exitSize = g.MeasureString(exitText, menuFont);
            int btnW = (int)Math.Max(startSize.Width, exitSize.Width) + 40;
            int btnH = 40;
            int cx = this.ClientSize.Width / 2;
            btnMainStart = new Rectangle(cx - btnW / 2, 300, btnW, btnH);
            btnMainExit = new Rectangle(cx - btnW / 2, 350 + 10, btnW, btnH);

            DrawButton(g, btnMainStart, startText);
            DrawButton(g, btnMainExit, exitText);
            
            // Yüksek skor
            if (highScore > 0)
            {
                string highScoreText = $"En Yüksek Skor: {highScore}";
                SizeF highScoreSize = g.MeasureString(highScoreText, menuFont);
                float highScoreX = (this.ClientSize.Width - highScoreSize.Width) / 2;
                g.DrawString(highScoreText, menuFont, Brushes.Gold, highScoreX, 450);
            }

            // İlk 5 skor
            if (topScores.Count > 0)
            {
                string header = "İlk 5 Skor:";
                g.DrawString(header, menuFont, Brushes.White, 40, 520);
                for (int i = 0; i < topScores.Count && i < 5; i++)
                {
                    g.DrawString($"{i + 1}. {topScores[i]}", menuFont, Brushes.White, 60, 520 + 30 * (i + 1));
                }
            }
        }

        private void DrawDifficultySelection(Graphics g)
        {
            string title = "ZORLUK SEÇİN";
            SizeF titleSize = g.MeasureString(title, titleFont);
            float titleX = (this.ClientSize.Width - titleSize.Width) / 2;
            g.DrawString(title, titleFont, Brushes.Yellow, titleX, 100);

            string[] items = {
                "1 - Kolay",
                "2 - Normal",
                "3 - Zor",
                "ESC - Ana menü"
            };
            for (int i = 0; i < items.Length; i++)
            {
                SizeF itemSize = g.MeasureString(items[i], menuFont);
                float itemX = (this.ClientSize.Width - itemSize.Width) / 2;
                float itemY = 220 + i * 40;
                g.DrawString(items[i], menuFont, Brushes.White, itemX, itemY);
            }
        }

        private void DrawCostumeSelection(Graphics g)
        {
            // Başlık
            string title = "KUŞ KOSTÜMÜ SEÇİN";
            SizeF titleSize = g.MeasureString(title, titleFont);
            float titleX = (this.ClientSize.Width - titleSize.Width) / 2;
            g.DrawString(title, titleFont, Brushes.Yellow, titleX, 100);
            
            // Kostüm seçenekleri
            string[] costumes = {
                "1 - Klasik (Sarı)",
                "2 - Kırmızı",
                "3 - Mavi",
                "4 - Gökkuşağı",
                "5 - Altın",
                "6 - Yeşil",
                "7 - Mor"
            };
            
            Color[] costumeColors = {
                Color.Yellow, Color.Red, Color.Blue, 
                Color.Purple, Color.Gold,
                Color.Green, Color.MediumPurple
            };
            
            for (int i = 0; i < costumes.Length; i++)
            {
                SizeF itemSize = g.MeasureString(costumes[i], menuFont);
                float itemX = (this.ClientSize.Width - itemSize.Width) / 2;
                float itemY = 200 + i * 40;
                
                // Kostüm önizlemesi
                float previewX = itemX - 50;
                float previewY = itemY - 5;
                using (SolidBrush costumeBrush = new SolidBrush(costumeColors[i]))
                {
                    g.FillEllipse(costumeBrush, previewX, previewY, 30, 30);
                }
                
                g.DrawString(costumes[i], menuFont, Brushes.White, itemX, itemY);
            }
            
            // Geri dön
            string backText = "ESC - Ana Menüye Dön";
            SizeF backSize = g.MeasureString(backText, menuFont);
            float backX = (this.ClientSize.Width - backSize.Width) / 2;
            g.DrawString(backText, menuFont, Brushes.Gray, backX, 450);
        }

        private void DrawGame(Graphics g)
        {
            // Oyun nesnelerini çiz
            pipeManager.Draw(g);
            bird.Draw(g);
            
            // Skor
            Font scoreFont = new Font("Arial", 28, FontStyle.Bold);
            g.DrawString($"Skor: {score}", scoreFont, Brushes.White, 20, 20);
            
            // Duraklatma bilgisi
            Font pauseFont = new Font("Arial", 14, FontStyle.Regular);
            g.DrawString("ESC - Duraklat  |  R: Yeniden  |  M: Menü", pauseFont, Brushes.White, 20, this.ClientSize.Height - 40);
        }

        private void DrawGameOver(Graphics g)
        {
            // Oyun nesnelerini çiz (donmuş)
            pipeManager.Draw(g);
            bird.Draw(g);
            
            // Karanlık overlay
            using (SolidBrush overlayBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
            {
                g.FillRectangle(overlayBrush, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }
            
            // Oyun bitti mesajı
            string gameOverText = "OYUN BİTTİ!";
            SizeF gameOverSize = g.MeasureString(gameOverText, titleFont);
            float gameOverX = (this.ClientSize.Width - gameOverSize.Width) / 2;
            float gameOverY = 200;
            
            g.DrawString(gameOverText, titleFont, Brushes.Red, gameOverX, gameOverY);
            
            // Skor bilgileri
            string scoreText = $"Skor: {score}";
            string highScoreText = $"En Yüksek: {highScore}";
            
            SizeF scoreSize = g.MeasureString(scoreText, menuFont);
            SizeF highScoreSize = g.MeasureString(highScoreText, menuFont);
            
            float scoreX = (this.ClientSize.Width - scoreSize.Width) / 2;
            float highScoreX = (this.ClientSize.Width - highScoreSize.Width) / 2;
            
            g.DrawString(scoreText, menuFont, Brushes.White, scoreX, gameOverY + 80);
            g.DrawString(highScoreText, menuFont, Brushes.Gold, highScoreX, gameOverY + 120);
            
            // Butonlar
            int btnW = 220;
            int btnH = 40;
            int cx = this.ClientSize.Width / 2;
            btnGameOverRestart = new Rectangle(cx - btnW / 2, (int)gameOverY + 200, btnW, btnH);
            btnGameOverMenu = new Rectangle(cx - btnW / 2, (int)gameOverY + 250, btnW, btnH);
            DrawButton(g, btnGameOverRestart, "Tekrar Oyna (Enter) ");
            DrawButton(g, btnGameOverMenu, "Ana Menü (ESC)");

            // İlk 5 skor
            if (topScores.Count > 0)
            {
                string header = "İlk 5 Skor:";
                g.DrawString(header, menuFont, Brushes.White, 40, (int)gameOverY + 300);
                for (int i = 0; i < topScores.Count && i < 5; i++)
                {
                    g.DrawString($"{i + 1}. {topScores[i]}", menuFont, Brushes.White, 60, (int)gameOverY + 330 + 30 * i);
                }
            }
        }

        private void DrawPaused(Graphics g)
        {
            // Oyun nesnelerini çiz (donmuş)
            pipeManager.Draw(g);
            bird.Draw(g);
            
            // Karanlık overlay
            using (SolidBrush overlayBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
            {
                g.FillRectangle(overlayBrush, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }
            
            // Duraklatma mesajı
            string pauseText = "DURAKLATILDI";
            SizeF pauseSize = g.MeasureString(pauseText, titleFont);
            float pauseX = (this.ClientSize.Width - pauseSize.Width) / 2;
            float pauseY = this.ClientSize.Height / 2 - 50;
            
            g.DrawString(pauseText, titleFont, Brushes.Yellow, pauseX, pauseY);
            
            // Butonlar: Devam / Yeniden başlat / Menü
            int btnW = 260;
            int btnH = 40;
            int cx = this.ClientSize.Width / 2;
            btnPauseResume = new Rectangle(cx - btnW / 2, (int)pauseY + 120, btnW, btnH);
            btnPauseRestart = new Rectangle(cx - btnW / 2, (int)pauseY + 170, btnW, btnH);
            btnPauseMenu = new Rectangle(cx - btnW / 2, (int)pauseY + 220, btnW, btnH);
            DrawButton(g, btnPauseResume, "Devam (ESC)");
            DrawButton(g, btnPauseRestart, "Yeniden Başlat (R)");
            DrawButton(g, btnPauseMenu, "Ana Menü (M)");
        }

        private void DrawButton(Graphics g, Rectangle rect, string text)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(48, 0, 0, 0)))
            {
                g.FillRectangle(b, rect);
            }
            g.DrawRectangle(Pens.White, rect);
            SizeF size = g.MeasureString(text, menuFont);
            float tx = rect.X + (rect.Width - size.Width) / 2f;
            float ty = rect.Y + (rect.Height - size.Height) / 2f;
            g.DrawString(text, menuFont, Brushes.White, tx, ty);
        }

        private void FlappyBirdGame_Resize(object sender, EventArgs e)
        {
            pipeManager?.SetWindowSize(this.ClientSize.Width, this.ClientSize.Height);
        }

        private void LoadHighScore()
        {
            try
            {
                if (File.Exists(scoreFilePath))
                {
                    string content = File.ReadAllText(scoreFilePath).Trim();
                    if (int.TryParse(content, out int parsed))
                    {
                        highScore = parsed;
                    }
                }
            }
            catch { /* sessizce geç */ }
        }

        private void SaveHighScore()
        {
            try
            {
                File.WriteAllText(scoreFilePath, highScore.ToString());
            }
            catch { /* sessizce geç */ }
        }

        private void LoadTopScores()
        {
            try
            {
                if (File.Exists(scoresListFilePath))
                {
                    var lines = File.ReadAllLines(scoresListFilePath)
                        .Select(x => x.Trim())
                        .Where(x => int.TryParse(x, out _))
                        .Select(int.Parse)
                        .ToList();
                    topScores = lines.OrderByDescending(x => x).Take(5).ToList();
                }
            }
            catch { }
        }

        private void SaveTopScores()
        {
            try
            {
                File.WriteAllLines(scoresListFilePath, topScores.Select(x => x.ToString()));
            }
            catch { }
        }

        private void UpdateTopScores(int newScore)
        {
            topScores.Add(newScore);
            topScores = topScores.OrderByDescending(x => x).Take(5).ToList();
            SaveTopScores();
        }
    }
}

