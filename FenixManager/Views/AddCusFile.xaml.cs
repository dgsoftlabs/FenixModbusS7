using ProjectDataLib;
using System;
using System.Linq;
using System.Windows;
using io = System.IO;
using wf = System.Windows.Forms;

namespace FenixWPF
{
    /// <summary>
    /// Interaction logic for AddInFile.xaml
    /// </summary>
    public partial class AddCusFile : Window
    {
        private ProjectContainer projectContainer { get; set; }
        private Project currentProject { get; set; }
        private String path { get; set; }
        private ElementKind currentElementKind { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddCusFile"/> class.
        /// </summary>
        /// <param name="projectContainer">The project container.</param>
        /// <param name="currentProject">The current project.</param>
        /// <param name="path">The path.</param>
        /// <param name="elementKind">The element kind.</param>
        public AddCusFile(ProjectContainer projectContainer, Project currentProject, string path, ElementKind elementKind)
        {
            try
            {
                InitializeComponent();

                this.projectContainer = projectContainer;
                this.currentProject = currentProject;
                this.path = path;
                currentElementKind = elementKind;
            }
            catch (Exception Ex)
            {
                this.projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Button_File control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_File_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wf.OpenFileDialog fr = new wf.OpenFileDialog();
                fr.Multiselect = true;
                fr.Filter = "All Files (*.*)|*.*";

                if (fr.ShowDialog() == wf.DialogResult.OK)
                {
                    if (fr.CheckPathExists)
                        TbAddFile.Text = fr.FileNames.Aggregate((p, k) => (p + ";" + k));
                }
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Button_OK control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)Ch1.IsChecked)
                {
                    if (string.IsNullOrEmpty(TbNewFile.Text))
                    {
                        MessageBox.Show("Please fill File(s) name(s)!");
                        return;
                    }

                    foreach (string s in TbNewFile.Text.Split(';'))
                    {
                        if (io.File.Exists(path + "\\" + s))
                        {
                            MessageBox.Show(String.Format("File: [{0}] already exist in this location!", path + "\\" + s));
                            return;
                        }
                        else
                            io.File.Create(path + "\\" + s).Close();
                    }

                    Close();
                }
                else
                {
                    if (string.IsNullOrEmpty(TbAddFile.Text))
                    {
                        MessageBox.Show("Please fill File(s) name(s)!");
                        return;
                    }

                    foreach (string s in TbAddFile.Text.Split(';'))
                    {
                        if (io.File.Exists(path + "//" + io.Path.GetFileName(s)))
                        {
                            MessageBox.Show(string.Format("File: [{0}] exist in target location!", s));
                            return;
                        }
                        else if (!io.File.Exists(s))
                        {
                            MessageBox.Show(string.Format("File: [{0}] not exist!", s));
                            return;
                        }
                        else if (path + "//" + io.Path.GetFileName(s) == s)
                        {
                            MessageBox.Show(string.Format("Yout try copy File1: [{0}] to File2: [{1}]! Not allowed!", s, path + "//" + io.Path.GetFileName(s)));
                            return;
                        }
                        else
                            io.File.Copy(s, path + "//" + io.Path.GetFileName(s));

                        Close();
                    }
                }
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Button_Cancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
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