// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.VideoProcessing;

/// <summary>
/// Service interface for video processing operations exposed to Copilot tools.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IVideoService"/> provides a high-level API for creating notes from video files
/// and consolidating video transcripts. It wraps <see cref="VideoNoteBatchProcessor"/> and
/// <see cref="VideoTranscriptProcessing.VideoTranscriptConsolidationService"/>.
/// </para>
/// <para>
/// Path resolution follows these rules:
/// <list type="bullet">
/// <item><description>Relative paths are resolved against the OneDrive resources root</description></item>
/// <item><description>Absolute paths are validated to be within the OneDrive root</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IVideoService
{
    /// <summary>
    /// Creates markdown notes from video files, extracting metadata and transcripts.
    /// </summary>
    /// <param name="inputPath">
    /// Path to video file or directory. Can be relative to OneDrive root (e.g., "Classes/Module1")
    /// or absolute path within the OneDrive folder.
    /// </param>
    /// <param name="outputPath">
    /// Optional output directory for generated notes. Defaults to vault resources path.
    /// </param>
    /// <param name="dryRun">If true, simulates processing without writing files.</param>
    /// <param name="noSummary">If true, skips AI summary generation.</param>
    /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VideoOperationResult"/> with processing statistics.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// video_create_notes("Classes/Week1")                    // Process all videos in Week1 folder
    /// video_create_notes("Classes/Week1", dryRun: true)      // Preview what would be processed
    /// video_create_notes("D:\OneDrive\Classes\video.mp4")    // Process single video file
    /// </code>
    /// </example>
    Task<VideoOperationResult> CreateNotesAsync(
        string inputPath,
        string? outputPath = null,
        bool dryRun = false,
        bool noSummary = false,
        bool forceOverwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consolidates video transcripts from a folder into a single class-level note.
    /// </summary>
    /// <param name="inputPath">
    /// Path to folder containing video notes. Can be relative to vault root
    /// (e.g., "Notes/Classes/Module1") or absolute.
    /// </param>
    /// <param name="recursive">If true, includes videos from subdirectories.</param>
    /// <param name="force">If true, overwrites existing consolidated note.</param>
    /// <param name="dryRun">If true, simulates without writing the consolidated note.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="VideoConsolidationResult"/> with consolidation statistics.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// video_consolidate_transcripts("Notes/Classes/Module1")              // Consolidate module folder
    /// video_consolidate_transcripts("Notes/Classes", recursive: true)    // Consolidate all classes
    /// video_consolidate_transcripts("Notes/Classes/Module1", dryRun: true)  // Preview consolidation
    /// </code>
    /// </example>
    Task<VideoConsolidationResult> ConsolidateTranscriptsAsync(
        string inputPath,
        bool recursive = false,
        bool force = false,
        bool dryRun = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a video note creation operation.
/// </summary>
public record VideoOperationResult
{
    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable summary of the operation.</summary>
    public required string Message { get; init; }

    /// <summary>Number of video files found.</summary>
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

/// <summary>
/// Result of a video transcript consolidation operation.
/// </summary>
public record VideoConsolidationResult
{
    /// <summary>Whether the consolidation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable summary of the consolidation.</summary>
    public required string Message { get; init; }

    /// <summary>Path to the output consolidated note.</summary>
    public string? OutputPath { get; init; }

    /// <summary>Number of transcripts aggregated into the consolidated note.</summary>
    public int TranscriptsAggregated { get; init; }

    /// <summary>Number of videos skipped (no transcript found).</summary>
    public int Skipped { get; init; }

    /// <summary>Whether the consolidated note was written (false if dry run or unchanged).</summary>
    public bool WasWritten { get; init; }

    /// <summary>Whether this was a dry run.</summary>
    public bool DryRun { get; init; }

    /// <summary>Error message if operation failed.</summary>
    public string? ErrorMessage { get; init; }
}
