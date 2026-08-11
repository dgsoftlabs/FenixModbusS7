using ProjectDataLib;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows;
using io = System.IO;
using wf = System.Windows.Forms;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for AddProject.xaml
    /// </summary>
    public partial class AddProject : Window
    {
        private ProjectContainer projectContainer;
        private Project currentProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddProject"/> class.
        /// </summary>
        /// <param name="prCon">The project container.</param>
        public AddProject(ProjectContainer prCon)
        {
            try
            {
                InitializeComponent();

                projectContainer = prCon;
                currentProject = new Project(projectContainer, "Project", Environment.UserName, "Company", "");
                DataContext = currentProject;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the click event of the Save button.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        [SupportedOSPlatform("windows")]
        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Dialog zapisu
                wf.SaveFileDialog sfd = new wf.SaveFileDialog();
                sfd.Filter = "Fenix files (*.pse)|*.pse|All files (*.*)|*.*";
                sfd.DefaultExt = "pse";
                sfd.AddExtension = true;
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (projectContainer.saveProject(currentProject, sfd.FileName))
                    {
                        currentProject.path = sfd.FileName;
                        currentProject.Db.Pr = currentProject;
                        currentProject.Db.PrCon = projectContainer;
                        currentProject.Db.OnDeserializedXML();
                        projectContainer.addProject(currentProject);

                        string projectDirectoryRoot = io.Path.GetDirectoryName(currentProject.path);
                        string projectHttpDirectory = projectDirectoryRoot + projectContainer.HttpCatalog;
                        if (!io.Directory.Exists(projectHttpDirectory))
                            io.Directory.CreateDirectory(projectHttpDirectory);

                        string[] files1 = io.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + projectContainer.TemplateCatalog, "*.cs");
                        foreach (string f in files1)
                        {
                            string nName = io.Path.GetFileName(f);
                            string TarDir = io.Path.GetDirectoryName(currentProject.path) + projectContainer.ScriptsCatalog;

                            if (!io.Directory.Exists(TarDir))
                                io.Directory.CreateDirectory(TarDir);

                            io.File.Copy(f, TarDir + "\\" + nName, true);

                            ScriptFile file = new ScriptFile(TarDir + "\\" + nName);

                            projectContainer.AddScriptFile(currentProject.objId, file);

                            //AttachTimers
                            var firstTimer = currentProject.ScriptEng.Timers.FirstOrDefault();
                            foreach (var scrFile in currentProject.ScriptFileList)
                            {
                                if (firstTimer is not null && string.IsNullOrEmpty(scrFile.TimerName))
                                    scrFile.TimerName = firstTimer.Name;
                            }
                        }

                        if (chAddWebTemplate.IsChecked == true)
                        {
                            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            string sourceHttpDirectory = baseDirectory + projectContainer.HttpCatalog;
                            string sourceIndexPath = io.Path.Combine(sourceHttpDirectory, "index.html");

                            if (io.File.Exists(sourceIndexPath))
                            {
                                string targetIndexPath = io.Path.Combine(projectHttpDirectory, "index.html");
                                io.File.Copy(sourceIndexPath, targetIndexPath, true);

                                var webChildren = ((ITreeViewModel)currentProject.WebServer1).Children;
                                if (webChildren != null)
                                {
                                    var existing = webChildren.OfType<CusFile>().Any(x => string.Equals(x.FullName, targetIndexPath, StringComparison.OrdinalIgnoreCase));
                                    if (!existing)
                                        webChildren.Add(new CusFile(new io.FileInfo(targetIndexPath)));
                                }
                            }
                        }

                        Close();
                    }
                }
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex)); ;
            }
        }

        /// <summary>
        /// Handles the click event of the Close button.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_Close_Click(object sender, RoutedEventArgs e)
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