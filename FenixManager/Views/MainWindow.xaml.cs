using Microsoft.Win32;
using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using FenixWPF.ViewModels;
using io = System.IO;
using wf = System.Windows.Forms;

namespace FenixWPF
{
    public partial class MainWindow : Window
    {
        #region Visibilty
        private readonly MainWindowViewModel _viewModel = new MainWindowViewModel();

        public Boolean mFile { get => _viewModel.mFile; set => _viewModel.mFile = value; }
        public Boolean mNew { get => _viewModel.mNew; set => _viewModel.mNew = value; }
        public Boolean mOpen { get => _viewModel.mOpen; set => _viewModel.mOpen = value; }
        public Boolean mAdd { get => _viewModel.mAdd; set => _viewModel.mAdd = value; }
        public Boolean mConnection { get => _viewModel.mConnection; set => _viewModel.mConnection = value; }
        public Boolean mDevice { get => _viewModel.mDevice; set => _viewModel.mDevice = value; }
        public Boolean mTag { get => _viewModel.mTag; set => _viewModel.mTag = value; }
        public Boolean mIntTag { get => _viewModel.mIntTag; set => _viewModel.mIntTag = value; }
        public Boolean mScriptFile { get => _viewModel.mScriptFile; set => _viewModel.mScriptFile = value; }
        public Boolean mFolder { get => _viewModel.mFolder; set => _viewModel.mFolder = value; }
        public Boolean mInFile { get => _viewModel.mInFile; set => _viewModel.mInFile = value; }
        public Boolean mClosePr { get => _viewModel.mClosePr; set => _viewModel.mClosePr = value; }
        public Boolean mSave { get => _viewModel.mSave; set => _viewModel.mSave = value; }
        public Boolean mSaveAs { get => _viewModel.mSaveAs; set => _viewModel.mSaveAs = value; }
        public Boolean mExit { get => _viewModel.mExit; set => _viewModel.mExit = value; }

        public Boolean mEdit { get => _viewModel.mEdit; set => _viewModel.mEdit = value; }
        public Boolean mCut { get => _viewModel.mCut; set => _viewModel.mCut = value; }
        public Boolean mCopy { get => _viewModel.mCopy; set => _viewModel.mCopy = value; }
        public Boolean mPaste { get => _viewModel.mPaste; set => _viewModel.mPaste = value; }
        public Boolean mDelete { get => _viewModel.mDelete; set => _viewModel.mDelete = value; }

        public Boolean mView { get => _viewModel.mView; set => _viewModel.mView = value; }
        public Boolean mSolution { get => _viewModel.mSolution; set => _viewModel.mSolution = value; }
        public Boolean mProperties { get => _viewModel.mProperties; set => _viewModel.mProperties = value; }
        public Boolean mOutput { get => _viewModel.mOutput; set => _viewModel.mOutput = value; }
        public Boolean mTable { get => _viewModel.mTable; set => _viewModel.mTable = value; }
        public Boolean mChart { get => _viewModel.mChart; set => _viewModel.mChart = value; }
        public Boolean mCommView { get => _viewModel.mCommView; set => _viewModel.mCommView = value; }
        public Boolean mEditor { get => _viewModel.mEditor; set => _viewModel.mEditor = value; }

        public Boolean mDriversSt { get => _viewModel.mDriversSt; set => _viewModel.mDriversSt = value; }
        public Boolean mStart { get => _viewModel.mStart; set => _viewModel.mStart = value; }
        public Boolean mStop { get => _viewModel.mStop; set => _viewModel.mStop = value; }
        public Boolean mStartAll { get => _viewModel.mStartAll; set => _viewModel.mStartAll = value; }
        public Boolean mStopAll { get => _viewModel.mStopAll; set => _viewModel.mStopAll = value; }

        public Boolean mTools { get => _viewModel.mTools; set => _viewModel.mTools = value; }
        public Boolean mBlock { get => _viewModel.mBlock; set => _viewModel.mBlock = value; }
        public Boolean mUnBlock { get => _viewModel.mUnBlock; set => _viewModel.mUnBlock = value; }
        public Boolean mSimulate { get => _viewModel.mSimulate; set => _viewModel.mSimulate = value; }
        public Boolean mShowLoc { get => _viewModel.mShowLoc; set => _viewModel.mShowLoc = value; }
        public Boolean mDrivers { get => _viewModel.mDrivers; set => _viewModel.mDrivers = value; }

