using System;
using System.Threading;
using System.Windows;

namespace FenixServer
{
    internal static class Program
    {
        public static Mutex CreateSingleInstanceMutex(out bool instanceCountOne)
        {
            return new Mutex(true, "FenixServer", out instanceCountOne);
        }

        public static string BuildStartupErrorMessage(Exception ex)
        {
            return "Fenix Server failed to start:\n" + ex;
        }

        public static void ShowSingleInstanceMessage()
        {
            MessageBox.Show("An FenixServer instance is already running", "Fenix Server", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
