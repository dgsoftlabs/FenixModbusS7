using System;
using System.Xml.Serialization;

namespace ProjectDataLib
{
    [Serializable]
    public class ScalingConfig
    {
        public double AppMin { get; set; } = 0;
        public double AppMax { get; set; } = 0;
        public double PlcMin { get; set; } = 0;
        public double PlcMax { get; set; } = 0;

        [XmlIgnore]
        public bool IsEnabled => !(AppMin == 0 && AppMax == 0 && PlcMin == 0 && PlcMax == 0)
                                 && AppMax != AppMin
                                 && PlcMax != PlcMin;

        /// <summary>
        /// Converts a raw PLC value to the App representation.
        /// appValue = (plcValue - PlcMin) / (PlcMax - PlcMin) * (AppMax - AppMin) + AppMin
        /// </summary>
        public double ToApp(double plcValue)
        {
            if (!IsEnabled) return plcValue;
            return (plcValue - PlcMin) / (PlcMax - PlcMin) * (AppMax - AppMin) + AppMin;
        }

        /// <summary>
        /// Converts an App value back to PLC representation.
        /// plcValue = (appValue - AppMin) / (AppMax - AppMin) * (PlcMax - PlcMin) + PlcMin
        /// </summary>
        public double ToPlc(double appValue)
        {
            if (!IsEnabled) return appValue;
            return (appValue - AppMin) / (AppMax - AppMin) * (PlcMax - PlcMin) + PlcMin;
        }

        public override string ToString()
        {
            if (!IsEnabled) return "None";
            return $"App [{AppMin} – {AppMax}]  ↔  PLC [{PlcMin} – {PlcMax}]";
        }
    }
}