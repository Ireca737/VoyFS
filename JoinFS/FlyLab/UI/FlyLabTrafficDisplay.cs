#if !CONSOLE
using System;
using System.Collections.Generic;
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
        private int ownshipHeadingDeg;
        private string ownshipCallsign = string.Empty;
        private string acarsStage = "NO ACARS";
        private double ownshipLatitude;
        private double ownshipLongitude;
        private double ownshipAltitude;
        private IReadOnlyList<FlyLabTrafficTarget> trafficTargets = Array.Empty<FlyLabTrafficTarget>();
        private static readonly int[] TrafficRangesNm = { 2, 5, 10, 20, 40 };
        private int trafficRangeIndex = TrafficRangesNm.Length - 1;
        private readonly Label rangeLabel;

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

        internal void SetAcarsStage(string stage)
        {
            string next = string.IsNullOrWhiteSpace(stage) ? "NO ACARS" : stage.Trim();
            if (string.Equals(acarsStage, next, StringComparison.Ordinal)) return;
            acarsStage = next;
            Invalidate();
        }

        internal void SetTraffic(double latitude, double longitude, double altitude, IReadOnlyList<FlyLabTrafficTarget> targets)
        {
            ownshipLatitude = latitude;
            ownshipLongitude = longitude;
            ownshipAltitude = altitude;
            trafficTargets = targets ?? Array.Empty<FlyLabTrafficTarget>();
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

            var rangeMinus = BuildRangeButton("−");
            var rangePlus = BuildRangeButton("+");
            rangeLabel = new Label
            {
                AutoSize = false,
                Size = new Size(82, 24),
                Location = new Point(38, 8),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = FlyLabTheme.Accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold)
            };

            rangeMinus.Location = new Point(8, 8);
            rangePlus.Location = new Point(124, 8);
            rangeMinus.Click += (_, __) => ChangeRange(-1);
            rangePlus.Click += (_, __) => ChangeRange(+1);

            Controls.Add(rangeMinus);
            Controls.Add(rangeLabel);
            Controls.Add(rangePlus);
            UpdateRangeLabel();
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
            // Reserve a real header band inside the Traffic Display for the heading box.
            // The rose begins below it, so the heading is no longer competing with the
            // separate COM/XPDR avionics strip.
            const int trafficHeaderHeight = 44;
            int availableHeight = Math.Max(80, ClientSize.Height - trafficHeaderHeight);
            int cy = trafficHeaderHeight + availableHeight / 2;
            int radius = Math.Max(30, Math.Min(ClientSize.Width, availableHeight) / 2 - 34);

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

            DrawTrafficTargets(g, cx, cy, radius);

            if (!string.IsNullOrWhiteSpace(ownshipCallsign))
            {
                using var callsignFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
                using var callsignBrush = new SolidBrush(FlyLabTheme.Accent);
                string text = "CALLSIGN:  " + ownshipCallsign;
                var size = g.MeasureString(text, callsignFont);
                // Phase 1 ACARS layout: keep the callsign on the lower-left edge
                // of the radar, directly above the SIMULATORE status indicator.
                g.DrawString(text, callsignFont, callsignBrush,
                    14F,
                    ClientSize.Height - size.Height - 8F);
            }

            // Phase 3A: show the live VaBase flight stage on the lower-right edge.
            using (var stageFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold))
            using (var captionBrush = new SolidBrush(FlyLabTheme.Accent))
            using (var valueBrush = new SolidBrush(
                string.Equals(acarsStage, "NO ACARS", StringComparison.OrdinalIgnoreCase)
                    ? JoinFS.Properties.Settings.Default.ColourInactiveBackground
                    : FlyLabTheme.Success))
            {
                const string caption = "ACARS STAGE:";
                string value = acarsStage;
                string combined = caption + " " + value;
                var totalSize = g.MeasureString(combined, stageFont);
                float x = ClientSize.Width - totalSize.Width - 14F;
                float y = ClientSize.Height - totalSize.Height - 8F;

                g.DrawString(caption, stageFont, captionBrush, x, y);
                var captionSize = g.MeasureString(caption + " ", stageFont);
                g.DrawString(value, stageFont, valueBrush, x + captionSize.Width, y);
            }

        }

        private double CurrentTrafficRangeNm => TrafficRangesNm[trafficRangeIndex];

        private static Button BuildRangeButton(string text)
        {
            var button = new Button
            {
                Size = new Size(26, 24),
                Text = text,
                BackColor = FlyLabTheme.Panel,
                ForeColor = FlyLabTheme.Accent,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = FlyLabTheme.GridLine;
            return button;
        }

        private void ChangeRange(int delta)
        {
            int next = Math.Max(0, Math.Min(TrafficRangesNm.Length - 1, trafficRangeIndex + delta));
            if (next == trafficRangeIndex) return;
            trafficRangeIndex = next;
            UpdateRangeLabel();
            Invalidate();
        }

        private void UpdateRangeLabel()
        {
            if (rangeLabel != null)
                rangeLabel.Text = "RNG " + TrafficRangesNm[trafficRangeIndex] + " NM";
        }

        private void DrawTrafficTargets(Graphics g, int cx, int cy, int radius)
        {
            if (trafficTargets == null || trafficTargets.Count == 0) return;

            using var font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);

            foreach (var target in trafficTargets)
            {
                double distanceNm = DistanceNm(ownshipLatitude, ownshipLongitude, target.Latitude, target.Longitude);
                if (distanceNm <= 0.001 || distanceNm > CurrentTrafficRangeNm) continue;

                double deltaAlt = target.Altitude - ownshipAltitude;
                bool proximate = distanceNm <= 6.0 && Math.Abs(deltaAlt) <= 1200.0;

                double bearing = BearingDeg(ownshipLatitude, ownshipLongitude, target.Latitude, target.Longitude);
                double relativeDeg = Normalize180(bearing - ownshipHeadingDeg);
                double angle = relativeDeg * Math.PI / 180.0;
                double r = radius * distanceNm / CurrentTrafficRangeNm;
                float x = (float)(cx + Math.Sin(angle) * r);
                float y = (float)(cy - Math.Cos(angle) * r);

                // Approved baseline TCAS symbology:
                // Other Traffic = hollow diamond; Proximate Traffic = filled diamond.
                // Both remain non-alert cyan/white. TA/RA colors are intentionally reserved
                // until a real threat-assessment layer exists.
                Color trafficColor = FlyLabTheme.Accent;
                using var pen = new Pen(trafficColor, 2F);
                using var brush = new SolidBrush(trafficColor);

                const float s = 6F;
                PointF[] diamond =
                {
                    new PointF(x, y - s), new PointF(x + s, y),
                    new PointF(x, y + s), new PointF(x - s, y)
                };
                if (proximate)
                    g.FillPolygon(brush, diamond);
                else
                    g.DrawPolygon(pen, diamond);

                string altitudeText = deltaAlt > 0
                    ? "+" + Math.Abs((int)Math.Round(deltaAlt / 100.0)).ToString("D2")
                    : deltaAlt < 0
                        ? "-" + Math.Abs((int)Math.Round(deltaAlt / 100.0)).ToString("D2")
                        : "00";
                string label = target.PilotId + "  " + altitudeText;
                g.DrawString(label, font, brush, x + 9F, y - 8F);
            }
        }

        private static double DistanceNm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthNm = 3440.065;
            double p1 = lat1 * Math.PI / 180.0;
            double p2 = lat2 * Math.PI / 180.0;
            double dp = (lat2 - lat1) * Math.PI / 180.0;
            double dl = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dp / 2) * Math.Sin(dp / 2) +
                       Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            return earthNm * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        }

        private static double BearingDeg(double lat1, double lon1, double lat2, double lon2)
        {
            double p1 = lat1 * Math.PI / 180.0;
            double p2 = lat2 * Math.PI / 180.0;
            double dl = (lon2 - lon1) * Math.PI / 180.0;
            double y = Math.Sin(dl) * Math.Cos(p2);
            double x = Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dl);
            return (Math.Atan2(y, x) * 180.0 / Math.PI + 360.0) % 360.0;
        }

        private static double Normalize180(double degrees)
        {
            degrees = (degrees + 180.0) % 360.0;
            if (degrees < 0) degrees += 360.0;
            return degrees - 180.0;
        }

        private static void DrawCentered(Graphics g, string text, Font font, Brush brush, float x, float y)
        {
            var size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, x - size.Width / 2F, y);
        }
    }
}
#endif
