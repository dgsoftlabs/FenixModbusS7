using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class InTag : IComparable<Tag>, ITag, IDriverModel, INotifyPropertyChanged, ITreeViewModel
    {
        [field: NonSerialized]
        [JsonIgnore]
        private ProjectContainer projCon_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public ProjectContainer PrCon
        {
            get { return projCon_; }
            set { projCon_ = value; }
        }

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
        public event EventHandler refreshedCycle;

        [field: NonSerialized]
        public event EventHandler refreshedPartial;

        [field: NonSerialized]
        public event EventHandler error;

        [field: NonSerialized]
        public event EventHandler information;

        [field: NonSerialized]
        public event EventHandler dataSent;

        [field: NonSerialized]
        public event EventHandler dataRecived;

        [field: NonSerialized]
        private Project Proj_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public Project Proj
        {
            get { return Proj_; }
            set
            {
                Proj_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Proj"));
            }
        }

        [NonSerialized]
        private IDriverModel idrv_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public IDriverModel idrv
        {
            get { return idrv_; }
            set
            {
                idrv_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("idrv"));
            }
        }

        private string Format_;

        [Category("04 Appearance"), DisplayName("Format Value")]
        [JsonIgnore]
        [TypeConverter(typeof(FormatValueConv))]
        public string Format
        {
            get { return Format_; }
            set
            {
                Format_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Format"));
            }
        }

        private Guid parentId_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlElement(ElementName = "Parent")]
        public Guid parentId
        {
            get { return parentId_; }
            set
            {
                parentId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("parentId"));
            }
        }

        private Guid objId_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlElement(ElementName = "Id")]
        public Guid objId
        {
            get { return objId_; }
            set
            {
                objId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("objId"));
            }
        }

        private string tagName_;

        [Category("01 Design"), DisplayName("Tag Name")]
        [XmlElement(ElementName = "Name")]
        public string tagName
        {
            get { return tagName_; }
            set
            {
                if (Proj != null)
                {
                    if (Proj_.tagsList.Where(x => x.tagName == value).Count<Tag>() > 0 || Proj_.InTagsList.Where(x => x.tagName == value).Count<InTag>() > 0)
                        tagName_ = tagName_ + "$$";
                    else
                        tagName_ = value;
                }
                else
                    tagName_ = value;

                propChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        private TypeData TypeData_;

        [Category("02 Data"), DisplayName("Data Type")]
        [JsonIgnore]
        public TypeData TypeData
        {
            get { return TypeData_; }
            set
            {
                TypeData_ = value;
                ResetValue();

                propChanged?.Invoke(this, new PropertyChangedEventArgs("TypeData_"));
            }
        }

        private string WriteScript_;

        [Category("03 Script"), DisplayName("Code"), Browsable(true)]
        [JsonIgnore]
        public string WriteScript
        {
            get { return WriteScript_; }
            set { WriteScript_ = value; }
        }

        private string describe_;

        [Category("01 Design"), DisplayName("Description")]
        [JsonIgnore]
        [XmlElement(ElementName = "Description")]
        public string describe
        {
            get { return describe_; }
            set
            {
                describe_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Description"));
            }
        }

        private string tagVersion_;

        [Category("01 Design"), DisplayName("Version")]
        [JsonIgnore]
        [XmlElement(ElementName = "Version")]
        public string tagVersion
        {
            get { return tagVersion_; }
            set
            {
                tagVersion_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("TagVersion"));
            }
        }

        private string TimerName_;

        [TypeConverter(typeof(InTagsTimers))]
        [Category("03 Script"), DisplayName("Trigger"), Browsable(true)]
        [JsonIgnore]
        public string TimerName
        {
            get { return TimerName_; }
            set { TimerName_ = value; }
        }

        private Object value_;

        [Category("02 Data"), DisplayName("Value")]
        public object value
        {
            get
            {
                return value_;
            }

            set
            {
                if (value is string)
                {
                    if (TypeData_ == TypeData.BIT)
                    {
                        if ((string)value == "1")
                            value_ = true;
                        else if ((string)value == "0")
                            value = false;
                        else
                            value = false;
                    }
                    else
                    {
                        value_ = value;
                    }
                }
                else
                {
                    value_ = value;
                }

                propChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }
        }

        public InTag(ProjectContainer prCon, Project pr, string tagName, string desribe, TypeData typeData, string initVal)
        {
            try
            {
                this.tagName_ = tagName;

                this.TypeData_ = typeData;

                this.describe_ = desribe;

                objId = Guid.NewGuid();

                this.Proj_ = pr;

                this.projCon_ = prCon;

                Format_ = "{0:0.00}";

                switch (this.TypeData_)
                {
                    case TypeData.BIT:
                        value_ = Convert.ToBoolean(initVal);
                        break;

                    case TypeData.SBYTE:
                        value_ = Convert.ToSByte(initVal);
                        break;

                    case TypeData.BYTE:
                        value_ = Convert.ToByte(initVal);
                        break;

                    case TypeData.CHAR:
                        value_ = Convert.ToChar(initVal);
                        break;

                    case TypeData.USHORT:
                        value_ = Convert.ToUInt16(initVal);
                        break;

                    case TypeData.SHORT:
                        value_ = Convert.ToInt16(initVal);
                        break;

                    case TypeData.UINT:
                        value_ = Convert.ToUInt32(initVal);
                        break;

                    case TypeData.INT:
                        value_ = Convert.ToInt32(initVal);
                        break;

                    case TypeData.FLOAT:
                        value_ = Convert.ToSingle(initVal);
                        break;

                    case TypeData.DOUBLE:
                        value_ = Convert.ToDouble(initVal);
                        break;

                    default:
                        value_ = Convert.ToInt32(initVal);
                        break;
                }

                Random random = new Random();
                Clr_ = Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));

                Width_ = 2;

                GrEnable_ = true;

                GrVisible_ = true;

                GrVisibleTab_ = true;

                ResetValue();
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.Tag.Tag() :" + Ex.Message); ;
            }
        }

        public InTag()
        {
        }

        public int getSize()
        {
            try
            {
                switch (this.TypeData_)
                {
                    case TypeData.BIT:
                        return 1;

                    case TypeData.SBYTE:
                        return 8;

                    case TypeData.BYTE:
                        return 8;

                    case TypeData.CHAR:
                        return 16;

                    case TypeData.USHORT:
                        return 16;

                    case TypeData.SHORT:
                        return 16;

                    case TypeData.UINT:
                        return 32;

                    case TypeData.INT:
                        return 32;

                    case TypeData.FLOAT:
                        return 32;

                    case TypeData.DOUBLE:
                        return 64;

                    case ProjectDataLib.TypeData.ShortToReal:
                        return 16;

                    default:
                        throw new ApplicationException("typeData isn't appropriate!");
                }
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.Tag.getSize() :" + Ex.Message);
            }
        }

        public Type getOwnType()
        {
            try
            {
                switch (this.TypeData_)
                {
                    case TypeData.BIT:
                        return typeof(Boolean);

                    case TypeData.SBYTE:
                        return typeof(SByte);

                    case TypeData.BYTE:
                        return typeof(Byte);

                    case TypeData.CHAR:
                        return typeof(Char);

                    case TypeData.USHORT:
                        return typeof(ushort);

                    case TypeData.SHORT:
                        return typeof(short);

                    case TypeData.UINT:
                        return typeof(uint);

                    case TypeData.INT:
                        return typeof(int);

                    case TypeData.DOUBLE:
                        return typeof(double);

                    case TypeData.FLOAT:
                        return typeof(float);

                    case TypeData.ShortToReal:
                        return typeof(float);

                    default:
                        throw new ApplicationException("typeData isn't appropriate!");
                }
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.Tag.getSize() :" + Ex.Message);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ITag)
                return Guid.Equals(this.objId, ((ITag)obj).Id);
            return false;
        }

        public override int GetHashCode()
        {
            return this.objId.GetHashCode();
        }

        public int CompareTo(Tag b)
        {
            return this.tagName_.CompareTo(b.tagName);
        }

        private Color Clr_;

        [Category("05 Graph"), DisplayName("Color")]
        [JsonIgnore]
        [XmlIgnore]
        public Color Clr
        {
            get { return Clr_; }
            set
            {
                Clr_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Clr"));
            }
        }

        [Browsable(false)]
        [XmlElement(ElementName = "Color")]
        public XmlColor ClrXml
        {
            get => Clr_;
            set => Clr = value;
        }

        private int Width_;

        [Category("05 Graph"), DisplayName("Line Width")]
        [JsonIgnore]
        public int Width
        {
            get { return Width_; }
            set
            {
                Width_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Width"));
            }
        }

        private Boolean GrEnable_;

        [Category("05 Graph"), DisplayName("Enable")]
        [JsonIgnore]
        [XmlElement(ElementName = "ChartEnable")]
        public Boolean GrEnable
        {
            get { return GrEnable_; }
            set
            {
                GrEnable_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("GrEnable"));
            }
        }

        private Boolean GrVisible_;

        [Category("05 Graph"), DisplayName("Visible (Chart)")]
        [JsonIgnore]
        [XmlElement(ElementName = "ChartVisible")]
        public Boolean GrVisible
        {
            get { return GrVisible_; }
            set
            {
                GrVisible_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("GrVisible"));
            }
        }

        private string GrAxisKey_ = "Y1";

        [Category("05 Graph"), DisplayName("Y Axis")]
        [TypeConverter(typeof(ChartAxisKeyConverter))]
        [JsonIgnore]
        [XmlElement(ElementName = "ChartAxisKey")]
        public string GrAxisKey
        {
            get { return GrAxisKey_; }
            set
            {
                GrAxisKey_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("GrAxisKey"));
            }
        }

        private Boolean GrVisibleTab_;

        [Category("06 Table"), DisplayName("Visible (Table)")]
        [JsonIgnore]
        [XmlElement(ElementName = "TableVisible")]
        public Boolean GrVisibleTab
        {
            get { return GrVisibleTab_; }
            set
            {
                GrVisibleTab_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("GrVisibleTab"));
            }
        }

        [Browsable(false)]
        public string formattedValue
        {
            get
            {
                if (Format_ == "{0:ASCII}")
                {
                    dynamic d = value_;
                    return Encoding.ASCII.GetString(BitConverter.GetBytes(d));
                }
                else
                {
                    return string.Format(Format, value);
                }
            }
        }

        [Browsable(false)]
        public string typeData
        { get { return Enum.GetName(typeof(TypeData), TypeData); } }

        [Browsable(false)]
        public string description
        { get { return describe; } }

        private Boolean ResetValue()
        {
            try
            {
                switch (this.TypeData_)
                {
                    case TypeData.BIT:
                        value = false;
                        break;

                    case TypeData.SBYTE:
                        value = Convert.ToSByte(0);
                        break;

                    case TypeData.BYTE:
                        value = Convert.ToByte(0);
                        break;

                    case TypeData.CHAR:
                        value = Convert.ToChar(0);
                        break;

                    case TypeData.USHORT:
                        value = Convert.ToUInt16(0);
                        break;

                    case TypeData.SHORT:
                        value = Convert.ToInt16(0);
                        break;

                    case TypeData.UINT:
                        value = Convert.ToUInt32(0);
                        break;

                    case TypeData.INT:
                        value = Convert.ToInt32(0);
                        break;

                    case TypeData.FLOAT:
                        value = Convert.ToSingle(0.0);
                        break;

                    case TypeData.DOUBLE:
                        value = Convert.ToDouble(0.0);
                        break;

                    case ProjectDataLib.TypeData.ShortToReal:
                        value = Convert.ToSingle(0.0);
                        break;

                    default:
                        value = 0;
                        break;
                }

                return true;
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return false;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Clr_ == null)
                Clr_ = Color.Black;

            if (Width_ == 0)
                Width_ = 2;

            ResetValue();
        }

        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;
            InTag Tg1 = (InTag)bf.Deserialize(ms);
            ms.Close();

            return Tg1;
        }

        Guid ITag.Id
        {
            get { return objId; }
            set { objId = value; }
        }

        Guid ITag.ParentId
        {
            get { return Proj_.InternalTagsDrv.objId; }
            set { Proj_.InternalTagsDrv.objId = value; }
        }

        string ITag.Name
        {
            get { return tagName; }
            set { tagName = value; }
        }

        Boolean ITag.ActName
        {
            get { return true; }
        }

        int ITag.DevAdress
        {
            get { return 0; }
            set {; }
        }

        Boolean ITag.ActDevAdress { get { return false; } }

        int ITag.Adress
        {
            get { return 0; }
            set {; }
        }

        Boolean ITag.ActAdress { get { return false; } }

        int ITag.BitByte
        {
            get { return 0; }
            set {; }
        }

        Boolean ITag.ActBitByte { get { return false; } }

        TypeData ITag.TypeData_
        {
            get { return TypeData; }
            set { TypeData = value; }
        }

        Boolean ITag.ActTypeData_ { get { return true; } }

        string ITag.AreaData
        {
            get { return string.Empty; }
            set {; }
        }

        Boolean ITag.ActAreaData { get { return false; } }

        Object ITag.Value
        {
            get { return this.value; }
            set
            {
                this.value = value;
            }
        }

        Boolean ITag.ActValue { get { return true; } }

        string ITag.Description
        {
            get { return describe; }
            set { describe = value; }
        }

        Boolean ITag.ActDscription { get { return true; } }

        string ITag.TagVersion
        {
            get { return tagVersion; }
            set { tagVersion = value; }
        }

        Boolean ITag.ActVersion { get { return true; } }

        Boolean ITag.ActParam
        {
            get { return false; }
        }

        Boolean ITag.ActSetValue
        {
            get { return false; }
        }

        void ITag.SetValue(Object val)
        {
            try
            {
                this.value = val;
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.Tag.SetValue() :" + Ex.Message);
            }
        }

        Type ITag.getOwnType()
        {
            try
            {
                switch (this.TypeData_)
                {
                    case TypeData.BIT:
                        return typeof(Boolean);

                    case TypeData.SBYTE:
                        return typeof(SByte);

                    case TypeData.BYTE:
                        return typeof(Byte);

                    case TypeData.CHAR:
                        return typeof(Char);

                    case TypeData.USHORT:
                        return typeof(ushort);

                    case TypeData.SHORT:
                        return typeof(short);

                    case TypeData.UINT:
                        return typeof(uint);

                    case TypeData.INT:
                        return typeof(int);

                    case TypeData.DOUBLE:
                        return typeof(double);

                    case TypeData.FLOAT:
                        return typeof(float);

                    case TypeData.ShortToReal:
                        return typeof(float);

                    default:
                        throw new ApplicationException("typeData isn't appropriate!");
                }
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.Tag.getSize() :" + Ex.Message);
            }
        }

        string ITag.GetFormatedValue()
        {
            try
            {
                if (string.IsNullOrEmpty(Format_))
                {
                    if (TypeData_ == TypeData.BIT)
                    {
                        if ((bool)value_)
                            return "1";
                        else
                            return "0";
                    }
                    else
                        return value.ToString();
                }
                else if (Format_ == "{0:ASCII}")
                {
                    dynamic d = value_;
                    return Encoding.ASCII.GetString(BitConverter.GetBytes(d));
                }
                else
                {
                    if (TypeData_ == TypeData.BIT)
                    {
                        if ((bool)value_)
                            return "1";
                        else
                            return "0";
                    }
                    else
                        return string.Format(Format, value);
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return value.ToString();
            }
        }

        bool ITag.ActClr
        {
            get { return true; }
        }

        Color ITag.Clr
        {
            get
            {
                return Clr;
            }
            set
            {
                Clr = value;
            }
        }

        bool ITag.ActWidth
        {
            get { return true; }
        }

        int ITag.Width
        {
            get
            {
                return Width;
            }
            set
            {
                Width = value;
            }
        }

        bool ITag.ActGrEnable
        {
            get { return true; }
        }

        bool ITag.GrEnable
        {
            get
            {
                return GrEnable;
            }
            set
            {
                GrEnable = value;
            }
        }

        bool ITag.ActGrVisible
        {
            get { return true; }
        }

        bool ITag.GrVisible
        {
            get
            {
                return GrVisible;
            }
            set
            {
                GrVisible = value;
            }
        }

        bool ITag.ActGrAxisKey
        {
            get { return true; }
        }

        string ITag.GrAxisKey
        {
            get { return GrAxisKey_; }
            set { GrAxisKey_ = value; propChanged?.Invoke(this, new PropertyChangedEventArgs("GrAxisKey")); }
        }

        [Obsolete("Stare")]
        bool ITag.ActGrMarkers
        {
            get { return true; }
        }

        bool ITag.ActGrVisibleTab
        {
            get { return true; }
        }

        bool ITag.GrVisibleTab
        {
            get
            {
                return GrVisibleTab;
            }
            set
            {
                GrVisibleTab = value;
            }
        }

        int ITag.BlockAdress
        {
            get
            {
                return 0;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        bool ITag.ActBlockAdress
        {
            get
            {
                return false;
            }
        }

        BytesOrder ITag.BytesOrder_
        {
            get
            {
                return BytesOrder.ABCD;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        bool ITag.ActBytesOrder_
        {
            get
            {
                return false;
            }
        }

        bool IDriverModel.activateCycle(List<ITag> tagsList)
        {
            return idrv.activateCycle(tagsList);
        }

        bool IDriverModel.addTagsComm(List<ITag> tagList)
        {
            return idrv.addTagsComm(tagList);
        }

        bool IDriverModel.removeTagsComm(List<ITag> tagList)
        {
            return idrv.removeTagsComm(tagList);
        }

        bool IDriverModel.reConfig()
        {
            return idrv.reConfig();
        }

        bool IDriverModel.deactivateCycle()
        {
            return idrv.deactivateCycle();
        }

        string IDriverModel.FormatFrameRequest(byte[] frame, NumberStyles num)
        {
            return idrv.FormatFrameRequest(frame, num);
        }

        string IDriverModel.FormatFrameResponse(byte[] frame, NumberStyles num)
        {
            return idrv.FormatFrameResponse(frame, num);
        }

        byte[] IDriverModel.sendBytes(byte[] data)
        {
            return idrv.sendBytes(data);
        }

        Guid IDriverModel.ObjId
        {
            get
            {
                return idrv.ObjId;
            }
        }

        string IDriverModel.driverName
        {
            get
            {
                return idrv.driverName;
            }
        }

        object IDriverModel.setDriverParam
        {
            get
            {
                return idrv.setDriverParam;
            }

            set
            {
                idrv.setDriverParam = value;
            }
        }

        MemoryAreaInfo[] IDriverModel.MemoryAreaInf
        {
            get
            {
                return idrv.MemoryAreaInf;
            }
        }

        bool IDriverModel.isAlive
        {
            get
            {
                return idrv.isAlive;
            }

            set
            {
                idrv.isAlive = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("isAlive"));
            }
        }

        bool IDriverModel.isBusy
        {
            get
            {
                return idrv.isBusy;
            }
        }

        object[] IDriverModel.plugins
        {
            get
            {
                return idrv.plugins;
            }
        }

        bool[] IDriverModel.AuxParam
        {
            get
            {
                return idrv.AuxParam;
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
                return tagName;
            }

            set
            {
                tagName = value;
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
                return ((IDriverModel)Proj.InternalTagsDrv).isAlive;
            }
            set
            {
                propChanged?.Invoke(this, new PropertyChangedEventArgs("IsLive"));
            }
        }

        bool ITreeViewModel.IsBlocked
        {
            get
            {
                return Proj_.InternalTagsDrv.IsBlocked;
            }
            set
            {
                propChanged?.Invoke(this, new PropertyChangedEventArgs("IsBlocked"));
            }
        }
    }
}