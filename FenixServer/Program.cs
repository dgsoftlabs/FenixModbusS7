using System;
using System.Threading;
using System.Windows.Forms;

namespace FenixServer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                bool instanceCountOne = false;
                using (Mutex mtex = new Mutex(true, "FenixServer", out instanceCountOne))
                {
                    if (instanceCountOne)
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new ManagerView(args));
                        mtex.ReleaseMutex();
                    }
                    else
                    {
                        MessageBox.Show("An FenixServer instance is already running");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Fenix Server failed to start:\n" + ex,
                    "Fenix Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}