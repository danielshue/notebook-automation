// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.VideoTranscriptProcessing;

/// <summary>
/// Shared constants for video transcript consolidation metadata and context keys.
/// </summary>
public static class VideoTranscriptConstants
{
    /// <summary>Template identifier used in metadata schema.</summary>
    public const string TemplateType = "video_transcript_consolidation";

    /// <summary>Context dictionary key for the aggregated transcript sources.</summary>
    public const string SourcesContextKey = "video_transcript_sources";
}

/// <summary>
/// Represents a request to consolidate transcripts starting from a specific folder.
/// </summary>
/// <param name="InputPath">The folder path (absolute or relative to OneDrive root) containing videos.</param>
/// <param name="Recursive">Whether to include all nested subdirectories.</param>
/// <param name="Force">Overwrite the consolidated markdown even if unchanged.</param>
/// <param name="DryRun">Skip writing files and log the intended changes only.</param>
public record VideoTranscriptConsolidationRequest(
    string InputPath,
    bool Recursive,
    bool Force,
    bool DryRun);

/// <summary>
/// Aggregated result information after consolidation completes.
/// </summary>
public record VideoTranscriptConsolidationResult(
    string OutputPath,
    int AggregatedCount,
    int SkippedCount,
    bool WasWritten,
    IReadOnlyList<VideoTranscriptSourceEntry> Sources);

/// <summary>
/// Captures metadata about an individual transcript included in the consolidated note.
/// </summary>
/// <param name="FriendlyTitle">Human-friendly title derived from the transcript or video file.</param>
/// <param name="Anchor">Anchor slug used for table-of-contents links.</param>
/// <param name="RelativeVideoPath">Relative path to the original video within OneDrive resources.</param>
/// <param name="RelativeTranscriptPath">Relative path to the source transcript file when available.</param>
/// <param name="NoteLink">Wiki-style link to the generated video note inside the vault, if present.</param>
/// <param name="Language">Language code used when selecting the transcript, if known.</param>
/// <param name="TranscriptContent">Raw transcript text appended to the consolidated markdown.</param>
public record VideoTranscriptSourceEntry(
    string FriendlyTitle,
    string Anchor,
    string RelativeVideoPath,
    string? RelativeTranscriptPath,
    string? NoteLink,
    string? Language,
    string TranscriptContent);
