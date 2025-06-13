// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.VideoProcessing;

/// <summary>
/// Provides batch processing capabilities for converting multiple video files to markdown notes.
/// </summary>
/// <remarks>
/// The <c>VideoNoteBatchProcessor</c> class coordinates the processing of multiple video files,
/// leveraging the <see cref="VideoNoteProcessor"/> for metadata extraction, transcript loading,
/// and note generation. It supports dry-run mode, output directory management, and eventing
/// for real-time progress tracking.
/// </remarks>
public class VideoNoteBatchProcessor(DocumentNoteBatchProcessor<VideoNoteProcessor> batchProcessor)
{
    /// <summary>
    /// The generic batch processor that handles the actual batch processing logic.
    /// </summary>
    private readonly DocumentNoteBatchProcessor<VideoNoteProcessor> batchProcessor = batchProcessor;

    /// <summary>
    /// Event triggered when processing progress changes.
    /// </summary>
    public event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged
    {
        add => batchProcessor.ProcessingProgressChanged += value;
        remove => batchProcessor.ProcessingProgressChanged -= value;
    }

    /// <summary>
    /// Event triggered when the processing queue changes.
    /// </summary>
    public event EventHandler<QueueChangedEventArgs>? QueueChanged
    {
        add => batchProcessor.QueueChanged += value;
        remove => batchProcessor.QueueChanged -= value;
    }

    /// <summary>
    /// Processes one or more video files, generating markdown notes for each.
    /// </summary>
    /// <param name="input">Input file path or directory containing video files.</param>
    /// <param name="output">Output directory where markdown notes will be saved.</param>
    /// <param name="videoExtensions">List of file extensions to recognize as video files.</param>
    /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
    /// <param name="dryRun">If true, simulates processing without writing output files.</param>
    /// <param name="createShareLinks">If true, creates OneDrive share links for the video files.</param>
    /// <returns>
    /// A tuple containing the count of successfully processed files and the count of failures.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method delegates to the generic <see cref="DocumentNoteBatchProcessor{TProcessor}"/>
    /// for all batch processing operations while maintaining backward compatibility with
    /// existing video-specific API.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Process all video files in a directory
    /// var processor = new VideoNoteBatchProcessor(logger);
    /// var result = await processor.ProcessVideosAsync(
    ///     "path/to/videos",
    ///     "path/to/notes",
    ///     new List&lt;string&gt; { ".mp4", ".mov", ".avi" },
    ///     "sk-yourapikeyhere");
    ///
    /// Console.WriteLine($"Processed: {result.processed}, Failed: {result.failed}");
    /// </code>
    /// </example>
    /// <summary>
    /// Processes one or more video files, generating markdown notes for each, with extended options.
    /// </summary>
    /// <param name="input">Input file path or directory containing video files.</param>
    /// <param name="output">Output directory where markdown notes will be saved.</param>
    /// <param name="videoExtensions">List of file extensions to recognize as video files.</param>
    /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
    /// <param name="dryRun">If true, simulates processing without writing output files.</param>
    /// <param name="noSummary">If true, disables OpenAI summary generation.</param>
    /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
    /// <param name="retryFailed">If true, retries only failed files from previous run.</param>
    /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>
    /// <param name="resourcesRoot">Optional override for OneDrive fullpath root directory.</param>
    /// <param name="appConfig">The application configuration object.</param>
    /// <param name="noShareLinks">If true, skips OneDrive share link creation.</param>
    /// <returns>A <see cref="BatchProcessResult"/> containing processing statistics and summary.</returns>
    public async Task<BatchProcessResult> ProcessVideosAsync(
        string input,
        string? output,
        List<string> videoExtensions,
        string? openAiApiKey,
        bool dryRun = false,
        bool noSummary = false,
        bool forceOverwrite = false,
        bool retryFailed = false,
        int? timeoutSeconds = null,
        string? resourcesRoot = null,
        AppConfig? appConfig = null,
        bool noShareLinks = false)
    {
        return await batchProcessor.ProcessDocumentsAsync(
            input,
            output,
            videoExtensions,
            openAiApiKey,
            dryRun,
            noSummary,
            forceOverwrite,
            retryFailed,
            timeoutSeconds,
            resourcesRoot,
            appConfig,
            "Video Note",
            "failed_videos.txt",
            noShareLinks).ConfigureAwait(false);
    }
}
