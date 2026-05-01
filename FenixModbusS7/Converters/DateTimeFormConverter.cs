using ProjectDataLib;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fenix
{
    public class DateTimeFormConverter : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime tm = (DateTime)values[0];
            Project pr = (Project)values[1];
            return tm.ToString(pr.longDT);
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue };
        }
    }
}
