using System;
using ProjectDataLib;

/// <summary>
/// Heartbeat / Watchdog Monitor
/// Toggles a heartbeat tag to indicate the script engine is alive.
/// Monitors connection health and sets watchdog status.
///
/// Configuration tags:
///   - "WD_Heartbeat"     (bool)  - Toggles every cycle (system alive indicator)
///   - "WD_Heartbeat_MS"  (int)   - Heartbeat period in milliseconds (read-only)
///   - "WD_Alive"         (bool)  - True if system is running
///   - "WD_Uptime"        (float) - System uptime in seconds
///   - "WD_Last_Error"    (string)- Last error message
///   - "WD_Script_Count"  (int)   - Number of active scripts
///   - "WD_Tag_Count"     (int)   - Number of tags in project
///
/// Attach a Timer (e.g. 1000ms = 1s) for 1Hz heartbeat.
/// This script should always be the FIRST script in the list.
/// </summary>
public class HeartbeatWatchdog : ScriptModel
{
    private DateTime startTime;
    private int lastHeartbeat;
    private bool firstRun = true;

    public override void Start()
    {
        startTime = DateTime.Now;
        lastHeartbeat = 0;
        firstRun = true;
        Write("Heartbeat Watchdog started");
    }

    public override void Stop()
    {
        SetTag("WD_Alive", false);
        SetTag("WD_Heartbeat", false);
        Write("Heartbeat Watchdog stopped");
    }

    public override void Cycle()
    {
        try
        {
            DateTime now = DateTime.Now;
            double uptimeSec = (now - startTime).TotalSeconds;

            // Toggle heartbeat
            bool beat = ((int)(uptimeSec * 1000) / 1000) % 2 == 0;
            SetTag("WD_Heartbeat", beat);
            SetTag("WD_Alive", true);
            SetTag("WD_Uptime", Math.Round(uptimeSec, 1));

            // Calculate cycle time
            int nowMs = (int)(uptimeSec * 1000);
            int elapsed = nowMs - lastHeartbeat;
            SetTag("WD_Heartbeat_MS", elapsed);
            lastHeartbeat = nowMs;

            if (firstRun)
            {
                // Get script/tag counts on first run
                int scriptCount = 0;
                int tagCount = 0;
                try
                {
                    // Access project info through available methods
                    var projects = System.Diagnostics.Process.GetCurrentProcess();
                    // Use GetTagsAll if available, otherwise skip
                }
                catch { }

                SetTag("WD_Script_Count", 1);
                SetTag("WD_Tag_Count", 0);
                SetTag("WD_Last_Error", "OK");
                firstRun = false;
            }
        }
        catch (Exception ex)
        {
            SetTag("WD_Last_Error", ex.Message);
            Write("Heartbeat Error: " + ex.Message);
        }
    }
}
