#if !CONSOLE
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Runtime FlyLabFS presentation for the upstream flight-plan dialog.
    /// Layout and flight-plan behaviour remain owned by JoinFS.
    /// </summary>
    internal static class FlyLabFlightPlanChrome
    {
        public static void Apply(Form form)
        {
            if (form == null)
                return;

            form.BackColor = FlyLabTheme.Background;
            form.ForeColor = FlyLabTheme.Text;
            form.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            foreach (Control control in form.Controls)
            {
                switch (control)
                {
                    case TextBox text:
                        text.BackColor = FlyLabTheme.Surface;
                        text.ForeColor = FlyLabTheme.Text;
                        text.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case ComboBox combo:
                        combo.BackColor = FlyLabTheme.Surface;
                        combo.ForeColor = FlyLabTheme.Text;
                        combo.FlatStyle = FlatStyle.Flat;
                        break;

                    case Label label:
                        label.BackColor = Color.Transparent;
                        label.ForeColor = label.Name == "Label_SimBriefStatus"
                            ? FlyLabTheme.Accent
                            : FlyLabTheme.TextMuted;
                        break;

                    case Button button:
                        ApplyButton(button);
                        break;
                }
            }
        }

        private static void ApplyButton(Button button)
        {
            bool primary = button.Name == "Button_OK";
            bool import = button.Name == "Button_ImportSimBrief";

            button.BackColor = primary
                ? FlyLabTheme.Primary
                : import ? FlyLabTheme.Accent : FlyLabTheme.Surface;
            button.ForeColor = import ? FlyLabTheme.Background : FlyLabTheme.Text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary || import ? 0 : 1;
            button.FlatAppearance.BorderColor = FlyLabTheme.GridLine;
            button.UseVisualStyleBackColor = false;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        }
    }
}
#endif
