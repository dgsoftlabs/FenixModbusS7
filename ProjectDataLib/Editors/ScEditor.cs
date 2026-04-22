using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;

namespace ProjectDataLib
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CusEventPropertyAttribute : Attribute
    {
    }

    public class ScEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using var editor = new ScriptEditorForm
            {
                ScriptText = value?.ToString() ?? " "
            };

            return editor.ShowDialog() == DialogResult.OK ? editor.ScriptText : value;
        }

        private sealed class ScriptEditorForm : Form
        {
            private readonly TextBox _editor = new TextBox();

            public ScriptEditorForm()
            {
                Text = "Script Editor";
                Width = 900;
                Height = 600;
                StartPosition = FormStartPosition.CenterParent;

                _editor.Multiline = true;
                _editor.ScrollBars = ScrollBars.Both;
                _editor.AcceptsTab = true;
                _editor.AcceptsReturn = true;
                _editor.WordWrap = false;
                _editor.Dock = DockStyle.Fill;

                var panel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 42
                };

                var okButton = new Button
                {
                    Text = "OK",
                    Width = 90,
                    Height = 28,
                    Left = 10,
                    Top = 7,
                    DialogResult = DialogResult.OK
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    Width = 90,
                    Height = 28,
                    Left = 110,
                    Top = 7,
                    DialogResult = DialogResult.Cancel
                };

                panel.Controls.Add(okButton);
                panel.Controls.Add(cancelButton);

                Controls.Add(_editor);
                Controls.Add(panel);

                AcceptButton = okButton;
                CancelButton = cancelButton;
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string ScriptText
            {
                get => _editor.Text;
                set => _editor.Text = value;
            }
        }
    }
}
