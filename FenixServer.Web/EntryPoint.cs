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
            var projectPath = args.FirstOrDefault(a => !a.StartsWith("--"));

            int port = 8080;
            var portIndex = Array.IndexOf(args, "--port");
            if (portIndex >= 0 && portIndex + 1 < args.Length && int.TryParse(args[portIndex + 1], out var p))
                port = p;

            Project? project = null;
            var container = new ProjectContainer();

            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                if (!File.Exists(projectPath))
                {
                    Console.Error.WriteLine($"Project file not found: {projectPath}");
                    Environment.Exit(1);
                }

                if (!container.openProjects(projectPath))
                {
                    Console.Error.WriteLine($"Failed to load project: {projectPath}");
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

            Program.ConfigureWebHost(project, container, port);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            Console.WriteLine($"FenixServer.Web starting on http://localhost:{port} ...");
            await Program.StartAsync(cts.Token);
            Console.WriteLine("Server running. Press Ctrl+C to stop.");

            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (TaskCanceledException) { }

            Console.WriteLine("Stopping...");
            await Program.StopAsync();
        }
    }
}
