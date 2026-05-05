using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace FenixServer
{
    public partial class ManagerView : Form
    {
        #region Fields

        private FileSystemWatcher fsWatcher;
        private ProjectContainer PrCon = new ProjectContainer();
        private string simPath;
        private Project Pr;
        public List<AlarmEvent> alarmList = new List<AlarmEvent>();
        public List<GraphElement> graphList = new List<GraphElement>();
        private byte[] EmptyBytes = new byte[] { 0 };
        private int sendBytes = 0;
        private int reciveBytes = 0;
        private int WebSend = 0;
        private int WebRecive = 0;
        private string AuthPage = "\\auth.html";
        private string IndexPage = "index.html";
        private PropManager prManag;
        private GridManager grManag;
        private EventManag evManag;
        private bool closeMarker;
        private bool shoudHided;
        private TaskDefinition td;
        private int ProbeCounter = 100;
        private Boolean IsAutoStart;

        public Boolean Autostart
        {
            get
            {
                using (TaskService ts = new TaskService())
                {
                    return ts.GetTask(PrCon.TaskName) != null;
                }
            }
            set
            {
                using (TaskService ts = new TaskService())
                {
                    if (value)
                    {
                        if (ts.GetTask(PrCon.TaskName) == null)
                        {
                            // Create a new task definition and assign properties
                            td = ts.NewTask();
                            td.RegistrationInfo.Description = "Start Fenix Server";
                            td.Principal.RunLevel = TaskRunLevel.Highest;

                            // Create a trigger that will fire the task at this time every other day
                            LogonTrigger lgTr = new LogonTrigger
                            {
                                UserId = Environment.UserName
                            };
                            td.Triggers.Add(lgTr);

                            // Create an action that will launch Notepad whenever the trigger fires
                            td.Actions.Add(new ExecAction(Application.ExecutablePath, "-r", null));

                            // Register the task in the root folder
                            ts.RootFolder.RegisterTaskDefinition(PrCon.TaskName, td);
                        }
                    }
                    else
                    {
                        if (ts.GetTask(PrCon.TaskName) != null)
                        {
                            // Remove the task we just created
                            ts.RootFolder.DeleteTask(PrCon.TaskName);
                        }
                    }
                }
            }
        }

        #endregion Fields

        #region Konstruktor

        public ManagerView(string[] args)
        {
            try
            {
                InitializeComponent();

                dockPan.Theme = new VS2015LightTheme();

                //Elementy dock
                prManag = new PropManager();
                prManag.Show(dockPan, DockState.DockLeft);

                grManag = new GridManager();
                grManag.Show(dockPan, DockState.Document);

                evManag = new EventManag();
                evManag.Show(dockPan, DockState.DockBottomAutoHide);

                //utworzenie kontenera
                PrCon = new ProjectContainer();

                //Informacja o tagach
                PrCon.ApplicationError = new EventHandler(AppError);
                PrCon.addProjectEv += new EventHandler<ProjectEventArgs>(addProjectEvent);
                evManag.btClear.Click += new EventHandler(btClear_Click);

                //Synchronizacja autostartu
                IsAutoStart = Autostart;

                ntfIcon.Icon = Properties.Resources.HttpServer;
                ntfIcon.Visible = true;

                #region Opcje startowe

                //args[0] - mode
                if (args.Length > 0)
                {
                    if (args[0] == "-s")
                    {
                        //Ustawienia naglowka
                        this.Text = "Fenix Server " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " [Simulation Mode]";

                        //Sciezka symulacujna
                        this.simPath = (string)Registry.GetValue(PrCon.RegUserRoot, PrCon.LastPathKey, "");

                        if (!string.IsNullOrEmpty(simPath))
                        {
                            //zdarzania
                            fsWatcher = new FileSystemWatcher(Path.GetDirectoryName(simPath));
                            fsWatcher.Changed += new FileSystemEventHandler(fsWatcher_Changed);
                            fsWatcher.Deleted += new FileSystemEventHandler(fsWatcher_Changed);
                            fsWatcher.Created += new FileSystemEventHandler(fsWatcher_Changed);
                            fsWatcher.Renamed += new RenamedEventHandler(fsWatcher_Changed);

                            // Begin watching.
                            fsWatcher.IncludeSubdirectories = false;
                            fsWatcher.EnableRaisingEvents = true;

                            PrCon.openProjects(simPath);

                            if (PrCon.projectList.Count > 0)
                                start();
                        }
                    }

                    //Autostart
                    else if (args?[0] == "-r")
                    {
                        string strp = (string)Registry.GetValue(PrCon.RegUserRoot, PrCon.LastPathKey, "");
                        if (!String.IsNullOrEmpty(strp))
                        {
                            if (PrCon.openProjects(strp))
                            {
                                start();
                                shoudHided = true;
                            }
                        }

                        this.Text = "Fenix Server " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " [Autorun]";
                    }
                    else
                    {
                        this.Text = "Fenix Server " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    }
                }

                #endregion Opcje startowe

                #region Info o Adminie

                //sparawdzenie czy jest admin
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (identity != null)
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);

                    if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                        Text = Text + " (Administrator)";
                }

                #endregion Info o Adminie
            }
            catch (Exception Ex)
            {
                AlarmEvent ev = new AlarmEvent("ManagerView: " + Ex.Message);
                alarmList.Add(ev);
                evManag.addEvent(ev);
            }
        }

        #endregion Konstruktor

        #region Internal Method

        public byte[] SendResponse(HttpListenerContext context)
        {
            try
            {
                #region DodatkoweDane

                grManag.Invoke(new System.Action(() =>
                {
                    WebSend++;
                    WebRecive++;

                    DataGridViewRow rw = (from r in grManag.dgvMain.Rows.Cast<DataGridViewRow>() where ((Guid)r.Tag) == PrCon.ServerGuid select r).ToList()[0];

                    rw.Cells["conRecive"].Value = WebRecive.ToString();
                    rw.Cells["conSend"].Value = WebSend.ToString();
                    rw.Cells["conStatus"].Value = Pr.WebServer1.Acitve;
                }));

                #endregion DodatkoweDane

                string reqPath = context.Request.RawUrl;

                #region Autoryzacja

                if (context.Request.IsAuthenticated || Pr.WebServer1.Auth != AuthenticationSchemes.Anonymous)
                {
                    //Sprawdzanie czy jest jakis user
                    if (context.User != null)
                    {
                        var identy = (HttpListenerBasicIdentity)context.User.Identity;
                        string name = identy.Name;
                        string pass = identy.Password;

                        #region Brak Autoryzacji

                        if (Pr.WebServer1.Users.Find(x => x.Name == name && x.Pass == pass) == null)
                        {
                            context.Response.StatusCode = 403;

                            string fAuth = Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog + AuthPage;

                            if (File.Exists(fAuth))
                                return UTF8Encoding.UTF8.GetBytes(File.ReadAllText(fAuth));
                            else
                                return UTF8Encoding.UTF8.GetBytes("Access Denied!");
                        }

                        #endregion Brak Autoryzacji

                        #region Autoryzacja

                        else
                        {
                            //Metody GET plikowe
                            if (context.Request.HttpMethod == "GET")
                            {
                                return GetMethodRes(reqPath);
                            }
                            //metody POST
                            else if (context.Request.HttpMethod == "POST")
                            {
                                return PostMethodRes(reqPath);
                            }
                            //Inne
                            else
                            {
                                return UTF8Encoding.UTF8.GetBytes(File.ReadAllText("Exception during autorization!"));
                            }
                        }

                        #endregion Autoryzacja
                    }
                    //brak usera
                    else
                    {
                        return UTF8Encoding.UTF8.GetBytes(File.ReadAllText("Problem with USER NAME"));
                    }
                }

                #endregion Autoryzacja

                #region Dostep Anonimowy

                else
                {
                    //obsluga rzadanie GET z klienta
                    if (context.Request.HttpMethod == "GET")
                        return GetMethodRes(reqPath);

                    //Obsluga POST
                    else if (context.Request.HttpMethod == "POST")
                        return PostMethodRes(reqPath);

                    //Rzadanie inne niz POST lub GET
                    else
                        return UTF8Encoding.UTF8.GetBytes(File.ReadAllText("Exception during autorization!"));
                }

                #endregion Dostep Anonimowy
            }
            catch (Exception Ex)
            {
                AlarmEvent ev = new AlarmEvent("SendResponse: " + Ex.Message);
                alarmList.Add(ev);
                evManag.addEvent(ev);

                return UTF8Encoding.UTF8.GetBytes(String.Empty);
            }
        }

        /// Obsluga rzadania Get
        private byte[] GetMethodRes(string s)
        {
            //PUSTY STRING INDEX.HTML
            if (String.IsNullOrEmpty(s) || String.IsNullOrWhiteSpace(s) || s == "/")
            {
                //Sprawdznie czy plik istnieje
                if (File.Exists(Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog + "\\" + IndexPage))
                {
                    return UTF8Encoding.UTF8.GetBytes(File.ReadAllText(Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog + "\\" + IndexPage));
                }
                //plik konfiguracji nie istnieje
                else
                {
                    alarmList.Add(new AlarmEvent("GET_METHOD_FAULT - 'index.html' does't exist"));
                    return EmptyBytes;
                }
            }

            //POBRANIE PLIKOW
            else
            {
                //Sprawdzenie czy plik istnieje

                string k = Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog + s.Replace('/', '\\');
                if (File.Exists(k))
                {
                    //Pobranie pliku
                    byte[] data = File.ReadAllBytes(Path.GetDirectoryName(Pr.path) + PrCon.HttpCatalog + "\\" + s);
                    return data;
                }
                else
                {
                    AlarmEvent ev = new AlarmEvent(String.Format("GET_METHOD_FAULT - '{0}' does't exist. Please add this file using Fenix Manager", s));
                    alarmList.Add(ev);
                    evManag.addEvent(ev);
                    return EmptyBytes;
                }
            }
        }

        /// Obsluga rzadanie POST
        private byte[] PostMethodRes(string s)
        {
            //Zabranie / z danych pozostalosc po starych metodach
            s = s.Remove(0, 1);

            //obsluga rzadan starego typu
            if (s == "@table" || s == "@timer" || s == "@user" || s == "@machine" || s.IndexOf("@tag") > -1)
                return OldReqData(s);

            //Nowy typ danych
            else
            {
                //Sprawdzanie wzorca
                // OBJECT \ NAME \ FIELD \ VALUE (OPTIONAL)
                if (Regex.IsMatch(s, @"^[A-Z]\w{1,}/\w{1,}/(?:[A-Z]\w{1,}/\d{1,}[.,]\d{1,}|[A-Z]\w{1,})", RegexOptions.IgnoreCase))
                {
                    //podzielenie zapytanie na na kawałki [object][name][param][value : optional]
                    string[] buff = s.Split('/');
                    //GET
                    if (buff.Length == 3)
                        return GetValFct(buff[0], buff[1], buff[2]);
                    //SET
                    else
                        return SetValFct(buff[0], buff[1], buff[2], buff[3]);
                }
                else
                {
                    AlarmEvent ev = new AlarmEvent("PostMethodRes: " + "Wrong request-" + s);
                    alarmList.Add(ev);
                    evManag.addEvent(ev);

                    return EmptyBytes;
                }
            }
        }

        /// Funkcja ustawiająca parametry
        private byte[] SetValFct(string obj, string name, string param, string value)
        {
            // Wyczysc liste alamrow
            if (obj == "Server" && name == "Alarms" && param == "Clr" && value == "*")
            {
                evManag.dgvMain.Invoke(new System.Action(() =>
                {
                    evManag.dgvMain.Rows.Clear();
                }));

                this.Invoke(new System.Action(() =>
                {
                    alarmList.Clear();
                }));

                return UTF8Encoding.UTF8.GetBytes("Alarms cleared!");
            }
            else if (obj == "Server" && name == "Communication" && param == "Res" && value == "*")
            {
                Invoke(new System.Action(() =>
                {
                    stop();
                    ReOpen();
                    start();
                }));

                return UTF8Encoding.UTF8.GetBytes("Communication restarted!");
            }
            else if (obj == "Server" && name == "Buffor" && param == "Set")
            {
                ProbeCounter = Convert.ToInt32(value);
                return UTF8Encoding.UTF8.GetBytes(ProbeCounter.ToString());
            }
            else
            {
                string cmd = String.Format("Project.Set{0}{1}(\"{2}\",\"{3}\")", obj, param, name, value);
                return UTF8Encoding.UTF8.GetBytes((string)Pr.ScriptCon.Eval(cmd));
            }
        }

        /// Funkcja zwracająca dane
        private byte[] GetValFct(string obj, string name, string param)
        {
            //W przypadku Event nie bedzie mozna sie odwolac do projektu trzeba go oddac z tąd
            if (obj == "Events" && name == "All" && param == "All")
            {
                string ret = JsonConvert.SerializeObject(alarmList.ToArray());
                return UTF8Encoding.UTF8.GetBytes(ret);
            }
            //Wprzypadku gdy mamy graph
            else if (obj == "Graph" && name == "All" && param == "All")
            {
                string ret = JsonConvert.SerializeObject(graphList.ToArray());
                return UTF8Encoding.UTF8.GetBytes(ret);
            }
            else if (obj == "Server" && name == "Buffor" && param == "Get")
            {
                return UTF8Encoding.UTF8.GetBytes(ProbeCounter.ToString());
            }
            else
            {
                string cmd = String.Format("Project.Get{0}{1}(\"{2}\")", obj, param, name);
                return UTF8Encoding.UTF8.GetBytes((string)Pr.ScriptCon.Eval(cmd));
            }
        }

        /// Pokaz Help
        private void ShowHelp()
        {
            System.Diagnostics.Process.Start(PrCon.HelpWebSite);
        }

        #endregion Internal Method

        #region Commands

        /// start
        private void start()
        {
            try
            {
                //Start
                Pr.WebServer1.Run();
                Pr.WebServer1._responderMethod = SendResponse;

                //Przyciski
                btStartWeb.Enabled = false;
                btStopWeb.Enabled = true;
                prManag.propGrid.Enabled = false;

                //Jezeli brakuje tagów do odczytu
                if (((ITableView)Pr).Children.Count == 0)
                    throw new ApplicationException("There is no Tags to read");

                //Start Sterowników
                foreach (IDriverModel idrv in ((IDriversMagazine)Pr).Children)
                {
                    idrv.refreshedCycle += Idrv_refreshedCycle;
                    idrv.dataSent += idrv_sendLogInfo;
                    idrv.dataRecived += idrv_reciveLogInfo;
                    idrv.error += Idrv_error;
                    idrv.activateCycle((from t in Pr.tagsList where t.idrv.ObjId == idrv.ObjId select (ITag)t).ToList());
                }

                //utworz dane dla graph
                graphList.Clear();
                foreach (ITag tg in ((ITableView)Pr).Children)
                    graphList.Add(new GraphElement(tg.Name));
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        /// Stop
        private void stop()
        {
            try
            {
                if (Pr == null)
                    return;

                //Stop
                if (Pr.WebServer1.Acitve)
                    Pr.WebServer1.Stop();

                //Przyciski
                btStartWeb.Enabled = true;
                btStopWeb.Enabled = false;
                prManag.propGrid.Enabled = true;

                //Start Sterowników
                foreach (IDriverModel idrv in ((IDriversMagazine)Pr).Children)
                {
                    idrv.deactivateCycle();
                    idrv.refreshedCycle -= Idrv_refreshedCycle;
                    idrv.dataSent -= idrv_sendLogInfo;
                    idrv.dataRecived -= idrv_reciveLogInfo;
                    idrv.error -= Idrv_error;
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        //open
        private void Open()
        {
            try
            {
                //Zabezpiecznie
                if (Pr != null)
                {
                    MessageBox.Show("Project is already load. Please close project and try again!");
                    return;
                }

                //Sprawdzenie rejestru czy istnieje poprzednie otwzrcie
                string strp = (string)Registry.GetValue(PrCon.RegUserRoot, PrCon.LastPathKey, Application.StartupPath + "\\Project.psf");
                ofd_Menager.InitialDirectory = Path.GetDirectoryName(strp);

                ofd_Menager.Filter = "XML Files (*.psx)|*.psx";
                ofd_Menager.Multiselect = false;

                if (ofd_Menager.ShowDialog() == DialogResult.OK)
                {
                    PrCon.openProjects(ofd_Menager.FileName);
                    Registry.SetValue(PrCon.RegUserRoot, PrCon.LastPathKey, ofd_Menager.FileName);
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void CloseAll()
        {
            stop();

            mPath.Text = "-";
            btStartWeb.Enabled = false;
            btStopWeb.Enabled = false;

            alarmList.Clear();
            graphList.Clear();

            grManag.dgvMain.Rows.Clear();
            prManag.propGrid.SelectedObject = null;
            evManag.dgvMain.Rows.Clear();
        }

        //hide
        private void HideForm()
        {
            try
            {
                ntfIcon.BalloonTipText = "Fenix Server hided!";
                ntfIcon.ShowBalloonTip(1000);
                Hide();
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        //showForm
        private void ShowForm()
        {
            this.Show();
        }

        //Ponowne otwarcie w celu odswierzenia
        private void ReOpen()
        {
            PrCon.closeAllProject(true);

            CloseAll();

            PrCon.openProjects(simPath);
        }

        #endregion Commands

        #region Internal Events

        //Start Event
        private void btStartButton1_Click(object sender, EventArgs e)
        {
            start();
        }

        //Stop Event
        private void btStopButton_Click(object sender, EventArgs e)
        {
            stop();
        }

        //File Changed
        private void fsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            //Synchroizacja miedzy watakmi
            this.Invoke(new System.Action(() =>
            {
                try
                {
                    //zabzpiecznie przed podwojnym dzialeniem
                    fsWatcher.EnableRaisingEvents = false;

                    if (!e.FullPath.Contains("ProjDatabase") && !e.FullPath.Contains(".cs"))
                    {
                        stop();
                        ReOpen();
                        start();
                    }

                    //zabzpiecznie przed podwojnym dzialeniem
                    fsWatcher.EnableRaisingEvents = true;
                }
                catch (Exception Ex)
                {
                    AlarmEvent ev = new AlarmEvent("fsWatcher_Changed : " + Ex.Message);
                    alarmList.Add(ev);
                    evManag.addEvent(ev);
                }
            }));
        }

        //open
        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            Open();
        }

        //open
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        //Close click
        private void closeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            closeMarker = true;
            Close();
        }

        //Notify Icon show App
        private void showServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
        }

        //Notyfi Close App
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeMarker = true;
            Close();
        }

        //Notyfi double click
        private void ntfIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        //Pokazanie autostart
        private void projectToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (IsAutoStart)
                mnAutostart.Image = Properties.Resources.check;
            else
                mnAutostart.Image = null;
        }

        //autostart click
        private void mnAutostart_Click(object sender, EventArgs e)
        {
            try
            {
                if (Autostart)
                {
                    Autostart = false;
                    IsAutoStart = false;
                }
                else
                {
                    Autostart = true;
                    IsAutoStart = true;
                }
            }
            catch (Exception Ex)
            {
                AlarmEvent ev = new AlarmEvent("mnAutostart_Click: " + Ex.Message);
                alarmList.Add(ev);
                evManag.addEvent(ev);
            }
        }

        //Formularz sie pokazal
        private void ManagerView_Shown(object sender, EventArgs e)
        {
            if (shoudHided)
            {
                HideForm();
            }
        }

        //About
        private void About1_Click(object sender, EventArgs e)
        {
            AboutMe AboutMe_ = new AboutMe();
            AboutMe_.ShowDialog();
        }

        //Help
        private void HelpView1_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        //Form Closing
        private void ManagerView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!closeMarker)
            {
                e.Cancel = true;
                HideForm();
            }
        }

        //Form Closed
        private void ManagerView_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                stop();
            }
            catch (Exception Ex)
            {
                AlarmEvent ev = new AlarmEvent("TableView.tableView_FormClosed: " + Ex.Message);
                alarmList.Add(ev);
                evManag.addEvent(ev);
            }
        }

        //Close project
        private void closeProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Usuniecie Trybu Sim.
            if (Text.Contains("[Simulation Mode]"))
            {
                Text = Text.Replace("[Simulation Mode]", " ");
                fsWatcher.Changed -= new FileSystemEventHandler(fsWatcher_Changed);
                fsWatcher.Deleted -= new FileSystemEventHandler(fsWatcher_Changed);
                fsWatcher.Created -= new FileSystemEventHandler(fsWatcher_Changed);
                fsWatcher.Renamed -= new RenamedEventHandler(fsWatcher_Changed);
                fsWatcher.EnableRaisingEvents = false;
            }

            PrCon.closeAllProject(true);

            CloseAll();

            Pr = null;
        }

        #endregion Internal Events

        #region External Events

        //Target dla dodania taga
        private void addProjectEvent(object sender, ProjectEventArgs e)
        {
            try
            {
                //Projekt
                Project pr = (Project)e.element;
                this.Pr = pr;

                //Wlasciwosci projektu
                prManag.propGrid.SelectedObject = pr.WebServer1;

                //Dodanie poloczenia web
                grManag.dgvMain.Rows.Add();
                grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conName"].Value = "Fenix Server";
                grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conType"].Value = "Http";
                grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conStatus"].Value = false;
                grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conRecive"].Value = 0;
                grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conSend"].Value = 0;
                grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Tag = PrCon.ServerGuid;

                //dodatkowe czynnosci
                foreach (Connection cn in pr.connectionList)
                {
                    //Wyswietlenie poloczen
                    grManag.dgvMain.Rows.Add();
                    grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conName"].Value = cn.connectionName;
                    grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conType"].Value = cn.DriverName;
                    grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conStatus"].Value = cn.isLive;
                    grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conRecive"].Value = 0;
                    grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Cells["conSend"].Value = 0;
                    grManag.dgvMain.Rows[grManag.dgvMain.Rows.Count - 1].Tag = cn.Idrv.ObjId;
                }

                btStartWeb.Enabled = true;
                mPath.Text = pr.path;
            }
            catch (Exception Ex)
            {
                AlarmEvent ev = new AlarmEvent("FenixServer.addProjectEvent: " + Ex.Message);
                alarmList.Add(ev);
                evManag.addEvent(ev);
            }
        }

        //AppError
        private void AppError(object sender1, EventArgs e1)
        {
            //Delegat
            EventHandler crossDef = delegate (object sender, EventArgs ev)
            {
                ProjectEventArgs e = (ProjectEventArgs)ev;

                if (e.element is Exception)
                {
                    //Dodanie zdarzenia
                    AlarmEvent ev1 = new AlarmEvent("Error: " + ((Exception)e.element).Message);
                    alarmList.Add(ev1);
                    evManag.addEvent(ev1);
                }
                else if (e.element2 is Exception)
                {
                    //Dodanie zdarzenia
                    AlarmEvent ev1 = new AlarmEvent("Error: " + ((Exception)e.element2).Message);
                    alarmList.Add(ev1);
                    evManag.addEvent(ev1);
                }
                else
                {
                    //Dodanie zdarzenia
                    AlarmEvent ev1 = new AlarmEvent("Error: " + e.element1.ToString());
                    alarmList.Add(ev1);
                    evManag.addEvent(ev1);
                }
            };

            try
            {
                //Wywolanie
                this.Invoke(crossDef, sender1, e1);
            }
            catch (Exception Ex)
            {
                AlarmEvent ev = new AlarmEvent("FenixServer.AppError: " + Ex.Message);
                alarmList.Add(ev);
                evManag.addEvent(ev);
            }
        }

        //Wyczysc dane
        private void btClear_Click(object sender, EventArgs e)
        {
            try
            {
                evManag.dgvMain.Rows.Clear();
                alarmList.Clear();
            }
            catch (Exception Ex)
            {
                AlarmEvent ev = new AlarmEvent("FenixServer.addProjectEvent: " + Ex.Message);
                alarmList.Add(ev);
                evManag.addEvent(ev);
            }
        }

        //Dane dostarczone do sterwonika
        private void idrv_reciveLogInfo(object sender1, EventArgs e1)
        {
            //Wywolanie zadarzenia
            grManag.dgvMain.Invoke(new System.Action<object, EventArgs>((sender, e) =>
            {
                try
                {
                    ProjectEventArgs ev = (ProjectEventArgs)e;
                    byte[] dane = (byte[])ev.element;

                    if (dane != null)
                        reciveBytes = reciveBytes + dane.Length;

                    if (grManag.dgvMain.Rows.Count > 0)
                    {
                        if (grManag.dgvMain.Rows.Cast<DataGridViewRow>().ToList().Exists(x => ((Guid)x.Tag).Equals(((IDriverModel)sender).ObjId)))
                        {
                            DataGridViewRow row = grManag.dgvMain.Rows.Cast<DataGridViewRow>().Where(r => ((Guid)r.Tag).Equals(((IDriverModel)sender).ObjId)).First();

                            if (row != null)
                            {
                                row.Cells["conRecive"].Value = reciveBytes.ToString();
                                row.Cells["conStatus"].Value = ((IDriverModel)sender).isAlive.ToString();
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    //Dodanie zdarzenia
                    AlarmEvent ev1 = new AlarmEvent("FenixServer.idrv_reciveLogInfo: " + Ex.Message);
                    alarmList.Add(ev1);
                    evManag.addEvent(ev1);
                }
            }), sender1, e1);
        }

        //Dane wyslane przez sterownik
        private void idrv_sendLogInfo(object sender1, EventArgs e1)
        {
            //Synchronizacja
            grManag.dgvMain.Invoke(new System.Action<object, EventArgs>((sender, e) =>
            {
                try
                {
                    //Projct
                    ProjectEventArgs ev = (ProjectEventArgs)e;
                    byte[] dane = (byte[])ev.element;

                    if (dane != null)
                        sendBytes = sendBytes + dane.Length;

                    if (grManag.dgvMain.Rows.Count > 0)
                    {
                        if (grManag.dgvMain.Rows.Cast<DataGridViewRow>().ToList().Exists(x => ((Guid)x.Tag).Equals(((IDriverModel)sender).ObjId)))
                        {
                            DataGridViewRow row = grManag.dgvMain.Rows.Cast<DataGridViewRow>().Where(r => ((Guid)r.Tag).Equals(((IDriverModel)sender).ObjId)).First();
                            if (row != null)
                            {
                                row.Cells["conSend"].Value = sendBytes.ToString();
                                row.Cells["conStatus"].Value = ((IDriverModel)sender).isAlive.ToString();
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    AlarmEvent ev1 = new AlarmEvent("FenixServer.idrv_sendLogInfo: " + Ex.Message);
                    alarmList.Add(ev1);
                    evManag.addEvent(ev1);
                }
            }), sender1, e1);
        }

        //Refresh sterownika
        private void Idrv_refreshedCycle(object sender, EventArgs e)
        {
            try
            {
                Invoke(new System.Action(() =>
                {
                    //Pobranie tagow
                    List<ITag> tgs = PrCon.GetAllITags(Pr.objId, Pr.objId, false, true);

                    //odswierz dane dla GraphWeb
                    if (graphList.Count > 0)
                    {
                        for (int i = 0; i < graphList.Count; i++)
                        {
                            //Czas
                            TimeSpan span = new System.TimeSpan(DateTime.Parse("1/1/1970").Ticks);
                            DateTime time = DateTime.Now.Subtract(span);
                            long tm = (long)(time.Ticks / 10000);

                            if (tgs[i].TypeData_ == TypeData.BIT)
                            {
                                if (graphList[i].data.Count() < 1)
                                    graphList[i].addDataPoint(new object[] { tm, 0.0 });
                                else
                                {
                                    var lastEl = Convert.ToBoolean(((object[])graphList[i].data.Last())[1]);
                                    if (lastEl != (bool)tgs[i].Value)
                                    {
                                        double pt;
                                        if (!double.TryParse(tgs[i].GetFormatedValue(), out pt))
                                            pt = 0;

                                        graphList[i].addDataPoint(new object[] { tm, Convert.ToDouble(lastEl) });
                                        graphList[i].addDataPoint(new object[] { tm, pt });
                                    }
                                    else
                                    {
                                        double pt;
                                        if (!double.TryParse(tgs[i].GetFormatedValue(), out pt))
                                            pt = 0;

                                        graphList[i].addDataPoint(new object[] { tm, pt });
                                    }
                                }
                            }
                            else
                            {
                                double pt;
                                if (!double.TryParse(tgs[i].GetFormatedValue(), out pt))
                                    pt = 0;

                                graphList[i].addDataPoint(new object[] { tm, pt });
                            }

                            //Ograniczenie casowe sek.
                            long min = ((long)graphList[i].data.First()[0]);
                            var diff = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(min));
                            if (diff > new TimeSpan(0, 0, ProbeCounter))
                            {
                                while (diff > new TimeSpan(0, 0, ProbeCounter))
                                {
                                    diff = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(((long)graphList[i].data.First()[0])));
                                    graphList[i].removeElement(0);
                                }
                            }
                        }
                    }
                }));
            }
            catch (Exception Ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        //beldy ze sterownkow
        private void Idrv_error(object sender, EventArgs e)
        {
            Invoke(new System.Action(() =>
            {
                ProjectEventArgs zd = (ProjectEventArgs)e;
                AlarmEvent ev = new AlarmEvent($"{sender.ToString()}: {zd.element1}");
                alarmList.Add(ev);
                evManag.addEvent(ev);
            }));
        }

        #endregion External Events

        #region Depreticeted Method

        /// Buduje kontrolki
        private string makeTable()
        {
            //Budowanie tabeli
            StringBuilder str = new StringBuilder();

            str.AppendLine("<table>");
            str.AppendLine("<thead>");

            //Kolumny
            str.AppendLine("<tr>");

            //Name
            str.AppendLine("<th>");
            str.AppendLine("Name");
            str.AppendLine("</th>");

            str.AppendLine("<th>");
            str.AppendLine("Device Adress");
            str.AppendLine("</th>");

            str.AppendLine("<th>");
            str.AppendLine("Adress");
            str.AppendLine("</th>");

            str.AppendLine("<th>");
            str.AppendLine("Bit/Byte");
            str.AppendLine("</th>");

            str.AppendLine("<th>");
            str.AppendLine("Date Type");
            str.AppendLine("</th>");

            str.AppendLine("<th>");
            str.AppendLine("Area Data");
            str.AppendLine("</th>");

            str.AppendLine("<th>");
            str.AppendLine("Value");
            str.AppendLine("</th>");

            //str.AppendLine("<th>");
            //str.AppendLine("Set Value");
            // str.AppendLine("</th>");

            str.AppendLine("<th>");
            str.AppendLine("Descritpion");
            str.AppendLine("</th>");

            str.AppendLine("</tr>");
            str.AppendLine("</thead>");

            //Obieg rzadow
            str.AppendLine("<tbody>");

            //interacja
            foreach (ITag tg in PrCon.GetAllITags(Pr.objId, Pr.objId, true, false))
            {
                str.AppendLine("<tr>");

                str.AppendLine("<td>");
                str.AppendLine(tg.Name);
                str.AppendLine("</td>");

                str.AppendLine("<td>");
                str.AppendLine(tg.DevAdress.ToString());
                str.AppendLine("</td>");

                str.AppendLine("<td>");
                str.AppendLine(tg.Adress.ToString());
                str.AppendLine("</td>");

                str.AppendLine("<td>");
                str.AppendLine(tg.BitByte.ToString());
                str.AppendLine("</td>");

                str.AppendLine("<td>");
                str.AppendLine(tg.TypeData_.ToString());
                str.AppendLine("</td>");

                str.AppendLine("<td>");
                str.AppendLine(tg.AreaData);
                str.AppendLine("</td>");

                str.AppendLine("<td class='value'>");
                str.AppendLine(tg.GetFormatedValue());
                str.AppendLine("</td>");

                //str.AppendLine("<td>");
                //str.AppendLine(string.Format("<input type=\"text\" id=\"{0}\" class=\"Tag\" value=\"0\"/>", tg.Name));
                //str.AppendLine("</td>");

                str.AppendLine("<td>");
                str.AppendLine(tg.Description);
                str.AppendLine("</td>");

                str.AppendLine("</tr>");
            }
            str.AppendLine("</tbody>");
            str.AppendLine("</table>");

            return str.ToString();
        }

        /// Oddaje date
        private string makeTimer()
        {
            return DateTime.Now.ToString();
        }

        /// Pobierz Taga
        private string makeTagData(string name)
        {
            try
            {
                if (String.IsNullOrEmpty(name))
                    throw new ApplicationException(String.Format("Problem with the TAG parameters. Param: {0}", name));

                //Parametryzacja
                string[] param = name.Split(':');
                string Selector = param[0];
                string TagName = param[1];

                //Szukanie Taga
                ITag tg = (ITag)Pr.tagsList.Where(x => x.tagName == TagName).ToList()[0];

                if (tg == null)
                    throw new ApplicationException(String.Format("Tag {0} doesnt exists.", TagName));

                //Selekcja danych
                if (Selector == ".value")
                    return tg.GetFormatedValue();
                else if (Selector == ".name")
                    return tg.Name;
                else if (Selector == ".adress")
                    return tg.Adress.ToString();
                else if (Selector == ".devAdress")
                    return tg.DevAdress.ToString();
                else if (Selector == ".description")
                    return tg.Description;
                else if (Selector == ".secAdress")
                    return tg.BitByte.ToString();
                else if (Selector == ".typeData")
                    return tg.TypeData_.ToString();
                else
                    return "Tag Selektor Fault";
            }
            catch (Exception Ex)
            {
                return Ex.Message;
            }
        }

        /// obsluga rzadan starego typu
        private byte[] OldReqData(string s)
        {
            if (s == "@table")
                return UTF8Encoding.UTF8.GetBytes(makeTable());
            else if (s == "@timer")
                return UTF8Encoding.UTF8.GetBytes(makeTimer());
            else if (s == "@user")
                return UTF8Encoding.UTF8.GetBytes("User");
            else if (s == "@machine")
                return UTF8Encoding.UTF8.GetBytes(Environment.MachineName);
            else if (s.IndexOf("@tag") > -1)
            {
                string content = s.Replace("@tag", "");
                return UTF8Encoding.UTF8.GetBytes(makeTagData(content));
            }
            else
            {
                return UTF8Encoding.UTF8.GetBytes("Error");
            }
        }

        #endregion Depreticeted Method
    }
}