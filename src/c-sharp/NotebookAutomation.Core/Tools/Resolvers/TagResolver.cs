// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Specialized resolver for tag validation, normalization, and hierarchical tag management.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TagResolver"/> provides comprehensive tag processing capabilities including
/// validation against reserved tags, normalization of tag formats, hierarchical tag structure
/// management, and intelligent tag suggestion based on content analysis and existing tag patterns.
/// </para>
/// <para>
/// <b>Required Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>tags</c> (IEnumerable&lt;string&gt;): List of tags to process</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Optional Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>reservedTags</c> (IEnumerable&lt;string&gt;): List of reserved tags to validate against</description></item>
/// <item><description><c>existingTags</c> (IEnumerable&lt;string&gt;): Existing tags for suggestion and validation</description></item>
/// <item><description><c>content</c> (string): Content to analyze for tag suggestions</description></item>
/// <item><description><c>tagSeparator</c> (string): Separator for hierarchical tags (default: "/")</description></item>
/// <item><description><c>normalizeCase</c> (bool): Whether to normalize tag case (default: true)</description></item>
/// <item><description><c>validateReserved</c> (bool): Whether to validate against reserved tags (default: true)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Extracted Metadata Fields:</b>
/// <list type="bullet">
/// <item><description><c>normalized-tags</c>: List of normalized and validated tags</description></item>
/// <item><description><c>invalid-tags</c>: List of tags that failed validation</description></item>
/// <item><description><c>suggested-tags</c>: List of suggested tags based on content analysis</description></item>
/// <item><description><c>hierarchical-tags</c>: Tags organized in hierarchical structure</description></item>
/// <item><description><c>tag-count</c>: Total number of valid tags</description></item>
/// <item><description><c>tag-hierarchy-depth</c>: Maximum depth of tag hierarchy</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Tag Normalization Rules:</b>
/// <list type="bullet">
/// <item><description>Convert to lowercase (configurable)</description></item>
/// <item><description>Replace spaces with hyphens</description></item>
/// <item><description>Remove special characters except hyphens and forward slashes</description></item>
/// <item><description>Trim leading/trailing whitespace and separators</description></item>
/// <item><description>Remove duplicate tags</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe for concurrent read operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var resolver = new TagResolver(logger);
/// var context = new Dictionary&lt;string, object&gt; 
/// { 
///     ["tags"] = new[] { "Machine Learning", "AI/Deep Learning", "python" },
///     ["reservedTags"] = new[] { "auto-generated" }
/// };
/// var metadata = resolver.ExtractMetadata(context);
/// var normalizedTags = metadata["normalized-tags"];
/// </code>
/// </example>
public class TagResolver : IFileTypeMetadataResolver
{
    private readonly ILogger<TagResolver> _logger;

