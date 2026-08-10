using System;
using ProjectDataLib;

/// <summary>
/// Rate of Change (Derivative) Monitor
/// Calculates how fast a process value is changing.
/// Useful for detecting trends, predicting faults, or triggering alarms on rapid changes.
///
/// Configuration tags:
///   - "ROC_Input"        (float) - Input signal to monitor
///   - "ROC_Output"       (float) - Rate of change (units/second)
///   - "ROC_HighLimit"    (float) - Alarm threshold for high RoC
///   - "ROC_Alarm"        (bool)  - Alarm output (true when RoC exceeds limit)
///   - "ROC_Enable"       (bool)  - Enable monitoring
///
/// Attach a Timer (e.g. 100-200ms) for responsive detection.
/// </summary>
public class RateOfChange : ScriptModel
{
    private double previousValue;
    private DateTime previousTime;
    private bool firstRun = true;

    public override void Start()
    {
        Write("Rate of Change Monitor started");
        firstRun = true;
    }

    public override void Stop()
    {
        Write("Rate of Change Monitor stopped");
    }

    public override void Cycle()
    {
        try
        {
            object enableObj = GetTag("ROC_Enable");
            bool enabled = enableObj == null || Convert.ToBoolean(enableObj);

            if (!enabled)
            {
                SetTag("ROC_Output", 0.0);
                SetTag("ROC_Alarm", false);
                return;
            }

            double currentValue = Convert.ToDouble(GetTag("ROC_Input") ?? 0);
            DateTime now = DateTime.Now;

            if (firstRun)
            {
                previousValue = currentValue;
                previousTime = now;
                firstRun = false;
                SetTag("ROC_Output", 0.0);
                return;
            }

            double dt = (now - previousTime).TotalSeconds;
            if (dt <= 0) return;

            // Calculate rate of change
            double delta = currentValue - previousValue;
            double roc = delta / dt;

            // Exponential smoothing for noise reduction
            // Uses a simple IIR filter: output = 0.7*new + 0.3*old
            double previousRoc = Convert.ToDouble(GetTag("ROC_Output") ?? 0);
            double smoothedRoc = (roc * 0.7) + (previousRoc * 0.3);

            SetTag("ROC_Output", Math.Round(smoothedRoc, 4));

            // Check alarm limit
            object limitObj = GetTag("ROC_HighLimit");
            if (limitObj != null)
            {
                double limit = Convert.ToDouble(limitObj);
                bool alarm = Math.Abs(smoothedRoc) > limit;
                SetTag("ROC_Alarm", alarm);

                if (alarm && Math.Abs(smoothedRoc) > limit)
                    Write($"ROC Alarm: Rate={smoothedRoc:F4}/s exceeds limit={limit}");
            }

            previousValue = currentValue;
            previousTime = now;
        }
        catch (Exception ex)
        {
            Write("ROC Error: " + ex.Message);
        }
    }
}
