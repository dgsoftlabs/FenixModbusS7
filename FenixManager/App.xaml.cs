using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;

namespace FenixWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex myMutex;

        public App()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            bool aIsNewInstance = false;
            myMutex = new Mutex(true, "FenixManager", out aIsNewInstance);

            if (!aIsNewInstance)
            {
                MessageBox.Show("Already an instance is running...");
                App.Current.Shutdown();
            }

            base.OnStartup(e);
        }

        private void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;

            if (!Directory.Exists(Environment.CurrentDirectory + "\\Logs"))
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\Logs");

            File.WriteAllText(Environment.CurrentDirectory + "\\Logs\\" + DateTime.Now.ToString("MM_dd_yy_H_mm_ss") + ".txt", e.StackTrace);

            if (e.Source == "Xceed.Wpf.AvalonDock" || e.Source == "AvalonDock")
            {
                string strp = (string)Registry.GetValue("HKEY_CURRENT_USER\\Software\\Fenix", "LastPath", "");
                var layoutPath = Path.GetDirectoryName(strp) + "\\Layout_.xml";
                if(File.Exists(layoutPath))
                {
                    File.Delete(layoutPath);
                }
            }
        }
    }
}