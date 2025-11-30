// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration.Validation;

/// <summary>
/// Represents the severity of a configuration validation issue.
/// </summary>
public enum ConfigurationValidationIssueSeverity
{
    /// <summary>
    /// Indicates a configuration error that must be resolved before continuing.
    /// </summary>
    Error,

    /// <summary>
    /// Indicates a configuration warning that should be reviewed but does not block execution.
    /// </summary>
    Warning
}
