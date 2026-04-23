using ProjectDataLib;
using System;
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
        /// Copies a directory and its contents to a new location.
        /// </summary>
        /// <param name="sourceDirName">The path of the source directory.</param>
        /// <param name="destDirName">The path of the destination directory.</param>
        /// <param name="copySubDirs">A flag indicating whether to copy subdirectories.</param>
        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            io.DirectoryInfo dir = new io.DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new io.DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            io.DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!io.Directory.Exists(destDirName))
            {
                io.Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            io.FileInfo[] files = dir.GetFiles();
            foreach (io.FileInfo file in files)
            {
                string temppath = io.Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (io.DirectoryInfo subdir in dirs)
                {
                    string temppath = io.Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
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

                        string[] files1 = io.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + projectContainer.TemplateCatalog);
                        foreach (string f in files1)
                        {
                            string nName = io.Path.GetFileName(f);
                            string TarDir = io.Path.GetDirectoryName(currentProject.path) + projectContainer.ScriptsCatalog;

                            if (!io.Directory.Exists(TarDir))
                                io.Directory.CreateDirectory(TarDir);

                            io.File.Copy(f, TarDir + "\\" + nName, true);

                            ScriptFile file = new ScriptFile(TarDir + "\\" + nName);

                            projectContainer.AddScriptFile(currentProject.objId, file);
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