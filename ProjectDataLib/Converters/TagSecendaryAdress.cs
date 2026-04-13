using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;

namespace ProjectDataLib
{
    public class TagSecendaryAdress : Int32Converter
    {
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(GetOptions(context).ToArray());
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
            return true;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            List<int> opcje = GetOptions(context);

            if (value is string)
            {
                try
                {
                    int val = int.Parse((string)value);

                    if (val < 0 || val > opcje.Count - 1)
                        return 0;
                    else
                        return val;
                }
                catch (Exception)
                {
                    return 0;
                }
            }

            return 0;
        }

        private static List<int> GetOptions(ITypeDescriptorContext context)
        {
            var opcje = new List<int> { 0 };

            if (context?.Instance is not Tag tg)
                return opcje;

            IDriverModel idrv = tg.idrv;
            if (idrv?.MemoryAreaInf == null)
                return opcje;

            MemoryAreaInfo mInf = idrv.MemoryAreaInf.FirstOrDefault(x => x != null && x.Name == tg.areaData);
            if (mInf == null)
                return opcje;

            int tagSize = tg.getSize();
            if (tagSize <= 0)
                return opcje;

            if (mInf.AdresSize > tagSize)
            {
                opcje.Clear();
                for (int i = 0; i < mInf.AdresSize / tagSize; i++)
                    opcje.Add(i);
            }

            return opcje;
        }
    }
}