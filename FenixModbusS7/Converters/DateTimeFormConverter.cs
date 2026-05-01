using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ProjectDataLib;

namespace Fenix
{
    public class DateTimeFormConverter : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return DependencyProperty.UnsetValue;

            if (values[0] == DependencyProperty.UnsetValue || values[0] is not DateTime tm)
                return DependencyProperty.UnsetValue;

            if (values[1] == DependencyProperty.UnsetValue || values[1] is not Project pr)
                return DependencyProperty.UnsetValue;

            return tm.ToString(pr.longDT);
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue };
        }
    }
}
