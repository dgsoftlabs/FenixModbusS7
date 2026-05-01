using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class ScriptFile : ITreeViewModel, INotifyPropertyChanged
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

        [field: NonSerialized]
        private ProjectContainer projCon_;

        [Browsable(false)]
        [XmlIgnore]
        public ProjectContainer PrCon
        {
            get { return projCon_; }
            set
            {
                projCon_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrCon)));
            }
        }

        [field: NonSerialized]
        private Project Proj_;

        [Browsable(false)]
        [XmlIgnore]
        public Project Proj
        {
            get { return Proj_; }
            set
            {
                Proj_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Proj)));
            }
        }

        private Guid objId_;

        [Browsable(false)]
        [XmlElement(ElementName = "Id")]
        public Guid objId
        {
            get { return objId_; }
            set
            {
                objId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(objId)));
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

                if (!string.IsNullOrEmpty(FilePath))
                {
                    File.Move(FilePath, Path.GetDirectoryName(FilePath) + "\\" + value);
                    FilePath = Path.GetDirectoryName(FilePath) + "\\" + value;
                }
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
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
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
            }
        }

        private Boolean Enable_;

        [Browsable(false)]
        public Boolean Enable
        {
            get { return Enable_; }
            set
            {
                Enable_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enable)));
            }
        }

        [Category("01 Design"), DisplayName("IsBlocked")]
        public bool IsBlocked
        {
            get { return !Enable; }
            set
            {
                Enable = !value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
            }
        }

        private string TimerName_;

        [TypeConverter(typeof(ScriptFileTimers))]
        [Category("03 Script"), DisplayName("Trigger"), Browsable(true)]
        public string TimerName
        {
            get { return TimerName_; }
            set
            {
                TimerName_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TimerName)));
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
                return ((IDriverModel)Proj_.ScriptEng).isAlive;
            }

            set
            {
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ITreeViewModel.IsLive)));
            }
        }

        bool ITreeViewModel.IsBlocked
        {
            get
            {
                return IsBlocked;
            }
            set
            {
                IsBlocked = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));
            }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }

        public ScriptFile(string path)
        {
            this.FilePath_ = path;
            this.Name_ = Path.GetFileName(path);
            objId_ = Guid.NewGuid();
            Enable_ = true;
        }

        public ScriptFile()
        {
        }
    }
}