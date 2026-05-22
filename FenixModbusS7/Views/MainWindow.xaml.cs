using AvalonDock.Layout;
using Microsoft.Win32;
using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using wf = System.Windows.Forms;

namespace Fenix
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;

            //PropertyGrid

            laPropGrid.Content = propManag;
            laPropGrid.Title = "\u2699\ufe0f Properties";
            laPropGrid.ContentId = "Properties";
            RightPan.Children.Add(laPropGrid);

            //TreeView

            tvMain.View.SelectedItemChanged += ContextMenu_AttachRigtMenu_SelectedItemChanged;

            laTvMain.Content = tvMain;
            laTvMain.Title = "\ud83d\uddc2\ufe0f Solution Explorer";
            laTvMain.ContentId = "Solution";
            LeftPan.Children.Add(laTvMain);

            //Exceptions
            frOutput = new OutputView(PrCon, exList);
            laOutput.Title = "\ud83d\udccb Output";
            laOutput.Content = frOutput;
            laOutput.ContentId = "Output";
            laGrOutput.Children.Add(laOutput);
            BottomPan.Children.Add(laGrOutput);

            //frOutput.View
            frOutput.View.DataContext = exList;
            frOutput.View.ItemsSource = exList;

            Title = "FenixModbusS7 " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            PrCon.addProjectEv += new EventHandler<ProjectEventArgs>(AddProjectEvent);

            PrCon.ApplicationError += new EventHandler(Error);

            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            if (identity != null)
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                    Title = Title + " (Administrator)";
            }

            CheckAccessForNodes();

            string[] s = Environment.GetCommandLineArgs();
            if (s.Length > 1)
            {
                if (File.Exists(s[1]))
                {
                    PrCon.openProjects(s[1]);
                    Pr = PrCon.projectList.First();
                    Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Pr.path);
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                SaveLayout();

                lbPathProject.Text = string.Empty;
                Pr = null;
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await VerifySoftwareUpdate(false);

                if (!String.IsNullOrEmpty(pathRun))
                {
                    PrCon.openProjects(pathRun);
                    Pr = PrCon.projectList[0];
                    Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Pr.path);

                    CheckAccessForNodes();
                }
                else if (Pr == null)
                {
                    TryLoadLastProjectFromRegistry();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void TryLoadLastProjectFromRegistry()
        {
            try
            {
                string startupPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string defaultProjectPath = Path.Combine(startupPath, "Project.pse");
                string lastPath = Registry.GetValue(PrCon.RegUserRoot, PrCon.LastPathKey, defaultProjectPath) as string;

                if (string.IsNullOrWhiteSpace(lastPath) || !File.Exists(lastPath))
                    return;

                if (PrCon.openProjects(lastPath))
                {
                    Pr = PrCon.projectList.FirstOrDefault();
                    if (Pr != null)
                    {
                        Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Pr.path);
                        CheckAccessForNodes();
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddProject fr = new AddProject(PrCon);
                fr.Owner = this;
                fr.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Pr != null)
                {
                    MessageBox.Show(this, "Project is already load. Please close project and try again!");
                    return;
                }

                string startupPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string strp = (string)Registry.GetValue(PrCon.RegUserRoot, PrCon.LastPathKey, startupPath + "\\Project.pse");
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = Path.GetDirectoryName(strp);
                ofd.Filter = "Fenix project files (*.pse;*.psx)|*.pse;*.psx";

                if (ofd.ShowDialog(this) == true)
                {
                    PrCon.openProjects(ofd.FileName);
                    Pr = PrCon.projectList.First();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ConnectionAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddConnection addConnection = new AddConnection(PrCon, PrCon.gConf, Pr.objId);
                addConnection.Owner = this;
                addConnection.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void DeviceAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddDevice addDevice = new AddDevice(PrCon, Pr.objId, SelGuid);
                addDevice.Owner = this;
                addDevice.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void TagAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddTag addTag = new AddTag(ref PrCon, Pr.objId, SelGuid);
                addTag.Owner = this;
                addTag.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void IntTagAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddInTag addTag = new AddInTag(Pr.objId, PrCon);
                addTag.Owner = this;
                addTag.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void TimerAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelObj is TimersFolder folder)
                    folder.AddTimer();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void TimerDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelObj is CustomTimer timer && _selectedTimersFolder != null)
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete timer '{timer.Name}'?",
                        "Delete Timer",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                        _selectedTimersFolder.RemoveTimer(timer);
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void FolderAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tvMain.View.SelectedItem is Project || tvMain.View.SelectedItem is WebServer || tvMain.View.SelectedItem is CusFile)
                {
                    string targetPath = Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog;
                    if (tvMain.View.SelectedItem is CusFile cf && !cf.IsFile)
                        targetPath = cf.FullName;

                    AddFolder fr = new AddFolder(PrCon, Pr, targetPath, actualKindElement);
                    fr.Owner = this;
                    fr.Show();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void FileAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string targetPath = Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog;
                if (tvMain.View.SelectedItem is CusFile cf && !cf.IsFile)
                    targetPath = cf.FullName;

                AddCusFile fr = new AddCusFile(PrCon, Pr, targetPath, actualKindElement);
                fr.Owner = this;
                fr.Show();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ScriptFileAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddScript fr = new AddScript(PrCon, Pr, SelGuid, actualKindElement);
                fr.Owner = this;
                fr.Show();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ScriptFileExistingAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddExistingScript fr = new AddExistingScript(PrCon, Pr, SelGuid, actualKindElement);
                fr.Owner = this;
                fr.Show();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ShowLocation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (actualKindElement == ElementKind.Project)
                {
                    Process.Start(new ProcessStartInfo(Path.GetDirectoryName(Pr.path)) { UseShellExecute = true });
                }
                else if (actualKindElement == ElementKind.HttpConfig)
                {
                    Process.Start(new ProcessStartInfo(Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog) { UseShellExecute = true });
                }
                else if (actualKindElement == ElementKind.Scripts)
                {
                    Process.Start(new ProcessStartInfo(Path.GetDirectoryName(Pr.path) + PrCon.ScriptsCatalog) { UseShellExecute = true });
                }
                else if (actualKindElement == ElementKind.InFile)
                {
                    if (tvMain.View.SelectedItem is CusFile selected && !string.IsNullOrWhiteSpace(selected.FullName))
                        Process.Start(new ProcessStartInfo(selected.FullName) { UseShellExecute = true });
                    else
                        Process.Start(new ProcessStartInfo(Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog) { UseShellExecute = true });
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ProjectClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var docs = dockManager.Layout.Descendents()
                    .OfType<LayoutAnchorable>()
                    .Where(x => x.ContentId != "Output" && x.ContentId != "Properties" && x.ContentId != "Solution")
                    .Select(x => x).ToList();

                for (int i = 0; i < docs.Count(); i++)
                    ((LayoutAnchorable)docs[i]).Close();

                SaveLayout();

                propManag.SelectedObject = null;
                exList.Clear();

                tvMain.View.ItemsSource = null;

                actualKindElement = ElementKind.Empty;

                PrCon.closeAllProject(true);

                Pr = null;

                lbPathProject.Text = "Ready";
                lbInfo.Text = string.Empty;

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ProjectSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool saved = false;

                if (String.IsNullOrEmpty(Pr.path))
                {
                    wf.SaveFileDialog sfd = new wf.SaveFileDialog();
                    sfd.Filter = "Fenix files (*.pse)|*.pse|All files (*.*)|*.*";
                    sfd.DefaultExt = "pse";
                    sfd.AddExtension = true;
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        PrCon.saveProject(Pr, sfd.FileName);
                        saved = true;
                    }
                }
                else
                {
                    PrCon.saveProject(Pr, Pr.path);
                    saved = true;
                }

                if (saved)
                    SaveLayout();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void DriverCommunicationStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelObj == null)
                    return;

                if (((ITreeViewModel)SelObj).IsBlocked)
                    throw new Exception(((ITreeViewModel)SelObj).Name + ": Element is blocked!");

                if (SelObj is IDriverModel)
                {
                    ((IDriverModel)SelObj).error -= Error;
                    ((IDriverModel)SelObj).information -= Error;

                    List<ITag> tgs = PrCon.GetAllITags(Pr.objId, ((IDriverModel)SelObj).ObjId);
                    ((IDriverModel)SelObj).error += Error;
                    ((IDriverModel)SelObj).information += Error;
                    ((IDriverModel)SelObj).activateCycle(tgs);

                    ((ITreeViewModel)SelObj).IsLive = ((IDriverModel)SelObj).isAlive;
                    foreach (ITreeViewModel obj1 in ((ITreeViewModel)SelObj).Children)
                    {
                        obj1.IsLive = ((IDriverModel)SelObj).isAlive;
                        foreach (ITreeViewModel obj2 in ((ITreeViewModel)obj1).Children)
                            obj2.IsLive = ((IDriverModel)SelObj).isAlive;
                    }

                    CheckAccessForNodes();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void DriverCommnicationStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelObj == null)
                    return;

                if (SelObj is IDriverModel)
                {
                    ((IDriverModel)SelObj).error -= Error;
                    ((IDriverModel)SelObj).information -= Error;
                    ((IDriverModel)SelObj).deactivateCycle();

                    ((ITreeViewModel)SelObj).IsLive = ((IDriverModel)SelObj).isAlive;
                    foreach (ITreeViewModel obj1 in ((ITreeViewModel)SelObj).Children)
                    {
                        obj1.IsLive = ((IDriverModel)SelObj).isAlive;
                        foreach (ITreeViewModel obj2 in ((ITreeViewModel)obj1).Children)
                            obj2.IsLive = ((IDriverModel)SelObj).isAlive;
                    }

                    if (((IDriverModel)SelObj).isAlive)
                    {
                        CommunicationWait fr = new CommunicationWait((IDriverModel)SelObj);
                        fr.Owner = this;
                        fr.ShowDialog();
                    }

                    CheckAccessForNodes();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void AllDriverCommunicationStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (IDriverModel id in ((IDriversMagazine)Pr).Children)
                {
                    try
                    {
                        id.information -= Error;
                        id.error -= Error;

                        List<ITag> tagsList = PrCon.GetAllITagsForDriver(Pr.objId, id.ObjId) ?? new List<ITag>();

                        id.information += Error;
                        id.error += Error;

                        id.activateCycle(tagsList);
                    }
                    catch (Exception Ex)
                    {
                        PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                    }
                }

                List<object> lista1 = new List<object>();
                lista1.AddRange(Pr.connectionList.ToArray());
                lista1.Add(Pr.InternalTagsDrv);
                lista1.Add(Pr.ScriptEng);

                foreach (object obj in lista1)
                {
                    ((ITreeViewModel)obj).IsLive = ((IDriverModel)obj).isAlive;
                    foreach (ITreeViewModel obj1 in ((ITreeViewModel)obj).Children)
                    {
                        obj1.IsLive = ((IDriverModel)obj).isAlive;
                        foreach (ITreeViewModel obj2 in ((ITreeViewModel)obj1).Children)
                            obj2.IsLive = ((IDriverModel)obj).isAlive;
                    }
                }

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void AllDriverCommunicationStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (IDriverModel id in ((IDriversMagazine)Pr).Children)
                {
                    try
                    {
                        id.information -= Error;
                        id.error -= Error;
                        id.deactivateCycle();

                        if (id.isAlive)
                        {
                            CommunicationWait fr = new CommunicationWait(id);
                            fr.Owner = this;
                            fr.ShowDialog();
                        }
                    }
                    catch (Exception Ex)
                    {
                        PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                    }
                }

                List<object> lista1 = new List<object>();
                lista1.AddRange(Pr.connectionList.ToArray());
                lista1.Add(Pr.InternalTagsDrv);
                lista1.Add(Pr.ScriptEng);

                foreach (object obj in lista1)
                {
                    ((ITreeViewModel)obj).IsLive = ((IDriverModel)obj).isAlive;
                    foreach (ITreeViewModel obj1 in ((ITreeViewModel)obj).Children)
                    {
                        obj1.IsLive = ((IDriverModel)obj).isAlive;
                        foreach (ITreeViewModel obj2 in ((ITreeViewModel)obj1).Children)
                            obj2.IsLive = ((IDriverModel)obj).isAlive;
                    }
                }

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ElementCut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrCon.copyCutElement(Pr.objId, SelGuid, actualKindElement, true);
                SelSrcPath = (tvMain.View.SelectedItem is CusFile srcFile && srcFile.IsFile) ? srcFile.FullName : string.Empty;

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ElementCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrCon.copyCutElement(Pr.objId, SelGuid, actualKindElement, false);
                SelSrcPath = (tvMain.View.SelectedItem is CusFile srcFile && srcFile.IsFile) ? srcFile.FullName : string.Empty;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ElementPaste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PrCon.cutMarks)
                {
                    if (!string.IsNullOrEmpty(SelSrcPath))
                    {
                        if (tvMain.View.SelectedItem is Project || tvMain.View.SelectedItem is WebServer || (tvMain.View.SelectedItem is CusFile folder && !folder.IsFile))
                        {
                            string basePath = tvMain.View.SelectedItem is CusFile selectedFolder && !selectedFolder.IsFile
                                ? selectedFolder.FullName
                                : Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog;

                            string dest = basePath + "\\" + Path.GetFileName(SelSrcPath);
                            if (dest == SelSrcPath)
                                throw new ApplicationException("This operation is forbbiden");

                            File.Copy(SelSrcPath, dest, true);
                            File.Delete(SelSrcPath);

                            SelSrcPath = string.Empty;
                            PrCon.SrcType = ElementKind.Empty;
                            PrCon.cutMarks = false;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(SelSrcPath))
                    {
                        if (tvMain.View.SelectedItem is Project || tvMain.View.SelectedItem is WebServer || (tvMain.View.SelectedItem is CusFile folder && !folder.IsFile))
                        {
                            string basePath = tvMain.View.SelectedItem is CusFile selectedFolder && !selectedFolder.IsFile
                                ? selectedFolder.FullName
                                : Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog;

                            string dest = basePath + "\\" + Path.GetFileName(SelSrcPath);
                            if (dest == SelSrcPath)
                                throw new ApplicationException("This operation is forbbiden");

                            File.Copy(SelSrcPath, dest, true);

                            SelSrcPath = string.Empty;
                            PrCon.SrcType = ElementKind.Empty;
                            PrCon.cutMarks = false;
                        }
                    }
                }

                SelSrcPath = string.Empty;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ElementDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DeleteElementMethod(Pr.objId, SelGuid, actualKindElement);
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void SolutionShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable solutionAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.ContentId == "Solution").FirstOrDefault();
                solutionAnchor?.IsVisible = true;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void PropertiesShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable propertiesAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.ContentId == "Properties").FirstOrDefault();
                propertiesAnchor?.IsVisible = true;
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void OutputShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable outputAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.ContentId == "Output").FirstOrDefault();
                outputAnchor?.IsVisible = true;
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void TableViewShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Pr == null)
                    return;

                var tableViewAnchor = new LayoutAnchorable
                {
                    CanClose = true,
                    Title = $"\ud83d\udcca {((ITreeViewModel)SelObj)?.Name ?? "Table View"}",
                    ContentId = $"TableView;{SelGuid};{actualKindElement}"
                };

                var tbView = new TableView(PrCon, Pr.objId, SelGuid, actualKindElement, tableViewAnchor);
                tableViewAnchor.Closed += EditorLayoutElement_Closed;
                tableViewAnchor.Content = tbView;

                var middlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePan1 != null)
                {
                    middlePan1.Children.Add(tableViewAnchor);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(tableViewAnchor));
                }

                tableViewAnchor.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void TableViewReadOnlyShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Pr == null)
                    return;

                var tableViewROAnchor = new LayoutAnchorable
                {
                    CanClose = true,
                    Title = $"\ud83d\udccb {((ITreeViewModel)SelObj)?.Name ?? "Table View RO"}",
                    ContentId = $"TableViewRO;{SelGuid};{actualKindElement}"
                };

                var tbViewRO = new TableViewRO(PrCon, Pr.objId, SelGuid, actualKindElement, tableViewROAnchor);
                tableViewROAnchor.Closed += EditorLayoutElement_Closed;
                tableViewROAnchor.Content = tbViewRO;

                var middlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePan1 != null)
                    middlePan1.Children.Add(tableViewROAnchor);
                else
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(tableViewROAnchor));

                tableViewROAnchor.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ChartViewShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var chartViewAnchor = new LayoutAnchorable
                {
                    CanClose = true,
                    Title = $"\ud83d\udcc8 {((ITreeViewModel)SelObj)?.Name ?? "Chart View"}",
                    ContentId = $"ChartView;{SelGuid};{actualKindElement}"
                };

                var chartView = new ChartView(PrCon, Pr.objId, SelGuid, actualKindElement, chartViewAnchor);
                chartViewAnchor.Closed += EditorLayoutElement_Closed;
                chartViewAnchor.Content = chartView;

                var middlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePan1 != null)
                {
                    middlePan1.Children.Add(chartViewAnchor);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(chartViewAnchor));
                }

                chartViewAnchor.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private void CommViewShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var communicationAnchor = new LayoutAnchorable
                {
                    CanClose = true,
                    Title = $"\ud83d\udce1 {((ITreeViewModel)SelObj)?.Name ?? "Comm View"}",
                    ContentId = $"CommView;{SelGuid};{actualKindElement}"
                };

                var commView = new CommunicationView(PrCon, Pr.objId, SelGuid, actualKindElement, communicationAnchor);
                communicationAnchor.Closed += EditorLayoutElement_Closed;
                communicationAnchor.Content = commView;

                var middlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePan1 != null)
                {
                    middlePan1.Children.Add(communicationAnchor);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(communicationAnchor));
                }

                communicationAnchor.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private void EditorShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable editorAnchor = new LayoutAnchorable
                {
                    CanClose = true,
                    ContentId = $"Editor;{SelGuid};{actualKindElement}"
                };

                ScriptEditor edit;

                if (actualKindElement == ElementKind.ScriptFile)
                {
                    var file = PrCon.GetScriptFile(Pr.objId, SelGuid);
                    edit = new ScriptEditor(PrCon, Pr.objId, file.FilePath, actualKindElement, editorAnchor);
                    editorAnchor.Title = file.Name;
                }
                else if (actualKindElement == ElementKind.InFile && tvMain.View.SelectedItem is CusFile selected && selected.IsFile)
                {
                    edit = new ScriptEditor(PrCon, Pr.objId, selected.FullName, actualKindElement, editorAnchor);
                    editorAnchor.Title = Path.GetFileName(selected.FullName);
                }
                else
                {
                    return;
                }

                editorAnchor.Closed += EditorLayoutElement_Closed;
                editorAnchor.Content = edit;

                var middlePane = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePane != null)
                {
                    middlePane.Children.Add(editorAnchor);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(editorAnchor));
                }

                editorAnchor.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void EditorLayoutElement_Closed(object sender, EventArgs e)
        {
            try
            {
                LayoutAnchorable win = (LayoutAnchorable)sender;
                win.Closed -= EditorLayoutElement_Closed;

                if (win.Content is TableView)
                {
                    TableView tbVw = (TableView)win.Content;
                    tbVw.View.ItemsSource = null;
                    win.Content = null;
                    GC.Collect();
                }
                else if (win.Content is ChartView)
                {
                    ChartView tbVw = (ChartView)win.Content;
                    win.Content = null;
                    tbVw = null;
                    GC.Collect();
                }
                else if (win.Content is ScriptEditor)
                {
                    ScriptEditor editor = (ScriptEditor)win.Content;
                    win.Content = null;
                }
                else if (win.Content is CommunicationView)
                {
                    CommunicationView commView = (CommunicationView)win.Content;
                    win.Content = null;
                    GC.Collect();
                }

                win = null;
                SaveLayout();
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void DriversConfigurationShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DriverConfigurator dConf = new DriverConfigurator(PrCon.gConf, PrCon);
                dConf.Owner = this;
                dConf.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void AboutShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                About about = new About();
                about.Owner = this;
                about.Show();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void UpdatesShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckVersion frVersion = new CheckVersion(PrCon);
                frVersion.Owner = this;
                frVersion.Show();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void HelpShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(PrCon.HelpWebSite) { UseShellExecute = true });
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void MainWindowExit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void DriverBlock_Click(object sender, RoutedEventArgs e)
        {
            if (SelObj != null)
            {
                if (!((ITreeViewModel)SelObj).IsBlocked)
                {
                    ((ITreeViewModel)SelObj).IsBlocked = true;
                    CheckAccessForNodes();
                }
            }
        }

        private void DriverUnblock_Click(object sender, RoutedEventArgs e)
        {
            if (SelObj != null)
            {
                if (((ITreeViewModel)SelObj).IsBlocked)
                {
                    ((ITreeViewModel)SelObj).IsBlocked = false;
                    CheckAccessForNodes();
                }
            }
        }

        private void DatabaseReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Pr.Db.Reset();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void DatabaseExplorerShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dbRelative = PrCon.Database.TrimStart('\\', '/');
                string fullDbPath = Path.Combine(Path.GetDirectoryName(Pr.path), dbRelative);
                string p = Path.GetDirectoryName(fullDbPath);
                if (Directory.Exists(p))
                    Process.Start(new ProcessStartInfo(p) { UseShellExecute = true });
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void DatabseTableViewShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable laTableView = new LayoutAnchorable();
                laTableView.CanClose = true;
                laTableView.ContentId = "TableDatabase";
                DBTableView db = new DBTableView(Pr);
                laTableView.Closed += EditorLayoutElement_Closed;
                laTableView.Content = db;

                var MiddlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().First();

                MiddlePan1.Children.Add(laTableView);
                laTableView.IsActive = true;
                laTableView.Title = "\ud83d\uddc4\ufe0f Table Database";

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void DatabaseTrendViewShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable laTableView = new LayoutAnchorable();
                laTableView.CanClose = true;
                laTableView.ContentId = "TrendDatabase";
                DBChartView chart = new DBChartView(Pr);
                laTableView.Closed += EditorLayoutElement_Closed;
                laTableView.Content = chart;

                var MiddlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().First();

                MiddlePan1.Children.Add(laTableView);
                laTableView.IsActive = true;
                laTableView.Title = "\ud83d\udcc9 Chart Database";

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void DatabeCSVExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tags = Pr.Db.GetAll();

                var tagNames = tags.Select(t => t.Name).Distinct().ToList();

                // Group by second precision — same logic as DBTableView pivot table
                var groups = tags
                    .GroupBy(t => new DateTime(t.Stamp.Year, t.Stamp.Month, t.Stamp.Day,
                                               t.Stamp.Hour, t.Stamp.Minute, t.Stamp.Second))
                    .OrderBy(g => g.Key);

                StringBuilder sb = new StringBuilder();

                // Header row: Stamp + one column per tag name
                sb.Append("Stamp");
                foreach (var name in tagNames)
                    sb.Append($",{name}");
                sb.AppendLine();

                // Data rows — one row per timestamp group
                foreach (var group in groups)
                {
                    sb.Append(group.Key.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (var name in tagNames)
                    {
                        var entry = group.FirstOrDefault(t => t.Name == name);
                        sb.Append(',');
                        if (entry != null)
                            sb.Append(entry.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    sb.AppendLine();
                }

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV files (*.csv)|*.csv";
                if (sfd.ShowDialog(this) == true)
                    File.WriteAllText(sfd.FileName, sb.ToString());
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ChartAddAxisY_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var axes = Pr.ChartConf.Axes;
                int idx = axes.Count + 1;
                bool isRight = idx % 2 == 0;
                string key = "Y" + idx;
                axes.Add(new ChartAxisConf(key, key, isRight));
                Pr.ChartConf.Axes = axes;
                Pr.ChartConfigNode.RefreshChildren();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ChartRemoveAxisY_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SelObj is ChartAxisNode axisNode)
                {
                    var axes = Pr.ChartConf.Axes;
                    if (axes.Count <= 1) return;
                    axes.Remove(axisNode.AxisConf);
                    Pr.ChartConf.Axes = axes;
                    Pr.ChartConfigNode.RefreshChildren();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ContextMenu_AttachRigtMenu_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                propManag.SelectedObject = e.NewValue;
                SelObj = e.NewValue;

                if (e.NewValue is Project)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxProject"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxProject"];
                    SelGuid = ((Project)e.NewValue).objId;
                    actualKindElement = ElementKind.Project;
                }
                else if (e.NewValue is WebServer)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxHttpServer"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxHttpServer"];
                    SelGuid = ((WebServer)e.NewValue).ObjId;
                    actualKindElement = ElementKind.HttpConfig;
                }
                else if (e.NewValue is CusFile)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    if (((CusFile)e.NewValue).IsFile)
                    {
                        ((ContextMenu)Resources["CtxInFile"]).DataContext = _viewModel;
                        tvMain.View.ContextMenu = (ContextMenu)Resources["CtxInFile"];
                        SelGuid = PrCon.HttpFileGuid;
                        actualKindElement = ElementKind.InFile;
                    }
                    else
                    {
                        ((ContextMenu)Resources["CtxHttpServer"]).DataContext = _viewModel;
                        tvMain.View.ContextMenu = (ContextMenu)Resources["CtxHttpServer"];
                        SelGuid = PrCon.HttpFileGuid;
                        actualKindElement = ElementKind.InFile;
                    }
                }
                else if (e.NewValue is DatabaseModel)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxDatabse"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxDatabse"];
                }
                else if (e.NewValue is ChartConfigNode)
                {
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxChartConfig"];
                }
                else if (e.NewValue is ChartAxisNode axisNode)
                {
                    propManag.SelectedObject = axisNode.AxisConf;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxChartAxis"];
                }
                else if (e.NewValue is ScriptsDriver)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxScripts"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxScripts"];
                    SelGuid = ((ScriptsDriver)e.NewValue).objId;
                    actualKindElement = ElementKind.Scripts;
                }
                else if (e.NewValue is ScriptFile)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxScriptFile"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxScriptFile"];
                    SelGuid = ((ScriptFile)e.NewValue).objId;
                    actualKindElement = ElementKind.ScriptFile;
                }
                else if (e.NewValue is InternalTagsDriver)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxInternalTags"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxInternalTags"];
                    SelGuid = ((InternalTagsDriver)e.NewValue).objId;
                    actualKindElement = ElementKind.InternalsTags;
                }
                else if (e.NewValue is InTag)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxIntTag"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxIntTag"];
                    SelGuid = ((InTag)e.NewValue).objId;
                    actualKindElement = ElementKind.IntTag;

                    if (e.NewValue != null)
                        ((INotifyPropertyChanged)e.NewValue).PropertyChanged += TreeViewPropertiesBind_PropertyChanged;
                }
                else if (e.NewValue is CustomTimer timer)
                {
                    _selectedTimersFolder = FindParentTimersFolder(timer);
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxTimer"];
                }
                else if (e.NewValue is TimersFolder tf)
                {
                    _selectedTimersFolder = tf;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxTimers"];
                }
                else if (e.NewValue is Connection)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxConnection"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxConnection"];
                    SelGuid = ((Connection)e.NewValue).objId;
                    actualKindElement = ElementKind.Connection;
                }
                else if (e.NewValue is Device)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxDevice"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxDevice"];
                    SelGuid = ((Device)e.NewValue).objId;
                    actualKindElement = ElementKind.Device;
                }
                else if (e.NewValue is Tag)
                {
                    if (e.OldValue != null)
                        (e.OldValue as System.ComponentModel.INotifyPropertyChanged)?.PropertyChanged -= TreeViewPropertiesBind_PropertyChanged;

                    ((ContextMenu)Resources["CtxTag"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxTag"];
                    SelGuid = ((Tag)e.NewValue).objId;
                    actualKindElement = ElementKind.Tag;

                    if (e.NewValue != null)
                        ((INotifyPropertyChanged)e.NewValue).PropertyChanged += TreeViewPropertiesBind_PropertyChanged;
                }

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void TreeViewPropertiesBind_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (SelObj is Tag || SelObj is ITag)
                {
                    if (!((IDriverModel)sender).isAlive)
                        propManag.SelectedObject = sender;
                }
            }));
        }

        public override string ToString()
        {
            return "FenixModbusS7";
        }
    }
}