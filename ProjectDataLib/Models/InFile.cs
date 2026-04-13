using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class InFile : ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        [field: NonSerialized]
        private ProjectContainer prCon_;

        [Browsable(false)]
        [XmlIgnore]
        public ProjectContainer PrCon
        {
            get => prCon_;
            set
            {
                prCon_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrCon)));
            }
        }

        [field: NonSerialized]
        private Project proj_;

        [Browsable(false)]
        [XmlIgnore]
        public Project Proj
        {
            get => proj_;
            set
            {
                proj_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Proj)));
            }
        }

        private Guid objId_;

        [Browsable(false)]
        [XmlElement(ElementName = "Id")]
        public Guid objId
        {
            get => objId_;
            set
            {
                objId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(objId)));
            }
        }

        private string name_;

        [Category("01 Design"), DisplayName("Name")]
        [XmlElement(ElementName = "Name")]
        public string Name
        {
            get => name_;
            set
            {
                name_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        private string filePath_;

        [Category("02 Misc"), ReadOnly(true), DisplayName("Path")]
        [XmlElement(ElementName = "Path")]
        public string FilePath
        {
            get => filePath_;
            set
            {
                filePath_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
            }
        }

        private bool enable_;

        [Browsable(false)]
        [XmlElement(ElementName = "Enable")]
        public bool Enable
        {
            get => enable_;
            set
            {
                enable_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enable)));
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
            }
        }

        [Category("01 Design"), DisplayName("IsBlocked")]
        [XmlIgnore]
        public bool IsBlocked
        {
            get => !Enable;
            set => Enable = !value;
        }

        ObservableCollection<object> ITreeViewModel.Children
        {
            get => new ObservableCollection<object>();
            set { }
        }

        string ITreeViewModel.Name
        {
            get => Name;
            set => Name = value;
        }

        bool ITreeViewModel.IsExpand
        {
            get => false;
            set { }
        }

        bool ITreeViewModel.IsLive
        {
            get => false;
            set { }
        }

        bool ITreeViewModel.IsBlocked
        {
            get => IsBlocked;
            set => IsBlocked = value;
        }

        Color ITreeViewModel.Clr
        {
            get => Color.White;
            set { }
        }

        public InFile(string path)
        {
            FilePath = path;
            Name = Path.GetFileName(path);
            objId = Guid.NewGuid();
            Enable = true;
        }

        public InFile()
        {
            objId = Guid.NewGuid();
            Enable = true;
        }
    }
}
