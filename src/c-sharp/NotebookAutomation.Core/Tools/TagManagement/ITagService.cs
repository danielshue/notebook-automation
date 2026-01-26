// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.TagManagement;

/// <summary>
/// Service interface for tag management operations in the Obsidian vault.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ITagService"/> provides a high-level API for tag management operations
/// designed for Copilot tool integration. It wraps the underlying <see cref="TagProcessor"/>
/// and handles path resolution, dry-run mode, and result formatting.
/// </para>
/// <para>
/// Path resolution rules:
/// <list type="bullet">
/// <item><description>Relative paths (e.g., "MBA/Notes") are resolved against the vault root</description></item>
/// <item><description>Absolute paths are validated to be within the vault</description></item>
/// <item><description>Null/empty paths default to the vault root</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ITagService
{
    /// <summary>
    /// Adds nested tags to markdown files based on frontmatter fields.
    /// </summary>
    /// <param name="path">
    /// Path to process. Can be:
    /// <list type="bullet">
    /// <item><description>Relative path: "MBA/Courses/Finance" → resolved against vault root</description></item>
    /// <item><description>Absolute path: "D:\Vault\MBA\Courses" → used as-is</description></item>
    /// <item><description>Single file: "MBA/Notes/lecture1.md"</description></item>
    /// </list>
    /// </param>
    /// <param name="dryRun">If true, simulates changes without modifying files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing statistics and any errors.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// tag_add_nested("MBA/Courses/Finance")
    /// tag_add_nested("D:\Vault\MBA\Courses\Finance")
    /// tag_add_nested("MBA/Notes/lecture1.md", dryRun: true)
    /// </code>
    /// </example>
    Task<TagOperationResult> AddNestedTagsAsync(string path, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consolidates duplicate and similar tags across files.
    /// </summary>
    /// <param name="path">Path to process, or null for entire vault.</param>
    /// <param name="dryRun">If true, simulates changes without modifying files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing statistics and any errors.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// tag_consolidate()                    // Entire vault
    /// tag_consolidate("MBA/Courses")       // Specific folder
    /// tag_consolidate(dryRun: true)        // Preview changes
    /// </code>
    /// </example>
    Task<TagOperationResult> ConsolidateTagsAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restructures tags according to the configured hierarchy.
    /// </summary>
    /// <param name="path">Path to process, or null for entire vault.</param>
    /// <param name="dryRun">If true, simulates changes without modifying files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing statistics and any errors.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// tag_restructure()                    // Entire vault
    /// tag_restructure("MBA/Courses")       // Specific folder
    /// </code>
    /// </example>
    Task<TagOperationResult> RestructureTagsAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates or adds a frontmatter key-value pair in markdown files.
    /// </summary>
    /// <param name="path">Path to file or directory to process.</param>
    /// <param name="key">Frontmatter key to update (e.g., "status", "tags", "author").</param>
    /// <param name="value">New value for the key.</param>
    /// <param name="dryRun">If true, simulates changes without modifying files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing statistics and any errors.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// tag_update_frontmatter("MBA/Notes/lecture1.md", "status", "reviewed")
    /// tag_update_frontmatter("MBA/Courses", "course", "Finance 101")
    /// tag_update_frontmatter("MBA/Notes", "author", "John Doe", dryRun: true)
    /// </code>
    /// </example>
    Task<TagOperationResult> UpdateFrontmatterAsync(string path, string key, string value, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Diagnoses YAML frontmatter issues in markdown files.
    /// </summary>
    /// <param name="path">Path to scan, or null for entire vault.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Diagnosis result containing files with issues and suggested fixes.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// tag_diagnose_yaml()                  // Scan entire vault
    /// tag_diagnose_yaml("MBA/Notes")       // Scan specific folder
    /// </code>
    /// </example>
    Task<YamlDiagnosisResult> DiagnoseYamlAsync(string? path = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks metadata consistency against the schema.
    /// </summary>
    /// <param name="path">Path to check, or null for entire vault.</param>
    /// <param name="dryRun">If true, reports issues without fixing them.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing statistics and any inconsistencies found.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// tag_metadata_check()                 // Check entire vault
    /// tag_metadata_check("MBA/Courses")    // Check specific folder
    /// tag_metadata_check(dryRun: true)     // Report only, don't fix
    /// </code>
    /// </example>
    Task<TagOperationResult> CheckMetadataAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes tag information from index files (_index.md, index.md).
    /// </summary>
    /// <param name="path">Path to process, or null for entire vault.</param>
    /// <param name="dryRun">If true, simulates changes without modifying files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing statistics and any errors.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// tag_clean_index()                    // Clean all index files in vault
    /// tag_clean_index("MBA/Courses")       // Clean index files in folder
    /// </code>
    /// </example>
    Task<TagOperationResult> CleanIndexFilesAsync(string? path = null, bool dryRun = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a tag operation.
/// </summary>
public record TagOperationResult
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable summary of the operation.</summary>
    public required string Message { get; init; }

    /// <summary>Number of files processed.</summary>
    public int FilesProcessed { get; init; }

    /// <summary>Number of files modified.</summary>
    public int FilesModified { get; init; }

    /// <summary>Number of tags added.</summary>
    public int TagsAdded { get; init; }

    /// <summary>Number of files with errors.</summary>
    public int FilesWithErrors { get; init; }

    /// <summary>Whether this was a dry run (no actual changes made).</summary>
    public bool DryRun { get; init; }

    /// <summary>Error message if the operation failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>List of files that had errors, with error messages.</summary>
    public IReadOnlyList<string>? ErrorFiles { get; init; }
}

/// <summary>
/// Result of YAML frontmatter diagnosis.
/// </summary>
public record YamlDiagnosisResult
{
    /// <summary>Whether the scan completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable summary.</summary>
    public required string Message { get; init; }

    /// <summary>Number of files scanned.</summary>
    public int FilesScanned { get; init; }

    /// <summary>Number of files with YAML issues.</summary>
    public int FilesWithIssues { get; init; }

    /// <summary>List of issues found.</summary>
    public IReadOnlyList<YamlIssue>? Issues { get; init; }
}

/// <summary>
/// A YAML frontmatter issue found during diagnosis.
/// </summary>
public record YamlIssue
{
    /// <summary>Path to the file with the issue.</summary>
    public required string FilePath { get; init; }

    /// <summary>Line number where the issue was found (if available).</summary>
    public int? LineNumber { get; init; }

    /// <summary>Description of the issue.</summary>
    public required string Description { get; init; }

    /// <summary>Suggested fix for the issue.</summary>
    public string? SuggestedFix { get; init; }
}
