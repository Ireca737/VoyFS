#if !CONSOLE
using System.Drawing;
using System.Windows.Forms;

namespace JoinFS.FlyLab.UI
{
    /// <summary>
    /// Runtime-only FlyLabFS presentation for the upstream Settings form.
    /// The form keeps ownership of layout, behaviour and user-configurable semantic colours.
    /// </summary>
    internal static class FlyLabSettingsChrome
    {
        public static void Apply(Form form)
        {
            if (form == null)
                return;

            form.BackColor = FlyLabTheme.Background;
            form.ForeColor = FlyLabTheme.Text;
            form.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            ApplyControls(form.Controls);
        }

        private static void ApplyControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                switch (control)
                {
                    case GroupBox group:
                        group.BackColor = FlyLabTheme.Background;
                        group.ForeColor = FlyLabTheme.Text;
                        group.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
                        break;

                    case TextBox textBox:
                        // Airport deliberately keeps its upstream validation background
                        // (normal/green) because the colour has functional meaning.
                        if (textBox.Name != "Text_Airport")
                        {
                            textBox.BackColor = FlyLabTheme.Surface;
                            textBox.ForeColor = FlyLabTheme.Text;
                        }
                        textBox.BorderStyle = BorderStyle.FixedSingle;
                        break;

                    case ComboBox combo:
                        combo.BackColor = FlyLabTheme.Surface;
                        combo.ForeColor = FlyLabTheme.Text;
                        combo.FlatStyle = FlatStyle.Flat;
                        break;

                    case CheckBox check:
                        check.BackColor = Color.Transparent;
                        check.ForeColor = FlyLabTheme.Text;
                        check.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
                        break;

                    case Button button:
                        ApplyButton(button);
                        break;

                    case Label label:
                        if (!IsSemanticColourLabel(label))
                        {
                            label.BackColor = Color.Transparent;
                            label.ForeColor = FlyLabTheme.TextMuted;
                        }
                        break;
                }

                if (control.HasChildren)
                    ApplyControls(control.Controls);
            }
        }

        private static void ApplyButton(Button button)
        {
            bool primary = button.Name == "Button_OK";
            bool destructive = button.Name == "Button_Reset";

            button.BackColor = primary
                ? FlyLabTheme.Primary
                : destructive ? FlyLabTheme.Surface : FlyLabTheme.Surface;
            button.ForeColor = destructive ? FlyLabTheme.Warning : FlyLabTheme.Text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = primary ? FlyLabTheme.Primary : FlyLabTheme.GridLine;
            button.UseVisualStyleBackColor = false;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        }

        private static bool IsSemanticColourLabel(Label label)
        {
            return label.Name == "Label_Active"
                || label.Name == "Label_Waiting"
                || label.Name == "Label_Inactive"
                || label.Name == "Label_LabelColour";
        }
    }
}
#endif
