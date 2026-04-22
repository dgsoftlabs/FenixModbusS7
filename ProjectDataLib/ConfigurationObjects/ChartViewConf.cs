using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class ChartViewConf : INotifyPropertyChanged
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

        private double maximumX_;

        [XmlElement(ElementName = "ChartX1", Type = typeof(DateTime))]
        public DateTime maximumX
        {
            get
            {
                return DateTime.FromOADate(double.IsNaN(maximumX_) ? 0 : maximumX_);
            }
            set
            {
                maximumX_ = value.ToOADate();
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(maximumX)));
            }
        }

        private double minimumX_;

        [XmlElement(ElementName = "ChartX0", Type = typeof(DateTime))]
        public DateTime minimumX
        {
            get
            {
                return DateTime.FromOADate(double.IsNaN(minimumX_) ? 0 : minimumX_);
            }
            set
            {
                minimumX_ = value.ToOADate();
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(minimumX)));
            }
        }

        private double maximumY_;

        [XmlElement(ElementName = "ChartY1")]
        public double maximumY
        {
            get
            {
                return maximumY_;
            }
            set
            {
                maximumY_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(maximumY)));
            }
        }

        private double minimumY_;

        [XmlElement(ElementName = "ChartY0")]
        public double minimumY
        {
            get
            {
                return minimumY_;
            }
            set
            {
                minimumY_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(minimumY)));
            }
        }

        private bool PanelIsExpand_;

        [Obsolete("Removed form chart")]
        public bool PanelIsExpand
        {
            get { return PanelIsExpand_; }
            set
            {
                PanelIsExpand_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PanelIsExpand)));
            }
        }

        public ChartViewConf()
        {
            maximumX = DateTime.Now;
            minimumX = DateTime.Now;

            maximumY = double.NaN;
            minimumY = double.NaN;

            TrackSpan = new TimeSpan(0, 0, 15);

            From = DateTime.Now;
            To = DateTime.Now;
        }

        [OptionalField]
        private TimeSpan TrackSpan_;

        [XmlIgnore]
        public TimeSpan TrackSpan
        {
            get { return TrackSpan_; }
            set
            {
                TrackSpan_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackSpan)));
            }
        }

        [XmlElement(ElementName = "ChartShowLast")]
        public XmlTimeSpan TrackSpanXml
        {
            get => TrackSpan_;
            set => TrackSpan = value;
        }

        private DateTime From_;

        [XmlElement(ElementName = "ChartFrom")]
        public DateTime From
        {
            get { return From_; }
            set
            {
                From_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(From)));
            }
        }

        private DateTime To_;

        [XmlElement(ElementName = "ChartTo")]
        public DateTime To
        {
            get { return To_; }
            set
            {
                To_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(To)));
            }
        }

        private bool histData_;

        [Obsolete("Removed form chart")]
        [XmlElement(ElementName = "ChartDatabaseMode")]
        public bool histData
        {
            get { return histData_; }
            set
            {
                histData_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(histData)));
            }
        }

        private bool CvCol1_;

        [XmlElement(ElementName = "CommViewColumn1Visibility")]
        public bool CvCol1
        {
            get { return CvCol1_; }
            set
            {
                CvCol1_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol1)));
            }
        }

        private bool CvCol2_;

        [XmlElement(ElementName = "CommViewColumn2Visibility")]
        public bool CvCol2
        {
            get { return CvCol2_; }
            set
            {
                CvCol2_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol2)));
            }
        }

        private bool CvCol3_;

        [XmlElement(ElementName = "CommViewColumn3Visibility")]
        public bool CvCol3
        {
            get { return CvCol3_; }
            set
            {
                CvCol3_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol3)));
            }
        }

        private bool CvCol4_;

        [XmlElement(ElementName = "CommViewColumn4Visibility")]
        public bool CvCol4
        {
            get { return CvCol4_; }
            set
            {
                CvCol4_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol4)));
            }
        }

        private bool CvCol5_;

        [XmlElement(ElementName = "CommViewColumn5Visibility")]
        public bool CvCol5
        {
            get { return CvCol5_; }
            set
            {
                CvCol5_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol5)));
            }
        }

        private bool CvCol6_;

        [XmlElement(ElementName = "CommViewColumn6Visibility")]
        public bool CvCol6
        {
            get { return CvCol6_; }
            set
            {
                CvCol6_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol6)));
            }
        }

        private bool CvCol7_;

        [XmlElement(ElementName = "CommViewColumn7Visibility")]
        public bool CvCol7
        {
            get { return CvCol7_; }
            set
            {
                CvCol7_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CvCol7)));
            }
        }

        private bool TvCol1_;

        [XmlElement(ElementName = "TableColumn1Visibility")]
        public bool TvCol1
        {
            get { return TvCol1_; }
            set
            {
                TvCol1_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol1)));
            }
        }

        private bool TvCol2_;

        [XmlElement(ElementName = "TableColumn2Visibility")]
        public bool TvCol2
        {
            get { return TvCol2_; }
            set
            {
                TvCol2_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol2)));
            }
        }

        private bool TvCol3_;

        [XmlElement(ElementName = "TableColumn3Visibility")]
        public bool TvCol3
        {
            get { return TvCol3_; }
            set
            {
                TvCol3_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol3)));
            }
        }

        private bool TvCol4_;

        [XmlElement(ElementName = "TableColumn4Visibility")]
        public bool TvCol4
        {
            get { return TvCol4_; }
            set
            {
                TvCol4_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol4)));
            }
        }

        private bool TvCol5_;

        [XmlElement(ElementName = "TableColumn5Visibility")]
        public bool TvCol5
        {
            get { return TvCol5_; }
            set
            {
                TvCol5_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol5)));
            }
        }

        private bool TvCol6_;

        [XmlElement(ElementName = "TableColumn6Visibility")]
        public bool TvCol6
        {
            get { return TvCol6_; }
            set
            {
                TvCol6_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol6)));
            }
        }

        private bool TvCol7_;

        [XmlElement(ElementName = "TableColumn7Visibility")]
        public bool TvCol7
        {
            get { return TvCol7_; }
            set
            {
                TvCol7_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol7)));
            }
        }

        private bool TvCol8_;

        [XmlElement(ElementName = "TableColumn8Visibility")]
        public bool TvCol8
        {
            get { return TvCol8_; }
            set
            {
                TvCol8_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol8)));
            }
        }

        private bool TvCol9_;

        [XmlElement(ElementName = "TableColumn9Visibility")]
        public bool TvCol9
        {
            get { return TvCol9_; }
            set
            {
                TvCol9_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol9)));
            }
        }

        private bool TvCol10_;

        [XmlElement(ElementName = "TableColumn10Visibility")]
        public bool TvCol10
        {
            get { return TvCol10_; }
            set
            {
                TvCol10_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol10)));
            }
        }

        private bool TvCol11_;

        [XmlElement(ElementName = "TableColumn11Visibility")]
        public bool TvCol11
        {
            get { return TvCol11_; }
            set
            {
                TvCol11_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol11)));
            }
        }

        private bool TvCol12_;

        [XmlElement(ElementName = "TableColumn12Visibility")]
        public bool TvCol12
        {
            get { return TvCol12_; }
            set
            {
                TvCol12_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol12)));
            }
        }

        private bool TvCol13_;

        [XmlElement(ElementName = "TableColumn13Visibility")]
        public bool TvCol13
        {
            get { return TvCol13_; }
            set
            {
                TvCol13_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol13)));
            }
        }

        private bool TvCol14_;

        [XmlElement(ElementName = "TableColumn14Visibility")]
        public bool TvCol14
        {
            get { return TvCol14_; }
            set
            {
                TvCol14_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol14)));
            }
        }

        private bool TvCol15_;

        [XmlElement(ElementName = "TableColumn15Visibility")]
        public bool TvCol15
        {
            get { return TvCol15_; }
            set
            {
                TvCol15_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TvCol15)));
            }
        }

        [OnDeserializing]
        private void OnDeserialized(StreamingContext context)
        {
            if (From.Ticks == 0)
                From = DateTime.Now;

            if (To.Ticks == 0)
                To = DateTime.Now;

            if (TrackSpan.Ticks == 0)
                TrackSpan = new TimeSpan(0, 0, 15);
        }

        public void OnDeserializedXML()
        {
            if (From.Ticks == 0)
                From = DateTime.Now;

            if (To.Ticks == 0)
                To = DateTime.Now;

            if (TrackSpan.Ticks == 0)
                TrackSpan = new TimeSpan(0, 0, 15);
        }
    }
}