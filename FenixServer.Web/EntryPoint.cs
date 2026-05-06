using Microsoft.Win32;
using ProjectDataLib;
using System.IO;

namespace FenixServer.Web
{
    /// <summary>
    /// Standalone console entry point.
    /// Usage: FenixServer.Web.exe [project.pse] [--port 8080]
    /// </summary>
    internal static class EntryPoint
    {
        public static async Task Main(string[] args)
        {
            ParseArgs(args, out var projectPath, out var port);

            Project? project = null;
            var container = new ProjectContainer();
            container.ApplicationError += (_, e) =>
            {
                if (e is not ProjectEventArgs pe)
                    return;

                var error = pe.element as Exception
                    ?? pe.element2 as Exception;

                if (error != null)
                    Console.Error.WriteLine($"[APP][ERROR] {error.Message}");
            };

            if (string.IsNullOrWhiteSpace(projectPath))
                projectPath = GetLastProjectPathFromRegistry(container);

            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                if (!File.Exists(projectPath))
                {
                    Console.Error.WriteLine($"Project file not found: {projectPath}");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                if (!container.openProjects(projectPath))
                {
                    Console.Error.WriteLine($"Failed to load project: {projectPath}");
                    Console.ReadKey();
                    Environment.Exit(2);
                }

                project = container.projectList.FirstOrDefault();
            }

            if (project == null)
            {
                Console.WriteLine("No project file specified. Running without a project.");
                Console.WriteLine($"Usage: FenixServer.Web.exe <project.pse> [--port {port}]");
                Console.WriteLine($"Starting on port {port} with empty project...");
                project = new Project();
            }

            AttachDriverEvents(project, container);
            StartAllDrivers(project, container);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                Program.ConfigureWebHost(project, container, port);
                Console.WriteLine($"FenixServer.Web starting on http://localhost:{port} ...");
                await Program.StartAsync(cts.Token);
                Console.WriteLine("Server running. Press Ctrl+C to stop.");
            }
            catch (Exception ex) when (!cts.IsCancellationRequested)
            {
                Console.Error.WriteLine($"[SERVER][ERROR] Failed to start: {ex.Message}");
                StopAllDrivers(project);
                await Program.StopAsync();
                Environment.Exit(3);
            }

            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (TaskCanceledException) { }

            Console.WriteLine("Stopping...");
            StopAllDrivers(project);
            await Program.StopAsync();
            Environment.Exit(0);
        }

        private static void ParseArgs(string[] args, out string? projectPath, out int port)
        {
            port = 8080;
            projectPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (string.Equals(arg, "--port", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedPort))
                        port = parsedPort;

                    i++;
                    continue;
                }

                if (arg.StartsWith("--port=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = arg.Substring("--port=".Length);
                    if (int.TryParse(value, out var parsedPort))
                        port = parsedPort;

                    continue;
                }

                if (arg.StartsWith("--", StringComparison.Ordinal))
                    continue;

                if (string.IsNullOrWhiteSpace(projectPath))
                    projectPath = arg;
            }
        }

        private static string? GetLastProjectPathFromRegistry(ProjectContainer container)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                    return null;

                return Registry.GetValue(container.RegUserRoot, container.LastPathKey, string.Empty) as string;
            }
            catch
            {
                return null;
            }
        }

        private static void AttachDriverEvents(Project project, ProjectContainer container)
        {
            foreach (var connection in project.connectionList)
            {
                if (connection.Idrv == null)
                    continue;

                AttachDriverEvents(connection.Idrv, connection.connectionName);
            }

            if (project.InternalTagsDrv != null)
                AttachDriverEvents((IDriverModel)project.InternalTagsDrv, "Internal Tags");

            if (project.ScriptEng != null)
                AttachDriverEvents((IDriverModel)project.ScriptEng, "Scripts");
        }

        private static void AttachDriverEvents(IDriverModel driver, string sourceName)
        {
            driver.information += (_, e) => Console.WriteLine(FormatDriverEvent("INFO", sourceName, e));
            driver.error += (_, e) => Console.Error.WriteLine(FormatDriverEvent("ERROR", sourceName, e));
            //driver.dataSent += (_, e) => Console.WriteLine(FormatDriverEvent("OUT", sourceName, e));
            //driver.dataRecived += (_, e) => Console.WriteLine(FormatDriverEvent("IN", sourceName, e));
        }

        private static string FormatDriverEvent(string level, string sourceName, EventArgs e)
        {
            if (e is ProjectEventArgs pe)
            {
                var tm = pe.element0 is DateTime dt ? dt : DateTime.Now;
                var message = pe.element1 as string
                    ?? pe.element as string
                    ?? pe.element2 as string
                    ?? "driver event";

                return $"[{tm:HH:mm:ss}] [{level}] [{sourceName}] {message}";
            }

            return $"[{DateTime.Now:HH:mm:ss}] [{level}] [{sourceName}] driver event";
        }

        private static void StartAllDrivers(Project project, ProjectContainer container)
        {
            foreach (var connection in project.connectionList)
            {
                if (connection.Idrv == null)
                {
                    Console.Error.WriteLine($"[DRIVER][ERROR] [{connection.connectionName}] driver not initialized");
                    continue;
                }

                var tags = container.GetAllITagsForDriver(project.objId, connection.Idrv.ObjId) ?? new List<ITag>();
                var started = connection.Idrv.activateCycle(tags);
                Console.WriteLine($"[DRIVER] [{connection.connectionName}] started: {started}");
            }

            if (project.InternalTagsDrv != null)
            {
                var intDrv = (IDriverModel)project.InternalTagsDrv;
                var tags = container.GetAllITagsForDriver(project.objId, container.IntTagsGuid) ?? new List<ITag>();
                var started = intDrv.activateCycle(tags);
                Console.WriteLine($"[DRIVER] [Internal Tags] started: {started}");
            }

            if (project.ScriptEng != null)
            {
                var scriptDrv = (IDriverModel)project.ScriptEng;
                var started = scriptDrv.activateCycle(new List<ITag>());
                Console.WriteLine($"[DRIVER] [Scripts] started: {started}");
            }
        }

        private static void StopAllDrivers(Project project)
        {
            foreach (var connection in project.connectionList)
            {
                try
                {
                    if (connection.Idrv != null)
                    {
                        var stopped = connection.Idrv.deactivateCycle();
                        Console.WriteLine($"[DRIVER] [{connection.connectionName}] stopped: {stopped}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[DRIVER][ERROR] [{connection.connectionName}] stop failed: {ex.Message}");
                }
            }

            try
            {
                if (project.InternalTagsDrv != null)
                {
                    var stopped = ((IDriverModel)project.InternalTagsDrv).deactivateCycle();
                    Console.WriteLine($"[DRIVER] [Internal Tags] stopped: {stopped}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DRIVER][ERROR] [Internal Tags] stop failed: {ex.Message}");
            }

            try
            {
                if (project.ScriptEng != null)
                {
                    var stopped = ((IDriverModel)project.ScriptEng).deactivateCycle();
                    Console.WriteLine($"[DRIVER] [Scripts] stopped: {stopped}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DRIVER][ERROR] [Scripts] stop failed: {ex.Message}");
            }
        }
    }
}
