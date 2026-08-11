using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class UserClass : INotifyPropertyChanged
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

        private string Name_;

        [Browsable(true), Category("Identy"), DisplayName("01 User Name")]
        [XmlElement(ElementName = "Login")]
        public string Name
        {
            get { return Name_; }
            set
            {
                Name_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        private string Pass_;

        [Browsable(true), Category("Identy"), DisplayName("02 Password")]
        [XmlElement(ElementName = "Password")]
        [PasswordPropertyText(true)]
        public string Pass
        {
            get { return Pass_; }
            set
            {
                Pass_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pass)));
            }
        }

        public UserClass()
        {
            this.Name_ = "User";
            this.Pass_ = "Password";
        }

        public UserClass(string name, string pass)
        {
            this.Name_ = name;
            this.Pass_ = pass;
        }

        public override string ToString()
        {
            return Name ?? "User";
        }
    }
}