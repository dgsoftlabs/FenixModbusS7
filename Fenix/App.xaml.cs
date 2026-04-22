using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Fenix
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
            base.OnStartup(e);
        }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            bool aIsNewInstance = false;
            myMutex = new Mutex(true, "FenixModbusS7", out aIsNewInstance);

            if (!aIsNewInstance)
            {
                MessageBox.Show("Already an instance is running...");
                App.Current.Shutdown();
                return;
            }

            var splash = new SplashScreenWindow();
            splash.Show();

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            await Task.Delay(2000);

            splash.Close();

            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
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