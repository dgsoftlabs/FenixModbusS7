using ProjectDataLib;
using System.Globalization;

namespace FenixServer.Web
{
    /// <summary>
    /// Modern REST API endpoints under /api/...
    /// All responses use application/json and proper HTTP status codes.
    /// These routes are intended for new JS clients using fetch().then(r => r.json()).
    /// </summary>
    internal static class ApiEndpointMappings
    {
        public static void MapApiEndpoints(WebApplication app)
        {
            // Tags
            app.MapGet("/api/tags",                     GetAllTags).WithName("ApiGetAllTags");
            app.MapGet("/api/tags/{name}",              GetTag).WithName("ApiGetTag");
            app.MapPut("/api/tags/{name}",              SetTag).WithName("ApiSetTag");

            // Connections
            app.MapGet("/api/connections",              GetAllConnections).WithName("ApiGetAllConnections");

            // Graph
            app.MapGet("/api/graph",                    GetGraph).WithName("ApiGetGraph");

            // Events
            app.MapGet("/api/events",                   GetEvents).WithName("ApiGetEvents");

            // Server / Buffor
            app.MapGet("/api/server/buffor",            GetBuffor).WithName("ApiGetBuffor");
            app.MapPut("/api/server/buffor/{seconds}",  SetBuffor).WithName("ApiSetBuffor");

            // Misc
            app.MapGet("/api/timer/{name}",             GetTimer).WithName("ApiGetTimer");
            app.MapGet("/api/user/{name}",              GetUser).WithName("ApiGetUser");
            app.MapGet("/api/machine/{name}",           GetMachine).WithName("ApiGetMachine");
        }

        // ── tags ───────────────────────────────────────────────────────────────────
        private static IResult GetAllTags(HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                var tags = project.PrCon.GetAllITags(project.objId, project.objId, false, false)
                           ?? new List<ITag>();

                var payload = tags
                    .Where(t => t != null)
                    .Select(t => new
                    {
                        tagName        = t.Name,
                        areaData       = t is Tag tg ? tg.areaData   : string.Empty,
                        startData      = t is Tag tg2 ? tg2.startData : 0,
                        deviceAdress   = t is Tag tg3 ? tg3.deviceAdress : 0,
                        scAdres        = t is Tag tg4 ? tg4.scAdres  : 0,
                        value          = t.Value,
                        ClrXml         = t is Tag tg5 ? (object?)new { Val = tg5.ClrXml?.Val } : null,
                        formattedValue = EndpointMappings.SafeGetFormattedValue(t),
                        typeData       = t.TypeData_.ToString(),
                        description    = t is Tag tg6 ? tg6.describe : null
                    })
                    .ToArray();

                return Results.Ok(payload);
            }
            catch
            {
                return Results.Problem("Failed to retrieve tags.");
            }
        }

        private static IResult GetTag(string name, HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                if (!EndpointMappings.TryFindTag(project, name, out var tag) || tag == null)
                    return Results.NotFound(new { error = $"Tag '{name}' not found." });

                return Results.Ok(new
                {
                    tagName        = tag.Name,
                    value          = tag.Value,
                    formattedValue = EndpointMappings.SafeGetFormattedValue(tag),
                    typeData       = tag.TypeData_.ToString()
                });
            }
            catch
            {
                return Results.Problem("Failed to retrieve tag.");
            }
        }

        private static IResult SetTag(string name, HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                if (!EndpointMappings.TryFindTag(project, name, out var tag) || tag == null)
                    return Results.NotFound(new { error = $"Tag '{name}' not found." });

                string? value = ctx.Request.Query["value"].FirstOrDefault()
                                ?? ctx.Request.RouteValues["value"]?.ToString();

                if (string.IsNullOrWhiteSpace(value))
                    return Results.BadRequest(new { error = "Query parameter 'value' is required." });

                tag.SetValue(value);
                return Results.Ok(new
                {
                    tagName = tag.Name,
                    value   = tag.Value
                });
            }
            catch
            {
                return Results.Problem("Failed to set tag value.");
            }
        }

        // ── connections ────────────────────────────────────────────────────────────
        private static IResult GetAllConnections(HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                var json = project.GetConnectionsAll(EndpointMappings.AllKey);
                // Already serialized — deserialize and re-serve as proper JSON response
                var obj = System.Text.Json.JsonSerializer.Deserialize<object>(json);
                return Results.Ok(obj);
            }
            catch
            {
                return Results.Problem("Failed to retrieve connections.");
            }
        }

        // ── graph ──────────────────────────────────────────────────────────────────
        private static IResult GetGraph(HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                return Results.Ok(EndpointMappings.BuildGraphResponse(project));
            }
            catch
            {
                return Results.Problem("Failed to retrieve graph data.");
            }
        }

        // ── events ─────────────────────────────────────────────────────────────────
        private static IResult GetEvents(HttpContext ctx)
        {
            try
            {
                return Results.Ok(EndpointMappings.BuildEventsResponse());
            }
            catch
            {
                return Results.Problem("Failed to retrieve events.");
            }
        }

        // ── server / buffor ────────────────────────────────────────────────────────
        private static IResult GetBuffor()
            => Results.Ok(new { seconds = EndpointMappings.ProbeCounter });

        private static IResult SetBuffor(int seconds)
        {
            EndpointMappings.ProbeCounter = Math.Max(1, seconds);
            return Results.Ok(new { seconds = EndpointMappings.ProbeCounter });
        }

        // ── misc ───────────────────────────────────────────────────────────────────
        private static IResult GetTimer(string name, HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                return Results.Ok(new { name, value = project.GetTimerValue(name) });
            }
            catch { return Results.Problem(); }
        }

        private static IResult GetUser(string name, HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                return Results.Ok(new { name, value = project.GetUserValue(name) });
            }
            catch { return Results.Problem(); }
        }

        private static IResult GetMachine(string name, HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                return Results.Ok(new { name, value = project.GetMachineValue(name) });
            }
            catch { return Results.Problem(); }
        }

        // ── V1 API Endpoints ───────────────────────────────────────────────────────
        // Reuses the same handlers as the non-versioned /api/ endpoints.
        public static void MapApiV1Endpoints(this WebApplication app)
        {
            const string v1 = EndpointMappings.ApiV1Prefix;

            // Tags
            app.MapGet($"{v1}/tags",                     GetAllTags).WithName("ApiV1GetAllTags");
            app.MapGet($"{v1}/tags/{{name}}",            GetTag).WithName("ApiV1GetTag");
            app.MapPut($"{v1}/tags/{{name}}",            SetTag).WithName("ApiV1SetTag");

            // Connections
            app.MapGet($"{v1}/connections",              GetAllConnections).WithName("ApiV1GetAllConnections");

            // Graph
            app.MapGet($"{v1}/graph",                    GetGraph).WithName("ApiV1GetGraph");

            // Events
            app.MapGet($"{v1}/events",                   GetEvents).WithName("ApiV1GetEvents");

            // Server / Buffor
            app.MapGet($"{v1}/server/buffor",            GetBuffor).WithName("ApiV1GetBuffor");
            app.MapPut($"{v1}/server/buffor/{{seconds}}", SetBuffor).WithName("ApiV1SetBuffor");

            // Misc
            app.MapGet($"{v1}/timer/{{name}}",           GetTimer).WithName("ApiV1GetTimer");
            app.MapGet($"{v1}/user/{{name}}",            GetUser).WithName("ApiV1GetUser");
            app.MapGet($"{v1}/machine/{{name}}",         GetMachine).WithName("ApiV1GetMachine");
        }
    }
}
