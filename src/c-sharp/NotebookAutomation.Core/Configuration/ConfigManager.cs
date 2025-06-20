// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides configuration discovery, loading, and validation functionality for the Notebook Automation system.
/// </summary>
/// <remarks>
/// <para><strong>Purpose and Role:</strong></para>
/// <para>ConfigManager is a modern, dependency-injected service responsible for discovering, loading, and validating
/// configuration files. It implements a comprehensive configuration management strategy with robust error handling,
/// logging, and multiple discovery locations.</para>
///
/// <para><strong>Key Differences from AppConfig:</strong></para>
/// <list type="bullet">
/// <item><description><strong>Service vs Model</strong>: ConfigManager is a service that loads configurations, AppConfig is the configuration data model</description></item>
/// <item><description><strong>Discovery vs Storage</strong>: ConfigManager discovers and loads config files, AppConfig stores and provides typed access to settings</description></item>
/// <item><description><strong>Dependency Injection</strong>: ConfigManager uses DI with interfaces for testability, AppConfig can work with or without DI</description></item>
/// <item><description><strong>Async Operations</strong>: ConfigManager provides async methods for file operations, AppConfig works synchronously</description></item>
/// <item><description><strong>Error Handling</strong>: ConfigManager returns result objects with detailed error information, AppConfig throws exceptions</description></item>
/// </list>
///
/// <para><strong>Integration with AppConfig:</strong></para>
/// <para>ConfigManager and AppConfig work together in a producer-consumer relationship:</para>
/// <list type="number">
/// <item><description><strong>Configuration Discovery</strong>: ConfigManager discovers config files using multiple strategies and locations</description></item>
/// <item><description><strong>File Loading</strong>: ConfigManager loads and validates JSON configuration files</description></item>
/// <item><description><strong>AppConfig Creation</strong>: ConfigManager deserializes JSON directly into AppConfig instances</description></item>
/// <item><description><strong>Result Packaging</strong>: ConfigManager returns ConfigManagerResult containing the loaded AppConfig and metadata</description></item>
/// </list>
///
/// <para><strong>Typical Usage Pattern:</strong></para>
/// <code>
/// // Modern approach using ConfigManager
/// var configManager = serviceProvider.GetRequiredService&lt;IConfigManager&gt;();
/// var options = new ConfigDiscoveryOptions
/// {
///     WorkingDirectory = Directory.GetCurrentDirectory(),
///     ExecutableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
///     Debug = true
/// };
/// var result = await configManager.LoadConfigurationAsync(options);
///
/// if (result.IsSuccess)
/// {
///     AppConfig appConfig = result.Configuration; // Strongly-typed access
///     string? loggingPath = appConfig.Paths.LoggingDir;
/// }
/// </code>
///
/// <para><strong>Discovery Strategy:</strong></para>
/// <para>ConfigManager implements a prioritized discovery strategy:</para>
/// <list type="number">
/// <item><description>CLI-specified path (highest priority)</description></item>
/// <item><description>NOTEBOOKAUTOMATION_CONFIG environment variable</description></item>
/// <item><description>Current working directory (config.json)</description></item>
/// <item><description>Executable directory (config.json)</description></item>
/// <item><description>Executable/config subdirectory (config.json)</description></item>
/// </list>
///
/// <para><strong>Validation and Error Handling:</strong></para>
/// <para>ConfigManager provides comprehensive validation including JSON syntax checking, file existence verification,
/// and detailed error reporting through ConfigManagerResult objects.</para>
///
/// <para><strong>Legacy Compatibility:</strong></para>
/// <para>While ConfigManager represents the modern approach, it works alongside ConfigurationSetup.DiscoverConfigurationFile()
/// for backward compatibility. ConfigManager is the preferred approach for new code due to its better testability,
/// error handling, and async support.</para>
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
    /// <param name="options">The configuration discovery options specifying search locations and behavior.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the configuration manager result with the loaded AppConfig instance.</returns>
    /// <remarks>
    /// This method implements the complete configuration loading workflow:
    /// <list type="number">
    /// <item><description>Discovers configuration file using the prioritized strategy</description></item>
    /// <item><description>Validates the discovered file exists and contains valid JSON</description></item>
    /// <item><description>Deserializes JSON content directly into an AppConfig instance</description></item>
    /// <item><description>Returns a ConfigManagerResult with success/failure status and detailed error information</description></item>
    /// </list>
    /// The method provides comprehensive logging and error handling, returning detailed failure information
    /// if configuration discovery or loading fails.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <example>
    /// <code>
    /// var options = new ConfigDiscoveryOptions
    /// {
    ///     WorkingDirectory = Directory.GetCurrentDirectory(),
    ///     ExecutableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
    ///     Debug = true
    /// };
    /// var result = await configManager.LoadConfigurationAsync(options);
    ///
    /// if (result.IsSuccess)
    /// {
    ///     AppConfig config = result.Configuration;
    ///     Console.WriteLine($"Loaded config from: {result.ConfigPath}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Failed to load config: {result.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
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
            _logger.LogInformation("Configuration loaded successfully from: {ConfigPath}", configPath);

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
    /// Validates that a configuration file exists and is readable with valid JSON content.
    /// </summary>
    /// <param name="configPath">The path to the configuration file to validate.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the configuration is valid and can be loaded.</returns>
    /// <remarks>
    /// This method performs comprehensive validation including:
    /// <list type="bullet">
    /// <item><description>File existence check using the injected file system wrapper</description></item>
    /// <item><description>JSON syntax validation by attempting to parse the content</description></item>
    /// <item><description>Error logging for validation failures while returning boolean results</description></item>
    /// </list>
    /// The method is used internally during configuration discovery and can be called independently
    /// for validating configuration files before attempting to load them.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configPath"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// bool isValid = await configManager.ValidateConfigurationAsync("config.json");
    /// if (isValid)
    /// {
    ///     var result = await configManager.LoadConfigurationAsync(options);
    /// }
    /// </code>
    /// </example>
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
    /// <param name="options">The configuration discovery options specifying search locations and debug behavior.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the discovered configuration file path or null if no valid configuration is found.</returns>
    /// <remarks>
    /// This private method implements the core configuration discovery algorithm using a prioritized strategy:
    /// <list type="number">
    /// <item><description><strong>CLI Path</strong>: Uses options.ConfigPath if specified (highest priority)</description></item>
    /// <item><description><strong>Environment Variable</strong>: Checks NOTEBOOKAUTOMATION_CONFIG environment variable</description></item>
    /// <item><description><strong>Working Directory</strong>: Looks for config.json in options.WorkingDirectory</description></item>
    /// <item><description><strong>Executable Directory</strong>: Searches options.ExecutableDirectory for config.json</description></item>
    /// <item><description><strong>Executable Config Subdirectory</strong>: Checks ExecutableDirectory/config/config.json</description></item>
    /// </list>
    /// Each location is validated using ValidateConfigurationAsync before being accepted. The method provides
    /// detailed debug logging when options.Debug is enabled, helping with configuration troubleshooting.
    /// </remarks>
    private async Task<string?> DiscoverConfigurationPathAsync(ConfigDiscoveryOptions options)
    {        // 1. CLI option takes highest priority
        if (!string.IsNullOrEmpty(options.ConfigPath))
        {
            if (options.Debug)
            {
                _logger.LogDebug("Checking CLI-specified config path: {ConfigPath}", options.ConfigPath);
            }

            // For explicitly specified config paths, always attempt to load them
            // even if validation fails - let the loading method handle the error
            if (_fileSystem.FileExists(options.ConfigPath))
            {
                if (options.Debug)
                {
                    _logger.LogDebug("Using CLI-specified configuration: {ConfigPath}", options.ConfigPath);
                }
                return options.ConfigPath;
            }

            _logger.LogWarning("CLI-specified configuration file not found: {ConfigPath}", options.ConfigPath);
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

        if (options.Debug)
        {
            _logger.LogDebug("No configuration file found in any of the discovery locations");
        }

        return null;
    }

    /// <summary>
    /// Loads configuration from the specified file path and deserializes it into an AppConfig instance.
    /// </summary>
    /// <param name="configPath">The path to the configuration file to load.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded and deserialized AppConfig instance.</returns>
    /// <remarks>
    /// This private method handles the actual file loading and JSON deserialization process:
    /// <list type="bullet">
    /// <item><description>Reads the file content asynchronously using the injected file system wrapper</description></item>
    /// <item><description>Deserializes JSON content directly into an AppConfig instance using System.Text.Json</description></item>
    /// <item><description>Uses case-insensitive property matching and allows trailing commas for flexibility</description></item>
    /// <item><description>Throws ConfigurationException if deserialization fails or returns null</description></item>
    /// </list>
    /// The resulting AppConfig instance contains all the strongly-typed configuration settings and can be used
    /// immediately for accessing configuration values through its properties and methods.
    /// </remarks>
    /// <exception cref="ConfigurationException">Thrown when JSON deserialization fails or returns null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON content is malformed.</exception>
    /// <exception cref="IOException">Thrown when file reading fails.</exception>
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
