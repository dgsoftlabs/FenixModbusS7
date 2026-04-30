using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Fenix
{
    public partial class ClearableTextBox : UserControl
    {
        private bool _updatingFromDouble;
        private bool _updatingFromText;

        // String Text property
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ClearableTextBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTextPropertyChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        // Double DoubleValue property — NaN is represented as empty string
        public static readonly DependencyProperty DoubleValueProperty =
            DependencyProperty.Register(nameof(DoubleValue), typeof(double), typeof(ClearableTextBox),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnDoubleValuePropertyChanged));

        public double DoubleValue
        {
            get => (double)GetValue(DoubleValueProperty);
            set => SetValue(DoubleValueProperty, value);
        }

        public static readonly RoutedEvent TextChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(TextChanged), RoutingStrategy.Bubble,
                typeof(TextChangedEventHandler), typeof(ClearableTextBox));

        public event TextChangedEventHandler TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }

        public ClearableTextBox()
        {
            InitializeComponent();
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ClearableTextBox ctrl)
                ctrl.UpdateClearButtonVisibility();
        }

        private static void OnDoubleValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ClearableTextBox ctrl && !ctrl._updatingFromText)
            {
                ctrl._updatingFromDouble = true;
                var val = (double)e.NewValue;
                ctrl.InnerTextBox.Text = double.IsNaN(val) ? string.Empty : val.ToString(CultureInfo.InvariantCulture);
                ctrl._updatingFromDouble = false;
            }
        }

        private void InnerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = InnerTextBox.Text;
            UpdateClearButtonVisibility();

            if (!_updatingFromDouble)
            {
                _updatingFromText = true;
                var txt = InnerTextBox.Text;
                if (string.IsNullOrWhiteSpace(txt))
                    DoubleValue = double.NaN;
                else if (double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ||
                         double.TryParse(txt, out d))
                    DoubleValue = d;
                _updatingFromText = false;
            }

            RaiseEvent(new TextChangedEventArgs(TextChangedEvent, e.UndoAction));
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            InnerTextBox.Text = string.Empty;
        }

        private void UpdateClearButtonVisibility()
        {
            if (ClearButton != null)
                ClearButton.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
