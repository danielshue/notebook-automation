// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves a OneDrive resources-relative path for an associated transcript file.
/// </summary>
/// <remarks>
/// <para>
/// Computes a path relative to the configured OneDrive resources root
/// (paths.onedrive_fullpath_root + paths.onedrive_resources_basepath) using
/// the transcript path provided in resolver context. If the transcript is
/// not under the resources root, the original path is returned. If no transcript
/// path is available, returns an empty string.
/// </para>
/// <para>
/// <b>Expected context:</b>
/// <list type="bullet">
/// <item><description><c>transcript</c> (string, preferred): Absolute path captured by processors.</description></item>
/// <item><description><c>transcript-path</c> (string, optional): Value produced by TranscriptResolver.</description></item>
/// </list>
/// </para>
/// </remarks>
public class OneDriveRelativePathResolver : IFieldValueResolver
{
    private readonly ILogger<OneDriveRelativePathResolver> _logger;
    private readonly AppConfig _config;

    public OneDriveRelativePathResolver(ILogger<OneDriveRelativePathResolver> logger, AppConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Resolve OneDrive resources-relative path for the transcript.
    /// </summary>
    /// <param name="fieldName">Schema field name (expected: "transcript-onedrive-relative-path").</param>
    /// <param name="context">Resolver context containing transcript path info.</param>
    /// <returns>Relative path if under OneDrive resources root, original path if not, or empty string when unavailable.</returns>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            // Gate by field name for clarity, but still compute if called for other names.
            if (!string.Equals(fieldName, "transcript-onedrive-relative-path", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("{Resolver} invoked for field '{Field}'", nameof(OneDriveRelativePathResolver), fieldName);
            }

            if (context == null)
            {
                _logger.LogDebug("{Resolver}: no context provided; returning empty", nameof(OneDriveRelativePathResolver));
                return string.Empty;
            }

            // Prefer in-memory key set by processors; fall back to transcript-path (from TranscriptResolver)
            var transcriptPath = TryGetString(context, "transcript")
                                ?? TryGetString(context, "transcript-path");

            if (string.IsNullOrWhiteSpace(transcriptPath))
            {
                _logger.LogDebug("{Resolver}: transcript path not found in context; returning empty", nameof(OneDriveRelativePathResolver));
                return string.Empty;
            }

            var root = BuildResourcesRoot(_config?.Paths?.OnedriveFullpathRoot, _config?.Paths?.OnedriveResourcesBasepath);
            if (string.IsNullOrWhiteSpace(root))
            {
                _logger.LogDebug("{Resolver}: OneDrive resources root not configured; returning original path");
                return transcriptPath;
            }

            // Normalize
            string fullTranscriptPath = Path.GetFullPath(transcriptPath);
            string normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;

            if (fullTranscriptPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                string relative = Path.GetRelativePath(root, fullTranscriptPath);
                _logger.LogDebug("Computed OneDrive relative transcript path: {Relative}", relative);
                return relative;
            }

            _logger.LogDebug("Transcript not under OneDrive resources root; returning original path: {Path}", transcriptPath);
            return transcriptPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"{nameof(OneDriveRelativePathResolver)}: failed to compute relative path; returning empty");
            return string.Empty;
        }
    }

    private static string? TryGetString(IDictionary<string, object> dict, string key)
        => dict.TryGetValue(key, out var val) && val is string s && !string.IsNullOrWhiteSpace(s) ? s : null;

    private static string BuildResourcesRoot(string? oneDriveRoot, string? oneDriveResourcesBase)
    {
        if (string.IsNullOrWhiteSpace(oneDriveRoot)) return string.Empty;

        try
        {
            var basePart = (oneDriveResourcesBase ?? string.Empty)
                .Trim('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var combined = string.IsNullOrEmpty(basePart)
                ? oneDriveRoot
                : Path.Combine(oneDriveRoot, basePart);

            return Path.GetFullPath(combined);
        }
        catch
        {
            return string.Empty;
        }
    }
}
