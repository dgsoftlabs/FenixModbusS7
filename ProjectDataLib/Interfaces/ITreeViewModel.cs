using System;
using System.Collections.ObjectModel;
using System.Drawing;

namespace ProjectDataLib
{
    public interface ITreeViewModel
    {
        ObservableCollection<Object> Children { get; set; }
        string Name { get; set; }
        bool IsExpand { get; set; }
        bool IsLive { get; set; }
        bool IsBlocked { get; set; }
        Color Clr { get; set; }
    }
}
