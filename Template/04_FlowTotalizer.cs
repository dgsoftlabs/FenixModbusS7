using System;
using ProjectDataLib;

/// <summary>
/// Flow Totalizer
/// Accumulates flow rate over time to calculate total volume.
///
/// Configuration tags:
///   - "Flow_Rate"        (float) - Instantaneous flow rate (e.g. L/min)
///   - "Flow_Total"       (float) - Accumulated total volume (e.g. Liters)
///   - "Flow_Total_Reset" (bool)  - Set true to reset totalizer
///   - "Flow_Enable"      (bool)  - Enable/disable totalization
///
/// Attach a Timer (e.g. 1000ms = 1s) matching your flow rate update interval.
/// </summary>
public class FlowTotalizer : ScriptModel
{
    private double accumulated;
    private DateTime lastTime;
    private bool firstRun = true;

    public override void Start()
    {
        Write("Flow Totalizer started");
        // Restore previous total from tag
        object total = GetTag("Flow_Total");
        accumulated = total != null ? Convert.ToDouble(total) : 0;
        firstRun = true;
    }

    public override void Stop()
    {
        Write("Flow Totalizer stopped");
    }

    public override void Cycle()
    {
        try
        {
            // Check reset
            object resetObj = GetTag("Flow_Total_Reset");
            if (resetObj != null && Convert.ToBoolean(resetObj))
            {
                accumulated = 0;
                SetTag("Flow_Total", 0.0);
                SetTag("Flow_Total_Reset", false); // auto-reset the flag
                Write("Flow totalizer reset");
                return;
            }

            object enableObj = GetTag("Flow_Enable");
            bool enabled = enableObj == null || Convert.ToBoolean(enableObj);

            if (!enabled)
                return;

            // Read flow rate
            double flowRate = Convert.ToDouble(GetTag("Flow_Rate") ?? 0);
            if (flowRate < 0) flowRate = 0;

            DateTime now = DateTime.Now;
            if (firstRun)
            {
                lastTime = now;
                firstRun = false;
                return;
            }

            // Calculate time delta in minutes (flow rate is per minute)
            double dtMinutes = (now - lastTime).TotalMinutes;
            lastTime = now;

            if (dtMinutes <= 0) return;

            // Accumulate: Volume = FlowRate * Time
            accumulated += flowRate * dtMinutes;

            // Write total
            SetTag("Flow_Total", Math.Round(accumulated, 3));
        }
        catch (Exception ex)
        {
            Write("Totalizer Error: " + ex.Message);
        }
    }
}