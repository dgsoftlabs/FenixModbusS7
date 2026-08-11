using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using io = System.IO;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for AddScriptFromTemplate.xaml
    /// </summary>
    public partial class AddScriptFromTemplate : Window
    {
        private ProjectContainer projectContainer { get; set; }
        private Project currentProject { get; set; }
        private Guid selectedId { get; set; }
        private ElementKind selectedElementKind { get; set; }

        private readonly List<string> templateFiles = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AddScriptFromTemplate"/> class.
        /// </summary>
        /// <param name="pc">The project container.</param>
        /// <param name="pr">The current project.</param>
        /// <param name="sel">The selected ID.</param>
        /// <param name="elKind">The selected element kind.</param>
        public AddScriptFromTemplate(ProjectContainer pc, Project pr, Guid sel, ElementKind elKind)
        {
            try
            {
                InitializeComponent();

                projectContainer = pc;
                currentProject = pr;
                selectedId = sel;
                selectedElementKind = elKind;

                LoadTemplates();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void LoadTemplates()
        {
            string templateDir = AppDomain.CurrentDomain.BaseDirectory + projectContainer.TemplateCatalog;
            if (io.Directory.Exists(templateDir))
            {
                templateFiles.AddRange(io.Directory.GetFiles(templateDir, "*.cs").OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
                foreach (string f in templateFiles)
                    LbTemplates.Items.Add(GetTemplateDisplayName(f));
            }

            if (LbTemplates.Items.Count > 0)
                LbTemplates.SelectedIndex = 0;
        }

        private static string GetTemplateDisplayName(string filePath)
        {
            // Strip a leading numeric prefix, e.g. "01_PID_Controller" -> "PID_Controller"
            return Regex.Replace(io.Path.GetFileNameWithoutExtension(filePath), @"^\d+_", string.Empty);
        }

        private void LbTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LbTemplates.SelectedIndex >= 0 && LbTemplates.SelectedIndex < templateFiles.Count)
                TbName.Text = GetTemplateDisplayName(templateFiles[LbTemplates.SelectedIndex]);
        }

        //OK
        /// <summary>
        /// Handles the click event of the OK button.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LbTemplates.SelectedIndex < 0 || LbTemplates.SelectedIndex >= templateFiles.Count)
                {
                    MessageBox.Show("Please select a template!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TbName.Text))
                {
                    MessageBox.Show("Please fill the script name!");
                    return;
                }

                string sourcePath = templateFiles[LbTemplates.SelectedIndex];
                string nName = TbName.Text.Trim();
                if (!nName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    nName += ".cs";

                string TarDir = io.Path.GetDirectoryName(currentProject.path) + projectContainer.ScriptsCatalog;
                if (!io.Directory.Exists(TarDir))
                    io.Directory.CreateDirectory(TarDir);

                string destPath = io.Path.Combine(TarDir, nName);
                io.File.Copy(sourcePath, destPath, true);

                projectContainer.AddScriptFile(currentProject.objId, new ScriptFile(destPath));

                Close();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Cancel
        /// <summary>
        /// Handles the click event of the Cancel button.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }
    }
}