    // Regex patterns for tag processing
    private static readonly Regex InvalidCharsPattern = new(@"[^\w\s\-\/]", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex WordBoundaryPattern = new(@"\b\w+\b", RegexOptions.Compiled);

    // Common academic and technical keywords for tag suggestions
    private static readonly HashSet<string> CommonKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "machine", "learning", "artificial", "intelligence", "deep", "neural", "network",
        "algorithm", "data", "science", "python", "javascript", "programming", "software",
        "development", "web", "mobile", "database", "security", "cloud", "devops",
        "mathematics", "statistics", "physics", "chemistry", "biology", "research",
        "university", "course", "lecture", "assignment", "project", "presentation"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TagResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic and error information.</param>
    public TagResolver(ILogger<TagResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the file type this resolver handles.
    /// </summary>
    public string FileType => "tag";

    /// <summary>
    /// Determines whether this resolver can resolve the specified field name given the provided context.
    /// </summary>
    /// <param name="fieldName">The field name to check for resolution capability.</param>
    /// <param name="context">Optional context containing tags and validation parameters.</param>
    /// <returns>True if this resolver can resolve the field; otherwise, false.</returns>
    /// <remarks>
    /// This resolver can handle tag-related fields including normalization, validation,
    /// suggestion, and hierarchical organization. Requires tags in the context.
    /// </remarks>
    public bool CanResolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (context == null || !context.ContainsKey("tags"))
            return false;

        // Standard tag metadata fields this resolver can handle
        var supportedFields = new HashSet<string>
        {
            "normalized-tags", "invalid-tags", "suggested-tags", "hierarchical-tags",
            "tag-count", "tag-hierarchy-depth", "tag-validation-report"
        };

        return supportedFields.Contains(fieldName);
    }

    /// <summary>
    /// Resolves the value for a specific field using tag validation and normalization.
    /// </summary>
    /// <param name="fieldName">The field name to resolve.</param>
    /// <param name="context">Context containing tags and validation parameters.</param>
    /// <returns>The resolved field value or null if not found.</returns>
    /// <remarks>
    /// This method processes tags according to the requested field, including normalization,
    /// validation against reserved tags, and hierarchical organization.
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (!CanResolve(fieldName, context))
            return null;

        try
        {
            var tags = ExtractTags(context!);
            if (!tags.Any())
                return fieldName == "tag-count" ? 0 : null;

            var normalizeCase = !context!.ContainsKey("normalizeCase") ||
                               (context["normalizeCase"] is bool normalize && normalize);
            var validateReserved = !context.ContainsKey("validateReserved") ||
                                  (context["validateReserved"] is bool validate && validate);
            var reservedTags = ExtractReservedTags(context);
            var tagSeparator = context.ContainsKey("tagSeparator") && context["tagSeparator"] is string sep ? sep : "/";

            var normalizedTags = NormalizeTags(tags, normalizeCase, tagSeparator);
            var invalidTags = validateReserved ? ValidateAgainstReserved(normalizedTags, reservedTags) : new List<string>();
            var validTags = normalizedTags.Except(invalidTags).ToList();

            return fieldName switch
            {
                "normalized-tags" => validTags,
                "invalid-tags" => invalidTags,
                "suggested-tags" => GenerateSuggestedTags(context),
                "hierarchical-tags" => OrganizeHierarchically(validTags, tagSeparator),
                "tag-count" => validTags.Count,
                "tag-hierarchy-depth" => GetMaxHierarchyDepth(validTags, tagSeparator),
                "tag-validation-report" => GenerateValidationReport(tags, normalizedTags, invalidTags),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tag field '{FieldName}'", fieldName);
            return null;
        }
    }

    /// <summary>
    /// Extracts comprehensive metadata from the tag processing workflow.
    /// </summary>
    /// <param name="context">Context containing tags and any additional parameters needed for metadata extraction.</param>
    /// <returns>A dictionary containing extracted metadata key-value pairs.</returns>
    /// <remarks>
    /// <para>
    /// This method performs complete tag processing including normalization, validation,
    /// hierarchical organization, and suggestion generation. It provides a comprehensive
    /// view of the tag processing results.
    /// </para>
    /// <para>
    /// The processing workflow includes tag normalization, validation against reserved tags,
    /// hierarchical structure analysis, and intelligent tag suggestions based on content analysis.
    /// </para>
    /// </remarks>
    public Dictionary<string, object> ExtractMetadata(Dictionary<string, object>? context = null)
    {
        var metadata = new Dictionary<string, object>();

        if (context == null || !context.ContainsKey("tags"))
            return metadata;

        try
        {
            var tags = ExtractTags(context);
            if (!tags.Any())
            {
                metadata["tag-count"] = 0;
                metadata["normalized-tags"] = new List<string>();
                return metadata;
            }

            var normalizeCase = !context.ContainsKey("normalizeCase") ||
                               (context["normalizeCase"] is bool normalize && normalize);
            var validateReserved = !context.ContainsKey("validateReserved") ||
                                  (context["validateReserved"] is bool validate && validate);
            var reservedTags = ExtractReservedTags(context);
            var tagSeparator = context.ContainsKey("tagSeparator") && context["tagSeparator"] is string sep ? sep : "/";

            // Perform tag normalization
            var normalizedTags = NormalizeTags(tags, normalizeCase, tagSeparator);

            // Validate against reserved tags
            var invalidTags = validateReserved ? ValidateAgainstReserved(normalizedTags, reservedTags) : new List<string>();
            var validTags = normalizedTags.Except(invalidTags).ToList();

            // Build metadata
            metadata["normalized-tags"] = validTags;
            metadata["tag-count"] = validTags.Count;

            if (invalidTags.Any())
                metadata["invalid-tags"] = invalidTags;

            // Hierarchical organization
            var hierarchicalTags = OrganizeHierarchically(validTags, tagSeparator);
            if (hierarchicalTags.Any())
            {
                metadata["hierarchical-tags"] = hierarchicalTags;
                metadata["tag-hierarchy-depth"] = GetMaxHierarchyDepth(validTags, tagSeparator);
            }

            // Generate suggestions if content is available
            var suggestedTags = GenerateSuggestedTags(context);
            if (suggestedTags.Any())
                metadata["suggested-tags"] = suggestedTags;

            // Generate validation report
            metadata["tag-validation-report"] = GenerateValidationReport(tags, normalizedTags, invalidTags);

            _logger.LogDebug("Processed {OriginalCount} tags, normalized to {NormalizedCount} valid tags",
                           tags.Count, validTags.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tag metadata");
        }

        return metadata;
    }

    /// <summary>
    /// Extracts tags from the context.
    /// </summary>
    private List<string> ExtractTags(Dictionary<string, object> context)
    {
        var tags = new List<string>();

        if (context["tags"] is IEnumerable<string> stringTags)
        {
            tags.AddRange(stringTags);
        }
        else if (context["tags"] is IEnumerable<object> objectTags)
        {
            tags.AddRange(objectTags.Select(t => t?.ToString()).Where(t => !string.IsNullOrEmpty(t))!);
        }
        else if (context["tags"] is string singleTag)
        {
            tags.Add(singleTag);
        }

        return tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
    }

    /// <summary>
    /// Extracts reserved tags from the context.
    /// </summary>
    private List<string> ExtractReservedTags(Dictionary<string, object> context)
    {
        var reservedTags = new List<string>();

        if (context.ContainsKey("reservedTags"))
        {
            if (context["reservedTags"] is IEnumerable<string> stringReserved)
            {
                reservedTags.AddRange(stringReserved);
            }
            else if (context["reservedTags"] is IEnumerable<object> objectReserved)
            {
                reservedTags.AddRange(objectReserved.Select(t => t?.ToString()).Where(t => !string.IsNullOrEmpty(t))!);
            }
        }

        return reservedTags;
    }

    /// <summary>
    /// Normalizes tags according to standard conventions.
    /// </summary>
    private List<string> NormalizeTags(List<string> tags, bool normalizeCase, string tagSeparator)
    {
        var normalized = new HashSet<string>();

        foreach (var tag in tags)
        {
            var normalizedTag = NormalizeTag(tag, normalizeCase, tagSeparator);
            if (!string.IsNullOrEmpty(normalizedTag))
            {
                normalized.Add(normalizedTag);
            }
        }

        return normalized.ToList();
    }

    /// <summary>
    /// Normalizes a single tag.
    /// </summary>
    private string NormalizeTag(string tag, bool normalizeCase, string tagSeparator)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return string.Empty;

        // Remove invalid characters but preserve the tag separator
        var escapedSeparator = Regex.Escape(tagSeparator);
        var invalidCharsPattern = new Regex($@"[^\w\s\-\/{escapedSeparator}]", RegexOptions.Compiled);
        var normalized = invalidCharsPattern.Replace(tag, "");

        // Replace whitespace with hyphens
        normalized = WhitespacePattern.Replace(normalized, "-");

        // Normalize case if requested
        if (normalizeCase)
            normalized = normalized.ToLowerInvariant();

        // Clean up separators
        normalized = normalized.Replace("/" + tagSeparator, tagSeparator);
        normalized = normalized.Trim('-', '/');

        // Trim the tag separator from the beginning and end
        if (tagSeparator.Length == 1)
            normalized = normalized.Trim(tagSeparator[0]);
        else
        {
            // For multi-character separators, trim manually
            while (normalized.StartsWith(tagSeparator))
                normalized = normalized.Substring(tagSeparator.Length);
            while (normalized.EndsWith(tagSeparator))
                normalized = normalized.Substring(0, normalized.Length - tagSeparator.Length);
        }

        // Remove consecutive separators
        while (normalized.Contains(tagSeparator + tagSeparator))
            normalized = normalized.Replace(tagSeparator + tagSeparator, tagSeparator);

        return normalized;
    }

    /// <summary>
    /// Validates tags against reserved tag list.
    /// </summary>
    private List<string> ValidateAgainstReserved(List<string> tags, List<string> reservedTags)
    {
        var invalidTags = new List<string>();

        foreach (var tag in tags)
        {
            if (reservedTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                invalidTags.Add(tag);
                _logger.LogWarning("Tag '{Tag}' is reserved and cannot be used", tag);
            }
        }

        return invalidTags;
    }

    /// <summary>
    /// Organizes tags hierarchically based on separator.
    /// </summary>
    private Dictionary<string, object> OrganizeHierarchically(List<string> tags, string tagSeparator)
    {
        var hierarchy = new Dictionary<string, object>();

        foreach (var tag in tags)
        {
            var parts = tag.Split(tagSeparator, StringSplitOptions.RemoveEmptyEntries);
            var current = hierarchy;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (i == parts.Length - 1)
                {
                    // Leaf node
                    if (!current.ContainsKey(part))
                        current[part] = new List<string>();
                }
                else
                {
                    // Branch node
                    if (!current.ContainsKey(part))
                        current[part] = new Dictionary<string, object>();

                    current = (Dictionary<string, object>)current[part];
                }
            }
        }

        return hierarchy;
    }

