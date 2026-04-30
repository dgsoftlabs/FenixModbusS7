using ProjectDataLib;
using System;
using System.Linq;
using System.Windows;
using io = System.IO;
using wf = System.Windows.Forms;

namespace Fenix
{
    public partial class AddExistingScript : Window
    {
        private ProjectContainer projectContainer { get; set; }
        private Project currentProject { get; set; }
        private Guid selectedId { get; set; }
        private ElementKind selectedElementKind { get; set; }

        public AddExistingScript(ProjectContainer pc, Project pr, Guid sel, ElementKind elKind)
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

        private void Button_File_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wf.OpenFileDialog fr = new wf.OpenFileDialog();
                fr.Multiselect = true;
                fr.Filter = "Script Files (*.cs)|*.cs";

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

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TbAddFile.Text))
                {
                    MessageBox.Show("Please fill File(s) path(s)!");
                    return;
                }

                foreach (string s in TbAddFile.Text.Split(';'))
                {
                    string nName = io.Path.GetFileName(s);
                    string TarDir = io.Path.GetDirectoryName(currentProject.path) + projectContainer.ScriptsCatalog;

                    if (!io.Directory.Exists(TarDir))
                        io.Directory.CreateDirectory(TarDir);

                    string destPath = TarDir + "\\" + nName;
                    using (var srcStream = new io.FileStream(s, io.FileMode.Open, io.FileAccess.Read, io.FileShare.ReadWrite))
                    using (var dstStream = new io.FileStream(destPath, io.FileMode.Create, io.FileAccess.Write, io.FileShare.None))
                        srcStream.CopyTo(dstStream);

                    projectContainer.AddScriptFile(currentProject.objId, new ScriptFile(TarDir + "\\" + nName));
                }

                Close();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

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
