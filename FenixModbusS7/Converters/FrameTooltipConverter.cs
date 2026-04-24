using ProjectDataLib;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Fenix
{
    public class FrameTooltipConverter : IMultiValueConverter
    {
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            IDriverModel idrv = values[0] as IDriverModel;
            byte[] data = values[1] as byte[];
            string info = values[2] as string;
            EventType type = values[3] is EventType eventType ? eventType : EventType.INFO;
            DateTime time = values[4] is DateTime dt ? dt : DateTime.MinValue;

            string direction = type.ToString();
            string frameText = string.Empty;

            if (idrv != null && data != null)
            {
                if (type == EventType.OUT)
                    frameText = idrv.FormatFrameRequest(data, NumberStyles.HexNumber);
                else if (type == EventType.IN)
                    frameText = idrv.FormatFrameResponse(data, NumberStyles.HexNumber);
            }

            if (!string.IsNullOrWhiteSpace(frameText))
                frameText = frameText.Replace('.', ' ').Trim();

            if (string.IsNullOrWhiteSpace(frameText))
                frameText = !string.IsNullOrWhiteSpace(info) ? info : "No frame data";

            var sb = new StringBuilder();
            sb.AppendLine($"Direction: {direction}");
            sb.AppendLine($"Bytes: {(data?.Length ?? 0)}");
            if (time != DateTime.MinValue)
                sb.AppendLine($"Time: {time:HH:mm:ss.fff}");
            if (!string.IsNullOrWhiteSpace(info))
                sb.AppendLine($"Info: {info}");
            sb.AppendLine();
            sb.AppendLine("Frame:");
            sb.Append(frameText);

            return sb.ToString();
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { DependencyProperty.UnsetValue };
        }
    }
}
