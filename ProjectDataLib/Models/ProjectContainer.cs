using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Net.Http;
using System.Diagnostics;
using System.Reflection;
using System.Xml;

namespace ProjectDataLib
{

    public class ProjectContainer : ITreeViewModel
    {
        #region Fields

        public List<Project> projectList;

        public GlobalConfiguration gConf;

        public List<WindowsStatus> winManagment;

        public Guid ServerGuid = new Guid("11111111-1111-1111-1111-111111111111");

        public Guid IntTagsGuid = new Guid("22222222-2222-2222-2222-222222222222");

        public Guid ScriptGuid = new Guid("33333333-3333-3333-3333-333333333333");

        public Guid HttpFileGuid = new Guid("44444444-4444-4444-4444-444444444444");

        public string HttpCatalog = "\\Http";

        public string ScriptsCatalog = "\\Scripts";

        public string TemplateCatalog = "\\Template";

        public string Database = "\\Database\\ProjDatabase.sqlite";

        public string RegUserRoot = "HKEY_CURRENT_USER\\Software\\Fenix";

        public string LastPathKey = "LastPath";

        public string TaskName = "FenixServer";

        public string HelpWebSite = "https://github.com/dgsoftlabs/FenixModbusS7/wiki";

        public string LayoutFile = "Layout_.xml";

        public EventHandler ApplicationError;

        public Guid SrcProject;

        public Guid SrcElement;
        public ElementKind SrcType = ElementKind.Empty;
        public Boolean cutMarks;

        public EventHandler<ProjectEventArgs> addProjectEv;

        public EventHandler<ProjectEventArgs> pasteProjectEv;
        public EventHandler<ProjectEventArgs> clearProjectsEv;
        public EventHandler<ProjectEventArgs> saveProjectEv;

        public EventHandler<ProjectEventArgs> addConnEv;
        public EventHandler<ProjectEventArgs> removeConnEv;
        public EventHandler<ProjectEventArgs> pasteConnEv;

        public EventHandler<ProjectEventArgs> addDeviceEv;
        public EventHandler<ProjectEventArgs> removeDeviceEv;
        public EventHandler<ProjectEventArgs> pasteDeviceEv;

        public EventHandler<ProjectEventArgs> addTagEv;
        public EventHandler<ProjectEventArgs> removeTagEv;
        public EventHandler<ProjectEventArgs> editTagEv;
        public EventHandler<ProjectEventArgs> pasteTagEv;

        public EventHandler<ProjectEventArgs> addIntTagEv;
        public EventHandler<ProjectEventArgs> removeIntTagEv;
        public EventHandler<ProjectEventArgs> editIntTagEv;
        public EventHandler<ProjectEventArgs> pasteIntTagEv;

        public EventHandler<ProjectEventArgs> addInFileEv;
        public EventHandler<ProjectEventArgs> removeInFileEv;

        public EventHandler<ProjectEventArgs> addScriptFileEv;
        public EventHandler<ProjectEventArgs> removeScriptFileEv;

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
                return ToString();
            }

