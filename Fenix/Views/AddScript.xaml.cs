using ProjectDataLib;
using System;
using System.Windows;
using io = System.IO;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for AddScript.xaml
    /// </summary>
    public partial class AddScript : Window
    {
        private ProjectContainer projectContainer { get; set; }
        private Project currentProject { get; set; }
        private Guid selectedId { get; set; }
        private ElementKind selectedElementKind { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddScript"/> class.
        /// </summary>
        /// <param name="pc">The project container.</param>
        /// <param name="pr">The current project.</param>
        /// <param name="sel">The selected ID.</param>
        /// <param name="elKind">The selected element kind.</param>
        public AddScript(ProjectContainer pc, Project pr, Guid sel, ElementKind elKind)
        {
            try
            {
                InitializeComponent();

                projectContainer = pc;
                currentProject = pr;
                selectedId = sel;
                selectedElementKind = elKind;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
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
                if (string.IsNullOrEmpty(TbNewFile.Text))
                {
                    MessageBox.Show("Please fill File(s) name(s)!");
                    return;
                }

                foreach (string s in TbNewFile.Text.Split(';'))
                {
                    string nName = io.Path.GetFileName(s);
                    string TarDir = io.Path.GetDirectoryName(currentProject.path) + projectContainer.ScriptsCatalog;

                    if (!io.Directory.Exists(TarDir))
                        io.Directory.CreateDirectory(TarDir);

                    io.File.Copy(System.AppDomain.CurrentDomain.BaseDirectory + "\\" + projectContainer.TemplateCatalog + "\\" + "Script.cs", TarDir + "\\" + nName + ".cs", true);

                    projectContainer.AddScriptFile(currentProject.objId, new ScriptFile(TarDir + "\\" + nName + ".cs"));
                }

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