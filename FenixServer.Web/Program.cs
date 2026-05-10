using Microsoft.Extensions.FileProviders;
using ProjectDataLib;
using System.IO;

namespace FenixServer.Web
{
    public static class Program
    {
        public static WebApplication? _app;
        private static WebApplicationBuilder? _builder;

        public static void ConfigureWebHost(Project project, ProjectContainer projectContainer, int port = 80)
        {
            _builder = WebApplication.CreateBuilder();
            _builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(port);
            });

            ConfigureServices(_builder, project, projectContainer);

            _app = _builder.Build();
            _app.UseCors();

            ConfigureStaticFiles(_app, project, projectContainer);
            _app.UseRouting();
            _app.MapFenixEndpoints();
        }

        public static void ConfigureWebHost(Project project, ProjectContainer projectContainer)
            => ConfigureWebHost(project, projectContainer, 80);

        public static async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_app == null)
                throw new InvalidOperationException("ConfigureWebHost must be called before StartAsync");

            var startTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _app.StartAsync(cancellationToken);
                    startTcs.TrySetResult();
                }
                catch (OperationCanceledException ex)
                {
                    startTcs.TrySetCanceled(ex.CancellationToken);
                }
                catch (Exception ex)
                {
                    startTcs.TrySetException(ex);
                }
            }, cancellationToken);

            await startTcs.Task;
        }

        public static async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
            }
        }

        private static void ConfigureServices(WebApplicationBuilder builder, Project project, ProjectContainer projectContainer)
        {
            builder.Services
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
        }

        private static void ConfigureStaticFiles(WebApplication app, Project project, ProjectContainer projectContainer)
        {
            var staticRoot = GetStaticRoot(project, projectContainer);
            if (!Directory.Exists(staticRoot))
            {
                app.MapRootEndpoint();
                return;
            }

            var staticProvider = new PhysicalFileProvider(staticRoot);

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = staticProvider,
                RequestPath = ""
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = staticProvider,
                RequestPath = "",
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            });
        }

        private static string GetStaticRoot(Project project, ProjectContainer projectContainer)
        {
            var projectDirectory = Path.GetDirectoryName(project.path) ?? string.Empty;
            var httpFolder = projectContainer.HttpCatalog.TrimStart('\\', '/');
            return Path.Combine(projectDirectory, httpFolder);
        }
    }
}
