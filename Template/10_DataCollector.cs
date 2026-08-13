using System;
using System.Collections.Generic;
using ProjectDataLib;

/// <summary>
/// Data Collector / Trend Recorder
/// Periodically samples multiple tags and logs them to the console output
/// in CSV format for external logging or copy-paste into Excel.
///
/// Configuration tags:
///   - "DC_Enable"        (bool)  - Enable data collection
///   - "DC_Interval"      (int)   - Logging interval in cycles (default: 10)
///   - "DC_Tag1_Name"     (string)- Name of first tag to collect (e.g. "Temp_Actual")
///   - "DC_Tag2_Name"     (string)- Name of second tag to collect (e.g. "Press_Actual")
///   - "DC_Tag3_Name"     (string)- Name of third tag to collect (e.g. "Flow_Rate")
///   - "DC_Log_Count"     (int)   - Number of samples collected
///
/// Data is written to the Output panel via Write().
/// Attach a Timer (e.g. 1000ms = 1s) to control sample rate.
/// </summary>
public class DataCollector : ScriptModel
{
    private int cycleCounter;
    private int logCount;
    private DateTime startTime;
    private bool headerWritten;

    private readonly List<string> tagNames = new List<string>();

    public override void Start()
    {
        Write("Data Collector started");
        cycleCounter = 0;
        logCount = 0;
        startTime = DateTime.Now;
        headerWritten = false;
        tagNames.Clear();
    }

    public override void Stop()
    {
        Write("Data Collector stopped. Total samples: " + logCount);

        // Write summary
        if (logCount > 0)
        {
            double duration = (DateTime.Now - startTime).TotalMinutes;
            Write($"--- Collection Summary: {logCount} samples in {duration:F1} minutes ---");
            Write($"--- Average rate: {logCount / Math.Max(duration, 0.01):F1} samples/min ---");
        }
    }

    public override void Cycle()
    {
        try
        {
            object enableObj = GetTag("DC_Enable");
            bool enabled = enableObj == null || Convert.ToBoolean(enableObj);
            if (!enabled) return;

            // Read interval
            int interval = Convert.ToInt32(GetTag("DC_Interval") ?? 10);
            if (interval < 1) interval = 1;

            cycleCounter++;

            if (cycleCounter % interval != 0)
                return;

            // Read tag names (only on first cycle or when changed)
            if (tagNames.Count == 0)
            {
                string t1 = Convert.ToString(GetTag("DC_Tag1_Name") ?? "");
                string t2 = Convert.ToString(GetTag("DC_Tag2_Name") ?? "");
                string t3 = Convert.ToString(GetTag("DC_Tag3_Name") ?? "");

                if (!string.IsNullOrEmpty(t1)) tagNames.Add(t1);
                if (!string.IsNullOrEmpty(t2)) tagNames.Add(t2);
                if (!string.IsNullOrEmpty(t3)) tagNames.Add(t3);
            }

            // Build CSV line
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string csvLine = timestamp;

            foreach (string tag in tagNames)
            {
                object val = GetTag(tag);
                csvLine += "," + (val != null ? Convert.ToString(val) : "N/A");
            }

            // Write header once
            if (!headerWritten && tagNames.Count > 0)
            {
                string header = "Timestamp," + string.Join(",", tagNames);
                Write("--- CSV Data Start ---");
                Write(header);
                headerWritten = true;
            }

            // Log the data
            Write(csvLine);
            logCount++;
            SetTag("DC_Log_Count", logCount);
        }
        catch (Exception ex)
        {
            Write("DataCollector Error: " + ex.Message);
        }
    }
}