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
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
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
        [ComVisible(false)]
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

        [Category("06 Formats"), DisplayName("DateTime Long"), Description("Format for display")]
        [Browsable(true)]
        [XmlElement(ElementName = "DBDateTimeFormat")]
        public string longDT { get; set; }

        private string projectName_;

        [Category("01 Design"), DisplayName("Project Name"), Description("Current project name")]
        [ComVisible(false)]
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
                propChanged?.Invoke(this, new PropertyChangedEventArgs("projectName"));
                propChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
                modificationApear();
            }
        }

        private Version fileVer_;

        [Category("02 Header"), DisplayName("Version"), Description("Version of files.")]
        [ComVisible(false)]
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
        [ComVisible(false)]
        [XmlElement(ElementName = "Autor")]
        public string autor
        {
            get { return autor_; }
            set
            {
                autor_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("autor"));
                modificationApear();
            }
        }

        private string company_;

        [Category("03 Information"), DisplayName("Company")]
        [ComVisible(false)]
        [XmlElement(ElementName = "Company")]
        public string company
        {
            get { return company_; }
            set
            {
                company_ = value;
                modificationApear();
                propChanged?.Invoke(this, new PropertyChangedEventArgs("company"));
            }
        }

        private string describe_;

        [Category("05 Misc"), DisplayName("Description")]
        [ComVisible(false)]
        [XmlElement(ElementName = "Description")]
        public string describe
        {
            get { return describe_; }
            set
            {
                describe_ = value;
                modificationApear();
                propChanged?.Invoke(this, new PropertyChangedEventArgs("describe"));
            }
        }

        private DateTime createTime_;

        [Category("04 Time"), DisplayName("Create Time"), ReadOnly(true)]
        [ComVisible(false)]
        [XmlElement(ElementName = "Created")]
        public DateTime createTime
        {
            get { return createTime_; }
            set { createTime_ = value; }
        }

        private DateTime modifeTime_;

        [Category("04 Time"), ReadOnly(true), DisplayName("Modification Time")]
        [ComVisible(false)]
        [XmlElement(ElementName = "LastModification")]
        public DateTime modifeTime
        {
            get { return modifeTime_; }
            set
            {
                modifeTime_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("modifeTime"));
            }
        }

        private Boolean modMarks_;

        [Browsable(false)]
        [ComVisible(false)]
        [XmlIgnore]
        public Boolean modMarks
        {
            get { return modMarks_; }
            set
            {
                modMarks_ = value;
                propChanged?.Invoke(this, new PropertyChangedEventArgs("modMarks"));
            }
        }

        private string path_ = string.Empty;

        [Browsable(false)]
        [ComVisible(false)]
        [XmlElement(ElementName = "ProjectPath")]
        public string path
        {
            get { return path_; }
            set
            {
                path_ = value; modificationApear();
                propChanged?.Invoke(this, new PropertyChangedEventArgs("path"));
            }
        }

        private ChartViewConf ChartConf_;

        [Browsable(false)]
        [ComVisible(false)]
        [XmlElement(ElementName = "EditorsConfiguration")]
        public ChartViewConf ChartConf
        {
            get { return ChartConf_; }
            set { ChartConf_ = value; modificationApear(); }
        }

        [Browsable(false)]
        [XmlElement(ElementName = "DatabaseConfiguration")]
        public DatabaseModel Db { get; set; }

        private Boolean IsExpand_;

        [Browsable(false)]
        [ComVisible(false)]
        public Boolean IsExpand
        {
            get { return IsExpand_; }
            set
            {
                IsExpand_ = value;
                modificationApear();

                propChanged?.Invoke(this, new PropertyChangedEventArgs("IsExpand"));
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
        [ComVisible(false)]
        [XmlIgnore]
        public dynamic ScriptCon
        {
            get
            {
                scriptCon_ ??= new LegacyScriptCompat(this);
                return scriptCon_;
            }
        }

        private List<InFile> FileList_;

        [Browsable(false)]
        [ComVisible(false)]
        [XmlElement(ElementName = "FileList", Type = typeof(List<InFile>))]
        public List<InFile> FileList
        {
            get { return FileList_; }
            set { FileList_ = value; }
        }

        private List<ScriptFile> ScriptFileList_;

        [Browsable(false)]
        [ComVisible(false)]
        [XmlElement(ElementName = "ScriptFileList", Type = typeof(List<ScriptFile>))]
        public List<ScriptFile> ScriptFileList
        {
            get { return ScriptFileList_; }
            set { ScriptFileList_ = value; }
        }

        private List<Connection> connectionList_ = new List<Connection>();

        [Browsable(false)]
        [ComVisible(false)]
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
        [ComVisible(false)]
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
        [ComVisible(false)]
        [XmlElement(ElementName = "TagList", Type = typeof(List<Tag>))]
        public List<Tag> tagsList
        {
            get { return tagsList_; }
            set { tagsList_ = value; modificationApear(); }
        }

        private List<InTag> InTagsList_ = new List<InTag>();

        [Browsable(false)]
        [ComVisible(false)]
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
        [ComVisible(false)]
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

            FileList_ = new List<InFile>();
            ScriptFileList_ = new List<ScriptFile>();

            ScriptEng_ = new ScriptsDriver(this);
            ScriptEng_.Proj = this;

            IsExpand = true;

            objId = Guid.NewGuid();

            InternalTags_ = new InternalTagsDriver(this);

            TreeViewChildren_ = new ObservableCollection<object>();
            TreeViewChildren_.Add(ScriptEng_);
            TreeViewChildren_.Add(InternalTags_);

            Db = new DatabaseModel();
            TreeViewChildren_.Add(Db);

            ((ITreeViewModel)ScriptEng_).Children = new ObservableCollection<object>(new object[] { new TimersFolder(ScriptEng_.Timers) }.Concat(ScriptFileList_.Cast<object>()));
            ((ITreeViewModel)InternalTagsDrv).Children = new ObservableCollection<object>(new object[] { new TimersFolder(InternalTagsDrv.Timers) }.Concat(InTagsList_.Cast<object>()));

            DriverChildren_ = new ObservableCollection<IDriverModel>();
            DriverChildren_.Add((IDriverModel)ScriptEng);
            DriverChildren_.Add((IDriverModel)InternalTagsDrv);

            foreach (var cn in connectionList_)
            {
                TreeViewChildren_.Add(cn);

                ((ITreeViewModel)cn).Children = new ObservableCollection<object>(from x in DevicesList_ where x.parentId == cn.objId select x);
            }

            foreach (var dev in DevicesList_)
                ((ITreeViewModel)dev).Children = new ObservableCollection<object>(from x in tagsList_ where x.parentId == dev.objId select x);

            TagChildren = new ObservableCollection<ITag>();

            this.ChartConf = new ChartViewConf();
            ((INotifyPropertyChanged)ChartConf).PropertyChanged += Project_PropertyChanged;
        }

        private void Project_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            modificationApear();
        }

        [ComVisible(false)]
        private void modificationApear()
        {
            modifeTime = DateTime.Now;
            modMarks = true;
        }

        [ComVisible(false)]
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            InternalTags_.Proj = this;

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

            if (FileList_ == null)
                FileList_ = new List<InFile>();

            ((INotifyPropertyChanged)ChartConf).PropertyChanged += Project_PropertyChanged;

            TreeViewChildren_ = new ObservableCollection<object>();
            TreeViewChildren_.Add(this.ScriptEng_);
            TreeViewChildren_.Add(this.InternalTags_);
            if (Db == null)
                Db = new DatabaseModel();
            TreeViewChildren_.Add(this.Db);

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

            if (FileList_ == null)
                FileList_ = new List<InFile>();

            ((INotifyPropertyChanged)ChartConf).PropertyChanged += Project_PropertyChanged;

            TreeViewChildren_ = new ObservableCollection<object>();
            TreeViewChildren_.Add(this.ScriptEng_);
            TreeViewChildren_.Add(this.InternalTags_);
            if (Db == null)
                Db = new DatabaseModel();
            TreeViewChildren_.Add(this.Db);

            ((ITreeViewModel)ScriptEng_).Children = new ObservableCollection<object>(new object[] { new TimersFolder(ScriptEng_.Timers) }.Concat(ScriptFileList_.Cast<object>()));
            ((ITreeViewModel)InternalTagsDrv).Children = new ObservableCollection<object>(new object[] { new TimersFolder(InternalTagsDrv.Timers) }.Concat(InTagsList_.Cast<object>()));

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

        [ComVisible(false)]
        public object Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, this);
            ms.Position = 0;

            Project Pr1 = (Project)bf.Deserialize(ms);
            ms.Close();

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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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

        [ComVisible(true)]
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
            private readonly Project project;
            private readonly ConcurrentDictionary<string, ScriptRunner<object>> csharpScripts = new ConcurrentDictionary<string, ScriptRunner<object>>();

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
                var runner = csharpScripts.GetOrAdd(expr, code =>
                    CSharpScript.Create<object>(code, scriptOptions, typeof(ScriptGlobals)).CreateDelegate());

                return runner(new ScriptGlobals { Project = project }).GetAwaiter().GetResult();
            }

            private class ScriptGlobals
            {
                public Project Project { get; set; }
            }
        }
    }
}
