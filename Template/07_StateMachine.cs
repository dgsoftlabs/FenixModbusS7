using System;
using ProjectDataLib;

/// <summary>
/// Simple Process State Machine
/// Implements a 4-state process: IDLE -> START -> RUN -> STOP -> IDLE
/// Useful for batch processes, machine control, or sequence automation.
///
/// Configuration tags:
///   - "SM_Command"       (int)   - 0=None, 1=Start, 2=Stop, 3=Reset
///   - "SM_State"         (int)   - Current state: 0=Idle, 1=Starting, 2=Running, 3=Stopping
///   - "SM_State_Name"    (string)- Current state name (read-only, for HMI display)
///   - "SM_Run_Time"      (float) - Elapsed time in RUN state (seconds)
///   - "SM_Cycle_Count"   (int)   - Number of completed cycles
///   - "SM_Status"        (string)- Status message
///
/// Example tags to control (create these in your project):
///   - "Motor_Run"        (bool)  - Motor run command
///   - "Valve_Open"       (bool)  - Valve position
///   - "Process_Active"   (bool)  - Process active indicator
///
/// Attach a Timer (e.g. 200ms) for responsive state transitions.
/// </summary>
public class StateMachine : ScriptModel
{
    private enum State { Idle = 0, Starting = 1, Running = 2, Stopping = 3 }
    private State currentState = State.Idle;
    private DateTime stateStartTime;
    private int cycleCount;
    private bool firstRun = true;

    private static readonly string[] StateNames = { "Idle", "Starting", "Running", "Stopping" };

    public override void Start()
    {
        Write("State Machine started");

        // Restore state from tag
        object savedState = GetTag("SM_State");
        if (savedState != null)
            currentState = (State)Convert.ToInt32(savedState);

        object savedCycle = GetTag("SM_Cycle_Count");
        cycleCount = savedCycle != null ? Convert.ToInt32(savedCycle) : 0;

        stateStartTime = DateTime.Now;
        firstRun = true;
    }

    public override void Stop()
    {
        Write("State Machine stopped");
        // Cleanup: turn everything off
        SetTag("Motor_Run", false);
        SetTag("Valve_Open", false);
        SetTag("Process_Active", false);
    }

    public override void Cycle()
    {
        try
        {
            int command = Convert.ToInt32(GetTag("SM_Command") ?? 0);
            double runTimeSec = (DateTime.Now - stateStartTime).TotalSeconds;

            switch (currentState)
            {
                case State.Idle:
                    SetTag("Motor_Run", false);
                    SetTag("Valve_Open", false);
                    SetTag("Process_Active", false);
                    SetTag("SM_Status", "Idle - waiting for Start command");

                    if (command == 1) // Start
                    {
                        currentState = State.Starting;
                        stateStartTime = DateTime.Now;
                        Write("StateMachine: Starting sequence initiated");
                    }
                    break;

                case State.Starting:
                    SetTag("Motor_Run", true);  // Start motor first
                    SetTag("Process_Active", true);
                    SetTag("SM_Status", "Starting...");

                    if (runTimeSec >= 2.0) // 2 second startup delay
                    {
                        SetTag("Valve_Open", true); // Open valve after motor stabilizes
                        currentState = State.Running;
                        stateStartTime = DateTime.Now;
                        Write("StateMachine: Now running");
                    }
                    break;

                case State.Running:
                    SetTag("SM_Status", $"Running ({runTimeSec:F1}s)");
                    SetTag("SM_Run_Time", Math.Round(runTimeSec, 1));

                    if (command == 2) // Stop
                    {
                        currentState = State.Stopping;
                        stateStartTime = DateTime.Now;
                        Write("StateMachine: Stopping sequence initiated");
                    }
                    break;

                case State.Stopping:
                    SetTag("Valve_Open", false);   // Close valve first
                    SetTag("SM_Status", "Stopping...");

                    if (runTimeSec >= 1.5) // 1.5 second stop delay
                    {
                        SetTag("Motor_Run", false);  // Then stop motor
                        SetTag("Process_Active", false);

                        if (command == 3 || command == 0) // Reset or auto-transition
                        {
                            cycleCount++;
                            SetTag("SM_Cycle_Count", cycleCount);
                            currentState = State.Idle;
                            stateStartTime = DateTime.Now;
                            Write($"StateMachine: Cycle {cycleCount} completed");
                        }
                    }
                    break;
            }

            // Update state tags
            SetTag("SM_State", (int)currentState);
            SetTag("SM_State_Name", StateNames[(int)currentState]);

            // Auto-reset command
            SetTag("SM_Command", 0);
        }
        catch (Exception ex)
        {
            Write("StateMachine Error: " + ex.Message);
        }
    }
}