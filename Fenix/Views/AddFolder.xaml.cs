using ProjectDataLib;
using System;
using System.Windows;
using io = System.IO;
using wf = System.Windows.Forms;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for AddFolder.xaml
    /// </summary>
    public partial class AddFolder : Window
    {
        private ProjectContainer projectContainer { get; set; }
        private Project currentProject { get; set; }
        private String path { get; set; }
        private ElementKind elementKind { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddFolder"/> class.
        /// </summary>
        /// <param name="pc">The project container.</param>
        /// <param name="pr">The current project.</param>
        /// <param name="path">The path.</param>
        /// <param name="elKind">The element kind.</param>
        public AddFolder(ProjectContainer pc, Project pr, string path, ElementKind elKind)
        {
            try
            {
                InitializeComponent();

                projectContainer = pc;
                currentProject = pr;
                this.path = path;
                elementKind = elKind;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Button_File control.
        /// Opens a folder browser dialog and sets the selected path to the text box.
        /// </summary>
        private void Button_File_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wf.FolderBrowserDialog fr = new wf.FolderBrowserDialog();

                if (fr.ShowDialog() == wf.DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(fr.SelectedPath))
                        TbAddFile.Text = fr.SelectedPath;
                }
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Button_OK control.
        /// Creates a new directory or copies files to the specified directory based on the user's selection.
        /// </summary>
        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)Ch1.IsChecked)
                {
                    string s = TbNewFile.Text;
                    if (string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show("Please fill Directory name!");
                        return;
                    }
                    else if (io.Directory.Exists(path + "\\" + s))
                    {
                        MessageBox.Show(String.Format("Dir: [{0}] already exist in this location!", path + "\\" + s));
                        return;
                    }
                    else
                        io.Directory.CreateDirectory(path + "\\" + s);

                    Close();
                }
                else
                {
                    string s = TbAddFile.Text;
                    if (string.IsNullOrEmpty(s))
                    {
                        MessageBox.Show("Please fill Directory name!");
                        return;
                    }
                    else
                    {
                        if (MessageBox.Show("Do you want to overwrite similar files?", "Attention", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                            DirectoryCopy(s, path, true);
                    }

                    Close();
                }
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Button_Cancel control.
        /// Closes the window.
        /// </summary>
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

        /// <summary>
        /// Copies a directory and its contents to the specified destination directory.
        /// </summary>
        /// <param name="sourceDirName">The source directory name.</param>
        /// <param name="destDirName">The destination directory name.</param>
        /// <param name="copySubDirs">A value indicating whether to copy subdirectories.</param>
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
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
                file.CopyTo(temppath, false);
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
    }
}