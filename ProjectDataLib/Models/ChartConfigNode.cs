using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;

namespace ProjectDataLib
{
    /// <summary>
    /// Tree node representing Chart Configuration in the project tree.
    /// </summary>
    public class ChartConfigNode : ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        private ObservableCollection<object> children_ = new ObservableCollection<object>();

        [Browsable(false)]
        public ObservableCollection<object> Children
        {
            get { return children_; }
            set { children_ = value; }
        }

        [Browsable(false)]
        public Project Project { get; }

        public ChartConfigNode(Project project)
        {
            Project = project;

            // Populate children – one node per axis
            RefreshChildren();
        }

        public void RefreshChildren()
        {
            children_.Clear();
            if (Project?.ChartConf?.Axes == null) return;

            foreach (var axis in Project.ChartConf.Axes)
                children_.Add(new ChartAxisNode(axis));
        }

        public string Name
        {
            get { return "Chart Configuration"; }
            set { }
        }

        private bool isExpand_ = true;
        public bool IsExpand
        {
            get { return isExpand_; }
            set { isExpand_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpand))); }
        }

        [Browsable(false)]
        public bool IsLive { get; set; }
        [Browsable(false)]
        public bool IsBlocked { get; set; }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }
    }

    /// <summary>
    /// Tree node representing a single chart axis in the project tree.
    /// </summary>
    public class ChartAxisNode : ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        [Browsable(false)]
        public ChartAxisConf AxisConf { get; }

        public ChartAxisNode(ChartAxisConf axisConf)
        {
            AxisConf = axisConf;
            ((INotifyPropertyChanged)axisConf).PropertyChanged += AxisConf_PropertyChanged;
        }

        private void AxisConf_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChartAxisConf.Key) || e.PropertyName == nameof(ChartAxisConf.Title))
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }

        public string Name
        {
            get { return string.IsNullOrWhiteSpace(AxisConf.Title) ? AxisConf.Key : $"{AxisConf.Key} – {AxisConf.Title}"; }
            set { }
        }

        [Browsable(false)]
        public ObservableCollection<object> Children { get; set; } = new ObservableCollection<object>();

        private bool isExpand_;
        public bool IsExpand
        {
            get { return isExpand_; }
            set { isExpand_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpand))); }
        }

        [Browsable(false)]
        public bool IsLive { get; set; }
        [Browsable(false)]
        public bool IsBlocked { get; set; }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }
    }
}
