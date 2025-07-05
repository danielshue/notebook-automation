// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides configuration discovery, loading, and validation functionality.
/// </summary>
/// <remarks>
/// This class implements the configuration management strategy, handling discovery across multiple
/// locations, environment variable support, and comprehensive error handling with logging.
/// </remarks>
public class ConfigManager(
    IFileSystemWrapper fileSystem,
    IEnvironmentWrapper environment,
    ILogger<ConfigManager> logger) : IConfigManager
{
    private readonly IFileSystemWrapper _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly IEnvironmentWrapper _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly ILogger<ConfigManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Discovers and loads the application configuration based on the specified options.
    /// </summary>
    /// <param name="options">The configuration discovery options.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the configuration manager result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public async Task<ConfigManagerResult> LoadConfigurationAsync(ConfigDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            if (options.Debug)
            {
                _logger.LogDebug("Starting configuration discovery process");
            }

            var configPath = await DiscoverConfigurationPathAsync(options);
            if (configPath == null)
            {
                var errorMessage = "No configuration file found. Please create a config.json file or specify one with --config.";
                _logger.LogError(errorMessage);
                return ConfigManagerResult.Failure(errorMessage);
            }

            if (options.Debug)
            {
                _logger.LogDebug("Loading configuration from: {ConfigPath}", configPath);
            }

            var configuration = await LoadConfigurationFromFileAsync(configPath);
            _logger.LogDebug("Configuration loaded successfully from: {ConfigPath}", configPath);

            return ConfigManagerResult.Success(configuration, configPath);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to load configuration: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            return ConfigManagerResult.Failure(errorMessage, ex);
        }
    }

    /// <summary>
    /// Validates that a configuration file exists and is readable.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the configuration is valid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configPath"/> is null or empty.</exception>
    public async Task<bool> ValidateConfigurationAsync(string configPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(configPath);

        try
        {
            if (!_fileSystem.FileExists(configPath))
            {
                return false;
            }

            // Try to parse the JSON to ensure it's valid
            var content = await _fileSystem.ReadAllTextAsync(configPath);
            using var document = JsonDocument.Parse(content);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Configuration validation failed for: {ConfigPath}", configPath);
            return false;
        }
    }

    /// <summary>
    /// Discovers the configuration file path based on the discovery strategy.
    /// </summary>
    /// <param name="options">The configuration discovery options.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the discovered path or null.</returns>
    /// <remarks>
    /// The discovery follows this priority order:
    /// <list type="number">
    ///   <item><description>CLI option (--config) - Highest priority, if invalid returns null immediately</description></item>
    ///   <item><description>Environment variable (NOTEBOOKAUTOMATION_CONFIG) - If set but invalid, continues to next option</description></item>
    ///   <item><description>Current working directory (./config.json)</description></item>
    ///   <item><description>Executable directory ({exe-dir}/config.json)</description></item>
    ///   <item><description>Executable config subdirectory ({exe-dir}/config/config.json)</description></item>
    ///   <item><description>User home directory ({user-home}/notebook-automation/config.json) - Cross-platform using Environment.SpecialFolder.UserProfile</description></item>
    /// </list>
    /// Each location is validated for file existence and JSON format before being accepted.
    /// If no valid configuration file is found in any location, returns null.
    /// </remarks>
    private async Task<string?> DiscoverConfigurationPathAsync(ConfigDiscoveryOptions options)
    {
        // 1. CLI option takes highest priority
        if (!string.IsNullOrEmpty(options.ConfigPath))
        {
            if (options.Debug)
            {
                _logger.LogDebug("Checking CLI-specified config path: {ConfigPath}", options.ConfigPath);
            }

            if (await ValidateConfigurationAsync(options.ConfigPath))
            {
                if (options.Debug)
                {
                    _logger.LogDebug("Using CLI-specified configuration: {ConfigPath}", options.ConfigPath);
                }
                return options.ConfigPath;
            }

            _logger.LogWarning("CLI-specified configuration file not found or invalid: {ConfigPath}", options.ConfigPath);
            return null;
        }

        // 2. Environment variable
        var envConfigPath = _environment.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG");
        if (!string.IsNullOrEmpty(envConfigPath))
        {
            if (options.Debug)
            {
                _logger.LogDebug("Checking environment variable config path: {ConfigPath}", envConfigPath);
            }

            if (await ValidateConfigurationAsync(envConfigPath))
            {
                if (options.Debug)
                {
                    _logger.LogDebug("Using environment variable configuration: {ConfigPath}", envConfigPath);
                }
                return envConfigPath;
            }

            _logger.LogWarning("Environment variable configuration file not found or invalid: {ConfigPath}", envConfigPath);
        }

        // 3. Current working directory
        var workingDirConfig = _fileSystem.CombinePath(options.WorkingDirectory, "config.json");
        if (options.Debug)
        {
            _logger.LogDebug("Checking current working directory: {ConfigPath}", workingDirConfig);
        }

        if (await ValidateConfigurationAsync(workingDirConfig))
        {
            if (options.Debug)
            {
                _logger.LogDebug("Using working directory configuration: {ConfigPath}", workingDirConfig);
            }
            return workingDirConfig;
        }

        // 4. Executable directory
        var executableConfig = _fileSystem.CombinePath(options.ExecutableDirectory, "config.json");
        if (options.Debug)
        {
            _logger.LogDebug("Checking executable directory: {ConfigPath}", executableConfig);
        }

        if (await ValidateConfigurationAsync(executableConfig))
        {
            if (options.Debug)
            {
                _logger.LogDebug("Using executable directory configuration: {ConfigPath}", executableConfig);
            }
            return executableConfig;
        }

        // 5. Executable/config subdirectory
        var executableSubdirConfig = _fileSystem.CombinePath(options.ExecutableDirectory, "config", "config.json");
        if (options.Debug)
        {
            _logger.LogDebug("Checking executable config subdirectory: {ConfigPath}", executableSubdirConfig);
        }

        if (await ValidateConfigurationAsync(executableSubdirConfig))
        {
            if (options.Debug)
            {
                _logger.LogDebug("Using executable config subdirectory configuration: {ConfigPath}", executableSubdirConfig);
            }
            return executableSubdirConfig;
        }

        // 6. User home directory - notebook-automation subdirectory
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(homeDirectory))
        {
            var homeDirConfig = _fileSystem.CombinePath(homeDirectory, "notebook-automation", "config.json");
            if (options.Debug)
            {
                _logger.LogDebug($"Checking user home notebook-automation directory: {homeDirConfig}");
            }

            if (await ValidateConfigurationAsync(homeDirConfig))
            {
                if (options.Debug)
                {
                    _logger.LogDebug($"Using home notebook-automation directory configuration: {homeDirConfig}");
                }
                return homeDirConfig;
            }
        }

        if (options.Debug)
        {
            _logger.LogDebug("No configuration file found in any of the discovery locations");
        }

        return null;
    }

    /// <summary>
    /// Loads configuration from the specified file path.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded configuration.</returns>
    private async Task<AppConfig> LoadConfigurationFromFileAsync(string configPath)
    {
        var content = await _fileSystem.ReadAllTextAsync(configPath);
        var configuration = JsonSerializer.Deserialize<AppConfig>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        });

        if (configuration == null)
        {
            throw new ConfigurationException($"Failed to deserialize configuration from: {configPath}");
        }

        return configuration;
    }
}
