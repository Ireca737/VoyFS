#if !CONSOLE
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Runtime FlyLab chrome for upstream JoinFS list forms.
    /// It deliberately avoids changes to the upstream Designer.
    /// </summary>
    internal sealed class FlyLabListChrome
    {
        private readonly Form form;
        private readonly DataGridView grid;
        private readonly Button refreshButton;
        private readonly Panel header;
        private readonly Panel footer;
        private readonly Label status;
        private readonly int originalGridBottomGap;
        private readonly int originalGridRightGap;

        private FlyLabListChrome(Form form, DataGridView grid, Button refreshButton, string title)
        {
            this.form = form;
            this.grid = grid;
            this.refreshButton = refreshButton;

            originalGridBottomGap = form.ClientSize.Height - grid.Bottom;
            originalGridRightGap = form.ClientSize.Width - grid.Right;

            header = new Panel { Height = 90, Dock = DockStyle.Top, Padding = new Padding(16, 8, 12, 8) };
            var titleLabel = new Label {
                AutoSize = true, Dock = DockStyle.Left, Text = title,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold)
            };
            var brandBlock = new Panel
            {
                Width = 178,
                Dock = DockStyle.Right,
                BackColor = FlyLabTheme.Background
            };
            var brandLogo = new PictureBox
            {
                Width = 64,
                Height = 64,
                Top = 5,
                Left = 4,
                Image = FlyLabIcons.BrandBitmap,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = FlyLabTheme.Background
            };
            var brand = new Label
            {
                Width = 102,
                Height = 64,
                Top = 5,
                Left = 72,
                Text = "FLYLAB FS",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
            };
            brandBlock.Controls.Add(brandLogo);
            brandBlock.Controls.Add(brand);

            header.Controls.Add(brandBlock);
            header.Controls.Add(titleLabel);

            footer = new Panel { Height = 46, Dock = DockStyle.Bottom, Padding = new Padding(16, 7, 12, 7) };
            status = new Label {
                AutoSize = true, Dock = DockStyle.Left, Text = "0 ATC available",
                Padding = new Padding(0, 7, 0, 0), Font = new Font("Segoe UI", 9F)
            };

            // Re-parent the existing upstream Refresh button instead of replacing it.
            form.Controls.Remove(refreshButton);
            refreshButton.Dock = DockStyle.Right;
            refreshButton.Width = 104;
            footer.Controls.Add(status);
            footer.Controls.Add(refreshButton);

            form.Controls.Add(grid);
            form.Controls.Add(footer);
            form.Controls.Add(header);
            header.BringToFront();
            footer.BringToFront();

            // Preserve upstream controls and logic; only move the visible list into the FlyLab frame.
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LayoutGrid();

            FlyLabTheme.ApplyListChrome(header, titleLabel, brand, footer, status);
            form.Resize += (_, __) => LayoutGrid();
        }

        public static FlyLabListChrome Attach(Form form, DataGridView grid, Button refreshButton, string title)
        {
            return new FlyLabListChrome(form, grid, refreshButton, title);
        }

        public void SetCount(int count, string noun)
        {
            status.Text = count + " " + noun + " available";
        }

        private void LayoutGrid()
        {
            int left = Math.Max(12, grid.Left);
            int top = header.Bottom + 10;
            int right = Math.Max(12, originalGridRightGap);
            int bottom = footer.Top - 10;

            grid.Location = new Point(left, top);
            grid.Size = new Size(
                Math.Max(100, form.ClientSize.Width - left - right),
                Math.Max(80, bottom - top));
        }
    }
}
#endif
