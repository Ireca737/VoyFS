#if !CONSOLE
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// FlyLabFS presentation shell for MainForm.
    /// L1 deliberately reuses the existing JoinFS controls and event handlers.
    /// Future avionics/traffic providers plug into this shell without moving logic into MainForm.
    /// </summary>
    internal sealed class FlyLabMainChrome
    {
        private readonly Form form;
        private readonly Panel trafficDisplay;
        private readonly Label trafficState;
        private readonly Button networkButton;
        private readonly Button simulatorButton;
        private readonly Button globalButton;
        private readonly Button networkIndicator;
        private readonly Button simulatorIndicator;
        private readonly Button globalIndicator;
        private readonly Panel avionicsStrip;
        private readonly Label com1Value;
        private readonly Label com2Value;
        private readonly Label xpdrValue;
        private readonly Label callsignValue;
        private readonly Main main;
        private readonly uint vuidCom1;
        private readonly uint vuidCom2;
        private readonly uint vuidSquawk;

        private FlyLabMainChrome(Form form, Main main)
        {
            this.form = form;
            this.main = main;
            vuidCom1 = VariableMgr.CreateVuid("com active frequency:1");
            vuidCom2 = VariableMgr.CreateVuid("com active frequency:2");
            vuidSquawk = VariableMgr.CreateVuid("transponder code:1");
            form.SuspendLayout();

            form.BackColor = FlyLabTheme.Background;
            form.ForeColor = FlyLabTheme.Text;
            form.Font = new Font("Segoe UI", 9F);
            form.MinimumSize = new Size(820, 760);
            form.Size = new Size(900, 820);

            // The upstream menu/status remain functional owners, but the FlyLab Main replaces their presentation.
            var upstreamMenu = Find<MenuStrip>(form, "Main_Menu");
            var upstreamStatus = Find<StatusStrip>(form, "StatusStrip_Main");

            var host = new Panel { Dock = DockStyle.Fill, BackColor = FlyLabTheme.Background, Padding = new Padding(12) };
            form.Controls.Add(host);
            host.BringToFront();
            if (upstreamMenu != null) upstreamMenu.Visible = false;
            if (upstreamStatus != null) upstreamStatus.Visible = false;

            var header = BuildHeader();
            var sidebar = BuildSidebar(form);
            var instrument = new Panel { Dock = DockStyle.Fill, BackColor = FlyLabTheme.Panel, Padding = new Padding(10) };

            host.Controls.Add(instrument);
            host.Controls.Add(sidebar);
            host.Controls.Add(header);

            avionicsStrip = BuildAvionicsStrip(out com1Value, out com2Value, out xpdrValue, out callsignValue);
            var connection = BuildConnectionDeck(form, out simulatorIndicator, out networkIndicator, out globalIndicator);
            trafficDisplay = BuildTrafficDisplay(out trafficState);
            trafficDisplay.Paint += (_, e) => PaintRadar(e.Graphics, trafficDisplay.ClientRectangle);

            instrument.Controls.Add(trafficDisplay);
            instrument.Controls.Add(connection);
            instrument.Controls.Add(avionicsStrip);

            networkButton = Find<Button>(form, "Button_Network");
            simulatorButton = Find<Button>(form, "Button_Simulator");
            globalButton = Find<Button>(form, "Button_Global");
            var timer = new System.Windows.Forms.Timer { Interval = 250 };
            timer.Tick += (_, __) => RefreshOperationalState();
            timer.Start();

            RefreshOperationalState();
            form.ResumeLayout(true);
        }

        public static FlyLabMainChrome Attach(Form form, Main main)
        {
            return form == null || main == null ? null : new FlyLabMainChrome(form, main);
        }

        private static Panel BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = FlyLabTheme.Background };
            var logo = new PictureBox { Dock = DockStyle.Left, Width = 62, Image = FlyLabIcons.BrandBitmap, SizeMode = PictureBoxSizeMode.Zoom };
            var title = new Label { Dock = DockStyle.Left, Width = 240, Text = "FLYLAB FS", ForeColor = FlyLabTheme.Accent, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold) };
            var subtitle = new Label { Dock = DockStyle.Fill, Text = "FLIGHT SIMULATION NETWORK", ForeColor = FlyLabTheme.TextMuted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI Semibold", 9F) };
            panel.Controls.Add(subtitle);
            panel.Controls.Add(title);
            panel.Controls.Add(logo);
            return panel;
        }

        private static Panel BuildSidebar(Form form)
        {
            var panel = new Panel { Dock = DockStyle.Left, Width = 185, BackColor = FlyLabTheme.Background, Padding = new Padding(0, 8, 12, 0), AutoScroll = true };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = FlyLabTheme.Background };
            panel.Controls.Add(flow);

            AddSection(flow, "FUNZIONI");
            AddProxy(flow, form, "PILOTI", "Menu_View_Session");
            AddProxy(flow, form, "AEREI", "Menu_View_Aircraft");
            AddProxy(flow, form, "ATC", "Menu_View_Atc");
            AddProxy(flow, form, "HUB", "Menu_View_Hubs");
            AddProxy(flow, form, "OGGETTI", "Menu_View_Objects");
            AddProxy(flow, form, "MAPPA", "Tool_Map");

            AddSection(flow, "SERVIZI");
            AddPlaceholder(flow, "COMMS");
            AddPlaceholder(flow, "ACARS");
            AddProxy(flow, form, "PIANO DI VOLO", "Button_FlightPlan");
            AddProxy(flow, form, "SIMBRIEF", "Button_SimBrief");

            AddSection(flow, "SISTEMA");
            AddProxy(flow, form, "SETTINGS", "Menu_File_Settings");
            AddProxy(flow, form, "MONITOR", "Menu_View_Monitor");
            AddPlaceholder(flow, "TOOLS");

            return panel;
        }

        private static Panel BuildAvionicsStrip(out Label com1, out Label com2, out Label xpdr, out Label callsign)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(8, 15, 26), Padding = new Padding(4, 2, 4, 0) };
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.FromArgb(8, 15, 26) };
            for (int i = 0; i < 4; i++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            table.Controls.Add(Readout("COM 1", out com1), 0, 0);
            table.Controls.Add(Readout("COM 2", out com2), 1, 0);
            table.Controls.Add(Readout("XPDR", out xpdr), 2, 0);
            table.Controls.Add(Readout("CALLSIGN", out callsign), 3, 0);
            panel.Controls.Add(table);
            return panel;
        }

        private static Control Readout(string caption, out Label value)
        {
            // Use explicit rows rather than overlapping Dock=Fill/Dock=Top labels.
            // This keeps both the caption and the live JoinFS value visible at all sizes.
            var p = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Color.FromArgb(8, 15, 26),
                ColumnCount = 1,
                RowCount = 2
            };
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            p.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var captionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = caption,
                ForeColor = FlyLabTheme.Accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            value = new Label
            {
                Dock = DockStyle.Fill,
                Text = string.Empty,
                ForeColor = FlyLabTheme.Text,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Consolas", 15F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            p.Controls.Add(captionLabel, 0, 0);
            p.Controls.Add(value, 0, 1);
            return p;
        }

        private static Panel BuildTrafficDisplay(out Label state)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 15, 26), Margin = new Padding(0, 8, 0, 8) };
            panel.Controls.Add(new Label { Dock = DockStyle.Top, Height = 32, Text = "TRAFFIC", ForeColor = FlyLabTheme.TextMuted, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold) });
            state = new Label { Dock = DockStyle.Fill, Text = "NO SIGNAL", ForeColor = FlyLabTheme.Error, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold) };
            panel.Controls.Add(state);
            state.BringToFront();
            return panel;
        }

        private static Panel BuildConnectionDeck(Form form, out Button simulatorIndicator, out Button networkIndicator, out Button globalIndicator)
        {
            var panel = new Panel { Dock = DockStyle.Bottom, Height = 125, BackColor = Color.FromArgb(8, 15, 26), Padding = new Padding(10, 4, 10, 6) };
            var status = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = FlyLabTheme.Surface };
            simulatorIndicator = ProxyButton(form, "SIMULATORE", "Button_Simulator", 150);
            networkIndicator = ProxyButton(form, "RETE", "Button_Network", 150);
            globalIndicator = ProxyButton(form, "GLOBALE", "Button_Global", 150);
            status.Controls.Add(simulatorIndicator);
            status.Controls.Add(networkIndicator);
            status.Controls.Add(globalIndicator);

            var join = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = FlyLabTheme.Surface };
            join.Controls.Add(ProxyButton(form, "CREA", "Button_Create", 110));
            var combo = Find<ComboBox>(form, "Combo_Join");
            if (combo != null)
            {
                combo.Width = 300; combo.Height = 32; combo.Margin = new Padding(8); combo.Font = new Font("Segoe UI", 10F);
                join.Controls.Add(combo);
            }
            join.Controls.Add(ProxyButton(form, "COLLEGATI", "Button_Join", 130));

            var info = new Label { Dock = DockStyle.Fill, Text = "", ForeColor = FlyLabTheme.TextMuted, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI Semibold", 9F), BackColor = Color.FromArgb(8, 15, 26) };
            panel.Controls.Add(info);
            panel.Controls.Add(join);
            panel.Controls.Add(status);
            return panel;
        }

        private void RefreshOperationalState()
        {
            MirrorState(simulatorButton, simulatorIndicator);
            MirrorState(networkButton, networkIndicator);
            MirrorState(globalButton, globalIndicator);

            // JoinFS is the sole source of truth for ownship avionics. FlyLab only presents
            // the values while the local user aircraft and its variable set are available.
            var ownship = main.sim?.userAircraft;
            bool avionicsAvailable = ownship?.variableSet != null;
            if (avionicsStrip != null) avionicsStrip.Visible = avionicsAvailable;
            if (avionicsAvailable)
            {
                com1Value.Text = ownship.variableSet.GetFrequency(vuidCom1).ToString("F3");
                com2Value.Text = ownship.variableSet.GetFrequency(vuidCom2).ToString("F3");
                xpdrValue.Text = ownship.variableSet.GetInteger(vuidSquawk).ToString("D4");
                callsignValue.Text = ownship.flightPlan.callsign ?? string.Empty;
            }
            else
            {
                com1Value.Text = string.Empty;
                com2Value.Text = string.Empty;
                xpdrValue.Text = string.Empty;
                callsignValue.Text = string.Empty;
            }

            if (networkButton == null || trafficState == null) return;
            bool online = networkButton.BackColor == JoinFS.Properties.Settings.Default.ColourActiveBackground;
            trafficState.Text = online ? string.Empty : "NO SIGNAL";
            trafficState.ForeColor = FlyLabTheme.Error;
            trafficDisplay.Invalidate();
        }

        private static void MirrorState(Button source, Button target)
        {
            if (source == null || target == null) return;
            target.BackColor = source.BackColor;
            target.ForeColor = source.ForeColor;
            target.Enabled = source.Enabled;
        }


        private void PaintRadar(Graphics g, Rectangle bounds)
        {
            if (networkButton == null ||
                networkButton.BackColor != JoinFS.Properties.Settings.Default.ColourActiveBackground)
                return;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int cx = bounds.Width / 2;
            int cy = bounds.Height / 2;
            int radius = Math.Max(30, Math.Min(bounds.Width, bounds.Height) / 2 - 30);

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

        private static void AddSection(FlowLayoutPanel flow, string text)
        {
            flow.Controls.Add(new Label { Width = 152, Height = 32, Margin = new Padding(4, 12, 4, 2), Text = text, ForeColor = FlyLabTheme.Accent, TextAlign = ContentAlignment.BottomLeft, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) });
        }

        private static void AddProxy(FlowLayoutPanel flow, Form form, string text, string sourceName)
        {
            flow.Controls.Add(ProxyButton(form, text, sourceName, 152));
        }

        private static void AddPlaceholder(FlowLayoutPanel flow, string text)
        {
            var b = ButtonStyle(text, 152);
            b.Enabled = false;
            b.Text += "  —";
            flow.Controls.Add(b);
        }

        private static Button ProxyButton(Form form, string text, string sourceName, int width)
        {
            var b = ButtonStyle(text, width);
            var source = Find<object>(form, sourceName);
            if (source != null)
            {
                if (source is Control control)
                {
                    b.Enabled = control.Enabled;
                    if (control is Button button)
                        b.Click += (_, __) => button.PerformClick();
                }
                else if (source is ToolStripItem item)
                {
                    b.Enabled = item.Enabled;
                    b.Click += (_, __) => item.PerformClick();
                }
            }
            else b.Enabled = false;
            return b;
        }

        private static Button ButtonStyle(string text, int width)
        {
            var b = new Button { Width = width, Height = 36, Margin = new Padding(4), Text = text, BackColor = FlyLabTheme.Panel, ForeColor = FlyLabTheme.Text, FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false, Font = new Font("Segoe UI Semibold", 9F) };
            b.FlatAppearance.BorderColor = FlyLabTheme.GridLine;
            return b;
        }

        private static T Find<T>(Control root, string name) where T : class
        {
            if (root == null) return null;
            if (root.Name == name && root is T hit) return hit;
            foreach (Control child in root.Controls)
            {
                var found = Find<T>(child, name);
                if (found != null) return found;
            }
            if (root is MenuStrip menu)
                foreach (ToolStripItem item in menu.Items)
                {
                    var found = FindItem<T>(item, name);
                    if (found != null) return found;
                }
            if (root is StatusStrip status)
                foreach (ToolStripItem item in status.Items)
                {
                    var found = FindItem<T>(item, name);
                    if (found != null) return found;
                }
            return null;
        }

        private static T FindItem<T>(ToolStripItem item, string name) where T : class
        {
            if (item.Name == name && item is T hit) return hit;
            if (item is ToolStripDropDownItem drop)
                foreach (ToolStripItem child in drop.DropDownItems)
                {
                    var found = FindItem<T>(child, name);
                    if (found != null) return found;
                }
            return null;
        }
    }
}
#endif
