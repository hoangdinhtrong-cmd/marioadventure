using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace MarioAdventure;

internal sealed class MainMenu : Form
{
    private readonly TextBox nameBox = new() { Text = "Player", Width = 220, Font = new Font("Segoe UI", 13), TextAlign = HorizontalAlignment.Center };
    private readonly Button sound = new();
    public MainMenu()
    {
        Text = "SUPER PLATFORM ADVENTURE — WinForms";
        ClientSize = new Size(930, 630);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(19, 29, 61);
        DoubleBuffered = true;
        Paint += (_, e) =>
        {
            var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var cloud = new SolidBrush(Color.FromArgb(55, 78, 131));
            for (int i = 0; i < 9; i++) g.FillEllipse(cloud, i * 135 - 40, 85 + i % 3 * 85, 195, 70);
            using var titleFont = new Font("Segoe UI", 30, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 13);
            using var gold = new SolidBrush(Color.FromArgb(255, 204, 89));
            using var pale = new SolidBrush(Color.FromArgb(221, 228, 249));
            // Center each line relative to the actual client width, not fixed X offsets.
            using var centered = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near
            };
            g.DrawString("SUPER PLATFORM", titleFont, gold,
                new RectangleF(22, 57, ClientSize.Width - 44, 65), centered);
            g.DrawString("ADVENTURE", titleFont, gold,
                new RectangleF(22, 119, ClientSize.Width - 44, 65), centered);
            g.DrawString("3 levels  •  enemies  •  coins  •  leaderboard", subFont, pale,
                new RectangleF(0, 193, ClientSize.Width, 35), centered);
            using var ground = new SolidBrush(Color.FromArgb(58, 103, 88));
            g.FillRectangle(ground, 0, 585, 930, 45);
        };
        var panel = new FlowLayoutPanel { Width = 350, Height = 380, Left = (ClientSize.Width - 350) / 2, Top = 233, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, AutoScroll = false };
        Controls.Add(panel);
        var label = new Label { Text = "TÊN NGƯỜI CHƠI", ForeColor = Color.White, Width = 330, Height = 27, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        panel.Controls.Add(label);
        nameBox.Margin = new Padding(58, 3, 0, 10);
        panel.Controls.Add(nameBox);
        AddButton(panel, "▶  BẮT ĐẦU CHƠI", () =>
        {
            string name = ScoreStore.CleanName(nameBox.Text);
            try { using var game = new GameForm(name); Hide(); game.ShowDialog(this); }
            catch (Exception ex) { MessageBox.Show(this, "Không mở được game: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { Show(); Audio.StopMusic(); sound.Text = Audio.Enabled ? "♪  ÂM THANH: BẬT" : "♪  ÂM THANH: TẮT"; }
        });
        AddButton(panel, "★  BẢNG THÀNH TÍCH", ShowScores);
        AddButton(panel, "?  HƯỚNG DẪN CHƠI", ShowHelp);
        sound.Text = "♪  ÂM THANH: BẬT";
        StyleButton(sound);
        sound.Click += (_, _) => { Audio.Toggle(false); sound.Text = Audio.Enabled ? "♪  ÂM THANH: BẬT" : "♪  ÂM THANH: TẮT"; };
        panel.Controls.Add(sound);
        AddButton(panel, "✕  THOÁT", Close);
    }
    private static void StyleButton(Button b)
    {
        b.Width = 330; b.Height = 45; b.Margin = new Padding(0, 3, 0, 5);
        b.BackColor = Color.FromArgb(47, 74, 135); b.ForeColor = Color.White;
        b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
        b.Font = new Font("Segoe UI", 11, FontStyle.Bold); b.Cursor = Cursors.Hand;
    }
    private static void AddButton(Control parent, string title, Action callback)
    {
        var b = new Button { Text = title }; StyleButton(b); b.Click += (_, _) => callback(); parent.Controls.Add(b);
    }
    private void ShowHelp() => MessageBox.Show(this,
        "SUPER PLATFORM ADVENTURE\n\n" +
        "A / ← : chạy trái        D / → : chạy phải\n" +
        "SPACE / W / ↑ : nhảy\n" +
        "P / ESC : tạm dừng hoặc tiếp tục\n" +
        "M : bật / tắt âm thanh\n" +
        "R : chơi lại màn hiện tại (khôi phục điểm/xu đầu màn, giữ số mạng còn lại)\n\n" +
        "Mục tiêu: thu thập xu, né gai, nhảy lên đầu quái để hạ chúng.\n" +
        "Đến cờ đích để qua màn; hoàn thành 3 màn để thắng.\n" +
        "Bạn có 3 mạng. Chạm quái hoặc gai, hoặc rơi xuống vực sẽ mất một mạng. Hết 3 mạng thì trò chơi kết thúc.\n\n" +
        "Bài tập môn Lập trình C# WinForms\n" +
        "Thành viên nhóm:\n1. Hoàng Đình Trọng\n2. Trần Anh Minh",
        "Hướng dẫn & thông tin nhóm", MessageBoxButtons.OK, MessageBoxIcon.Information);
    private void ShowScores()
    {
        try
        {
            var entries = ScoreStore.Read();
            var lines = entries.Select((s, i) => $"{i + 1,2}.  {s.Name,-20}  {s.Score,6} điểm  |  Màn {s.Level}");
            MessageBox.Show(this, entries.Count == 0 ? "Chưa có thành tích nào. Hãy chơi để lập kỷ lục!" : string.Join("\n", lines), "TOP 10 THÀNH TÍCH", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, $"Không đọc được bảng thành tích: {ex.Message}", "Lỗi file", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
}
