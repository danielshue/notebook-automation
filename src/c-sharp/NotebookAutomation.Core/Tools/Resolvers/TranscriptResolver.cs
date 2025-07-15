// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Specialized resolver for transcript file discovery, loading, and metadata extraction.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TranscriptResolver"/> provides comprehensive transcript file handling,
/// including automatic discovery of transcript files associated with media files, parsing
/// of various transcript formats (SRT, VTT, TXT, JSON), and extraction of transcript metadata
/// such as duration, word count, and speaker information.
/// </para>
/// <para>
/// <b>Required Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>filePath</c> (string): Path to the media file or transcript file</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Optional Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>transcriptPath</c> (string): Explicit path to transcript file</description></item>
/// <item><description><c>searchRadius</c> (int): Directory search radius for transcript discovery (default: 2)</description></item>
/// <item><description><c>extractContent</c> (bool): Whether to extract full transcript content (default: true)</description></item>
/// <item><description><c>extractTimings</c> (bool): Whether to extract timing information (default: true)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Extracted Metadata Fields:</b>
/// <list type="bullet">
/// <item><description><c>transcript-path</c>: Path to the discovered transcript file</description></item>
/// <item><description><c>transcript-format</c>: Format of the transcript (srt, vtt, txt, json)</description></item>
/// <item><description><c>transcript-duration</c>: Duration in seconds (for timed transcripts)</description></item>
/// <item><description><c>transcript-word-count</c>: Total word count in transcript</description></item>
/// <item><description><c>transcript-speaker-count</c>: Number of identified speakers</description></item>
/// <item><description><c>transcript-content</c>: Full transcript text content</description></item>
/// <item><description><c>transcript-segments</c>: List of timed segments (for structured formats)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Supported Transcript Formats:</b>
/// <list type="bullet">
/// <item><description>SRT (SubRip Subtitle): Standard timed subtitle format</description></item>
/// <item><description>VTT (WebVTT): Web Video Text Tracks format</description></item>
/// <item><description>TXT: Plain text transcripts</description></item>
/// <item><description>JSON: Structured transcript data with timing and speaker information</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe for concurrent read operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var resolver = new TranscriptResolver(logger);
/// var context = new Dictionary&lt;string, object&gt; { ["filePath"] = "/path/to/video.mp4" };
/// var metadata = resolver.ExtractMetadata(context);
/// var transcriptPath = metadata.ContainsKey("transcript-path") ? metadata["transcript-path"] : null;
/// </code>
/// </example>
public class TranscriptResolver : IFileTypeMetadataResolver
{
    private readonly ILogger<TranscriptResolver> _logger;