    /// <summary>
    /// Gets the maximum hierarchy depth of tags.
    /// </summary>
    private int GetMaxHierarchyDepth(List<string> tags, string tagSeparator)
    {
        return tags.Select(tag => tag.Split(tagSeparator, StringSplitOptions.RemoveEmptyEntries).Length).DefaultIfEmpty(0).Max();
    }

    /// <summary>
    /// Generates suggested tags based on content analysis.
    /// </summary>
    private List<string> GenerateSuggestedTags(Dictionary<string, object> context)
    {
        var suggestions = new List<string>();

        if (!context.ContainsKey("content") || context["content"] is not string content)
            return suggestions;

        try
        {
            // Extract existing tags for comparison
            var existingTags = new HashSet<string>();
            if (context.ContainsKey("existingTags") && context["existingTags"] is IEnumerable<string> existing)
            {
                foreach (var tag in existing)
                    existingTags.Add(tag.ToLowerInvariant());
            }

            // Extract words from content
            var words = WordBoundaryPattern.Matches(content)
                .Cast<Match>()
                .Select(m => m.Value.ToLowerInvariant())
                .Where(w => w.Length > 3)
                .ToList();

            // Find common keywords
            var keywordMatches = words.Intersect(CommonKeywords, StringComparer.OrdinalIgnoreCase).ToList();

            // Generate suggestions from keywords
            foreach (var keyword in keywordMatches)
            {
                if (!existingTags.Contains(keyword))
                {
                    suggestions.Add(keyword);
                }
            }

            // Generate compound suggestions
            var compoundSuggestions = GenerateCompoundSuggestions(keywordMatches, existingTags);
            suggestions.AddRange(compoundSuggestions);

            // Limit suggestions to prevent overwhelming
            return suggestions.Take(10).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating tag suggestions");
            return new List<string>();
        }
    }

