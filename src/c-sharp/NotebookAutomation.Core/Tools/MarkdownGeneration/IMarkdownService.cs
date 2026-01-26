// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.MarkdownGeneration;

/// <summary>
/// Service interface for markdown generation operations exposed to Copilot tools.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IMarkdownService"/> provides a high-level API for converting HTML, EPUB,
/// and other document formats to markdown notes. It wraps <see cref="MarkdownNoteBatchProcessor"/>.
/// </para>
/// <para>
/// Path resolution follows these rules:
/// <list type="bullet">
/// <item><description>Relative paths are resolved against the OneDrive resources root</description></item>
/// <item><description>Absolute paths are validated to be within the OneDrive root</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IMarkdownService
{
    /// <summary>
    /// Converts HTML, EPUB, or other document files to markdown notes.
    /// </summary>
    /// <param name="inputPath">
    /// Path to file or directory. Can be relative to OneDrive root (e.g., "Documents/Books")
    /// or absolute path within the OneDrive folder.
    /// </param>
    /// <param name="outputPath">
    /// Optional output directory for generated notes. Defaults to vault resources path.
    /// </param>
    /// <param name="dryRun">If true, simulates processing without writing files.</param>
    /// <param name="noSummary">If true, skips AI summary generation (recommended for large documents).</param>
    /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="MarkdownOperationResult"/> with processing statistics.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// markdown_generate("Documents/Books")                    // Convert all HTML/EPUB in Books folder
    /// markdown_generate("Documents/Books", dryRun: true)      // Preview what would be processed
    /// markdown_generate("D:\OneDrive\Documents\book.epub")    // Convert single EPUB file
    /// markdown_generate("Documents/Articles", noSummary: true)  // Convert without AI summary
    /// </code>
    /// </example>
    Task<MarkdownOperationResult> GenerateAsync(
        string inputPath,
        string? outputPath = null,
        bool dryRun = false,
        bool noSummary = true,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a markdown generation operation.
/// </summary>
public record MarkdownOperationResult
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable summary of the operation.</summary>
    public required string Message { get; init; }

    /// <summary>Number of source files found.</summary>
    public int FilesFound { get; init; }

    /// <summary>Number of notes successfully created.</summary>
    public int NotesCreated { get; init; }

    /// <summary>Number of files that failed to process.</summary>
    public int Failed { get; init; }

    /// <summary>Whether this was a dry run.</summary>
    public bool DryRun { get; init; }

    /// <summary>Total processing time.</summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>Total tokens used for AI summaries.</summary>
    public int TotalTokens { get; init; }

    /// <summary>Error message if operation failed.</summary>
    public string? ErrorMessage { get; init; }
}
