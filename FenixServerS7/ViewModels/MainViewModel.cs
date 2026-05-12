using Microsoft.Win32;
using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FenixServer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ProjectContainer _projectContainer;
        private string _windowTitle = "Fenix Server";
        private string _currentProjectPath = string.Empty;
        private int _alarmCount;
        private string _lastAlarmMessage = "No alarms";
        private bool _isRunning;
        private PropertyRow _selectedProperty;
        private ConnectionRow _selectedConnection;
        private Project _currentProject;

        public MainViewModel(string[] startupArgs)
        {
            StartupArgs = startupArgs ?? Array.Empty<string>();

            Events = new ObservableCollection<AlarmEvent>();
            Properties = new ObservableCollection<PropertyRow>();
            Connections = new ObservableCollection<ConnectionRow>();

            _projectContainer = new ProjectContainer();
            _projectContainer.ApplicationError += OnApplicationError;
            _projectContainer.addProjectEv += (_, e) =>
            {
                if (e?.element is Project pr)
                {
                    _currentProject = pr;
                    RefreshConnections();
                }
            };

            OpenProjectCommand = new RelayCommand(() => _ = OpenProjectInteractiveAsync());
            OpenInBrowserCommand = new RelayCommand(OpenInBrowser, () => IsRunning && !string.IsNullOrWhiteSpace(GetServerBrowseAddress()));
            StartCommand = new RelayCommand(() => _ = StartAsync(), () => CurrentProject != null && !IsRunning);
            StopCommand = new RelayCommand(() => _ = StopAsync(), () => IsRunning);
            ClearEventsCommand = new RelayCommand(ClearEvents, () => Events.Count > 0);

            AddEvent(new AlarmEvent("WPF shell initialized"));
            SetWindowTitle(BuildBaseTitle());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string[] StartupArgs { get; }

        public ObservableCollection<AlarmEvent> Events { get; }

        public ObservableCollection<PropertyRow> Properties { get; }

        public ObservableCollection<ConnectionRow> Connections { get; }

        public ICommand OpenProjectCommand { get; }

        public ICommand OpenInBrowserCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand StopCommand { get; }

        public ICommand ClearEventsCommand { get; }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle == value)
                {
                    return;
                }

                _windowTitle = value;
                OnPropertyChanged();
            }
        }

        public string CurrentProjectPath
        {
            get => _currentProjectPath;
            private set
            {
                if (_currentProjectPath == value)
                {
                    return;
                }

                _currentProjectPath = value;
                OnPropertyChanged();
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (_isRunning == value)
                {
                    return;
                }

                _isRunning = value;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public Project CurrentProject
        {
            get => _currentProject;
            private set
            {
                if (_currentProject == value)
                {
                    return;
                }

                _currentProject = value;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public int AlarmCount
        {
            get => _alarmCount;
            private set
            {
                if (_alarmCount == value)
                {
                    return;
                }

                _alarmCount = value;
                OnPropertyChanged();
            }
        }

        public string LastAlarmMessage
        {
            get => _lastAlarmMessage;
            private set
            {
                if (_lastAlarmMessage == value)
                {
                    return;
                }

                _lastAlarmMessage = value;
                OnPropertyChanged();
            }
        }

        public PropertyRow SelectedProperty
        {
            get => _selectedProperty;
            set
            {
                if (_selectedProperty == value)
                {
                    return;
                }

                _selectedProperty = value;
                OnPropertyChanged();
            }
        }

        public ConnectionRow SelectedConnection
        {
            get => _selectedConnection;
            set
            {
                if (_selectedConnection == value)
                {
                    return;
                }

                _selectedConnection = value;
                OnPropertyChanged();
                SyncPropertiesFromSelection();
            }
        }

        public async Task InitializeAsync()
        {
            SetWindowTitle(BuildBaseTitle());

            if (StartupArgs.Length == 0)
            {
                return;
            }

            var mode = StartupArgs[0]?.ToLowerInvariant();
            if (mode != "-s" && mode != "-r")
            {
                return;
            }

            var path = Registry.GetValue(_projectContainer.RegUserRoot, _projectContainer.LastPathKey, "") as string;
            if (string.IsNullOrWhiteSpace(path))
            {
                AddEvent(new AlarmEvent("No recent project found in registry."));
                return;
            }

            if (!OpenProject(path))
            {
                return;
            }

            if (mode == "-s")
            {
                SetWindowTitle(BuildBaseTitle("[Simulation Mode]"));
            }
            else
            {
                SetWindowTitle(BuildBaseTitle("[Autorun]"));
            }

            await StartAsync();
        }

        public async Task ShutdownAsync()
        {
            try
            {
                await FenixServer.Web.WebHostExtensions.StopWebHostAsync();
            }
            catch
            {
            }

            if (CurrentProject != null)
            {
                foreach (var connection in CurrentProject.connectionList)
                {
                    try
                    {
                        ((IDriverModel)connection).deactivateCycle();
                    }
                    catch
                    {
                    }
                }

                try
                {
                    ((IDriverModel)CurrentProject.ScriptEng).deactivateCycle();
                }
                catch
                {
                }

                try
                {
                    ((IDriverModel)CurrentProject.InternalTagsDrv).deactivateCycle();
                }
                catch
                {
                }
            }

            IsRunning = false;
            RefreshConnections();
        }

        public void AddEvent(AlarmEvent alarmEvent)
        {
            if (alarmEvent == null)
            {
                return;
            }

            ExecuteOnUiThread(() =>
            {
                Events.Add(alarmEvent);
                try
                {
                    FenixServer.Web.EndpointMappings.PublishEvent(alarmEvent.Mess, new DateTimeOffset(alarmEvent.Tm));
                }
                catch
                {
                }

                RefreshEventSummary();
                RaiseCanExecuteChanged();
            });
        }

        private async Task OpenProjectInteractiveAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Fenix Projects (*.pse;*.psx)|*.pse;*.psx|All files (*.*)|*.*",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            OpenProject(dialog.FileName);
            await Task.CompletedTask;
        }

        private bool OpenProject(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    AddEvent(new AlarmEvent("Project file not found."));
                    return false;
                }

                if (_projectContainer.projectList.Count > 0)
                {
                    _projectContainer.closeAllProject(false);
                }

                if (!_projectContainer.openProjects(path))
                {
                    AddEvent(new AlarmEvent("Failed to open project."));
                    return false;
                }

                CurrentProject = _projectContainer.projectList.FirstOrDefault();
                CurrentProjectPath = path;
                Registry.SetValue(_projectContainer.RegUserRoot, _projectContainer.LastPathKey, path);

                if (CurrentProject != null)
                {
                    SetWindowTitle(BuildBaseTitle());
                    AddEvent(new AlarmEvent($"Project loaded: {CurrentProject.projectName}"));
                }

                RefreshConnections();
                return CurrentProject != null;
            }
            catch (Exception ex)
            {
                AddEvent(new AlarmEvent("Open project error: " + ex.Message));
                return false;
            }
        }

        private void OpenInBrowser()
        {
            var address = GetServerBrowseAddress();
            if (string.IsNullOrWhiteSpace(address))
            {
                AddEvent(new AlarmEvent("Server address is not configured."));
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = address,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddEvent(new AlarmEvent("Open browser error: " + ex.Message));
            }
        }

        private string GetServerBrowseAddress()
        {
            var rawPrefix = CurrentProject?.WebServer1?.Prefixes?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            if (string.IsNullOrWhiteSpace(rawPrefix))
            {
                return string.Empty;
            }

            var prefix = rawPrefix.Trim();
            if (prefix.Contains("+"))
            {
                prefix = prefix.Replace("+", "localhost");
            }

            if (prefix.Contains("*"))
            {
                prefix = prefix.Replace("*", "localhost");
            }

            return prefix;
        }

        private async Task StartAsync()
        {
            if (CurrentProject == null || IsRunning)
            {
                return;
            }

            try
            {
                var missingDrivers = CurrentProject.connectionList
                    .Where(c => c.Idrv == null)
                    .Select(c => $"{c.connectionName} ({c.DriverName})")
                    .ToList();

                if (missingDrivers.Count > 0)
                {
                    AddEvent(new AlarmEvent("Communication not started. Missing driver(s): " + string.Join(", ", missingDrivers)));
                    RefreshConnections();
                    return;
                }

                // 1) Start external connection drivers
                foreach (var connection in CurrentProject.connectionList)
                {
                    var driver = (IDriverModel)connection;
                    var driverId = connection.Idrv.ObjId;
                    var tags = _projectContainer.GetAllITagsForDriver(CurrentProject.objId, driverId) ?? new List<ITag>();
                    var started = driver.activateCycle(tags);
                    if (!started)
                    {
                        AddEvent(new AlarmEvent($"Driver did not start: {connection.connectionName} ({connection.DriverName})"));
                    }
                    else if (tags.Count == 0)
                    {
                        AddEvent(new AlarmEvent($"Driver started without tags: {connection.connectionName} ({connection.DriverName})"));
                    }
                }

                // 2) Start Scripts driver
                ((IDriverModel)CurrentProject.ScriptEng).activateCycle(new List<ITag>());

                // 3) Start InternalTags driver
                var allInternalTags = CurrentProject.InTagsList.Cast<ITag>().ToList();
                ((IDriverModel)CurrentProject.InternalTagsDrv).activateCycle(allInternalTags);

                // 4) Start HTTP host
                await FenixServer.Web.WebHostExtensions.InitializeAndStartWebHostAsync(CurrentProject, _projectContainer);

                IsRunning = true;
                AddEvent(new AlarmEvent("Communication started."));
                RefreshConnections();
            }
            catch (Exception ex)
            {
                AddEvent(new AlarmEvent("Start error: " + ex.Message));
            }
        }

        private async Task StopAsync()
        {
            if (CurrentProject == null || !IsRunning)
            {
                return;
            }

            try
            {
                // 1) Stop HTTP host
                await FenixServer.Web.WebHostExtensions.StopWebHostAsync();

                // 2) Stop external connection drivers
                foreach (var connection in CurrentProject.connectionList)
                {
                    ((IDriverModel)connection).deactivateCycle();
                }

                // 3) Stop Scripts + InternalTags drivers
                ((IDriverModel)CurrentProject.ScriptEng).deactivateCycle();
                ((IDriverModel)CurrentProject.InternalTagsDrv).deactivateCycle();

                IsRunning = false;
                AddEvent(new AlarmEvent("Communication stopped."));
                RefreshConnections();
            }
            catch (Exception ex)
            {
                AddEvent(new AlarmEvent("Stop error: " + ex.Message));
            }
        }

        private void RefreshConnections()
        {
            Connections.Clear();

            if (CurrentProject == null)
            {
                Properties.Clear();
                SelectedConnection = null;
                return;
            }

            Connections.Add(new ConnectionRow
            {
                SourceId = CurrentProject.WebServer1.ObjId,
                Kind = "Server",
                Name = "HttpServer",
                Protocol = "HTTP",
                Status = IsRunning ? "Running" : "Stopped",
                Sent = 0,
                Received = 0
            });

            Connections.Add(new ConnectionRow
            {
                SourceId = _projectContainer.ScriptGuid,
                Kind = "Driver",
                Name = "Scripts",
                Protocol = "Scripts",
                Status = ((IDriverModel)CurrentProject.ScriptEng).isAlive ? "Running" : "Stopped",
                Sent = 0,
                Received = 0
            });

            Connections.Add(new ConnectionRow
            {
                SourceId = _projectContainer.IntTagsGuid,
                Kind = "Driver",
                Name = "InternalTags",
                Protocol = "InternalTags",
                Status = ((IDriverModel)CurrentProject.InternalTagsDrv).isAlive ? "Running" : "Stopped",
                Sent = 0,
                Received = 0
            });

            foreach (var connection in CurrentProject.connectionList)
            {
                var isAlive = connection.Idrv?.isAlive ?? false;
                var status = connection.Idrv == null
                    ? "Driver Missing"
                    : (isAlive ? "Running" : "Stopped");

                Connections.Add(new ConnectionRow
                {
                    SourceId = connection.objId,
                    Kind = "Connection",
                    Name = connection.connectionName,
                    Protocol = connection.DriverName,
                    Status = status,
                    Sent = 0,
                    Received = 0
                });
            }

            SelectedConnection = Connections.FirstOrDefault();
        }

        private void SyncPropertiesFromSelection()
        {
            Properties.Clear();

            if (SelectedConnection == null)
            {
                return;
            }

            Properties.Add(new PropertyRow { Name = "Kind", Value = SelectedConnection.Kind });
            Properties.Add(new PropertyRow { Name = "Name", Value = SelectedConnection.Name });
            Properties.Add(new PropertyRow { Name = "Protocol", Value = SelectedConnection.Protocol });
            Properties.Add(new PropertyRow { Name = "Status", Value = SelectedConnection.Status });

            AddAddressProperties();
        }

        private void AddAddressProperties()
        {
            if (CurrentProject == null || SelectedConnection == null)
            {
                return;
            }

            if (string.Equals(SelectedConnection.Kind, "Server", StringComparison.OrdinalIgnoreCase))
            {
                var webServer = CurrentProject.WebServer1;
                var prefix = webServer?.Prefixes?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    return;
                }

                Properties.Add(new PropertyRow { Name = "Address", Value = prefix });
                Properties.Add(new PropertyRow { Name = "Authentication", Value = webServer.Auth.ToString() });
                Properties.Add(new PropertyRow { Name = "Users", Value = (webServer.Users?.Count ?? 0).ToString() });

                if (Uri.TryCreate(prefix, UriKind.Absolute, out var uri))
                {
                    Properties.Add(new PropertyRow { Name = "IP Address", Value = uri.Host });
                    Properties.Add(new PropertyRow { Name = "Port", Value = uri.Port.ToString() });
                }

                return;
            }

            var connection = CurrentProject.connectionList?.FirstOrDefault(c => c.objId == SelectedConnection.SourceId);
            if (connection == null)
            {
                return;
            }

            switch (connection.Parameters)
            {
                case TcpDriverParam tcp:
                    Properties.Add(new PropertyRow { Name = "IP Address", Value = tcp.Ip });
                    Properties.Add(new PropertyRow { Name = "Port", Value = tcp.Port.ToString() });
                    break;

                case S7DriverParam s7:
                    Properties.Add(new PropertyRow { Name = "IP Address", Value = s7.Ip });
                    Properties.Add(new PropertyRow { Name = "Port", Value = s7.Port.ToString() });
                    break;

                case IoDriverParam io:
                    Properties.Add(new PropertyRow { Name = "Port", Value = io.PortName });
                    break;
            }
        }

        private void OnApplicationError(object sender, EventArgs e)
        {
            if (e is ProjectEventArgs args)
            {
                if (args.element is Exception ex)
                {
                    AddEvent(new AlarmEvent(ex.Message));
                }
                else if (args.element != null)
                {
                    AddEvent(new AlarmEvent(args.element.ToString()));
                }
            }
        }

        private string BuildBaseTitle(string suffix = null)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return $"Fenix Server {version}".Trim();
            }

            return $"Fenix Server {version} {suffix}".Trim();
        }

        public void SetWindowTitle(string title)
        {
            WindowTitle = string.IsNullOrWhiteSpace(title) ? "Fenix Server" : title;
        }

        public void UpdateAlarmInfo(int count, string lastMessage)
        {
            AlarmCount = count;
            LastAlarmMessage = string.IsNullOrWhiteSpace(lastMessage) ? "No alarms" : lastMessage;
        }

        private void ClearEvents()
        {
            ExecuteOnUiThread(() =>
            {
                Events.Clear();
                RefreshEventSummary();
                RaiseCanExecuteChanged();
            });
        }

        private void ExecuteOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action);
        }

        private void RefreshEventSummary()
        {
            AlarmCount = Events.Count;
            LastAlarmMessage = Events.Count == 0 ? "No alarms" : Events[^1].Mess;
        }

        private void RaiseCanExecuteChanged()
        {
            if (ClearEventsCommand is RelayCommand clearRelay)
            {
                clearRelay.RaiseCanExecuteChanged();
            }

            if (OpenInBrowserCommand is RelayCommand browserRelay)
            {
                browserRelay.RaiseCanExecuteChanged();
            }

            if (StartCommand is RelayCommand startRelay)
            {
                startRelay.RaiseCanExecuteChanged();
            }

            if (StopCommand is RelayCommand stopRelay)
            {
                stopRelay.RaiseCanExecuteChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
