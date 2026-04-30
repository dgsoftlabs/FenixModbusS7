using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using OxyPlot.Series;
using ProjectDataLib;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Layout;

namespace Fenix
{
    public partial class ChartView : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private int index;
        private TimeSpan defaultTrackSpan;
        private bool isTrackSpanFilterActive;

        private ProjectContainer PrCon_;

        public ProjectContainer PrCon
        {
            get { return PrCon_; }
            set
            {
                PrCon_ = value;
                propChanged_?.Invoke(this, new PropertyChangedEventArgs("PrCon"));
            }
        }

        private Project Pr_;

        public Project Pr
        {
            get { return Pr_; }
            set
            {
                Pr_ = value;
                propChanged_?.Invoke(this, new PropertyChangedEventArgs("Pr"));
            }
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

        private ObservableCollection<ITag> ITagList;
        private ObservableCollection<IDriverModel> IDriverList;
        private LayoutAnchorable Win;
        private ElementKind ElKind;
        private Guid Sel;

        private PropertyChangedEventHandler propChanged_;

        public event PropertyChangedEventHandler PropertyChanged;

        public PlotModel plotModel { get; private set; }

        public LinearAxis AxY1 { get; set; }

        public LinearAxis AxY2 { get; set; }

        public DateTimeAxis AxX1 { get; set; }

        public ChartView(ProjectContainer prCon, Guid projId, Guid sel, ElementKind elkind, LayoutAnchorable win)
        {
            try
            {
                // Project container
                PrCon = prCon;
                Pr = PrCon.getProject(projId);
                Win = win;
                ElKind = elkind;
                Sel = sel;
                defaultTrackSpan = Pr.ChartConf.TrackSpan;

                InitializeComponent();

                TrackSpanInput.TextChanged += TrackSpanInput_TextChanged;

                // Object selection
                if (ElKind == ElementKind.Project)
                {
                    ITagList = ((ITableView)Pr).Children;
                    IDriverList = ((IDriversMagazine)Pr).Children;

                    // Collection
                    ((ITableView)Pr).Children.CollectionChanged += ITagList_CollectionChanged;
                    ((ITreeViewModel)Pr).Children.CollectionChanged += Project_ChildrenChanged;
                    ((IDriversMagazine)Pr).Children.CollectionChanged += IDriver_CollectionChanged;

                    // Properties
                    ((INotifyPropertyChanged)Pr).PropertyChanged += Project_PropertyChanged;
                    ((INotifyPropertyChanged)Pr.ChartConf).PropertyChanged += ChartConf_PropertyChanged;

                    foreach (Connection c in Pr.connectionList)
                        ((INotifyPropertyChanged)c).PropertyChanged += Connection_PropertyChanged;

                    foreach (Device d in Pr.DevicesList)
                        ((INotifyPropertyChanged)d).PropertyChanged += Device_PropertyChanged;

                    foreach (ITag t in ((ITableView)Pr).Children)
                        ((INotifyPropertyChanged)t).PropertyChanged += ITag_PropertyChanged;

                    Win.Closing += Win_Closing;

                    foreach (IDriverModel idr in IDriverList)
                        idr.refreshedCycle += driverRefreshed;
                }
                else if (ElKind == ElementKind.Connection)
                {
                    if (Pr.connectionList.Exists(x => x.objId == Sel))
                    {
                        Con = PrCon.getConnection(Pr.objId, Sel);

                        ITagList = ((ITableView)Con).Children;
                        IDriverList = ((IDriversMagazine)Con).Children;

                        ((ITableView)Pr).Children.CollectionChanged += ITagList_CollectionChanged;
                        ((ITreeViewModel)Pr).Children.CollectionChanged += Project_ChildrenChanged;
                        ((ITreeViewModel)Con).Children.CollectionChanged += Device_CollectionChanged;
                        ((IDriversMagazine)Con).Children.CollectionChanged += IDriver_CollectionChanged;

                        ((INotifyPropertyChanged)Pr).PropertyChanged += Project_PropertyChanged;
                        ((INotifyPropertyChanged)Pr.ChartConf).PropertyChanged += ChartConf_PropertyChanged;

                        ((INotifyPropertyChanged)Con).PropertyChanged += Connection_PropertyChanged;

                        foreach (Device d in ((ITreeViewModel)Con).Children)
                            ((INotifyPropertyChanged)d).PropertyChanged += Device_PropertyChanged;

                        foreach (ITag t in ((ITableView)Con).Children)
                            ((INotifyPropertyChanged)t).PropertyChanged += ITag_PropertyChanged;

                        Win.Closing += Win_Closing;

                        foreach (IDriverModel idr in IDriverList)
                            idr.refreshedCycle += driverRefreshed;
                    }
                    else
                    {
                        Win.Close();
                    }
                }
                else if (ElKind == ElementKind.Device)
                {
                    if (Pr.DevicesList.Exists(x => x.objId == Sel))
                    {
                        Dev = PrCon.getDevice(Pr.objId, Sel);
                        Con = PrCon.getConnection(Pr.objId, Dev.parentId);

                        // Data
                        ITagList = ((ITableView)Dev).Children;
                        IDriverList = ((IDriversMagazine)Dev).Children;

                        // Collection

                        ((ITableView)Dev).Children.CollectionChanged += ITagList_CollectionChanged;
                        ((ITreeViewModel)Pr).Children.CollectionChanged += Project_ChildrenChanged;
                        ((ITreeViewModel)Con).Children.CollectionChanged += Device_CollectionChanged;
                        ((IDriversMagazine)Dev).Children.CollectionChanged += IDriver_CollectionChanged;

                        // Properties

                        ((INotifyPropertyChanged)Pr).PropertyChanged += Project_PropertyChanged;
                        ((INotifyPropertyChanged)Pr.ChartConf).PropertyChanged += ChartConf_PropertyChanged;
                        ((INotifyPropertyChanged)Con).PropertyChanged += Connection_PropertyChanged;
                        ((INotifyPropertyChanged)Dev).PropertyChanged += Device_PropertyChanged;

                        foreach (ITag t in ((ITableView)Con).Children)
                            ((INotifyPropertyChanged)t).PropertyChanged += ITag_PropertyChanged;

                        Win.Closing += Win_Closing;

                        foreach (IDriverModel idr in IDriverList)
                            idr.refreshedCycle += driverRefreshed;
                    }
                    else
                    {
                        Win.Close();
                    }
                }

                // Axes
                AxX1 = new DateTimeAxis { Position = AxisPosition.Bottom, MajorGridlineStyle = LineStyle.Dash };
                AxX1.StringFormat = "HH:mm:ss";

                // Chart
                plotModel = new PlotModel { };
                plotModel.Axes.Add(AxX1);

                RebuildYAxes();

                View.Model = plotModel;

                // Tag iteration
                foreach (ITag t in ITagList.Where(x => x.GrEnable))
                {
                    if (t.GrEnable)
                    {
                        // Add series
                        var s1 = new LineSeries
                        {
                            Title = t.Name,
                            TrackerFormatString = "{0}" + Environment.NewLine + "Y: {4:0.000}" + Environment.NewLine + "X: {2:" + Pr.longDT + "}",
                            Color = OxyColor.FromRgb(t.Clr.R, t.Clr.G, t.Clr.B),
                            StrokeThickness = t.Width,
                            IsVisible = t.GrVisible,
                            YAxisKey = ResolveAxisKey(t.GrAxisKey)
                        };

                        s1.Tag = t.Id;
                        plotModel.Series.Add(s1);
                    }
                }

                plotModel.InvalidatePlot(true);

                RefreshObservablePath();

                index = PrCon.winManagment.Count;
                PrCon.winManagment.Add(new WindowsStatus(index, true, false));

                //Konteksty
                DataContext = this;
                UpdateFilterStates();

                // Set interaction description
                InteractionDescription = "Left Click + Drag: Pan | Ctrl + Right Click + Drag: Zoom Rectangle | Mouse Wheel: Zoom | C: Copy to Clipboard";

            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private string _interactionDescription;
        public string InteractionDescription
        {
            get => _interactionDescription;
            set
            {
                if (_interactionDescription != value)
                {
                    _interactionDescription = value;
                    OnPropertyChanged();
                    UpdateStatusText();
                }
            }
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateStatusText()
        {
            int totalPoints = plotModel?.Series.OfType<LineSeries>().Sum(s => s.Points.Count) ?? 0;
            bool paused = Pr?.ChartConf?.histData ?? false;
            string pauseState = paused ? " | ⏸ PAUSED" : string.Empty;
            StatusText = $"{_interactionDescription} | Points: {totalPoints}{pauseState}";
        }

        public bool IsTrackSpanFilterActive
        {
            get => isTrackSpanFilterActive;
            set
            {
                if (isTrackSpanFilterActive != value)
                {
                    isTrackSpanFilterActive = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateFilterStates()
        {
            IsTrackSpanFilterActive = Pr != null && Pr.ChartConf.TrackSpan != defaultTrackSpan;
        }

        private void Project_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Connection.connectionName))
                RefreshObservablePath();
        }

        private void Connection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Connection.connectionName))
                RefreshObservablePath();
        }

        private void Device_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Device.name))
                RefreshObservablePath();
        }

        private void ITag_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName != nameof(ITag.Value) && e.PropertyName != "IsLive" && e.PropertyName != "isAlive")
                {
                    View.Dispatcher.Invoke(new Action(() =>
                    {
                        ITag tg = (ITag)sender;

                        if (!tg.GrEnable)
                        {
                            // Series exists
                                    if (plotModel.Series.ToList().Exists(x => (Guid)x.Tag == tg.Id))
                                    {
                                        var ser = (LineSeries)(from x in plotModel.Series where x.Title == tg.Name select x).First();
                                        plotModel.Series.Remove(ser);
                                    }
                                    // Series does not exist
                                    else
                                        return;
                        }
                        else
                        {
                            // Check
                            if (!plotModel.Series.ToList().Exists(x => (Guid)x.Tag == tg.Id))
                            {
                                // Add series
                                var s1 = new LineSeries
                                {
                                    Title = tg.Name,
                                    TrackerFormatString = "{0}" + Environment.NewLine + "Y: {4:0.000}" + Environment.NewLine + "X: {2:" + Pr.longDT + "}",
                                    Color = OxyColor.FromRgb(tg.Clr.R, tg.Clr.G, tg.Clr.B),
                                    StrokeThickness = tg.Width,
                                    IsVisible = tg.GrVisible,
                                    YAxisKey = ResolveAxisKey(tg.GrAxisKey)
                                };

                                s1.Tag = tg.Id;
                                plotModel.Series.Add(s1);
                            }
                            else
                            {
                                var ser = (LineSeries)(from x in plotModel.Series where (Guid)x.Tag == tg.Id select x).First();
                                ser.Title = tg.Name;
                                ser.TrackerFormatString = "{0}" + Environment.NewLine + "Y: {4:0.000}" + Environment.NewLine + "X: {2:" + Pr.longDT + "}";
                                ser.Color = OxyColor.FromRgb(tg.Clr.R, tg.Clr.G, tg.Clr.B);
                                ser.StrokeThickness = tg.Width;
                                ser.IsVisible = tg.GrVisible;
                                ser.YAxisKey = ResolveAxisKey(tg.GrAxisKey);
                            }
                        }

                        plotModel.InvalidatePlot(true);
                    }));
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private string ResolveAxisKey(string grAxisKey)
        {
            var key = string.IsNullOrEmpty(grAxisKey) ? "Y1" : grAxisKey;
            return plotModel.Axes.OfType<LinearAxis>().Any(a => a.Key == key) ? key : "Y1";
        }

        private void RevalidateSeriesAxisKeys()
        {
            foreach (var series in plotModel.Series.OfType<LineSeries>())
                series.YAxisKey = ResolveAxisKey(series.YAxisKey);
        }

        private void RebuildYAxes()
        {
            // Unsubscribe old axis conf events
            if (Pr?.ChartConf?.Axes != null)
                foreach (var axConf in Pr.ChartConf.Axes)
                    ((INotifyPropertyChanged)axConf).PropertyChanged -= AxisConf_PropertyChanged;

            // Remove existing Y axes
            var toRemove = plotModel.Axes.OfType<LinearAxis>().ToList();
            foreach (var ax in toRemove)
                plotModel.Axes.Remove(ax);

            AxY1 = null;
            AxY2 = null;

            var axisList = Pr?.ChartConf?.Axes;
            if (axisList == null || axisList.Count == 0)
            {
                AxY1 = new LinearAxis { Key = "Y1", Position = AxisPosition.Left, MajorGridlineStyle = LineStyle.Dash };
                plotModel.Axes.Add(AxY1);
                return;
            }

            int leftTier = 0, rightTier = 0;
            foreach (var axConf in axisList)
            {
                var position = axConf.IsRight ? AxisPosition.Right : AxisPosition.Left;
                int tier = axConf.IsRight ? rightTier++ : leftTier++;

                var ax = new LinearAxis
                {
                    Key = axConf.Key,
                    Title = axConf.Title,
                    Position = position,
                    PositionTier = tier,
                    MajorGridlineStyle = LineStyle.Dash,
                    IsAxisVisible = axConf.IsVisible,
                    Minimum = double.IsNaN(axConf.Minimum) ? double.NaN : axConf.Minimum,
                    Maximum = double.IsNaN(axConf.Maximum) ? double.NaN : axConf.Maximum
                };
                plotModel.Axes.Add(ax);

                if (axConf.Key == "Y1") AxY1 = ax;
                if (axConf.Key == "Y2") AxY2 = ax;

                ((INotifyPropertyChanged)axConf).PropertyChanged += AxisConf_PropertyChanged;
            }

            if (AxY1 == null && plotModel.Axes.OfType<LinearAxis>().Any())
                AxY1 = plotModel.Axes.OfType<LinearAxis>().First();
        }

        private void AxisConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var axConf = (ChartAxisConf)sender;
            var ax = plotModel.Axes.OfType<LinearAxis>().FirstOrDefault(a => a.Key == axConf.Key);
            if (ax == null) return;

            switch (e.PropertyName)
            {
                case nameof(ChartAxisConf.Minimum): ax.Minimum = double.IsNaN(axConf.Minimum) ? double.NaN : axConf.Minimum; break;
                case nameof(ChartAxisConf.Maximum): ax.Maximum = double.IsNaN(axConf.Maximum) ? double.NaN : axConf.Maximum; break;
                case nameof(ChartAxisConf.Title):   ax.Title = axConf.Title; break;
                case nameof(ChartAxisConf.IsVisible): ax.IsAxisVisible = axConf.IsVisible; break;
                case nameof(ChartAxisConf.IsRight):
                    ax.Position = axConf.IsRight ? AxisPosition.Right : AxisPosition.Left;
                    break;
            }

            plotModel.InvalidatePlot(false);
        }

        private void ChartConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(ChartViewConf.histData))
                {
                    UpdateStatusText();
                }
                else if (e.PropertyName == nameof(ChartViewConf.TrackSpan))
                {
                    UpdateFilterStates();
                }
                else if (e.PropertyName == nameof(ChartViewConf.Axes))
                {
                    RebuildYAxes();
                    RevalidateSeriesAxisKeys();
                    plotModel.InvalidatePlot(true);
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Project_ChildrenChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    if (e.OldItems[0] is Connection)
                    {
                        Connection cn = (Connection)e.OldItems[0];
                        if (ElKind == ElementKind.Connection)
                        {
                            if (cn.objId == Sel)
                            {
                                if (Con != null)
                                    ((INotifyPropertyChanged)Con).PropertyChanged -= Connection_PropertyChanged;

                                Win.Close();
                            }
                        }
                        else if (ElKind == ElementKind.Device)
                        {
                            if (cn.objId == Dev.parentId)
                            {
                                if (Con != null)
                                    ((INotifyPropertyChanged)Con).PropertyChanged -= Connection_PropertyChanged;

                                if (Dev != null)
                                    ((INotifyPropertyChanged)Dev).PropertyChanged -= Device_PropertyChanged;

                                Win.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Device_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    if (e.OldItems[0] is Device)
                    {
                        Device dev = (Device)e.OldItems[0];
                        if (ElKind == ElementKind.Connection)
                        {
                            if (Dev != null)
                            {
                                if (Dev.objId == dev.objId)
                                    ((INotifyPropertyChanged)Dev).PropertyChanged -= Device_PropertyChanged;
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void ITagList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    ITag t = (ITag)e.NewItems[0];

                    if (t.GrEnable)
                    {
                        if (t as Tag != null)
                            ((Tag)t).propChanged += ITag_PropertyChanged;

                        if (t as InTag != null)
                            ((INotifyPropertyChanged)t).PropertyChanged += ITag_PropertyChanged;

                        // Add series
                            var s1 = new LineSeries
                            {
                                Title = t.Name,
                                TrackerFormatString = "{0}" + Environment.NewLine + "Y: {4:0.000}" + Environment.NewLine + "X: {2:" + Pr.longDT + "}",
                                Color = OxyColor.FromRgb(t.Clr.R, t.Clr.G, t.Clr.B),
                                StrokeThickness = t.Width,
                                IsVisible = t.GrVisible,
                                Tag = t.Id
                            };

                        plotModel.Series.Add(s1);
                    }

                    plotModel.InvalidatePlot(true);
                }
                else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    ITag t = (ITag)e.OldItems[0];

                    if (t as Tag != null)
                        ((Tag)t).propChanged -= ITag_PropertyChanged;

                    if (t as InTag != null)
                        ((INotifyPropertyChanged)t).PropertyChanged -= ITag_PropertyChanged;

                    if (plotModel.Series.ToList().Exists(x => x.Title == t.Name))
                    {
                        var ser = plotModel.Series.ToList().Find(x => x.Title == t.Name);
                        plotModel.Series.Remove(ser);

                        plotModel.InvalidatePlot(true);
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void IDriver_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                ((IDriverModel)e.OldItems[0]).refreshedCycle -= driverRefreshed;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                ((IDriverModel)e.NewItems[0]).refreshedCycle += driverRefreshed;
            }
        }

        private void RefreshObservablePath()
        {
            try
            {
                if (ElKind == ElementKind.Project)
                {
                    Win.Title = Pr.projectName;
                }
                else if (ElKind == ElementKind.Connection)
                {
                    Connection cn = PrCon.getConnection(Pr.objId, Sel);
                    if (cn != null && Pr != null)
                        Win.Title = Pr.projectName + "." + cn.connectionName;
                }
                else if (ElKind == ElementKind.Device)
                {
                    Device dev = PrCon.getDevice(Pr.objId, Sel);
                    Connection cn = PrCon.getConnection(Pr.objId, dev.parentId);
                    if (cn != null && Pr != null && dev != null)
                        Win.Title = Pr.projectName + "." + cn.connectionName + "." + dev.name;
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        public void driverRefreshed(object sender, EventArgs e)
        {
            try
            {
                View.Dispatcher.InvokeAsync(new Action(() =>
                {
                    if (Pr?.ChartConf?.histData == true)
                    {
                        UpdateStatusText();
                        return;
                    }

                    if (sender is ScriptsDriver || sender is InternalTagsDriver)
                    {
                        foreach (ITag tg in ITagList.Where(x => x.GrEnable && x is InTag))
                        {
                            if (plotModel.Series.ToList().Exists(x => x.Title == tg.Name))
                            {
                                Boolean lastPoint = false;
                                var ser = (LineSeries)(from x in plotModel.Series where x.Title == tg.Name select x).First();

                                if (tg.TypeData_ == TypeData.BIT)
                                {
                                    if (ser.Points.Count != 0)
                                        lastPoint = Convert.ToBoolean(ser.Points[ser.Points.Count - 1].Y);

                                    if (lastPoint != (bool)tg.Value)
                                    {
                                        var data = DateTime.Now;
                                        ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(lastPoint)));
                                        ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(tg.Value)));
                                    }
                                    else
                                    {
                                        ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(tg.Value)));
                                    }
                                }
                                else if (tg.TypeData_ == TypeData.CHAR)
                                {
                                    int pom = (int)Char.GetNumericValue((char)tg.Value);
                                    double val = Convert.ToDouble(pom);
                                    ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), val));
                                }
                                else
                                {
                                    ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(tg.Value)));
                                }
                            }
                        }

                        plotModel.InvalidatePlot(true);
                        UpdateStatusText();
                    }
                    else
                    {
                        #region Communication Driver

                        foreach (ITag tg in ITagList.Where(x => x.GrEnable && x is Tag && ((IDriverModel)sender).ObjId == ((IDriverModel)x).ObjId))
                        {
                            if (plotModel.Series.ToList().Exists(x => x.Title == tg.Name))
                            {
                                Boolean lastPoint = false;
                                var ser = (LineSeries)(from x in plotModel.Series where x.Title == tg.Name select x).First();

                                if (tg.TypeData_ == TypeData.BIT)
                                {
                                    if (ser.Points.Count != 0)
                                        lastPoint = Convert.ToBoolean(ser.Points[ser.Points.Count - 1].Y);

                                    if (lastPoint != (bool)tg.Value)
                                    {
                                        var data = DateTime.Now;
                                        ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(lastPoint)));
                                        ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(tg.Value)));
                                    }
                                    else
                                    {
                                        ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(tg.Value)));
                                    }
                                }
                                else if (tg.TypeData_ == TypeData.CHAR)
                                {
                                    int pom = (int)Char.GetNumericValue((char)tg.Value);
                                    double val = Convert.ToDouble(pom);
                                    ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), val));
                                }
                                else
                                {
                                    ser.Points.Add(new DataPoint(DateTime.Now.ToOADate(), Convert.ToDouble(tg.Value)));
                                }
                            }
                        }

                        plotModel.InvalidatePlot(true);
                        UpdateStatusText();

                        #endregion CommDriver
                    }

                    // Time range constraint
                    foreach (LineSeries sr in plotModel.Series)
                    {
                        if (sr.Points.Count > 0)
                        {
                            double min = sr.Points.First().X;
                            TimeSpan diff = DateTime.Now.Subtract(DateTime.FromOADate(min));
                            if (diff > Pr.ChartConf.TrackSpan)
                            {
                                while (diff > Pr.ChartConf.TrackSpan)
                                {
                                    diff = DateTime.Now.Subtract(DateTime.FromOADate(sr.Points.First().X));
                                    sr.Points.RemoveAt(0);
                                }
                            }
                        }
                    }
                }));
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void TrackSpanInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilterStates();
        }

        private void Button_ClearTrackSpanFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Pr.ChartConf.TrackSpan = defaultTrackSpan;
                UpdateFilterStates();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }



        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var ser in plotModel.Series)
                    ((LineSeries)ser).Points.Clear();

                plotModel.InvalidatePlot(true);
                UpdateStatusText();
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
                if (ElKind == ElementKind.Project)
                {
                    // Collection

                        ((ITableView)Pr).Children.CollectionChanged -= ITagList_CollectionChanged;
                        ((ITreeViewModel)Pr).Children.CollectionChanged -= Project_ChildrenChanged;
                        ((IDriversMagazine)Pr).Children.CollectionChanged -= IDriver_CollectionChanged;

                        // Properties

                        ((INotifyPropertyChanged)Pr).PropertyChanged -= Project_PropertyChanged;
                        ((INotifyPropertyChanged)Pr.ChartConf).PropertyChanged -= ChartConf_PropertyChanged;

                        foreach (Connection c in Pr.connectionList)
                            ((INotifyPropertyChanged)c).PropertyChanged -= Connection_PropertyChanged;

                        foreach (Device d in Pr.DevicesList)
                            ((INotifyPropertyChanged)d).PropertyChanged -= Device_PropertyChanged;

                        foreach (ITag t in ((ITableView)Pr).Children)
                            ((INotifyPropertyChanged)t).PropertyChanged -= ITag_PropertyChanged;

                        Win.Closing -= Win_Closing;

                        foreach (IDriverModel idr in IDriverList)
                            idr.refreshedCycle -= driverRefreshed;
                    }
                    else if (ElKind == ElementKind.Connection)
                    {
                        // Collection

                        ((ITableView)Pr).Children.CollectionChanged -= ITagList_CollectionChanged;
                        ((ITreeViewModel)Pr).Children.CollectionChanged -= Project_ChildrenChanged;
                        ((ITreeViewModel)Con).Children.CollectionChanged -= Device_CollectionChanged;
                        ((IDriversMagazine)Con).Children.CollectionChanged -= IDriver_CollectionChanged;

                        // Properties

                        ((INotifyPropertyChanged)Pr).PropertyChanged -= Project_PropertyChanged;
                        ((INotifyPropertyChanged)Pr.ChartConf).PropertyChanged -= ChartConf_PropertyChanged;

                        ((INotifyPropertyChanged)Con).PropertyChanged -= Connection_PropertyChanged;

                        foreach (Device d in ((ITreeViewModel)Con).Children)
                            ((INotifyPropertyChanged)d).PropertyChanged -= Device_PropertyChanged;

                        foreach (ITag t in ((ITableView)Con).Children)
                            ((INotifyPropertyChanged)t).PropertyChanged -= ITag_PropertyChanged;

                        Win.Closing -= Win_Closing;

                        foreach (IDriverModel idr in IDriverList)
                            idr.refreshedCycle -= driverRefreshed;
                    }
                    else if (ElKind == ElementKind.Device)
                    {
                        // Collection

                        ((ITableView)Dev).Children.CollectionChanged -= ITagList_CollectionChanged;
                        ((ITreeViewModel)Pr).Children.CollectionChanged -= Project_ChildrenChanged;
                        ((ITreeViewModel)Con).Children.CollectionChanged -= Device_CollectionChanged;
                        ((IDriversMagazine)Dev).Children.CollectionChanged -= IDriver_CollectionChanged;

                        // Properties

                        ((INotifyPropertyChanged)Pr).PropertyChanged -= Project_PropertyChanged;
                        ((INotifyPropertyChanged)Pr.ChartConf).PropertyChanged -= ChartConf_PropertyChanged;
                        ((INotifyPropertyChanged)Con).PropertyChanged -= Connection_PropertyChanged;
                        ((INotifyPropertyChanged)Dev).PropertyChanged -= Device_PropertyChanged;

                    foreach (ITag t in ((ITableView)Con).Children)
                        ((INotifyPropertyChanged)t).PropertyChanged -= ITag_PropertyChanged;

                    Win.Closing -= Win_Closing;

                    foreach (IDriverModel idr in IDriverList)
                        idr.refreshedCycle -= driverRefreshed;
                }

                // Unsubscribe per-axis property change handlers
                if (Pr?.ChartConf?.Axes != null)
                    foreach (var axConf in Pr.ChartConf.Axes)
                        ((INotifyPropertyChanged)axConf).PropertyChanged -= AxisConf_PropertyChanged;

                // Update window state information
                PrCon.winManagment.RemoveAll(x => x.index == index);

                // Cleanup
                propChanged_ = null;
                AxX1 = null;
                AxY1 = null;
                AxY2 = null;

                ITagList = null;
                IDriverList = null;

                Win = null;
                PrCon = null;
                Pr = null;
                Con = null;
                Dev = null;
                DataContext = null;
                View.DataContext = null;
                View = null;

                plotModel = null;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }



        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_PauseResume_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool nowPaused = !Pr.ChartConf.histData;
                Pr.ChartConf.histData = nowPaused;
                var btn = (Button)sender;
                var icon = (System.Windows.Controls.TextBlock)((System.Windows.Controls.StackPanel)btn.Content).Children[0];
                var label = (System.Windows.Controls.TextBlock)((System.Windows.Controls.StackPanel)btn.Content).Children[1];
                icon.Text = nowPaused ? "▶️" : "⏸️";
                label.Text = nowPaused ? "Resume" : "Pause";
                UpdateStatusText();
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private void Button_ExportPng_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"chart_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                };
                if (dlg.ShowDialog() == true)
                {
                    var oldBackground = plotModel.Background;
                    var oldPlotAreaBackground = plotModel.PlotAreaBackground;
                    try
                    {
                        plotModel.Background = OxyColors.White;
                        plotModel.PlotAreaBackground = OxyColors.White;

                        using var stream = File.Create(dlg.FileName);
                        var exporter = new PngExporter
                        {
                            Width = Math.Max(1, (int)View.ActualWidth),
                            Height = Math.Max(1, (int)View.ActualHeight)
                        };
                        exporter.Export(plotModel, stream);
                    }
                    finally
                    {
                        plotModel.Background = oldBackground;
                        plotModel.PlotAreaBackground = oldPlotAreaBackground;
                    }
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }
    }

    internal class TimeSpanValid : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (((TimeSpan)value).Ticks == 0)
                return new ValidationResult(false, "Incorrect value");
            else
                return new ValidationResult(true, null);
        }
    }
}