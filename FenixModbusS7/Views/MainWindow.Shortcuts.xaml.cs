using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Fenix
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_1 = 9000;
        private const int HOTKEY_ID_2 = 9001;
        private const int HOTKEY_ID_3 = 9002;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);

            // Ctrl + Shift + R
            RegisterHotKey(helper.Handle, HOTKEY_ID_1, 0x0002 | 0x0004, (uint)KeyInterop.VirtualKeyFromKey(Key.R));
            // Ctrl + Shift + S
            RegisterHotKey(helper.Handle, HOTKEY_ID_2, 0x0002 | 0x0004, (uint)KeyInterop.VirtualKeyFromKey(Key.S));
            // Ctrl + Shift + C
            RegisterHotKey(helper.Handle, HOTKEY_ID_3, 0x0002 | 0x0004, (uint)KeyInterop.VirtualKeyFromKey(Key.C));
        }

        protected override void OnClosed(EventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID_1);

            base.OnClosed(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                switch (wParam.ToInt32())
                {
                    case HOTKEY_ID_1:
                        // Ctrl+Shift+R
                        AllDriverCommunicationStop_Click(this, new RoutedEventArgs());

                        Thread.Sleep(1000);

                        ProjectSave_Click(this, new RoutedEventArgs());
                        AllDriverCommunicationStart_Click(this, new RoutedEventArgs());
                        break;

                    case HOTKEY_ID_2:
                        // Ctrl+Shift+S
                        ProjectSave_Click(this, new RoutedEventArgs());
                        AllDriverCommunicationStart_Click(this, new RoutedEventArgs());
                        break;

                    case HOTKEY_ID_3:
                        // Ctrl+Shift+C
                        AllDriverCommunicationStop_Click(this, new RoutedEventArgs());
                        break;
                }
            }

            return IntPtr.Zero;
        }

    }
}
