using System;
using System.Collections.Generic;
using System.Linq;
using ProjectDataLib;

/// <summary>
/// Moving Average Filter
/// Smooths noisy analog signals by averaging the last N samples.
///
/// Configuration tags:
///   - "Filter_Raw_Input"     (float) - Raw (noisy) input signal
///   - "Filter_Smooth_Output" (float) - Filtered output signal
///   - "Filter_Window_Size"   (int)   - Number of samples to average (default: 10)
///   - "Filter_Enable"        (bool)  - Enable/disable filtering
///
/// Attach a fast Timer (e.g. 50-100ms) for best results.
/// </summary>
public class MovingAverageFilter : ScriptModel
{
    private Queue<double> buffer = new Queue<double>();
    private int windowSize = 10;
    private bool firstRun = true;

    public override void Start()
    {
        Write("Moving Average Filter started");
        buffer.Clear();
    }

    public override void Stop()
    {
        Write("Moving Average Filter stopped");
    }

    public override void Cycle()
    {
        try
        {
            object enableObj = GetTag("Filter_Enable");
            bool enabled = enableObj != null && Convert.ToBoolean(enableObj);

            if (!enabled)
            {
                // Passthrough mode
                double raw = Convert.ToDouble(GetTag("Filter_Raw_Input") ?? 0);
                SetTag("Filter_Smooth_Output", Math.Round(raw, 3));
                return;
            }

            // Read window size (allows runtime adjustment)
            object wsObj = GetTag("Filter_Window_Size");
            if (wsObj != null)
            {
                int newSize = Convert.ToInt32(wsObj);
                if (newSize < 1) newSize = 1;
                if (newSize > 1000) newSize = 1000;

                // Resize buffer if window changed
                while (buffer.Count > newSize)
                    buffer.Dequeue();

                windowSize = newSize;
            }

            // Read raw input
            double input = Convert.ToDouble(GetTag("Filter_Raw_Input") ?? 0);

            // Add to buffer
            buffer.Enqueue(input);
            if (buffer.Count > windowSize)
                buffer.Dequeue();

            // Calculate average
            double average = buffer.Average();

            // Write filtered output
            SetTag("Filter_Smooth_Output", Math.Round(average, 3));
        }
        catch (Exception ex)
        {
            Write("Filter Error: " + ex.Message);
        }
    }
}
