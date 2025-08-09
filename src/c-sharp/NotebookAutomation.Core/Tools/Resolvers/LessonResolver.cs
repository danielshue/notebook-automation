// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves the <c>lesson</c> segment from the file path using the metadata hierarchy detector.
/// </summary>
/// <remarks>
/// <para>
/// <b>Context keys:</b> expects <c>filePath</c> or <c>_internal_path</c> in the resolver context. If neither is provided, no value
/// is produced.
/// </para>
/// <para>
/// <b>Precedence:</b> schema defaults &lt; resolver-derived value &lt; frontmatter &lt; explicit overrides. The pipeline will
/// keep a non-empty frontmatter value or an explicit override in preference to the resolved value.
/// </para>
/// </remarks>
public class LessonResolver(ILogger<LessonResolver> logger, IMetadataHierarchyDetector hierarchy) : IFieldValueResolver
{
    private readonly ILogger<LessonResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMetadataHierarchyDetector _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));

    /// <summary>
    /// Resolves the lesson identifier or name based on the provided path context.
    /// </summary>
    /// <param name="fieldName">The field name to resolve (typically <c>"lesson"</c>).</param>
    /// <param name="context">Resolver context providing <c>filePath</c> or <c>_internal_path</c>.</param>
    /// <returns>The resolved lesson as a string; returns <see cref="string.Empty"/> when not found or on failure.</returns>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            var path = GetPath(context);
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var info = _hierarchy.FindHierarchyInfo(path);
            return info.TryGetValue("lesson", out var value) ? value : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LessonResolver failed; returning empty");
            return string.Empty;
        }
    }

    private static string? GetPath(Dictionary<string, object>? ctx)
    {
        if (ctx == null) return null;
        if (ctx.TryGetValue("filePath", out var fp) && fp is string s1) return s1;
        if (ctx.TryGetValue("_internal_path", out var ip) && ip is string s2) return s2;
        return null;
    }
}
