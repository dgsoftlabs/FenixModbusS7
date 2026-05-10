using ProjectDataLib;
using System.Globalization;
using System.Text.Json;

namespace FenixServer.Web
{
    /// <summary>
    /// Legacy endpoint mappings — byte-for-byte compatible with the original HttpListener WebServer.
    /// URL pattern: /{obj}/{name}/{param}/{value}
    ///
    /// All JSON responses are returned as text/plain so that legacy JS calling
    /// JSON.parse(xhr.responseText) or JSON.parse(await response.text()) always
    /// receives a raw string — never a pre-parsed object.
    ///
    /// Documented legacy routes (POST and GET both accepted):
    ///   /Tags/All/All                   → tag array JSON
    ///   /Tag/{name}/Value/{val}         → set tag, returns new value as plain text
    ///   /Tag/{name}/Value               → get single tag value as plain text
    ///   /Connections/All/All            → connection array JSON
    ///   /Events/All/All                 → event array JSON
    ///   /Graph/All/All                  → graph series JSON
    ///   /Server/Buffor/Get              → probe counter as plain text
    ///   /Server/Buffor/Set/{n}          → set probe counter, returns new value
    ///   /Timer/{name}/Value             → timer value as plain text
    ///   /User/{name}/Value              → user value as plain text
    ///   /Machine/{name}/Value           → machine name as plain text
    /// </summary>
    internal static class LegacyEndpointMappings
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        public static void MapLegacyEndpoints(WebApplication app)
        {
            app.MapGet("/{obj}/{name}",                HandleNoParam).WithName("LegacyGet");
            app.MapPost("/{obj}/{name}",               HandleNoParam).WithName("LegacyPost");

            app.MapGet("/{obj}/{name}/{param}",        HandleWithParam).WithName("LegacyGetParam");
            app.MapPost("/{obj}/{name}/{param}",       HandleWithParam).WithName("LegacyPostParam");

            app.MapGet("/{obj}/{name}/{param}/{value}", HandleWithValue).WithName("LegacyGetValue");
            app.MapPost("/{obj}/{name}/{param}/{value}", HandleWithValue).WithName("LegacyPostValue");
        }

        private static IResult HandleNoParam(string obj, string name, HttpContext ctx)
            => Dispatch(obj, name, null, null, ctx);

        private static IResult HandleWithParam(string obj, string name, string? param, HttpContext ctx)
            => Dispatch(obj, name, param, null, ctx);

        private static IResult HandleWithValue(string obj, string name, string? param, string? value, HttpContext ctx)
            => Dispatch(obj, name, param, value, ctx);

        private static IResult Dispatch(string obj, string name, string? param, string? value, HttpContext ctx)
        {
            try
            {
                var project = EndpointMappings.GetProjectFromServices(ctx.RequestServices);
                var req = BuildRequest(obj, name, param, value, ctx);
                return Route(project, req);
            }
            catch
            {
                return EndpointMappings.ErrorResult();
            }
        }

        private static IResult Route(Project project, EndpointMappings.EndpointRequest req)
        {
            return req.ObjectKey switch
            {
                "server"                      => HandleServer(req),
                "tag" or "tags"               => HandleTag(project, req),
                "graph"                       => HandleGraph(project, req),
                "connection" or "connections" => HandleConnection(project, req),
                "event" or "events"           => HandleEvent(req),
                "timer"                       => HandleValueParam(req, project.GetTimerValue(req.NameKey)),
                "user"                        => HandleValueParam(req, project.GetUserValue(req.NameKey)),
                "machine"                     => HandleValueParam(req, project.GetMachineValue(req.NameKey)),
                _                             => EndpointMappings.ErrorResult()
            };
        }

        // ── timer / user / machine ─────────────────────────────────────────────────
        // Legacy: /Timer/{name}/Value, /User/{name}/Value, /Machine/{name}/Value
        // Param must be "value" — matches old HttpListener behaviour exactly.
        private static IResult HandleValueParam(EndpointMappings.EndpointRequest req, string result)
        {
            if (!req.ParamKey.Equals("value", StringComparison.OrdinalIgnoreCase))
                return EndpointMappings.ErrorResult();
            return PlainText(result);
        }

