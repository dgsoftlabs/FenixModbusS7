using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Fenix
{
    public class AreaDataConv : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ITag tgx = (ITag)value;

            if (!tgx.ActAreaData)
                return new List<string>() { "" };

            return (from x in ((Tag)tgx).idrv.MemoryAreaInf select x.Name);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}