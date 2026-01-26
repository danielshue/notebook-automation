// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.PdfProcessing;

/// <summary>
/// Service interface for PDF processing operations exposed to Copilot tools.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IPdfService"/> provides a high-level API for converting PDF files
/// to markdown notes. It wraps <see cref="PdfNoteBatchProcessor"/>.
/// </para>
/// <para>
/// Path resolution follows these rules:
/// <list type="bullet">
/// <item><description>Relative paths are resolved against the OneDrive resources root</description></item>
/// <item><description>Absolute paths are validated to be within the OneDrive root</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IPdfService
{
    /// <summary>
    /// Converts PDF files to markdown notes, extracting text and optionally generating AI summaries.
    /// </summary>
    /// <param name="inputPath">
    /// Path to PDF file or directory. Can be relative to OneDrive root (e.g., "Documents/Papers")
    /// or absolute path within the OneDrive folder.
    /// </param>
    /// <param name="outputPath">
    /// Optional output directory for generated notes. Defaults to vault resources path.
    /// </param>
    /// <param name="dryRun">If true, simulates processing without writing files.</param>
    /// <param name="noSummary">If true, skips AI summary generation.</param>
    /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PdfOperationResult"/> with processing statistics.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// pdf_convert("Documents/Papers")                     // Convert all PDFs in Papers folder
    /// pdf_convert("Documents/Papers", dryRun: true)       // Preview what would be processed
    /// pdf_convert("D:\OneDrive\Documents\paper.pdf")      // Convert single PDF file
    /// </code>
    /// </example>
    Task<PdfOperationResult> ConvertAsync(
        string inputPath,
        string? outputPath = null,
        bool dryRun = false,
        bool noSummary = false,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a PDF conversion operation.
/// </summary>
public record PdfOperationResult
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable summary of the operation.</summary>
    public required string Message { get; init; }

    /// <summary>Number of PDF files found.</summary>
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
