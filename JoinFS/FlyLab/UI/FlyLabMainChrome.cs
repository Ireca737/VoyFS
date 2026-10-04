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
        private readonly FlyLabTrafficDisplay trafficDisplay;
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
        private readonly Label com1Caption;
        private readonly Label com2Caption;
        private readonly Label xpdrCaption;
        private readonly Label callsignCaption;
        private readonly Main main;
        private readonly uint vuidCom1;
        private readonly uint vuidCom2;
        private readonly uint vuidSquawk;
        private bool avionicsWasAvailable;
        private DateTime avionicsPowerOnAt;
        private bool phantomCaptured;
        private double phantomLongitude;
        private double phantomLatitude;

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

            avionicsStrip = BuildAvionicsStrip(out com1Caption, out com1Value, out com2Caption, out com2Value, out xpdrCaption, out xpdrValue, out callsignCaption, out callsignValue);
            var connection = BuildConnectionDeck(form, out simulatorIndicator, out networkIndicator, out globalIndicator);
            trafficDisplay = new FlyLabTrafficDisplay();

            instrument.Controls.Add(trafficDisplay);
            instrument.Controls.Add(connection);
            instrument.Controls.Add(avionicsStrip);
            // WinForms z-order matters for Dock=Fill: keep the avionics strip above the
            // traffic surface so its value row cannot be painted underneath it.
            avionicsStrip.BringToFront();

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

        private static Panel BuildAvionicsStrip(out Label com1Caption, out Label com1, out Label com2Caption, out Label com2, out Label xpdrCaption, out Label xpdr, out Label callsignCaption, out Label callsign)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.FromArgb(8, 15, 26), Padding = new Padding(4, 2, 4, 0) };
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Color.FromArgb(8, 15, 26) };
            for (int i = 0; i < 4; i++) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            table.Controls.Add(Readout("COM 1:", true, out com1Caption, out com1), 0, 0);
            table.Controls.Add(Readout("COM 2:", true, out com2Caption, out com2), 1, 0);
            table.Controls.Add(Readout("XPDR:", true, out xpdrCaption, out xpdr), 2, 0);
            table.Controls.Add(Readout("CALLSIGN:", false, out callsignCaption, out callsign), 3, 0);
            panel.Controls.Add(table);
            return panel;
        }

        private static Control Readout(string caption, bool dynamicValue, out Label captionLabel, out Label value)
        {
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(8, 15, 26),
                Padding = new Padding(0, 9, 0, 0)
            };
            captionLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 4, 0),
                Text = caption,
                ForeColor = FlyLabTheme.Accent,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            value = new Label
            {
                AutoSize = true,
                Margin = new Padding(0),
                Text = string.Empty,
                ForeColor = dynamicValue ? FlyLabTheme.Success : FlyLabTheme.Accent,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            p.Controls.Add(captionLabel);
            p.Controls.Add(value);
            return p;
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

            // Presentation-only power-up sequence. JoinFS data is read immediately;
            // only the first rendering is staggered to suggest independent avionics systems.
            if (avionicsAvailable && !avionicsWasAvailable)
                avionicsPowerOnAt = DateTime.UtcNow;

            if (avionicsAvailable)
            {
                double powerOnMs = (DateTime.UtcNow - avionicsPowerOnAt).TotalMilliseconds;
                SetReadoutVisible(callsignCaption, callsignValue, powerOnMs >= 0);
                SetReadoutVisible(com1Caption, com1Value, powerOnMs >= 800);
                SetReadoutVisible(com2Caption, com2Value, powerOnMs >= 2400);
                SetReadoutVisible(xpdrCaption, xpdrValue, powerOnMs >= 1200);

                string com1 = ownship.variableSet.GetFrequency(vuidCom1).ToString("F3");
                string com2 = ownship.variableSet.GetFrequency(vuidCom2).ToString("F3");
                string xpdr = ownship.variableSet.GetInteger(vuidSquawk).ToString("D4");
                string callsign = ownship.flightPlan.callsign ?? string.Empty;

                // Labels remain FlyLab cyan; live radio/transponder values are green.
                // Callsign is an identity value and therefore remains cyan.
                com1Value.Text = com1;
                com2Value.Text = com2;
                xpdrValue.Text = xpdr;
                callsignValue.Text = callsign;
            }
            else
            {
                SetReadoutVisible(callsignCaption, callsignValue, false);
                SetReadoutVisible(com1Caption, com1Value, false);
                SetReadoutVisible(com2Caption, com2Value, false);
                SetReadoutVisible(xpdrCaption, xpdrValue, false);
                com1Value.Text = string.Empty;
                com2Value.Text = string.Empty;
                xpdrValue.Text = string.Empty;
                callsignValue.Text = string.Empty;
            }

            avionicsWasAvailable = avionicsAvailable;

            // L3.1 geometry test: capture one fixed geographic phantom 3 NM at 045°
            // from the first valid ownship position. Moving the aircraft afterwards must
            // change the phantom's relative distance/bearing without moving the phantom.
            var ownPosition = ownship?.Position;
            if (!phantomCaptured && ownPosition != null)
            {
                const double distanceNm = 3.0;
                const double bearingRad = Math.PI / 4.0;
                const double earthRadiusNm = 3440.065;
                // Sim.Pos.geo follows JoinFS' native convention: x=longitude and
                // z=latitude, both already in radians (the Vector geodesic helpers use radians).
                double lon1 = ownPosition.geo.x;
                double lat1 = ownPosition.geo.z;
                double angularDistance = distanceNm / earthRadiusNm;

                double lat2 = Math.Asin(
                    Math.Sin(lat1) * Math.Cos(angularDistance) +
                    Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearingRad));
                double lon2 = lon1 + Math.Atan2(
                    Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(lat1),
                    Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

                phantomLongitude = lon2;
                phantomLatitude = lat2;
                phantomCaptured = true;
            }

            if (phantomCaptured && ownPosition != null)
            {
                double distanceMetres = Vector.GeodesicDistance(
                    ownPosition.geo.x, ownPosition.geo.z, phantomLongitude, phantomLatitude);
                double distanceNm = distanceMetres * 0.00053995680346;
                double bearingRad = Vector.GeodesicBearing(
                    ownPosition.geo.x, ownPosition.geo.z, phantomLongitude, phantomLatitude);
                double bearingDeg = bearingRad * 180.0 / Math.PI;
                trafficDisplay.SetPhantom(true, distanceNm, bearingDeg);
            }
            else
            {
                trafficDisplay.SetPhantom(false, 0.0, 0.0);
            }

            if (networkButton == null || trafficDisplay == null) return;

            bool online = networkButton.BackColor == JoinFS.Properties.Settings.Default.ColourActiveBackground;
            trafficDisplay.NetworkAvailable = online;
        }

        private static void SetReadoutVisible(Label caption, Label value, bool visible)
        {
            if (caption != null) caption.Visible = visible;
            if (value != null) value.Visible = visible;
        }

        private static void MirrorState(Button source, Button target)
        {
            if (source == null || target == null) return;
            target.BackColor = source.BackColor;
            target.ForeColor = source.ForeColor;
            target.Enabled = source.Enabled;
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
