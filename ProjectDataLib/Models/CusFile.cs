using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ProjectDataLib
{
    public class CusFile : ITreeViewModel, INotifyPropertyChanged
    {
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                propChanged += value;
            }

            remove
            {
                propChanged -= value;
            }
        }

        [Browsable(false)]
        public bool IsFile { get; set; }

        private string FullName_;

        [DisplayName("File Path")]
        [Category("01 Design"), ReadOnly(true)]
        public string FullName
        {
            get { return FullName_; }
            set
            {
                FullName_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        public CusFile(DirectoryInfo d)
        {
            FullName = d.FullName;
            var SubDir = (from x in d.GetDirectories() select new CusFile(x)).ToList();
            SubDir.AddRange(from x in d.GetFiles() select new CusFile(x));
            Children_ = new ObservableCollection<object>(SubDir);
        }

        public CusFile(FileInfo f)
        {
            IsFile = true;
            Children_ = new ObservableCollection<object>();
            FullName = f.FullName;
        }

        private ObservableCollection<Object> Children_;

        ObservableCollection<object> ITreeViewModel.Children
        {
            get
            {
                return Children_;
            }

            set
            {
                Children_ = value;
            }
        }

        private bool IsBlocked_;

        [Browsable(false)]
        public bool IsBlocked
        {
            get
            {
                return IsBlocked_;
            }
            set
            {
                IsBlocked_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
            }
        }

        private bool IsExpand_;

        [Browsable(false)]
        public bool IsExpand
        {
            get { return IsExpand_; }
            set
            {
                IsExpand_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpand)));
            }
        }

        bool ITreeViewModel.IsLive
        {
            get
            {
                return false;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        string ITreeViewModel.Name
        {
            get
            {
                return Path.GetFileName(FullName);
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }
    }
}
