using System;
using ProjectDataLib;

/// <summary>
/// Signal Generator
/// Generates test signals (Sine, Square, Sawtooth, Random) for testing purposes.
///
/// Configuration tags:
///   - "SigGen_Output"      (float) - Generated signal output
///   - "SigGen_Type"        (int)   - 0=Sine, 1=Square, 2=Sawtooth, 3=Random, 4=Ramp
///   - "SigGen_Amplitude"   (float) - Signal amplitude (default: 100)
///   - "SigGen_Frequency"   (float) - Frequency in Hz (default: 0.1)
///   - "SigGen_Offset"      (float) - DC offset (default: 0)
///   - "SigGen_Enable"      (bool)  - Enable output
///   - "SigGen_Pulse_Out"   (bool)  - 1Hz heartbeat pulse
///
/// Attach a Timer (e.g. 100ms) for smooth signal generation.
/// </summary>
public class SignalGenerator : ScriptModel
{
    private double time;
    private int pulseCounter;
    private Random rng = new Random();
    private DateTime lastTime;
    private bool firstRun = true;

    public override void Start()
    {
        Write("Signal Generator started");
        time = 0;
        pulseCounter = 0;
        firstRun = true;
    }

    public override void Stop()
    {
        Write("Signal Generator stopped");
    }

    public override void Cycle()
    {
        try
        {
            object enableObj = GetTag("SigGen_Enable");
            bool enabled = enableObj == null || Convert.ToBoolean(enableObj);
            if (!enabled)
            {
                SetTag("SigGen_Output", 0.0);
                return;
            }

            DateTime now = DateTime.Now;
            if (firstRun)
            {
                lastTime = now;
                firstRun = false;
                return;
            }

            double dt = (now - lastTime).TotalSeconds;
            lastTime = now;

            if (dt <= 0 || dt > 1) dt = 0.1; // sanity cap
            time += dt;

            // Read config
            int type = Convert.ToInt32(GetTag("SigGen_Type") ?? 0);
            double amp = Convert.ToDouble(GetTag("SigGen_Amplitude") ?? 100);
            double freq = Convert.ToDouble(GetTag("SigGen_Frequency") ?? 0.1);
            double offset = Convert.ToDouble(GetTag("SigGen_Offset") ?? 0);

            double output = 0;
            double cyclePos = (time * freq) % 1.0; // 0..1 within one cycle

            switch (type)
            {
                case 1: // Square
                    output = cyclePos < 0.5 ? amp : -amp;
                    break;

                case 2: // Sawtooth
                    output = (cyclePos * 2 * amp) - amp;
                    break;

                case 3: // Random noise
                    output = (rng.NextDouble() * 2 - 1) * amp;
                    break;

                case 4: // Ramp 0..100%
                    output = cyclePos * amp;
                    break;

                default: // Sine
                    output = Math.Sin(2 * Math.PI * time * freq) * amp;
                    break;
            }

            output += offset;
            SetTag("SigGen_Output", Math.Round(output, 2));

            // 1Hz pulse
            int newCount = (int)(time * 1.0) % 2;
            if (newCount != pulseCounter)
            {
                pulseCounter = newCount;
                SetTag("SigGen_Pulse_Out", pulseCounter == 1);
            }
        }
        catch (Exception ex)
        {
            Write("SigGen Error: " + ex.Message);
        }
    }
}
