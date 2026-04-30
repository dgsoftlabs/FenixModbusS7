using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class InternalTagsDriver : IDriverModel, ITreeViewModel, INotifyPropertyChanged
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

        private Guid objId_;

        [Browsable(false)]
        public Guid objId
        {
            get { return objId_; }
            set
            {
                objId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(objId)));
            }
        }

        [field: NonSerialized]
        private Project Proj_;

        [Browsable(false)]
        [XmlIgnore]
        public Project Proj
        {
            get { return Proj_; }
            set
            {
                Proj_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Proj)));
            }
        }

        private Boolean isExpand_;

        [Browsable(false)]
        public Boolean isExpand
        {
            get { return isExpand_; }
            set
            {
                isExpand_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(isExpand)));
            }
        }

        private List<CustomTimer> Timers_;

        [Category("01 Design"), DisplayName("Timers")]
        [TypeConverter(typeof(EmptyConverter))]
        public List<CustomTimer> Timers
        {
            get { return Timers_; }
            set
            {
                Timers_ = value;
            }
        }

        [Browsable(false)]
        public bool isTimersFolderExpand { get; set; } = true;

        private bool IsBlocked_;

        [Category("01 Design"), DisplayName("IsBlocked")]
        public bool IsBlocked
        {
            get { return IsBlocked_; }
            set
            {
                IsBlocked_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBlocked)));

                if (_Children != null)
                    foreach (ITreeViewModel itv in _Children)
                        itv.IsBlocked = value;
            }
        }

        public InternalTagsDriver(Project pr)
        {
            this.Proj_ = pr;
            this.objId_ = new Guid("22222222-2222-2222-2222-222222222222");

            Timers_ = new List<CustomTimer>();
            Timers_.Add(new CustomTimer());
        }

        public InternalTagsDriver()
        {
        }

        [field: NonSerialized]
        private List<InTag> TagList_;

        [Browsable(false)]
        [XmlIgnore]
        public List<InTag> TagList
        {
            get { return TagList_; }
            set { TagList_ = value; }
        }

        [field: NonSerialized]
        private List<System.Threading.Timer> BckTimers = new List<System.Threading.Timer>();

        [field: NonSerialized]
        private EventHandler sendInfoEv;

        [field: NonSerialized]
        private EventHandler errorSendEv;

        [field: NonSerialized]
        private EventHandler refreshedPartial;

        [field: NonSerialized]
        private EventHandler refreshCycleEv;

        [field: NonSerialized]
        private EventHandler sendLogInfoEv;

        [field: NonSerialized]
        private EventHandler reciveLogInfoEv;

        private Boolean isLive;

        string IDriverModel.driverName
        {
            get { return "InternalTags"; }
        }

        object IDriverModel.setDriverParam
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool IDriverModel.activateCycle(List<ITag> tagsList)
        {
            try
            {
                TagList_ = (from c in tagsList where c is InTag select (InTag)c).ToList();

                List<CustomTimer> CusTm = (from tk in Proj_.InternalTagsDrv.Timers where Proj_.InTagsList.FindAll(x => x.TimerName == tk.Name).ToList().Count > 0 select tk).ToList();

                BckTimers.Clear();
                foreach (var ti in CusTm)
                {
                    List<InTag> tgs = (from tt in Proj_.InTagsList where tt.TimerName == ti.Name select tt).ToList();
                    BckTimers.Add(new System.Threading.Timer(TimerTask, tgs, ti.Delay, ti.Time));
                }

                isLive = true;

                foreach (IDriverModel it in this.TagList)
                    it.isAlive = true;

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        bool IDriverModel.addTagsComm(List<ITag> tagList)
        {
            try
            {
                List<InTag> tgs = (from c in tagList where c is InTag select (InTag)c).ToList();
                this.TagList_.AddRange(tgs);
                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        bool IDriverModel.removeTagsComm(List<ITag> tagList)
        {
            try
            {
                List<InTag> tgs = (from c in tagList where c is InTag select (InTag)c).ToList();

                foreach (InTag tg in tgs)
                    this.TagList_.RemoveAll(x => x.objId == tg.objId);

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        bool IDriverModel.reConfig()
        {
            try
            {
                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        bool IDriverModel.deactivateCycle()
        {
            try
            {
                foreach (var tm in BckTimers)
                    tm.Dispose();

                BckTimers.Clear();

                isLive = false;

                if (TagList != null)
                {
                    foreach (IDriverModel it in this.TagList)
                        it.isAlive = false;
                }

                return true;
            }
            catch (Exception Ex)
            {
                if (errorSendEv != null)
                    errorSendEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message, Ex));

                return false;
            }
        }

        private void TimerTask(object StateObj)
        {
            try
            {
                foreach (InTag tg in (List<InTag>)StateObj)
                {
                    tg.value = Proj_.ScriptCon.Eval(tg.WriteScript);
                    refreshedPartial?.Invoke(this, new ProjectEventArgs(tg));
                }

                refreshCycleEv?.Invoke(this, new ProjectEventArgs(StateObj));
            }
            catch (Exception Ex)
            {
                if (sendInfoEv != null)
                    sendInfoEv(this, new ProjectEventArgs(new byte[] { 0 }, DateTime.Now, Ex.Message));
            }
        }

        event EventHandler IDriverModel.refreshedCycle
        {
            add { refreshCycleEv += value; }
            remove { refreshCycleEv -= value; }
        }

        event EventHandler IDriverModel.refreshedPartial
        {
            add { refreshedPartial += value; }
            remove { refreshedPartial -= value; }
        }

        event EventHandler IDriverModel.error
        {
            add { errorSendEv += value; }
            remove { errorSendEv -= value; }
        }

        event EventHandler IDriverModel.information
        {
            add { sendInfoEv += value; }
            remove { sendInfoEv -= value; }
        }

        event EventHandler IDriverModel.dataSent
        {
            add { sendLogInfoEv += value; }
            remove { sendLogInfoEv -= value; }
        }

        event EventHandler IDriverModel.dataRecived
        {
            add { reciveLogInfoEv += value; }
            remove { reciveLogInfoEv -= value; }
        }

        MemoryAreaInfo[] IDriverModel.MemoryAreaInf
        {
            get { throw new NotImplementedException(); }
        }

        string IDriverModel.FormatFrameRequest(byte[] frame, NumberStyles num)
        {
            return string.Empty;
        }

        string IDriverModel.FormatFrameResponse(byte[] frame, NumberStyles num)
        {
            return string.Empty;
        }

        bool IDriverModel.isAlive
        {
            get { return isLive; }
            set
            {
                isLive = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IDriverModel.isAlive)));
            }
        }

        bool IDriverModel.isBusy
        {
            get
            {
                return false;
            }
        }

        byte[] IDriverModel.sendBytes(byte[] data)
        {
            throw new NotImplementedException();
        }

        object[] IDriverModel.plugins
        {
            get { throw new NotImplementedException(); }
        }

        bool[] IDriverModel.AuxParam
        {
            get { throw new NotImplementedException(); }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            BckTimers = new List<System.Threading.Timer>();
            isLive = false;
        }

        Guid IDriverModel.ObjId
        {
            get
            {
                return objId_;
            }
        }

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
                return "Internal Tags";
            }

            set
            {
            }
        }

        bool ITreeViewModel.IsExpand
        {
            get
            {
                return isExpand;
            }

            set
            {
                isExpand = value;
            }
        }

        bool ITreeViewModel.IsLive
        {
            get
            {
                return isLive;
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
                return IsBlocked;
            }
            set
            {
                IsBlocked = value;
            }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }
    }
}