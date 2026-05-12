using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using Microsoft.Win32;
using ProjectDataLib;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Fenix
{
    public partial class MainWindow : Window
    {
        private void AddProjectEvent(object sender, ProjectEventArgs ev)
        {
            try
            {
                Project pr = (Project)ev.element;
                this.Pr = pr;

                #region Sprawdzenie czy istnieje zapisany layout

                if (File.Exists(Path.GetDirectoryName(PrCon.projectList.First().path) + "\\" + PrCon.LayoutFile))
                {
                    Project pp = (Project)sender;
                    XmlLayoutSerializer serializer = new XmlLayoutSerializer(dockManager);

                    serializer.LayoutSerializationCallback += (s, args) =>
                    {
                        if (string.IsNullOrEmpty(args.Model.ContentId))
                            return;

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

                            case "TableDatabase":
                                LayoutAnchorable laDatabase = (LayoutAnchorable)args.Model;
                                laDatabase.CanClose = true;
                                DBTableView dbView = new DBTableView(pr);
                                laDatabase.Closed += EditorLayoutElement_Closed;
                                args.Content = dbView;
                                break;

                            case "TrendDatabase":
                                LayoutAnchorable laTrendDb = (LayoutAnchorable)args.Model;
                                laTrendDb.CanClose = true;
                                DBChartView trendDbView = new DBChartView(pr);
                                laTrendDb.Closed += EditorLayoutElement_Closed;
                                args.Content = trendDbView;
                                break;

                            case "TableView":
                                LayoutAnchorable laTableView = (LayoutAnchorable)args.Model;
                                laTableView.CanClose = true;
                                TableView tbView = new TableView(PrCon, pp.objId, Guid.Parse(param[1]), (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laTableView);
                                laTableView.Closed += EditorLayoutElement_Closed;
                                args.Content = tbView;
                                break;

                            case "TableViewRO":
                                LayoutAnchorable laTableViewRO = (LayoutAnchorable)args.Model;
                                laTableViewRO.CanClose = true;
                                TableViewRO tbViewRO = new TableViewRO(PrCon, pp.objId, Guid.Parse(param[1]), (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laTableViewRO);
                                laTableViewRO.Closed += EditorLayoutElement_Closed;
                                args.Content = tbViewRO;
                                break;

                            case "ChartView":
                                LayoutAnchorable laChartView = (LayoutAnchorable)args.Model;
                                laChartView.CanClose = true;
                                ChartView chView = new ChartView(PrCon, pp.objId, Guid.Parse(param[1]), (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laChartView);
                                laChartView.Closed += EditorLayoutElement_Closed;
                                args.Content = chView;
                                break;

                            case "CommView":
                                LayoutAnchorable laCommView = (LayoutAnchorable)args.Model;
                                laCommView.CanClose = true;
                                CommunicationView comView = new CommunicationView(PrCon, pp.objId, Guid.Parse(param[1]), (ElementKind)Enum.Parse(typeof(ElementKind), param[2]), laCommView);
                                laCommView.Closed += EditorLayoutElement_Closed;
                                args.Content = comView;
                                break;

                            case "Editor":
                                LayoutAnchorable laEditorView = (LayoutAnchorable)args.Model;
                                laEditorView.CanClose = true;

                                ElementKind editorKind = (ElementKind)Enum.Parse(typeof(ElementKind), param[2]);
                                string editorPath = param[1];

                                // For ScriptFile the ContentId stores a GUID, resolve it to a file path
                                if (editorKind == ElementKind.ScriptFile && Guid.TryParse(param[1], out Guid scriptGuid))
                                {
                                    var scriptFile = PrCon.GetScriptFile(pp.objId, scriptGuid);
                                    editorPath = scriptFile?.FilePath ?? string.Empty;
                                }

                                if (File.Exists(editorPath))
                                {
                                    ScriptEditor edView = new ScriptEditor(PrCon, pp.objId, editorPath, editorKind, laEditorView);
                                    laEditorView.Closed += EditorLayoutElement_Closed;
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

                    string ss = Path.GetDirectoryName(PrCon.projectList.First().path) + "\\" + PrCon.LayoutFile;
                    serializer.Deserialize(ss);
                }

                #endregion Sprawdzenie czy istnieje zapisany layout

                tvMain.View.DataContext = ((ITreeViewModel)PrCon).Children;
                tvMain.View.ItemsSource = ((ITreeViewModel)PrCon).Children;

                TreeViewItem PrNode = FindTviFromObjectRecursive(tvMain.View, pr);
                if (PrNode != null) PrNode.IsSelected = true;

                lbPathProject.Text = Pr.path;
                Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Pr.path);

                CheckAccessForNodes();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
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

                LayoutAnchorable lpAnchor = dockManager.Layout.Descendents().OfType<LayoutAnchorable>().Where(x => x.ContentId == "Output").First();
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
    }
}