using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Fenix
{
    internal class ImageConverter : IValueConverter
    {
        private static readonly Dictionary<Type, string> ImageMappings = new Dictionary<Type, string>()
        {
            { typeof(Project),            "📁" },
            { typeof(DatabaseModel),      "🗄️" },
            { typeof(ChartConfigNode),    "📐" },
            { typeof(ChartAxisNode),      "📏" },
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

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ImageMappings.TryGetValue(value.GetType(), out string emoji))
                return emoji;

            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}