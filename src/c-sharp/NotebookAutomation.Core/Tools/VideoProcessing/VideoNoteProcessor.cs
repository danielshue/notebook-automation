using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;
using Xabe.FFmpeg;

namespace NotebookAutomation.Core.Tools.VideoProcessing
{
    /// <summary>
    /// Provides functionality for extracting metadata and generating markdown notes from video files.
    /// </summary>
    public class VideoNoteProcessor
    {
        private readonly ILogger _logger;

        public VideoNoteProcessor(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Extracts metadata from a video file (duration, resolution, codec, etc.).
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <returns>Dictionary of extracted metadata.</returns>
        public async Task<Dictionary<string, object>> ExtractMetadataAsync(string videoPath)
        {
            var metadata = new Dictionary<string, object>
            {
                { "title", Path.GetFileNameWithoutExtension(videoPath) },
                { "source_file", videoPath },
                { "generated", DateTime.UtcNow.ToString("u") }
            };
            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
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
        /// <param name="text">Transcript or extracted text.</param>
        /// <param name="openAiApiKey">OpenAI API key.</param>
        /// <param name="prompt">Optional prompt.</param>
        /// <param name="promptFileName">Optional prompt file name.</param>
        /// <returns>Summary text.</returns>
        public async Task<string> GenerateAiSummaryAsync(string text, string? openAiApiKey, string? prompt = null, string? promptFileName = null)
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
        /// <param name="summary">AI-generated summary.</param>
        /// <param name="metadata">Metadata dictionary.</param>
        /// <returns>Markdown note content.</returns>
        public string GenerateMarkdownNote(string summary, Dictionary<string, object> metadata)
        {
            metadata["summary"] = summary;
            var markdownBody = $"# Video Note\n\n{summary}";
            var builder = new MarkdownNoteBuilder(_logger);
            return builder.BuildNote(metadata, markdownBody);
        }

        /// <summary>
        /// Attempts to load a transcript file for the given video (same basename, .txt or .md),
        /// matching the Python logic: prefer transcript if present, fallback to metadata summary.
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <returns>Transcript text if found, otherwise null.</returns>
        public string? TryLoadTranscript(string videoPath)
        {
            var baseName = Path.Combine(Path.GetDirectoryName(videoPath) ?? string.Empty, Path.GetFileNameWithoutExtension(videoPath));
            var transcriptCandidates = new[] { baseName + ".txt", baseName + ".md" };
            foreach (var transcriptPath in transcriptCandidates)
            {
                if (File.Exists(transcriptPath))
                {
                    _logger.LogInformation("Loaded transcript: {Path}", transcriptPath);
                    return File.ReadAllText(transcriptPath);
                }
            }
            _logger.LogInformation("No transcript found for video: {VideoPath}", videoPath);
            return null;
        }

        /// <summary>
        /// Generates a video note, using transcript if available, otherwise metadata summary.
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <param name="openAiApiKey">OpenAI API key.</param>
        /// <param name="promptFileName">Prompt file for AI summary.</param>
        /// <returns>Markdown note content.</returns>
        public async Task<string> GenerateVideoNoteAsync(string videoPath, string? openAiApiKey, string? promptFileName = null)
        {
            var metadata = await ExtractMetadataAsync(videoPath);
            string? transcript = TryLoadTranscript(videoPath);
            string summaryInput;
            if (!string.IsNullOrWhiteSpace(transcript))
            {
                summaryInput = transcript;
                _logger.LogInformation("Using transcript for AI summary for video: {VideoPath}", videoPath);
            }
            else
            {
                summaryInput = $"Video file: {Path.GetFileName(videoPath)}\n(No transcript available. Using metadata only.)";
                _logger.LogInformation("No transcript found. Using metadata for AI summary for video: {VideoPath}", videoPath);
            }
            string aiSummary = await GenerateAiSummaryAsync(summaryInput, openAiApiKey, null, promptFileName);
            return GenerateMarkdownNote(aiSummary, metadata);
        }
    }
}
