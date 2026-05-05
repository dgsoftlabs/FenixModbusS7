using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Fenix
{
    public class BitByteConv : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ITag itg = ((ITag)value);

            if (!itg.ActBitByte)
                return new List<int> { 0 };

            Tag tg = ((Tag)itg);

            MemoryAreaInfo mArea = (from x in tg.idrv.MemoryAreaInf where x.Name == tg.areaData select x).First();

            switch (tg.TypeData)
            {
                case TypeData.BIT:

                    if (mArea.AdresSize > 1)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.BYTE:
                    if (mArea.AdresSize > 8)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 8; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.SBYTE:
                    if (mArea.AdresSize > 8)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 8; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }
                    ;

                case TypeData.CHAR:
                    if (mArea.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 16; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.SHORT:
                    if (mArea.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 16; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.USHORT:
                    if (mArea.AdresSize > 16)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 16; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.INT:
                    if (mArea.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 32; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.UINT:
                    if (mArea.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 32; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.FLOAT:
                    if (mArea.AdresSize > 32)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 32; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }

                case TypeData.DOUBLE:
                    if (mArea.AdresSize > 64)
                    {
                        List<int> buff = new List<int>();
                        for (int i = 0; i < mArea.AdresSize / 64; i++)
                            buff.Add(i);

                        return buff;
                    }
                    else
                    {
                        return new List<int>() { 0 };
                    }
            }

            return new int[] { 0 };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}