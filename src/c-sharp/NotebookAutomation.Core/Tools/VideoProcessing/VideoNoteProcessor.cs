using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Utils;
using NotebookAutomation.Core.Tools.Shared;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Configuration;

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
        private readonly IOneDriveService? _oneDriveService;
        private readonly AppConfig? _appConfig;
        private readonly MetadataTemplateManager? _templateManager;
        private readonly MetadataHierarchyDetector? _hierarchyDetector;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoNoteProcessor"/> class with a logger and AI summarizer.
        /// </summary>
        /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
        /// <param name="aiSummarizer">The AISummarizer service for generating AI-powered summaries.</param>
        /// <param name="oneDriveService">Optional OneDriveService for generating share links.</param>
        /// <param name="appConfig">Optional AppConfig for accessing application configuration.</param>
        public VideoNoteProcessor(
            ILogger<VideoNoteProcessor> logger,
            AISummarizer aiSummarizer,
            IOneDriveService? oneDriveService = null,
            AppConfig? appConfig = null) : base(logger, aiSummarizer)
        {
            _oneDriveService = oneDriveService;
            _appConfig = appConfig;

            // Initialize template manager and hierarchy detector if appConfig is provided
            if (_appConfig != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_appConfig.Paths.MetadataFile))
                    {
                        _templateManager = new MetadataTemplateManager(logger, _appConfig);
                    }

                    _hierarchyDetector = new MetadataHierarchyDetector(logger, _appConfig);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to initialize metadata components");
                }
            }
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
            var metadata = new Dictionary<string, object>                {
                    // Friendly title: remove numbers, underscores, file extension, and trim
                    { "title", FriendlyTitleHelper.GetFriendlyTitleFromFileName(Path.GetFileNameWithoutExtension(videoPath)) },
                    { "type", "note/video-note" },
                    { "status", "unwatched" },
                    { "author", new string[0] }, // Empty string array with correct field name
                    { "onedrive-shared-link", string.Empty }, // Will be populated by OneDrive service if available
                    { "onedrive_fullpath_file_reference", Path.GetFullPath(videoPath) }, // Full path to the video
                    { "transcript", string.Empty } // Will be populated if transcript file is found
                };

            // Extract module and lesson from directory structure
            var courseStructureExtractor = new CourseStructureExtractor(Logger);
            courseStructureExtractor.ExtractModuleAndLesson(videoPath, metadata);

            // Extract file creation date but exclude unwanted metadata fields
            try
            {
                var fileInfo = new FileInfo(videoPath);                // Date fields are now excluded per requirements
                if (fileInfo.Exists)
                {
                    // Add the file size in a human-readable format
                    long fileSizeBytes = fileInfo.Length;
                    metadata["video-size"] = FormatFileSizeToString(fileSizeBytes);
                    Logger.LogDebug("Added video size to metadata: {VideoSize}", metadata["video-size"]);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarningWithPath(ex, "Failed to extract file system metadata for video: {filePath}", videoPath);
            }

            try
            {
                var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(videoPath);
                var videoStream = mediaInfo.VideoStreams?.FirstOrDefault();
                if (videoStream != null)
                {
                    // Map to prefixed field names
                    metadata["video-duration"] = videoStream.Duration.ToString();
                    metadata["video-resolution"] = $"{videoStream.Width}x{videoStream.Height}";
                    metadata["video-codec"] = videoStream.Codec;
                }
                else
                {
                    metadata["video-duration"] = mediaInfo.Duration.ToString();
                }

                // Try to extract video upload date from metadata, fallback to current date
                DateTime videoUploadDate = DateTime.UtcNow;
                try
                {
                    // Try to get creation date from video metadata first
                    if (mediaInfo.CreationTime.HasValue)
                    {
                        videoUploadDate = mediaInfo.CreationTime.Value;
                    }
                    else
                    {
                        // Fallback to file creation time
                        var fileInfo = new FileInfo(videoPath);
                        if (fileInfo.Exists)
                        {
                            videoUploadDate = fileInfo.CreationTime;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to extract video upload date, using current date");
                }

                metadata["video-uploaded"] = videoUploadDate.ToString("yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to extract real video metadata, using simulated values.");
                metadata["video-duration"] = "[Simulated duration]";
                metadata["video-resolution"] = "[Unknown]";
                metadata["video-codec"] = "[Unknown]";
                metadata["video-uploaded"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }

            return metadata;
        }

        /// <summary>
        /// Formats a file size in bytes to a human-readable string (KB, MB, GB, etc.)
        /// </summary>
        /// <param name="bytes">File size in bytes</param>
        /// <returns>Human-readable file size string</returns>
        private string FormatFileSizeToString(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            // Format with appropriate precision (2 decimal places for smaller values, 1 for larger)
            if (size < 10)
                return $"{size:0.00} {suffixes[suffixIndex]}";
            else if (size < 100)
                return $"{size:0.0} {suffixes[suffixIndex]}";
            else
                return $"{Math.Round(size)} {suffixes[suffixIndex]}";
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
        /// Overrides the base class implementation to add title variable substitution for prompts.
        /// </summary>
        /// <param name="text">The extracted text/content.</param>
        /// <param name="variables">Optional variables to substitute in the prompt template.</param>
        /// <param name="promptFileName">Optional name of the prompt template file to use.</param>
        /// <returns>The summary text, or a simulated summary if unavailable.</returns>
        public override async Task<string> GenerateAiSummaryAsync(string text, Dictionary<string, string>? variables = null, string? promptFileName = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                Logger.LogWarning("Empty text provided for AI summary generation. Returning placeholder.");
                return "[No content available for summarization]";
            }

            try
            {
                // Set default prompt file name for video notes if not specified
                promptFileName ??= "final_summary_prompt";

                // If variables dictionary wasn't provided, create one
                variables ??= new Dictionary<string, string>();

                // If title variable wasn't provided, extract it from the file name if possible
                if (!variables.ContainsKey("title"))
                {
                    string title = "Untitled Video";

                    // Try to extract title from the first line if it starts with "Video file: "
                    if (text.StartsWith("Video file: "))
                    {
                        string fileName = text.Split('\n')[0].Substring("Video file: ".Length).Trim();
                        title = FriendlyTitleHelper.GetFriendlyTitleFromFileName(Path.GetFileNameWithoutExtension(fileName));
                    }

                    variables["title"] = title;
                    Logger.LogDebug("Added title '{Title}' to prompt variables", title);
                }

                // Call base implementation with our enriched variables
                return await base.GenerateAiSummaryAsync(text, variables, promptFileName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error generating AI summary");
                return "[Error during summarization]";
            }
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
        /// <item><description>A consistent structure with appropriate headers</description></item>        /// </list>        /// <para>
        /// The note is generated using the <see cref="MarkdownNoteBuilder"/> utility and follows
        /// the structure expected by Obsidian or similar markdown-based knowledge management systems.
        /// </para>
        /// </remarks>        
        public override string GenerateMarkdownNote(string bodyText, Dictionary<string, object>? metadata = null, string noteType = "Document Note", bool suppressBody = false, bool includeNoteTypeTitle = false)
        {
            // For video notes, we need special handling to extract and merge frontmatter
            var yamlHelper = new YamlHelper(Logger);

            // Use default metadata if none provided
            metadata = metadata ?? new Dictionary<string, object>();

            // Debug: Log the original summary
            Logger.LogInformation("VideoNoteProcessor.GenerateMarkdownNote called - Original AI summary (first 200 chars): {Summary}",
                bodyText.Length > 200 ? bodyText.Substring(0, 200) + "..." : bodyText);

            // Extract any existing frontmatter from the AI summary
            string? summaryFrontmatter = yamlHelper.ExtractFrontmatter(bodyText);

            Dictionary<string, object> summaryMetadata = new();

            if (!string.IsNullOrWhiteSpace(summaryFrontmatter))
            {
                summaryMetadata = yamlHelper.ParseYamlToDictionary(summaryFrontmatter);
                Logger.LogInformation("Extracted frontmatter from AI summary with {Count} fields", summaryMetadata.Count);
            }
            else
            {
                Logger.LogInformation("No frontmatter found in AI summary");
            }

            // Remove frontmatter from the summary content using YamlHelper
            string cleanSummary = yamlHelper.RemoveFrontmatter(bodyText);

            // Debug: Log the cleaned summary
            Logger.LogInformation("Cleaned summary (first 200 chars): {CleanSummary}",
                cleanSummary.Length > 200 ? cleanSummary.Substring(0, 200) + "..." : cleanSummary);

            // Merge metadata: video metadata takes precedence, but preserve AI tags if they exist
            var mergedMetadata = new Dictionary<string, object>(metadata);

            // If AI summary has tags and video metadata doesn't, use AI tags
            if (summaryMetadata.ContainsKey("tags") && !mergedMetadata.ContainsKey("tags"))
            {
                mergedMetadata["tags"] = summaryMetadata["tags"];
            }

            // Merge other non-conflicting AI metadata
            foreach (var kvp in summaryMetadata)
            {
                if (kvp.Key != "tags" && !mergedMetadata.ContainsKey(kvp.Key))
                {
                    mergedMetadata[kvp.Key] = kvp.Value;
                }
            }            // Apply path-based hierarchy detection if file path is available
            // Note: We temporarily add the path to metadata just for hierarchy detection
            var pathForHierarchy = metadata.ContainsKey("_internal_path") ? metadata["_internal_path"].ToString() : null;
            if (!string.IsNullOrEmpty(pathForHierarchy) && _hierarchyDetector != null)
            {
                try
                {
                    Logger.LogDebugWithPath("Detecting hierarchy information from path: {FilePath}", pathForHierarchy);
                    var hierarchyInfo = _hierarchyDetector!.FindHierarchyInfo(pathForHierarchy);
                    // Update metadata with detected hierarchy information
                    mergedMetadata = _hierarchyDetector!.UpdateMetadataWithHierarchy(mergedMetadata, hierarchyInfo);
                    Logger.LogInformationWithPath(
                        "Added hierarchy metadata - Program: {Program}, Course: {Course}, Class: {Class}",
                        pathForHierarchy!,
                        hierarchyInfo["program"],
                        hierarchyInfo["course"],
                        hierarchyInfo["class"]);
                }
                catch (Exception ex)
                {
                    Logger.LogWarningWithPath(ex, "Error detecting hierarchy information from path: {FilePath}", pathForHierarchy);
                }
            }

            // Remove internal path field as it's only used for hierarchy detection
            mergedMetadata.Remove("_internal_path");

            // Apply template enhancements if template manager is available
            if (_templateManager != null)
            {
                try
                {
                    // Add template metadata (template-type, etc.)
                    mergedMetadata = _templateManager!.EnhanceMetadataWithTemplate(mergedMetadata, noteType);
                    Logger.LogDebug("Enhanced metadata with template fields for note type: {NoteType}", noteType);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error applying template metadata");
                }
            }

            // Remove all date-related fields from metadata
            var dateFieldsToRemove = mergedMetadata.Keys
                .Where(k => k.StartsWith("date-") || k.EndsWith("-date"))
                .ToList();

            foreach (var dateField in dateFieldsToRemove)
            {
                mergedMetadata.Remove(dateField);
                Logger.LogDebug("Removed date field {DateField} from metadata", dateField);
            }

            // Log mergedMetadata keys and values once before serialization (for debug)
            Logger.LogDebug("Final mergedMetadata before serialization:");
            foreach (var kvp in mergedMetadata)
            {
                Logger.LogDebug("{Key}: {Value}", kvp.Key, kvp.Value);
            }            // Use base implementation with cleaned summary and merged metadata
            // Include a title but use the friendly title from frontmatter instead of the note type
            return base.GenerateMarkdownNote(cleanSummary, mergedMetadata, noteType, suppressBody, includeNoteTypeTitle: true);
        }

        /// <summary>
        /// Attempts to load a transcript file for the given video.
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <returns>The transcript text if found, otherwise null.</returns>
        /// <remarks>
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
                Logger.LogWarning("Empty video path provided to TryLoadTranscript");
                return null;
            }

            string directory = Path.GetDirectoryName(videoPath) ?? string.Empty;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoPath);

            Logger.LogDebugWithPath("Looking for transcript for video: {FilePath}", videoPath);

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
                    Logger.LogDebug("Directory does not exist: {Path}", searchPath);
                    continue;
                }

                // Get all .txt and .md files in the directory
                var candidates = Directory.GetFiles(searchPath, "*.txt")
                    .Concat(Directory.GetFiles(searchPath, "*.md"))
                    .Select(path => new FileInfo(path))
                    .ToList();

                if (!candidates.Any())
                {
                    Logger.LogDebug("No transcript candidates found in: {Path}", searchPath);
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
                        Logger.LogInformation("Found language-specific transcript ({LangCode}): {Path}", langCode, candidate.FullName);
                        return File.ReadAllText(candidate.FullName);
                    }
                }

                // Second priority: language-specific transcript with normalized name
                foreach (var candidate in candidates)
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(candidate.Name);
                    if (nameWithoutExt.StartsWith(altBaseName + ".") && IsLikelyLanguageCode(nameWithoutExt.Substring(altBaseName.Length + 1)))
                    {
                        string langCode = nameWithoutExt.Substring(altBaseName.Length + 1);
                        Logger.LogInformation("Found language-specific transcript with normalized name ({LangCode}): {Path}", langCode, candidate.FullName);
                        return File.ReadAllText(candidate.FullName);
                    }
                }

                // Third priority: generic transcript with exact name (video.txt, video.md)
                var exactTxtPath = Path.Combine(searchPath, fileNameWithoutExt + ".txt");
                if (File.Exists(exactTxtPath))
                {
                    Logger.LogInformationWithPath("Found generic transcript: {FilePath}", exactTxtPath);
                    return File.ReadAllText(exactTxtPath);
                }                var exactMdPath = Path.Combine(searchPath, fileNameWithoutExt + ".md");
                if (File.Exists(exactMdPath))
                {
                    Logger.LogInformationWithPath("Found generic transcript: {FilePath}", exactMdPath);
                    return File.ReadAllText(exactMdPath);
                }

                // Fourth priority: generic transcript with normalized name (video_alt.txt, video_alt.md)
                var altTxtPath = Path.Combine(searchPath, altBaseName + ".txt");
                if (File.Exists(altTxtPath))
                {
                    Logger.LogInformationWithPath("Found generic transcript with normalized name: {FilePath}", altTxtPath);
                    return File.ReadAllText(altTxtPath);
                }                var altMdPath = Path.Combine(searchPath, altBaseName + ".md");
                if (File.Exists(altMdPath))
                {
                    Logger.LogInformationWithPath("Found generic transcript with normalized name: {FilePath}", altMdPath);
                    return File.ReadAllText(altMdPath);
                }
            }

            Logger.LogInformationWithPath("No transcript found for video: {FilePath}", videoPath);
            return null;
        }

        /// <summary>
        /// Finds the path to the transcript file for a video without loading its content.
        /// </summary>
        /// <param name="videoPath">Path to the video file.</param>
        /// <returns>The path to the transcript file, or null if no transcript is found.</returns>
        /// <remarks>
        /// This method uses the same search logic as TryLoadTranscript but only returns the file path
        /// instead of loading the content. This is useful for storing the path in metadata.
        /// </remarks>
        private string? FindTranscriptPath(string videoPath)
        {
            if (string.IsNullOrEmpty(videoPath))
            {
                Logger.LogWarning("Empty video path provided to FindTranscriptPath");
                return null;
            }

            string directory = Path.GetDirectoryName(videoPath) ?? string.Empty;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoPath);

            Logger.LogDebug("Looking for transcript file for video: {VideoPath}", videoPath);

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
                    Logger.LogDebugWithPath("Directory does not exist: {FilePath}", searchPath);
                    continue;
                }

                // Get all .txt and .md files in the directory
                var candidates = Directory.GetFiles(searchPath, "*.txt")
                    .Concat(Directory.GetFiles(searchPath, "*.md"))
                    .Select(path => new FileInfo(path))
                    .ToList();

                if (!candidates.Any())
                {
                    Logger.LogDebugWithPath("No transcript candidates found in: {FilePath}", searchPath);
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
                        Logger.LogDebugWithPath($"Found language-specific transcript ({langCode}): {{FilePath}}", candidate.FullName);
                        return candidate.FullName;
                    }
                }

                // Second priority: language-specific transcript with normalized name
                foreach (var candidate in candidates)
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(candidate.Name);
                    if (nameWithoutExt.StartsWith(altBaseName + ".") && IsLikelyLanguageCode(nameWithoutExt.Substring(altBaseName.Length + 1)))
                    {
                        string langCode = nameWithoutExt.Substring(altBaseName.Length + 1);
                        Logger.LogInformation("Found language-specific transcript with normalized name ({LangCode}): {Path}", langCode, candidate.FullName);
                        return candidate.FullName;
                    }
                }

                // Third priority: generic transcript with exact name (video.txt, video.md)
                var exactTxtPath = Path.Combine(searchPath, fileNameWithoutExt + ".txt");
                if (File.Exists(exactTxtPath))
                {
                    Logger.LogInformation("Found generic transcript: {Path}", exactTxtPath);
                    return exactTxtPath;
                }

                var exactMdPath = Path.Combine(searchPath, fileNameWithoutExt + ".md");
                if (File.Exists(exactMdPath))
                {
                    Logger.LogInformation("Found generic transcript: {Path}", exactMdPath);
                    return exactMdPath;
                }

                // Fourth priority: generic transcript with normalized name (video_alt.txt, video_alt.md)
                var altTxtPath = Path.Combine(searchPath, altBaseName + ".txt");
                if (File.Exists(altTxtPath))
                {
                    Logger.LogInformation("Found generic transcript with normalized name: {Path}", altTxtPath);
                    return altTxtPath;
                }

                var altMdPath = Path.Combine(searchPath, altBaseName + ".md");
                if (File.Exists(altMdPath))
                {
                    Logger.LogInformation("Found generic transcript with normalized name: {Path}", altMdPath);
                    return altMdPath;
                }
            }

            Logger.LogInformation("No transcript file found for video: {VideoPath}", videoPath);
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
        /// <param name="resourcesRoot">Optional override for OneDrive fullpath root directory.</param>
        /// <param name="noShareLinks">If true, skips OneDrive share link creation.</param>
        /// <returns>A complete markdown note as a string.</returns>
        public async Task<string> GenerateVideoNoteAsync(
            string videoPath,
            string? openAiApiKey,
            string? promptFileName = null,
            bool noSummary = false,
            int? timeoutSeconds = null,
            string? resourcesRoot = null,
            bool noShareLinks = false)
        {            // Extract metadata and transcript
            var metadata = await ExtractMetadataAsync(videoPath);

            // Find transcript file and store its path in metadata
            string? transcriptPath = FindTranscriptPath(videoPath);
            if (!string.IsNullOrEmpty(transcriptPath))
            {
                metadata["transcript"] = transcriptPath;
                Logger.LogInformationWithPath("Found transcript file and added path to metadata: {FilePath}", transcriptPath);
            }
            string? shareLink = null;
            if (!noShareLinks && _oneDriveService != null)
            {
                try
                {
                    Logger.LogInformationWithPath("Generating OneDrive share link for: {FilePath}", videoPath);
                    shareLink = await _oneDriveService.CreateShareLinkAsync(videoPath);
                    if (!string.IsNullOrEmpty(shareLink))
                    {
                        Logger.LogInformation("OneDrive share link generated successfully");
                        // Store in metadata in the onedrive-shared-link field
                        metadata["onedrive-shared-link"] = shareLink;
                        Logger.LogDebug("Added OneDrive share link to metadata");
                    }
                    else
                    {
                        Logger.LogWarning("Failed to generate OneDrive share link for: {VideoPath}", videoPath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error generating OneDrive share link for: {VideoPath}", videoPath);
                }
            }
            string? transcript = TryLoadTranscript(videoPath);

            // Choose input for summary based on transcript availability
            string summaryInput;
            if (transcript != null && !string.IsNullOrWhiteSpace(transcript))
            {
                summaryInput = transcript;
                Logger.LogInformation("Using transcript for AI summary for video: {VideoPath}", videoPath);
            }
            else
            {
                summaryInput = $"Video file: {Path.GetFileName(videoPath)}\n(No transcript available. Using metadata only.)";
                Logger.LogInformation("No transcript found. Using metadata for AI summary for video: {VideoPath}", videoPath);
            }
            string aiSummary = string.Empty;
            if (noSummary)
            {
                // When no summary is requested, create minimal content with Note section                // Skip AI summarizer entirely to avoid API calls
                aiSummary = "## Note\n\n";

                Logger.LogInformation("Skipping AI summary generation for video (noSummary=true): {VideoPath}", videoPath);
            }
            else
            {
                // Only call AI summarizer when summary is actually requested
                Logger.LogInformation("Generating AI summary for video: {VideoPath}", videoPath);

                // Pass title from metadata for prompt variables
                var promptVariables = new Dictionary<string, string>();
                if (metadata.TryGetValue("title", out var titleObj) && titleObj != null)
                {
                    promptVariables["title"] = titleObj.ToString() ?? "Untitled Video";
                }

                aiSummary = await GenerateAiSummaryAsync(summaryInput, promptVariables, "final_summary_prompt");
            }

            // Add internal path for hierarchy detection (will be removed in GenerateMarkdownNote)
            metadata["_internal_path"] = videoPath;            // Generate the basic markdown note - include title heading using friendly title from metadata
            string markdownNote = GenerateMarkdownNote(aiSummary, metadata, "Video Note", includeNoteTypeTitle: true);

            // Add OneDrive share link section to markdown content if share link was generated
            if (!string.IsNullOrEmpty(shareLink))
            {
                // Find the position to insert the share link (after the summary but before ## Notes)
                var notesPattern = "## Notes";
                int notesIndex = markdownNote.IndexOf(notesPattern);

                if (notesIndex != -1)
                {
                    // Insert share link section before ## Notes
                    string shareSection = $"\n## References\n- [Video Recording]({shareLink})\n\n";
                    markdownNote = markdownNote.Insert(notesIndex, shareSection);
                }
                else
                {
                    // If no ## Notes section found, append at the end
                    string shareSection = $"\n\n## References\n- [Video Recording]({shareLink})\n\n## Notes\n";
                    markdownNote += shareSection;
                }
            }

            return markdownNote;
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