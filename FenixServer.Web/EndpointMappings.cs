using ProjectDataLib;
using System.Collections.Concurrent;
using System.Globalization;

namespace FenixServer.Web
{
    public static class EndpointMappings
    {
        internal const string PlainTextContentType = "text/plain";
        internal const string JsonContentType = "application/json";
        internal const string ErrorResponse = "Error";
        internal const string AllKey = "all";
        internal const string BufforName = "Buffor";
        internal const int MaxEventsCount = 1000;

        // API Versioning
        internal const string ApiV1Prefix = "/api/v1";
        internal const string ApiCurrentVersion = "1.0";
        internal const int ApiMajorVersion = 1;
        internal const int ApiMinorVersion = 0;

        internal static int ProbeCounter = 100;
        internal static readonly object GraphSync = new object();
        internal static readonly ConcurrentDictionary<string, List<object[]>> GraphData = new();
        internal static readonly ConcurrentQueue<EventEntry> Events = new();

        internal readonly record struct EndpointRequest(string ObjectKey, string NameKey, string ParamKey, string ValueKey);
        internal readonly record struct EventEntry(DateTimeOffset Tm, string Mess);

        public static void PublishEvent(string? message, DateTimeOffset? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Events.Enqueue(new EventEntry(timestamp ?? DateTimeOffset.Now, message.Trim()));
            while (Events.Count > MaxEventsCount && Events.TryDequeue(out _)) { }
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
            app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
            app.MapGet("/api/version", () => Results.Ok(new
            {
                version = ApiCurrentVersion,
                majorVersion = ApiMajorVersion,
                minorVersion = ApiMinorVersion
            }));
            LegacyEndpointMappings.MapLegacyEndpoints(app);
            ApiEndpointMappings.MapApiEndpoints(app);
            ApiEndpointMappings.MapApiV1Endpoints(app);
        }

        internal static IResult ErrorResult()
            => Results.Text(ErrorResponse, PlainTextContentType);

        internal static Project GetProjectFromServices(IServiceProvider services)
            => services.GetRequiredService<Project>()
               ?? throw new InvalidOperationException("Project not properly initialized");

        internal static bool TryFindTag(Project project, string tagName, out ITag? tag)
        {
            tag = null;
            if (project?.PrCon == null || string.IsNullOrWhiteSpace(tagName))
                return false;

            var allTags = project.PrCon.GetAllITags(project.objId, project.objId, false, false);
            if (allTags == null || allTags.Count == 0)
                return false;

            var normalized = tagName.Trim();
            tag = allTags.FirstOrDefault(t =>
                t != null &&
                !string.IsNullOrWhiteSpace(t.Name) &&
                string.Equals(t.Name.Trim(), normalized, StringComparison.OrdinalIgnoreCase));

            return tag != null;
        }

        internal static string SafeGetFormattedValue(ITag? tag)
        {
            if (tag == null) return string.Empty;
            try { return tag.GetFormatedValue(); }
            catch { return tag.Value?.ToString() ?? string.Empty; }
        }

        internal static object BuildGraphResponse(Project project)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var tags = project.PrCon.GetAllITags(project.objId, project.objId, false, true) ?? new List<ITag>();

            lock (GraphSync)
            {
                foreach (var tag in tags)
                    AppendGraphPoint(tag, timestamp);

                return BuildGraphSeriesResponse(tags);
            }
        }

        internal static object[] BuildEventsResponse()
        {
            return Events
                .Select(e => new
                {
                    Tm = e.Tm,
                    Mess = e.Mess,
                    frDateTime = e.Tm.LocalDateTime.ToString("d.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
                })
                .Cast<object>()
                .ToArray();
        }

        private static void AppendGraphPoint(ITag? tag, long timestamp)
        {
            try
            {
                if (tag == null) return;
                var key = tag.Name ?? string.Empty;
                var series = GraphData.GetOrAdd(key, _ => new List<object[]>());

                if (tag.TypeData_ == TypeData.BIT)
                    AppendBitGraphPoint(series, tag, timestamp);
                else
                    AppendNumericGraphPoint(series, tag, timestamp);

                TrimSeries(series);
            }
            catch { }
        }

        private static void AppendBitGraphPoint(List<object[]> series, ITag tag, long timestamp)
        {
            var currentPoint = SafeGetBitPoint(tag.Value);
            if (series.Count < 1) { series.Add(new object[] { timestamp, currentPoint }); return; }
            var lastPoint = SafeGetDouble(series[^1][1]);
            if (!lastPoint.Equals(currentPoint))
                series.Add(new object[] { timestamp, lastPoint });
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
                    point = 0d;
            }
            series.Add(new object[] { timestamp, point });
        }

        private static object[] BuildGraphSeriesResponse(List<ITag> tags)
        {
            return tags
                .Where(t => t != null)
                .Select(t => new
                {
                    label = t.Name,
                    data = GraphData.TryGetValue(t.Name ?? string.Empty, out var series)
                        ? series
                        : new List<object[]>()
                })
                .Cast<object>()
                .ToArray();
        }

        private static void TrimSeries(List<object[]> series)
        {
            while (series.Count > 0)
            {
                var min = Convert.ToInt64(series[0][0]);
                var diff = DateTime.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(min).UtcDateTime;
                if (diff <= TimeSpan.FromSeconds(ProbeCounter)) break;
                series.RemoveAt(0);
            }
        }

        internal static double SafeGetBitPoint(object? value)
        {
            if (value is bool b) return b ? 1d : 0d;
            if (value is string s)
            {
                if (bool.TryParse(s, out var pb)) return pb ? 1d : 0d;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var pn)) return pn != 0d ? 1d : 0d;
            }
            return SafeGetDouble(value) != 0d ? 1d : 0d;
        }

        internal static double SafeGetDouble(object? value)
        {
            if (value == null) return 0d;
            if (value is double d) return d;
            if (value is IConvertible)
            {
                try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
                catch { }
            }
            return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0d;
        }
    }
}
