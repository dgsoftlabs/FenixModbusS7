using System;
using ProjectDataLib;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Base template for Fenix scripts.
/// Extends ScriptModel to access GetTag(), SetTag(), Write().
/// Attach a Timer in the ScriptsDriver configuration to run Cycle() periodically.
/// </summary>
public class Script : ScriptModel
{
    public override void Start()
    {
        Write("Script started: " + this.ToString());
    }

    public override void Stop()
    {
        Write("Script stopped: " + this.ToString());
    }

    public override void Cycle()
    {
        // Your cyclic logic here
    }
}
