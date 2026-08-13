using ProjectDataLib;
using System.Collections.Concurrent;
using System.Globalization;

namespace FenixServer.Api
{
    /// <summary>
    /// Centralized endpoint mappings for the FenixServer web API.
    /// Provides both modern REST endpoints (/api/) and legacy compatibility endpoints (/Tag/Value, etc.)
    /// </summary>
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

        /// <summary>
        /// Publishes an event to the event log queue
        /// </summary>
        /// <param name="message">The event message to publish</param>
        /// <param name="timestamp">Optional timestamp for the event (defaults to current time)</param>
        public static void PublishEvent(string? message, DateTimeOffset? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Events.Enqueue(new EventEntry(timestamp ?? DateTimeOffset.Now, message.Trim()));
            while (Events.Count > MaxEventsCount && Events.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Maps the root endpoint to provide basic application information
        /// </summary>
        /// <param name="app">The WebApplication instance</param>
        public static void MapRootEndpoint(this WebApplication app)
        {
            app.MapGet("/", () => Results.Ok(new
            {
                app = "FenixServer.Api",
                status = "running",
                health = "/health"
            }));
        }

        /// <summary>
        /// Maps all Fenix endpoints including health, version, legacy and API endpoints
        /// </summary>
        /// <param name="app">The WebApplication instance</param>
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

        /// <summary>
        /// Retrieves the Project instance from service provider
        /// </summary>
        /// <param name="services">Service provider to retrieve project from</param>
        /// <returns>Project instance</returns>
        internal static Project GetProjectFromServices(IServiceProvider services)
            => services.GetRequiredService<Project>()
               ?? throw new InvalidOperationException("Project not properly initialized");

        /// <summary>
        /// Attempts to find a tag by name in the project
        /// </summary>
        /// <param name="project">The project to search</param>
        /// <param name="tagName">Name of the tag to find</param>
        /// <param name="tag">Output parameter for found tag</param>
        /// <returns>True if tag was found, false otherwise</returns>
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

        /// <summary>
        /// Safely gets formatted value from a tag
        /// </summary>
        /// <param name="tag">The tag to get value from</param>
        /// <returns>Formatted value or empty string if tag is null</returns>
        internal static string SafeGetFormattedValue(ITag? tag)
        {
            if (tag == null) return string.Empty;
            try { return tag.GetFormatedValue(); }
            catch { return tag.Value?.ToString() ?? string.Empty; }
        }

        /// <summary>
        /// Builds graph response data for all tags in the project
        /// </summary>
        /// <param name="project">The project containing tags</param>
        /// <returns>Graph series data structure</returns>
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

        /// <summary>
        /// Builds events response data from the event queue
        /// </summary>
        /// <returns>Array of event objects with timestamps and formatted dates</returns>
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

        /// <summary>
        /// Safely converts a value to bit point (0 or 1)
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Double value of 0 or 1</returns>
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

        /// <summary>
        /// Safely converts a value to double
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Double value or 0 if conversion fails</returns>
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