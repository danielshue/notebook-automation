using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
    /// </para>
    /// </remarks>
    public class VideoNoteBatchProcessor
    {
        /// <summary>
        /// The logger instance used for diagnostic and error reporting.
        /// </summary>
        private readonly ILogger _logger;
        
        /// <summary>
        /// The video processor instance used for processing individual video files.
        /// </summary>
        private readonly VideoNoteProcessor _videoProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoNoteBatchProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
        /// <remarks>
        /// Creates a new <see cref="VideoNoteProcessor"/> instance using the provided logger.
        /// </remarks>
        public VideoNoteBatchProcessor(ILogger logger)
        {
            _logger = logger;
            _videoProcessor = new VideoNoteProcessor(logger);
        }

        /// <summary>
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
        /// This method handles batch processing of video files with the following workflow:
        /// </para>
        /// <list type="number">
        /// <item><description>Locates video files based on the input path and extensions</description></item>
        /// <item><description>For each video file:
        ///     <list type="bullet">
        ///     <item><description>Calls <see cref="VideoNoteProcessor.GenerateVideoNoteAsync"/> to create a markdown note</description></item>
        ///     <item><description>Saves the note to the output directory (unless in dry run mode)</description></item>
        ///     <item><description>Logs results and increments success/failure counters</description></item>
        ///     </list>
        /// </description></item>
        /// <item><description>Returns a summary of processed and failed counts</description></item>
        /// </list>
        /// <para>
        /// The method supports both directory and single file inputs. When a directory is provided,
        /// it recursively searches for all files matching the specified video extensions.
        /// </para>
        /// <para>
        /// In dry run mode, the method performs all processing but does not write any output files,
        /// which is useful for testing or previewing the operation.
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
        public async Task<(int processed, int failed)> ProcessVideosAsync(
            string input,
            string? output,
            List<string> videoExtensions,
            string? openAiApiKey,
            bool dryRun = false)
        {
            var processed = 0;
            var failed = 0;
            var videoFiles = new List<string>();
            if (Directory.Exists(input))
            {
                foreach (var ext in videoExtensions)
                {
                    videoFiles.AddRange(Directory.GetFiles(input, "*" + ext, SearchOption.AllDirectories));
                }
                _logger.LogInformation("Found {Count} video files in directory: {Dir}", videoFiles.Count, input);
            }
            else if (File.Exists(input) && videoExtensions.Exists(ext => input.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                videoFiles.Add(input);
            }
            else
            {
                _logger.LogError("Input must be a video file or directory containing videos: {Input}", input);
                return (0, 1);
            }
            foreach (var videoPath in videoFiles)
            {
                try
                {
                    _logger.LogInformation("Processing video: {VideoPath}", videoPath);
                    string markdown = await _videoProcessor.GenerateVideoNoteAsync(videoPath, openAiApiKey, "chunk_summary_prompt.md");
                    if (!dryRun)
                    {
                        string outputDir = output ?? "Generated";
                        Directory.CreateDirectory(outputDir);
                        string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(videoPath) + ".md");
                        await File.WriteAllTextAsync(outputPath, markdown);
                        _logger.LogInformation("Markdown note saved to: {OutputPath}", outputPath);
                    }
                    else
                    {
                        _logger.LogInformation("[DRY RUN] Markdown note would be generated for: {VideoPath}", videoPath);
                    }
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process video: {VideoPath}", videoPath);
                    failed++;
                }
            }
            _logger.LogInformation("Video processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            return (processed, failed);
        }
    }
}
