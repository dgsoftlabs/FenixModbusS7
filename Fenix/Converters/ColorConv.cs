using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Fenix
{
    public class ColorConv : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Color cl)
                return new SolidColorBrush(Color.FromArgb(cl.A, cl.R, cl.G, cl.B));

            if (value is Color mediaColor)
                return new SolidColorBrush(mediaColor);

            return Brushes.Transparent;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
                return System.Drawing.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);

            if (value is Color cl)
                return System.Drawing.Color.FromArgb(cl.A, cl.R, cl.G, cl.B);

            return System.Drawing.Color.Transparent;
        }
    }
}