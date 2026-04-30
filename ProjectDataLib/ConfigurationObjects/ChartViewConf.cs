using System;
using System.Collections.Generic;
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

        private List<ChartAxisConf> axes_;

        [XmlArray(ElementName = "ChartAxes")]
        [XmlArrayItem(ElementName = "Axis")]
        public List<ChartAxisConf> Axes
        {
            get { return axes_; }
            set
            {
                axes_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Axes)));
            }
        }

        public ChartViewConf()
        {
            TrackSpan = new TimeSpan(0, 0, 15);
            axes_ = null;
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

        private bool histData_;

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

        [OnDeserializing]
        private void OnDeserialized(StreamingContext context)
        {
            if (TrackSpan.Ticks == 0)
                TrackSpan = new TimeSpan(0, 0, 15);
        }

        public void OnDeserializedXML()
        {
            if (TrackSpan.Ticks == 0)
                TrackSpan = new TimeSpan(0, 0, 15);

            if (axes_ == null || axes_.Count == 0)
                axes_ = new List<ChartAxisConf>
                {
                    new ChartAxisConf("Y1", "Y1", isRight: false),
                    new ChartAxisConf("Y2", "Y2", isRight: true)
                };
        }
    }
}