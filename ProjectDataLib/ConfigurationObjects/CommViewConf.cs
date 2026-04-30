using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class CommViewConf : INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        private bool CvCol1_;
        [XmlElement(ElementName = "CommViewColumn1Visibility")]
        public bool CvCol1 { get => CvCol1_; set { CvCol1_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol1))); } }

        private bool CvCol2_;
        [XmlElement(ElementName = "CommViewColumn2Visibility")]
        public bool CvCol2 { get => CvCol2_; set { CvCol2_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol2))); } }

        private bool CvCol3_;
        [XmlElement(ElementName = "CommViewColumn3Visibility")]
        public bool CvCol3 { get => CvCol3_; set { CvCol3_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol3))); } }

        private bool CvCol4_;
        [XmlElement(ElementName = "CommViewColumn4Visibility")]
        public bool CvCol4 { get => CvCol4_; set { CvCol4_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol4))); } }

        private bool CvCol5_;
        [XmlElement(ElementName = "CommViewColumn5Visibility")]
        public bool CvCol5 { get => CvCol5_; set { CvCol5_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol5))); } }

        private bool CvCol6_;
        [XmlElement(ElementName = "CommViewColumn6Visibility")]
        public bool CvCol6 { get => CvCol6_; set { CvCol6_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol6))); } }

        private bool CvCol7_;
        [XmlElement(ElementName = "CommViewColumn7Visibility")]
        public bool CvCol7 { get => CvCol7_; set { CvCol7_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol7))); } }
    }
}
