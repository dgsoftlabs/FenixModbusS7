using ProjectDataLib;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Fenix
{
    public class ValueConv : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (((ITag)values[1]).GetFormatedValue());
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { value };
        }
    }
}