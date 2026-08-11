using ProjectDataLib;

namespace FenixServer.Api
{
    /// <summary>
    /// Extension methods for integrating ASP.NET Core web host into WinForms application.
    /// </summary>
    public static class WebHostExtensions
    {
        private static CancellationTokenSource? _hostCts;

        /// <summary>
        /// Initializes and starts the ASP.NET Core web server.
        /// </summary>
        public static async Task InitializeAndStartWebHostAsync(
            Project project,
            ProjectContainer projectContainer)
        {
            _hostCts = new CancellationTokenSource();

            try
            {
                // Configure the web host with project dependencies
                Program.ConfigureWebHost(project, projectContainer);

                // Start the host asynchronously
                await Program.StartAsync(_hostCts.Token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize web host: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Gracefully stops the ASP.NET Core web server.
        /// </summary>
        public static async Task StopWebHostAsync()
        {
            try
            {
                _hostCts?.Cancel();
                await Program.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping web host: {ex}");
            }
            finally
            {
                _hostCts?.Dispose();
                _hostCts = null;
            }
        }
    }
}
