using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class TableViewConf : INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        private bool TvCol1_;
        [XmlElement(ElementName = "TableColumn1Visibility")]
        public bool TvCol1 { get => TvCol1_; set { TvCol1_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol1))); } }

        private bool TvCol2_;
        [XmlElement(ElementName = "TableColumn2Visibility")]
        public bool TvCol2 { get => TvCol2_; set { TvCol2_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol2))); } }

        private bool TvCol3_;
        [XmlElement(ElementName = "TableColumn3Visibility")]
        public bool TvCol3 { get => TvCol3_; set { TvCol3_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol3))); } }

        private bool TvCol4_;
        [XmlElement(ElementName = "TableColumn4Visibility")]
        public bool TvCol4 { get => TvCol4_; set { TvCol4_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol4))); } }

        private bool TvCol5_;
        [XmlElement(ElementName = "TableColumn5Visibility")]
        public bool TvCol5 { get => TvCol5_; set { TvCol5_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol5))); } }

        private bool TvCol6_;
        [XmlElement(ElementName = "TableColumn6Visibility")]
        public bool TvCol6 { get => TvCol6_; set { TvCol6_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol6))); } }

        private bool TvCol7_;
        [XmlElement(ElementName = "TableColumn7Visibility")]
        public bool TvCol7 { get => TvCol7_; set { TvCol7_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol7))); } }

        private bool TvCol8_;
        [XmlElement(ElementName = "TableColumn8Visibility")]
        public bool TvCol8 { get => TvCol8_; set { TvCol8_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol8))); } }

        private bool TvCol9_;
        [XmlElement(ElementName = "TableColumn9Visibility")]
        public bool TvCol9 { get => TvCol9_; set { TvCol9_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol9))); } }

        private bool TvCol10_;
        [XmlElement(ElementName = "TableColumn10Visibility")]
        public bool TvCol10 { get => TvCol10_; set { TvCol10_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol10))); } }

        private bool TvCol11_;
        [XmlElement(ElementName = "TableColumn11Visibility")]
        public bool TvCol11 { get => TvCol11_; set { TvCol11_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol11))); } }

        private bool TvCol12_;
        [XmlElement(ElementName = "TableColumn12Visibility")]
        public bool TvCol12 { get => TvCol12_; set { TvCol12_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol12))); } }

        private bool TvCol13_;
        [XmlElement(ElementName = "TableColumn13Visibility")]
        public bool TvCol13 { get => TvCol13_; set { TvCol13_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol13))); } }

        private bool TvCol14_;
        [XmlElement(ElementName = "TableColumn14Visibility")]
        public bool TvCol14 { get => TvCol14_; set { TvCol14_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol14))); } }

        private bool TvCol15_;
        [XmlElement(ElementName = "TableColumn15Visibility")]
        public bool TvCol15 { get => TvCol15_; set { TvCol15_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol15))); } }
    }
}
