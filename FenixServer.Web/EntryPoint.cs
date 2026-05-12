using Microsoft.Win32;
using ProjectDataLib;
using System.IO;
using System.Runtime.Loader;

namespace FenixServer.Web
{
    /// <summary>
    /// Standalone console entry point.
    /// Usage: FenixServer.Web.exe [project.pse]
    /// </summary>
    internal static class EntryPoint
    {
        public static async Task Main(string[] args)
        {
            ParseArgs(args, out var projectPath);

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
                Console.WriteLine("Usage: FenixServer.Web.exe <project.pse>");
                Console.WriteLine("Starting with empty project...");
                project = new Project();
            }

            using var cts = new CancellationTokenSource();
            var shutdownStarted = 0;

            void EnsureShutdown(string reason)
            {
                if (Interlocked.Exchange(ref shutdownStarted, 1) != 0)
                    return;

                try
                {
                    Console.WriteLine($"Stopping... ({reason})");
                }
                catch
                {
                }

                try
                {
                    StopAllDrivers(project);
                }
                catch
                {
                }

                try
                {
                    Program.StopAsync().GetAwaiter().GetResult();
                }
                catch
                {
                }
            }

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                EnsureShutdown("Ctrl+C");
            };

            AppDomain.CurrentDomain.ProcessExit += (_, __) => EnsureShutdown("ProcessExit");
            AssemblyLoadContext.Default.Unloading += _ => EnsureShutdown("Unloading");

            try
            {
                int activePort;
                string startupMessage;

                var configuredPort = GetPortFromProjectPrefix(project);
                Program.ConfigureWebHost(project, container, configuredPort);
                startupMessage = $"FenixServer.Web starting from project setup on http://localhost:{configuredPort} ...";
                activePort = configuredPort;

                PrintStartupBanner();
                PrintStartupDocumentation(project, activePort);
                Console.WriteLine(startupMessage);
                Console.WriteLine($"Project path: {project.path}");
                Console.WriteLine();

                AttachDriverEvents(project, container);
                StartAllDrivers(project, container);

                await Program.StartAsync(cts.Token);
                Console.WriteLine("Server running. Press Ctrl+C to stop.");
            }
            catch (Exception ex) when (!cts.IsCancellationRequested)
            {
                Console.Error.WriteLine($"[SERVER][ERROR] Failed to start: {ex.Message}");
                EnsureShutdown("StartError");
                Environment.Exit(3);
            }

            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (TaskCanceledException) { }

            EnsureShutdown("Cancellation");
            Environment.Exit(0);
        }

        private static void PrintStartupBanner()
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("   F E N I X   S E R V E R   W E B");
            Console.WriteLine("========================================");
            Console.WriteLine();
        }

        private static void PrintStartupDocumentation(Project project, int port)
        {
            Console.WriteLine("Run parameters:");
            Console.WriteLine("- <project.pse>      Optional path to project file");
            Console.WriteLine("- No project path:   Loads the last project from registry and starts it");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  FenixServer.Web.exe");
            Console.WriteLine("  FenixServer.Web.exe demo.pse");
            Console.WriteLine("  FenixServer.Web.exe .\\p\\demo.pse");
            Console.WriteLine();
        }

        private static void ParseArgs(string[] args, out string? projectPath)
        {
            projectPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.StartsWith("--", StringComparison.Ordinal))
                    continue;

                if (string.IsNullOrWhiteSpace(projectPath))
                    projectPath = arg;
            }
        }

        private static int GetPortFromProjectPrefix(Project project)
        {
            var rawPrefix = project?.WebServer1?.Prefixes?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            if (string.IsNullOrWhiteSpace(rawPrefix))
                return 80;

            var normalized = rawPrefix.Trim().Replace("+", "localhost").Replace("*", "localhost");
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && uri.Port > 0)
                return uri.Port;

            return 80;
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
            Console.WriteLine();
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
