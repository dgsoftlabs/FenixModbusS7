using ProjectDataLib;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Fenix
{
    public class ColorConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EventElement ev = (EventElement)value;

            switch (ev.Type)
            {
                case EventType.ERROR:
                    return new SolidColorBrush(Colors.Red);

                case EventType.INFO:
                    return new SolidColorBrush(Colors.Yellow);

                case EventType.IN:
                    return new SolidColorBrush(Colors.LightCyan);

                case EventType.OUT:
                    return new SolidColorBrush(Colors.White);

                default:
                    return new SolidColorBrush(Colors.White);
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}