using NotebookAutomation.Core.Tools.Shared;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tools.VideoProcessing
{
    /// <summary>
    /// Provides batch processing capabilities for converting multiple video files to markdown notes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The VideoNoteBatchProcessor class coordinates the processing of multiple video files,
    /// either from a specified directory or a single file path. It leverages the 
    /// <see cref="VideoNoteProcessor"/> to handle the details of metadata extraction, 
    /// transcript loading, and note generation for each video.
    /// </para>
    /// <para>
    /// This batch processor is responsible for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Identifying video files based on their extensions</description></item>
    /// <item><description>Coordinating the processing of each video file</description></item>
    /// <item><description>Managing output directory creation and file writing</description></item>
    /// <item><description>Tracking success and failure counts</description></item>
    /// <item><description>Supporting dry run mode for testing without file writes</description></item>
    /// </list>
    /// <para>
    /// The class is designed to be used by both CLI and API interfaces, providing a central
    /// point for video batch processing operations with appropriate logging and error handling.
    /// This implementation delegates all batch processing logic to the generic 
    /// <see cref="DocumentNoteBatchProcessor{TProcessor}"/> for maintainability and code reuse.
    /// </para>
    /// </remarks>
    public class VideoNoteBatchProcessor
    {
        /// <summary>
        /// The generic batch processor that handles the actual batch processing logic.
        /// </summary>
        private readonly DocumentNoteBatchProcessor<VideoNoteProcessor> _batchProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoNoteBatchProcessor"/> class with a batch processor.
        /// </summary>
        /// <param name="batchProcessor">The batch processor to use for processing video notes.</param>
        public VideoNoteBatchProcessor(DocumentNoteBatchProcessor<VideoNoteProcessor> batchProcessor)
        {
            _batchProcessor = batchProcessor;
        }

        /// Processes one or more video files, generating markdown notes for each.
        /// </summary>
        /// <param name="input">Input file path or directory containing video files.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="videoExtensions">List of file extensions to recognize as video files.</param>
        /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
        /// <param name="dryRun">If true, simulates processing without writing output files.</param>
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
        /// <param name="resourcesRoot">Optional override for resources root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
        /// <returns>A tuple containing the count of successfully processed files and the count of failures.</returns>
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
        /// <param name="resourcesRoot">Optional override for resources root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
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
            AppConfig? appConfig = null)
        {
            return await _batchProcessor.ProcessDocumentsAsync(
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
                "failed_videos.txt");
        }
    }
}