        public Boolean mDatabase { get => _viewModel.mDatabase; set => _viewModel.mDatabase = value; }
        public Boolean mDbShowFile { get => _viewModel.mDbShowFile; set => _viewModel.mDbShowFile = value; }
        public Boolean mDbReset { get => _viewModel.mDbReset; set => _viewModel.mDbReset = value; }
        public Boolean mShowDb { get => _viewModel.mShowDb; set => _viewModel.mShowDb = value; }
        public Boolean mShowTrendDb { get => _viewModel.mShowTrendDb; set => _viewModel.mShowTrendDb = value; }
        public Boolean mSaveCSV { get => _viewModel.mSaveCSV; set => _viewModel.mSaveCSV = value; }

        public Boolean mHelp { get => _viewModel.mHelp; set => _viewModel.mHelp = value; }
        public Boolean mUpdates { get => _viewModel.mUpdates; set => _viewModel.mUpdates = value; }
        public Boolean mAbout { get => _viewModel.mAbout; set => _viewModel.mAbout = value; }
        public Boolean mViewHelp { get => _viewModel.mViewHelp; set => _viewModel.mViewHelp = value; }

        #endregion Visibilty

        #region Fileds

        private ProjectContainer PrCon = new ProjectContainer();
        private ElementKind actualKindElement;

        private object SelObj;
        private Guid SelGuid;

        private Project Pr;
        private string pathRun = "";

        private string SelSrcPath = string.Empty;

        private LayoutAnchorable laPropGrid = new LayoutAnchorable();
        private PropertiesGridManager propManag = new PropertiesGridManager();

        private LayoutAnchorable laTvMain = new LayoutAnchorable();
        private TreeViewManager tvMain = new TreeViewManager();

        private LayoutAnchorGroup laGrOutput = new LayoutAnchorGroup();
        private LayoutAnchorable laOutput = new LayoutAnchorable();
        private Output frOutput;

        private ObservableCollection<CustomException> exList = new ObservableCollection<CustomException>();

        private io.FileSystemWatcher FsWatcher;

        #region External Events

