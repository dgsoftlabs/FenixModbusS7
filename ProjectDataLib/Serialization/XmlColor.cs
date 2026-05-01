using System;
using System.Drawing;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    public class XmlColor
    {
        private Color color_ = Color.Black;

        public XmlColor()
        { }

        public XmlColor(Color c)
        { color_ = c; }

        public Color ToColor()
        {
            return color_;
        }

        public void FromColor(Color c)
        {
            color_ = c;
        }

        public static implicit operator Color(XmlColor x)
        {
            return x.ToColor();
        }

        public static implicit operator XmlColor(Color c)
        {
            return new XmlColor(c);
        }

        [XmlAttribute]
        public string Val
        {
            get { return ColorTranslator.ToHtml(color_); }
            set
            {
                try
                {
                    if (Alpha == 0xFF)
                        color_ = ColorTranslator.FromHtml(value);
                    else
                        color_ = Color.FromArgb(Alpha, ColorTranslator.FromHtml(value));
                }
                catch (Exception)
                {
                    color_ = Color.Black;
                }
            }
        }

        [XmlAttribute]
        public byte Alpha
        {
            get { return color_.A; }
            set
            {
                if (value != color_.A)
                    color_ = Color.FromArgb(value, color_);
            }
        }

        public bool ShouldSerializeAlpha()
        { return Alpha < 0xFF; }
    }
}