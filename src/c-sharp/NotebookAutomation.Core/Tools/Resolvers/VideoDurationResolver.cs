// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using Xabe.FFmpeg;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves the duration of a video file using FFmpeg into a human-readable string.
/// </summary>
/// <remarks>
/// <para>
/// <b>Context:</b> Requires <c>context["filePath"]</c> (absolute path to the video). Accepts <c>_internal_path</c> as an alias.
/// Returns duration as <c>HH:mm:ss</c> when hours are present; otherwise <c>mm:ss</c>.
/// </para>
/// <para>
/// <b>Precedence:</b> Resolver values are merged by <see cref="IMetadataPipeline"/> (schema defaults < resolvers < content/frontmatter < overrides).
/// Returning <see langword="null"/> when context is missing allows higher-precedence sources to win; empty string indicates failure.
/// </para>
/// <example>
/// <code>
/// var ctx = new Dictionary<string, object> { ["filePath"] = @"D:\\media\\clip.mp4" };
/// var duration = (string?)resolver.Resolve("video-duration", ctx); // e.g., "01:23:45"
/// </code>
/// </example>
/// </remarks>
public class VideoDurationResolver(ILogger<VideoDurationResolver> logger) : IFieldValueResolver
{
    private readonly ILogger<VideoDurationResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Resolves the video duration using FFmpeg.
    /// </summary>
    /// <param name="fieldName">The field name to resolve (e.g., <c>"video-duration"</c>).</param>
    /// <param name="context">Context containing <c>filePath</c> for the video file.</param>
    /// <returns>
    /// A duration string formatted as <c>HH:mm:ss</c> when hours are present, otherwise <c>mm:ss</c>.
    /// Returns <see cref="string.Empty"/> on errors, or <see langword="null"/> if <c>filePath</c> is missing.
    /// </returns>
    /// <remarks>
    /// This method blocks on FFmpeg metadata retrieval. Exceptions are caught and logged at Warning level.
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            if (context == null)
            {
                _logger.LogDebug("{Resolver}: filePath missing from context; returning null", nameof(VideoDurationResolver));
                return null;
            }
            // Accept either filePath or _internal_path
            string? path = null;
            if (context.TryGetValue("filePath", out var value) && value is string p1 && !string.IsNullOrWhiteSpace(p1))
                path = p1;
            else if (context.TryGetValue("_internal_path", out var value2) && value2 is string p2 && !string.IsNullOrWhiteSpace(p2))
                path = p2;
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogDebug("{Resolver}: path not provided; returning null");
                return null;
            }

            var task = FFmpeg.GetMediaInfo(path);
            task.Wait();
            var info = task.Result;
            var duration = info?.Duration ?? TimeSpan.Zero;
            string formatted = duration.ToString(duration.TotalHours >= 1 ? "hh\\:mm\\:ss" : "mm\\:ss");
            _logger.LogDebug("Resolved video duration for {Path}: {Duration}", path, formatted);
            return formatted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve video duration; returning empty");
            return string.Empty;
        }
    }
}
