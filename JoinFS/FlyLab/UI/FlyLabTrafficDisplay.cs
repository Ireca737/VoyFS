#if !CONSOLE
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// FlyLab tactical traffic display surface. Presentation only: network/traffic state
    /// is supplied by the FlyLab shell; JoinFS remains the source of operational data.
    /// </summary>
    internal sealed class FlyLabTrafficDisplay : Control
    {
        private bool networkAvailable;

        internal bool NetworkAvailable
        {
            get => networkAvailable;
            set
            {
                if (networkAvailable == value) return;
                networkAvailable = value;
                Invalidate();
            }
        }

        internal FlyLabTrafficDisplay()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(8, 15, 26);
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var titleFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(FlyLabTheme.TextMuted))
            {
                const string title = "TRAFFIC";
                var size = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, titleBrush, (ClientSize.Width - size.Width) / 2F, 8F);
            }

            if (!networkAvailable)
            {
                using var font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold);
                using var brush = new SolidBrush(FlyLabTheme.Error);
                const string text = "NO SIGNAL";
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, brush,
                    (ClientSize.Width - size.Width) / 2F,
                    (ClientSize.Height - size.Height) / 2F);
                return;
            }

            int cx = ClientSize.Width / 2;
            int cy = ClientSize.Height / 2;
            int radius = Math.Max(30, Math.Min(ClientSize.Width, ClientSize.Height) / 2 - 42);

            using (var pen = new Pen(Color.FromArgb(80, FlyLabTheme.Accent), 1F))
            {
                for (int i = 1; i <= 4; i++)
                {
                    int r = radius * i / 4;
                    g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
                }
                g.DrawLine(pen, cx - radius, cy, cx + radius, cy);
                g.DrawLine(pen, cx, cy - radius, cx, cy + radius);
            }

            using (var pen = new Pen(FlyLabTheme.Success, 2F))
            {
                Point[] ownship =
                {
                    new Point(cx, cy - 12),
                    new Point(cx - 9, cy + 10),
                    new Point(cx, cy + 5),
                    new Point(cx + 9, cy + 10),
                    new Point(cx, cy - 12)
                };
                g.DrawLines(pen, ownship);
            }

            using (var font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(FlyLabTheme.TextMuted))
                g.DrawString("N", font, brush, cx - 5, cy - radius + 6);
        }
    }
}
#endif
