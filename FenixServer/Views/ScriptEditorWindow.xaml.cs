using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FenixServer.Views
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CusEventPropertyAttribute : Attribute
    {
    }

    internal sealed class ScriptEditorWindow : Window
    {
        private readonly System.Windows.Controls.TextBox _editor;

        public ScriptEditorWindow()
        {
            Title = "Script Editor";
            Width = 900;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ShowInTaskbar = false;

            _editor = new System.Windows.Controls.TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 90,
                Height = 28,
                Margin = new Thickness(10, 7, 0, 7),
                IsDefault = true
            };
            okButton.Click += (_, _) => { DialogResult = true; Close(); };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 90,
                Height = 28,
                Margin = new Thickness(5, 7, 10, 7),
                IsCancel = true
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            var dockPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            dockPanel.Children.Add(buttonPanel);
            dockPanel.Children.Add(_editor);

            Content = dockPanel;
        }

        public string ScriptText
        {
            get => _editor.Text;
            set => _editor.Text = value;
        }

        /// <summary>
        /// Shows the script editor as a modal dialog and returns the edited text.
        /// </summary>
        public static string ShowEditor(string initialText, Window owner = null)
        {
            var window = new ScriptEditorWindow { ScriptText = initialText ?? string.Empty };
            if (owner != null)
                window.Owner = owner;

            var result = window.ShowDialog();
            return result == true ? window.ScriptText : initialText;
        }
    }
}
