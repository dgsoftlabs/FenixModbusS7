using Microsoft.Win32;
using ProjectDataLib;
using System.Runtime.Loader;

namespace FenixServer.Api
{
    /// <summary>
    /// Standalone console entry point.
    /// Usage: FenixServer.Api.exe [project.pse]
    /// </summary>
    internal static class EntryPoint
    {
        public static async Task Main(string[] args)
        {
            // UTF-8 so box-drawing and status glyphs render correctly in the console.
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }

            ParseArgs(args, out var projectPath);

            Project? project = null;
            var container = new ProjectContainer();
            container.ApplicationError += RegisterAppError;

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                projectPath = GetLastProjectPathFromRegistry(container);
            }

            if (!container.openProjects(projectPath))
            {
                ConsoleUi.Error($"Failed to load project: {projectPath}");
                Console.ReadKey();
                Environment.Exit(2);
            }

            project = container?.projectList?.FirstOrDefault() ?? new Project();

            using var cts = new CancellationTokenSource();
            var shutdownStarted = 0;

            void EnsureShutdown(string reason)
            {
                if (Interlocked.Exchange(ref shutdownStarted, 1) != 0)
                    return;

                try
                {
                    ConsoleUi.Warn($"Shutting down... ({reason})");
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
                var appVersion = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown";
                PrintStartupBanner(appVersion);

                var configuredPort = Program.GetConfiguredPort(project);
                Program.ConfigureWebHost(project, container, configuredPort);

                PrintProjectPanel(project, configuredPort);
                PrintParametersPanel(project);
                PrintEndpointLinks(configuredPort);
                ConsoleUi.Rule();

                AttachDriverEvents(project);
                StartAllDrivers(project, container);
                PrintConnectionStatus(project);

                await Program.StartAsync(cts.Token);
                ConsoleUi.Ok("Server running. Press Ctrl+C to stop.");
                ConsoleUi.Rule();

                _ = RunStatusLoopAsync(project, cts.Token);
            }
            catch (Exception ex) when (!cts.IsCancellationRequested)
            {
                ConsoleUi.Error($"Failed to start: {ex.Message}");
                EnsureShutdown("StartError");
                Environment.Exit(3);
            }

            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (TaskCanceledException) { }

            EnsureShutdown("Cancellation");
            Environment.Exit(0);
        }

        private static void RegisterAppError(object? o, EventArgs e)
        {
            if (e is not ProjectEventArgs pe)
                return;

            var error = pe.element as Exception
                ?? pe.element2 as Exception;

            if (error != null)
                ConsoleUi.Error($"[APP] {error.Message}");
        }

        private static void PrintStartupBanner(string version)
        {
            Console.WriteLine();
            ConsoleUi.Banner("Fenix Server API", "version " + version);
            Console.WriteLine();
        }

        private static void PrintProjectPanel(Project project, int port)
        {
            ConsoleUi.Box(new[]
            {
                $"Project  : {project.projectName}",
                $"Path     : {project.path}",
                $"Endpoint : http://localhost:{port}",
                $"Elements : {project.connectionList.Count} connection(s), {project.DevicesList.Count} device(s)"
            });
        }

        private static void PrintParametersPanel(Project project)
        {
            var lines = new List<string> { "Web server" };

            try { lines.Add($"  Auth     : {project.WebServer1?.Auth}"); } catch { }
            try { lines.Add($"  Users    : {FormatUsers(project.WebServer1?.Users)}"); } catch { }
            try { lines.Add($"  Prefixes : {FormatPrefixes(project.WebServer1?.Prefixes)}"); } catch { }

            foreach (var connection in project.connectionList)
            {
                var driverName = connection.Idrv?.driverName ?? "not initialized";
                lines.Add($"Connection '{connection.connectionName}' [{driverName}]");
                lines.Add("  " + DescribeDriverParam(connection.Idrv?.setDriverParam));
            }

            ConsoleUi.Box(lines.ToArray());
        }

        private static string FormatUsers(List<UserClass>? users)
        {
            if (users == null || users.Count == 0)
                return "0";

            return $"{users.Count} ({string.Join(", ", users.Select(u => u.Name))})";
        }

        private static string FormatPrefixes(string[]? prefixes)
        {
            if (prefixes == null || prefixes.Length == 0)
                return "(none)";

            return string.Join("; ", prefixes);
        }