    // Transcript file patterns for discovery
    private static readonly string[] TranscriptExtensions = { ".srt", ".vtt", ".txt", ".json" };
    private static readonly string[] VideoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm" };
    private static readonly string[] AudioExtensions = { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma" };

    // Regex patterns for transcript parsing
    // Updated SRT pattern to match blocks even if not followed by double newline (last block)
    // Updated SRT pattern: matches blocks separated by a single blank line and last block even if only a single newline
    private static readonly Regex SrtPattern = new(@"(\d+)\s*\n(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})\s*\n(.*?)(?:\r?\n\r?\n|\r?\n$|$)", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex VttPattern = new(@"(\d{2}:\d{2}:\d{2}\.\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2}\.\d{3})\s*\n(.*?)\n\n", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TimePattern = new(@"(\d{2}):(\d{2}):(\d{2})[,\.](\d{3})", RegexOptions.Compiled);
    private static readonly Regex SpeakerPattern = new(@"^\s*([A-Z][a-z\s]+):\s*(.*)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex WordCountPattern = new(@"\b\w+\b", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscriptResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic and error information.</param>
    public TranscriptResolver(ILogger<TranscriptResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the file type this resolver handles.
    /// </summary>
    public string FileType => "transcript";

    /// <summary>
    /// Determines whether this resolver can resolve the specified field name given the provided context.
    /// </summary>
    /// <param name="fieldName">The field name to check for resolution capability.</param>
    /// <param name="context">Optional context containing file path and transcript data.</param>
    /// <returns>True if this resolver can resolve the field; otherwise, false.</returns>
    /// <remarks>
    /// This resolver can handle transcript-related fields including path discovery, format detection,
    /// content extraction, and timing analysis. Requires filePath in the context.
    /// </remarks>
    public bool CanResolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (context == null || !context.ContainsKey("filePath"))
            return false;

        // Standard transcript metadata fields this resolver can handle
        var supportedFields = new HashSet<string>
        {
            "transcript-path", "transcript-format", "transcript-duration", "transcript-word-count",
            "transcript-speaker-count", "transcript-content", "transcript-segments", "transcript-exists"
        };

        return supportedFields.Contains(fieldName);
    }

    /// <summary>
    /// Resolves the value for a specific field using transcript discovery and analysis.
    /// </summary>
    /// <param name="fieldName">The field name to resolve.</param>
    /// <param name="context">Context containing file path and resolution parameters.</param>
    /// <returns>The resolved field value or null if not found.</returns>
    /// <remarks>
    /// This method discovers and analyzes transcript files to extract the requested metadata.
    /// It supports automatic transcript discovery for media files and direct transcript analysis.
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (!CanResolve(fieldName, context))
            return null;

        var filePath = context!["filePath"] as string;
        if (string.IsNullOrEmpty(filePath))
            return null;

        try
        {
            var transcriptPath = DiscoverTranscriptPath(filePath, context);

            if (fieldName == "transcript-exists")
                return transcriptPath != null;

            if (fieldName == "transcript-path")
                return transcriptPath;

            if (string.IsNullOrEmpty(transcriptPath) || !File.Exists(transcriptPath))
                return null;

            var content = File.ReadAllText(transcriptPath);
            var format = GetTranscriptFormat(transcriptPath);

            return fieldName switch
            {
                "transcript-format" => format,
                "transcript-duration" => GetTranscriptDuration(content, format),
                "transcript-word-count" => GetWordCount(content),
                "transcript-speaker-count" => GetSpeakerCount(content),
                "transcript-content" => ExtractPlainTextContent(content, format),
                "transcript-segments" => ExtractSegments(content, format),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving transcript field '{FieldName}' for file '{FilePath}'", fieldName, filePath);
            return null;
        }
    }

    /// <summary>
    /// Extracts comprehensive metadata from the transcript file.
    /// </summary>
    /// <param name="context">Context containing file path and any additional parameters needed for metadata extraction.</param>
    /// <returns>A dictionary containing extracted metadata key-value pairs.</returns>
    /// <remarks>
    /// <para>
    /// This method performs complete transcript metadata extraction, including discovery
    /// of associated transcript files, format detection, content analysis, and timing information.
    /// </para>
    /// <para>
    /// The extraction process adapts based on the transcript format, providing relevant
    /// metadata for each format while maintaining consistent field naming conventions.
    /// </para>
    /// </remarks>
    public Dictionary<string, object> ExtractMetadata(Dictionary<string, object>? context = null)
    {
        var metadata = new Dictionary<string, object>();

        if (context == null || !context.ContainsKey("filePath"))
            return metadata;

        var filePath = context["filePath"] as string;
        if (string.IsNullOrEmpty(filePath))
            return metadata;

        try
        {
            var transcriptPath = DiscoverTranscriptPath(filePath, context);

            metadata["transcript-exists"] = transcriptPath != null;

            if (transcriptPath == null || !File.Exists(transcriptPath))
                return metadata;

            metadata["transcript-path"] = transcriptPath;

            var content = File.ReadAllText(transcriptPath);
            var format = GetTranscriptFormat(transcriptPath);

            metadata["transcript-format"] = format;
            metadata["transcript-word-count"] = GetWordCount(content);
            metadata["transcript-speaker-count"] = GetSpeakerCount(content);

            var duration = GetTranscriptDuration(content, format);
            if (duration.HasValue)
                metadata["transcript-duration"] = duration.Value;

            // Extract content if requested
            var extractContent = !context.ContainsKey("extractContent") ||
                                (context["extractContent"] is bool extract && extract);

            if (extractContent)
            {
                metadata["transcript-content"] = ExtractPlainTextContent(content, format);
            }

            // Extract timing information if requested
            var extractTimings = !context.ContainsKey("extractTimings") ||
                               (context["extractTimings"] is bool timings && timings);

            if (extractTimings)
            {
                var segments = ExtractSegments(content, format);
                if (segments.Any())
                    metadata["transcript-segments"] = segments;
            }

            _logger.LogDebug("Extracted {Count} metadata fields from transcript file '{TranscriptPath}'",
                           metadata.Count, transcriptPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting transcript metadata for file '{FilePath}'", filePath);
        }

        return metadata;
    }

    /// <summary>
    /// Discovers the transcript file path associated with a media file or validates a direct transcript path.
    /// </summary>
    private string? DiscoverTranscriptPath(string filePath, Dictionary<string, object> context)
    {
        // Check if explicit transcript path is provided
        if (context.ContainsKey("transcriptPath") && context["transcriptPath"] is string explicitPath)
        {
            return File.Exists(explicitPath) ? explicitPath : null;
        }

        // Check if the file itself is a transcript
        var extension = Path.GetExtension(filePath);
        if (TranscriptExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return File.Exists(filePath) ? filePath : null;
        }

        // Search for transcript files related to the media file
        var searchRadius = context.ContainsKey("searchRadius") && context["searchRadius"] is int radius ? radius : 2;
        return SearchForTranscriptFile(filePath, searchRadius);
    }

    /// <summary>
    /// Searches for transcript files in the vicinity of a media file.
    /// </summary>
    private string? SearchForTranscriptFile(string mediaFilePath, int searchRadius)
    {
        try
        {
            var mediaFile = new FileInfo(mediaFilePath);
            var baseName = Path.GetFileNameWithoutExtension(mediaFile.Name);
            var searchDirectory = mediaFile.DirectoryName;

            if (string.IsNullOrEmpty(searchDirectory) || !Directory.Exists(searchDirectory))
                return null;

            // Search patterns in order of preference
            var searchPatterns = new[]
            {
            $"{baseName}.srt",
            $"{baseName}.vtt",
            $"{baseName}.txt",
            $"{baseName}.json",
            $"{baseName}_transcript.txt",
            $"{baseName}_transcript.srt",
            $"{baseName}_transcript.vtt"
        };

            // Search in the same directory first
            foreach (var pattern in searchPatterns)
            {
                var transcriptPath = Path.Combine(searchDirectory, pattern);
                if (File.Exists(transcriptPath))
                {
                    _logger.LogDebug("Found transcript file: {TranscriptPath}", transcriptPath);
                    return transcriptPath;
                }
            }

            // Search in subdirectories if search radius allows
            if (searchRadius > 0)
            {
                try
                {
                    var directories = Directory.GetDirectories(searchDirectory, "*", SearchOption.AllDirectories)
                        .Where(d => GetDirectoryDepth(d, searchDirectory) <= searchRadius);

                    foreach (var directory in directories)
                    {
                        foreach (var pattern in searchPatterns)
                        {
                            var transcriptPath = Path.Combine(directory, pattern);
                            if (File.Exists(transcriptPath))
                            {
                                _logger.LogDebug("Found transcript file: {TranscriptPath}", transcriptPath);
                                return transcriptPath;
                            }
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    // Directory doesn't exist, which is expected for non-existent paths
                    _logger.LogDebug("Directory '{SearchDirectory}' does not exist for transcript search", searchDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching subdirectories for transcript files in '{SearchDirectory}'", searchDirectory);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching for transcript file for media file '{MediaFilePath}'", mediaFilePath);
            return null;
        }
    }

    /// <summary>
    /// Gets the directory depth relative to a base directory.
    /// </summary>
    private int GetDirectoryDepth(string directory, string baseDirectory)
    {
        var relativePath = Path.GetRelativePath(baseDirectory, directory);
        return relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Determines the transcript format based on file extension and content.
    /// </summary>
    private string GetTranscriptFormat(string transcriptPath)
    {
        var extension = Path.GetExtension(transcriptPath).ToLowerInvariant();
        return extension switch
        {
            ".srt" => "srt",
            ".vtt" => "vtt",
            ".json" => "json",
            ".txt" => "txt",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Extracts the duration from a timed transcript.
    /// </summary>
    private double? GetTranscriptDuration(string content, string format)
    {
        try
        {
            return format switch
            {
                "srt" => GetSrtDuration(content),
                "vtt" => GetVttDuration(content),
                "json" => GetJsonDuration(content),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract duration from transcript");
            return null;
        }
    }

    /// <summary>
    /// Gets duration from SRT format.
    /// </summary>
    private double? GetSrtDuration(string content)
    {
        var matches = SrtPattern.Matches(content);
        if (!matches.Any())
            return null;

        var lastMatch = matches.Last();
        var endTime = lastMatch.Groups[3].Value;
        return ParseTimeToSeconds(endTime);
    }

    /// <summary>
    /// Gets duration from VTT format.
    /// </summary>
    private double? GetVttDuration(string content)
    {
        var matches = VttPattern.Matches(content);
        if (!matches.Any())
            return null;

        var lastMatch = matches.Last();
        var endTime = lastMatch.Groups[2].Value;
        return ParseTimeToSeconds(endTime);
    }

    /// <summary>
    /// Gets duration from JSON format.
    /// </summary>
    private double? GetJsonDuration(string content)
    {
        try
        {
            var json = JsonDocument.Parse(content);
            if (json.RootElement.TryGetProperty("duration", out var durationProp))
                return durationProp.GetDouble();

            // Try to find the last segment
            if (json.RootElement.TryGetProperty("segments", out var segmentsProp) && segmentsProp.ValueKind == JsonValueKind.Array)
            {
                var segments = segmentsProp.EnumerateArray();
                var lastSegment = segments.LastOrDefault();
                if (lastSegment.TryGetProperty("end", out var endProp))
                    return endProp.GetDouble();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse JSON transcript for duration");
        }

        return null;
    }

    /// <summary>
    /// Parses time string to seconds.
    /// </summary>
    private double ParseTimeToSeconds(string timeString)
    {
        var match = TimePattern.Match(timeString);
        if (!match.Success)
            return 0;

        var hours = int.Parse(match.Groups[1].Value);
        var minutes = int.Parse(match.Groups[2].Value);
        var seconds = int.Parse(match.Groups[3].Value);
        var milliseconds = int.Parse(match.Groups[4].Value);

        return hours * 3600 + minutes * 60 + seconds + milliseconds / 1000.0;
    }

    /// <summary>
    /// Counts words in the transcript content.
    /// </summary>
    private int GetWordCount(string content)
    {
        var plainText = StripTimingInformation(content);
        // Remove empty lines and trim whitespace
        var lines = plainText.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l));
        var wordCount = 0;
        foreach (var line in lines)
        {
            wordCount += WordCountPattern.Matches(line).Count;
        }
        return wordCount;
    }

    /// <summary>
    /// Counts the number of speakers in the transcript.
    /// </summary>
    private int GetSpeakerCount(string content)
    {
        var speakers = new HashSet<string>();
        var matches = SpeakerPattern.Matches(content);

        foreach (Match match in matches)
        {
            var speaker = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(speaker))
                speakers.Add(speaker);
        }

        return speakers.Count;
    }

    /// <summary>
    /// Extracts plain text content from the transcript, removing timing information.
    /// </summary>
    private string ExtractPlainTextContent(string content, string format)
    {
        return format switch
        {
            "srt" => ExtractSrtText(content),
            "vtt" => ExtractVttText(content),
            "json" => ExtractJsonText(content),
            "txt" => content,
            _ => content
        };
    }

    /// <summary>
    /// Extracts text from SRT format.
    /// </summary>
    private string ExtractSrtText(string content)
    {
        var matches = SrtPattern.Matches(content);
        var textParts = matches.Select(match => match.Groups[4].Value.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
        return string.Join("\n", textParts);
    }

    /// <summary>
    /// Extracts text from VTT format.
    /// </summary>
    private string ExtractVttText(string content)
    {
        var matches = VttPattern.Matches(content);
        var textParts = matches.Select(match => match.Groups[3].Value.Trim()).ToArray();
        return string.Join("\n", textParts);
    }

    /// <summary>
    /// Extracts text from JSON format.
    /// </summary>
    private string ExtractJsonText(string content)
    {
        try
        {
            var json = JsonDocument.Parse(content);
            var textParts = new List<string>();

            if (json.RootElement.TryGetProperty("text", out var textProp))
                return textProp.GetString() ?? "";

            if (json.RootElement.TryGetProperty("segments", out var segmentsProp) && segmentsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var segment in segmentsProp.EnumerateArray())
                {
                    if (segment.TryGetProperty("text", out var segmentTextProp))
                        textParts.Add(segmentTextProp.GetString() ?? "");
                }
            }

            return string.Join("\n", textParts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse JSON transcript for text extraction");
            return "";
        }
    }

    /// <summary>
    /// Extracts timed segments from the transcript.
    /// </summary>
    private List<object> ExtractSegments(string content, string format)
    {
        try
        {
            return format switch
            {
                "srt" => ExtractSrtSegments(content),
                "vtt" => ExtractVttSegments(content),
                "json" => ExtractJsonSegments(content),
                _ => new List<object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not extract segments from transcript");
            return new List<object>();
        }
    }

    /// <summary>
    /// Extracts segments from SRT format.
    /// </summary>
    private List<object> ExtractSrtSegments(string content)
    {
        var segments = new List<object>();
        var matches = SrtPattern.Matches(content);

        foreach (Match match in matches)
        {
            var segmentText = match.Groups[4].Value.Trim();
            if (!string.IsNullOrWhiteSpace(segmentText))
            {
                var segment = new Dictionary<string, object>
                {
                    { "start", ParseTimeToSeconds(match.Groups[2].Value) },
                    { "end", ParseTimeToSeconds(match.Groups[3].Value) },
                    { "text", segmentText }
                };
                segments.Add(segment);
            }
        }

        return segments;
    }

    /// <summary>
    /// Extracts segments from VTT format.
    /// </summary>
    private List<object> ExtractVttSegments(string content)
    {
        var segments = new List<object>();
        var matches = VttPattern.Matches(content);

        foreach (Match match in matches)
        {
            var segmentText = match.Groups[3].Value.Trim();
            if (!string.IsNullOrWhiteSpace(segmentText))
            {
                var segment = new Dictionary<string, object>
                {
                    { "start", ParseTimeToSeconds(match.Groups[1].Value) },
                    { "end", ParseTimeToSeconds(match.Groups[2].Value) },
                    { "text", segmentText }
                };
                segments.Add(segment);
            }
        }

        return segments;
    }

    /// <summary>
    /// Extracts segments from JSON format.
    /// </summary>
    private List<object> ExtractJsonSegments(string content)
    {
        var segments = new List<object>();

        try
        {
            var json = JsonDocument.Parse(content);
            if (json.RootElement.TryGetProperty("segments", out var segmentsProp) && segmentsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var segment in segmentsProp.EnumerateArray())
                {
                    var segmentObj = new Dictionary<string, object>();

                    if (segment.TryGetProperty("start", out var startProp))
                        segmentObj["start"] = startProp.GetDouble();

                    if (segment.TryGetProperty("end", out var endProp))
                        segmentObj["end"] = endProp.GetDouble();

                    if (segment.TryGetProperty("text", out var textProp))
                        segmentObj["text"] = textProp.GetString() ?? "";

                    segments.Add(segmentObj);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse JSON transcript segments");
        }

        return segments;
    }

    /// <summary>
    /// Strips timing information from transcript content.
    /// </summary>
    private string StripTimingInformation(string content)
    {
        // Remove SRT timing
        content = SrtPattern.Replace(content, "$4\n");

        // Remove VTT timing
        content = VttPattern.Replace(content, "$3\n");

        // Remove common timing patterns
        content = Regex.Replace(content, @"\d{2}:\d{2}:\d{2}[,\.]\d{3}", "");
        content = Regex.Replace(content, @"-->\s*", "");

        return content.Trim();
    }
}
