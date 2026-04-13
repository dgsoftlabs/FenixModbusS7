using System;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    public class XMLVersion
    {
        private Version ver_ = new Version();

        public XMLVersion()
        { }

        public XMLVersion(Version c)
        { ver_ = c ?? new Version(); }

        public Version ToVersion()
        {
            return ver_;
        }

        public void FromVersion(Version c)
        {
            ver_ = c;
        }

        public static implicit operator Version(XMLVersion x)
        {
            return x?.ToVersion() ?? new Version();
        }

        public static implicit operator XMLVersion(Version c)
        {
            return c == null ? new XMLVersion(new Version()) : new XMLVersion(c);
        }

        [XmlAttribute]
        public string Ver
        {
            get { return (ver_ ?? new Version()).ToString(); }
            set
            {
                try
                {
                    ver_ = string.IsNullOrWhiteSpace(value) ? new Version() : new Version(value);
                }
                catch (Exception)
                {
                    ver_ = new Version();
                }
            }
        }
    }
}