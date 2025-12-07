// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.VideoTranscriptProcessing;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves the <c>video-onedrive-relative-path</c> frontmatter array for consolidated transcript notes.
/// </summary>
public class VideoSourcesResolver(ILogger<VideoSourcesResolver> logger) : IFieldValueResolver
{
    private readonly ILogger<VideoSourcesResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Resolves video source metadata from the consolidation context.
    /// </summary>
    /// <param name="fieldName">Field being resolved.</param>
    /// <param name="context">Resolver context provided by the metadata pipeline.</param>
    /// <returns>Array of OneDrive-relative video paths captured during consolidation.</returns>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (context == null)
        {
            _logger.LogDebug("No context supplied to {Resolver}", nameof(VideoSourcesResolver));
            return Array.Empty<string>();
        }

        if (!context.TryGetValue(VideoTranscriptConstants.SourcesContextKey, out var value) ||
            value is not IEnumerable<VideoTranscriptSourceEntry> sources)
        {
            _logger.LogDebug("Context missing transcript sources for field {Field}", fieldName);
            return Array.Empty<string>();
        }

        var resolved = sources
            .Select(entry => entry.RelativeVideoPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Replace(Path.DirectorySeparatorChar, '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogDebug("Resolved {Count} consolidated video path(s) for field {Field}", resolved.Length, fieldName);

        return resolved;
    }
}
