using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class Project : IDisposable, ITreeViewModel, INotifyPropertyChanged, ITableView, IDriversMagazine
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

        [field: NonSerialized]
        private ObservableCollection<object> TreeViewChildren_;

        ObservableCollection<object> ITreeViewModel.Children
        {
            get
            {
                return TreeViewChildren_;
            }

            set
            {
                TreeViewChildren_ = value;
            }
        }

        [field: NonSerialized]
        private ObservableCollection<ITag> TagChildren;

        ObservableCollection<ITag> ITableView.Children
        {
            get
            {
                return TagChildren;
            }

            set
            {
                TagChildren = value;
            }
        }

        [field: NonSerialized]
        private ObservableCollection<IDriverModel> DriverChildren_;

        ObservableCollection<IDriverModel> IDriversMagazine.Children
        {
            get
            {
                return DriverChildren_;
            }

            set
            {
                DriverChildren_ = value;
            }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }

        private Guid objId_;

        [Browsable(false)]
        [XmlElement(ElementName = "Id")]
        public Guid objId
        {
            get { return objId_; }
            set
            {
                objId_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(objId)));
            }
        }

        [Category("06 Formats"), DisplayName("DateTime Long"), Description("Format for display")]
        [Browsable(true)]
        [XmlElement(ElementName = "DBDateTimeFormat")]
        public string longDT { get; set; }

        private string projectName_;

        [Category("01 Design"), DisplayName("Project Name"), Description("Current project name")]
                [XmlElement(ElementName = "Name")]
        public string projectName
        {
            get
            {
                return projectName_;
            }
            set
            {
                projectName_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(projectName)));
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ITreeViewModel.Name)));
                modificationApear();
            }
        }

        private Version fileVer_;

        [Category("02 Header"), DisplayName("Version"), Description("Version of files.")]
                [XmlIgnore]
        public Version fileVer
        {
            get { return fileVer_; }
            set
            {
                fileVer_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(fileVer)));
                modificationApear();
            }
        }

        [XmlElement(ElementName = "FileVersion")]
        [Browsable(false)]
        public XMLVersion fileVerXml
        {
            get => fileVer_;
            set => fileVer_ = value;
        }

        private string autor_;

        [Category("03 Information"), DisplayName("Autor")]
                [XmlElement(ElementName = "Autor")]
        public string autor
        {
            get { return autor_; }
            set
            {
                autor_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(autor)));
                modificationApear();
            }
        }

        private string company_;

        [Category("03 Information"), DisplayName("Company")]
                [XmlElement(ElementName = "Company")]
        public string company
        {
            get { return company_; }
            set
            {
                company_ = value;
                modificationApear();
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(company)));
            }
        }

        private string describe_;

        [Category("05 Misc"), DisplayName("Description")]
                [XmlElement(ElementName = "Description")]
        public string describe
        {
            get { return describe_; }
            set
            {
                describe_ = value;
                modificationApear();
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(describe)));
            }
        }

        private DateTime createTime_;

        [Category("04 Time"), DisplayName("Create Time"), ReadOnly(true)]
                [XmlElement(ElementName = "Created")]
        public DateTime createTime
        {
            get { return createTime_; }
            set { createTime_ = value; }
        }

        private DateTime modifeTime_;

        [Category("04 Time"), ReadOnly(true), DisplayName("Modification Time")]
                [XmlElement(ElementName = "LastModification")]
        public DateTime modifeTime
        {
            get { return modifeTime_; }
            set
            {
                modifeTime_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(modifeTime)));
            }
        }

        private Boolean modMarks_;

        [Browsable(false)]
                [XmlIgnore]
        public Boolean modMarks
        {
            get { return modMarks_; }
            set
            {
                modMarks_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(modMarks)));
            }
        }

        private string path_ = string.Empty;

        [Browsable(false)]
                [XmlElement(ElementName = "ProjectPath")]
        public string path
        {
            get { return path_; }
            set
            {
                path_ = value; modificationApear();
                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(path)));
            }
        }

        private ChartViewConf ChartConf_;

        [Browsable(false)]
                [XmlElement(ElementName = "EditorsConfiguration")]
        public ChartViewConf ChartConf
        {
            get { return ChartConf_; }
            set { ChartConf_ = value; modificationApear(); }
        }

        private TableViewConf TableConf_;

        [Browsable(false)]
                [XmlElement(ElementName = "TableConfiguration")]
        public TableViewConf TableConf
        {
            get { return TableConf_; }
            set { TableConf_ = value; modificationApear(); }
        }

        private CommViewConf CommConf_;

        [Browsable(false)]
                [XmlElement(ElementName = "CommViewConfiguration")]
        public CommViewConf CommConf
        {
            get { return CommConf_; }
            set { CommConf_ = value; modificationApear(); }
        }

        [Browsable(false)]
        [XmlElement(ElementName = "DatabaseConfiguration")]
        public DatabaseModel Db { get; set; }

        [field: NonSerialized]
        [Browsable(false)]
        [XmlIgnore]
        public ChartConfigNode ChartConfigNode { get; private set; }

        private Boolean IsExpand_;

        [Browsable(false)]
                public Boolean IsExpand
        {
            get { return IsExpand_; }
            set
            {
                IsExpand_ = value;
                modificationApear();

                propChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpand)));
            }
        }

        [NonSerialized]
        private ProjectContainer PrCon_; [Browsable(false)]

        [XmlIgnore]
        public ProjectContainer PrCon
        {
            get { return PrCon_; }
            set { PrCon_ = value; }
        }

        [field: NonSerialized]
        private LegacyScriptCompat scriptCon_;

        [Browsable(false)]
                [XmlIgnore]
        public dynamic ScriptCon
        {
            get
            {
                scriptCon_ ??= new LegacyScriptCompat(this);
                return scriptCon_;
            }
        }

        private WebServer WebServer1_;

        [Browsable(false)]
        [XmlElement(ElementName = "WebServerConfiguration")]
        public WebServer WebServer1
        {
            get { return WebServer1_; }
            set { WebServer1_ = value; }
        }

