using ProjectDataLib;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Fenix
{
    public class RowDataConv : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ITag)value).GrVisibleTab;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}