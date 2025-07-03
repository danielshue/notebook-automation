// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for managing OneDrive authentication and operations.
/// </summary>

internal class OneDriveCommands
{
    private readonly ILogger<OneDriveCommands> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveCommands"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information and errors.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public OneDriveCommands(ILogger<OneDriveCommands> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers OneDrive-related commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    /// <param name="configOption">The global config file option.</param>
    /// <param name="debugOption">The global debug option.</param>
    /// <param name="verboseOption">The global verbose output option.</param>
    /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
    public void Register(
        RootCommand rootCommand,
        Option<string> configOption,
        Option<bool> debugOption,
        Option<bool> verboseOption,
        Option<bool> dryRunOption)
    {
        var refreshTokenCommand = new Command("refresh-token", "Refresh the OneDrive authentication token");

        /// <summary>
        /// Refreshes the OneDrive authentication token.
        /// </summary>
        refreshTokenCommand.SetHandler(async () =>
        {
            try
            {
                var oneDriveService = _serviceProvider.GetService<IOneDriveService>();
                if (oneDriveService == null)
                {
                    _logger.LogError("OneDriveService is not available.");
                    Console.WriteLine("Error: OneDriveService is not available.");
                    return;
                }

                Console.WriteLine("Refreshing OneDrive authentication token...");
                await oneDriveService.RefreshAuthenticationAsync();
                Console.WriteLine("OneDrive authentication token refreshed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh OneDrive authentication token.");
                Console.WriteLine($"Error: {ex.Message}");
            }
        });

        rootCommand.AddCommand(refreshTokenCommand);
    }
}
