using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using AvalonDock.Layout;

namespace Fenix
{
    public partial class TableViewRO : UserControl, INotifyPropertyChanged
    {
        private int index;

        private ProjectContainer PrCon_;
        public ProjectContainer PrCon
        {
            get { return PrCon_; }
            set { PrCon_ = value; propChanged_?.Invoke(this, new PropertyChangedEventArgs(nameof(PrCon))); }
        }

        private Project Pr_;
        public Project Pr
        {
            get { return Pr_; }
            set { Pr_ = value; propChanged_?.Invoke(this, new PropertyChangedEventArgs(nameof(Pr))); }
        }

        private Connection Con_;
        public Connection Con
        {
            get { return Con_; }
            set { Con_ = value; }
        }

        private Device Dev_;
        public Device Dev
        {
            get { return Dev_; }
            set { Dev_ = value; }
        }

        private PropertyChangedEventHandler propChanged_;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged_ += value; }
            remove { propChanged_ -= value; }
        }

        private ObservableCollection<ITag> ITagList;
        private LayoutAnchorable Win;
        private ElementKind elKind;
        private Guid Sel;

        public TableViewRO(ProjectContainer prCon, Guid pr, Guid sel, ElementKind elkind, LayoutAnchorable win)
        {
            try
            {
                InitializeComponent();

                PrCon = prCon;
                Pr = PrCon.projectList.First();
                Sel = sel;
                elKind = elkind;
                Win = win;

                View.DataContext = this;
                ((System.Windows.FrameworkElement)Resources["ProxyElement"]).DataContext = this;

                if (elKind == ElementKind.Project)
                {
                    Win.Title = "[RO] " + Pr.projectName;
                    ITagList = ((ITableView)Pr).Children;
                }
                else if (elKind == ElementKind.Connection)
                {
                    if (Pr.connectionList.Exists(x => x.objId == Sel))
                    {
                        Con = PrCon.getConnection(Pr.objId, Sel);
                        Win.Title = "[RO] " + Pr.projectName + "." + Con.connectionName;
                        ITagList = ((ITableView)Con).Children;
                        ((INotifyPropertyChanged)Con).PropertyChanged += Pr_Conn_Dev_PropChanged;
                        ((ITreeViewModel)Con).Children.CollectionChanged += Dev_CollectionChanged;
                    }
                    else { Win.Close(); }
                }
                else if (elKind == ElementKind.Device)
                {
                    if (Pr.DevicesList.Exists(x => x.objId == Sel))
                    {
                        Dev = PrCon.getDevice(Pr.objId, Sel);
                        Con = PrCon.getConnection(Pr.objId, Dev.parentId);
                        Win.Title = "[RO] " + Pr.projectName + "." + Con.connectionName + "." + Dev.name;
                        ITagList = ((ITableView)Dev).Children;
                        ((INotifyPropertyChanged)Con).PropertyChanged += Pr_Conn_Dev_PropChanged;
                        ((INotifyPropertyChanged)Dev).PropertyChanged += Pr_Conn_Dev_PropChanged;
                    }
                    else { Win.Close(); }
                }

                Win.Closing += Win_Closing;
                ((INotifyPropertyChanged)Pr).PropertyChanged += Pr_Conn_Dev_PropChanged;
                ((ITreeViewModel)Pr).Children.CollectionChanged += ProjectChildrenChanged;

                index = PrCon.winManagment.Count;
                PrCon.winManagment.Add(new WindowsStatus(index, true, false));

                View.ItemsSource = ITagList;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ProjectChildrenChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove) return;

                if (e.OldItems[0] is Connection cn)
                {
                    if (elKind == ElementKind.Connection && cn.objId == Sel)
                    {
                        if (Con != null) ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                        Win.Close();
                    }
                    else if (elKind == ElementKind.Device && Dev != null && cn.objId == Dev.parentId)
                    {
                        if (Con != null) ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                        if (Dev != null) ((INotifyPropertyChanged)Dev).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                        Win.Close();
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Dev_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove) return;

                if (e.OldItems[0] is Device dev && elKind == ElementKind.Connection && Dev != null && Dev.objId == dev.objId)
                    ((INotifyPropertyChanged)Dev).PropertyChanged -= Pr_Conn_Dev_PropChanged;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Pr_Conn_Dev_PropChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName != "Name" && e.PropertyName != "name") return;

                if (elKind == ElementKind.Project)
                {
                    Win.Title = "[RO] " + Pr.projectName;
                }
                else if (elKind == ElementKind.Connection)
                {
                    Connection cn = PrCon.getConnection(Pr.objId, Sel);
                    if (cn != null && Pr != null)
                        Win.Title = "[RO] " + Pr.projectName + "." + cn.connectionName;
                }
                else if (elKind == ElementKind.Device)
                {
                    Device dev = PrCon.getDevice(Pr.objId, Sel);
                    Connection cn = PrCon.getConnection(Pr.objId, dev.parentId);
                    if (cn != null && Pr != null && dev != null)
                        Win.Title = "[RO] " + Pr.projectName + "." + cn.connectionName + "." + dev.name;
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Win_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (elKind == ElementKind.Connection && Con != null)
                {
                    ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                    ((ITreeViewModel)Con).Children.CollectionChanged -= Dev_CollectionChanged;
                }
                else if (elKind == ElementKind.Device)
                {
                    if (Con != null) ((INotifyPropertyChanged)Con).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                    if (Dev != null) ((INotifyPropertyChanged)Dev).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                }

                ((INotifyPropertyChanged)Pr).PropertyChanged -= Pr_Conn_Dev_PropChanged;
                ((ITreeViewModel)Pr).Children.CollectionChanged -= ProjectChildrenChanged;
                Win.Closing -= Win_Closing;

                PrCon.winManagment.RemoveAll(x => x.index == index);
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }
    }
}
