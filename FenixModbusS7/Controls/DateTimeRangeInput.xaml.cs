using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Fenix
{
    public partial class DateTimeRangeInput : UserControl
    {
        private bool _isUpdatingControls;
        private readonly TimePickerPopup _fromTimePicker = new();
        private readonly TimePickerPopup _toTimePicker   = new();
        private readonly Popup _fromTimePopup;
        private readonly Popup _toTimePopup;

        public static readonly DependencyProperty FromValueProperty =
            DependencyProperty.Register(nameof(FromValue), typeof(DateTime?), typeof(DateTimeRangeInput),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFromValueChanged));

        public DateTime? FromValue
        {
            get => (DateTime?)GetValue(FromValueProperty);
            set => SetValue(FromValueProperty, value);
        }

        public static readonly DependencyProperty ToValueProperty =
            DependencyProperty.Register(nameof(ToValue), typeof(DateTime?), typeof(DateTimeRangeInput),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnToValueChanged));

        public DateTime? ToValue
        {
            get => (DateTime?)GetValue(ToValueProperty);
            set => SetValue(ToValueProperty, value);
        }

        public static readonly DependencyProperty FromToolTipProperty =
            DependencyProperty.Register(nameof(FromToolTip), typeof(string), typeof(DateTimeRangeInput),
                new PropertyMetadata("Start date and time"));

        public string FromToolTip
        {
            get => (string)GetValue(FromToolTipProperty);
            set => SetValue(FromToolTipProperty, value);
        }

        public static readonly DependencyProperty ToToolTipProperty =
            DependencyProperty.Register(nameof(ToToolTip), typeof(string), typeof(DateTimeRangeInput),
                new PropertyMetadata("End date and time"));

        public string ToToolTip
        {
            get => (string)GetValue(ToToolTipProperty);
            set => SetValue(ToToolTipProperty, value);
        }

        public DateTimeRangeInput()
        {
            InitializeComponent();

            _fromTimePopup = new Popup { StaysOpen = false, AllowsTransparency = true, Placement = PlacementMode.Bottom, Child = _fromTimePicker };
            _toTimePopup   = new Popup { StaysOpen = false, AllowsTransparency = true, Placement = PlacementMode.Bottom, Child = _toTimePicker };

            Loaded += (_, __) =>
            {
                _fromTimePopup.PlacementTarget = FromClockButton;
                _toTimePopup.PlacementTarget   = ToClockButton;
                SyncFromControls();
                SyncToControls();
                _fromTimePicker.TimeConfirmed += (s, t) => { _fromTimePopup.IsOpen = false; FromTimeTextBox.Text = t.ToString(@"hh\:mm\:ss"); };
                _toTimePicker.TimeConfirmed   += (s, t) => { _toTimePopup.IsOpen   = false; ToTimeTextBox.Text   = t.ToString(@"hh\:mm\:ss"); };
            };
        }

        private static void OnFromValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateTimeRangeInput control)
                control.SyncFromControls();
        }

        private static void OnToValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateTimeRangeInput control)
                control.SyncToControls();
        }

        private void SyncFromControls()
        {
            SyncControls(FromValue, FromDatePicker, FromTimeTextBox, FromTimeWatermark, FromClearButton);
        }

        private void SyncToControls()
        {
            SyncControls(ToValue, ToDatePicker, ToTimeTextBox, ToTimeWatermark, ToClearButton);
        }

        private void SyncControls(DateTime? value, DatePicker datePicker, TextBox timeTextBox, TextBlock watermark, Button clearButton)
        {
            if (_isUpdatingControls)
                return;

            _isUpdatingControls = true;
            try
            {
                datePicker.SelectedDate = value?.Date;
                timeTextBox.Text = value?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
                UpdateDecorations(value, timeTextBox.Text, watermark, clearButton);
            }
            finally
            {
                _isUpdatingControls = false;
            }
        }

        private void UpdateFromValueFromControls()
        {
            FromValue = GetValueFromControls(FromDatePicker, FromTimeTextBox, FromValue);
            UpdateDecorations(FromValue, FromTimeTextBox.Text, FromTimeWatermark, FromClearButton);
        }

        private void UpdateToValueFromControls()
        {
            ToValue = GetValueFromControls(ToDatePicker, ToTimeTextBox, ToValue);
            UpdateDecorations(ToValue, ToTimeTextBox.Text, ToTimeWatermark, ToClearButton);
        }

        private DateTime? GetValueFromControls(DatePicker datePicker, TextBox timeTextBox, DateTime? currentValue)
        {
            if (_isUpdatingControls)
                return currentValue;

            var selectedDate = datePicker.SelectedDate;
            var hasTime = TryParseTime(timeTextBox.Text, out var parsedTime);

            if (selectedDate is null && string.IsNullOrWhiteSpace(timeTextBox.Text))
                return null;

            if (selectedDate is not null)
                return selectedDate.Value.Date + (hasTime ? parsedTime : TimeSpan.Zero);

            return currentValue;
        }

        private static bool TryParseTime(string text, out TimeSpan time)
        {
            return TimeSpan.TryParseExact(text, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out time)
                   || TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time)
                   || TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out time);
        }

        private static void UpdateDecorations(DateTime? value, string text, TextBlock watermark, Button clearButton)
        {
            watermark.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
            clearButton.Visibility = value.HasValue || !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FromDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFromValueFromControls();
        }

        private void FromTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFromValueFromControls();
        }

        private void FromTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SyncFromControls();
        }

        private void FromClearButton_Click(object sender, RoutedEventArgs e)
        {
            FromValue = null;
        }

        private void FromClockButton_Click(object sender, RoutedEventArgs e)
        {
            var current = TryParseTime(FromTimeTextBox.Text, out var t) ? t : TimeSpan.Zero;
            _fromTimePicker.SetTime(current);
            _fromTimePopup.IsOpen = true;
        }

        private void ToClockButton_Click(object sender, RoutedEventArgs e)
        {
            var current = TryParseTime(ToTimeTextBox.Text, out var t) ? t : TimeSpan.Zero;
            _toTimePicker.SetTime(current);
            _toTimePopup.IsOpen = true;
        }

        private void ToDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateToValueFromControls();
        }

        private void ToTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateToValueFromControls();
        }

        private void ToTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SyncToControls();
        }

        private void ToClearButton_Click(object sender, RoutedEventArgs e)
        {
            ToValue = null;
        }
    }
}
