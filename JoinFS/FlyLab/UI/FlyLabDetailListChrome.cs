#if !CONSOLE
using System;
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Runtime chrome for list forms that keep functional detail controls below the grid.
    /// Adds FlyLab branding without replacing or re-parenting upstream controls.
    /// </summary>
    internal sealed class FlyLabDetailListChrome
    {
        private readonly Form form;
        private readonly DataGridView grid;
        private readonly Panel header;
        private readonly int originalGridBottomGap;

        private FlyLabDetailListChrome(Form form, DataGridView grid, string title)
        {
            this.form = form;
            this.grid = grid;
            originalGridBottomGap = form.ClientSize.Height - grid.Bottom;

            header = new Panel
            {
                Height = 58,
                Dock = DockStyle.Top,
                Padding = new Padding(16, 10, 16, 8)
            };

            var titleLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Left,
                Text = title,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold)
            };

            var brand = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Right,
                Text = "FLYLAB FS",
                Padding = new Padding(0, 6, 0, 0),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };

            header.Controls.Add(brand);
            header.Controls.Add(titleLabel);
            form.Controls.Add(header);
            header.BringToFront();

            FlyLabTheme.ApplyListChrome(header, titleLabel, brand, null, null);
            LayoutGrid();
            form.Resize += (_, __) => LayoutGrid();
        }

        public static FlyLabDetailListChrome Attach(Form form, DataGridView grid, string title)
        {
            return new FlyLabDetailListChrome(form, grid, title);
        }

        private void LayoutGrid()
        {
            int top = header.Bottom + 10;
            int bottom = Math.Max(top + 80, form.ClientSize.Height - originalGridBottomGap);
            grid.Top = top;
            grid.Height = Math.Max(80, bottom - top);
        }
    }
}
#endif
