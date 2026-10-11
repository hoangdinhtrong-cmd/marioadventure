using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace MarioAdventure;

internal sealed class GameForm : Form
{
    private sealed class Enemy
    {
        public float X, Y, LeftBound, RightBound, Speed;
        public bool Alive = true;
        public RectangleF Bounds => new(X, Y, 32, 30);
    }
    private sealed class Coin
    {
        public float X, Y;
        public bool Taken;
        public RectangleF Bounds => new(X - 10, Y - 10, 20, 20);
    }
    private readonly string player;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly HashSet<Keys> pressed = new();
    private readonly List<RectangleF> solids = new();
    private readonly List<RectangleF> spikes = new();
    private readonly List<Enemy> enemies = new();
    private readonly List<Coin> coins = new();
    private float px, py, vx, vy, camera;
    private bool grounded, paused, finished, saved, jumpHeld;
    private int level = 1, lives = 3, score, collected, seconds;
    private float worldWidth, finishX;
    private readonly System.Diagnostics.Stopwatch levelClock = new();
    private int levelStartScore, levelStartCollected;
    private const int ScreenW = 1000, ScreenH = 640;
    private const float Pw = 29, Ph = 43;
    private RectangleF PlayerRect => new(px, py, Pw, Ph);

    public GameForm(string name)
    {
        player = name;
        Text = "Super Platform Adventure — " + name;
        ClientSize = new Size(ScreenW, ScreenH);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = Color.SkyBlue;
        KeyDown += OnKeyDown;
        KeyUp += (_, e) => { pressed.Remove(e.KeyCode); if (e.KeyCode is Keys.Space or Keys.Up or Keys.W) jumpHeld = false; };
        Deactivate += (_, _) => { if (!finished) SetPaused(true); };
        FormClosed += (_, _) => { timer.Stop(); timer.Dispose(); Audio.StopMusic(); };
        timer.Tick += (_, _) =>
        {
            try { TickGame(); }
            catch (Exception ex)
            {
                SetPaused(true);
                MessageBox.Show(this, "Game đã tạm dừng do lỗi: " + ex.Message + "\nNhấn R để khởi động lại màn hoặc đóng cửa sổ.", "Lỗi game", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        BuildLevel(1, true);
        timer.Start();
        Audio.StartMusic();
    }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool firstPress = pressed.Add(e.KeyCode);
        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Space or Keys.A or Keys.D or Keys.W or Keys.P or Keys.R or Keys.M or Keys.Escape)
            e.SuppressKeyPress = true;
        if (firstPress && (e.KeyCode is Keys.P or Keys.Escape)) SetPaused(!paused);
        if (firstPress && e.KeyCode == Keys.M) { Audio.Toggle(!paused && !finished); Invalidate(); }
        if (firstPress && e.KeyCode == Keys.R && !finished)
        {
            score = levelStartScore; collected = levelStartCollected;
            BuildLevel(level, false); Audio.StartMusic(); Invalidate();
        }
    }
    private void SetPaused(bool value)
    {
        paused = value;
        if (paused) { levelClock.Stop(); pressed.Clear(); jumpHeld = false; Audio.StopMusic(); }
        else { levelClock.Start(); Audio.StartMusic(); }
        Invalidate();
    }
    private void BuildLevel(int target, bool resetSession)
    {
        if (resetSession) { lives = 3; score = 0; collected = 0; seconds = 0; saved = false; }
        levelStartScore = score; levelStartCollected = collected;
        level = target; px = 80; py = 410; vx = vy = 0; camera = 0; grounded = false; paused = false; finished = false; jumpHeld = false;
        pressed.Clear(); solids.Clear(); spikes.Clear(); enemies.Clear(); coins.Clear();
        // Hand-designed topography: actual ground elevation, voids, bridges and ruins differ by stage.
        // Runs are (start x, end x, floor top y); gaps between runs are real pits.
        (float from, float to, float top)[] terrain = level switch
        {
            1 => new (float, float, float)[] {
                (0,530,520),(610,1060,520),(1130,1520,490),(1605,2070,520),
                (2150,2570,475),(2655,3100,520) },
            2 => new (float, float, float)[] {
                (0,450,520),(545,925,485),(1015,1395,450),(1490,1820,510),
                (1915,2290,465),(2385,2760,425),(2855,3240,495),(3335,3700,520) },
            _ => new (float, float, float)[] {
                (0,425,520),(515,815,465),(910,1210,510),(1300,1600,440),
                (1690,1940,490),(2030,2280,420),(2375,2650,470),
                (2745,2995,425),(3090,3410,480),(3500,3900,520) }
        };
        foreach (var (from, to, top) in terrain)
            solids.Add(new RectangleF(from, top, to - from, ScreenH - top + 80));
        worldWidth = terrain[^1].to;
        finishX = worldWidth - 130;

        // Upper stepping stones; maximum rise from adjacent ground is ~80px.
        // Coins are placed above stable, attainable stepping stones.
        (float x, float y, float width)[] platforms = level switch
        {
            1 => new (float, float, float)[] {
                (240,445,135),(690,447,150),(1230,414,145),(1710,440,135),
                (2280,399,145),(2720,445,145) },
            2 => new (float, float, float)[] {
                (200,450,115),(610,414,145),(1050,377,140),(1545,439,120),
                (1980,393,135),(2480,350,145),(2950,426,130),(3420,445,130) },
            _ => new (float, float, float)[] {
                (190,444,120),(560,394,130),(975,437,120),(1360,365,135),
                (1755,420,120),(2080,344,130),(2450,394,125),(2810,350,120),
                (3170,408,130),(3570,450,130) }
        };
        foreach (var (x, y, width) in platforms)
        {
            solids.Add(new RectangleF(x, y, width, 23));
            for (int i = 0; i < 3; i++)
                coins.Add(new Coin { X = x + 24 + i * ((width - 48) / 2), Y = y - 30 });
        }
        // Bonus coins above ground and over the take-off edges; accessible without blind jumps.
        foreach (var (from, to, top) in terrain)
        {
            float x = (from + to) * .5f;
            coins.Add(new Coin { X = x, Y = top - 32 });
        }

        // Ground patrols inherit the height of the terrain they stand on.
        int terrainIndex = 0;
        foreach (var (from, to, top) in terrain)
        {
            // Keep spawn and flag areas quiet. Do not spawn on narrow sections or hazards.
            if (terrainIndex++ == 0 || to > finishX) continue;
            float ex = from + Math.Min(125, (to - from) / 2);
            enemies.Add(new Enemy { X = ex, Y = top - 30,
                LeftBound = from + 24, RightBound = to - 56,
                Speed = (terrainIndex % 2 == 0 ? 1 : -1) * (1.0f + level * 0.27f) });
        }
        if (level >= 2)
        {
            foreach (var (from, to, top) in terrain.Skip(1).Where((_, i) => i % 2 == 1))
            {
                float x = to - 68;
                if (x - from > 155 && !enemies.Any(en => Math.Abs(en.X - x) < 95))
                    spikes.Add(new RectangleF(x, top - 23, 44, 23));
            }
        }
        seconds = 0;
        levelClock.Restart();
    }
    private bool EnemyCanStep(float nextX, Enemy enemy)
    {
        // Both feet must remain on the SAME ground elevation: guards against cliffs and pits.
        float front = enemy.Speed >= 0 ? nextX + 34 : nextX - 2;
        float back = enemy.Speed >= 0 ? nextX + 5 : nextX + 29;
        bool Supported(float x) => solids.Any(block =>
            Math.Abs(block.Top - (enemy.Y + 30)) < 2 && block.Left <= x && block.Right >= x);
        return Supported(front) && Supported(back) &&
            !spikes.Any(s => s.Left <= front && s.Right >= front &&
                Math.Abs(s.Bottom - (enemy.Y + 30)) < 3);
    }

    private void TickGame()
    {
        if (paused || finished) return;
        seconds = (int)levelClock.Elapsed.TotalSeconds;
        bool left = pressed.Contains(Keys.Left) || pressed.Contains(Keys.A);
        bool right = pressed.Contains(Keys.Right) || pressed.Contains(Keys.D);
        vx = left == right ? 0 : left ? -4.7f : 4.7f;
        bool wantsJump = pressed.Contains(Keys.Space) || pressed.Contains(Keys.Up) || pressed.Contains(Keys.W);
        if (wantsJump && !jumpHeld && grounded)
        {
            vy = -13.8f; grounded = false; Audio.Effect(true);
        }
        jumpHeld = wantsJump;
        // Resolve the horizontal and vertical axes separately to prevent tunneling into platforms.
        px += vx;
        px = Math.Clamp(px, 0, worldWidth - Pw);
        foreach (var block in solids)
        {
            if (PlayerRect.IntersectsWith(block))
            {
                if (vx > 0) px = block.Left - Pw;
                else if (vx < 0) px = block.Right;
            }
        }
        float prevBottom = py + Ph;
        float prevTop = py;
        vy = Math.Min(14.5f, vy + 0.68f);
        py += vy;
        grounded = false;
        foreach (var block in solids)
        {
            if (!PlayerRect.IntersectsWith(block)) continue;
            if (vy >= 0 && prevBottom <= block.Top + 9)
            {
                py = block.Top - Ph; vy = 0; grounded = true;
            }
            else if (vy < 0 && prevTop >= block.Bottom - 9)
            {
                py = block.Bottom; vy = 0;
            }
        }
        foreach (var coin in coins)
        {
            if (coin.Taken || !PlayerRect.IntersectsWith(coin.Bounds)) continue;
            coin.Taken = true; score += 100; collected++; Audio.Effect(true);
        }
        foreach (var enemy in enemies.Where(en => en.Alive))
        {
            float nextX = enemy.X + enemy.Speed;
            if (nextX < enemy.LeftBound || nextX > enemy.RightBound ||
                !EnemyCanStep(nextX, enemy))
            {
                enemy.Speed = -enemy.Speed;
                nextX = enemy.X + enemy.Speed;
            }
            // If boxed in from both sides, remain still rather than crossing a pit.
            if (nextX >= enemy.LeftBound && nextX <= enemy.RightBound &&
                EnemyCanStep(nextX, enemy)) enemy.X = nextX;
            if (!PlayerRect.IntersectsWith(enemy.Bounds)) continue;
            if (vy > 0 && prevBottom <= enemy.Y + 14)
            {
                enemy.Alive = false; py = enemy.Y - Ph; vy = -9.5f; score += 200; Audio.Effect(true);
            }
            else { LoseLife(); return; }
        }
        if (spikes.Any(s => PlayerRect.IntersectsWith(s)) || py > ScreenH + 90)
        {
            LoseLife(); return;
        }
        if (px + Pw >= finishX)
        {
            score += 500 + Math.Max(0, 300 - seconds) * 2;
            if (level < 3)
            {
                timer.Stop(); Audio.Effect(true);
                MessageBox.Show(this, $"Hoàn thành màn {level}!\nĐiểm hiện tại: {score:N0}\nSẵn sàng đến màn {level + 1}.", "QUA MÀN", MessageBoxButtons.OK, MessageBoxIcon.Information);
                BuildLevel(level + 1, false);
                timer.Start(); Audio.StartMusic();
            }
            else EndGame(true);
        }
        camera = Math.Clamp(px - ScreenW * 0.35f, 0, worldWidth - ScreenW);
        Invalidate();
    }
    private void LoseLife()
    {
        Audio.Effect(false); lives--;
        if (lives <= 0) { EndGame(false); return; }
        px = 80; py = 410; vy = vx = 0; grounded = false;
        // Restart enemies at their patrol bounds, preserving collected coins and points.
        foreach (var en in enemies) if (en.Alive) en.X = (en.LeftBound + en.RightBound) / 2;
        pressed.Clear(); jumpHeld = false;
        Invalidate();
    }
    private void EndGame(bool won)
    {
        if (finished) return;
        finished = true; timer.Stop(); levelClock.Stop(); Audio.StopMusic();
        string saveInfo = "";
        if (!saved)
        {
            try { ScoreStore.Save(new ScoreEntry(player, score, level, DateTime.Now)); saved = true; saveInfo = "\nThành tích đã được lưu."; }
            catch (Exception ex) { saveInfo = "\nKhông lưu được điểm: " + ex.Message; }
        }
        MessageBox.Show(this, (won ? "CHIẾN THẮNG! Bạn đã hoàn thành 3 màn." : "GAME OVER! Bạn đã hết mạng.") +
            $"\nNgười chơi: {player}\nTổng điểm: {score:N0}\nSố xu: {collected}{saveInfo}",
            won ? "CHIẾN THẮNG" : "KẾT THÚC", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Close();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var sky = new LinearGradientBrush(ClientRectangle,
            level == 1 ? Color.FromArgb(72, 167, 235) : level == 2 ? Color.FromArgb(240, 162, 86) : Color.FromArgb(35, 39, 95),
            level == 1 ? Color.FromArgb(198, 234, 250) : level == 2 ? Color.FromArgb(250, 218, 157) : Color.FromArgb(129, 105, 178), LinearGradientMode.Vertical))
            g.FillRectangle(sky, ClientRectangle);
        DrawBackground(g);
        g.TranslateTransform(-camera, 0);
        DrawWorld(g);
        g.ResetTransform();
        DrawHud(g);
        if (paused) DrawPause(g);
    }
    private void DrawBackground(Graphics g)
    {
        using var mountain = new SolidBrush(level == 1 ? Color.FromArgb(72, 116, 167) :
            level == 2 ? Color.FromArgb(171, 102, 94) : Color.FromArgb(73, 61, 119));
        using var cloud = new SolidBrush(level == 3 ? Color.FromArgb(158, 145, 194) : Color.FromArgb(220, 245, 255));
        for (int i = 0; i < 9; i++)
        {
            float x = i * 225 - (camera * .18f % 225) - 70;
            g.FillPolygon(mountain, new[] { new PointF(x - 90, 520), new PointF(x + 75, 280 + i % 3 * 35), new PointF(x + 230, 520) });
            float cx = i * 240 - (camera * .09f % 240);
            g.FillEllipse(cloud, cx, 100 + i % 3 * 43, 85, 29);
            g.FillEllipse(cloud, cx + 19, 88 + i % 3 * 43, 55, 40);
        }
    }
    private void DrawWorld(Graphics g)
    {
        using var dirt = new SolidBrush(level == 1 ? Color.FromArgb(147, 86, 52) :
            level == 2 ? Color.FromArgb(172, 109, 66) : Color.FromArgb(76, 69, 104));
        using var grass = new SolidBrush(level == 1 ? Color.FromArgb(93, 186, 87) :
            level == 2 ? Color.FromArgb(243, 199, 92) : Color.FromArgb(121, 162, 205));
        using var brick = new Pen(Color.FromArgb(102, 59, 44), 2);
        foreach (var block in solids)
        {
            if (block.Right < camera - 30 || block.Left > camera + ScreenW + 30) continue;
            g.FillRectangle(dirt, block);
            g.FillRectangle(grass, block.X, block.Y, block.Width, 9);
            for (float x = block.Left + 23; x < block.Right; x += 42) g.DrawLine(brick, x, block.Top + 14, x, block.Bottom);
        }
        using var spikeBrush = new SolidBrush(Color.FromArgb(218, 226, 233));
        foreach (var s in spikes)
            for (int i = 0; i < 3; i++) g.FillPolygon(spikeBrush, new[] { new PointF(s.X + i * 16, s.Bottom), new PointF(s.X + i * 16 + 8, s.Top), new PointF(s.X + i * 16 + 16, s.Bottom) });
        using var gold = new SolidBrush(Color.FromArgb(255, 211, 59));
        using var goldOutline = new Pen(Color.FromArgb(202, 137, 14), 3);
        foreach (var c in coins.Where(c => !c.Taken))
        {
            g.FillEllipse(gold, c.X - 10, c.Y - 10, 20, 20);
            g.DrawEllipse(goldOutline, c.X - 10, c.Y - 10, 20, 20);
        }
        foreach (var en in enemies.Where(en => en.Alive))
        {
            using var body = new SolidBrush(Color.FromArgb(155, 67, 147));
            using var eye = new SolidBrush(Color.White);
            g.FillEllipse(body, en.X, en.Y, 32, 30);
            g.FillEllipse(eye, en.X + 7, en.Y + 7, 6, 10);
            g.FillEllipse(eye, en.X + 20, en.Y + 7, 6, 10);
        }
        using var pole = new Pen(Color.FromArgb(238, 241, 242), 6);
        using var flag = new SolidBrush(Color.FromArgb(255, 198, 48));
        g.DrawLine(pole, finishX, 520, finishX, 330);
        g.FillPolygon(flag, new[] { new PointF(finishX + 3, 333), new PointF(finishX + 76, 351), new PointF(finishX + 3, 375) });
        // Distinct original character design (not copyrighted Mario sprite assets).
        using var outfit = new SolidBrush(Color.FromArgb(35, 84, 187));
        using var face = new SolidBrush(Color.FromArgb(255, 206, 164));
        using var hat = new SolidBrush(Color.FromArgb(236, 84, 70));
        using var shoes = new SolidBrush(Color.FromArgb(70, 43, 49));
        g.FillRectangle(outfit, px + 3, py + 18, 23, 22);
        g.FillEllipse(face, px + 5, py + 5, 20, 21);
        g.FillRectangle(hat, px + 3, py, 23, 10);
        g.FillRectangle(hat, px, py + 9, 27, 5);
        g.FillRectangle(shoes, px, py + 38, 14, 5);
        g.FillRectangle(shoes, px + 16, py + 38, 14, 5);
        g.FillEllipse(Brushes.Black, px + (vx >= 0 ? 19 : 9), py + 13, 3, 4);
    }
    private void DrawHud(Graphics g)
    {
        using var hud = new SolidBrush(Color.FromArgb(191, 17, 27, 58));
        using var white = new SolidBrush(Color.White);
        using var f = new Font("Segoe UI", 12, FontStyle.Bold);
        using var small = new Font("Segoe UI", 10, FontStyle.Regular);
        g.FillRectangle(hud, 0, 0, ScreenW, 58);
        g.DrawString($"★ {player}     { (level == 1 ? "ĐỒNG CỎ" : level == 2 ? "SA MẠC" : "ĐÊM TRĂNG") } {level}/3     ♥ {lives}     XU {collected}     ĐIỂM {score:N0}", f, white, 18, 8);
        g.DrawString($"{seconds}s   •   M: {(Audio.Enabled ? "Sound ON" : "Sound OFF")}   •   P: Pause   •   R: Retry", small, white, 18, 34);
        float progress = Math.Clamp(px / finishX, 0, 1);
        using var track = new SolidBrush(Color.FromArgb(67, 78, 109));
        using var filled = new SolidBrush(Color.FromArgb(254, 202, 70));
        g.FillRectangle(track, 775, 31, 205, 9);
        g.FillRectangle(filled, 775, 31, 205 * progress, 9);
    }
    private void DrawPause(Graphics g)
    {
        using var overlay = new SolidBrush(Color.FromArgb(177, 7, 17, 39));
        using var white = new SolidBrush(Color.White);
        using var f = new Font("Segoe UI", 36, FontStyle.Bold);
        using var hint = new Font("Segoe UI", 15);
        g.FillRectangle(overlay, ClientRectangle);
        g.DrawString("TẠM DỪNG", f, white, 350, 230);
        g.DrawString("Nhấn P / ESC để tiếp tục", hint, white, 371, 315);
    }
}
