using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class Device : ITreeViewModel, INotifyPropertyChanged, ITableView, IDriverModel, IDriversMagazine
    {
        [field: NonSerialized]
        [XmlIgnore]
        public EventHandler adressChanged;

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

        private Guid projId_;

        [Browsable(false)]
        [XmlElement(ElementName = "ProjectId")]
        public Guid projId
        {
            get { return projId_; }
            set
            {
                projId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(projId)));
            }
        }

        private Guid parentId_;

        [Browsable(false)]
        [XmlElement(ElementName = "Parent")]
        public Guid parentId
        {
            get { return parentId_; }
            set
            {
                parentId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(parentId)));
            }
        }

        private Guid objId_;

        [Browsable(false)]
        [XmlElement(ElementName = "Id")]
        public Guid objId
        {
            get { return objId_; }
            set
            {
                if (PrCon != null)
                {
                    var tgs = from tx in prCon_.getProject(projId_).tagsList where tx.parentId == objId_ select tx;
                    foreach (Tag tx in tgs)
                        tx.parentId = value;
                }

                objId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(objId)));
            }
        }

        [NonSerialized]
        private IDriverModel idrv_;

        [Browsable(false)]
        [XmlIgnore]
        public IDriverModel idrv
        {
            get { return idrv_; }
            set
            {
                idrv_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(idrv)));
            }
        }

        [NonSerialized]
        private ProjectContainer prCon_;

        [Browsable(false)]
        [XmlIgnore]
        public ProjectContainer PrCon
        {
            get { return prCon_; }
            set
            {
                prCon_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrCon)));
            }
        }

        private string name_;

        [Category("01 Design"), DisplayName("Device Name")]
        [XmlElement(ElementName = "Name")]
        public string name
        {
            get { return name_; }
            set
            {
                name_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(name)));
            }
        }

        private ushort adress_;

        [Category("02 Data"), DisplayName("Device Adress")]
        [XmlElement(ElementName = "Adress")]
        [TypeConverter(typeof(BlockConverter))]
        public ushort adress
        {
            get { return adress_; }
            set
            {
                adress_ = value;

                if (adressChanged != null)
                    adressChanged(this, new ProjectEventArgs(adress_));

                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(adress)));
            }
        }

        private Boolean IsExpand_ = true;

        [Browsable(false)]
        public Boolean IsExpand
        {
            get { return IsExpand_; }
            set
            {
                IsExpand_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpand)));
            }
        }

        [field: NonSerialized]
        private ObservableCollection<object> _Children;

        ObservableCollection<object> ITreeViewModel.Children
        {
            get
            {
                return _Children;
            }

            set
            {
                _Children = value;
            }
        }

        string ITreeViewModel.Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        bool ITreeViewModel.IsExpand
        {
            get
            {
                return IsExpand;
            }

            set
            {
                IsExpand = value;
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
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ITreeViewModel.IsLive)));
            }
        }

        bool ITreeViewModel.IsBlocked
        {
            get
            {
                return PrCon.getConnection(projId_, parentId_).IsBlocked;
            }
            set
            {
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ITreeViewModel.IsBlocked)));
            }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }

        [field: NonSerialized]
        private ObservableCollection<ITag> ChildrenTags;

        ObservableCollection<ITag> ITableView.Children
        {
            get
            {
                return ChildrenTags;
            }

            set
            {
                ChildrenTags = value;
            }
        }

        public Device(string name, ushort adresss, ProjectContainer projCon, Guid projId1)
        {
            this.name = name;

            this.projId_ = projId1;

            this.adress_ = adresss;

            this.prCon_ = projCon;

            objId = Guid.NewGuid();

            _Children = new ObservableCollection<object>();
            ChildrenTags = new ObservableCollection<ITag>();
            Children_ = new ObservableCollection<IDriverModel>();
        }

        public Device()
        {
        }

        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;

            Device Dv1 = (Device)bf.Deserialize(ms);
            ms.Close();

            Dv1.objId_ = Guid.NewGuid();
            Dv1.projId_ = Guid.Empty;
            Dv1.parentId_ = Guid.Empty;
            Dv1.prCon_ = prCon_;
            Dv1._Children = new ObservableCollection<object>();
            Dv1.ChildrenTags = new ObservableCollection<ITag>();
            Dv1.Children_ = new ObservableCollection<IDriverModel>();

            return Dv1;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
        }

        Guid IDriverModel.ObjId
        {
            get
            {
                return objId;
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

        event EventHandler IDriverModel.refreshedCycle
        {
            add
            {
                idrv.refreshedCycle += value;
            }

            remove
            {
                idrv.refreshedCycle -= value;
            }
        }

        event EventHandler IDriverModel.refreshedPartial
        {
            add
            {
                idrv.refreshedPartial += value;
            }

            remove
            {
                idrv.refreshedPartial -= value;
            }
        }

        event EventHandler IDriverModel.error
        {
            add
            {
                idrv.error += value;
            }

            remove
            {
                idrv.error -= value;
            }
        }

        event EventHandler IDriverModel.information
        {
            add
            {
                idrv.information += value;
            }

            remove
            {
                idrv.information -= value;
            }
        }

        event EventHandler IDriverModel.dataSent
        {
            add
            {
                idrv.dataSent += value;
            }

            remove
            {
                idrv.dataSent -= value;
            }
        }

        event EventHandler IDriverModel.dataRecived
        {
            add
            {
                idrv.dataRecived += value;
            }

            remove
            {
                idrv.dataRecived -= value;
            }
        }

        [field: NonSerialized]
        private ObservableCollection<IDriverModel> Children_;

        ObservableCollection<IDriverModel> IDriversMagazine.Children
        {
            get
            {
                return Children_;
            }

            set
            {
                Children_ = value;
            }
        }
    }
}