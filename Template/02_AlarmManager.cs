using System;
using System.Collections.Generic;
using ProjectDataLib;

/// <summary>
/// Alarm Manager
/// Monitors multiple tags and triggers alarms when values exceed thresholds.
/// Writes alarm status to output tags.
///
/// Configuration tags (create in your project):
///   - "Alarm_Temperature_High" (float) - High limit for temperature
///   - "Alarm_Temperature_Low"  (float) - Low limit for temperature
///   - "Alarm_Pressure_High"    (float) - High limit for pressure
///   - "Alarm_Pressure_Low"     (float) - Low limit for pressure
///   - "Temp_Actual"            (float) - Current temperature reading
///   - "Press_Actual"           (float) - Current pressure reading
///   - "Alarm_Reset"            (bool)  - Reset all acknowledged alarms
///
/// Output tags (written by script):
///   - "Alarm_Temp_High_Active"  (bool) - High temperature alarm active
///   - "Alarm_Temp_Low_Active"   (bool) - Low temperature alarm active
///   - "Alarm_Press_High_Active" (bool) - High pressure alarm active
///   - "Alarm_Press_Low_Active"  (bool) - Low pressure alarm active
///   - "Alarm_Count"            (int)   - Total active alarm count
///
/// Attach a Timer (e.g. 500ms) to run Cycle() periodically.
/// </summary>
public class AlarmManager : ScriptModel
{
    private struct Alarm
    {
        public string Name;
        public string InputTag;
        public string OutputTag;
        public double Limit;
        public bool IsHighAlarm; // false = low alarm
        public bool IsActive;
        public bool IsAcknowledged;
        public bool WasActive;
    }

    private List<Alarm> alarms = new List<Alarm>();
    private bool firstRun = true;

    public override void Start()
    {
        Write("Alarm Manager started");

        // Define alarms
        alarms = new List<Alarm>
        {
            new Alarm { Name = "Temp High",  InputTag = "Temp_Actual", OutputTag = "Alarm_Temp_High_Active",  Limit = 0, IsHighAlarm = true },
            new Alarm { Name = "Temp Low",   InputTag = "Temp_Actual", OutputTag = "Alarm_Temp_Low_Active",   Limit = 0, IsHighAlarm = false },
            new Alarm { Name = "Press High", InputTag = "Press_Actual", OutputTag = "Alarm_Press_High_Active", Limit = 0, IsHighAlarm = true },
            new Alarm { Name = "Press Low",  InputTag = "Press_Actual", OutputTag = "Alarm_Press_Low_Active",  Limit = 0, IsHighAlarm = false }
        };

        firstRun = true;
    }

    public override void Stop()
    {
        Write("Alarm Manager stopped");
    }

    public override void Cycle()
    {
        try
        {
            // Read alarm limits (allows runtime modification)
            double tempHigh = Convert.ToDouble(GetTag("Alarm_Temperature_High"));
            double tempLow = Convert.ToDouble(GetTag("Alarm_Temperature_Low"));
            double pressHigh = Convert.ToDouble(GetTag("Alarm_Pressure_High"));
            double pressLow = Convert.ToDouble(GetTag("Alarm_Pressure_Low"));

            // Update limits
            alarms[0] = UpdateLimit(alarms[0], tempHigh);
            alarms[1] = UpdateLimit(alarms[1], tempLow);
            alarms[2] = UpdateLimit(alarms[2], pressHigh);
            alarms[3] = UpdateLimit(alarms[3], pressLow);

            // Check reset
            bool reset = Convert.ToBoolean(GetTag("Alarm_Reset") ?? false);

            int activeCount = 0;

            foreach (var alarm in alarms)
            {
                double value = Convert.ToDouble(GetTag(alarm.InputTag) ?? 0);

                // Check alarm condition
                bool triggered = alarm.IsHighAlarm
                    ? value > alarm.Limit
                    : value < alarm.Limit;

                // Update alarm state
                var updated = alarm;
                updated.WasActive = alarm.IsActive;
                updated.IsActive = triggered;

                // Acknowledge on reset
                if (reset && alarm.IsActive)
                    updated.IsAcknowledged = true;
                else if (!triggered)
                    updated.IsAcknowledged = false;

                // Write active status
                SetTag(alarm.OutputTag, triggered);

                // Log on state change
                if (triggered && !alarm.WasActive)
                    Write($"ALARM: {alarm.Name} triggered! Value={value:F2}, Limit={alarm.Limit:F2}");
                else if (!triggered && alarm.WasActive)
                    Write($"OK: {alarm.Name} returned to normal. Value={value:F2}");

                if (triggered) activeCount++;
            }

            SetTag("Alarm_Count", activeCount);

            if (firstRun)
                firstRun = false;
        }
        catch (Exception ex)
        {
            Write("AlarmManager Error: " + ex.Message);
        }
    }

    private Alarm UpdateLimit(Alarm alarm, double newLimit)
    {
        alarm.Limit = newLimit;
        return alarm;
    }
}