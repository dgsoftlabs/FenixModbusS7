using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class CusFile : ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        [field: NonSerialized]
        private ObservableCollection<object> children_ = new ObservableCollection<object>();

        [Browsable(false)]
        [XmlIgnore]
        public Guid ObjId { get; set; } = Guid.NewGuid();

        [Browsable(false)]
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }

        [Browsable(false)]
        [XmlElement(ElementName = "FullName")]
        public string FullName
        {
            get => fullName_;
            set
            {
                fullName_ = value;
                Name = IsFile ? Path.GetFileName(value) : Path.GetFileName(value.TrimEnd(Path.DirectorySeparatorChar));
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FullName)));
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        private string fullName_ = string.Empty;

        [Browsable(false)]
        [XmlElement(ElementName = "IsFile")]
        public bool IsFile { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public ObservableCollection<object> Children
        {
            get => children_;
            set => children_ = value ?? new ObservableCollection<object>();
        }

        ObservableCollection<object> ITreeViewModel.Children
        {
            get => children_;
            set => children_ = value;
        }

        string ITreeViewModel.Name
        {
            get => Name;
            set => Name = value;
        }

        bool ITreeViewModel.IsExpand
        {
            get => true;
            set { }
        }

        bool ITreeViewModel.IsLive
        {
            get => false;
            set { }
        }

        bool ITreeViewModel.IsBlocked
        {
            get => false;
            set { }
        }

        Color ITreeViewModel.Clr
        {
            get => Color.White;
            set { }
        }

        public CusFile()
        {
        }

        public CusFile(FileInfo file)
        {
            IsFile = true;
            FullName = file.FullName;
        }

        public CusFile(DirectoryInfo directory)
        {
            IsFile = false;
            FullName = directory.FullName;
        }

        public ITreeViewModel DirSearch(string path, ITreeViewModel root)
        {
            if (root is not CusFile rootFile)
                return null;

            if (string.Equals(rootFile.FullName, path, StringComparison.OrdinalIgnoreCase))
                return rootFile;

            foreach (var child in root.Children.OfType<CusFile>())
            {
                var found = DirSearch(path, child);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
