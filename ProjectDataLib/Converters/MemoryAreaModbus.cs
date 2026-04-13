using System;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace ProjectDataLib
{
    public class MemoryAreaModbus : StringConverter
    {
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (context?.Instance is not Tag tag)
                return new StandardValuesCollection(Array.Empty<string>());

            IDriverModel idrv = tag.idrv;
            if (idrv?.MemoryAreaInf == null)
                return new StandardValuesCollection(Array.Empty<string>());

            return new StandardValuesCollection(idrv.MemoryAreaInf
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => x.Name)
                .ToArray());
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return false;
        }
    }
}