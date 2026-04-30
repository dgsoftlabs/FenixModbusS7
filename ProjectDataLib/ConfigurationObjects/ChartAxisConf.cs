using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class ChartAxisConf : INotifyPropertyChanged
    {
        [field: NonSerialized]
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        private string key_ = "Y1";

        [XmlElement(ElementName = "Key")]
        public string Key
        {
            get { return key_; }
            set { key_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Key))); }
        }

        private string title_ = string.Empty;

        [XmlElement(ElementName = "Title")]
        public string Title
        {
            get { return title_; }
            set { title_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title))); }
        }

        private double minimum_ = double.NaN;

        [ClearableTextInput]
        [XmlElement(ElementName = "Minimum")]
        public double Minimum
        {
            get { return minimum_; }
            set { minimum_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Minimum))); }
        }

        private double maximum_ = double.NaN;

        [ClearableTextInput]
        [XmlElement(ElementName = "Maximum")]
        public double Maximum
        {
            get { return maximum_; }
            set { maximum_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Maximum))); }
        }

        private bool isRight_ = false;

        [XmlElement(ElementName = "IsRight")]
        public bool IsRight
        {
            get { return isRight_; }
            set { isRight_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRight))); }
        }

        private bool isVisible_ = true;

        [XmlElement(ElementName = "IsVisible")]
        public bool IsVisible
        {
            get { return isVisible_; }
            set { isVisible_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible))); }
        }

        public ChartAxisConf() { }

        public ChartAxisConf(string key, string title, bool isRight = false)
        {
            key_ = key;
            title_ = title;
            isRight_ = isRight;
            minimum_ = double.NaN;
            maximum_ = double.NaN;
            isVisible_ = true;
        }
    }
}
