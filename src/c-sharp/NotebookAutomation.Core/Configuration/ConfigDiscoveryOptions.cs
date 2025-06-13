// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents options for configuration discovery and loading.
/// </summary>
/// <remarks>
/// This class encapsulates the parameters needed for the configuration discovery process,
/// including CLI-specified paths, debug settings, and discovery preferences.
/// </remarks>
public class ConfigDiscoveryOptions
{
    /// <summary>
    /// Gets or sets the configuration file path specified via CLI argument.
    /// </summary>
    /// <value>
    /// The path to the configuration file specified by the user, or null if not provided.
    /// </value>
    public string? ConfigPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether debug mode is enabled.
    /// </summary>
    /// <value>
    /// true if debug mode is enabled; otherwise, false.
    /// </value>
    public bool Debug { get; set; }

    /// <summary>
    /// Gets or sets the executable directory path.
    /// </summary>
    /// <value>
    /// The path to the directory containing the executable.
    /// </value>
    public string ExecutableDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current working directory path.
    /// </summary>
    /// <value>
    /// The current working directory path.
    /// </value>
    public string WorkingDirectory { get; set; } = string.Empty;
}
