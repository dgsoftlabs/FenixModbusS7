using System;
using ProjectDataLib;

/// <summary>
/// Deadband Filter
/// Only updates the output when the input changes beyond a configurable threshold.
/// Reduces network traffic and prevents unnecessary writes to the PLC.
///
/// Configuration tags:
///   - "DB_Input"         (float) - Raw input signal
///   - "DB_Output"        (float) - Filtered output (only updates on significant change)
///   - "DB_Threshold"     (float) - Deadband threshold (default: 1.0)
///   - "DB_Enable"        (bool)  - Enable deadband filtering
///   - "DB_Last_Change"   (float) - Time since last output change (seconds)
///   - "DB_Change_Count"  (int)   - Number of output changes
///
/// Attach a Timer (e.g. 100ms) for responsive filtering.
/// Best for: noisy position feedback, temperature readings, level sensors.
/// </summary>
public class DeadbandFilter : ScriptModel
{
    private double lastOutput;
    private DateTime lastChangeTime;
    private int changeCount;
    private bool firstRun = true;

    public override void Start()
    {
        Write("Deadband Filter started");

        // Restore state
        object outObj = GetTag("DB_Output");
        lastOutput = outObj != null ? Convert.ToDouble(outObj) : 0;
        lastChangeTime = DateTime.Now;
        changeCount = 0;
        firstRun = true;
    }

    public override void Stop()
    {
        Write("Deadband Filter stopped");
    }

    public override void Cycle()
    {
        try
        {
            object enableObj = GetTag("DB_Enable");
            bool enabled = enableObj == null || Convert.ToBoolean(enableObj);

            double input = Convert.ToDouble(GetTag("DB_Input") ?? 0);

            if (!enabled)
            {
                // Passthrough
                SetTag("DB_Output", Math.Round(input, 3));
                return;
            }

            double threshold = Convert.ToDouble(GetTag("DB_Threshold") ?? 1.0);
            if (threshold < 0) threshold = 0;

            if (firstRun)
            {
                lastOutput = input;
                SetTag("DB_Output", Math.Round(input, 3));
                firstRun = false;
                return;
            }

            // Check if change exceeds deadband
            double delta = Math.Abs(input - lastOutput);

            if (delta > threshold)
            {
                SetTag("DB_Output", Math.Round(input, 3));
                lastOutput = input;
                lastChangeTime = DateTime.Now;
                changeCount++;
                SetTag("DB_Change_Count", changeCount);
            }

            // Report time since last change
            double secondsSinceChange = (DateTime.Now - lastChangeTime).TotalSeconds;
            SetTag("DB_Last_Change", Math.Round(secondsSinceChange, 1));
        }
        catch (Exception ex)
        {
            Write("Deadband Error: " + ex.Message);
        }
    }
}