            set
            {
            }
        }

        bool ITreeViewModel.IsExpand
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        bool ITreeViewModel.IsLive
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        bool ITreeViewModel.IsBlocked
        {
            get
            {
                return false;
            }

            set { }
        }

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }

        #endregion Fields

        #region Konstruktor

        public ProjectContainer()
        {
            try
            {
                winManagment = new List<WindowsStatus>() { new WindowsStatus(0, false, false) };
                projectList = new List<Project>();
                gConf = new GlobalConfiguration();
                _Children = new ObservableCollection<object>();
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        #endregion Konstruktor

        #region Project

        public Guid addProject(Project project)
        {
            try
            {
                projectList.Add(project);
                _Children.Add(project);

                if (addProjectEv != null)
                    addProjectEv(project, new ProjectEventArgs(project));

                return project.objId;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return Guid.Empty;
            }
        }

        public Project getProject(Guid Id)
        {
            try
            {
                Project pr = projectList.Find(x => x.objId.Equals(Id));

                if (pr == null)
                    throw new ApplicationException(String.Format("Guid: {0} isn't sign project", Id));

                return pr;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public bool openProjects(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                var extension = Path.GetExtension(path);
                if (!string.Equals(extension, ".pse", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        $"Unsupported project format '{extension}'. Only '.pse' is supported in .NET 10.");
                }

                Project buff;
                var xmlSer = new XmlSerializer(typeof(Project));
                using (Stream fsream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    buff = (Project)xmlSer.Deserialize(fsream);
                }

                buff.path = path;
                buff.PrCon = this;

                buff.OnDeserializedXML();
                buff.ChartConf.OnDeserializedXML();

                foreach (Project pr in projectList)
                    if (pr.objId.Equals(buff.objId))
                        throw new ApplicationException(pr.projectName + ": " + "Project is alredy open.");

                foreach (InTag tg in buff.InTagsList)
                {
                    tg.Proj = buff;
                    tg.PrCon = this;
                    tg.idrv = buff.InternalTagsDrv;
                }

                buff.ScriptEng.Proj = buff;

                List<string> deleteFiles = new List<string>();
                foreach (ScriptFile scf in buff.ScriptFileList)
                {
                    scf.FilePath = Path.GetDirectoryName(buff.path) + this.ScriptsCatalog + "\\" + scf.Name;

                    if (!File.Exists(scf.FilePath))
                        deleteFiles.Add(scf.FilePath);

                    scf.Proj = buff;
                    scf.PrCon = this;
                }

                foreach (string gp in deleteFiles)
                {
                    ScriptFile fb = buff.ScriptFileList.Where(x => x.FilePath == gp).First();

                    if (fb != null)
                        ((ITreeViewModel)buff.ScriptEng).Children.Remove(fb);

                    buff.ScriptFileList.RemoveAll(x => x.FilePath == gp);
                }

                deleteFiles.Clear();

                foreach (InFile fil in buff.FileList)
                {
                    fil.FilePath = Path.GetDirectoryName(buff.path) + this.HttpCatalog + "\\" + fil.Name;

                    if (!File.Exists(fil.FilePath))
                        deleteFiles.Add(fil.FilePath);
                }

                foreach (string gp in deleteFiles)
                    buff.FileList.RemoveAll(x => x.FilePath == gp);

                foreach (Connection cn in buff.connectionList)
                {
                    cn.OnDeserializedXML();

                    cn.gConf = gConf;

                    cn.PrCon = this;

                    cn.Idrv = gConf?.newDrv(cn.DriverName);

                    cn.assemblyError = cn.Idrv == null ? true : false;

                    foreach (Tag tg in buff.tagsList)
                    {
                        if (tg.connId.Equals(cn.objId))
                            tg.idrv = cn.Idrv;

                        tg.PrCon = this;
                        tg.Proj = buff;

                        tg.OnDeserializedXml();
                    }
                }

                ((IDriversMagazine)buff).Children = new ObservableCollection<IDriverModel>();
                ((IDriversMagazine)buff).Children.Add(buff.InternalTagsDrv);
                ((IDriversMagazine)buff).Children.Add(buff.ScriptEng);

                foreach (Connection cn in buff.connectionList)
                    ((IDriversMagazine)buff).Children.Add(cn.Idrv);

                foreach (Connection cx in buff.connectionList)
                {
                    ((IDriversMagazine)cx).Children = new ObservableCollection<IDriverModel>();
                    ((IDriversMagazine)cx).Children.Add(buff.InternalTagsDrv);
                    ((IDriversMagazine)cx).Children.Add(buff.ScriptEng);
                    ((IDriversMagazine)cx).Children.Add(cx.Idrv);
                }

                foreach (Device dx in buff.DevicesList)
                {
                    dx.idrv = (from cn in buff.connectionList where cn.objId == dx.parentId select cn.Idrv).First();
                    ((IDriversMagazine)dx).Children = new ObservableCollection<IDriverModel>();
                    ((IDriversMagazine)dx).Children.Add(buff.InternalTagsDrv);
                    ((IDriversMagazine)dx).Children.Add(buff.ScriptEng);
                    ((IDriversMagazine)dx).Children.Add(dx.idrv);
                }

                buff.Db.Pr = buff;
                buff.Db.PrCon = this;
                buff.Db.OnDeserializedXML();
                addProject(buff);

                foreach (Device dev in buff.DevicesList)
                {
                    dev.adressChanged += new EventHandler(DevicesAdressChange);

                    dev.PrCon = this;
                }

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean saveProject(Project proj, string path)
        {
            try
            {
                var extension = Path.GetExtension(path);
                if (!string.Equals(extension, ".pse", StringComparison.OrdinalIgnoreCase))
                {
                    throw new NotSupportedException(
                        $"Unsupported project format '{extension}'. Only '.pse' is supported in .NET 10.");
                }

                proj.modMarks = false;
                proj.modifeTime = DateTime.Now;
                proj.fileVer = Assembly.GetExecutingAssembly().GetName().Version;

                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string tempPath = path + ".tmp";
                string backupPath = path + ".bak";

                XmlSerializer xmlSer = new XmlSerializer(typeof(Project));
                using (Stream fsream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    xmlSer.Serialize(fsream, proj);
                }

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, backupPath, true);
                }
                else
                {
                    File.Move(tempPath, path, true);
                }

                saveProjectEv?.Invoke(proj, new ProjectEventArgs(proj));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean closeAllProject(bool blockSaving)
        {
            try
            {
                foreach (Project proj in projectList)
                {
                    foreach (Device dev in proj.DevicesList)
                        ((ITreeViewModel)dev).Children.Clear();

                    foreach (Connection conn in proj.connectionList)
                        ((ITreeViewModel)conn).Children.Clear();

                    ((ITreeViewModel)proj.ScriptEng).Children.Clear();

                    ((ITreeViewModel)proj).Children.Clear();

                    proj.Dispose();
                }

                ((ITreeViewModel)this).Children.Clear();
                projectList.Clear();

                clearProjectsEv?.Invoke(this, new ProjectEventArgs(null));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean copyCutElement(Guid projId, Guid idEl, ElementKind elKind, Boolean IsCut)
        {
            try
            {
                SrcProject = projId;
                SrcElement = idEl;
                cutMarks = IsCut;
                SrcType = elKind;
                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean pasteElement(Guid TarProj, Guid TarElement)
        {
            try
            {
                switch (SrcType)
                {
                    #region Project

                    case ElementKind.Project:
                        throw new ApplicationException("Operation is not allowed!");

                    #endregion Project

                    #region Connection

                    case ElementKind.Connection:

                        Connection Cnn0 = getConnection(SrcProject, SrcElement);

                        Connection Cnn1 = (Connection)Cnn0.Clone();
                        addConnection(TarProj, Cnn1);

                        List<Device> Dvs0 = getProject(SrcProject).DevicesList.Where(x => x.parentId == Cnn0.objId).ToList();
                        foreach (Device Dv0 in Dvs0)
                        {
                            Device Dv1 = (Device)Dv0.Clone();
                            addDevice(TarProj, Cnn1.objId, Dv1);

                            List<Tag> Tgs0 = getProject(SrcProject).tagsList.Where(x => x.parentId == Dv0.objId).ToList();
                            foreach (Tag Tg0 in Tgs0)
                            {
                                Tag Tg1 = (Tag)Tg0.Clone();
                                addTag(TarProj, Dv1.objId, Tg1);
                            }
                        }

                        if (pasteConnEv != null)
                            pasteConnEv(getProject(TarProj), new ProjectEventArgs(Cnn1));

                        if (cutMarks)
                            removeConnection(SrcProject, SrcElement);

                        break;

                    #endregion Connection

                    #region Device

                    case ElementKind.Device:

                        Device Dvv0 = getDevice(SrcProject, SrcElement);

                        Device Dvv1 = (Device)Dvv0.Clone();
                        addDevice(TarProj, TarElement, Dvv1);

                        List<Tag> Tgs0_ = getProject(SrcProject).tagsList.Where(x => x.parentId == Dvv0.objId).ToList();
                        foreach (Tag Tg0 in Tgs0_)
                        {
                            Tag Tg1 = (Tag)Tg0.Clone();
                            addTag(TarProj, Dvv1.objId, Tg1);
                        }

                        if (pasteDeviceEv != null)
                            pasteDeviceEv(this, new ProjectEventArgs(Dvv1));

                        if (cutMarks)
                            removeDevice(SrcProject, SrcElement);

                        break;

                    #endregion Device

                    #region Tag

                    case ElementKind.Tag:

                        Tag Tgg0 = (Tag)getTag(SrcProject, SrcElement);
                        Tag Tgg1 = (Tag)Tgg0.Clone();

                        Device devTar = (Device)getElementById(TarProj, TarElement);
                        IDriverModel idvTar = getConnection(TarProj, devTar.parentId).Idrv;

                        if (Tgg0.idrv.MemoryAreaInf[0].Name != idvTar.MemoryAreaInf[0].Name)
                        {
                            MessageBox.Show("You cant copy Tags between different drivers!!!");
                            return false;
                        }

                        addTag(TarProj, TarElement, Tgg1);

                        if (pasteTagEv != null)
                            pasteTagEv(this, new ProjectEventArgs(Tgg1));

                        if (cutMarks)
                            removeTag(SrcProject, SrcElement);

                        break;

                        #endregion Tag
                }

                cutMarks = false;
                SrcProject = Guid.Empty;
                SrcElement = Guid.Empty;
                SrcType = ElementKind.Empty;

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean deleteElement(Guid projId, Guid idEl, ElementKind elKind)
        {
            try
            {
                if (elKind == ElementKind.Project)
                {
                    cutMarks = false;
                    return true;
                }

                if (elKind == ElementKind.InFile)
                {
                    RemoveInFile(projId, idEl);
                    cutMarks = false;
                    return true;
                }

                if (elKind == ElementKind.ScriptFile)
                {
                    RemoveScriptFile(projId, idEl);
                    cutMarks = false;
                    return true;
                }

                if (elKind == ElementKind.Connection)
                {
                    removeConnection(projId, idEl);
                    cutMarks = false;
                    return true;
                }

                if (elKind == ElementKind.Device)
                {
                    removeDevice(projId, idEl);
                    cutMarks = false;
                    return true;
                }

                if (elKind == ElementKind.Tag)
                {
                    removeTag(projId, idEl);
                    cutMarks = false;
                    return true;
                }

                if (elKind == ElementKind.IntTag)
                {
                    RemoveIntTag(projId, idEl);
                    cutMarks = false;
                    return true;
                }

                return false;
            }
            catch (Exception Ex)
            {
                cutMarks = false;
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Object getElementById(Guid proj, Guid name)
        {
            try
            {
                Project pr = getProject(proj);

                Object obj = null;

                if (proj == name)
                    return pr;

                if (name == ScriptGuid)
                    return pr.ScriptEng;

                obj = pr.FileList.Find(x => x.objId == name);
                if (obj != null)
                    return obj;

                obj = pr.ScriptFileList.Find(x => x.objId == name);
                if (obj != null)
                    return obj;

                if (name == IntTagsGuid)
                    return pr.InternalTagsDrv;

                obj = pr.InTagsList.Find(x => x.objId.Equals(name));
                if (obj != null)
                    return obj;

                obj = pr.connectionList.Find(x => x.objId.Equals(name));
                if (obj != null)
                    return obj;

                obj = pr.DevicesList.Find(x => x.objId.Equals(name));
                if (obj != null)
                    return obj;

                obj = pr.tagsList.Find(x => x.objId.Equals(name));
                if (obj != null)
                    return obj;

                return null;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public Object getElementById(Guid name)
        {
            try
            {
                foreach (Project pr in projectList)
                {
                    Object obj = null;

                    if (pr.objId == name)
                        return pr;

                    if (name == ScriptGuid)
                        return pr.ScriptEng;

                    obj = pr.FileList.Find(x => x.objId == name);
                    if (obj != null)
                        return obj;

                    obj = pr.ScriptFileList.Find(x => x.objId == name);
                    if (obj != null)
                        return obj;

                    if (name == IntTagsGuid)
                        return pr.InternalTagsDrv;

                    obj = pr.InTagsList.Find(x => x.objId.Equals(name));
                    if (obj != null)
                        return obj;

                    obj = pr.connectionList.Find(x => x.objId.Equals(name));
                    if (obj != null)
                        return obj;

                    obj = pr.DevicesList.Find(x => x.objId.Equals(name));
                    if (obj != null)
                        return obj;

                    obj = pr.tagsList.Find(x => x.objId.Equals(name));
                    if (obj != null)
                        return obj;
                }

                return null;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public List<IDriverModel> GetAllDriversForSel(Guid Pr_, Guid Obj_)
        {
            try
            {
                List<IDriverModel> IDriverList = new List<IDriverModel>();

                Type SelectedObject = getElementById(Pr_, Obj_) == null ? null : getElementById(Pr_, Obj_).GetType();

                Project Pr = getProject(Pr_);

                if (SelectedObject == typeof(Project))
                {
                    IDriverList.AddRange((from dr in Pr.connectionList select dr.Idrv).ToList());
                }
                else if (SelectedObject == typeof(Connection))
                {
                    IDriverList.Add(getConnection(Pr_, Obj_).Idrv);
                }
                else if (SelectedObject == typeof(Device))
                {
                    IDriverList.Add(getConnection(Pr_, getDevice(Pr_, Obj_).parentId).Idrv);
                }
                else if (SelectedObject == typeof(Tag))
                {
                    IDriverList.Add(getTag(Pr_, Obj_).idrv);
                }

                return IDriverList;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public Boolean anyCommunication()
        {
            try
            {
                foreach (Project pr in this.projectList)
                {
                    foreach (Connection cn in pr.connectionList)
                    {
                        if (cn.Idrv != null)
                            if (cn.Idrv.isAlive)
                                return true;
                    }

                    if (((IDriverModel)pr.InternalTagsDrv).isAlive)
                        return true;

                    if (((IDriverModel)pr.ScriptEng).isAlive)
                        return true;
                }
                return false;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean anyWindowCommunication(int i)
        {
            try
            {
                WindowsStatus[] stat = winManagment.Where(x => x.Live && x.index != i).ToArray();
                if (stat.Length > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public void DevicesAdressChange(Object sender, EventArgs e)
        {
            try
            {
                Device dev = (Device)sender;

                List<Tag> tags = getAllTagsFromDevice(dev.projId, dev.objId);

                if (tags.Count > 0)
                {
                    foreach (Tag tg in tags)
                        tg.deviceAdress = dev.adress;
                }
            }
            catch (Exception Ex)
            {
                throw new ApplicationException("ProjectContainer.DevicesAdressChange :" + Ex.Message);
            }
        }

        #endregion Project

        #region Connection

        public Guid addConnection(Guid projId, Connection con)
        {
            try
            {
                con.parentId = projId;

                Project pr = getProject(projId);

                pr.connectionList.Add(con);
                ((ITreeViewModel)pr).Children.Add(con);

                if (con.Idrv == null)
                    con.Idrv = gConf.newDrv(con.DriverName);

                con.gConf = gConf;

                ((IDriversMagazine)pr).Children.Add(con.Idrv);
                ((IDriversMagazine)con).Children.Add(con.Idrv);

                if (addConnEv != null)
                    addConnEv(pr, new ProjectEventArgs(con));

                return con.objId;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return Guid.Empty;
            }
        }

        public Boolean removeConnection(Guid proj, Guid id)
        {
            try
            {
                Project pr = getProject(proj);

                Connection cn = getConnection(proj, id);

                List<Device> devces = (from d in pr.DevicesList where d.parentId == cn.objId select d).ToList();
                foreach (Device dev in devces)
                    removeDevice(proj, dev.objId);

                pr.connectionList.Remove(cn);
                ((ITreeViewModel)pr).Children.Remove(cn);
                ((IDriversMagazine)pr).Children.Remove(cn.Idrv);

                ((ITableView)cn).Children.Clear();
                ((IDriversMagazine)cn).Children.Clear();

                if (removeConnEv != null)
                    removeConnEv(pr, new ProjectEventArgs(cn));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Connection getConnection(Guid proj, Guid cn)
        {
            try
            {
                Project pr = getProject(proj);

                Connection cnn = pr.connectionList.Find(x => x.objId.Equals(cn));

                return cnn;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public List<Tag> getAllTagsFromConnection(Guid proj, Guid con)
        {
            try
            {
                Project pr = getProject(proj);

                List<Device> tagFolder = pr.DevicesList.Where(x => x.parentId.Equals(con)).ToList();

                List<Tag> buff = new List<Tag>();
                foreach (Device tF in tagFolder)
                {
                    List<Tag> b = pr.tagsList.Where(x => x.parentId.Equals(tF.objId)).ToList();
                    buff.AddRange(b.ToArray());
                }

                return buff;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        #endregion Connection

        #region Device

        public Guid addDevice(Guid projId, Guid connId, Device tF)
        {
            try
            {
                Project pr = getProject(projId);

                Connection cn = getConnection(projId, connId);

                tF.parentId = cn.objId;

                tF.idrv = cn.Idrv;

                tF.projId = projId;

                tF.adressChanged += new EventHandler(DevicesAdressChange);

                pr.DevicesList.Add(tF);
                ((ITreeViewModel)cn).Children.Add(tF);

                ((IDriversMagazine)tF).Children.Add(cn.Idrv);

                if (addDeviceEv != null)
                    addDeviceEv(pr, new ProjectEventArgs(tF));

                return tF.objId;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return Guid.Empty;
            }
        }

        public Boolean removeDevice(Guid projId, Guid tagFolId)
        {
            try
            {
                Project pr = getProject(projId);

                Device cF = pr.DevicesList.Find(x => x.objId.Equals(tagFolId));

                Connection cn = getConnection(projId, cF.parentId);

                var dvs = pr.tagsList.FindAll(x => x.parentId == cF.objId);
                foreach (Tag tg in dvs)
                    removeTag(pr.objId, tg.objId);

                pr.DevicesList.Remove(cF);
                ((ITreeViewModel)cn).Children.Remove(cF);

                ((ITableView)cF).Children.Clear();
                ((IDriversMagazine)cF).Children.Clear();

                if (removeDeviceEv != null)
                    removeDeviceEv(pr, new ProjectEventArgs(cF));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Device getDevice(Guid proj, Guid tagFolId)
        {
            try
            {
                Project pr = getProject(proj);
                Device tF = pr.DevicesList.Find(x => x.objId.Equals(tagFolId));
                return tF;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public List<Tag> getAllTagsFromDevice(Guid proj, Guid device)
        {
            try
            {
                Project pr = getProject(proj);
                List<Tag> tagList1 = pr.tagsList.Where(x => x.parentId == device).ToList();
                return tagList1;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        #endregion Device

        #region Driver

        public Connection GetConnectionFromDriver(Guid pr, Guid drv)
        {
            try
            {
                Project pp = getProject(pr);

                foreach (Connection cn in pp.connectionList)
                {
                    if (cn.Idrv.ObjId == drv)
                        return cn;
                }

                throw new ApplicationException("connection is not found");
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        #endregion Driver

        #region Tag

        public Boolean addTag(Guid projId, Guid tagFolId, Tag tg)
        {
            try
            {
                Project pr = getProject(projId);

                tg.PrCon = this;
                tg.Proj = pr;

                while (getProject(projId).tagsList.Exists(x => x.tagName == tg.tagName))
                    tg.tagName = tg.tagName + "_Copy";

                tg.parentId = tagFolId;

                Device dev = getDevice(projId, tagFolId);
                tg.connId = dev.parentId;
                tg.deviceAdress = dev.adress;

                Connection cn = getConnection(projId, tg.connId);
                tg.idrv = cn.Idrv;

                ((ITableView)cn).Children.Add(tg);

                ((ITableView)dev).Children.Add(tg);

                pr.tagsList.Add(tg);
                ((ITreeViewModel)dev).Children.Add(tg);

                ((ITableView)pr).Children.Add(tg);

                if (addTagEv != null)
                    addTagEv(pr, new ProjectEventArgs(tg));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean removeTag(Guid projId, Guid tagId)
        {
            try
            {
                Project pr = getProject(projId);

                Tag tg = pr.tagsList.Find(x => x.objId.Equals(tagId));
                Device dev = getDevice(projId, tg.parentId);

                pr.tagsList.Remove(tg);
                ((ITreeViewModel)dev).Children.Remove(tg);

                foreach (Connection cn in pr.connectionList)
                    ((ITableView)cn).Children.Remove(tg);

                foreach (Device dev1 in pr.DevicesList)
                    ((ITableView)dev1).Children.Remove(tg);

                ((ITableView)pr).Children.Remove(tg);

                if (removeTagEv != null)
                    removeTagEv(pr, new ProjectEventArgs(tg));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Tag getTag(Guid projId, Guid tagId)
        {
            try
            {
                Project pr = getProject(projId);
                Tag tg = pr.tagsList.Find(x => x.objId.Equals(tagId));
                return tg;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        #endregion Tag

        #region InTag

        public Boolean AddIntTag(Guid projId, InTag tag)
        {
            try
            {
                Project pr = getProject(projId);
                tag.Proj = pr;
                tag.idrv = pr.InternalTagsDrv;
                tag.PrCon = this;

                pr.InTagsList.Add(tag);
                ((ITreeViewModel)pr.InternalTagsDrv).Children.Add(tag);

                ((ITableView)pr).Children.Add(tag);

                if (addIntTagEv != null)
                    addIntTagEv(pr, new ProjectEventArgs(tag));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean RemoveIntTag(Guid projId, Guid tagId)
        {
            try
            {
                Project pr = getProject(projId);

                InTag buff = pr.InTagsList.Find(x => x.objId == tagId);

                pr.InTagsList.RemoveAll(x => x.objId == tagId);
                ((ITreeViewModel)pr.InternalTagsDrv).Children.Remove(buff);

                foreach (Connection cn in pr.connectionList)
                    ((ITableView)cn).Children.Remove(buff);

                foreach (Device dev1 in pr.DevicesList)
                    ((ITableView)dev1).Children.Remove(buff);

                ((ITableView)pr).Children.Remove(buff);

                if (removeIntTagEv != null)
                    removeIntTagEv(pr, new ProjectEventArgs(buff));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public InTag GetIntTag(Guid projId, Guid objid)
        {
            try
            {
                return getProject(projId).InTagsList.Find(x => x.objId == objid);
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        #endregion InTag

        #region ITag

        public void ITagChanged(ITag ir)
        {
            try
            {
                Object obj = getElementById(ir.Id);

                if (obj.GetType() == typeof(Tag))
                {
                    if (editTagEv != null)
                        editTagEv(((Tag)ir).Proj, new ProjectEventArgs(obj));
                }
                else if (obj.GetType() == typeof(InTag))
                {
                    if (editIntTagEv != null)
                        editIntTagEv(((InTag)ir).Proj, new ProjectEventArgs(obj));
                }
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        public List<ITag> GetAllITags(Guid Pr_, Guid Obj_)
        {
            try
            {
                List<ITag> ITagList = new List<ITag>();

                Type SelectedObject = getElementById(Pr_, Obj_).GetType();

                Project Pr = getProject(Pr_);

                if (SelectedObject == typeof(Project))
                {
                    ITagList.AddRange((from tg in Pr.tagsList select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(InternalTagsDriver))
                {
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(Connection))
                {
                    ITagList.AddRange((from tg in getAllTagsFromConnection(Pr_, Obj_) select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(Device))
                {
                    ITagList.AddRange((from tg in getAllTagsFromDevice(Pr_, Obj_) select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(Tag))
                {
                    ITagList.Add((ITag)getTag(Pr_, Obj_));
                }
                else if (SelectedObject == typeof(InTag))
                {
                    ITagList.Add((ITag)GetIntTag(Pr_, Obj_));
                }
                else
                {
                    return null;
                }

                return ITagList;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public List<ITag> GetITagsSel(Guid Pr_, Guid Obj_)
        {
            try
            {
                List<ITag> ITagList = new List<ITag>();

                Type SelectedObject = getElementById(Pr_, Obj_).GetType();

                Project Pr = getProject(Pr_);

                if (SelectedObject == typeof(Project))
                {
                    ITagList.AddRange((from tg in Pr.tagsList select (ITag)tg).ToList());
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(InternalTagsDriver))
                {
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(Connection))
                {
                    ITagList.AddRange((from tg in getAllTagsFromConnection(Pr_, Obj_) select (ITag)tg).ToList());
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(Device))
                {
                    ITagList.AddRange((from tg in getAllTagsFromDevice(Pr_, Obj_) select (ITag)tg).ToList());
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(Tag))
                {
                    ITagList.Add((ITag)getTag(Pr_, Obj_));
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }
                else if (SelectedObject == typeof(InTag))
                {
                    ITagList.Add((ITag)GetIntTag(Pr_, Obj_));
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }
                else
                {
                    throw new ApplicationException("Element not exists");
                }

                return ITagList;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public List<ITag> GetAllITags(Guid Pr1, Guid Obj1, Boolean TabVisble, Boolean GraphVisible)
        {
            try
            {
                List<ITag> ITagList = new List<ITag>();

                Type SelectedObject = getElementById(Pr1, Obj1).GetType();

                Project Pr = getProject(Pr1);

                if (SelectedObject == typeof(Project))
                {
                    ITagList.AddRange((from tg in Pr.tagsList select (ITag)tg).ToList());
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());
                }

                else if (SelectedObject == typeof(InternalTagsDriver))
                    ITagList.AddRange((from tg in Pr.InTagsList select (ITag)tg).ToList());

                else if (SelectedObject == typeof(Connection))
                    ITagList.AddRange((from tg in getAllTagsFromConnection(Pr1, Obj1) select (ITag)tg).ToList());

                else if (SelectedObject == typeof(Device))
                    ITagList.AddRange((from tg in getAllTagsFromDevice(Pr1, Obj1) select (ITag)tg).ToList());

                else if (SelectedObject == typeof(Tag))
                    ITagList.Add((ITag)getTag(Pr1, Obj1));

                else if (SelectedObject == typeof(InTag))
                    ITagList.Add((ITag)GetIntTag(Pr1, Obj1));

                else
                    throw new ApplicationException("Element not exists");

                if (TabVisble && GraphVisible)
                    return (from t in ITagList where t.GrVisibleTab && t.GrEnable select t).ToList();

                else if (!TabVisble && GraphVisible)
                    return (from t in ITagList where t.GrEnable select t).ToList();

                else if (TabVisble && !GraphVisible)
                    return (from t in ITagList where t.GrVisibleTab select t).ToList();

                else
                    return (from t in ITagList select t).ToList();
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public List<ITag> GetAllITagsForDriver(Guid Pr_, Guid Driver_)
        {
            try
            {
                Project Pr = getProject(Pr_);

                if (Driver_ != IntTagsGuid && Driver_ != ScriptGuid)
                {
                    List<Tag> tss = Pr.tagsList.FindAll(x => x.idrv.ObjId == Driver_);

                    if (tss.Count > 0) return (from cc in tss select (ITag)cc).ToList();
                    else return new List<ITag>();
                }
                else if (Driver_ == IntTagsGuid)
                {
                    if (Pr.InTagsList.Count > 0) return (from cc in Pr.InTagsList select (ITag)cc).ToList();
                    else return new List<ITag>();
                }
                else
                    return new List<ITag>();
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public List<ITag> GetAllITags()
        {
            try
            {
                if (projectList.Count > 0)
                {
                    Project Pr = projectList.First();
                    List<ITag> buff1 = Pr.tagsList.Cast<ITag>().ToList();
                    List<ITag> buff2 = Pr.InTagsList.Cast<ITag>().ToList();
                    buff1.AddRange(buff2);

                    return buff1;
                }
                else
                    return null;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        #endregion ITag

        #region InFile

        public Boolean AddInFile(Guid projId, InFile file)
        {
            try
            {
                Project pr = getProject(projId);
                pr.FileList.Add(file);

                if (addInFileEv != null)
                    addInFileEv(pr, new ProjectEventArgs(file));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean RemoveInFile(Guid projId, Guid file)
        {
            try
            {
                Project pr = getProject(projId);
                pr.FileList.RemoveAll(x => x.objId == file);

                if (removeInFileEv != null)
                    removeInFileEv(pr, new ProjectEventArgs(file));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public InFile GetInFile(Guid projId, Guid file)
        {
            try
            {
                Project pr = getProject(projId);
                return pr.FileList.Find(x => x.objId == file);
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        #endregion InFile

        #region ScriptFile

        public ScriptFile GetScriptFile(Guid projId, Guid file)
        {
            try
            {
                Project pr = getProject(projId);
                return pr.ScriptFileList.Find(x => x.objId == file);
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return null;
            }
        }

        public Boolean AddScriptFile(Guid projId, ScriptFile file)
        {
            try
            {
                Project pr = getProject(projId);

                file.Proj = pr;
                file.PrCon = pr.PrCon;

                pr.ScriptFileList.Add(file);
                ((ITreeViewModel)pr.ScriptEng).Children.Add(file);

                if (addScriptFileEv != null)
                    addScriptFileEv(pr, new ProjectEventArgs(file));

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        public Boolean RemoveScriptFile(Guid projId, Guid file)
        {
            try
            {
                Project pr = getProject(projId);

                ScriptFile sf = GetScriptFile(projId, file);

                if (sf != null)
                {
                    if (((ITreeViewModel)pr.ScriptEng).Children.ToList().Exists(x => ((ScriptFile)x).objId == file))
                        ((ITreeViewModel)pr.ScriptEng).Children.Remove(sf);

                    pr.ScriptFileList.Remove(sf);

                    if (removeScriptFileEv != null)
                        removeScriptFileEv(pr, new ProjectEventArgs(file));
                }

                return true;
            }
            catch (Exception Ex)
            {
                ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
                return false;
            }
        }

        #endregion ScriptFile

        #region GitHub

        public static async Task<string> GetVersionFromGitHub()
        {
            string fileUrl = "https://raw.githubusercontent.com/dgsoftlabs/FenixModbusS7/master/version.xml";

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string content = await client.GetStringAsync(fileUrl);
                    return content;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static Version ParseVersionFromContent(string content)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(content);
                XmlNode node = doc.DocumentElement.SelectSingleNode("/Fenix/version");
                string version = node.InnerText;
                return new Version(version);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string ParseUrlFromContent(string content)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(content);
                XmlNode node = doc.DocumentElement.SelectSingleNode("/Fenix/url");
                return node.InnerText;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        public override string ToString()
        {
            return "ProjectContainer";
        }
    }
}