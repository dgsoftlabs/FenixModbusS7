using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;

namespace ProjectDataLib
{
    [Serializable]
    [Obsolete("Typ jest przestarzaly")]
    public class InFile : ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
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

        private Guid objId_;

        [Browsable(false)]
        public Guid objId
        {
            get { return objId_; }
            set
            {
                objId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("objId"));
            }
        }

        private string Name_;

        [Category("01 Design"), DisplayName("Name")]
        public string Name
        {
            get { return Name_; }
            set
            {
                Name_ = value;

                File.Move(FilePath, Path.GetDirectoryName(FilePath) + "\\" + value);
                FilePath = Path.GetDirectoryName(FilePath) + "\\" + value;

                propChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        private String FilePath_;

        [Category("02 Misc"), ReadOnly(true), DisplayName("Path")]
        public string FilePath
        {
            get { return FilePath_; }
            set
            {
                FilePath_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("FilePath"));
            }
        }

        ObservableCollection<object> ITreeViewModel.Children
        {
            get
            {
                return new ObservableCollection<object>();
            }

            set
            {
            }
        }

        string ITreeViewModel.Name
        {
            get
            {
                return Name;
            }

            set
            {
                Name = value;
            }
        }

        bool ITreeViewModel.IsExpand
        {
            get
            {
                return false;
            }

            set
            {
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
            }
        }

        bool ITreeViewModel.IsBlocked
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }

        public InFile(string path)
        {
            this.FilePath_ = path;
            this.Name_ = Path.GetFileName(path);
            objId_ = Guid.NewGuid();
        }

        public InFile()
        {
        }
    }
}
