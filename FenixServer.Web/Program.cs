using ProjectDataLib;

namespace FenixServer.Web
{
    public static class Program
    {
        public static WebApplication? _app;
        private static WebApplicationBuilder? _builder;

        /// <summary>
        /// Initializes ASP.NET Core application for use in WinForms host.
        /// Call this once from your WinForms application startup.
        /// </summary>
        public static void ConfigureWebHost(Project project, ProjectContainer projectContainer)
        {
            _builder = WebApplication.CreateBuilder(new[] { "--urls", "http://+:80/" });

            _builder.Services
                .AddSingleton(project)
                .AddSingleton(projectContainer)
                .AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });

            _app = _builder.Build();

            _app.UseCors();

            // Health check endpoint
            _app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

            // Project script evaluation endpoints
            _app.MapPost("/api/{obj}/{name}/{param}", HandleScriptEval)
                .WithName("EvaluateScript")
                .WithOpenApi();

            _app.MapPost("/api/{obj}/{name}", HandleScriptEval)
                .WithName("EvaluateScriptNoParam")
                .WithOpenApi();

            _app.MapGet("/api/tags/all", GetAllTags)
                .WithName("GetAllTags")
                .WithOpenApi();

            _app.MapGet("/api/tags/{name}", GetTagValue)
                .WithName("GetTagValue")
                .WithOpenApi();

            _app.MapPost("/api/tags/{name}/{value}", SetTagValue)
                .WithName("SetTagValue")
                .WithOpenApi();

            _app.MapGet("/api/connections/all", GetAllConnections)
                .WithName("GetAllConnections")
                .WithOpenApi();

            _app.MapGet("/api/events/all", GetAllEvents)
                .WithName("GetAllEvents")
                .WithOpenApi();
        }

        /// <summary>
        /// Starts the web host asynchronously in a background thread.
        /// </summary>
        public static async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_app == null)
                throw new InvalidOperationException("ConfigureWebHost must be called before StartAsync");

            _ = Task.Run(async () =>
            {
                try
                {
                    await _app.RunAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Web host error: {ex.Message}");
                }
            }, cancellationToken);

            // Give the host time to start
            await Task.Delay(500, cancellationToken);
        }

        /// <summary>
        /// Stops the web host gracefully.
        /// </summary>
        public static async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
            }
        }

        private static Project GetProjectFromServices(IServiceProvider services)
        {
            var project = services.GetRequiredService<Project>();
            if (project?.ScriptCon == null)
                throw new InvalidOperationException("Project not properly initialized");
            return project;
        }

        private static IResult HandleScriptEval(
            string obj, 
            string name, 
            string? param,
            HttpContext context)
        {
            try
            {
                var project = GetProjectFromServices(context.RequestServices);

                // Build script command: Prj.GetObject[Param]("Name")
                string cmd = string.IsNullOrEmpty(param)
                    ? $"Prj.Get{obj}(\"{name}\")"
                    : $"Prj.Get{obj}{param}(\"{name}\")";

                var result = project.ScriptCon.Eval(cmd);
                return Results.Ok(new { value = result?.ToString() ?? "null" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        private static IResult GetAllTags(HttpContext context)
        {
            try
            {
                var project = GetProjectFromServices(context.RequestServices);
                var result = project.ScriptCon.Eval("Prj.GetTagsAll(\"all\")");
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        private static IResult GetTagValue(string name, HttpContext context)
        {
            try
            {
                var project = GetProjectFromServices(context.RequestServices);
                var result = project.GetTag(name);
                return Results.Ok(new { name, value = result?.ToString() ?? "null" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        private static IResult SetTagValue(string name, string value, HttpContext context)
        {
            try
            {
                var project = GetProjectFromServices(context.RequestServices);
                var result = project.SetTag(name, value);
                return Results.Ok(new { name, value = result?.ToString() ?? "null" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        private static IResult GetAllConnections(HttpContext context)
        {
            try
            {
                var project = GetProjectFromServices(context.RequestServices);
                var result = project.ScriptCon.Eval("Prj.GetConnectionsAll(\"all\")");
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        private static IResult GetAllEvents(HttpContext context)
        {
            try
            {
                var project = GetProjectFromServices(context.RequestServices);
                var result = project.ScriptCon.Eval("Prj.GetEventsAll(\"all\")");
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }
    }
}
