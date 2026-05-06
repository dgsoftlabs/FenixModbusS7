using ProjectDataLib;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;

namespace FenixServer.Web
{
    public static class EndpointMappings
    {
        private const string PlainTextContentType = "text/plain";
        private const string JsonContentType = "application/json";
        private const string ErrorResponse = "Error";
        private const string AllKey = "all";
        private const string BufforName = "Buffor";
        private const int MaxEventsCount = 1000;

        private static int _probeCounter = 100;
        private static readonly object _graphSync = new object();
        private static readonly ConcurrentDictionary<string, List<object[]>> _graphData = new();
        private static readonly ConcurrentQueue<EventEntry> _events = new();

        private readonly record struct EndpointRequest(string ObjectKey, string NameKey, string ParamKey, string ValueKey);
        private readonly record struct EventEntry(DateTimeOffset Tm, string Mess);

        public static void PublishEvent(string? message, DateTimeOffset? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _events.Enqueue(new EventEntry(timestamp ?? DateTimeOffset.Now, message.Trim()));
            while (_events.Count > MaxEventsCount && _events.TryDequeue(out _))
            {
            }
        }

        public static void MapRootEndpoint(this WebApplication app)
        {
            app.MapGet("/", () => Results.Ok(new
            {
                app = "FenixServer",
                status = "running",
                health = "/health"
            }));
        }

        public static void MapFenixEndpoints(this WebApplication app)
        {
            MapHealthEndpoint(app);
            MapRequestEndpoints(app);
        }

        private static void MapHealthEndpoint(WebApplication app)
        {
            app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
        }

        private static void MapRequestEndpoints(WebApplication app)
        {
            app.MapPost("/{obj}/{name}/{param}",
                    (string obj, string name, string? param, HttpContext context) => HandleRequestWithParam(obj, name, param, context))
                .WithName("CodeRequest")
                .WithOpenApi();

            app.MapPost("/{obj}/{name}",
                    (string obj, string name, HttpContext context) => HandleRequestWithoutParam(obj, name, context))
                .WithName("CodeRequestNoParam")
                .WithOpenApi();

            app.MapGet("/{obj}/{name}/{param}",
                    (string obj, string name, string? param, HttpContext context) => HandleRequestWithParam(obj, name, param, context))
                .WithName("CodeRequestGet")
                .WithOpenApi();

            app.MapGet("/{obj}/{name}",
                    (string obj, string name, HttpContext context) => HandleRequestWithoutParam(obj, name, context))
                .WithName("CodeRequestNoParamGet")
                .WithOpenApi();

            app.MapPost("/{obj}/{name}/{param}/{value}", HandleRequestWithValue)
                .WithName("CodeRequestWithValue")
                .WithOpenApi();

            app.MapGet("/{obj}/{name}/{param}/{value}", HandleRequestWithValue)
                .WithName("CodeRequestWithValueGet")
                .WithOpenApi();
        }

        private static Project GetProjectFromServices(IServiceProvider services)
        {
            return services.GetRequiredService<Project>()
                ?? throw new InvalidOperationException("Project not properly initialized");
        }

        private static IResult HandleRequestWithValue(
            string obj,
            string name,
            string? param,
            string? value,
            HttpContext context)
        {
            return HandleRequestCore(obj, name, param, value, context);
        }

        private static IResult HandleRequestWithParam(
            string obj,
            string name,
            string? param,
            HttpContext context)
        {
            return HandleRequestCore(obj, name, param, null, context);
        }

        private static IResult HandleRequestWithoutParam(
            string obj,
            string name,
            HttpContext context)
        {
            return HandleRequestCore(obj, name, null, null, context);
        }

        private static IResult HandleRequestCore(
            string obj,
            string name,
            string? param,
            string? value,
            HttpContext context)
        {
            try
            {
                var project = GetProjectFromServices(context.RequestServices);
                var request = CreateRequest(obj, name, param, value, context);
                return DispatchRequest(project, request);
            }
            catch
            {
                return ErrorResult();
            }
        }

        private static IResult DispatchRequest(Project project, EndpointRequest request)
        {
            return request.ObjectKey switch
            {
                "server" => HandleServerRequest(request),
                "tag" or "tags" => HandleTagRequest(project, request),
                "graph" => HandleGraphRequest(project, request),
                "connection" or "connections" => HandleConnectionRequest(project, request),
                "event" or "events" => HandleEventRequest(request),
                "timer" => Results.Text(project.GetTimerValue(request.NameKey), PlainTextContentType),
                "user" => Results.Text(project.GetUserValue(request.NameKey), PlainTextContentType),
                "machine" => Results.Text(project.GetMachineValue(request.NameKey), PlainTextContentType),
                _ => ErrorResult()
            };
        }

