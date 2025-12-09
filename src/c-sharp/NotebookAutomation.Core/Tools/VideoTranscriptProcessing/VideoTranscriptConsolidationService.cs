// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using NotebookAutomation.Core.Tools.Shared;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.VideoTranscriptProcessing;

/// <summary>
/// Consolidates multiple video transcripts into a single markdown file using existing metadata infrastructure.
/// </summary>
public class VideoTranscriptConsolidationService(
    ILogger<VideoTranscriptConsolidationService> logger,
    AppConfig appConfig,
    MarkdownNoteBuilder markdownNoteBuilder,
    MarkdownParser markdownParser,
    IYamlHelper yamlHelper,
    IMetadataPipeline metadataPipeline,
    VideoNoteProcessor videoNoteProcessor)
{
    private readonly ILogger<VideoTranscriptConsolidationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AppConfig _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
    private readonly MarkdownNoteBuilder _noteBuilder = markdownNoteBuilder ?? throw new ArgumentNullException(nameof(markdownNoteBuilder));
    private readonly MarkdownParser _markdownParser = markdownParser ?? throw new ArgumentNullException(nameof(markdownParser));
    private readonly IYamlHelper _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
    private readonly IMetadataPipeline _metadataPipeline = metadataPipeline ?? throw new ArgumentNullException(nameof(metadataPipeline));
    private readonly VideoNoteProcessor _videoNoteProcessor = videoNoteProcessor ?? throw new ArgumentNullException(nameof(videoNoteProcessor));

    /// <summary>
    /// Executes the transcript consolidation workflow for the provided request.
    /// </summary>
    /// <param name="request">Request describing the folder scope and execution options.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A result describing output location and aggregated transcripts.</returns>
    public async Task<VideoTranscriptConsolidationResult> ConsolidateAsync(
        VideoTranscriptConsolidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveOneDriveRoot = NormalizeExistingPath(_appConfig.Paths?.GetEffectiveOneDriveRoot());
        if (string.IsNullOrWhiteSpace(effectiveOneDriveRoot))
        {
            throw new InvalidOperationException("OneDrive root is not configured. Ensure onedrive_fullpath_root is set in configuration.");
        }

        var effectiveVaultRoot = NormalizeExistingPath(_appConfig.Paths?.GetEffectiveVaultRoot())
            ?? NormalizeExistingPath(_appConfig.Paths?.NotebookVaultFullpathRoot)
            ?? Path.Combine(AppContext.BaseDirectory, "Generated");

        // Resolve input against OneDrive root configuration
        var resolvedInputPath = PathUtils.ResolveInputPath(
            request.InputPath,
            _appConfig.Paths?.OnedriveFullpathRoot,
            _appConfig.Paths?.OnedriveResourcesBasepath);

        if (!Directory.Exists(resolvedInputPath))
        {
            throw new DirectoryNotFoundException($"Input directory not found: {resolvedInputPath}");
        }

        string folderName = new DirectoryInfo(resolvedInputPath).Name;
        string friendlyFolderTitle = FriendlyTitleHelper.GetFriendlyTitleFromFileName(folderName);

        string relativeFolderPath = Path.GetRelativePath(effectiveOneDriveRoot, resolvedInputPath);
        if (relativeFolderPath == ".")
        {
            relativeFolderPath = string.Empty;
        }

        var outputDirectory = string.IsNullOrEmpty(relativeFolderPath)
            ? effectiveVaultRoot
            : Path.Combine(effectiveVaultRoot, relativeFolderPath);

        var safeFileStem = MarkdownParser.SanitizeForFilename(folderName);
        var outputFileName = string.IsNullOrWhiteSpace(safeFileStem)
            ? "consolidated-transcript.md"
            : $"{safeFileStem}-transcript.md";

        var outputPath = Path.Combine(outputDirectory, outputFileName);

        var videoExtensions = (_appConfig.VideoExtensions?.Count > 0
            ? _appConfig.VideoExtensions
            : new List<string> { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm" })
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .Select(ext => ext.ToLowerInvariant())
            .Distinct()
            .ToList();

        var searchOption = request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var candidateVideos = Directory.EnumerateFiles(resolvedInputPath, "*", searchOption)
            .Where(path => videoExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidateVideos.Count == 0)
        {
            _logger.LogWarning("No video files found under {Path}", resolvedInputPath);
        }

        var aggregationEntries = new List<VideoTranscriptSourceEntry>();
        var skippedCount = 0;

        foreach (var videoPath in candidateVideos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string? transcriptPath = _videoNoteProcessor.GetTranscriptPath(videoPath);
                string? transcriptText = _videoNoteProcessor.TryLoadTranscript(videoPath);

                if (string.IsNullOrWhiteSpace(transcriptText))
                {
                    skippedCount++;
                    continue;
                }

                var friendlyTitle = FriendlyTitleHelper.GetFriendlyTitleFromFileName(Path.GetFileNameWithoutExtension(videoPath));
                var anchor = MarkdownParser.SanitizeForFilename(friendlyTitle);

                string relativeVideoPath = PathUtils.MakeRelative(effectiveOneDriveRoot, videoPath)
                    .Replace(Path.DirectorySeparatorChar, '/');
                string? relativeTranscriptPath = transcriptPath != null
                    ? PathUtils.MakeRelative(effectiveOneDriveRoot, transcriptPath)
                        .Replace(Path.DirectorySeparatorChar, '/')
                    : null;

                string? noteLink = BuildNoteLink(effectiveVaultRoot, effectiveOneDriveRoot, videoPath);

                aggregationEntries.Add(new VideoTranscriptSourceEntry(
                    friendlyTitle,
                    anchor,
                    relativeVideoPath,
                    relativeTranscriptPath,
                    noteLink,
                    DetectTranscriptLanguage(transcriptPath),
                    transcriptText.TrimEnd()));
            }
            catch (Exception ex)
            {
                skippedCount++;
                _logger.LogWarning(ex, "Failed to aggregate transcript for video {VideoPath}", videoPath);
            }
        }

        if (aggregationEntries.Count == 0)
        {
            _logger.LogInformation("No transcripts discovered for consolidation under {Path}", resolvedInputPath);
        }
        else
        {
            _logger.LogDebug("Collected {Count} transcript source entries for consolidation", aggregationEntries.Count);
        }

        // Build markdown body with TOC and individual sections
        var markdownBody = BuildMarkdownBody(friendlyFolderTitle, aggregationEntries);

        // Prepare metadata and context for pipeline composition
        var metadata = new Dictionary<string, object>
        {
            ["template-type"] = VideoTranscriptConstants.TemplateType,
            ["title"] = $"{friendlyFolderTitle} Video Transcripts",
            ["status"] = "compiled",
            ["tags"] = new[] { "transcript", "consolidated" },
            ["_internal_path"] = outputPath
        };

        var pipelineContext = new Dictionary<string, object>
        {
            [VideoTranscriptConstants.SourcesContextKey] = aggregationEntries,
            ["_internal_path"] = outputPath,
            ["filePath"] = outputPath,
            ["skip_onedrive_share_link"] = true
        };

        var composed = _metadataPipeline.Compose(markdownBody, metadata, "Transcript", pipelineContext);
        var finalMetadata = composed.Metadata;

        string[] metadataKeysToRemove =
        {
            "video-duration",
            "video_duration",
            "transcript-onedrive-relative-path",
            "transcript_onedrive_relative_path",
            "share-link",
            "share_link",
            "onedrive-shared-link",
            "onedrive_shared_link",
            "skip_onedrive_share_link"
        };

        foreach (var key in metadataKeysToRemove)
        {
            finalMetadata.Remove(key);
        }

        var videoPathArray = aggregationEntries
            .Select(entry => entry.RelativeVideoPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Replace(Path.DirectorySeparatorChar, '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (videoPathArray.Length > 0)
        {
            _logger.LogDebug("Persisting {Count} video path(s) into metadata", videoPathArray.Length);
            finalMetadata["video-onedrive-relative-path"] = videoPathArray
                .Select(path => (object)path)
                .ToArray();
        }
        else
        {
            finalMetadata.Remove("video-onedrive-relative-path");
        }
        var bodyWithoutFrontmatter = composed.CleanBody;

        if (!finalMetadata.ContainsKey("template-type"))
        {
            finalMetadata["template-type"] = VideoTranscriptConstants.TemplateType;
        }

        var finalMarkdown = _noteBuilder.BuildNote(finalMetadata, bodyWithoutFrontmatter, outputFileName);

        bool shouldWrite = true;
        if (File.Exists(outputPath) && !request.Force)
        {
            try
            {
                var existing = await _markdownParser.ParseFileAsync(outputPath).ConfigureAwait(false);
                if (TryExtractExistingVideoSources(existing.Frontmatter, out var existingSources))
                {
                    var newSourceSet = aggregationEntries.Select(e => e.RelativeVideoPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (existingSources.SetEquals(newSourceSet))
                    {
                        shouldWrite = false;
                        _logger.LogInformation("Skipping write for {OutputPath} because video sources are unchanged.", outputPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Unable to compare existing consolidated transcript; will regenerate file.");
            }
        }

        if (shouldWrite && !request.DryRun)
        {
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(outputPath, finalMarkdown, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Wrote consolidated transcript to {OutputPath}", outputPath);
        }

        return new VideoTranscriptConsolidationResult(
            outputPath,
            aggregationEntries.Count,
            skippedCount,
            shouldWrite && !request.DryRun,
            aggregationEntries);
    }

    private static string? DetectTranscriptLanguage(string? transcriptPath)
    {
        if (string.IsNullOrWhiteSpace(transcriptPath))
        {
            return null;
        }

        var name = Path.GetFileNameWithoutExtension(transcriptPath);
        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length > 1)
        {
            var candidate = parts[^1];
            if (candidate.Length >= 2 && candidate.Length <= 5)
            {
                return candidate.ToLowerInvariant();
            }
        }

        return null;
    }

    private static string BuildMarkdownBody(string folderTitle, IReadOnlyList<VideoTranscriptSourceEntry> entries)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# {folderTitle} Video Transcripts");
        builder.AppendLine();

        if (entries.Count > 0)
        {
            builder.AppendLine("## Table of Contents");
            foreach (var entry in entries)
            {
                var headingLink = entry.FriendlyTitle;
                builder.AppendLine($"- [[#{headingLink}|{entry.FriendlyTitle}]]");
            }
            builder.AppendLine();
        }
        else
        {
            builder.AppendLine("> ⚠️ No transcripts were found for the selected folder.");
            builder.AppendLine();
        }

        foreach (var entry in entries)
        {
            builder.AppendLine($"## {entry.FriendlyTitle}");
            var metadataLines = new List<string>();

            var videoLink = FormatPathAsMarkdownLink(
                entry.RelativeVideoPath,
                CreateFriendlyTitleFromPath(entry.RelativeVideoPath, "Video"));

            if (!string.IsNullOrWhiteSpace(videoLink))
            {
                metadataLines.Add($"> 🎥 **Video**: {videoLink}");
            }

            if (!string.IsNullOrWhiteSpace(entry.RelativeTranscriptPath))
            {
                var transcriptLink = FormatPathAsMarkdownLink(
                    entry.RelativeTranscriptPath!,
                    CreateFriendlyTitleFromPath(entry.RelativeTranscriptPath, "Transcript"));

                if (!string.IsNullOrWhiteSpace(transcriptLink))
                {
                    metadataLines.Add($"> 📄 **Transcript**: {transcriptLink}");
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.NoteLink))
            {
                metadataLines.Add($"> 📘 **Video Note**: {entry.NoteLink}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Language))
            {
                metadataLines.Add($"> 🗣 **Language**: `{entry.Language}`");
            }

            foreach (var line in metadataLines)
            {
                builder.AppendLine(line);
            }

            builder.AppendLine();
            builder.AppendLine(entry.TranscriptContent);
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string FormatPathAsMarkdownLink(string? path, string? friendlyLabel)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var label = string.IsNullOrWhiteSpace(friendlyLabel)
            ? CreateFriendlyTitleFromPath(path)
            : friendlyLabel;

        if (string.IsNullOrWhiteSpace(label))
        {
            label = path;
        }

        return $"[{label}](<{path}>)";
    }

    private static string CreateFriendlyTitleFromPath(string? path, string? suffix = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var fileName = segments.LastOrDefault() ?? path;

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        if (baseName.Contains('.'))
        {
            var lastDot = baseName.LastIndexOf('.');
            if (lastDot > 0)
            {
                baseName = baseName[..lastDot];
            }
        }

        var friendly = FriendlyTitleHelper.GetFriendlyTitleFromFileName(baseName);

        if (!string.IsNullOrWhiteSpace(suffix) && !friendly.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            friendly = string.IsNullOrWhiteSpace(friendly)
                ? suffix
                : $"{friendly} {suffix}";
        }

        return friendly;
    }

    private static string? BuildNoteLink(string vaultRoot, string oneDriveRoot, string videoPath)
    {
        if (string.IsNullOrWhiteSpace(vaultRoot) || string.IsNullOrWhiteSpace(oneDriveRoot))
        {
            return null;
        }

        try
        {
            var relativeVideoDir = Path.GetDirectoryName(Path.GetRelativePath(oneDriveRoot, videoPath)) ?? string.Empty;
            var noteFileName = Path.GetFileNameWithoutExtension(videoPath) + "-video.md";

            var noteFullPath = string.IsNullOrEmpty(relativeVideoDir)
                ? Path.Combine(vaultRoot, noteFileName)
                : Path.Combine(vaultRoot, relativeVideoDir, noteFileName);

            if (!File.Exists(noteFullPath))
            {
                return null;
            }

            var relativeToVault = Path.GetRelativePath(vaultRoot, noteFullPath)
                .Replace(Path.DirectorySeparatorChar, '/');

            if (relativeToVault.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                relativeToVault = relativeToVault[..^3];
            }

            var lastSegment = relativeToVault.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            string? friendlyTitle = null;

            if (!string.IsNullOrWhiteSpace(lastSegment))
            {
                friendlyTitle = FriendlyTitleHelper.GetFriendlyTitleFromFileName(lastSegment);
            }

            if (!string.IsNullOrWhiteSpace(friendlyTitle))
            {
                return $"[[{relativeToVault}|{friendlyTitle}]]";
            }

            return $"[[{relativeToVault}]]";
        }
        catch
        {
            return null;
        }
    }

    private bool TryExtractExistingVideoSources(
        IReadOnlyDictionary<string, object> frontmatter,
        out HashSet<string> existing)
    {
        existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!frontmatter.TryGetValue("video-onedrive-relative-path", out var sourcesObj) || sourcesObj is null)
        {
            return false;
        }

        try
        {
            if (sourcesObj is IEnumerable<object> list)
            {
                foreach (var item in list)
                {
                    if (item is string pathValue && !string.IsNullOrWhiteSpace(pathValue))
                    {
                        existing.Add(pathValue);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to interpret existing video_sources metadata");
            return false;
        }

        return existing.Count > 0;
    }

    private static string? NormalizeExistingPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetFullPath(path);
    }
}
