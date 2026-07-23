using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    public class GlobalConfiguration
    {
        public List<String> assmemblyPath = new List<string>();

        /// <summary>
        /// Gets or sets the base path used to locate configuration and driver files.
        /// Defaults to the current AppDomain base directory if not set.
        /// </summary>
        public string BasePath { get; set; }

        public GlobalConfiguration()
        {
            BasePath = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                OpenData(Path.Combine(BasePath, "GlobalConfiguration.xml"));
            }
            catch (Exception)
            {
                autoSearchDrv();
            }
        }

        private Boolean SaveData(string path)
        {
            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(List<String>));
                Stream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                xml.Serialize(fs, assmemblyPath);
                fs.Close();
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private Boolean OpenData(string path)
        {
            try
            {
                if (!File.Exists(path))
                    throw new ApplicationException(path + " File does't exist");

                XmlSerializer xml = new XmlSerializer(typeof(List<String>));
                Stream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                assmemblyPath = (List<String>)xml.Deserialize(fs);
                fs.Close();

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Boolean CheckAssembly(Assembly asm)
        {
            try
            {
                Type tp = asm.GetType("nmDriver.Driver");

                if (tp == null)
                    return false;

                Type it = tp.GetInterface("IDriverModel");
                if (it == null)
                    return false;

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Boolean checkAssembly(String path)
        {
            try
            {
                Assembly asm = Assembly.LoadFile(path);
                return CheckAssembly(asm);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void addDrvMan(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                Assembly asm = Assembly.LoadFile(path);

                if (!CheckAssembly(asm))
                {
                    Debug.WriteLine("[GlobalConfiguration] Library isn't appropriate: " + path);
                    return;
                }
                else
                {
                    assmemblyPath.Add(path);
                    SaveData(Path.Combine(BasePath, "GlobalConfiguration.xml"));
                }
            }
            else
            {
                Debug.WriteLine("[GlobalConfiguration] Path to Driver is empty!");
            }
        }

        public void removeDrv(string path)
        {
            try
            {
                assmemblyPath.Remove(path);
                SaveData(Path.Combine(BasePath, "GlobalConfiguration.xml"));
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("[GlobalConfiguration] removeDrv: " + Ex.Message);
            }
        }

        public void autoSearchDrv()
        {
            DirectoryInfo dInfo = new DirectoryInfo(BasePath);
            FileInfo[] paths = dInfo.GetFiles("*.dll");

            assmemblyPath.Clear();

            foreach (FileInfo s in paths)
            {
                try
                {
                    if (checkAssembly(s.FullName))
                        addDrvMan(s.FullName);
                }
                catch (Exception)
                {
                }
            }
        }

        [Obsolete("Przestarzała funkcja. Trzeba użyć GetDrivers ")]
        public string[] getDrvLst()
        {
            List<string> buff = new List<string>();

            foreach (string s in assmemblyPath)
            {
                if (!String.IsNullOrEmpty(s))
                {
                    if (File.Exists(s))
                    {
                        Assembly asm = Assembly.LoadFile(s);

                        if (!CheckAssembly(asm))
                        {
                            Debug.WriteLine("[GlobalConfiguration] Assembly: " + asm.FullName + " Isn't appropriate!");
                            return null;
                        }
                        else
                        {
                            Type tp = asm.GetType("nmDriver.Driver");

                            IDriverModel idrv = (IDriverModel)asm.CreateInstance(tp.FullName);
                            buff.Add(idrv.driverName);
                        }
                    }
                }
            }

            return buff.ToArray();
        }

        public List<IDriverModel> GetDrivers()
        {
            List<IDriverModel> buff = new List<IDriverModel>();

            foreach (string s in assmemblyPath)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    if (File.Exists(s))
                    {
                        Assembly asm = Assembly.LoadFile(s);

                        if (!CheckAssembly(asm))
                        {
                            Debug.WriteLine("[GlobalConfiguration] Assembly: " + asm.FullName + " Isn't appropriate!");
                            return null;
                        }
                        else
                        {
                            Type tp = asm.GetType("nmDriver.Driver");

                            IDriverModel idrv = (IDriverModel)asm.CreateInstance(tp.FullName);
                            buff.Add(idrv);
                        }
                    }
                }
            }

            return buff;
        }

        public IDriverModel newDrv(string name)
        {
            try
            {
                IDriverModel idrv = null;

                string path = assmemblyPath.Find(x => x.Contains(name));

                if (File.Exists(path))
                {
                    Assembly asDriver = Assembly.LoadFile(path);
                    Type tpDriver = asDriver.GetType("nmDriver.Driver");
                    idrv = (IDriverModel)asDriver.CreateInstance(tpDriver.FullName);

                    return idrv;
                }
                else
                {
                    Debug.WriteLine("[GlobalConfiguration] Please Install Driver: " + name);
                    return null;
                }
            }
            catch (Exception)
            {
                Debug.WriteLine("[GlobalConfiguration] Problem with loading driver: " + name);
                return null;
            }
        }
    }
}