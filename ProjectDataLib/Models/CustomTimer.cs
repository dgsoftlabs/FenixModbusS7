using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;

namespace ProjectDataLib
{
    [Serializable]
    public class CustomTimer : IComparable<CustomTimer>, ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler _propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propChanged += value; }
            remove { _propChanged -= value; }
        }

        public CustomTimer()
        {
            Name_ = "Timer";
            Time_ = 1000;
            Delay_ = 0;
        }

        private string Name_;

        [Category("Design"), DisplayName("01 Timer Name")]
        public string Name
        {
            get { return Name_; }
            set
            {
                Name_ = value;
                _propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        private int Time_;

        [Category("Design"), DisplayName("02 Time [ms]")]
        public int Time
        {
            get { return Time_; }
            set { Time_ = value; }
        }

        private int Delay_;

        [Category("Design"), DisplayName("03 Delay [ms]")]
        public int Delay
        {
            get { return Delay_; }
            set { Delay_ = value; }
        }

        int IComparable<CustomTimer>.CompareTo(CustomTimer other)
        {
            return this.Time_.CompareTo(other.Time_);
        }

        ObservableCollection<object> ITreeViewModel.Children
        {
            get { return new ObservableCollection<object>(); }
            set { }
        }

        string ITreeViewModel.Name
        {
            get { return Name_; }
            set
            {
                Name_ = value;
                _propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        bool ITreeViewModel.IsExpand
        {
            get { return false; }
            set { }
        }

        bool ITreeViewModel.IsLive
        {
            get { return false; }
            set { }
        }

        bool ITreeViewModel.IsBlocked
        {
            get { return false; }
            set { }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }
    }
}
