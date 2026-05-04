using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fenix
{
    internal class TemplateItems : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}