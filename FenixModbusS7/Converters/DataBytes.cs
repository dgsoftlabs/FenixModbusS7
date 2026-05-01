using ProjectDataLib;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Fenix
{
    public class DataBytes : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            IDriverModel idrv = (IDriverModel)values[0];
            byte[] data = (byte[])values[1];
            string info = (string)values[2];
            EventType Type = (EventType)values[3];

            if (Type == EventType.OUT)
                return idrv.FormatFrameRequest(data, NumberStyles.HexNumber);
            else if (Type == EventType.IN)
                return idrv.FormatFrameResponse(data, NumberStyles.HexNumber);
            else
            {
                return "INTERNAL PROBLEM";
            }
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue };
        }
    }
}
