using FenixServer.ViewModels;
using System;
using System.Threading;
using System.Windows;

namespace FenixServer
{
    public partial class App : Application
    {
        private Mutex? _singleInstanceMutex;
        private MainViewModel? _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                _singleInstanceMutex = Program.CreateSingleInstanceMutex(out bool instanceCountOne);

                if (!instanceCountOne)
                {
                    Program.ShowSingleInstanceMessage();
                    Shutdown();
                    return;
                }

                _mainViewModel = new MainViewModel(e.Args);
                var window = new MainWindow
                {
                    DataContext = _mainViewModel
                };

                window.Closing += (_, _) =>
                {
                    try
                    {
                        _mainViewModel?.ShutdownAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                    }
                };

                MainWindow = window;
                window.Show();

                _ = _mainViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Program.BuildStartupErrorMessage(ex),
                    "Fenix Server",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _mainViewModel?.ShutdownAsync().GetAwaiter().GetResult();
            }
            catch
            {
            }

            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();

            base.OnExit(e);
        }
    }
}