        // ── server/Buffor ──────────────────────────────────────────────────────────
        // /Server/Buffor/Get  → returns counter as plain number
        // /Server/Buffor/Set/{n} → sets counter, returns new value
        private static IResult HandleServer(EndpointMappings.EndpointRequest req)
        {
            if (!req.NameKey.Equals(EndpointMappings.BufforName, StringComparison.OrdinalIgnoreCase))
                return EndpointMappings.ErrorResult();

            if (req.ParamKey.Equals("get", StringComparison.OrdinalIgnoreCase))
                return PlainText(EndpointMappings.ProbeCounter.ToString(CultureInfo.InvariantCulture));

            if (req.ParamKey.Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(req.ValueKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    EndpointMappings.ProbeCounter = Math.Max(1, parsed);
                return PlainText(EndpointMappings.ProbeCounter.ToString(CultureInfo.InvariantCulture));
            }

            return EndpointMappings.ErrorResult();
        }

        // ── tags ───────────────────────────────────────────────────────────────────
        private static IResult HandleTag(Project project, EndpointMappings.EndpointRequest req)
        {
            if (IsAll(req))
                return GetAllTagsLegacy(project);

            if (req.ParamKey.Equals("value", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(req.ValueKey))
                return SetTagValueLegacy(project, req.NameKey, req.ValueKey);

            if (req.ParamKey.Equals("value", StringComparison.OrdinalIgnoreCase))
                return GetTagValueLegacy(project, req.NameKey);

            return EndpointMappings.ErrorResult();
        }

        /// <summary>
        /// Builds the exact DTO shape the original HttpListener WebServer returned:
        /// tagName, areaData, startData, deviceAdress, scAdres, value, formattedValue, typeData, description
        /// No ClrXml — matching real legacy output confirmed from production logs.
        /// </summary>
        private static IResult GetAllTagsLegacy(Project project)
        {
            var tags = project.PrCon.GetAllITags(project.objId, project.objId, false, false)
                       ?? new List<ITag>();

            var payload = tags
                .Where(t => t != null)
                .Select(t => new LegacyTagDto(
                    tagName:        t.Name,
                    areaData:       t is Tag tg  ? tg.areaData          : string.Empty,
                    startData:      t is Tag tg2 ? tg2.startData        : 0,
                    deviceAdress:   t is Tag tg3 ? (int)tg3.deviceAdress : 0,
                    scAdres:        t is Tag tg4 ? tg4.scAdres          : 0,
                    value:          t.Value,
                    formattedValue: EndpointMappings.SafeGetFormattedValue(t),
                    typeData:       t.TypeData_.ToString(),
                    description:    t is Tag tg5 ? tg5.describe         : null
                ))
                .ToArray();

            return PlainText(JsonSerializer.Serialize(payload, _jsonOptions));
        }

        private static IResult GetTagValueLegacy(Project project, string tagName)
        {
            if (EndpointMappings.TryFindTag(project, tagName, out var tag))
                return PlainText(tag?.Value?.ToString() ?? string.Empty);

            return EndpointMappings.ErrorResult();
        }

        private static IResult SetTagValueLegacy(Project project, string tagName, string value)
        {
            if (!EndpointMappings.TryFindTag(project, tagName, out var tag) || tag == null)
                return EndpointMappings.ErrorResult();

            try
            {
                tag.SetValue(value);
                return PlainText(tag.Value?.ToString() ?? string.Empty);
            }
            catch
            {
                return EndpointMappings.ErrorResult();
            }
        }

        // ── graph ──────────────────────────────────────────────────────────────────
        // /Graph/All/All → [{label, data:[[ts,val],...]}]
        private static IResult HandleGraph(Project project, EndpointMappings.EndpointRequest req)
        {
            if (!IsAll(req)) return EndpointMappings.ErrorResult();
            return PlainText(JsonSerializer.Serialize(EndpointMappings.BuildGraphResponse(project), _jsonOptions));
        }

        // ── connections ────────────────────────────────────────────────────────────
        // /Connections/All/All → [{Parameters,connectionName,DriverName,IsBlocked,isLive}]
        // GetConnectionsAll already returns Newtonsoft-serialized JSON string — pass through.
        private static IResult HandleConnection(Project project, EndpointMappings.EndpointRequest req)
        {
            if (!IsAll(req)) return EndpointMappings.ErrorResult();
            return PlainText(project.GetConnectionsAll(EndpointMappings.AllKey));
        }

        // ── events ─────────────────────────────────────────────────────────────────
        // /Events/All/All → [{Tm,Mess,frDateTime}]
        private static IResult HandleEvent(EndpointMappings.EndpointRequest req)
        {
            if (!IsAll(req)) return EndpointMappings.ErrorResult();
            return PlainText(JsonSerializer.Serialize(EndpointMappings.BuildEventsResponse(), _jsonOptions));
        }

        // ── DTO ────────────────────────────────────────────────────────────────────
        // Sealed record keeps field order and names identical to original legacy output.
        private sealed record LegacyTagDto(
            string?  tagName,
            string   areaData,
            int      startData,
            int      deviceAdress,
            int      scAdres,
            object?  value,
            string   formattedValue,
            string   typeData,
            string?  description);

        // ── helpers ────────────────────────────────────────────────────────────────
        private static bool IsAll(EndpointMappings.EndpointRequest req)
            => req.NameKey.Equals(EndpointMappings.AllKey, StringComparison.OrdinalIgnoreCase)
            && req.ParamKey.Equals(EndpointMappings.AllKey, StringComparison.OrdinalIgnoreCase);

        private static IResult PlainText(string text)
            => Results.Text(text, EndpointMappings.PlainTextContentType);

        private static EndpointMappings.EndpointRequest BuildRequest(
            string obj, string name, string? param, string? value, HttpContext ctx)
        {
            var objectKey = Normalize(obj);
            var nameKey   = Decode(name);
            var paramKey  = StripQuery(Normalize(param));
            var valueKey  = !string.IsNullOrWhiteSpace(Decode(value))
                ? Decode(value)
                : ctx.Request.RouteValues["value"]?.ToString() ?? string.Empty;

            return new EndpointMappings.EndpointRequest(objectKey, nameKey, paramKey, valueKey);
        }

        private static string Normalize(string? raw)
            => (raw ?? string.Empty).Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim().ToLowerInvariant() ?? string.Empty;

        private static string Decode(string? raw)
            => Uri.UnescapeDataString((raw ?? string.Empty).Trim().Replace('+', ' '));

        private static string StripQuery(string s)
        {
            var i = s.IndexOf('?');
            return i >= 0 ? s[..i] : s;
        }
    }
}
