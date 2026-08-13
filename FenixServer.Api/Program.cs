using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Console;
using ProjectDataLib;
using System.Net;
using System.Text;

namespace FenixServer.Api
{
    public static class Program
    {
        public static WebApplication? _app;
        private static WebApplicationBuilder? _builder;

        public static void ConfigureWebHost(Project project, ProjectContainer projectContainer, int port = 80)
        {
            _builder = WebApplication.CreateBuilder();
            _builder.WebHost.UseUrls($"http://*:{port}");

            ConfigureServices(_builder, project, projectContainer);

            _app = _builder.Build();
            _app.UseCors();
            ConfigureAuthentication(_app, project);

            ConfigureStaticFiles(_app, project, projectContainer);
            _app.UseRouting();
            _app.MapFenixEndpoints();
        }

        public static void ConfigureWebHost(Project project, ProjectContainer projectContainer)
            => ConfigureWebHost(project, projectContainer, GetConfiguredPort(project));

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
            // Single-line, timestamped console logging for a cleaner server console.
            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(options =>
            {
                options.TimestampFormat = "HH:mm:ss ";
                options.SingleLine = true;
                options.ColorBehavior = LoggerColorBehavior.Enabled;
            });

            builder.Services
                .AddSingleton(project)
                                .AddSingleton(projectContainer)
                                .AddHostedService<ReconnectionService>()
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

        private static void ConfigureAuthentication(WebApplication app, Project? project)
        {
            var auth = project?.WebServer1?.Auth ?? AuthenticationSchemes.Anonymous;
            var isBasicEnabled = (auth & AuthenticationSchemes.Basic) == AuthenticationSchemes.Basic;
            if (!isBasicEnabled || project is null)
            {
                return;
            }

            app.Use(async (context, next) =>
            {
                if (TryAuthenticateBasic(context, project))
                {
                    await next();
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Basic realm=\"FenixServer.Api\"";
            });
        }

        private static bool TryAuthenticateBasic(HttpContext context, Project project)
        {
            var authorizationHeader = context.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var encodedCredentials = authorizationHeader.Substring("Basic ".Length).Trim();
            if (string.IsNullOrWhiteSpace(encodedCredentials))
            {
                return false;
            }

            string decodedCredentials;
            try
            {
                var bytes = Convert.FromBase64String(encodedCredentials);
                decodedCredentials = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return false;
            }

            var separatorIndex = decodedCredentials.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return false;
            }

            var username = decodedCredentials[..separatorIndex];
            var password = decodedCredentials[(separatorIndex + 1)..];

            var users = project?.WebServer1?.Users;
            if (users == null || users.Count == 0)
            {
                return true;
            }

            return users.Any(u =>
                string.Equals(u?.Name ?? string.Empty, username, StringComparison.Ordinal) &&
                string.Equals(u?.Pass ?? string.Empty, password, StringComparison.Ordinal));
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

        internal static int GetConfiguredPort(Project project)
        {
            var rawPrefix = project?.WebServer1?.Prefixes?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
            if (string.IsNullOrWhiteSpace(rawPrefix))
            {
                return 80;
            }

            var normalized = rawPrefix.Trim().Replace("+", "localhost").Replace("*", "localhost");
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                return uri.Port;
            }

            return 80;
        }
    }
}