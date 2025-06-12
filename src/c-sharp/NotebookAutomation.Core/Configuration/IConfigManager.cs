// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Defines the contract for configuration management operations.
/// </summary>
/// <remarks>
/// This interface abstracts configuration discovery, loading, and validation operations,
/// enabling testable and maintainable configuration management throughout the application.
/// </remarks>
public interface IConfigManager
{
    /// <summary>
    /// Discovers and loads the application configuration based on the specified options.
    /// </summary>
    /// <param name="options">The configuration discovery options.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the configuration manager result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    Task<ConfigManagerResult> LoadConfigurationAsync(ConfigDiscoveryOptions options);

    /// <summary>
    /// Validates that a configuration file exists and is readable.
    /// </summary>
    /// <param name="configPath">The path to the configuration file.</param>
    /// <returns>A task that represents the asynchronous operation. The task result indicates whether the configuration is valid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="configPath"/> is null or empty.</exception>
    Task<bool> ValidateConfigurationAsync(string configPath);
}