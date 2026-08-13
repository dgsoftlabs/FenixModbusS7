using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Fenix
{
    public class ValueMarkConv : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value)
                    return new SolidColorBrush(Colors.Green);
                else
                    return new SolidColorBrush(Colors.Red);
            }
            else
                return new SolidColorBrush(Colors.White);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}