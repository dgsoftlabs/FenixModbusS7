using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fenix
{
    public partial class TimePickerPopup : UserControl
    {
        public event EventHandler<TimeSpan>? TimeConfirmed;

        private int _hour;
        private int _minute;
        private int _second;

        public TimePickerPopup()
        {
            InitializeComponent();
        }

        public void SetTime(TimeSpan time)
        {
            _hour   = time.Hours;
            _minute = time.Minutes;
            _second = time.Seconds;
            Refresh();
        }

        private void Refresh()
        {
            HourDisplay.Text = _hour.ToString("D2");
            MinDisplay.Text  = _minute.ToString("D2");
            SecDisplay.Text  = _second.ToString("D2");
        }

        // --- Hour ---
        private void HourUp_Click(object sender, RoutedEventArgs e)   { _hour = (_hour + 1) % 24;  Refresh(); }
        private void HourDown_Click(object sender, RoutedEventArgs e) { _hour = (_hour + 23) % 24; Refresh(); }
        private void Hour_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) HourUp_Click(sender, e);
            else             HourDown_Click(sender, e);
        }

        // --- Minute ---
        private void MinUp_Click(object sender, RoutedEventArgs e)   { _minute = (_minute + 1) % 60;  Refresh(); }
        private void MinDown_Click(object sender, RoutedEventArgs e) { _minute = (_minute + 59) % 60; Refresh(); }
        private void Min_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) MinUp_Click(sender, e);
            else             MinDown_Click(sender, e);
        }

        // --- Second ---
        private void SecUp_Click(object sender, RoutedEventArgs e)   { _second = (_second + 1) % 60;  Refresh(); }
        private void SecDown_Click(object sender, RoutedEventArgs e) { _second = (_second + 59) % 60; Refresh(); }
        private void Sec_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0) SecUp_Click(sender, e);
            else             SecDown_Click(sender, e);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            TimeConfirmed?.Invoke(this, new TimeSpan(_hour, _minute, _second));
        }
    }
}
