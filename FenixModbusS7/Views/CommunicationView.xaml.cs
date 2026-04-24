using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AvalonDock.Layout;
using io = System.IO;
using wf = System.Windows.Forms;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for CommunicationView.xaml
    /// </summary>
    public partial class CommunicationView : UserControl, INotifyPropertyChanged
    {
        //Field
        private ProjectContainer projectContainer;

        public Project project { get; set; }
        private Guid selectedElementId;
        private ElementKind elementKind;
        private LayoutAnchorable layoutAnchorableWindow;
        private ObservableCollection<IDriverModel> DriverList = new ObservableCollection<IDriverModel>();
        private ObservableCollection<EventElement> Stack = new ObservableCollection<EventElement>();

        private int index;

        private PropertyChangedEventHandler propChanged_;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                propChanged_ += value;
            }

            remove
            {
                propChanged_ -= value;
            }
        }

        private bool mScroll_ = true;

        public Boolean mScroll
        { get { return mScroll_; } set { mScroll_ = value; propChanged_?.Invoke(this, new PropertyChangedEventArgs("mScroll")); } }

        //Konstuktor
        public CommunicationView(ProjectContainer prCon, Guid pr, Guid sel, ElementKind elKind, LayoutAnchorable win)
        {
            InitializeComponent();

            //zmienne
            projectContainer = prCon;
            project = prCon.projectList.First();
            selectedElementId = sel;
            elementKind = elKind;
            layoutAnchorableWindow = win;

            //dataselection
            if (elKind == ElementKind.Project)
            {
                //Title
                layoutAnchorableWindow.Title = project.projectName;

                DriverList = ((IDriversMagazine)project).Children;

                //Zdarzenia
                ((INotifyPropertyChanged)project).PropertyChanged += CommunicationView_PropertyChanged;
            }
            else if (elKind == ElementKind.Connection)
            {
                if (project.connectionList.Exists(x => x.objId == selectedElementId))
                {
                    //Typy
                    Connection cn = projectContainer.getConnection(project.objId, selectedElementId);

                    DriverList = ((IDriversMagazine)cn).Children;

                    //Title
                    layoutAnchorableWindow.Title = project.projectName + "." + cn.connectionName;

                    //Zmiana tytulu
                    ((INotifyPropertyChanged)project).PropertyChanged += CommunicationView_PropertyChanged;
                    ((INotifyPropertyChanged)cn).PropertyChanged += CommunicationView_PropertyChanged;
                }
                else
                {
                    layoutAnchorableWindow.Close();
                }
            }
            else if (elKind == ElementKind.Device)
            {
                if (project.DevicesList.Exists(x => x.objId == selectedElementId))
                {
                    //Typy bazowe
                    Device dev = projectContainer.getDevice(project.objId, selectedElementId);
                    Connection cn = projectContainer.getConnection(project.objId, dev.parentId);

                    DriverList = ((IDriversMagazine)dev).Children;

                    //Title
                    layoutAnchorableWindow.Title = project.projectName + "." + cn.connectionName + "." + dev.name;

                    //Zdarzenie
                    ((INotifyPropertyChanged)project).PropertyChanged += CommunicationView_PropertyChanged;
                    ((INotifyPropertyChanged)cn).PropertyChanged += CommunicationView_PropertyChanged;
                    ((INotifyPropertyChanged)dev).PropertyChanged += CommunicationView_PropertyChanged;
                }
                else
                {
                    layoutAnchorableWindow.Close();
                }
            }

            //Drivery
            foreach (IDriverModel idrv in DriverList)
            {
                idrv.dataRecived += new EventHandler(idrv_reciveLogInfo);
                idrv.dataSent += new EventHandler(idrv_sendLogInfo);
                idrv.error += new EventHandler(idrv_errorRecived);
                idrv.information += new EventHandler(idrv_informationRecived);
            }

            //Obserwacja stacku
            DataContext = this;
            View.DataContext = this;
            ((FrameworkElement)Resources["ProxyElement"]).DataContext = this;
            View.ItemsSource = Stack;

            //Zdarzenia
            layoutAnchorableWindow.Closing += Win_Closing;

            //Dodanie indeksu okna do globalnego buffora informacji o stanie okien
            index = projectContainer.winManagment.Count;
            projectContainer.winManagment.Add(new WindowsStatus(index, true, false));

            //zmiana kolkcji
            Stack.CollectionChanged += Stack_CollectionChanged;
            DriverList.CollectionChanged += DriverList_CollectionChanged;
        }

        //Zmiana listy driverow
        private void DriverList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    IDriverModel idrv = ((IDriverModel)e.OldItems[0]);
                    idrv.dataRecived -= new EventHandler(idrv_reciveLogInfo);
                    idrv.dataSent -= new EventHandler(idrv_sendLogInfo);
                    idrv.error -= new EventHandler(idrv_errorRecived);
                    idrv.information -= new EventHandler(idrv_informationRecived);
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    IDriverModel idrv = ((IDriverModel)e.NewItems[0]);
                    idrv.dataRecived += new EventHandler(idrv_reciveLogInfo);
                    idrv.dataSent += new EventHandler(idrv_sendLogInfo);
                    idrv.error += new EventHandler(idrv_errorRecived);
                    idrv.information += new EventHandler(idrv_informationRecived);
                }
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Title data
        private void CommunicationView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                //dataselection
                if (elementKind == ElementKind.Project)
                {
                    layoutAnchorableWindow.Title = project.projectName;
                }
                else if (elementKind == ElementKind.Connection)
                {
                    //Typy
                    Connection cn = projectContainer.getConnection(project.objId, selectedElementId);

                    //Title
                    layoutAnchorableWindow.Title = project.projectName + "." + cn.connectionName;
                }
                else if (elementKind == ElementKind.Device)
                {
                    //Typy bazowe
                    Device dev = projectContainer.getDevice(project.objId, selectedElementId);
                    Connection cn = projectContainer.getConnection(project.objId, dev.parentId);

                    //Title
                    layoutAnchorableWindow.Title = project.projectName + "." + cn.connectionName + "." + dev.name;
                }
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //Scroll Button
        private void Stack_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (View.Items.Count > 0 && mScroll)
            {
                var border = VisualTreeHelper.GetChild(View, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null) scroll.ScrollToEnd();
                }
            }
        }

        //Zamykanie okna
        private void Win_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                //Zdarzenia
                layoutAnchorableWindow.Closing -= Win_Closing;

                if (elementKind == ElementKind.Project)
                {
                    ((INotifyPropertyChanged)project).PropertyChanged -= CommunicationView_PropertyChanged;
                }
                else if (elementKind == ElementKind.Connection)
                {
                    //Typy
                    Connection cn = projectContainer.getConnection(project.objId, selectedElementId);

                    //Zmiana tytulu
                    ((INotifyPropertyChanged)project).PropertyChanged -= CommunicationView_PropertyChanged;
                    ((INotifyPropertyChanged)cn).PropertyChanged -= CommunicationView_PropertyChanged;
                }
                else if (elementKind == ElementKind.Device)
                {
                    //Typy bazowe
                    Device dev = projectContainer.getDevice(project.objId, selectedElementId);
                    Connection cn = projectContainer.getConnection(project.objId, dev.parentId);

                    //Zdarzenie
                    ((INotifyPropertyChanged)project).PropertyChanged -= CommunicationView_PropertyChanged;
                    ((INotifyPropertyChanged)cn).PropertyChanged -= CommunicationView_PropertyChanged;
                    ((INotifyPropertyChanged)dev).PropertyChanged -= CommunicationView_PropertyChanged;
                }

                DriverList.CollectionChanged -= DriverList_CollectionChanged;

                //Informacja o stanie okna
                projectContainer.winManagment.RemoveAll(x => x.index == index);
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// Wyslanie danych przez sterownik
        private void idrv_sendLogInfo(object sender1, EventArgs e1)
        {
            //Delegat
            EventHandler crossDef = delegate (object sender, EventArgs e)
            {
                //Projekt Event
                ProjectEventArgs ev = (ProjectEventArgs)e;

                IDriverModel idrv = (IDriverModel)sender;
                Connection cn = null;
                if (project.connectionList.Exists(x => x.Idrv.ObjId == idrv.ObjId))
                    cn = project.connectionList.Find(x => x.Idrv.ObjId == idrv.ObjId);

                //Dane
                byte[] data = (byte[])ev.element;

                //Czas
                DateTime time = (DateTime)ev.element0;

                //Informacja
                String description = (String)ev.element1;

                EventElement lsEl = null;
                if (Stack.Count > 0)
                    lsEl = Stack.Last();

                Stack.Add(new EventElement(project, sender, data, time, description, EventType.OUT, lsEl, cn?.connectionName ?? "No Name"));
            };

            try
            {
                //Wywolanie
                this.Dispatcher?.Invoke(crossDef, sender1, e1);
            }
            catch (Exception Ex)
            {
                if (projectContainer.ApplicationError != null)
                    projectContainer.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        // Odebranie danych przez sterownik
        private void idrv_reciveLogInfo(object sender1, EventArgs e1)
        {
            EventHandler crossDef = delegate (object sender, EventArgs e)
            {
                ProjectEventArgs ev = (ProjectEventArgs)e;

                IDriverModel idrv = (IDriverModel)sender;
                Connection cn = null;
                if (project.connectionList.Exists(x => x.Idrv.ObjId == idrv.ObjId))
                    cn = project.connectionList.Find(x => x.Idrv.ObjId == idrv.ObjId);

                //Dane
                byte[] data = (byte[])ev.element;

                //Czas
                DateTime time = (DateTime)ev.element0;

                //Informacja
                String description = (String)ev.element1;

                EventElement lsEl = null;
                if (Stack.Count > 0)
                    lsEl = Stack.Last();

                Stack.Add(new EventElement(project, sender, data, time, description, EventType.IN, lsEl, cn?.connectionName ?? "No Name"));
            };

            try
            {
                this.Dispatcher?.Invoke(crossDef, sender1, e1);
            }
            catch (Exception Ex)
            {
                if (projectContainer.ApplicationError != null)
                    projectContainer.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        //Informacja
        private void idrv_informationRecived(object sender, EventArgs e)
        {
            EventHandler lacznik = delegate (object se, EventArgs e1)
            {
                ProjectEventArgs ev = (ProjectEventArgs)e1;

                IDriverModel idrv = (IDriverModel)sender;
                Connection cn = null;
                if (project.connectionList.Exists(x => x.Idrv.ObjId == idrv.ObjId))
                    cn = project.connectionList.Find(x => x.Idrv.ObjId == idrv.ObjId);

                //Dane
                byte[] data = (byte[])ev.element;

                //Czas
                DateTime time = (DateTime)ev.element0;

                //Informacja
                String description = (String)ev.element1;

                EventElement lsEl = null;
                if (Stack.Count > 0)
                    lsEl = Stack.Last();

                //Stack add
                Stack.Add(new EventElement(project, se, data, time, description, EventType.INFO, lsEl, cn?.connectionName ?? "No Name"));
            };

            View.Dispatcher?.Invoke(lacznik, sender, e);
        }

        // Error
        private void idrv_errorRecived(object sender, EventArgs e)
        {
            EventHandler lacznik = delegate (object se, EventArgs e1)
            {
                ProjectEventArgs ev = (ProjectEventArgs)e1;

                IDriverModel idrv = (IDriverModel)sender;
                Connection cn = null;
                if (project.connectionList.Exists(x => x.Idrv.ObjId == idrv.ObjId))
                    cn = project.connectionList.Find(x => x.Idrv.ObjId == idrv.ObjId);

                //Dane
                byte[] data = (byte[])ev.element;

                //Czas
                DateTime time = (DateTime)ev.element0;

                //Informacja
                String description = (String)ev.element1;

                EventElement lsEl = null;
                if (Stack.Count > 0)
                    lsEl = Stack.Last();

                Stack.Add(new EventElement(project, se, data, time, description, EventType.ERROR, lsEl, cn?.connectionName ?? "No Name"));
            };

            View.Dispatcher?.Invoke(lacznik, sender, e);
        }

        //Wyczysc Log
        private void Button_Clr_Click(object sender, RoutedEventArgs e)
        {
            Stack.Clear();
        }

        //Save CSV
        private void SaveCSV(object sender, RoutedEventArgs e)
        {
            wf.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "*.CSV | *.csv";

            if (sfd.ShowDialog() == wf.DialogResult.OK)
            {
                //View.SelectAllCells();
                View.SelectAll();
                View.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                ApplicationCommands.Copy.Execute(null, View);
                View.UnselectAll();
                String result = (string)Clipboard.GetData(DataFormats.CommaSeparatedValue);

                //Zapisanie danych
                io.File.WriteAllText(sfd.FileName, result);
                Clipboard.Clear();
            }
        }

        private void Clipbord(object sender, RoutedEventArgs e)
        {
            //View.SelectAllCells();
            View.SelectAll();
            View.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            ApplicationCommands.Copy.Execute(null, View);
            View.UnselectAll();
        }
    }

    public class DateTimeFormConverter : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime tm = (DateTime)values[0];
            Project pr = (Project)values[1];
            return tm.ToString(pr.longDT);
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue };
        }
    }

    public class DataBytes : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            IDriverModel idrv = (IDriverModel)values[0];
            byte[] data = (byte[])values[1];
            string info = (string)values[2];
            EventType Type = (EventType)values[3];

            if (Type == EventType.OUT)
                return idrv.FormatFrameRequest(data, NumberStyles.HexNumber);
            else if (Type == EventType.IN)
                return idrv.FormatFrameResponse(data, NumberStyles.HexNumber);
            else
            {
                return "INTERNAL PROBLEM";
            }
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue };
        }
    }

    public class ColorConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EventElement ev = (EventElement)value;

            switch (ev.Type)
            {
                case EventType.ERROR:
                    return new SolidColorBrush(Colors.Red);
                case EventType.INFO:
                    return new SolidColorBrush(Colors.Yellow);
                case EventType.IN:
                    return new SolidColorBrush(Colors.LightCyan);
                case EventType.OUT:
                    return new SolidColorBrush(Colors.White);
                default:
                    return new SolidColorBrush(Colors.White);
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class TagsConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EventElement ev = (EventElement)value;

            string s = "";

            var tgs = from t in ev.Pr.tagsList where t.idrv.ObjId == ((IDriverModel)ev.Sender).ObjId select t;
            foreach (ITag tg in tgs)
                s = s + string.Format("{0}: {1}  ;", tg.Name, tg.GetFormatedValue());

            return s;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class BoolToVis : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return Visibility.Hidden;
            else
                return Visibility.Visible;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}