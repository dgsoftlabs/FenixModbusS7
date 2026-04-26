using ProjectDataLib;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for CheckVersion.xaml
    /// </summary>
    public partial class CheckVersion : Window, INotifyPropertyChanged
    {
        private PropertyChangedEventHandler propChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { propChanged += value; }
            remove { propChanged -= value; }
        }

        private ProjectContainer PrCon;

        private Version serVer;
        public Version SerVer
        {
            get => serVer;
            set
            {
                serVer = value;
                OnPropertyChanged(nameof(SerVer));
            }
        }

        private Version insVer;
        public Version InsVer
        {
            get => insVer;
            set
            {
                insVer = value;
                OnPropertyChanged(nameof(InsVer));
            }
        }

        private string status;
        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private bool update;
        public bool Update
        {
            get => update;
            set
            {
                update = value;
                OnPropertyChanged(nameof(Update));
            }
        }

        private int progressValue;
        public int ProgressValue
        {
            get => progressValue;
            set
            {
                progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        private string Address { get; set; }

        public CheckVersion(ProjectContainer prcn)
        {
            InitializeComponent();
            PrCon = prcn;
            DataContext = this;
            Loaded += CheckVersion_OnLoaded;
            InsVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

        private async void CheckVersion_OnLoaded(object sender, RoutedEventArgs e)
        {
           await Task.Run(async () => await CheckForUpdates());
        }

        private async Task CheckForUpdates()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                UpdateStatus("No internet connection.", 0);
                return;
            }

            UpdateStatus("Starting...", 10);
            UpdateStatus("Downloading...", 50);
            var versionContent = await ProjectContainer.GetVersionFromGitHub();
            UpdateStatus("Checking Version...", 100);

            var newVersion = ProjectContainer.ParseVersionFromContent(versionContent);
            var url = ProjectContainer.ParseUrlFromContent(versionContent);

            SerVer = newVersion;
            Address = url;
            Update = newVersion > InsVer;

            UpdateStatus("Finished", 0);
        }

        private void UpdateStatus(string status, int progress)
        {
            Status = status;
            ProgressValue = progress;
        }

        private void Button_Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Address))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Address)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(ex));
            }
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnPropertyChanged(string propertyName)
        {
            propChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}