// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration.Validation;

/// <summary>
/// Represents a single configuration validation issue.
/// </summary>
/// <param name="Severity">The severity of the issue.</param>
/// <param name="Key">The configuration key or path associated with the issue.</param>
/// <param name="Message">A human-readable description of the problem.</param>
/// <param name="Suggestion">Optional remediation guidance.</param>
public sealed record ConfigurationValidationIssue(
    ConfigurationValidationIssueSeverity Severity,
    string Key,
    string Message,
    string? Suggestion = null);
