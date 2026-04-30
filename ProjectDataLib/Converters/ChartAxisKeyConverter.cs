using System;
using System.ComponentModel;

namespace ProjectDataLib
{
    /// <summary>
    /// TypeConverter that provides a dropdown list of available Y axis keys
    /// from the project's ChartConf for use in PropertyGrid.
    /// </summary>
    public class ChartAxisKeyConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (context?.Instance is Tag tag && tag.Proj?.ChartConf?.Axes != null)
            {
                var keys = new System.Collections.Generic.List<string>();
                foreach (var axis in tag.Proj.ChartConf.Axes)
                    keys.Add(axis.Key);
                return new StandardValuesCollection(keys);
            }

            if (context?.Instance is InTag inTag && inTag.Proj?.ChartConf?.Axes != null)
            {
                var keys = new System.Collections.Generic.List<string>();
                foreach (var axis in inTag.Proj.ChartConf.Axes)
                    keys.Add(axis.Key);
                return new StandardValuesCollection(keys);
            }

            return new StandardValuesCollection(new[] { "Y1" });
        }
    }
}
