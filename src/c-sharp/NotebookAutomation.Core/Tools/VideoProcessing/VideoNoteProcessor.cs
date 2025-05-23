using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.VideoProcessing
{
    /// <summary>
    /// Provides functionality for extracting metadata and generating markdown notes from video files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The VideoNoteProcessor class is responsible for processing video files to extract metadata,
    /// load associated transcripts when available, generate AI-powered summaries of content,
    /// and produce rich markdown notes for use in an Obsidian vault or similar knowledge base.
    /// </para>
    /// <para>
    /// This processor handles various video formats (MP4, MOV, AVI, MKV, WEBM, etc.) and can extract
    /// comprehensive technical metadata such as duration, resolution, codec information, and file properties.
    /// It works in conjunction with the OpenAiSummarizer to create intelligent summaries of video content
    /// when transcripts are available, or based on metadata when they are not.
    /// </para>
    /// <para>
    /// The class integrates with Xabe.FFmpeg for video metadata extraction and supports finding
    /// and loading associated transcript files (with .txt or .md extensions) that may exist alongside videos.
    /// </para>
    /// <para>
    /// This processor is primarily used by the <see cref="VideoNoteBatchProcessor"/> for handling
    /// multiple video files and is exposed to command-line interfaces through the 
    /// <see cref="VideoNoteProcessingEntrypoint"/>.
    /// </para>
    /// </remarks>
    public class VideoNoteProcessor
    {
        /// <summary>
        /// Logger instance for recording diagnostic information and errors.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoNoteProcessor"/> class with a logger.
        /// </summary>
        /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
        public VideoNoteProcessor(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Extracts comprehensive metadata from a video file.
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <returns>A dictionary containing extracted video metadata.</returns>
        /// <remarks>
        /// <para>
        /// This method extracts a wide range of metadata from a video file, including:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Basic file properties (name, path, size, creation date, modification date)</description></item>
        /// <item><description>Video-specific properties (duration, resolution, codec)</description></item>
        /// <item><description>Content identification (title derived from filename)</description></item>
        /// </list>
        /// <para>
        /// The metadata extraction leverages Xabe.FFmpeg for detailed video information. If the FFmpeg analysis fails,
        /// the method will still return basic file information and log a warning rather than failing completely.
        /// </para>
        /// </remarks>
        public async Task<Dictionary<string, object>> ExtractMetadataAsync(string videoPath)
        {
            var metadata = new Dictionary<string, object>
            {
                { "title", Path.GetFileNameWithoutExtension(videoPath) },
                { "source_file", videoPath },
                { "generated", DateTime.UtcNow.ToString("u") },
                { "file_name", Path.GetFileName(videoPath) },
                { "file_extension", Path.GetExtension(videoPath) }
            };

            try
            {
                var fileInfo = new FileInfo(videoPath);
                metadata["size_bytes"] = fileInfo.Exists ? fileInfo.Length : 0;
                metadata["last_modified"] = fileInfo.Exists ? fileInfo.LastWriteTimeUtc.ToString("u") : "[Unknown]";
                metadata["created"] = fileInfo.Exists ? fileInfo.CreationTimeUtc.ToString("u") : "[Unknown]";
                metadata["directory"] = fileInfo.DirectoryName ?? string.Empty;
                metadata["type"] = "video";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract file system metadata for video: {Path}", videoPath);
            }

            try
            {
                var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(videoPath);
                var videoStream = mediaInfo.VideoStreams?.FirstOrDefault();
                if (videoStream != null)
                {
                    metadata["duration"] = videoStream.Duration.ToString();
                    metadata["resolution"] = $"{videoStream.Width}x{videoStream.Height}";
                    metadata["codec"] = videoStream.Codec;
                }
                else
                {
                    metadata["duration"] = mediaInfo.Duration.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract real video metadata, using simulated values.");
                metadata["duration"] = "[Simulated duration]";
                metadata["resolution"] = "[Unknown]";
                metadata["codec"] = "[Unknown]";
            }
            
            return metadata;
        }

        /// <summary>
        /// Generates an AI summary for the video transcript or metadata.
        /// </summary>
        /// <param name="text">Transcript or extracted text to summarize.</param>
        /// <param name="openAiApiKey">OpenAI API key for accessing the summarization service.</param>
        /// <param name="prompt">Optional custom prompt to guide the summary generation.</param>
        /// <param name="promptFileName">Optional filename of a prompt template to use.</param>
        /// <returns>The AI-generated summary text, or a placeholder if summarization fails.</returns>
        /// <remarks>
        /// <para>
        /// This method uses the OpenAI API to generate a contextual summary of the provided text,
        /// which is typically either a video transcript or metadata information. The summary helps
        /// users quickly understand the content of a video without watching it in full.
        /// </para>
        /// <para>
        /// If no OpenAI API key is provided, a simulated summary placeholder is returned.
        /// The method supports custom prompts either provided directly as a string or loaded
        /// from a template file.
        /// </para>
        /// <para>
        /// The AI summarization process is handled by the <see cref="OpenAiSummarizer"/> service.
        /// </para>
        /// </remarks>
        public async Task<string> GenerateAiSummaryAsync(string text, string? openAiApiKey = null, string? prompt = null, string? promptFileName = null)
        {
            if (string.IsNullOrWhiteSpace(openAiApiKey))
            {
                _logger.LogWarning("No OpenAI API key provided. Using simulated summary.");
                return "[Simulated AI summary of video]";
            }
            
            var summarizer = new OpenAiSummarizer(_logger, openAiApiKey);
            var summary = await summarizer.SummarizeAsync(text, prompt, promptFileName);
            return summary ?? "[AI summary unavailable]";
        }

        /// <summary>
        /// Generates a markdown note from video metadata and summary.
        /// </summary>
        /// <param name="summary">AI-generated summary or content for the body of the note.</param>
        /// <param name="metadata">Dictionary of metadata to include in the note's frontmatter.</param>
        /// <returns>A complete markdown note with frontmatter and content.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a properly formatted markdown note that combines the AI-generated
        /// summary with the video's metadata. The resulting note includes:
        /// </para>
        /// <list type="bullet">
        /// <item><description>YAML frontmatter with all extracted metadata</description></item>
        /// <item><description>The summary as the main content body</description></item>
        /// <item><description>A consistent structure with appropriate headers</description></item>
        /// </list>
        /// <para>
        /// The note is generated using the <see cref="MarkdownNoteBuilder"/> utility and follows
        /// the structure expected by Obsidian or similar markdown-based knowledge management systems.
        /// </para>
        /// </remarks>
        public string GenerateMarkdownNote(string summary, Dictionary<string, object> metadata)
        {
            metadata["summary"] = summary;
            var markdownBody = $"# Video Note\n\n{summary}";
            var builder = new MarkdownNoteBuilder(_logger);
            return builder.BuildNote(metadata, markdownBody);
        }

        /// <summary>
        /// Attempts to load a transcript file for the given video.
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <returns>The transcript text if found, otherwise null.</returns>
        /// <remarks>
        /// <para>
        /// This method searches for transcript files that match the video filename but with
        /// .txt or .md extensions. It follows the same logic as the Python implementation,
        /// preferring to use an available transcript for summary generation rather than
        /// relying solely on video metadata.
        /// </para>
        /// <para>
        /// The method looks for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>A .txt file with the same base name as the video</description></item>
        /// <item><description>A .md file with the same base name as the video</description></item>
        /// </list>
        /// <para>
        /// If a transcript is found, it's loaded and returned as plain text.
        /// </para>
        /// </remarks>
        public string? TryLoadTranscript(string videoPath)
        {
            if (string.IsNullOrEmpty(videoPath))
            {
                _logger.LogWarning("Empty video path provided to TryLoadTranscript");
                return null;
            }

            string directory = Path.GetDirectoryName(videoPath) ?? string.Empty;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoPath);
            string baseName = Path.Combine(directory, fileNameWithoutExt);
            
            if (File.Exists(baseName + ".txt"))
            {
                _logger.LogInformation("Loaded transcript: {Path}", baseName + ".txt");
                return File.ReadAllText(baseName + ".txt");
            }
            
            if (File.Exists(baseName + ".md"))
            {
                _logger.LogInformation("Loaded transcript: {Path}", baseName + ".md");
                return File.ReadAllText(baseName + ".md");
            }
            
            _logger.LogInformation("No transcript found for video: {VideoPath}", videoPath);
            return null;
        }

        /// <summary>
        /// Generates a video note, using transcript if available, otherwise metadata summary.
        /// </summary>
        /// <param name="videoPath">Path to the video file to process.</param>
        /// <param name="openAiApiKey">OpenAI API key for generating summaries.</param>
        /// <param name="promptFileName">Optional prompt file name to use for AI summarization.</param>
        /// <returns>A complete markdown note as a string.</returns>
        /// <remarks>
        /// <para>
        /// This method coordinates the entire video note generation process by:
        /// </para>
        /// <list type="number">
        /// <item><description>Extracting comprehensive metadata from the video file</description></item>
        /// <item><description>Attempting to load an associated transcript file</description></item>
        /// <item><description>Generating an AI summary from either the transcript or metadata</description></item>
        /// <item><description>Combining everything into a well-formatted markdown note</description></item>
        /// </list>
        /// <para>
        /// The method prefers using a transcript when available for more accurate summaries,
        /// but will fall back to using just the metadata information when no transcript exists.
        /// </para>
        /// <para>
        /// This is the primary entry point for generating a complete video note from a single video file.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var processor = new VideoNoteProcessor(logger);
        /// string markdownNote = await processor.GenerateVideoNoteAsync(
        ///     "path/to/lecture.mp4",
        ///     "sk-yourapikeyhere",
        ///     "video_summary_prompt.md");
        /// 
        /// // Save the generated note
        /// File.WriteAllText("lecture_notes.md", markdownNote);
        /// </code>
        /// </example>
        public async Task<string> GenerateVideoNoteAsync(string videoPath, string? openAiApiKey, string? promptFileName = null)
        {
            // Extract metadata and transcript
            var metadata = await ExtractMetadataAsync(videoPath);
            string? transcript = TryLoadTranscript(videoPath);
            
            // Choose input for summary based on transcript availability
            string summaryInput;
            if (transcript != null && !string.IsNullOrWhiteSpace(transcript))
            {
                summaryInput = transcript;
                _logger.LogInformation("Using transcript for AI summary for video: {VideoPath}", videoPath);
            }
            else
            {
                summaryInput = $"Video file: {Path.GetFileName(videoPath)}\n(No transcript available. Using metadata only.)";
                _logger.LogInformation("No transcript found. Using metadata for AI summary for video: {VideoPath}", videoPath);
            }
            
            // Generate summary and markdown note
            string aiSummary = await GenerateAiSummaryAsync(summaryInput, openAiApiKey, null, promptFileName);
            return GenerateMarkdownNote(aiSummary, metadata);
        }
    }
}