#pragma warning disable CS0618 // InFile is obsolete - kept for legacy project compatibility
        private List<InFile> FileList_;

        [Browsable(false)]
                [XmlElement(ElementName = "FileList", Type = typeof(List<InFile>))]
        public List<InFile> FileList
        {
            get { return FileList_; }
            set { FileList_ = value; }
        }
#pragma warning restore CS0618

        private List<ScriptFile> ScriptFileList_;

        [Browsable(false)]
                [XmlElement(ElementName = "ScriptFileList", Type = typeof(List<ScriptFile>))]
        public List<ScriptFile> ScriptFileList
        {
            get { return ScriptFileList_; }
            set { ScriptFileList_ = value; }
        }

        private List<Connection> connectionList_ = new List<Connection>();

        [Browsable(false)]
                [XmlElement(ElementName = "ConnectionList", Type = typeof(List<Connection>))]
        public List<Connection> connectionList
        {
            get { return connectionList_; }
            set
            {
                connectionList_ = value;

                modificationApear();
            }
        }

        private List<Device> DevicesList_ = new List<Device>();

        [Browsable(false)]
                [XmlElement(ElementName = "DeviceList", Type = typeof(List<Device>))]
        public List<Device> DevicesList
        {
            get { return DevicesList_; }
            set
            {
                DevicesList_ = value;
                modificationApear();
            }
        }

        private List<Tag> tagsList_ = new List<Tag>();

        [Browsable(false)]
                [XmlElement(ElementName = "TagList", Type = typeof(List<Tag>))]
        public List<Tag> tagsList
        {
            get { return tagsList_; }
            set { tagsList_ = value; modificationApear(); }
        }

        private List<InTag> InTagsList_ = new List<InTag>();

        [Browsable(false)]
                [XmlElement(ElementName = "InternalTagList", Type = typeof(List<InTag>))]
        public List<InTag> InTagsList
        {
            get { return InTagsList_; }
            set { InTagsList_ = value; modificationApear(); }
        }

        private ScriptsDriver ScriptEng_;

        [Browsable(false)]
        [XmlElement(ElementName = "ScriptEngine")]
        public ScriptsDriver ScriptEng
        {
            get { return ScriptEng_; }
            set { ScriptEng_ = value; }
        }

        private InternalTagsDriver InternalTags_;

        [Browsable(false)]
                [XmlElement(ElementName = "IntTagsEngine")]
        public InternalTagsDriver InternalTagsDrv
        {
            get { return InternalTags_; }
            set { InternalTags_ = value; }
        }

        string ITreeViewModel.Name
        {
            get
            {
                return projectName;
            }

            set
            {
                projectName = value;
            }
        }

        bool ITreeViewModel.IsLive
        {
            get
            {
                return false;
            }
            set { }
        }

        bool ITreeViewModel.IsBlocked
        {
            get
            {
                return false;
            }
            set { }
        }

        public Project()
        {
        }

        public Project(ProjectContainer prcn, string projectName, string autor, string company, string describe)
        {
            this.projectName = projectName;
            this.autor = autor;
            this.company = company;
            this.describe = describe;
            this.createTime_ = DateTime.Now;
            this.modifeTime = DateTime.Now;
            this.modMarks = true;

            longDT = "yyyy-MM-dd HH:mm:ss.fff";

            fileVer_ = Assembly.GetExecutingAssembly().GetName().Version;

            this.PrCon_ = prcn;

#pragma warning disable CS0618 // InFile is obsolete - kept for legacy project compatibility
            FileList_ = new List<InFile>();
#pragma warning restore CS0618
            ScriptFileList_ = new List<ScriptFile>();

            WebServer1_ = new WebServer(null);
            WebServer1_.PrCon = prcn;
            WebServer1_.Proj = this;

            ScriptEng_ = new ScriptsDriver(this);
            ScriptEng_.Proj = this;

            IsExpand = true;

            objId = Guid.NewGuid();

            InternalTags_ = new InternalTagsDriver(this);

            TreeViewChildren_ = new ObservableCollection<object>();
            TreeViewChildren_.Add(WebServer1_);
            TreeViewChildren_.Add(ScriptEng_);
            TreeViewChildren_.Add(InternalTags_);

            Db = new DatabaseModel();
            TreeViewChildren_.Add(Db);

            ((ITreeViewModel)WebServer1_).Children = new ObservableCollection<object>();

            ((ITreeViewModel)ScriptEng_).Children = new ObservableCollection<object>(new object[] { new TimersFolder(ScriptEng_.Timers, ScriptEng_.isTimersFolderExpand, v => ScriptEng_.isTimersFolderExpand = v) }.Concat(ScriptFileList_.Cast<object>()));
            ((ITreeViewModel)InternalTagsDrv).Children = new ObservableCollection<object>(new object[] { new TimersFolder(InternalTagsDrv.Timers, InternalTagsDrv.isTimersFolderExpand, v => InternalTagsDrv.isTimersFolderExpand = v) }.Concat(InTagsList_.Cast<object>()));

            DriverChildren_ = new ObservableCollection<IDriverModel>();
            DriverChildren_.Add((IDriverModel)ScriptEng);
            DriverChildren_.Add((IDriverModel)InternalTagsDrv);

            foreach (var cn in connectionList_)
            {
                TreeViewChildren_.Add(cn);
                DriverChildren_.Add((IDriverModel)cn);
                ((ITreeViewModel)cn).Children = new ObservableCollection<object>(from x in DevicesList_ where x.parentId == cn.objId select x);
            }

            foreach (var dev in DevicesList_)
                ((ITreeViewModel)dev).Children = new ObservableCollection<object>(from x in tagsList_ where x.parentId == dev.objId select x);

            TagChildren = new ObservableCollection<ITag>();

            this.ChartConf = new ChartViewConf();
            this.ChartConf.Axes ??= new();
            this.ChartConf.Axes.Add(new ChartAxisConf("Y1", "Y1", false));

            ((INotifyPropertyChanged)ChartConf).PropertyChanged += Project_PropertyChanged;

            this.TableConf = new TableViewConf();
            this.CommConf = new CommViewConf();

            ChartConfigNode = new ChartConfigNode(this);
            TreeViewChildren_.Add(ChartConfigNode);
        }

        private void Project_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            modificationApear();
        }

                private void modificationApear()
        {
            modifeTime = DateTime.Now;
            modMarks = true;
        }

                [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InternalTags_.Proj = this;

            if (WebServer1_ == null)
                WebServer1_ = new WebServer(null);

            WebServer1_.PrCon = this.PrCon;
            WebServer1_.Proj = this;

            if (FileList_ == null)
            {
#pragma warning disable CS0618 // InFile is obsolete - kept for legacy project compatibility
                FileList_ = new List<InFile>();
#pragma warning restore CS0618
            }

            if (string.IsNullOrEmpty(longDT))
                longDT = "yyyy-MM-dd HH:mm:ss.fff";

            if (ScriptEng_ == null)
                ScriptEng_ = new ScriptsDriver(this);

            ScriptEng_.Proj = this;

            if (ScriptFileList_ == null)
                ScriptFileList_ = new List<ScriptFile>();

            foreach (var sf in ScriptFileList_)
            {
                sf.Proj = this;
                sf.PrCon = PrCon;
            }

            ((INotifyPropertyChanged)ChartConf).PropertyChanged += Project_PropertyChanged;

            TreeViewChildren_ = new ObservableCollection<object>();
            TreeViewChildren_.Add(this.WebServer1_);
            TreeViewChildren_.Add(this.ScriptEng_);
            TreeViewChildren_.Add(this.InternalTags_);
            if (Db == null)
                Db = new DatabaseModel();
            TreeViewChildren_.Add(this.Db);

            if (ChartConf == null)
                ChartConf = new ChartViewConf();
            if (TableConf == null)
                TableConf = new TableViewConf();
            if (CommConf == null)
                CommConf = new CommViewConf();
            ChartConfigNode = new ChartConfigNode(this);
            TreeViewChildren_.Add(ChartConfigNode);

            DirectoryInfo gt = new DirectoryInfo(Path.GetDirectoryName(this.path) + "\\Http");

            if (gt.Exists)
            {
                var subDir = (from x in gt.GetDirectories() select new CusFile(x)).ToList();
                subDir.AddRange(from x in gt.GetFiles() select new CusFile(x));
                ((ITreeViewModel)WebServer1_).Children = new ObservableCollection<object>(subDir);
                FileList.Clear();
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.path) + "\\Http");
                gt = new DirectoryInfo(Path.GetDirectoryName(this.path) + "\\Http");
                var subDir = (from x in gt.GetDirectories() select new CusFile(x)).ToList();
                subDir.AddRange(from x in gt.GetFiles() select new CusFile(x));
                ((ITreeViewModel)WebServer1_).Children = new ObservableCollection<object>(subDir);
                FileList.Clear();
            }

            ((ITreeViewModel)ScriptEng_).Children = new ObservableCollection<object>(ScriptFileList_);
            ((ITreeViewModel)InternalTagsDrv).Children = new ObservableCollection<object>(InTagsList_);

            foreach (var cn in connectionList_)
            {
                TreeViewChildren_.Add(cn);

                ((ITreeViewModel)cn).Children = new ObservableCollection<object>(from x in DevicesList_ where x.parentId == cn.objId select x);
                ((ITableView)cn).Children = new ObservableCollection<ITag>((from x in tagsList where x.connId == cn.objId select x).Union<ITag>(InTagsList));
            }

            foreach (var dev in DevicesList_)
            {
                ((ITreeViewModel)dev).Children = new ObservableCollection<object>(from x in tagsList_ where x.parentId == dev.objId select x);
                ((ITableView)dev).Children = new ObservableCollection<ITag>((from x in tagsList where x.parentId == dev.objId select x).Union<ITag>(InTagsList));
            }

            var query = tagsList.Union<ITag>(InTagsList);
            TagChildren = new ObservableCollection<ITag>(query);
        }

        public void OnDeserializedXML()
        {
            InternalTags_.Proj = this;

            if (WebServer1_ == null)
                WebServer1_ = new WebServer(null);

            WebServer1_.PrCon = this.PrCon;
            WebServer1_.Proj = this;

            if (FileList_ == null)
            {
#pragma warning disable CS0618 // InFile is obsolete - kept for legacy project compatibility
                FileList_ = new List<InFile>();
#pragma warning restore CS0618
            }

            if (string.IsNullOrEmpty(longDT))
                longDT = "yyyy-MM-dd HH:mm:ss.fff";

            if (ScriptEng_ == null)
                ScriptEng_ = new ScriptsDriver(this);

            ScriptEng_.Proj = this;

            if (ScriptFileList_ == null)
                ScriptFileList_ = new List<ScriptFile>();

            foreach (var sf in ScriptFileList_)
            {
                sf.Proj = this;
                sf.PrCon = PrCon;
            }

            ((INotifyPropertyChanged)ChartConf).PropertyChanged += Project_PropertyChanged;

            TreeViewChildren_ = new ObservableCollection<object>();
            TreeViewChildren_.Add(this.WebServer1_);
            TreeViewChildren_.Add(this.ScriptEng_);
            TreeViewChildren_.Add(this.InternalTags_);
            if (Db == null)
                Db = new DatabaseModel();
            TreeViewChildren_.Add(this.Db);

            if (ChartConf == null)
                ChartConf = new ChartViewConf();
            if (TableConf == null)
                TableConf = new TableViewConf();
            if (CommConf == null)
                CommConf = new CommViewConf();
            ChartConfigNode = new ChartConfigNode(this);
            TreeViewChildren_.Add(ChartConfigNode);

            DirectoryInfo gt = new DirectoryInfo(Path.GetDirectoryName(this.path) + "\\Http");

            if (gt.Exists)
            {
                var subDir = (from x in gt.GetDirectories() select new CusFile(x)).ToList();
                subDir.AddRange(from x in gt.GetFiles() select new CusFile(x));
                ((ITreeViewModel)WebServer1_).Children = new ObservableCollection<object>(subDir);
                FileList.Clear();
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(this.path) + "\\Http");
                gt = new DirectoryInfo(Path.GetDirectoryName(this.path) + "\\Http");
                var subDir = (from x in gt.GetDirectories() select new CusFile(x)).ToList();
                subDir.AddRange(from x in gt.GetFiles() select new CusFile(x));
                ((ITreeViewModel)WebServer1_).Children = new ObservableCollection<object>(subDir);
                FileList.Clear();
            }

            ((ITreeViewModel)ScriptEng_).Children = new ObservableCollection<object>(new object[] { new TimersFolder(ScriptEng_.Timers, ScriptEng_.isTimersFolderExpand, v => ScriptEng_.isTimersFolderExpand = v) }.Concat(ScriptFileList_.Cast<object>()));
            ((ITreeViewModel)InternalTagsDrv).Children = new ObservableCollection<object>(new object[] { new TimersFolder(InternalTagsDrv.Timers, InternalTagsDrv.isTimersFolderExpand, v => InternalTagsDrv.isTimersFolderExpand = v) }.Concat(InTagsList_.Cast<object>()));

            foreach (var cn in connectionList_)
            {
                TreeViewChildren_.Add(cn);

                ((ITreeViewModel)cn).Children = new ObservableCollection<object>(from x in DevicesList_ where x.parentId == cn.objId select x);
                ((ITableView)cn).Children = new ObservableCollection<ITag>((from x in tagsList where x.connId == cn.objId select x).Union<ITag>(InTagsList));
            }

            foreach (var dev in DevicesList_)
            {
                ((ITreeViewModel)dev).Children = new ObservableCollection<object>(from x in tagsList_ where x.parentId == dev.objId select x);
                ((ITableView)dev).Children = new ObservableCollection<ITag>((from x in tagsList where x.parentId == dev.objId select x).Union<ITag>(InTagsList));
            }

            var query = tagsList.Union<ITag>(InTagsList);
            TagChildren = new ObservableCollection<ITag>(query);
        }

                public object Clone()
        {
            Project Pr1 = ObjectCloner.DeepClone(this);
            Pr1.objId_ = Guid.NewGuid();
            Pr1.connectionList_.Clear();
            Pr1.DevicesList_.Clear();
            Pr1.tagsList_.Clear();

            Pr1.modifeTime_ = DateTime.Now;
            Pr1.createTime_ = DateTime.Now;
            Pr1.path_ = "";
            Pr1.TagChildren = new ObservableCollection<ITag>();
            Pr1.DriverChildren_ = new ObservableCollection<IDriverModel>();
            Pr1.TreeViewChildren_ = new ObservableCollection<object>();

            return Pr1;
        }

                public object GetTag(string s)
        {
            try
            {
                Tag tg = tagsList_.Find(x => x.tagName == s);
                if (tg != null)
                    return tg.value;
                else
                {
                    InTag tgs = InTagsList_.Find(x => x.tagName == s);
                    if (tgs != null)
                        return tgs.value;
                    else
                        return 0;
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return 0;
            }
        }

                public ITag GetITag(string s)
        {
            try
            {
                Tag tg = tagsList_.Find(x => x.tagName == s);
                if (tg != null)
                    return tg;
                else
                {
                    InTag tgs = InTagsList_.Find(x => x.tagName == s);
                    if (tgs != null)
                        return tgs;
                    else
                        return null;
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return null;
            }
        }

                public Object SetTag(string s, object val)
        {
            try
            {
                Tag tg = tagsList_.Find(x => x.tagName == s);
                if (tg != null)
                {
                    tg.setValueMethod(val);
                    return tg.value;
                }
                else
                {
                    InTag tgs = InTagsList_.Find(x => x.tagName == s);
                    if (tgs != null)
                    {
                        tgs.value = val;
                        return tgs.value;
                    }
                    else return null;
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return 0;
            }
        }

        public void Write(object sender, string s)
        {
            PrCon_.ApplicationError?.Invoke(sender, new ProjectEventArgs(new Information(s)));
        }

        #region Scripts for Web

                public string SetTagValue(string s, object val)
        {
            try
            {
                Tag tg = tagsList_.Find(x => x.tagName == s);

                if (tg != null)
                {
                    tg.setValueMethod(val);
                    return tg.value.ToString();
                }
                else
                {
                    InTag tgs = InTagsList_.Find(x => x.tagName == s);
                    if (tgs != null)
                    {
                        tgs.value = val;
                        return tgs.value.ToString();
                    }
                    else return tgs.value.ToString();
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return string.Empty;
            }
        }

                public string GetTagValue(string s)
        {
            try
            {
                Tag tg = tagsList_.Find(x => x.tagName == s);
                if (tg != null)
                {
                    return tg.value.ToString();
                }
                else
                {
                    InTag tgs = InTagsList_.Find(x => x.tagName == s);
                    if (tgs != null)
                    {
                        return tgs.value.ToString();
                    }
                    else return tgs.value.ToString();
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return "Empty";
            }
        }

                public string GetTimerValue(string s)
        {
            try
            {
                return DateTime.Now.ToString();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return "Empty";
            }
        }

                public string GetUserValue(string s)
        {
            try
            {
                return "User";
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return "Empty";
            }
        }

                public string GetMachineValue(string s)
        {
            try
            {
                return Environment.MachineName;
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return "Empty";
            }
        }

                public string GetTagsAll(string name)
        {
            try
            {
                return JsonConvert.SerializeObject(PrCon_.GetAllITags(objId, objId, false, false));
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return "Empty";
            }
        }

                public string GetConnectionsAll(string name)
        {
            try
            {
                return JsonConvert.SerializeObject(connectionList.ToArray());
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));

                return "Empty";
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    WebServer1_?.Dispose();
                    connectionList_.Clear();
                    DevicesList_.Clear();
                    tagsList_.Clear();
                    InTagsList_.Clear();
                    FileList_?.Clear();
                    ScriptFileList_?.Clear();
                }

                disposedValue = true;
            }
        }

        ~Project()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support

        #endregion Scripts for Web

        [Serializable]
        public class LegacyScriptCompat
        {
            private const int MaxCachedCSharpScripts = 256;

            private readonly Project project;
            private readonly ConcurrentDictionary<string, ScriptRunner<object>> csharpScripts = new ConcurrentDictionary<string, ScriptRunner<object>>();
            private readonly ConcurrentQueue<string> csharpScriptsOrder = new ConcurrentQueue<string>();
            private readonly ConcurrentDictionary<string, byte> invalidCsharpExpressions = new ConcurrentDictionary<string, byte>();
            private readonly object csharpScriptsSync = new object();

            private static readonly ScriptOptions scriptOptions = ScriptOptions.Default
                .AddReferences(typeof(object).Assembly, typeof(Project).Assembly)
                .AddImports("System", "System.Math", "ProjectDataLib");

            public LegacyScriptCompat(Project project)
            {
                this.project = project;
            }

            public object Eval(string expression)
            {
                if (string.IsNullOrWhiteSpace(expression))
                    return null;

                string expr = expression.Trim().Replace(',', '.');

                try
                {
                    return EvalAsDataExpression(expr);
                }
                catch
                {
                    return EvalAsCSharp(expr);
                }
            }

            private static object EvalAsDataExpression(string expr)
            {
                var table = new DataTable { Locale = CultureInfo.InvariantCulture };
                return table.Compute(expr, string.Empty);
            }

            private object EvalAsCSharp(string expr)
            {
                // Allow retry for previously failed expressions in case runtime context changed
                // (for example after app/domain reload or compatibility updates).
                invalidCsharpExpressions.TryRemove(expr, out _);

                if (!csharpScripts.TryGetValue(expr, out ScriptRunner<object> runner))
                {
                    ScriptRunner<object> compiledRunner;
                    try
                    {
                        compiledRunner = CSharpScript.Create<object>(expr, scriptOptions, typeof(ScriptGlobals)).CreateDelegate();
                    }
                    catch (CompilationErrorException ex)
                    {
                        invalidCsharpExpressions.TryAdd(expr, 0);
                        throw new InvalidOperationException(ex.Message, ex);
                    }

                    lock (csharpScriptsSync)
                    {
                        if (!csharpScripts.TryGetValue(expr, out runner))
                        {
                            runner = compiledRunner;
                            csharpScripts[expr] = runner;
                            csharpScriptsOrder.Enqueue(expr);

                            while (csharpScripts.Count > MaxCachedCSharpScripts && csharpScriptsOrder.TryDequeue(out string toRemove))
                            {
                                csharpScripts.TryRemove(toRemove, out _);
                            }
                        }
                    }
                }

                return runner(new ScriptGlobals { Project = project, Prj = project }).GetAwaiter().GetResult();
            }
        }
    }
}