        private void AddProjectEvent(object sender, ProjectEventArgs ev)
        {
            try
            {
                Project pr = (Project)ev.element;
                this.Pr = pr;

                #region Sprawdzenie czy istnieje zapisany layout

                if (io.File.Exists(io.Path.GetDirectoryName(PrCon.projectList.First().path) + "\\" + PrCon.LayoutFile))
                {
                    Project pp = (Project)sender;
                    XmlLayoutSerializer serializer = new XmlLayoutSerializer(dockManager);

                    serializer.LayoutSerializationCallback += (s, args) =>
                    {
                        string[] param = args.Model.ContentId.Split(';');
                        switch (param[0])
                        {
                            case "Properties":
                                args.Content = propManag;
                                break;

                            case "Solution":
                                args.Content = tvMain;
                                break;

                            case "Output":
                                args.Content = frOutput;
                                break;

                            case "Database":
                                //args.Content = new DbExplorer(null);
                                break;

                            case "TableView":
                                LayoutAnchorable laTableView = (LayoutAnchorable)args.Model;
                                laTableView.CanClose = true;
                                TableView tbView = new TableView(PrCon, pp.objId, Guid.Parse(param[1]), (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laTableView);
                                laTableView.Closed += LaCtrl_Closed;
                                args.Content = tbView;
                                break;

                            case "ChartView":
                                LayoutAnchorable laChartView = (LayoutAnchorable)args.Model;
                                laChartView.CanClose = true;
                                ChartView chView = new ChartView(PrCon, pp.objId, Guid.Parse(param[1]), (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laChartView);
                                laChartView.Closed += LaCtrl_Closed;
                                args.Content = chView;
                                break;

                            case "CommView":
                                LayoutAnchorable laCommView = (LayoutAnchorable)args.Model;
                                laCommView.CanClose = true;
                                CommunicationView comView = new CommunicationView(PrCon, pp.objId, Guid.Parse(param[1]), (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laCommView);
                                laCommView.Closed += LaCtrl_Closed;
                                args.Content = comView;
                                break;

                            case "Editor":
                                LayoutAnchorable laEditorView = (LayoutAnchorable)args.Model;
                                laEditorView.CanClose = true;

                                //Zabezpieczenie
                                if (io.File.Exists(param[1]))
                                {
                                    Editor edView = new Editor(PrCon, pp.objId, param[1], (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laEditorView);
                                    laEditorView.Closed += LaCtrl_Closed;
                                    args.Content = edView;
                                }
                                else
                                {
                                    laEditorView.Close();
                                }

                                break;

                            default:
                                args.Content = new System.Windows.Controls.TextBox() { Text = args.Model.ContentId };
                                break;
                        }
                    };

                    string ss = io.Path.GetDirectoryName(PrCon.projectList.First().path) + "\\" + PrCon.LayoutFile;
                    serializer.Deserialize(ss);
                }

                #endregion Sprawdzenie czy istnieje zapisany layout

                tvMain.View.DataContext = ((ITreeViewModel)PrCon).Children;
                tvMain.View.ItemsSource = ((ITreeViewModel)PrCon).Children;

                TreeViewItem PrNode = FindTviFromObjectRecursive(tvMain.View, pr);
                if (PrNode != null) PrNode.IsSelected = true;

                if (!io.Directory.Exists(io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog))
                    io.Directory.CreateDirectory(io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog);

                FsWatcher = new io.FileSystemWatcher(io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog);
                FsWatcher.IncludeSubdirectories = true;
                FsWatcher.Created += FsWatch_Changed;
                FsWatcher.Deleted += FsWatch_Changed;
                FsWatcher.Renamed += FsWatch_Changed;
                FsWatcher.EnableRaisingEvents = true;

                lbPathProject.Content = Pr.path;
                Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Pr.path);

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void FsWatch_Changed(object sender, io.FileSystemEventArgs e)
        {
            try
            {
                #region Renamed

                if (e.ChangeType == io.WatcherChangeTypes.Renamed)
                {
                    io.RenamedEventArgs k = (io.RenamedEventArgs)e;
                    ITreeViewModel m = Pr.WebServer1.DirSearch(k.OldFullPath, Pr.WebServer1);

                    if (m != null)
                        ((CusFile)m).FullName = k.FullPath;
                }

                #endregion Renamed

                #region Creted

                else if (e.ChangeType == io.WatcherChangeTypes.Created)
                {
                    if (io.File.Exists(e.FullPath))
                    {
                        string dir = io.Path.GetDirectoryName(e.FullPath);
                        if (dir == io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                ((ITreeViewModel)Pr.WebServer1).Children.Add(new CusFile(new io.FileInfo(e.FullPath)));
                            }));
                        }
                        else
                        {
                            ITreeViewModel tm = Pr.WebServer1.DirSearch(dir, Pr.WebServer1);
                            if (tm != null)
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    tm.Children.Add(new CusFile(new io.FileInfo(e.FullPath)));
                                }));
                            }
                        }
                    }
                    else
                    {
                        string rDir = io.Directory.GetParent(e.FullPath).FullName;
                        if (rDir == io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                ((ITreeViewModel)Pr.WebServer1).Children.Add(new CusFile(new io.DirectoryInfo(e.FullPath)));
                            }));
                        }
                        else
                        {
                            ITreeViewModel tm = Pr.WebServer1.DirSearch(rDir, Pr.WebServer1);
                            if (tm != null)
                            {
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    tm.Children.Add(new CusFile(new io.DirectoryInfo(e.FullPath)));
                                }));
                            }
                        }
                    }
                }

                #endregion Creted

                #region Delete

                if (e.ChangeType == System.IO.WatcherChangeTypes.Deleted)
                {
                    string sDir = io.Directory.GetParent(e.FullPath).FullName;
                    if (sDir == io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            var rem = ((ITreeViewModel)Pr.WebServer1).Children.ToList().Where(x => ((CusFile)x).FullName == e.FullPath).First();
                            ((ITreeViewModel)Pr.WebServer1).Children.Remove(rem);
                        }));
                    }
                    else
                    {
                        ITreeViewModel tm = Pr.WebServer1.DirSearch(e.FullPath, Pr.WebServer1);
                        ITreeViewModel par = Pr.WebServer1.DirSearch(sDir, Pr.WebServer1);

                        if (tm != null)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                (par).Children.Remove((CusFile)tm);
                            }));
                        }
                    }
                }

                #endregion Delete
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Error(object sender, EventArgs ev)
        {
            this.Dispatcher.Invoke(() =>
            {
                ProjectEventArgs e = (ProjectEventArgs)ev;

                if (e.element is Exception)
                    exList.Add(new CustomException(sender, (Exception)e.element));
                else if (e.element2 is Exception)
                    exList.Add(new CustomException(sender, (Exception)e.element2));
                else
                    exList.Add(new CustomException(sender, new Exception(e.element1.ToString())));

                LayoutAnchorable lpAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.Title == "Output").First();
                lpAnchor.IsActive = true;
            });
        }

        public static TreeViewItem FindTviFromObjectRecursive(ItemsControl ic, object o)
        {
            //Search for the object model in first level children (recursively)
            TreeViewItem tvi = ic.ItemContainerGenerator.ContainerFromItem(o) as TreeViewItem;
            if (tvi != null) return tvi;
            //Loop through user object models
            foreach (object i in ic.Items)
            {
                //Get the TreeViewItem associated with the iterated object model
                TreeViewItem tvi2 = ic.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
                tvi = FindTviFromObjectRecursive(tvi2, o);
                if (tvi != null) return tvi;
            }
            return null;
        }

        #endregion External Events

        #endregion Fileds

        #region Konstruktor

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;

            //PropertyGrid

            laPropGrid.Content = propManag;
            laPropGrid.Title = "Properties";
            laPropGrid.ContentId = "Properties";
            RightPan.Children.Add(laPropGrid);

            //TreeView

            tvMain.View.SelectedItemChanged += View_SelectedItemChanged;

            laTvMain.Content = tvMain;
            laTvMain.Title = "Solution";
            laTvMain.ContentId = "Solution";
            LeftPan.Children.Add(laTvMain);

            //Exceptions
            frOutput = new Output(PrCon, exList);
            laOutput.Title = "Output";
            laOutput.Content = frOutput;
            laOutput.ContentId = "Output";
            laGrOutput.Children.Add(laOutput);
            BottomPan.Children.Add(laGrOutput);

            //frOutput.View
            frOutput.View.DataContext = exList;
            frOutput.View.ItemsSource = exList;

            Title = "Fenix Manager " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

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
                if (io.File.Exists(s[1]))
                {
                    PrCon.openProjects(s[1]);
                    Pr = PrCon.projectList.First();
                    Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Pr.path);
                }
            }
        }

        #endregion Konstruktor

        #region Internal Commands

        private void CheckAccessForNodes()
        {
            try
            {
                propManag.Enabled = _viewModel.CheckAccessForNodes(
                    tvMain.View.SelectedItem,
                    SelObj,
                    PrCon.SrcType,
                    PrCon.anyCommunication(),
                    propManag.Enabled);
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void DeleteElementMethod(Guid projId, Guid id, ElementKind elKind)
        {
            try
            {
                //Brak projektów w Buforze
                if (PrCon.projectList.Count == 0)
                    return;

                //Chcemy usunąc projekt

                wf.DialogResult result = wf.MessageBox.Show("Do you really want delate this element", "Warning", wf.MessageBoxButtons.OKCancel, wf.MessageBoxIcon.Warning);
                if (result == wf.DialogResult.OK)
                {
                    PrCon.deleteElement(Pr.objId, id, elKind);
                    return;
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private async Task VerifySoftwareUpdate(object sender)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Dispatcher.Invoke(() => lbInfo.Content = "No Internet connection.");
                return;
            }

            Dispatcher.Invoke(() => lbInfo.Content = "Checking update for software...");

            try
            {
                string result = await ProjectContainer.GetVersionFromGitHub();
                if (result != null)
                {
                    var newVer = ProjectContainer.ParseVersionFromContent(result);
                    var url = ProjectContainer.ParseUrlFromContent(result);

                    CheckVersion(newVer, url, (bool)sender);
                    Dispatcher.Invoke(() => lbInfo.Content = "Completed");
                }
            }
            catch (Exception)
            {
                // Handle exceptions if necessary
            }
        }

        private void CheckVersion(Version newVersion, string url, bool automatic)
        {
            // Get the running version
            Version curVersion = Assembly.GetExecutingAssembly().GetName().Version;

            // Compare the versions
            if (curVersion < newVersion)
            {
                // Ask the user if they would like to download the new version
                string title = "New version detected.";
                string question = $"Download the new version Fenix {newVersion}?";

                if (wf.MessageBox.Show(question, title, wf.MessageBoxButtons.YesNo, wf.MessageBoxIcon.Question) == wf.DialogResult.Yes)
                {
                    // Navigate the default web browser to the app homepage (the URL comes from the XML content)
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            else if (automatic)
            {
                MessageBox.Show("Your version is up to date.");
            }
        }

        #endregion Internal Commands

        #region Internal Events

        private void New0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //dodanie projektu
                AddProject fr = new AddProject(PrCon);
                fr.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Open0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Pr != null)
                {
                    MessageBox.Show("Project is already load. Please close project and try again!");
                    return;
                }

                string startupPath = io.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string strp = (string)Registry.GetValue(PrCon.RegUserRoot, PrCon.LastPathKey, startupPath + "\\Project.psf");
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = io.Path.GetDirectoryName(strp);
                ofd.Filter = "XML Files (*.psx)|*.psx";

                if (ofd.ShowDialog() == true)
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

        private void Connection0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddConnection addConnection_ = new AddConnection(PrCon, PrCon.gConf, Pr.objId);
                addConnection_.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Device0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddDevice addTagFolder_ = new FenixWPF.AddDevice(PrCon, Pr.objId, SelGuid);
                addTagFolder_.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Tag0_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                AddTag addTag_ = new FenixWPF.AddTag(ref PrCon, Pr.objId, SelGuid);

                //reakcja USERA
                addTag_.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void IntTag0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FenixWPF.AddInTag aTag = new FenixWPF.AddInTag(Pr.objId, PrCon);
                aTag.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tvMain.View.SelectedItem is CusFile)
                {
                    AddFolder fr = new AddFolder(PrCon, Pr, ((CusFile)tvMain.View.SelectedItem).FullName, actualKindElement);
                    fr.Show();
                }
                else if (tvMain.View.SelectedItem is WebServer)
                {
                    AddFolder fr = new AddFolder(PrCon, Pr, io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog, actualKindElement);
                    fr.Show();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void File0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tvMain.View.SelectedItem is CusFile)
                {
                    AddCusFile fr = new AddCusFile(PrCon, Pr, ((CusFile)tvMain.View.SelectedItem).FullName, actualKindElement);
                    fr.Show();
                }
                else if (tvMain.View.SelectedItem is WebServer)
                {
                    AddCusFile fr = new AddCusFile(PrCon, Pr, io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog, actualKindElement);
                    fr.Show();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ScriptFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddScript fr = new AddScript(PrCon, Pr, SelGuid, actualKindElement);
                fr.Show();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ScriptFileExisting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddExistingScript fr = new AddExistingScript(PrCon, Pr, SelGuid, actualKindElement);
                fr.Show();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ShLocation0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (actualKindElement == ElementKind.Project)
                {
                    Process.Start(io.Path.GetDirectoryName(Pr.path));
                }
                else if (actualKindElement == ElementKind.HttpConfig)
                {
                    Process.Start(io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog);
                }
                else if (actualKindElement == ElementKind.Scripts)
                {
                    Process.Start(io.Path.GetDirectoryName(Pr.path) + PrCon.ScriptsCatalog);
                }
                else if (actualKindElement == ElementKind.InFile)
                {
                    Process.Start(((CusFile)tvMain.View.SelectedItem).FullName);
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ClProject0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Zapisanie layoutu
                if (Pr != null)
                {
                    XmlLayoutSerializer serializer = new XmlLayoutSerializer(dockManager);
                    serializer.Serialize(System.IO.Path.GetDirectoryName(PrCon.projectList.First().path) + "\\" + PrCon.LayoutFile);
                }

                //Zamkniecie edytorow
                var docs = dockManager.Layout.Descendents()
                    .OfType<LayoutAnchorable>()
                    .Where(x => x.Title != "Output" && x.Title != "Properties" && x.Title != "Solution")
                    .Select(x => x).ToList();

                for (int i = 0; i < docs.Count(); i++)
                    ((LayoutAnchorable)docs[i]).Close();

                propManag.SelectedObject = null;
                exList.Clear();

                tvMain.View.ItemsSource = null;

                actualKindElement = ElementKind.Empty;

                FsWatcher.EnableRaisingEvents = false;
                FsWatcher.Created -= FsWatch_Changed;
                FsWatcher.Deleted -= FsWatch_Changed;
                FsWatcher.Renamed -= FsWatch_Changed;
                FsWatcher = null;

                PrCon.closeAllProject(true);

                Pr = null;

                //Sprawdzenie i ustawienie menu
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Save0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Zapisanie projectu

                if (String.IsNullOrEmpty(Pr.path))
                {
                    wf.SaveFileDialog sfd = new wf.SaveFileDialog();
                    sfd.Filter = "Fenix files (*.psx)|*.psx|All files (*.*)|*.*";
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        PrCon.saveProject(Pr, sfd.FileName);
                    }
                }
                else
                    PrCon.saveProject(Pr, Pr.path);
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Start0_Click(object sender, RoutedEventArgs e)
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

        private void Stop0_Click(object sender, RoutedEventArgs e)
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
                        CommStop fr = new CommStop((IDriverModel)SelObj);
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

        private void StartAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (IDriverModel id in ((IDriversMagazine)Pr).Children)
                {
                    id.information -= Error;
                    id.error -= Error;

                    List<ITag> tagsList = PrCon.GetAllITagsForDriver(Pr.objId, id.ObjId);

                    id.information += Error;
                    id.error += Error;

                    id.activateCycle(tagsList);
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

        private void StopAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (IDriverModel id in ((IDriversMagazine)Pr).Children)
                {
                    id.information -= Error;
                    id.error -= Error;
                    id.deactivateCycle();

                    if (id.isAlive)
                    {
                        CommStop fr = new CommStop(id);
                        fr.ShowDialog();
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

        private void Simulation0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + @"\\FenixServer.exe";
                pInfo.UseShellExecute = true;
                pInfo.Verb = "runas";
                pInfo.Arguments = "-s";

                Process.Start(pInfo);
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Cut0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tvMain.View.SelectedItem is CusFile)
                {
                    CusFile f = (CusFile)tvMain.View.SelectedItem;
                    if (f.IsFile)
                    {
                        PrCon.SrcType = actualKindElement;
                        SelSrcPath = f.FullName;
                        PrCon.cutMarks = true;
                    }
                }
                else
                {
                    PrCon.copyCutElement(Pr.objId, SelGuid, actualKindElement, true);
                    SelSrcPath = string.Empty;
                }

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Copy0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tvMain.View.SelectedItem is CusFile)
                {
                    CusFile f = (CusFile)tvMain.View.SelectedItem;
                    if (f.IsFile)
                    {
                        PrCon.SrcType = actualKindElement;
                        SelSrcPath = f.FullName;
                        PrCon.cutMarks = false;
                    }
                }
                else
                {
                    PrCon.copyCutElement(Pr.objId, SelGuid, actualKindElement, false);
                    SelSrcPath = string.Empty;
                }

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Paste0_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (PrCon.SrcType != ElementKind.InFile)
                {
                    PrCon.pasteElement(Pr.objId, SelGuid);
                    CheckAccessForNodes();
                }
                else
                {
                    if (PrCon.cutMarks)
                    {
                        if (!string.IsNullOrEmpty(SelSrcPath))
                        {
                            if (tvMain.View.SelectedItem is WebServer)
                            {
                                string dest = io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog + "\\" + io.Path.GetFileName(SelSrcPath);
                                if (dest == SelSrcPath)
                                    throw new ApplicationException("This operation is forbbiden");

                                io.File.Copy(SelSrcPath, dest, true);
                                io.File.Delete(SelSrcPath);

                                SelSrcPath = string.Empty;
                                PrCon.SrcType = ElementKind.Empty;
                                PrCon.cutMarks = false;
                            }
                            else
                            {
                                string dest = ((CusFile)tvMain.View.SelectedItem).FullName + "\\" + io.Path.GetFileName(SelSrcPath);

                                if (dest == SelSrcPath)
                                    throw new ApplicationException("This operation is forbbiden");

                                io.File.Copy(SelSrcPath, dest, true);
                                io.File.Delete(SelSrcPath);

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
                            if (tvMain.View.SelectedItem is WebServer)
                            {
                                string dest = io.Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog + "\\" + io.Path.GetFileName(SelSrcPath);
                                if (dest == SelSrcPath)
                                    throw new ApplicationException("This operation is forbbiden");

                                io.File.Copy(SelSrcPath, dest, true);

                                SelSrcPath = string.Empty;
                                PrCon.SrcType = ElementKind.Empty;
                                PrCon.cutMarks = false;
                            }
                            else
                            {
                                string dest = ((CusFile)tvMain.View.SelectedItem).FullName + "\\" + io.Path.GetFileName(SelSrcPath);

                                if (dest == SelSrcPath)
                                    throw new ApplicationException("This operation is forbbiden");

                                io.File.Copy(SelSrcPath, dest, true);

                                SelSrcPath = string.Empty;
                                PrCon.SrcType = ElementKind.Empty;
                                PrCon.cutMarks = false;
                            }
                        }
                    }

                    SelSrcPath = string.Empty;
                    CheckAccessForNodes();
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Delete0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tvMain.View.SelectedItem is CusFile)
                {
                    CusFile f = (CusFile)tvMain.View.SelectedItem;
                    if (f == null)
                        return;

                    if (wf.MessageBox.Show("Do you want to remove this file or directory?", "Attention", wf.MessageBoxButtons.OKCancel) == wf.DialogResult.OK)
                    {
                        if (f.IsFile)
                            io.File.Delete(f.FullName);
                        else
                            io.Directory.Delete(f.FullName, true);

                        CheckAccessForNodes();
                    }
                }
                else
                {
                    DeleteElementMethod(Pr.objId, SelGuid, actualKindElement);
                    CheckAccessForNodes();
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Solution0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable lpAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.Title == "Solution").First();
                lpAnchor.IsVisible = true;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Properties0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable lpAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.Title == "Properties").First();
                lpAnchor.IsVisible = true;
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Output0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable lpAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.Title == "Output").First();
                lpAnchor.IsVisible = true;
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void TableView0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Pr == null)
                    return;

                var laTableViewAnchorable = new LayoutAnchorable
                {
                    CanClose = true,
                    ContentId = $"TableView;{SelGuid};{actualKindElement}"
                };

                var tbView = new TableView(PrCon, Pr.objId, SelGuid, actualKindElement, laTableViewAnchorable);
                laTableViewAnchorable.Closed += LaCtrl_Closed;
                laTableViewAnchorable.Content = tbView;

                var middlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePan1 != null)
                {
                    middlePan1.Children.Add(laTableViewAnchorable);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(laTableViewAnchorable));
                }

                laTableViewAnchorable.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ChartView0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var laChartView = new LayoutAnchorable
                {
                    CanClose = true,
                    ContentId = $"ChartView;{SelGuid};{actualKindElement}"
                };

                var chartView = new ChartView(PrCon, Pr.objId, SelGuid, actualKindElement, laChartView);
                laChartView.Closed += LaCtrl_Closed;
                laChartView.Content = chartView;

                var middlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePan1 != null)
                {
                    middlePan1.Children.Add(laChartView);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(laChartView));
                }

                laChartView.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private void CommView0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var laCommunicationView = new LayoutAnchorable
                {
                    CanClose = true,
                    ContentId = $"CommView;{SelGuid};{actualKindElement}"
                };

                var commView = new CommunicationView(PrCon, Pr.objId, SelGuid, actualKindElement, laCommunicationView);
                laCommunicationView.Closed += LaCtrl_Closed;
                laCommunicationView.Content = commView;

                var middlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePan1 != null)
                {
                    middlePan1.Children.Add(laCommunicationView);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(laCommunicationView));
                }

                laCommunicationView.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private void Editor0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable laEdit = new LayoutAnchorable
                {
                    CanClose = true,
                    ContentId = actualKindElement == ElementKind.InFile
                        ? $"Editor;{((CusFile)tvMain.View.SelectedItem).FullName};{actualKindElement}"
                        : $"Editor;{SelGuid};{actualKindElement}"
                };

                Editor edit;
                if (actualKindElement == ElementKind.InFile)
                {
                    var selectedFile = (CusFile)tvMain.View.SelectedItem;
                    edit = new Editor(PrCon, Pr.objId, selectedFile.FullName, actualKindElement, laEdit);
                    laEdit.Title = Path.GetFileName(selectedFile.FullName);
                }
                else if (actualKindElement == ElementKind.ScriptFile)
                {
                    var file = PrCon.GetScriptFile(Pr.objId, SelGuid);
                    edit = new Editor(PrCon, Pr.objId, file.FilePath, actualKindElement, laEdit);
                    laEdit.Title = file.Name;
                }
                else
                {
                    return;
                }

                laEdit.Closed += LaCtrl_Closed;
                laEdit.Content = edit;

                var middlePane = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (middlePane != null)
                {
                    middlePane.Children.Add(laEdit);
                }
                else
                {
                    dockManager.Layout.RootPanel.Children.Add(new LayoutDocumentPane(laEdit));
                }

                laEdit.IsActive = true;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void LaCtrl_Closed(object sender, EventArgs e)
        {
            try
            {
                //Okno
                LayoutAnchorable win = (LayoutAnchorable)sender;
                win.Closed -= LaCtrl_Closed;

                //TableView
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
                else if (win.Content is Editor)
                {
                    Editor editor = (Editor)win.Content;
                    win.Content = null;
                }
                else if (win.Content is CommunicationView)
                {
                    CommunicationView commView = (CommunicationView)win.Content;
                    win.Content = null;
                    GC.Collect();
                }

                win = null;
                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void DriveConf0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DriverConfigurator dConf = new DriverConfigurator(PrCon.gConf, PrCon);
                dConf.ShowDialog();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void About0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                About about = new About();
                about.Show();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void Updates0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckVersion frVersion = new CheckVersion(PrCon);
                frVersion.Show();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void ShHelp0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(PrCon.HelpWebSite);
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                if (Pr != null)
                {
                    XmlLayoutSerializer serializer = new XmlLayoutSerializer(dockManager);
                    serializer.Serialize(io.Path.GetDirectoryName(PrCon.projectList.First().path) + "\\" + PrCon.LayoutFile);
                }

                lbPathProject.Content = string.Empty;
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
                    //Ta metoda zostala przeniesiona tu w konstruktorze wyrzucala blad
                    PrCon.openProjects(pathRun);
                    Pr = PrCon.projectList[0];
                    Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Pr.path);

                    CheckAccessForNodes();
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Exit0_Click(object sender, RoutedEventArgs e)
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

        private void Block_Click(object sender, RoutedEventArgs e)
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

        private void Unblock_Click(object sender, RoutedEventArgs e)
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

        private void MenuItem_DbReset_Click(object sender, RoutedEventArgs e)
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

        private void View_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                propManag.SelectedObject = e.NewValue;
                SelObj = e.NewValue;

                if (e.NewValue is Project)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxProject"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxProject"];
                    SelGuid = ((Project)e.NewValue).objId;
                    actualKindElement = ElementKind.Project;
                }
                else if (e.NewValue is WebServer)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxHttpServer"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxHttpServer"];
                    SelGuid = ((WebServer)e.NewValue).ObjId;
                    actualKindElement = ElementKind.HttpConfig;
                }
                else if (e.NewValue is DatabaseModel)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxDatabse"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxDatabse"];
                }
                else if (e.NewValue is CusFile)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

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
                else if (e.NewValue is ScriptsDriver)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxScripts"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxScripts"];
                    SelGuid = ((ScriptsDriver)e.NewValue).objId;
                    actualKindElement = ElementKind.Scripts;
                }
                else if (e.NewValue is ScriptFile)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxScriptFile"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxScriptFile"];
                    SelGuid = ((ScriptFile)e.NewValue).objId;
                    actualKindElement = ElementKind.ScriptFile;
                }
                else if (e.NewValue is InternalTagsDriver)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxInternalTags"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxInternalTags"];
                    SelGuid = ((InternalTagsDriver)e.NewValue).objId;
                    actualKindElement = ElementKind.InternalsTags;
                }
                else if (e.NewValue is InTag)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxIntTag"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxIntTag"];
                    SelGuid = ((InTag)e.NewValue).objId;
                    actualKindElement = ElementKind.IntTag;

                    if (e.NewValue != null)
                        ((INotifyPropertyChanged)e.NewValue).PropertyChanged += MainWindow_PropertyChanged;
                }
                else if (e.NewValue is Connection)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxConnection"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxConnection"];
                    SelGuid = ((Connection)e.NewValue).objId;
                    actualKindElement = ElementKind.Connection;
                }
                else if (e.NewValue is Device)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxDevice"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxDevice"];
                    SelGuid = ((Device)e.NewValue).objId;
                    actualKindElement = ElementKind.Device;
                }
                else if (e.NewValue is Tag)
                {
                    if (e.OldValue != null)
                        ((INotifyPropertyChanged)e.OldValue).PropertyChanged -= MainWindow_PropertyChanged;

                    ((ContextMenu)Resources["CtxTag"]).DataContext = _viewModel;
                    tvMain.View.ContextMenu = (ContextMenu)Resources["CtxTag"];
                    SelGuid = ((Tag)e.NewValue).objId;
                    actualKindElement = ElementKind.Tag;

                    if (e.NewValue != null)
                        ((INotifyPropertyChanged)e.NewValue).PropertyChanged += MainWindow_PropertyChanged;
                }

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void MainWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private void MenuItem_ShowDbFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string p = io.Path.GetDirectoryName(Pr.path) + io.Path.GetDirectoryName(PrCon.Database);
                if (io.Directory.Exists(p))
                    Process.Start(p);
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void MenuItem_ShowDatabse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable laTableView = new LayoutAnchorable();
                laTableView.CanClose = true;
                laTableView.ContentId = "Database";
                DbExplorer db = new DbExplorer(Pr);
                laTableView.Closed += LaCtrl_Closed;
                laTableView.Content = db;

                var MiddlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().First();

                MiddlePan1.Children.Add(laTableView);
                laTableView.IsActive = true;
                laTableView.Title = "Database";

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void MenuItem_ShowTrendDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LayoutAnchorable laTableView = new LayoutAnchorable();
                laTableView.CanClose = true;
                laTableView.ContentId = "TrendDatabase";
                ChartViewDatabase chart = new ChartViewDatabase(Pr);
                laTableView.Closed += LaCtrl_Closed;
                laTableView.Content = chart;

                var MiddlePan1 = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().First();

                MiddlePan1.Children.Add(laTableView);
                laTableView.IsActive = true;
                laTableView.Title = "Chart Database";

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void MenuItem_SaveDatabeCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                var tags = Pr.Db.GetAll();

                // Write header
                sb.AppendLine("Name,Stamp,Value");

                // Write data rows
                foreach (var tag in tags)
                {
                    string line = $"{tag.Name},{tag.Stamp:yyyy-MM-dd HH:mm:ss.fff},{tag.Value.ToString().Replace(',', '.')}";
                    sb.AppendLine(line);
                }

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "CSV files (*.csv)|*.csv";
                if (sfd.ShowDialog() == true)
                    io.File.WriteAllText(sfd.FileName, sb.ToString());
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        public override string ToString()
        {
            return "Fenix Manager";
        }

        #endregion Internal Events

    }
}