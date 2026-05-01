using ProjectDataLib;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Fenix
{
    public class TagsConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            EventElement ev = (EventElement)value;

            string s = "";

            var tgs = from t in ev.Pr.tagsList where t.idrv.ObjId == ((IDriverModel)ev.Sender).ObjId select t;
            foreach (ITag tg in tgs)
                s = s + string.Format("{0}: {1}  ;", tg.Name, tg.GetFormatedValue());

            return s;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}