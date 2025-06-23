// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Configuration;

/// <summary>
/// Service responsible for discovering and locating configuration files.
/// </summary>
/// <remarks>
/// This service handles configuration file discovery using the same logic as ConfigManager
/// for consistent behavior across the application.
/// </remarks>
internal class ConfigurationDiscoveryService
{
    /// <summary>
    /// Discovers the configuration file path using the same logic as ConfigManager for consistent display.
    /// </summary>
    /// <param name="explicitConfigPath">Explicitly provided config path via CLI.</param>
    /// <returns>Path to the configuration file if found, otherwise null.</returns>
    public async Task<string?> DiscoverConfigurationForDisplayAsync(string? explicitConfigPath)
    {
        var fileSystem = new FileSystemWrapper();
        var environment = new EnvironmentWrapper();
        var logger = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning))
            .CreateLogger<ConfigManager>();

        var configManager = new ConfigManager(fileSystem, environment, logger);

        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = explicitConfigPath,
            Debug = false,
            ExecutableDirectory = environment.GetExecutableDirectory(),
            WorkingDirectory = environment.GetCurrentDirectory()
        };

        var result = await configManager.LoadConfigurationAsync(options);
        return result.IsSuccess ? result.ConfigurationPath : null;
    }


    /// <summary>
    /// Parses the configuration path from command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Configuration path if found, otherwise null.</returns>
    public string? ParseConfigPathFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--config" || args[i] == "-c")
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
