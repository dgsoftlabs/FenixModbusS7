using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class WebServer : IDisposable, ITreeViewModel, INotifyPropertyChanged
    {
        [field: NonSerialized]
        [XmlIgnore]
        public HttpListener _listener = new HttpListener();

        [field: NonSerialized]
        private ProjectContainer projCon_;

        [Browsable(false)]
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
        private Project Proj_;

        [Browsable(false)]
        [XmlIgnore]
        public Project Proj
        {
            get { return Proj_; }
            set { Proj_ = value; }
        }

        [field: NonSerialized]
        [XmlIgnore]
        public Func<HttpListenerContext, byte[]> _responderMethod;

        private Boolean IsExpand_;

        [Browsable(false)]
        public Boolean IsExpand
        {
            get { return IsExpand_; }
            set { IsExpand_ = value; }
        }

        private Guid ObjId_;

        [Browsable(false)]
        [XmlElement(ElementName = "Id")]
        public Guid ObjId
        {
            get { return ObjId_; }
            set { ObjId_ = value; }
        }

        private List<string> Prefixes_ = new List<string>();

        [Browsable(true), Category("03 Adresses"), DisplayName("Prefixes"), Description("Allowed Adress")]
        [TypeConverter(typeof(EmptyConverter))]
        public string[] Prefixes
        {
            get
            {
                try
                {
                    return _listener.Prefixes.Where(x => x != String.Empty).ToArray();
                }
                catch (Exception) { return null; }
            }
            set
            {
                try
                {
                    _listener.Prefixes.Clear();
                    Prefixes_.Clear();

                    foreach (string s in value)
                        Prefixes_.Add(s);

                    foreach (string s in value)
                        _listener.Prefixes.Add(s);
                }
                catch (Exception)
                {
                }
            }
        }

        private List<UserClass> Users_ = new List<UserClass>();

        [Browsable(true), Category("01 Users"), DisplayName("Users")]
        [XmlElement(ElementName = "Users", Type = typeof(List<UserClass>))]
        [TypeConverter(typeof(EmptyConverter))]
        public List<UserClass> Users
        {
            get { return Users_; }
            set
            {
                foreach (UserClass s in value)
                    Users_.Add(s);
            }
        }

        private AuthenticationSchemes Auth_;

        [Browsable(true), Category("02 Authentication"), DisplayName("Schema"), Description("Authentication Schema")]
        [TypeConverter(typeof(BlockAuthConverter))]
        public AuthenticationSchemes Auth
        {
            get
            {
                return _listener.AuthenticationSchemes;
            }
            set
            {
                Auth_ = value;
                _listener.AuthenticationSchemes = value;
            }
        }

        public ITreeViewModel DirSearch(string sDir, ITreeViewModel El)
        {
            ITreeViewModel buff = null;

            foreach (ITreeViewModel d in El.Children)
            {
                if (((CusFile)d).FullName == sDir)
                    return d;

                foreach (ITreeViewModel f in d.Children)
                {
                    if (((CusFile)f).FullName == sDir)
                        return f;

                    buff = DirSearch(sDir, d);

                    if (buff != null)
                        return buff;
                }
            }

            return null;
        }

        private Boolean Active_;

        [Browsable(false)]
        public Boolean Acitve
        {
            get { return Active_; }
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
                return "HttpServer";
            }

            set
            {
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

        Color ITreeViewModel.Clr
        {
            get { return Color.White; }
            set { }
        }

        public WebServer(Func<HttpListenerContext, byte[]> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "WebServer.Coinstr  - Needs Windows XP SP2, Server 2003 or later.");

            ObjId_ = new Guid("11111111-1111-1111-1111-111111111111");

            _listener = new HttpListener();
            Prefixes_.Add("http://+:80/");
            _listener.Prefixes.Add("http://+:80/");

            Auth = AuthenticationSchemes.Anonymous;

            _Children = new ObservableCollection<object>();

            if (method != null)
                _responderMethod = method;
        }

        public WebServer()
        {
        }

        public void Run()
        {
            _listener.Start();
            Active_ = true;

            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        System.Threading.ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;

                            try
                            {
                                byte[] buf = _responderMethod(ctx);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch { Active_ = false; }
                            finally
                            {
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { }
            });
        }

        public void Stop()
        {
            try
            {
                _listener.Stop();
                Active_ = false;
            }
            catch (Exception)
            {
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "WebServer.Coinstr  - Needs Windows XP SP2, Server 2003 or later.");

            ObjId_ = new Guid("11111111-1111-1111-1111-111111111111");

            _listener = new HttpListener();

            _listener.AuthenticationSchemes = Auth_;

            if (Prefixes_ == null)
            {
                Prefixes_ = new List<string>();
                Prefixes_.Add("http://+:80/");
                _listener.Prefixes.Add("http://+:80/");
            }
            else
            {
                if (Prefixes_.Count == 0)
                {
                    Prefixes_.Add("http://+:80/");
                    _listener.Prefixes.Add("http://+:80/");
                }
                else
                {
                    foreach (string s in Prefixes_)
                        _listener.Prefixes.Add(s);
                }
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
                    if (_listener == null)
                        return;

                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                        _listener.Close();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}
