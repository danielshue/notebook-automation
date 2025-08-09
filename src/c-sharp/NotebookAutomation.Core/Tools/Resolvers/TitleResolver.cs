// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves a human-friendly title for notes based on the source file path.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="FriendlyTitleHelper"/> to generate titles from file names. If no file path is present
/// in the resolver context (expects <c>"filePath"</c> or <c>"_internal_path"</c>), this returns <see langword="null"/>
/// so that precedence rules can apply (e.g., frontmatter or explicit overrides).
/// </para>
/// <para>
/// <b>Precedence:</b> Values are composed by <see cref="IMetadataPipeline"/> using the order:
/// schema defaults < resolvers < content/frontmatter < explicit overrides. Returning <see langword="null"/>
/// allows higher-precedence sources to take effect.
/// </para>
/// </remarks>
public class TitleResolver(ILogger<TitleResolver> logger) : IFieldValueResolver
{
    private readonly ILogger<TitleResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Resolves a friendly, human-readable title for the specified metadata field based on the provided context.
    /// </summary>
    /// <param name="fieldName">The name of the field being resolved, typically <c>"title"</c>.</param>
    /// <param name="context">
    /// Optional resolution context. This resolver looks for a file path under the keys
    /// <c>"filePath"</c> or <c>"_internal_path"</c> to derive the title from the source filename.
    /// </param>
    /// <returns>
    /// The resolved title string when a file path is available and can be parsed; otherwise, <see langword="null"/> to allow
    /// precedence rules to promote frontmatter or schema defaults.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method removes common structural prefixes (for example, <c>lesson</c>, <c>module</c>, <c>class</c>, <c>week</c>, <c>lecture</c>)
    /// from the start of the filename before delegating to <see cref="FriendlyTitleHelper.GetFriendlyTitleFromFileName(string)"/>.
    /// </para>
    /// <para>
    /// If the context does not contain a usable file path, the resolver returns <see langword="null"/> so that the pipeline's
    /// precedence rules can apply (frontmatter and explicit overrides take priority over schema defaults).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var ctx = new Dictionary<string, object> { ["filePath"] = "C:/notes/module_02-roi-analysis.md" };
    /// var title = (string?)new TitleResolver(logger).Resolve("title", ctx);
    /// // title == "ROI Analysis"
    /// </code>
    /// </example>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            string? path = null;
            if (context != null)
            {
                if (context.TryGetValue("filePath", out var fp) && fp is not null)
                {
                    path = fp.ToString();
                }
                else if (context.TryGetValue("_internal_path", out var ip) && ip is not null)
                {
                    path = ip.ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogDebug($"TitleResolver had no path context; returning null for {fieldName}");
                return null; // Defer to frontmatter/overrides; avoids hard-coded defaults
            }

            string name = Path.GetFileNameWithoutExtension(path);
            // Remove common structural prefixes so that leading numbers can be stripped by the helper
            // e.g., "lesson_02-roi-analysis" -> "02-roi-analysis"
            name = Regex.Replace(name, @"^(lesson|lessons|module|modules|class|classes|week|lecture)[_\-\s]+", string.Empty, RegexOptions.IgnoreCase);
            string title = FriendlyTitleHelper.GetFriendlyTitleFromFileName(name);
            _logger.LogDebug($"Resolved {fieldName} to '{title}' from path '{path}'");
            return title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"TitleResolver failed for field {fieldName}");
            return null;
        }
    }
}
