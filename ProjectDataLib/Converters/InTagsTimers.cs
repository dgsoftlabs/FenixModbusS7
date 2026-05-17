using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace ProjectDataLib
{
    public class InTagsTimers : StringConverter
    {
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            InTag tag = ((InTag)context.Instance);

            List<String> buff = new List<string>() { String.Empty };
            buff.AddRange(tag.Proj.InternalTagsDrv.Timers.Select(x => x.Name));

            return new StandardValuesCollection(buff);
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
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