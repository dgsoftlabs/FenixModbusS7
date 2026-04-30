using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace ProjectDataLib
{
    public class TimersFolder : ITreeViewModel
    {
        private readonly List<CustomTimer> _timersList;
        private readonly ObservableCollection<object> _children;

        public TimersFolder(List<CustomTimer> timers)
        {
            _timersList = timers;
            _children = new ObservableCollection<object>(timers);
        }

        public void AddTimer()
        {
            var timer = new CustomTimer();
            _timersList.Add(timer);
            _children.Add(timer);
        }

        public void RemoveTimer(CustomTimer timer)
        {
            _timersList.Remove(timer);
            _children.Remove(timer);
        }

        ObservableCollection<object> ITreeViewModel.Children
        {
            get { return _children; }
            set { }
        }

        string ITreeViewModel.Name
        {
            get { return "Timers"; }
            set { }
        }

        private bool _isExpand = true;

        bool ITreeViewModel.IsExpand
        {
            get { return _isExpand; }
            set { _isExpand = value; }
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
