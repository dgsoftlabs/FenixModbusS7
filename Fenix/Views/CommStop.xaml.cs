using System.Windows;
using ProjectDataLib;
using System;

namespace Fenix
{
    public partial class CommStop : Window
    {
        public IDriverModel Idrv { get; set; }
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        public CommStop(IDriverModel idrv)
        {
            InitializeComponent();

            Idrv = idrv;
            Closing += CommStop_Closing;
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void CommStop_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            dispatcherTimer.Tick -= dispatcherTimer_Tick;
            Closing -= CommStop_Closing;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (!Idrv.isAlive)
            {
                dispatcherTimer.Stop();
                Close();
            }

            if (dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();
        }
    }
}