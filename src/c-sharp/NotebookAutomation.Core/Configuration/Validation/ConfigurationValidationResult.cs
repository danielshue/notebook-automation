// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration.Validation;

/// <summary>
/// Represents the result of executing configuration validation checks.
/// </summary>
public sealed class ConfigurationValidationResult
{
    private readonly IReadOnlyCollection<ConfigurationValidationIssue> _issues;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationResult"/> class.
    /// </summary>
    /// <param name="issues">The validation issues that were discovered.</param>
    /// <param name="configPath">The configuration file path that was evaluated, if known.</param>
    public ConfigurationValidationResult(
        IReadOnlyCollection<ConfigurationValidationIssue> issues,
        string? configPath)
    {
        _issues = issues ?? throw new ArgumentNullException(nameof(issues));
        ConfigurationPath = configPath;
    }

    /// <summary>
    /// Gets the configuration path that was validated, if available.
    /// </summary>
    public string? ConfigurationPath { get; }

    /// <summary>
    /// Gets a value indicating whether the configuration passed validation without errors.
    /// </summary>
    public bool IsValid => !_issues.Any(issue => issue.Severity == ConfigurationValidationIssueSeverity.Error);

    /// <summary>
    /// Gets a value indicating whether the result contains warnings.
    /// </summary>
    public bool HasWarnings => _issues.Any(issue => issue.Severity == ConfigurationValidationIssueSeverity.Warning);

    /// <summary>
    /// Gets the collection of validation issues.
    /// </summary>
    public IReadOnlyCollection<ConfigurationValidationIssue> Issues => _issues;

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <param name="configPath">The configuration file path that was evaluated.</param>
    /// <returns>A <see cref="ConfigurationValidationResult"/> representing success.</returns>
    public static ConfigurationValidationResult Success(string? configPath) =>
        new([], configPath);

    /// <summary>
    /// Creates a validation result with issues.
    /// </summary>
    /// <param name="issues">The validation issues that were discovered.</param>
    /// <param name="configPath">The configuration file path that was evaluated.</param>
    /// <returns>A <see cref="ConfigurationValidationResult"/> representing validation issues.</returns>
    public static ConfigurationValidationResult FromIssues(
        IEnumerable<ConfigurationValidationIssue> issues,
        string? configPath)
    {
        ArgumentNullException.ThrowIfNull(issues);
        var issueList = issues.ToList();
        return new(issueList, configPath);
    }
}
