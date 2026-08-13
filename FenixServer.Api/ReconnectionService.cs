using ProjectDataLib;
using System.Collections.Concurrent;

namespace FenixServer.Api
{
    /// <summary>
    /// Background service that listens for driver errors and attempts
    /// to reconnect any broken Modbus/PLC connections.
    /// </summary>
    public class ReconnectionService : BackgroundService
    {
        private readonly Project _project;
        private readonly ProjectContainer _container;
        private readonly ILogger<ReconnectionService> _logger;

        /// <summary>
        /// Connections that have reported an error and need reconnection.
        /// Key: connection.objId, Value: connection.connectionName
        /// </summary>
        private readonly ConcurrentDictionary<Guid, string> _deadConnections = new();

        /// <summary>
        /// Delay between reconnection attempts (in seconds).
        /// </summary>
        private const int RetryIntervalSeconds = 5;

        public ReconnectionService(
            Project project,
            ProjectContainer container,
            ILogger<ReconnectionService> logger)
        {
            _project = project;
            _container = container;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Subscribe to error events for all existing connections
            SubscribeToDriverErrors();

            _logger.LogInformation(
                "ReconnectionService started. Retrying every {Interval}s on error.",
                RetryIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(RetryIntervalSeconds), stoppingToken);
                    await ReconnectDeadConnectionsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during reconnection check.");
                }
            }

            _logger.LogInformation("ReconnectionService stopped.");
        }

        private void SubscribeToDriverErrors()
        {
            foreach (var connection in _project.connectionList)
            {
                var driver = connection.Idrv;
                if (driver == null)
                    continue;

                // Unsubscribe first to avoid duplicate registrations on restart
                driver.error -= OnDriverError;
                driver.error += OnDriverError;
            }
        }

        private void OnDriverError(object? sender, EventArgs e)
        {
            // Find which connection this driver belongs to
            var driverId = Guid.Empty;
            if (sender is IDriverModel drv)
                driverId = drv.ObjId;

            var connection = _project.connectionList
                .FirstOrDefault(c => c.Idrv?.ObjId == driverId);

            if (connection == null)
                return;

            // Mark as dead – will be retried in the main loop
            if (_deadConnections.TryAdd(connection.objId, connection.connectionName))
            {
                _logger.LogWarning(
                    "[{Name}] Error reported by driver. Connection marked for reconnection.",
                    connection.connectionName);
            }
        }

        private async Task ReconnectDeadConnectionsAsync(CancellationToken cancellationToken)
        {
            // Re-subscribe on every pass so drivers that were created or replaced
            // after startup (e.g. after a DriverName change) are also monitored.
            SubscribeToDriverErrors();

            // Proactively mark connections whose driver is not running. This covers
            // the case where the PLC/device was unreachable when the server started:
            // the startup error event is raised before this service subscribes, so it
            // would otherwise be missed and no retry would ever happen.
            DetectDeadConnections();

            if (_deadConnections.IsEmpty)
                return;

            // Snapshot to avoid modifying collection during enumeration
            var snapshot = _deadConnections.ToArray();

            foreach (var (connId, connName) in snapshot)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var connection = _project.connectionList
                    .FirstOrDefault(c => c.objId == connId);

                if (connection == null)
                {
                    _deadConnections.TryRemove(connId, out _);
                    continue;
                }

                var driver = connection.Idrv;
                if (driver == null)
                {
                    _deadConnections.TryRemove(connId, out _);
                    continue;
                }

                _logger.LogWarning(
                    "[{Name}] Attempting reconnection...",
                    connName);

                try
                {
                    // Full restart cycle: stop then start
                    driver.deactivateCycle();

                    // Wait for the driver's background worker to drain so activateCycle
                    // is not rejected just because the previous worker was still busy.
                    await WaitForDriverIdleAsync(driver, cancellationToken);

                    var tags = _container.GetAllITagsForDriver(
                        _project.objId,
                        driver.ObjId) ?? new List<ITag>();

                    bool started = driver.activateCycle(tags);

                    if (started)
                    {
                        _logger.LogInformation(
                            "[{Name}] Reconnection successful.",
                            connName);

                        _deadConnections.TryRemove(connId, out _);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[{Name}] Reconnection attempt failed. Will retry in {Interval}s.",
                            connName,
                            RetryIntervalSeconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[{Name}] Reconnection threw an exception. Will retry in {Interval}s.",
                        connName,
                        RetryIntervalSeconds);
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Marks any connection whose driver is not currently running (failed to start,
        /// dropped, or crashed) as dead so it gets retried by the reconnection loop.
        /// The web server starts every connection at startup and has no per-driver
        /// stop endpoint, so a non-alive driver always means the cycle is broken.
        /// </summary>
        private void DetectDeadConnections()
        {
            foreach (var connection in _project.connectionList)
            {
                var driver = connection.Idrv;
                if (driver == null || connection.IsBlocked)
                    continue;

                if (!driver.isAlive &&
                    _deadConnections.TryAdd(connection.objId, connection.connectionName))
                {
                    _logger.LogWarning(
                        "[{Name}] Driver is not running. Connection marked for reconnection.",
                        connection.connectionName);
                }
            }
        }

        /// <summary>
        /// Waits a short, bounded window for the driver's background worker to finish
        /// after deactivateCycle, so the subsequent activateCycle is not rejected
        /// because the worker is still busy. Proceeds anyway after the window.
        /// </summary>
        private async Task WaitForDriverIdleAsync(IDriverModel driver, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow.AddSeconds(4);

            try
            {
                while (driver.isBusy && !cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (Exception)
            {
                // If the driver state cannot be inspected, just proceed with the retry.
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Unsubscribe from events to prevent memory leaks
            foreach (var connection in _project.connectionList)
            {
                var driver = connection.Idrv;
                if (driver != null)
                    driver.error -= OnDriverError;
            }

            await base.StopAsync(cancellationToken);
        }
    }
}