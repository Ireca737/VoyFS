#if !CONSOLE
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Central FlyLabFS visual tokens. Keep form-specific code free from hard-coded colours.
    /// </summary>
    internal static class FlyLabTheme
    {
        public static readonly Color Background = Color.FromArgb(15, 22, 38);   // #0F1626
        public static readonly Color Panel = Color.FromArgb(26, 35, 56);        // #1A2338
        public static readonly Color Surface = Color.FromArgb(36, 46, 72);      // #242E48
        public static readonly Color Primary = Color.FromArgb(59, 130, 246);    // #3B82F6
        public static readonly Color Accent = Color.FromArgb(6, 182, 212);      // #06B6D4
        public static readonly Color Text = Color.FromArgb(241, 245, 249);
        public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);
        public static readonly Color Success = Color.FromArgb(34, 197, 94);
        public static readonly Color Warning = Color.FromArgb(245, 158, 11);
        public static readonly Color Error = Color.FromArgb(239, 68, 68);
        public static readonly Color Offline = Color.FromArgb(148, 163, 184);
        public static readonly Color GridLine = Color.FromArgb(51, 65, 85);
        public static readonly Color Selection = Color.FromArgb(30, 64, 175);

        public static void ApplyListChrome(Panel header, Label title, Label brand, Panel footer, Label status)
        {
            header.BackColor = Background;
            title.ForeColor = Text;
            brand.ForeColor = Accent;

            if (footer != null)
                footer.BackColor = Background;
            if (status != null)
                status.ForeColor = TextMuted;
        }

        public static void ApplySecondaryGrid(DataGridView grid)
        {
            if (grid == null)
                return;

            grid.BackgroundColor = Panel;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = GridLine;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            grid.ColumnHeadersHeight = 30;
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Surface;
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        }

        public static void ApplyDetailFields(params Label[] labels)
        {
            foreach (Label label in labels)
            {
                if (label == null)
                    continue;

                label.BackColor = Surface;
                label.ForeColor = Text;
                label.BorderStyle = BorderStyle.FixedSingle;
                label.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            }
        }

        public static void ApplyCaptions(params Label[] labels)
        {
            foreach (Label label in labels)
            {
                if (label == null)
                    continue;

                label.BackColor = Background;
                label.ForeColor = TextMuted;
                label.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            }
        }

        public static void ApplyListForm(Form form, DataGridView grid, Button refreshButton, ContextMenuStrip contextMenu)
        {
            form.BackColor = Background;
            form.ForeColor = Text;
            form.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            grid.BackgroundColor = Panel;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = GridLine;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextMuted;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Text;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            grid.ColumnHeadersHeight = 34;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.DefaultCellStyle.BackColor = Panel;
            grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Selection;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
            grid.RowTemplate.Height = 32;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            refreshButton.BackColor = Primary;
            refreshButton.ForeColor = Color.White;
            refreshButton.FlatStyle = FlatStyle.Flat;
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.UseVisualStyleBackColor = false;
            refreshButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);

            if (contextMenu != null)
            {
                contextMenu.BackColor = Surface;
                contextMenu.ForeColor = Text;
                contextMenu.RenderMode = ToolStripRenderMode.System;
            }
        }
    }
}
#endif
