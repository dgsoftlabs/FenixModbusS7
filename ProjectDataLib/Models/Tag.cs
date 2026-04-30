using Newtonsoft.Json;
using System;
using System.Collections;
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
    public class Tag : IComparable<Tag>, ITag, INotifyPropertyChanged, IDriverModel, ITreeViewModel
    {
        public event PropertyChangedEventHandler propChanged;

        public event EventHandler refreshedCycle;

        public event EventHandler refreshedPartial;

        public event EventHandler error;

        public event EventHandler information;

        public event EventHandler dataSent;

        public event EventHandler dataRecived;

        [field: NonSerialized]
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
        private Project Proj_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public Project Proj
        {
            get { return Proj_; }
            set { Proj_ = value; }
        }

        private Guid parentId_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlElement(ElementName = "Parent")]
        public Guid parentId
        {
            get { return parentId_; }
            set { parentId_ = value; }
        }

        private Guid connId_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlElement(ElementName = "ConnectionId")]
        public Guid connId
        {
            get { return connId_; }
            set { connId_ = value; }
        }

        private Guid objId_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlElement(ElementName = "Id")]
        public Guid objId
        {
            get { return objId_; }
            set { objId_ = value; }
        }

        private string tagName_;

        [Category("01 Design"), DisplayName("Tag Name")]
        [XmlElement(ElementName = "Name")]
        public string tagName
        {
            get { return tagName_; }
            set
            {
                if (Proj_?.tagsList.Where(x => x.tagName == value).Count<Tag>() > 0 || Proj_?.InTagsList.Where(x => x.tagName == value).Count<InTag>() > 0)
                    tagName_ = tagName_ + "$$";
                else
                    tagName_ = value;

                propChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
            }
        }

        private TypeData TypeData_;

        [Category("03 Data"), DisplayName("Value Type")]
        [JsonIgnore]
        public TypeData TypeData
        {
            get { return TypeData_; }
            set
            {
                TypeData_ = value;

                if (idrv != null)
                {
                    MemoryAreaInfo mInf = idrv_?.MemoryAreaInf.Where(x => x.Name.Equals(this.areaData_)).ToArray()[0];

                    this.coreData_ = new Boolean[getSize() < mInf.AdresSize ? mInf.AdresSize : getSize()];

                    this.coreDataSend_ = new Boolean[coreData_.Length];

                    this.bitAdres_ = (this.startData_ * mInf.AdresSize);

                    int[] war = this.getScAdressPos();
                    if (!war.Contains(this.scAdres_))
                        this.scAdres_ = war[0];
                }

                ResetValue();

                propChanged?.Invoke(this, new PropertyChangedEventArgs("TypeData_"));
            }
        }

        private int BlockAdress_;

        [Category("02 Details"), DisplayName("DBAdress"), Browsable(true)]
        [JsonIgnore]
        [XmlElement(ElementName = "DBAdress")]
        [TypeConverter(typeof(BlockAdressVisibility))]
        public int BlockAdress
        {
            get { return BlockAdress_; }
            set
            {
                BlockAdress_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("BlockAdress"));
            }
        }

        private string areaData_;

        [Category("02 Details"), DisplayName("Memory Area")]
        [TypeConverter(typeof(MemoryAreaModbus))]
        [XmlElement(ElementName = "MemoryArea")]
        public string areaData
        {
            get { return areaData_; }
            set
            {
                areaData_ = value;

                if (idrv != null)
                {
                    MemoryAreaInfo mInf = idrv_.MemoryAreaInf.Where(x => x.Name.Equals(this.areaData_)).ToArray()[0];

                    this.coreData_ = new Boolean[getSize() < mInf.AdresSize ? mInf.AdresSize : getSize()];

                    this.coreDataSend_ = new Boolean[coreData_.Length];

                    this.bitAdres_ = (this.startData_ * mInf.AdresSize);

                    int[] war = this.getScAdressPos();
                    if (!war.Contains(this.scAdres_))
                        this.scAdres_ = war[0];
                }

                propChanged?.Invoke(this, new PropertyChangedEventArgs("AreaData"));
                propChanged?.Invoke(this, new PropertyChangedEventArgs("BitByte"));
            }
        }

        private int startData_;

        [Category("02 Details"), DisplayName("Adress")]
        [XmlElement(ElementName = "Adress")]
        public int startData
        {
            get { return startData_; }
            set
            {
                startData_ = value;

                if (idrv != null)
                {
                    MemoryAreaInfo mInf = idrv_.MemoryAreaInf.Where(x => x.Name.Equals(this.areaData_)).ToArray()[0];

                    this.bitAdres_ = (this.startData_ * mInf.AdresSize);
                }

                propChanged?.Invoke(this, new PropertyChangedEventArgs("Adress"));
            }
        }

        private int bitAdres_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public int bitAdres
        {
            get { return bitAdres_; }
            set { bitAdres_ = value; }
        }

        private ushort deviceAdress_;

        [Browsable(false)]
        [XmlElement(ElementName = "DeviceAdress")]
        public ushort deviceAdress
        {
            get { return deviceAdress_; }
            set
            {
                deviceAdress_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("DevAdress"));
            }
        }

        private int scAdres_;

        [Category("02 Details"), DisplayName("Offset[bit/byte]")]
        [XmlElement("Offset")]
        [TypeConverter(typeof(TagSecendaryAdress))]
        public int scAdres
        {
            get { return scAdres_; }
            set
            {
                scAdres_ = value;

                int[] war = getScAdressPos();

                if (war != null)
                {
                    if (!war.Contains(this.scAdres_))
                        this.scAdres_ = war[0];
                }

                propChanged?.Invoke(this, new PropertyChangedEventArgs("BitByte"));
            }
        }

        private BytesOrder bytesOrder_;

        [Category("02 Details"), DisplayName("Bytes Order"), Browsable(true)]
        [JsonIgnore]
        [XmlElement(ElementName = "BytesOrder")]
        public BytesOrder bytesOrder
        {
            get { return bytesOrder_; }
            set
            {
                bytesOrder_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("BytesOrder_"));
            }
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

        private Object value_;

        [Browsable(false)]
        [XmlIgnore]
        public object value
        {
            get
            {
                return value_;
            }

            set
            {
                value_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }
        }

        private string ReadScript_;

        [CusEventProperty]
        [Category("04 Script"), DisplayName("Execute After Read"), Browsable(true)]
        [Editor(typeof(ScEditor), typeof(UITypeEditor))]
        [JsonIgnore]
        public string ReadScript
        {
            get { return ReadScript_; }
            set { ReadScript_ = value; }
        }

        private string WriteScript_;

        [CusEventProperty]
        [Category("04 Script"), DisplayName("Execute Before Write"), Browsable(true)]
        [Editor(typeof(ScEditor), typeof(UITypeEditor))]
        [JsonIgnore]
        public string WriteScript
        {
            get { return WriteScript_; }
            set { WriteScript_ = value; }
        }

        private Boolean[] coreData_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public Boolean[] coreData
        {
            get { return coreData_; }
            set { coreData_ = value; }
        }

        private Boolean[] coreDataSend_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public Boolean[] coreDataSend
        {
            get { return coreDataSend_; }
            set { coreDataSend_ = value; }
        }

        [JsonIgnore]
        [XmlIgnore]
        public Boolean setMod;

        [NonSerialized]
        private IDriverModel idrv_;

        [Browsable(false)]
        [JsonIgnore]
        [XmlIgnore]
        public IDriverModel idrv
        {
            get { return idrv_; }
            set { idrv_ = value; }
        }

        public Tag(string tagName, BytesOrder bOrder, string areaData, int startData, int secondAdress, string desribe, TypeData typeData, IDriverModel idr, Guid connId, int blAdres)
        {
            try
            {
                this.tagName_ = tagName;

                this.areaData_ = areaData;

                this.startData_ = startData;

                this.scAdres_ = secondAdress;

                this.BlockAdress_ = blAdres;

                this.bytesOrder_ = bOrder;

                this.TypeData_ = typeData;

                this.describe_ = desribe;

                this.connId_ = connId;

                objId = Guid.NewGuid();

                setMod = false;

                MemoryAreaInfo aInfo = idr.MemoryAreaInf.Where(x => x.Name.Equals(this.areaData_)).ToArray()[0];

                this.coreData_ = new Boolean[getSize() < aInfo.AdresSize ? aInfo.AdresSize : getSize()];

                this.coreDataSend_ = new Boolean[coreData_.Length];

                this.bitAdres_ = (this.startData_ * aInfo.AdresSize);

                Format_ = "{0:0.00}";

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

        public Tag()
        {
        }

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

        public override bool Equals(object obj)
        {
            try
            {
                if (obj is IDriverModel)
                    return Guid.Equals(this.objId, ((ITag)obj).Id);
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this.objId.GetHashCode();
        }

        public int CompareTo(Tag b)
        {
            return this.startData.CompareTo(b.startData);
        }

        public void refreshData()
        {
            try
            {
                Boolean[] dane1 = OrderBytesFct(this.bytesOrder_, ref coreData_);

                byte[] arrByte = new byte[dane1.Length / 8 == 0 ? 1 : dane1.Length / 8];
                BitArray btAr = new BitArray(dane1);

                btAr.CopyTo(arrByte, 0);

                Boolean ScriptMark = false;
                if (!String.IsNullOrEmpty(ReadScript_))
                {
                    ScriptMark = true;
                }
                else
                    ScriptMark = false;

                switch (this.TypeData_)
                {
                    case ProjectDataLib.TypeData.BIT:
                        value = (Boolean)dane1[scAdres_];
                        break;

                    case ProjectDataLib.TypeData.BYTE:

                        if (!ScriptMark)
                        {
                            value = arrByte[scAdres_];
                        }
                        else
                        {
                            value = arrByte[scAdres_];
                            string code = ReadScript_.Replace("x", String.Format("{0:0}", value)).Replace(',', '.');
                            value = (byte)Proj_.ScriptCon.Eval(code);
                        }

                        break;

                    case ProjectDataLib.TypeData.SBYTE:
                        if (!ScriptMark)
                        {
                            value = (sbyte)arrByte[scAdres_];
                        }
                        else
                        {
                            value = (sbyte)arrByte[scAdres_];
                            string code = ReadScript_.Replace("x", String.Format("{0:0}", value)).Replace(',', '.');
                            value = (sbyte)Proj_.ScriptCon.Eval(code);
                        }
                        break;

                    case ProjectDataLib.TypeData.CHAR:
                        value = BitConverter.ToChar(arrByte, 0);
                        break;

                    case ProjectDataLib.TypeData.SHORT:

                        if (!ScriptMark)
                        {
                            value = BitConverter.ToInt16(arrByte, 0);
                        }
                        else
                        {
                            value = BitConverter.ToInt16(arrByte, 0);
                            string code = ReadScript_.Replace("x", String.Format("{0:0}", value)).Replace(',', '.');
                            value = (short)Proj_.ScriptCon.Eval(code);
                        }
                        break;

                    case ProjectDataLib.TypeData.USHORT:

                        if (!ScriptMark)
                        {
                            value = BitConverter.ToUInt16(arrByte, 0);
                        }
                        else
                        {
                            value = BitConverter.ToUInt16(arrByte, 0);
                            string code = ReadScript_.Replace("x", String.Format("{0:0}", value)).Replace(',', '.');
                            value = (ushort)Proj_.ScriptCon.Eval(code);
                        }

                        break;

                    case ProjectDataLib.TypeData.INT:

                        if (!ScriptMark)
                        {
                            value = BitConverter.ToInt32(arrByte, 0);
                        }
                        else
                        {
                            value = BitConverter.ToInt32(arrByte, 0);
                            string code = ReadScript_.Replace("x", String.Format("{0:0}", value)).Replace(',', '.');
                            value = (int)Proj_.ScriptCon.Eval(code);
                        }
                        break;

                    case ProjectDataLib.TypeData.UINT:

                        if (!ScriptMark)
                        {
                            value = BitConverter.ToUInt32(arrByte, 0);
                        }
                        else
                        {
                            value = BitConverter.ToUInt32(arrByte, 0);
                            string code = ReadScript_.Replace("x", String.Format("{0:0}", value)).Replace(',', '.');
                            value = (uint)Proj_.ScriptCon.Eval(code);
                        }
                        break;

                    case ProjectDataLib.TypeData.FLOAT:

                        if (!ScriptMark)
                        {
                            value = BitConverter.ToSingle(arrByte, 0);
                        }
                        else
                        {
                            value = BitConverter.ToSingle(arrByte, 0);
                            string code = ReadScript_.Replace("x", String.Format("{0:0.000000}", value)).Replace(',', '.');
                            value = Convert.ToSingle(Proj_.ScriptCon.Eval(code));
                            break;
                        }
                        break;

                    case ProjectDataLib.TypeData.DOUBLE:

                        if (!ScriptMark)
                        {
                            value = BitConverter.ToDouble(arrByte, 0);
                        }
                        else
                        {
                            value = BitConverter.ToDouble(arrByte, 0);
                            string code = ReadScript_.Replace("x", String.Format("{0:0.000000}", value)).Replace(',', '.');
                            value = (double)Proj_.ScriptCon.Eval(code);
                        }

                        break;

                    case ProjectDataLib.TypeData.ShortToReal:

                        if (!ScriptMark)
                        {
                            value = BitConverter.ToInt16(arrByte, 0);
                            value = double.Parse(value.ToString());
                            break;
                        }
                        else
                        {
                            value = BitConverter.ToInt16(arrByte, 0);
                            value = float.Parse(value.ToString());
                            string code = ReadScript_.Replace("x", String.Format("{0:0.000000}", value)).Replace(',', '.');
                            value_ = Convert.ToSingle(Proj_.ScriptCon.Eval(code));
                        }

                        break;
                }
            }
            catch (Exception Ex)
            {
                throw new ApplicationException(this.tagName_ + " " + Ex.Message);
            }
        }

        public void setValueMethod(Object obj)
        {
            lock (this)
            {
                if (obj == null)
                    return;

                setMod = true;

                Array.Copy(coreData_, coreDataSend_, coreData_.Length);
                BitArray biArr;
                byte[] btArr;

                Boolean ScriptMark = !String.IsNullOrEmpty(WriteScript_);

                switch (TypeData_)
                {
                    case ProjectDataLib.TypeData.BIT:
                        if (obj.GetType() == typeof(string))
                        {
                            if (obj.ToString() == "1")
                                obj = true;
                            else if (obj.ToString() == "0")
                                obj = false;
                            else
                                obj = Convert.ToBoolean(obj);
                        }

                        coreDataSend_[scAdres_] = (Boolean)obj;
                        break;

                    case ProjectDataLib.TypeData.BYTE:
                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToByte(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (byte)Proj_.ScriptCon.Eval(code);
                        }

                        biArr = new BitArray(coreDataSend_);
                        btArr = new byte[biArr.Length / 8];
                        biArr.CopyTo(btArr, 0);

                        btArr[scAdres_] = (byte)obj;
                        biArr = new BitArray(btArr);
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.SBYTE:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToSByte(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (sbyte)Proj_.ScriptCon.Eval(code);
                        }

                        biArr = new BitArray(coreDataSend_);
                        btArr = new byte[biArr.Length / 8];
                        biArr.CopyTo(btArr, 0);

                        Buffer.BlockCopy(new sbyte[] { (sbyte)obj }, 0, btArr, scAdres_, 1);

                        biArr = new BitArray(btArr);
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.CHAR:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToChar(obj);

                        biArr = new BitArray(BitConverter.GetBytes((Char)obj));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.DOUBLE:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToDouble(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (double)Proj_.ScriptCon.Eval(code);
                        }
                        biArr = new BitArray(BitConverter.GetBytes((Double)obj));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.FLOAT:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToSingle(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = Convert.ToSingle(Proj_.ScriptCon.Eval(code));
                        }

                        biArr = new BitArray(BitConverter.GetBytes(Convert.ToSingle(obj)));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.INT:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToInt32(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (int)Proj_.ScriptCon.Eval(code);
                        }
                        biArr = new BitArray(BitConverter.GetBytes((Int32)obj));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.UINT:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToUInt32(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (uint)Proj_.ScriptCon.Eval(code);
                        }
                        biArr = new BitArray(BitConverter.GetBytes((UInt32)obj));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.SHORT:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToInt16(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (short)Proj_.ScriptCon.Eval(code);
                        }
                        biArr = new BitArray(BitConverter.GetBytes((short)obj));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.USHORT:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToUInt16(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (ushort)Proj_.ScriptCon.Eval(code);
                        }
                        biArr = new BitArray(BitConverter.GetBytes((ushort)obj));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;

                    case ProjectDataLib.TypeData.ShortToReal:

                        if (obj.GetType() == typeof(string))
                            obj = Convert.ToDouble(obj);

                        if (ScriptMark)
                        {
                            string code = WriteScript_.Replace("x", String.Format("{0:0}", obj)).Replace(',', '.');
                            obj = (double)Proj_.ScriptCon.Eval(code);
                        }
                        short buff = Convert.ToInt16(obj);
                        biArr = new BitArray(BitConverter.GetBytes(buff));
                        biArr.CopyTo(coreDataSend_, 0);
                        break;
                }

                this.coreDataSend_ = OrderBytesFct(this.bytesOrder_, ref coreDataSend_);
            }
        }

        public TagAdress nextAdress()
        {
            try
            {
                MemoryAreaInfo mInf = idrv_.MemoryAreaInf.Where(x => x.Name.Equals(this.areaData_)).ToArray()[0];

                switch (this.TypeData_)
                {
                    case TypeData.BIT:
                        if (mInf.AdresSize == 1)
                        {
                            return new TagAdress(this.startData + 1, 0);
                        }
                        else
                        {
                            if (scAdres_ < mInf.AdresSize - 1)
                                return new TagAdress(this.startData, scAdres_ + 1);
                            else
                                return new TagAdress(this.startData + 1, 0);
                        }

                    case TypeData.SBYTE:

                        if (mInf.AdresSize == 1)
                        {
                            return new TagAdress(this.startData + 8, 0);
                        }
                        else
                        {
                            if (scAdres_ < (mInf.AdresSize / 8) - 1)
                                return new TagAdress(this.startData, scAdres_ + 1);
                            else
                                return new TagAdress(this.startData + 1, 0);
                        }

                    case TypeData.BYTE:
                        if (mInf.AdresSize == 1)
                        {
                            return new TagAdress(this.startData + 8, 0);
                        }
                        else
                        {
                            if (scAdres_ < (mInf.AdresSize / 8) - 1)
                                return new TagAdress(this.startData, scAdres_ + 1);
                            else
                                return new TagAdress(this.startData + 1, 0);
                        }

                    case TypeData.CHAR:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    case TypeData.USHORT:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    case TypeData.SHORT:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    case TypeData.UINT:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    case TypeData.INT:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    case TypeData.DOUBLE:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    case TypeData.FLOAT:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    case TypeData.ShortToReal:
                        return new TagAdress(this.startData + (getSize() / (mInf.AdresSize)), 0);

                    default:
                        throw new ApplicationException("This kind od DataType in unsed!");
                }
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.Tag.nextAdress() :" + Ex.Message);
            }
        }

        public int[] getScAdressPos()
        {
            List<int> buffor = new List<int>();

            if (idrv == null)
                return null;

            MemoryAreaInfo info = idrv_?.MemoryAreaInf.Where(x => x.Name == this.areaData_).ToList()[0];

            switch (this.TypeData_)
            {
                case TypeData.BIT:
                    for (int i = 0; i < info.AdresSize; i++)
                        buffor.Add(i);
                    break;

                case TypeData.BYTE:
                    for (int i = 0; i < info.AdresSize / 8; i++)
                        buffor.Add(i);
                    break;

                case TypeData.SBYTE:
                    for (int i = 0; i < info.AdresSize / 8; i++)
                        buffor.Add(i);
                    break;

                case TypeData.CHAR:
                    for (int i = 0; i < info.AdresSize / 16; i++)
                        buffor.Add(i);
                    break;

                case TypeData.SHORT:
                    for (int i = 0; i < info.AdresSize / 16; i++)
                        buffor.Add(i);
                    break;

                case TypeData.USHORT:
                    for (int i = 0; i < info.AdresSize / 16; i++)
                        buffor.Add(i);
                    break;

                case TypeData.INT:
                    for (int i = 0; i < info.AdresSize / 32; i++)
                        buffor.Add(i);
                    break;

                case TypeData.UINT:
                    for (int i = 0; i < info.AdresSize / 32; i++)
                        buffor.Add(i);
                    break;

                case TypeData.FLOAT:
                    for (int i = 0; i < info.AdresSize / 32; i++)
                        buffor.Add(i);
                    break;

                case TypeData.DOUBLE:
                    for (int i = 0; i < info.AdresSize / 64; i++)
                        buffor.Add(i);
                    break;
            }

            if (buffor.Count == 0)
                buffor.Add(0);

            return buffor.ToArray();
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

        private string Format_;

        [Category("05 Appearance"), DisplayName("Format Value")]
        [JsonIgnore]
        [TypeConverter(typeof(FormatValueConv))]
        public string Format
        {
            get { return Format_; }
            set { Format_ = value; }
        }

        private Boolean[] OrderBytesFct(BytesOrder BytesOrder_, ref Boolean[] data)
        {
            try
            {
                switch (bytesOrder_)
                {
                    case BytesOrder.BADC:
                        if (data.Length > 15)
                        {
                            for (int i = 0; i < data.Length; i = i + 16)
                            {
                                Boolean[] b1 = data.ToList().GetRange(i, 8).ToArray();

                                Boolean[] b2 = data.ToList().GetRange(i + 8, 8).ToArray();

                                Array.Copy(b2, 0, data, i, 8);

                                Array.Copy(b1, 0, data, i + 8, 8);
                            }

                            return data;
                        }
                        else
                            return data;

                    case BytesOrder.ABCD:
                        return data;

                    case BytesOrder.DCBA:
                        byte[] dDane = new byte[data.Length / 8 == 1 ? 1 : data.Length / 8];
                        BitArray btArr = new BitArray(data);
                        btArr.CopyTo(dDane, 0);
                        dDane = dDane.Reverse().ToArray();

                        btArr = new BitArray(dDane);
                        btArr.CopyTo(data, 0);
                        return data;

                    default:
                        return data;
                }
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectDataLib.Tag.OrderBytesFct :" + Ex.Message);
            }
        }

        private Color Clr_;

        [Category("06 Graph"), DisplayName("Color")]
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

        [XmlElement(ElementName = "Color")]
        [Browsable(false)]
        public XmlColor ClrXml
        {
            get => Clr_;
            set => Clr = value;
        }

        private int Width_;

        [Category("06 Graph"), DisplayName("Line Width")]
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

        [Category("06 Graph"), DisplayName("Enable")]
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

        [Category("06 Graph"), DisplayName("Visible (Chart)")]
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

        [Category("06 Graph"), DisplayName("Y Axis")]
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

        [Category("07 Table"), DisplayName("Visible (Table)")]
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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            try
            {
                setMod = false;
                ResetValue();

                if (Clr_ == null)
                    Clr_ = Color.Black;

                if (Width_ == 0)
                    Width_ = 2;
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        public void OnDeserializedXml()
        {
            try
            {
                setMod = false;
                ResetValue();

                if (Clr_ == null)
                    Clr_ = Color.Black;

                if (Width_ == 0)
                    Width_ = 2;

                if (idrv != null)
                {
                    MemoryAreaInfo mInf = idrv_?.MemoryAreaInf.Where(x => x.Name.Equals(this.areaData_)).ToArray()[0];

                    this.coreData_ = new Boolean[getSize() < mInf.AdresSize ? mInf.AdresSize : getSize()];

                    this.coreDataSend_ = new Boolean[coreData_.Length];

                    this.bitAdres_ = (this.startData_ * mInf.AdresSize);

                    int[] war = this.getScAdressPos();
                    if (!war.Contains(this.scAdres_))
                        this.scAdres_ = war[0];
                }
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

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

        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;
            Tag Tg1 = (Tag)bf.Deserialize(ms);
            ms.Close();

            Tg1.objId_ = Guid.NewGuid();
            Tg1.parentId_ = Guid.Empty;
            Tg1.connId_ = Guid.Empty;
            Tg1.projCon_ = projCon_;
            Tg1.Proj_ = Proj_;

            return Tg1;
        }

        Guid ITag.Id
        {
            get { return objId; }
            set
            {
                objId = value;
            }
        }

        Guid ITag.ParentId
        {
            get { return parentId; }
            set
            {
                parentId = value;
            }
        }

        string ITag.Name
        {
            get { return tagName; }
            set
            {
                tagName = value;
            }
        }

        Boolean ITag.ActName
        {
            get { return true; }
        }

        int ITag.DevAdress
        {
            get { return deviceAdress; }
            set
            {
                deviceAdress = (ushort)value;
            }
        }

        Boolean ITag.ActDevAdress { get { return true; } }

        int ITag.Adress
        {
            get { return startData; }
            set
            {
                startData = value;
            }
        }

        Boolean ITag.ActAdress { get { return true; } }

        int ITag.BitByte
        {
            get { return scAdres; }
            set
            {
                scAdres = value;
            }
        }

        Boolean ITag.ActBitByte { get { return true; } }

        TypeData ITag.TypeData_
        {
            get { return TypeData; }
            set
            {
                TypeData = value;
            }
        }

        Boolean ITag.ActTypeData_ { get { return true; } }

        string ITag.AreaData
        {
            get { return areaData; }
            set
            {
                areaData = value;
            }
        }

        Boolean ITag.ActAreaData { get { return true; } }

        Object ITag.Value
        {
            get { return value; }
            set
            {
                this.value = value;
            }
        }

        Boolean ITag.ActValue { get { return true; } }

        string ITag.Description
        {
            get { return tagVersion; }
            set
            {
                describe = value;
            }
        }

        Boolean ITag.ActDscription { get { return true; } }

        string ITag.TagVersion
        {
            get { return tagVersion; }
            set { tagVersion = value; }
        }

        Boolean ITag.ActVersion { get { return true; } }

        Boolean ITag.ActParam { get { return false; } }

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

        Boolean ITag.ActSetValue
        {
            get { return true; }
        }

        void ITag.SetValue(Object val)
        {
            try
            {
                this.setValueMethod(val);
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.Tag.SetValue() :" + Ex.Message);
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

        bool ITag.ActWidth
        {
            get { return true; }
        }

        bool ITag.ActGrEnable
        {
            get { return true; }
        }

        bool ITag.ActGrVisible
        {
            get { return true; }
        }

        bool ITag.ActGrAxisKey
        {
            get { return true; }
        }

        string ITag.GrAxisKey
        {
            get { return GrAxisKey; }
            set { GrAxisKey = value; }
        }

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

        bool ITag.ActBlockAdress
        {
            get
            {
                return idrv_.AuxParam[1];
            }
        }

        BytesOrder ITag.BytesOrder_
        {
            get
            {
                return bytesOrder;
            }

            set
            {
                bytesOrder = value;
            }
        }

        bool ITag.ActBytesOrder_
        {
            get
            {
                return true;
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
                return idrv_.isAlive;
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
                return PrCon.getConnection(Proj_.objId, connId).IsBlocked;
            }
            set
            {
                propChanged?.Invoke(this, new PropertyChangedEventArgs("IsBlocked"));
            }
        }
    }
}