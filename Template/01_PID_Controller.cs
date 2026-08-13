using System;
using ProjectDataLib;

/// <summary>
/// PID Controller
/// Reads a Process Value (PV) tag, compares with SetPoint (SP),
/// and writes the calculated output to a Control Output tag.
///
/// Configuration (create these tags in your project):
///   - "PID_PV"      (float)  - Process Value (from PLC)
///   - "PID_SP"      (float)  - SetPoint (desired value)
///   - "PID_Out"     (float)  - Control Output (to PLC/actuator)
///   - "PID_Kp"      (float)  - Proportional gain
///   - "PID_Ki"      (float)  - Integral gain
///   - "PID_Kd"      (float)  - Derivative gain
///   - "PID_Enable"  (bool)   - Master enable
///
/// Attach a Timer (e.g. 100ms) to run Cycle() periodically.
/// </summary>
public class PID_Controller : ScriptModel
{
    private double integral;
    private double prevError;
    private double prevPV;
    private DateTime lastTime;
    private bool firstRun = true;

    public override void Start()
    {
        Write("PID Controller started");
        integral = 0;
        prevError = 0;
        prevPV = 0;
        firstRun = true;
    }

    public override void Stop()
    {
        Write("PID Controller stopped");
    }

    public override void Cycle()
    {
        try
        {
            // Check if PID is enabled
            object enableObj = GetTag("PID_Enable");
            if (enableObj == null || Convert.ToBoolean(enableObj) == false)
                return;

            // Read gains
            double Kp = Convert.ToDouble(GetTag("PID_Kp"));
            double Ki = Convert.ToDouble(GetTag("PID_Ki"));
            double Kd = Convert.ToDouble(GetTag("PID_Kd"));

            // Read process value and setpoint
            double pv = Convert.ToDouble(GetTag("PID_PV"));
            double sp = Convert.ToDouble(GetTag("PID_SP"));

            // Calculate time delta
            DateTime now = DateTime.Now;
            if (firstRun)
            {
                lastTime = now;
                prevError = sp - pv;
                prevPV = pv;
                firstRun = false;
                return;
            }

            double dt = (now - lastTime).TotalSeconds;
            if (dt <= 0) dt = 0.001; // prevent division by zero
            lastTime = now;

            // PID calculations
            double error = sp - pv;

            // Proportional
            double pTerm = Kp * error;

            // Integral (with anti-windup clamp)
            integral += error * dt;
            double iTerm = Ki * integral;
            // Clamp integral term to prevent windup
            iTerm = Math.Max(-100, Math.Min(100, iTerm));

            // Derivative (on PV to avoid derivative kick)
            double dPv = (pv - prevPV) / dt;
            double dTerm = -Kd * dPv;

            // Calculate output
            double output = pTerm + iTerm + dTerm;
            output = Math.Max(0, Math.Min(100, output)); // clamp 0-100%

            // Write output
            SetTag("PID_Out", Math.Round(output, 2));

            // Store for next cycle
            prevError = error;
            prevPV = pv;
        }
        catch (Exception ex)
        {
            Write("PID Error: " + ex.Message);
        }
    }
}