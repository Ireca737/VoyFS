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
        private bool phantomAvailable;
        private double phantomDistanceNm;
        private double phantomBearingDeg;
        private string phantomCallsign = string.Empty;
        private int phantomRelativeAltitudeHundreds;
        private int ownshipHeadingDeg;
        private string ownshipCallsign = string.Empty;

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

        internal void SetPhantom(bool available, double distanceNm, double bearingDeg, string callsign, int relativeAltitudeHundreds)
        {
            phantomAvailable = available;
            phantomDistanceNm = distanceNm;
            phantomBearingDeg = bearingDeg;
            phantomCallsign = callsign ?? string.Empty;
            phantomRelativeAltitudeHundreds = relativeAltitudeHundreds;
            Invalidate();
        }

        internal void SetOwnshipHeading(int headingDeg)
        {
            ownshipHeadingDeg = ((headingDeg % 360) + 360) % 360;
            Invalidate();
        }

        internal void SetOwnshipCallsign(string callsign)
        {
            ownshipCallsign = callsign ?? string.Empty;
            Invalidate();
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

            // Heading is the primary top reference: large white digits in a green frame.
            string headingText = ownshipHeadingDeg.ToString("D3") + "°";
            using (var headingFont = new Font("Segoe UI Semibold", 14F, FontStyle.Bold))
            using (var headingBrush = new SolidBrush(Color.White))
            using (var headingPen = new Pen(FlyLabTheme.Success, 3F))
            {
                var size = g.MeasureString(headingText, headingFont);
                var box = new RectangleF(
                    (ClientSize.Width - Math.Max(78F, size.Width + 20F)) / 2F,
                    5F,
                    Math.Max(78F, size.Width + 20F),
                    34F);
                g.DrawRectangle(headingPen, box.X, box.Y, box.Width, box.Height);
                g.DrawString(headingText, headingFont, headingBrush,
                    box.X + (box.Width - size.Width) / 2F,
                    box.Y + (box.Height - size.Height) / 2F - 1F);
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
            // Leave a little more vertical breathing room for the live heading labels.
            int radius = Math.Max(30, Math.Min(ClientSize.Width, ClientSize.Height) / 2 - 58);

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

            // Live ownship orientation references. The radar remains North-Up for now;
            // these values give the pilot heading, reciprocal and left/right beams.
            int heading = ownshipHeadingDeg;
            int reciprocal = (heading + 180) % 360;
            int rightBeam = (heading + 90) % 360;
            int leftBeam = (heading + 270) % 360;
            using (var font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(FlyLabTheme.TextMuted))
            {
                DrawCentered(g, reciprocal.ToString("D3") + "°", font, brush, cx, cy + radius + 6);

                string leftText = leftBeam.ToString("D3") + "°";
                string rightText = rightBeam.ToString("D3") + "°";
                var leftSize = g.MeasureString(leftText, font);
                var rightSize = g.MeasureString(rightText, font);
                g.DrawString(leftText, font, brush, cx - radius - leftSize.Width - 8, cy - leftSize.Height / 2F);
                g.DrawString(rightText, font, brush, cx + radius + 8, cy - rightSize.Height / 2F);
            }

            if (!string.IsNullOrWhiteSpace(ownshipCallsign))
            {
                using var callsignFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
                using var callsignBrush = new SolidBrush(FlyLabTheme.Accent);
                string text = "CALLSIGN:  " + ownshipCallsign;
                var size = g.MeasureString(text, callsignFont);
                g.DrawString(text, callsignFont, callsignBrush,
                    ClientSize.Width - size.Width - 14F,
                    ClientSize.Height - size.Height - 8F);
            }

            // L3.1 diagnostic phantom: a fixed geographic point captured relative to the
            // ownship at startup. Full scale is temporarily 10 NM for this geometry test.
            if (phantomAvailable)
            {
                const double rangeNm = 10.0;
                double clampedDistance = Math.Min(rangeNm, Math.Max(0.0, phantomDistanceNm));
                double angle = phantomBearingDeg * Math.PI / 180.0;
                float targetRadius = (float)(radius * clampedDistance / rangeNm);
                float tx = cx + (float)(Math.Sin(angle) * targetRadius);
                float ty = cy - (float)(Math.Cos(angle) * targetRadius);

                using (var targetPen = new Pen(FlyLabTheme.Warning, 2F))
                    g.DrawRectangle(targetPen, tx - 5, ty - 5, 10, 10);

                using (var targetFont = new Font("Segoe UI Semibold", 8F, FontStyle.Bold))
                using (var targetBrush = new SolidBrush(FlyLabTheme.Warning))
                {
                    string altitude = phantomRelativeAltitudeHundreds > 0
                        ? "+" + phantomRelativeAltitudeHundreds.ToString("D2")
                        : phantomRelativeAltitudeHundreds < 0
                            ? phantomRelativeAltitudeHundreds.ToString("D2")
                            : "00";
                    g.DrawString(phantomCallsign, targetFont, targetBrush, tx + 8, ty - 12);
                    g.DrawString(altitude, targetFont, targetBrush, tx + 8, ty + 1);
                }
            }
        }

        private static void DrawCentered(Graphics g, string text, Font font, Brush brush, float x, float y)
        {
            var size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, x - size.Width / 2F, y);
        }
    }
}
#endif
