using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using io = System.IO;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for TreeViewManager.xaml
    /// </summary>
    public partial class TreeViewManager : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeViewManager"/> class.
        /// </summary>
        public TreeViewManager()
        {
            InitializeComponent();
            DataContext = this;
        }
    }


    internal class ImageConverter : IValueConverter
    {
        private static readonly Dictionary<Type, string> ImageMappings = new Dictionary<Type, string>()
        {
            { typeof(Project),            "📁" },
            { typeof(CusFile),            "📂" },
            { typeof(DatabaseModel),      "🗄️" },
            { typeof(ScriptsDriver),      "📋" },
            { typeof(ScriptFile),         "📄" },
            { typeof(TimersFolder),       "📁" },
            { typeof(InternalTagsDriver), "🏷️" },
            { typeof(InTag),              "🔖" },
            { typeof(CustomTimer),        "⏱️" },
            { typeof(Connection),         "🔌" },
            { typeof(Device),             "💻" },
            { typeof(Tag),                "🏷️" }
        };

        private static readonly Dictionary<string, string> ExtensionMappings = new Dictionary<string, string>()
        {
            { ".html", "🌐" },
            { ".js",   "📜" },
            { ".ico",  "🖼️" },
            { ".jpg",  "🖼️" }
        };

        /// <summary>
        /// Converts the specified value to an emoji string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted image.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CusFile file)
            {
                if (file.IsFile && ExtensionMappings.TryGetValue(io.Path.GetExtension(file.FullName), out string ext))
                    return ext;
                else
                    return "📂";
            }

            if (ImageMappings.TryGetValue(value.GetType(), out string emoji))
                return emoji;

            return "📄";
        }

        /// <summary>
        /// Converts the specified value back to the original value.
        /// </summary>
        /// <param name="value">The image value to convert back.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The original value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    internal class StateRunConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts the specified values to a string representing the state.
        /// </summary>
        /// <param name="values">The values to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted state string.</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)values[1])
                return string.Empty;
            else
            {
                if ((bool)values[0])
                    return "↻";
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts the specified value back to the original values.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetTypes">The types of the target properties.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The original values.</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    internal class StateBlockConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified value to a string representing the state.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted state string.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return "[|]";
            else
                return string.Empty;
        }

        /// <summary>
        /// Converts the specified value back to the original value.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The original value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    internal class BoolNegConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified value to its negation.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The negated value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }

        /// <summary>
        /// Converts the specified value back to its negation.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The negated value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }
    }

    internal class TemplateItems : IValueConverter
    {
        /// <summary>
        /// Converts the specified value to itself.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        /// <summary>
        /// Converts the specified value back to the original value.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The original value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    internal class Clr : IValueConverter
    {
        /// <summary>
        /// Converts the specified value to a <see cref="SolidColorBrush"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The converted <see cref="SolidColorBrush"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Drawing.Color cl = (System.Drawing.Color)value;
            return new SolidColorBrush(Color.FromArgb(cl.A, cl.R, cl.G, cl.B));
        }

        /// <summary>
        /// Converts the specified value back to the original value.
        /// </summary>
        /// <param name="value">The value to convert back.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>The original value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}