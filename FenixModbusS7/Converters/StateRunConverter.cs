using System;
using System.Globalization;
using System.Windows.Data;

namespace Fenix
{
    internal class StateRunConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)values[1])
                return string.Empty;
            else
            {
                if ((bool)values[0])
                    return "↻";
                else
                    return string.Empty;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}