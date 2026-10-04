#if !CONSOLE
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Lightweight FlyLabFS startup presentation. It owns no JoinFS logic and closes itself
    /// automatically; initialization continues normally behind the splash.
    /// </summary>
    internal sealed class FlyLabSplashForm : Form
    {
        private readonly DateTime shownAt = DateTime.UtcNow;
        private readonly Timer timer;

        private FlyLabSplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = FlyLabTheme.Background;
            ClientSize = new Size(560, 320);
            Opacity = 0.0;

            var logo = new PictureBox
            {
                Image = FlyLabIcons.BrandBitmap,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(230, 34),
                Size = new Size(100, 100),
                BackColor = Color.Transparent
            };
            var title = new Label
            {
                Text = "FLYLAB FS",
                ForeColor = FlyLabTheme.Accent,
                Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 138),
                Size = new Size(480, 48)
            };
            var subtitle = new Label
            {
                Text = "FLIGHT SIMULATION NETWORK",
                ForeColor = FlyLabTheme.TextMuted,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 184),
                Size = new Size(480, 26)
            };
            var credits = new Label
            {
                Text = "Powered by JoinFS\r\nFlyLab Project · Virtual Over Italy",
                ForeColor = FlyLabTheme.TextMuted,
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 246),
                Size = new Size(480, 42)
            };

            Controls.Add(logo);
            Controls.Add(title);
            Controls.Add(subtitle);
            Controls.Add(credits);

            timer = new Timer { Interval = 25 };
            timer.Tick += Animate;
            Shown += (_, __) => timer.Start();
        }

        private void Animate(object sender, EventArgs e)
        {
            double ms = (DateTime.UtcNow - shownAt).TotalMilliseconds;
            if (ms < 250)
                Opacity = Math.Min(1.0, ms / 250.0);
            else if (ms < 1500)
                Opacity = 1.0;
            else if (ms < 1800)
                Opacity = Math.Max(0.0, 1.0 - ((ms - 1500.0) / 300.0));
            else
            {
                timer.Stop();
                Close();
            }
        }

        internal static void ShowStartup()
        {
            using var splash = new FlyLabSplashForm();
            splash.ShowDialog();
        }
    }
}
#endif
