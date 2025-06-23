// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides methods to set up application configuration with support for various sources,
/// including JSON files, environment variables, and user secrets.
/// </summary>
/// <remarks>
/// This class simplifies the process of building a configuration for the application,
/// ensuring compatibility with different environments (e.g., development, production).
/// It supports optional user secrets and config file paths, making it flexible for
/// various deployment scenarios.
/// </remarks>
public static class ConfigurationSetup
{
    /// <summary>
    /// Discovers the configuration file path using the modern ConfigManager approach.
    /// This method replaces the legacy AppConfig.FindConfigFile() with consistent discovery logic.
    /// </summary>
    /// <param name="explicitConfigPath">Optional explicit configuration path from CLI or other source.</param>
    /// <param name="configFileName">Name of the configuration file to search for. Defaults to "config.json".</param>
    /// <returns>Path to the configuration file if found, otherwise null.</returns>
    /// <remarks>
    /// This method uses the same discovery order as ConfigManager:
    /// 1. CLI option (if provided via explicitConfigPath)
    /// 2. Environment variable (NOTEBOOKAUTOMATION_CONFIG)
    /// 3. Current working directory
    /// 4. Executable directory
    /// 5. Executable config subdirectory
    /// </remarks>
    public static async Task<string?> DiscoverConfigurationFileAsync(string? explicitConfigPath = null, string configFileName = "config.json")
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
    /// Synchronous wrapper for DiscoverConfigurationFileAsync to maintain compatibility with existing code.
    /// </summary>
    /// <param name="explicitConfigPath">Optional explicit configuration path from CLI or other source.</param>
    /// <param name="configFileName">Name of the configuration file to search for. Defaults to "config.json".</param>
    /// <returns>Path to the configuration file if found, otherwise empty string for compatibility.</returns>
    public static string DiscoverConfigurationFile(string? explicitConfigPath = null, string configFileName = "config.json")
    {
        try
        {
            var task = DiscoverConfigurationFileAsync(explicitConfigPath, configFileName);
            task.Wait();
            return task.Result ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }    /// <summary>
         /// Creates a standard configuration with support for config files and user secrets.
         /// </summary>
         /// <param name="environment">The current environment (development, production, etc.)</param>
         /// <param name="userSecretsId">Optional user secrets ID. If null, will attempt to use assembly-defined ID.</param>
         /// <param name="configPath">Optional path to the config file. If null, will search for config.json in standard locations.</param>
         /// <returns>A configured IConfiguration instance.</returns>
    public static IConfiguration BuildConfiguration(
        string environment = "Development",
        string? userSecretsId = null,
        string? configPath = null)
    {
        // Find the config file if not specified
        if (string.IsNullOrEmpty(configPath))
        {
            configPath = DiscoverConfigurationFile();
        }

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());

        // Add JSON configuration if found
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            configurationBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
        }

        // Add environment variables
        configurationBuilder.AddEnvironmentVariables();

        // Add user secrets in development environment
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(userSecretsId))
            {
                configurationBuilder.AddUserSecrets(userSecretsId);
            }
            else
            {
                // Try to use the assembly's user secrets ID
                configurationBuilder.AddUserSecrets<AppConfig>();
            }
        }

        return configurationBuilder.Build();
    }

    /// <summary>
    /// Creates a configuration with user secrets support for the given assembly type.
    /// </summary>
    /// <typeparam name="T">The type from the assembly that has the UserSecretsId attribute.</typeparam>
    /// <param name="environment">The current environment (development, production, etc.)</param>
    /// <param name="configPath">Optional path to the config file. If null, will search for config.json in standard locations.</param>
    /// <returns>A configured IConfiguration instance.</returns>
    public static IConfiguration BuildConfiguration<T>(
        string environment = "Development",
        string? configPath = null)
    where T : class
    {        // Find the config file if not specified
        if (string.IsNullOrEmpty(configPath))
        {
            configPath = DiscoverConfigurationFile();
        }

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());

        // Add JSON configuration if found
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            configurationBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
        }

        // Add environment variables
        configurationBuilder.AddEnvironmentVariables();

        // Add user secrets in development environment
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            configurationBuilder.AddUserSecrets<T>();
        }

        return configurationBuilder.Build();
    }

    /// <summary>
    /// Creates a configuration using the new ConfigManager system with enhanced discovery capabilities.
    /// </summary>
    /// <typeparam name="T">The type from the assembly that has the UserSecretsId attribute.</typeparam>
    /// <param name="environment">The current environment (development, production, etc.)</param>
    /// <param name="configPath">Optional path to the config file. If null, will use ConfigManager for discovery.</param>
    /// <param name="useNewConfigManager">Whether to use the new ConfigManager for discovery (default: true).</param>
    /// <returns>A configured IConfiguration instance.</returns>
    public static IConfiguration BuildConfigurationWithConfigManager<T>(
        string environment = "Development",
        string? configPath = null,
        bool useNewConfigManager = true)
    where T : class
    {
        if (useNewConfigManager)
        {
            // Use the new ConfigManager for discovery
            var fileSystem = new FileSystemWrapper();
            var environmentWrapper = new EnvironmentWrapper();
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ConfigManager>();
            var configManager = new ConfigManager(fileSystem, environmentWrapper, logger); var options = new ConfigDiscoveryOptions
            {
                ConfigPath = configPath,
                Debug = false,
                ExecutableDirectory = environmentWrapper.GetExecutableDirectory(),
                WorkingDirectory = environmentWrapper.GetCurrentDirectory()
            };            // Perform discovery synchronously (wrapping async call)
            var discoveryTask = configManager.LoadConfigurationAsync(options);
            discoveryTask.Wait();
            var discoveryResult = discoveryTask.Result; if (discoveryResult.IsSuccess && !string.IsNullOrEmpty(discoveryResult.ConfigurationPath))
            {
                configPath = discoveryResult.ConfigurationPath;
            }
        }
        else
        {            // Fall back to modern discovery instead of legacy method
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = DiscoverConfigurationFile();
            }
        }

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());

        // Add JSON configuration if found
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            configurationBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
        }

        // Add environment variables
        configurationBuilder.AddEnvironmentVariables();

        // Add user secrets in development environment
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            configurationBuilder.AddUserSecrets<T>();
        }

        return configurationBuilder.Build();
    }
}