        private static string DescribeDriverParam(object? param)
        {
            if (param == null)
                return "(no parameters)";

            var parts = new List<string>();

            foreach (var prop in param.GetType().GetProperties(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                if (!IsScalar(prop.PropertyType))
                    continue;

                try
                {
                    var value = prop.GetValue(param);
                    var label = (Attribute.GetCustomAttribute(prop, typeof(System.ComponentModel.DisplayNameAttribute))
                        as System.ComponentModel.DisplayNameAttribute)?.DisplayName ?? prop.Name;

                    parts.Add($"{label}={value}");
                }
                catch
                {
                }
            }

            return parts.Count == 0 ? "(no parameters)" : string.Join(", ", parts);
        }

        private static bool IsScalar(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(Guid);
        }

        private static void PrintEndpointLinks(int port)
        {
            ConsoleUi.Link("Open in browser :", $"http://localhost:{port}");

            var lanIp = GetLocalIpv4();
            if (!string.IsNullOrWhiteSpace(lanIp))
                ConsoleUi.Link("On the network  :", $"http://{lanIp}:{port}");
        }

        private static string? GetLocalIpv4()
        {
            try
            {
                return System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName())
                    .FirstOrDefault(a =>
                        a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !System.Net.IPAddress.IsLoopback(a))
                    ?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static void PrintConnectionStatus(Project project)
        {
            if (project.connectionList.Count == 0)
            {
                ConsoleUi.Warn("Project has no connections configured.");
                return;
            }

            ConsoleUi.Title("Connections:");
            foreach (var connection in project.connectionList)
            {
                bool live = connection.Idrv != null && connection.Idrv.isAlive;
                if (live)
                    ConsoleUi.Ok($"  ● {connection.connectionName}  LIVE");
                else
                    ConsoleUi.Error($"  ○ {connection.connectionName}  DEAD");
            }
        }

        /// <summary>
        /// Periodic heartbeat that prints a single colored status line whenever the
        /// LIVE/DEAD state of any connection changes.
        /// </summary>
        private static async Task RunStatusLoopAsync(Project project, CancellationToken ct)
        {
            var lastSignature = string.Empty;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(5000, ct);

                    var signature = string.Join("|", project.connectionList.Select(c =>
                        $"{c.connectionName}:{(c.Idrv != null && c.Idrv.isAlive)}"));

                    if (signature == lastSignature)
                        continue;

                    lastSignature = signature;

                    int live = project.connectionList.Count(c => c.Idrv != null && c.Idrv.isAlive);
                    int total = project.connectionList.Count;

                    var segments = new List<(string Text, ConsoleColor Color)>
                    {
                        ("Status ", ConsoleColor.Cyan),
                        ($"{live}/{total} LIVE", live == total ? ConsoleColor.Green : ConsoleColor.Yellow)
                    };

                    foreach (var connection in project.connectionList)
                    {
                        bool alive = connection.Idrv != null && connection.Idrv.isAlive;
                        segments.Add(($"   {connection.connectionName}", alive ? ConsoleColor.Green : ConsoleColor.Red));
                        segments.Add((alive ? "  ●" : "  ○", alive ? ConsoleColor.Green : ConsoleColor.Red));
                    }

                    ConsoleUi.WriteLine(segments.ToArray());
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                // Never let the heartbeat crash the server.
            }
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

        private static void AttachDriverEvents(Project project)
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
            driver.information += (_, e) => ConsoleUi.Info(FormatDriverEvent("INFO", sourceName, e));
            driver.error += (_, e) => ConsoleUi.Error(FormatDriverEvent("ERROR", sourceName, e));
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
                    ConsoleUi.Error($"[DRIVER] {connection.connectionName}  driver not initialized");
                    continue;
                }

                var tags = container.GetAllITagsForDriver(project.objId, connection.Idrv.ObjId) ?? new List<ITag>();
                var started = connection.Idrv.activateCycle(tags);
                if (started)
                    ConsoleUi.Ok($"[DRIVER] {connection.connectionName}  started");
                else
                    ConsoleUi.Error($"[DRIVER] {connection.connectionName}  FAILED to start");
            }

            if (project.InternalTagsDrv != null)
            {
                var intDrv = (IDriverModel)project.InternalTagsDrv;
                var tags = container.GetAllITagsForDriver(project.objId, container.IntTagsGuid) ?? new List<ITag>();
                var started = intDrv.activateCycle(tags);
                if (started)
                    ConsoleUi.Ok("[DRIVER] Internal Tags  started");
                else
                    ConsoleUi.Error("[DRIVER] Internal Tags  FAILED to start");
            }

            if (project.ScriptEng != null)
            {
                var scriptDrv = (IDriverModel)project.ScriptEng;
                var started = scriptDrv.activateCycle(new List<ITag>());
                if (started)
                    ConsoleUi.Ok("[DRIVER] Scripts  started");
                else
                    ConsoleUi.Error("[DRIVER] Scripts  FAILED to start");
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
                        connection.Idrv.deactivateCycle();
                        ConsoleUi.App($"[DRIVER] {connection.connectionName}  stopped");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleUi.Error($"[DRIVER] {connection.connectionName}  stop failed: {ex.Message}");
                }
            }

            try
            {
                if (project.InternalTagsDrv != null)
                {
                    ((IDriverModel)project.InternalTagsDrv).deactivateCycle();
                    ConsoleUi.App("[DRIVER] Internal Tags  stopped");
                }
            }
            catch (Exception ex)
            {
                ConsoleUi.Error($"[DRIVER] Internal Tags  stop failed: {ex.Message}");
            }

            try
            {
                if (project.ScriptEng != null)
                {
                    ((IDriverModel)project.ScriptEng).deactivateCycle();
                    ConsoleUi.App("[DRIVER] Scripts  stopped");
                }
            }
            catch (Exception ex)
            {
                ConsoleUi.Error($"[DRIVER] Scripts  stop failed: {ex.Message}");
            }
        }
    }
}