        private static EndpointRequest CreateRequest(
            string obj,
            string name,
            string? param,
            string? value,
            HttpContext context)
        {
            var objectKey = NormalizeFirstToken(obj);
            var nameKey = DecodeRouteValue(name);
            var paramKey = NormalizeParameter(param);
            var valueKey = GetValueKey(value, context);

            return new EndpointRequest(objectKey, nameKey, paramKey, valueKey);
        }

        private static string GetValueKey(string? value, HttpContext context)
        {
            var valueKey = DecodeRouteValue(value);
            if (!string.IsNullOrWhiteSpace(valueKey))
            {
                return valueKey;
            }

            return context.Request.RouteValues["value"]?.ToString() ?? string.Empty;
        }

        private static string NormalizeFirstToken(string? rawValue)
        {
            return (rawValue ?? string.Empty)
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?
                .Trim()
                .ToLowerInvariant() ?? string.Empty;
        }

        private static string NormalizeParameter(string? rawValue)
        {
            var parameter = NormalizeFirstToken(rawValue);
            var querySeparatorIndex = parameter.IndexOf('?');
            return querySeparatorIndex >= 0
                ? parameter[..querySeparatorIndex]
                : parameter;
        }

        private static string DecodeRouteValue(string? rawValue)
        {
            var normalized = (rawValue ?? string.Empty).Trim().Replace('+', ' ');
            return Uri.UnescapeDataString(normalized);
        }

        private static bool IsAllRequest(EndpointRequest request)
        {
            return request.NameKey.Equals(AllKey, StringComparison.OrdinalIgnoreCase) && request.ParamKey == AllKey;
        }

        private static IResult HandleServerRequest(EndpointRequest request)
        {
            if (!request.NameKey.Equals(BufforName, StringComparison.OrdinalIgnoreCase))
            {
                return ErrorResult();
            }

            if (request.ParamKey == "get")
            {
                return ProbeCounterResult();
            }

            if (request.ParamKey == "set")
            {
                if (int.TryParse(request.ValueKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    _probeCounter = Math.Max(1, parsed);
                }

                return ProbeCounterResult();
            }

            return ErrorResult();
        }

        private static IResult ProbeCounterResult()
        {
            return Results.Text(_probeCounter.ToString(CultureInfo.InvariantCulture), PlainTextContentType);
        }

        private static IResult HandleTagRequest(Project project, EndpointRequest request)
        {
            if (IsAllRequest(request))
            {
                return GetAllTagsResult(project);
            }

            if (request.ParamKey == "value" && !string.IsNullOrWhiteSpace(request.ValueKey))
            {
                return SetTagValue(project, request.NameKey, request.ValueKey);
            }

            return GetTagValue(project, request.NameKey);
        }

        private static IResult HandleGraphRequest(Project project, EndpointRequest request)
        {
            return IsAllRequest(request)
                ? Results.Json(BuildGraphResponse(project))
                : ErrorResult();
        }

        private static IResult HandleConnectionRequest(Project project, EndpointRequest request)
        {
            return IsAllRequest(request)
                ? Results.Text(project.GetConnectionsAll(AllKey), JsonContentType)
                : ErrorResult();
        }

        private static IResult HandleEventRequest(EndpointRequest request)
        {
            return IsAllRequest(request)
                ? Results.Json(BuildEventsResponse())
                : ErrorResult();
        }

        private static object[] BuildEventsResponse()
        {
            return _events
                .Select(e => new
                {
                    Tm = e.Tm,
                    Mess = e.Mess,
                    frDateTime = e.Tm.LocalDateTime.ToString("d.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
                })
                .Cast<object>()
                .ToArray();
        }

        private static object BuildGraphResponse(Project project)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var tags = project.PrCon.GetAllITags(project.objId, project.objId, false, true) ?? new List<ITag>();

            lock (_graphSync)
            {
                foreach (var tag in tags)
                {
                    AppendGraphPoint(tag, timestamp);
                }

                return BuildGraphSeriesResponse(tags);
            }
        }

        private static void AppendGraphPoint(ITag? tag, long timestamp)
        {
            try
            {
                if (tag == null)
                {
                    return;
                }

                var key = tag.Name ?? string.Empty;
                var series = _graphData.GetOrAdd(key, _ => new List<object[]>());

                if (tag.TypeData_ == TypeData.BIT)
                {
                    AppendBitGraphPoint(series, tag, timestamp);
                }
                else
                {
                    AppendNumericGraphPoint(series, tag, timestamp);
                }

                TrimSeries(series);
            }
            catch
            {
            }
        }

        private static void AppendBitGraphPoint(List<object[]> series, ITag tag, long timestamp)
        {
            var currentPoint = SafeGetBitPoint(tag.Value);

            if (series.Count < 1)
            {
                series.Add(new object[] { timestamp, currentPoint });
                return;
            }

            var lastPoint = SafeGetDouble(series[^1][1]);
            if (!lastPoint.Equals(currentPoint))
            {
                series.Add(new object[] { timestamp, lastPoint });
            }

            series.Add(new object[] { timestamp, currentPoint });
        }

        private static void AppendNumericGraphPoint(List<object[]> series, ITag tag, long timestamp)
        {
            var point = SafeGetDouble(tag.Value);

            if (point == 0d)
            {
                var raw = tag.GetFormatedValue();
                if (!double.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out point)
                    && !double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out point))
                {
                    point = 0d;
                }
            }