    /// <summary>
    /// Generates compound tag suggestions from keywords.
    /// </summary>
    private List<string> GenerateCompoundSuggestions(List<string> keywords, HashSet<string> existingTags)
    {
        var suggestions = new List<string>();

        // Common compound patterns
        var patterns = new Dictionary<string, string[]>
        {
            ["machine"] = new[] { "learning", "intelligence" },
            ["deep"] = new[] { "learning", "neural" },
            ["artificial"] = new[] { "intelligence", "neural" },
            ["data"] = new[] { "science", "analysis" },
            ["web"] = new[] { "development", "programming" },
            ["software"] = new[] { "development", "engineering" }
        };

        foreach (var keyword in keywords)
        {
            if (patterns.ContainsKey(keyword))
            {
                foreach (var complement in patterns[keyword])
                {
                    if (keywords.Contains(complement))
                    {
                        var compound = $"{keyword}-{complement}";
                        if (!existingTags.Contains(compound))
                        {
                            suggestions.Add(compound);
                        }
                    }
                }
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Generates a validation report for tag processing.
    /// </summary>
    private Dictionary<string, object> GenerateValidationReport(List<string> originalTags, List<string> normalizedTags, List<string> invalidTags)
    {
        return new Dictionary<string, object>
        {
            ["original-count"] = originalTags.Count,
            ["normalized-count"] = normalizedTags.Count,
            ["invalid-count"] = invalidTags.Count,
            ["duplicate-count"] = originalTags.Count - originalTags.Distinct().Count(),
            ["validation-success"] = !invalidTags.Any(),
            ["normalization-applied"] = originalTags.Count != normalizedTags.Count ||
                                      !originalTags.SequenceEqual(normalizedTags)
        };
    }
}
