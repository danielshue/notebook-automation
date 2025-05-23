using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;
using NotebookAutomation.Core.Tools.Shared;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
public class VideoNoteProcessor : DocumentNoteProcessorBase
    {
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoNoteProcessor"/> class with a logger.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
    public VideoNoteProcessor(ILogger logger) : base(logger) { }

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
        /// <inheritdoc/>
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
        // Inherit base implementation

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
            // Use base implementation for consistent formatting
            return base.GenerateMarkdownNote(summary, metadata, "Video Note");
        }

        /// <summary>
        /// Attempts to load a transcript file for the given video.
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <returns>The transcript text if found, otherwise null.</returns>        /// <remarks>
        /// <para>
        /// This method searches for transcript files that match the video filename using a sophisticated
        /// prioritized search strategy. It looks for transcript files in multiple locations and formats,
        /// similar to the Python implementation.
        /// </para>
        /// <para>
        /// The search follows this priority order:
        /// </para>
        /// <list type="number">
        /// <item><description>Language-specific transcripts in same directory (e.g., video.en.txt, video.zh-cn.txt)</description></item>
        /// <item><description>Generic transcript in same directory (video.txt, video.md)</description></item>
        /// <item><description>Language-specific transcripts in Transcripts subdirectory</description></item>
        /// <item><description>Generic transcript in Transcripts subdirectory</description></item>
        /// </list>
        /// <para>
        /// The method also handles name normalization by checking alternative spellings with hyphens
        /// replaced by underscores and vice versa.
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
            
            _logger.LogDebug("Looking for transcript for video: {VideoPath}", videoPath);
            
            // Define search paths in priority order
            var searchPaths = new List<string>
            {
                directory, // Same directory as video
                Path.Combine(directory, "Transcripts") // Transcripts subdirectory
            };

            // Search through directories in priority order
            foreach (var searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath))
                {
                    _logger.LogDebug("Directory does not exist: {Path}", searchPath);
                    continue;
                }
                
                // Get all .txt and .md files in the directory
                var candidates = Directory.GetFiles(searchPath, "*.txt")
                    .Concat(Directory.GetFiles(searchPath, "*.md"))
                    .Select(path => new FileInfo(path))
                    .ToList();
                
                if (!candidates.Any())
                {
                    _logger.LogDebug("No transcript candidates found in: {Path}", searchPath);
                    continue;
                }
                
                // Generate alternate base name by replacing hyphens with underscores or vice versa
                string altBaseName = fileNameWithoutExt.Contains('-') 
                    ? fileNameWithoutExt.Replace('-', '_') 
                    : fileNameWithoutExt.Replace('_', '-');
                
                // First priority: language-specific transcript with exact name (video.en.txt, video.zh-cn.txt)
                foreach (var candidate in candidates)
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(candidate.Name);
                    if (nameWithoutExt.StartsWith(fileNameWithoutExt + ".") && 
                        IsLikelyLanguageCode(nameWithoutExt.Substring(fileNameWithoutExt.Length + 1)))
                    {
                        string langCode = nameWithoutExt.Substring(fileNameWithoutExt.Length + 1);
                        _logger.LogInformation("Found language-specific transcript ({LangCode}): {Path}", langCode, candidate.FullName);
                        return File.ReadAllText(candidate.FullName);
                    }
                }
                
                // Second priority: language-specific transcript with normalized name
                foreach (var candidate in candidates)
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(candidate.Name);
                    if (nameWithoutExt.StartsWith(altBaseName + ".") && 
                        IsLikelyLanguageCode(nameWithoutExt.Substring(altBaseName.Length + 1)))
                    {
                        string langCode = nameWithoutExt.Substring(altBaseName.Length + 1);
                        _logger.LogInformation("Found language-specific transcript with normalized name ({LangCode}): {Path}", 
                            langCode, candidate.FullName);
                        return File.ReadAllText(candidate.FullName);
                    }
                }
                
                // Third priority: generic transcript with exact name (video.txt, video.md)
                string exactTxtPath = Path.Combine(searchPath, fileNameWithoutExt + ".txt");
                if (File.Exists(exactTxtPath))
                {
                    _logger.LogInformation("Found generic transcript: {Path}", exactTxtPath);
                    return File.ReadAllText(exactTxtPath);
                }
                
                string exactMdPath = Path.Combine(searchPath, fileNameWithoutExt + ".md");
                if (File.Exists(exactMdPath))
                {
                    _logger.LogInformation("Found generic transcript: {Path}", exactMdPath);
                    return File.ReadAllText(exactMdPath);
                }
                
                // Fourth priority: generic transcript with normalized name (video_alt.txt, video_alt.md)
                string altTxtPath = Path.Combine(searchPath, altBaseName + ".txt");
                if (File.Exists(altTxtPath))
                {
                    _logger.LogInformation("Found generic transcript with normalized name: {Path}", altTxtPath);
                    return File.ReadAllText(altTxtPath);
                }
                
                string altMdPath = Path.Combine(searchPath, altBaseName + ".md");
                if (File.Exists(altMdPath))
                {
                    _logger.LogInformation("Found generic transcript with normalized name: {Path}", altMdPath);
                    return File.ReadAllText(altMdPath);
                }
            }
            
            _logger.LogInformation("No transcript found for video: {VideoPath}", videoPath);
            return null;
        }

        /// <summary>
        /// Determines whether a string is likely to be a language code.
        /// </summary>
        /// <param name="code">The string to check.</param>
        /// <returns>True if the string matches language code patterns like 'en', 'zh-cn', etc.</returns>
        /// <remarks>
        /// Recognizes standard language codes like 'en', 'fr', 'de', as well as regional variants
        /// with hyphenation like 'en-us', 'zh-cn', etc. This is used to identify language-specific
        /// transcripts.
        /// </remarks>
        private bool IsLikelyLanguageCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
                
            // Match patterns like: en, fr, de (2 chars)
            if (code.Length == 2 && code.All(char.IsLetter))
                return true;
                
            // Match patterns like: eng, fra, deu (3 chars)
            if (code.Length == 3 && code.All(char.IsLetter))
                return true;
                
            // Match patterns like: en-us, zh-cn (with hyphen)
            if (code.Length >= 5 && code.Length <= 7 && code.Contains('-'))
            {
                var parts = code.Split('-');
                return parts.Length == 2 && 
                       parts.All(p => p.Length >= 2 && p.Length <= 3 && p.All(char.IsLetter));
            }
            
            return false;
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
        /// <summary>
        /// Generates a video note, using transcript if available, otherwise metadata summary, with extended options.
        /// </summary>
        /// <param name="videoPath">Path to the video file to process.</param>
        /// <param name="openAiApiKey">OpenAI API key for generating summaries.</param>
        /// <param name="promptFileName">Optional prompt file name to use for AI summarization.</param>
        /// <param name="noSummary">If true, disables OpenAI summary generation.</param>
        /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>
        /// <param name="resourcesRoot">Optional override for resources root directory.</param>
        /// <returns>A complete markdown note as a string.</returns>
        public async Task<string> GenerateVideoNoteAsync(
            string videoPath,
            string? openAiApiKey,
            string? promptFileName = null,
            bool noSummary = false,
            int? timeoutSeconds = null,
            string? resourcesRoot = null)
        {
            // Extract metadata and transcript
            var metadata = await ExtractMetadataAsync(videoPath);
            // If resourcesRoot is provided, add it to metadata for downstream use
            if (!string.IsNullOrWhiteSpace(resourcesRoot))
            {
                metadata["resources_root"] = resourcesRoot;
            }
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
            string aiSummary;
            if (noSummary)
            {
                aiSummary = "[Summary generation disabled by --no-summary flag.]";
            }
            else
            {
                aiSummary = await GenerateAiSummaryAsync(summaryInput, openAiApiKey, null, promptFileName);
            }
            return GenerateMarkdownNote(aiSummary, metadata);
        }

        /// <inheritdoc/>
        public override async Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string filePath)
        {
            var metadata = await ExtractMetadataAsync(filePath);
            string? transcript = TryLoadTranscript(filePath);
            string text = transcript ?? "";
            return (text, metadata);
        }
        
    }
}