            series.Add(new object[] { timestamp, point });
        }

        private static object[] BuildGraphSeriesResponse(List<ITag> tags)
        {
            return tags
                .Where(tag => tag != null)
                .Select(tag => new
                {
                    label = tag.Name,
                    data = _graphData.TryGetValue(tag.Name ?? string.Empty, out var series)
                        ? series
                        : new List<object[]>()
                })
                .Cast<object>()
                .ToArray();
        }

        private static double SafeGetBitPoint(object? value)
        {
            if (value is bool booleanValue)
            {
                return booleanValue ? 1d : 0d;
            }

            if (value is string stringValue)
            {
                if (bool.TryParse(stringValue, out var parsedBool))
                {
                    return parsedBool ? 1d : 0d;
                }

                if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedNumber))
                {
                    return parsedNumber != 0d ? 1d : 0d;
                }
            }

            return SafeGetDouble(value) != 0d ? 1d : 0d;
        }

        private static double SafeGetDouble(object? value)
        {
            if (value == null)
            {
                return 0d;
            }

            if (value is double doubleValue)
            {
                return doubleValue;
            }

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }

            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0d;
        }

        private static void TrimSeries(List<object[]> series)
        {
            while (series.Count > 0)
            {
                var min = Convert.ToInt64(series[0][0]);
                var diff = DateTime.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(min).UtcDateTime;
                if (diff <= TimeSpan.FromSeconds(_probeCounter))
                {
                    break;
                }

                series.RemoveAt(0);
            }
        }

        private static IResult ErrorResult()
        {
            return Results.Text(ErrorResponse, PlainTextContentType);
        }

        private static IResult GetTagValue(Project project, string tagName)
        {
            if (TryFindTag(project, tagName, out var tag))
            {
                return Results.Text(tag?.Value?.ToString() ?? string.Empty, PlainTextContentType);
            }

            return ErrorResult();
        }

        private static IResult SetTagValue(Project project, string tagName, string value)
        {
            if (!TryFindTag(project, tagName, out var tag) || tag == null)
            {
                return ErrorResult();
            }

            try
            {
                tag.SetValue(value);
                return Results.Text(tag.Value?.ToString() ?? string.Empty, PlainTextContentType);
            }
            catch
            {
                return ErrorResult();
            }
        }

        private static bool TryFindTag(Project project, string tagName, out ITag? tag)
        {
            tag = null;

            if (project?.PrCon == null || string.IsNullOrWhiteSpace(tagName))
            {
                return false;
            }

            var allTags = project.PrCon.GetAllITags(project.objId, project.objId, false, false);
            if (allTags == null || allTags.Count == 0)
            {
                return false;
            }

            var normalizedTagName = tagName.Trim();
            tag = allTags.FirstOrDefault(t =>
                t != null &&
                !string.IsNullOrWhiteSpace(t.Name) &&
                string.Equals(t.Name.Trim(), normalizedTagName, StringComparison.OrdinalIgnoreCase));

            return tag != null;
        }

        private static IResult GetAllTagsResult(Project project)
        {
            var legacyJson = project.GetTagsAll(AllKey);
            if (!string.IsNullOrWhiteSpace(legacyJson)
                && !legacyJson.Equals("Empty", StringComparison.OrdinalIgnoreCase)
                && (legacyJson.Contains("\"value\"", StringComparison.OrdinalIgnoreCase)
                    || legacyJson.Contains("\"Value\"", StringComparison.OrdinalIgnoreCase)))
            {
                return Results.Text(legacyJson, JsonContentType);
            }

            var tags = project.PrCon.GetAllITags(project.objId, project.objId, false, false) ?? new List<ITag>();
            var payload = tags
                .Where(tag => tag != null)
                .Select(tag => new
                {
                    Name = tag.Name,
                    Value = tag.Value,
                    value = tag.Value,
                    FormattedValue = SafeGetFormattedValue(tag),
                    TypeData = tag.TypeData_.ToString()
                })
                .ToArray();

            return Results.Json(payload);
        }

        private static string SafeGetFormattedValue(ITag? tag)
        {
            if (tag == null)
            {
                return string.Empty;
            }

            try
            {
                return tag.GetFormatedValue();
            }
            catch
            {
                return tag.Value?.ToString() ?? string.Empty;
            }
        }
    }
}
