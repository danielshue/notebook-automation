// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves OneDrive resources-relative paths for PDF files, video files, and transcript files.
/// </summary>
/// <remarks>
/// <para>
/// Computes a path relative to the configured OneDrive resources root
/// (paths.onedrive_fullpath_root + paths.onedrive_resources_basepath) using
/// the file path provided in resolver context. If the file is
/// not under the resources root, the original path is returned. If no file
/// path is available, returns an empty string.
/// </para>
/// <para>
/// <b>Supported field names:</b>
/// <list type="bullet">
/// <item><description><c>transcript-onedrive-relative-path</c>: For transcript files</description></item>
/// <item><description><c>pdf-onedrive-relative-path</c>: For PDF files</description></item>
/// <item><description><c>video-onedrive-relative-path</c>: For video files</description></item>
/// <item><description><c>pdftext-onedrive-relative-path</c>: For PDF extracted text files</description></item>
/// <item><description><c>onedrive_relative_path</c>: Generic field (legacy support)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Expected context:</b>
/// <list type="bullet">
/// <item><description><c>transcript</c> (string): Absolute path for transcript files.</description></item>
/// <item><description><c>filePath</c> (string): Absolute path for PDF/video files.</description></item>
/// <item><description><c>extracted_text_file</c> (string): Absolute path for PDF text files.</description></item>
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
    /// Resolve OneDrive resources-relative path for various file types.
    /// </summary>
    /// <param name="fieldName">Schema field name (supports transcript-onedrive-relative-path, pdf-onedrive-relative-path, video-onedrive-relative-path, pdftext-onedrive-relative-path, onedrive_relative_path).</param>
    /// <param name="context">Resolver context containing file path info.</param>
    /// <returns>Relative path if under OneDrive resources root, original path if not, or empty string when unavailable.</returns>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            // Log field name for debugging, but don't gate functionality
            _logger.LogDebug("{Resolver} invoked for field '{Field}'", nameof(OneDriveRelativePathResolver), fieldName);

            if (context == null)
            {
                _logger.LogDebug("{Resolver}: no context provided; returning empty", nameof(OneDriveRelativePathResolver));
                return string.Empty;
            }

            // Determine the appropriate context key based on field name
            string? filePath = GetFilePathFromContext(fieldName, context);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogDebug("{Resolver}: file path not found in context for field '{Field}'; returning empty", nameof(OneDriveRelativePathResolver), fieldName);
                return string.Empty;
            }

            var root = BuildResourcesRoot(_config?.Paths?.OnedriveFullpathRoot, _config?.Paths?.OnedriveResourcesBasepath);
            if (string.IsNullOrWhiteSpace(root))
            {
                _logger.LogDebug("{Resolver}: OneDrive resources root not configured; returning original path for field '{Field}'", nameof(OneDriveRelativePathResolver), fieldName);
                return filePath;
            }

            // Normalize
            string fullFilePath = Path.GetFullPath(filePath);
            string normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;

            if (fullFilePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                string relative = Path.GetRelativePath(root, fullFilePath);
                _logger.LogDebug("Computed OneDrive relative path for field '{Field}': {Relative}", fieldName, relative);
                return relative;
            }

            _logger.LogDebug("File not under OneDrive resources root for field '{Field}'; returning original path: {Path}", fieldName, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"{nameof(OneDriveRelativePathResolver)}: failed to compute relative path for field '{fieldName}'; returning empty");
            return string.Empty;
        }
    }

    /// <summary>
    /// Extract the appropriate file path from context based on the field name.
    /// </summary>

    private string? GetFilePathFromContext(string fieldName, Dictionary<string, object> context)
    {
        return fieldName.ToLowerInvariant() switch
        {
            "transcript-onedrive-relative-path" =>
                TryGetString(context, "transcript") ?? TryGetString(context, "transcript-path"),

            "pdf-onedrive-relative-path" or "video-onedrive-relative-path" or "onedrive_relative_path" =>
                TryGetString(context, "filePath") ?? TryGetString(context, "_internal_path"),

            "pdftext-onedrive-relative-path" =>
                TryGetString(context, "pdftext_file") ?? TryGetString(context, "extracted_text_file"),
            _ =>
                // Fallback: try common context keys
                TryGetString(context, "filePath") ??
                TryGetString(context, "transcript") ??
                TryGetString(context, "_internal_path")
        };
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
