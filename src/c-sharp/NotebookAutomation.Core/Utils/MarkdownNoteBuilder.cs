// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Provides a reusable, strongly-typed builder for generating markdown notes with YAML frontmatter.
/// </summary>
/// <remarks>
/// <para>
/// This class simplifies the creation of markdown notes that require YAML frontmatter for metadata, supporting both
/// frontmatter-only and full note (frontmatter + body) scenarios. It uses <see cref="YamlHelper"/> for serialization.
/// </para>
/// <example>
/// <code>
/// var builder = new MarkdownNoteBuilder();
/// var frontmatter = new Dictionary&lt;string, object&gt; { ["title"] = "Sample" };
/// string note = builder.BuildNote(frontmatter, "# Heading\nContent");
/// </code>
/// </example>
/// </remarks>
public class MarkdownNoteBuilder(IYamlHelper yamlHelper, AppConfig appConfig)
{
    private readonly IYamlHelper _yamlHelper = yamlHelper;
    private readonly AppConfig _appConfig = appConfig;

    /// <summary>
    /// Builds a markdown note containing only YAML frontmatter (no content body).
    /// </summary>
    /// <param name="frontmatter">A dictionary of frontmatter keys and values to serialize as YAML.</param>
    /// <param name="filename">Optional filename for banner pattern matching.</param>
    /// <returns>A markdown string containing only the YAML frontmatter block.</returns>
    /// <remarks>
    /// The resulting string will have a YAML frontmatter block delimited by <c>---</c> and two trailing newlines.
    /// </remarks>
    /// <example>
    /// <code>
    /// var frontmatter = new Dictionary&lt;string, object&gt; { ["title"] = "Sample" };    /// string note = builder.CreateMarkdownWithFrontmatter(frontmatter);
    /// </code>
    /// </example>
    public string CreateMarkdownWithFrontmatter(Dictionary<string, object> frontmatter, string? filename = null)
    {
        // Apply banner configuration
        ApplyBannerConfiguration(frontmatter, filename);

        var yaml = _yamlHelper.UpdateFrontmatter(string.Empty, frontmatter);

        // Remove any trailing newlines or content after frontmatter
        int end = yaml.IndexOf("---", 3, StringComparison.Ordinal);
        if (end > 0)
        {
            return yaml[..(end + 3)] + "\n\n";
        }

        return yaml.TrimEnd() + "\n\n";
    }

    /// <summary>
    /// Builds a markdown note with both YAML frontmatter and a markdown content body.
    /// </summary>
    /// <param name="frontmatter">A dictionary of frontmatter keys and values to serialize as YAML.</param>
    /// <param name="body">The markdown content body to append after the frontmatter.</param>
    /// <param name="filename">Optional filename for banner pattern matching.</param>
    /// <returns>A markdown string containing the YAML frontmatter block followed by the content body.</returns>
    /// <remarks>
    /// The frontmatter is always placed at the top of the note, followed by the markdown body.    /// </remarks>
    /// <example>
    /// <code>
    /// var frontmatter = new Dictionary&lt;string, object&gt; { ["title"] = "Sample" };
    /// string note = builder.BuildNote(frontmatter, "# Heading\nContent");
    /// </code>
    /// </example>
    public string BuildNote(Dictionary<string, object> frontmatter, string body, string? filename = null)
    {
        // Apply banner configuration
        ApplyBannerConfiguration(frontmatter, filename);

        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithEmissionPhaseObjectGraphVisitor(args => new NoQuotesForBannerVisitor(args.InnerVisitor))
            .Build();
        var yaml = serializer.Serialize(frontmatter);

        // Post-process to fix banner field formatting if it contains wiki links
        if (frontmatter.ContainsKey("banner") && frontmatter["banner"] is string bannerValue)
        {
            if (bannerValue.Contains("[[") && bannerValue.Contains("]]"))
            {
                // Find and replace the banner line to preserve wiki link syntax
                var lines = yaml.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("banner:"))
                    {
                        lines[i] = $"banner: '{bannerValue}'";
                        break;
                    }
                }

                yaml = string.Join('\n', lines);
            }
        }

        var newFrontmatter = $"---\n{yaml}---\n\n";
        return newFrontmatter + body;
    }


    /// <summary>
    /// Determines whether a default banner should be added to the frontmatter.
    /// </summary>
    /// <param name="frontmatter">The frontmatter dictionary to check.</param>
    /// <returns>True if a default banner should be added, false otherwise.</returns>
    /// <remarks>
    /// This method checks if the frontmatter explicitly indicates whether a banner is wanted.
    /// It only adds a banner if the template or configuration specifically requests one,
    /// or if the frontmatter already contains a banner placeholder.
    /// </remarks>
    private static bool ShouldAddDefaultBanner(Dictionary<string, object> frontmatter)
    {
        // Don't add banner if already explicitly set (including empty/null values)
        if (frontmatter.ContainsKey("banner"))
        {
            return false;
        }

        // Check if this is a template type that should have banners
        if (frontmatter.TryGetValue("template-type", out var templateType))
        {
            var templateTypeStr = templateType?.ToString() ?? string.Empty;

            // Only add banners for specific template types that are known to need them
            return templateTypeStr switch
            {
                "main" => true,          // Main index pages
                "program" => true,       // Program index pages
                "course" => true,        // Course index pages
                _ => false               // All other types (including video-reference, pdf-reference, etc.)
            };
        }

        // Default: don't add banner unless explicitly requested
        return false;
    }

    /// <summary>
    /// Applies banner configuration to the frontmatter based on template type and filename patterns.
    /// </summary>
    /// <param name="frontmatter">The frontmatter dictionary to modify.</param>
    /// <param name="filename">Optional filename for pattern matching.</param>
    /// <remarks>
    /// This method checks banner configuration in the following order:
    /// 1. If banner is already explicitly set, preserves it
    /// 2. Checks filename patterns for matches
    /// 3. Checks template-type specific banners
    /// 4. Falls back to default banner if enabled
    /// </remarks>
    private void ApplyBannerConfiguration(Dictionary<string, object> frontmatter, string? filename)
    {
        // Don't add banner if globally disabled or configuration is missing
        if (_appConfig?.Banners?.Enabled != true)
        {
            return;
        }

        // Don't override existing banner setting
        if (frontmatter.ContainsKey("banner"))
        {
            return;
        }

        string? bannerContent = null;

        // 1. Check filename patterns first (most specific)
        if (!string.IsNullOrWhiteSpace(filename))
        {
            bannerContent = GetBannerByFilenamePattern(filename);
        }

        // 2. Check template-type specific banners
        if (bannerContent == null && frontmatter.TryGetValue("template-type", out var templateType))
        {
            var templateTypeStr = templateType?.ToString() ?? string.Empty;
            bannerContent = GetBannerByTemplateType(templateTypeStr);
        }

        // 3. Fall back to default banner for backward compatibility
        if (bannerContent == null && ShouldAddDefaultBanner(frontmatter))
        {
            bannerContent = _appConfig?.Banners?.DefaultBanner;
        }

        // Apply the banner if found
        if (!string.IsNullOrWhiteSpace(bannerContent))
        {
            frontmatter["banner"] = FormatBannerContent(bannerContent);
        }
    }

    /// <summary>
    /// Gets banner content based on filename pattern matching.
    /// </summary>
    /// <param name="filename">The filename to match against patterns.</param>
    /// <returns>Banner content if a pattern matches, null otherwise.</returns>
    private string? GetBannerByFilenamePattern(string filename)
    {
        if (_appConfig?.Banners?.FilenamePatterns == null)
        {
            return null;
        }

        foreach (var pattern in _appConfig.Banners.FilenamePatterns)
        {
            if (IsFilenameMatch(filename, pattern.Key))
            {
                return pattern.Value;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets banner content based on template type.
    /// </summary>
    /// <param name="templateType">The template type to look up.</param>
    /// <returns>Banner content if template type is configured, null otherwise.</returns>
    private string? GetBannerByTemplateType(string templateType)
    {
        return _appConfig?.Banners?.TemplateBanners?.TryGetValue(templateType, out var banner) == true ? banner : null;
    }

    /// <summary>
    /// Formats banner content based on the configured format type.
    /// </summary>
    /// <param name="bannerContent">The raw banner content.</param>
    /// <returns>Formatted banner content.</returns>
    private string FormatBannerContent(string bannerContent)
    {
        return (_appConfig?.Banners?.Format) switch
        {
            "image" => bannerContent, // For Obsidian image references, use as-is
            "text" => bannerContent,
            "markdown" => bannerContent,
            "html" => bannerContent,
            _ => bannerContent // Default: use as-is
        };
    }

    /// <summary>
    /// Checks if a filename matches a wildcard pattern.
    /// </summary>
    /// <param name="filename">The filename to check.</param>
    /// <param name="pattern">The pattern with wildcards (* and ?).</param>
    /// <returns>True if the filename matches the pattern, false otherwise.</returns>
    private static bool IsFilenameMatch(string filename, string pattern)
    {
        // Simple wildcard matching - convert to regex
        var regexPattern = "^" + pattern
            .Replace("*", ".*")
            .Replace("?", ".")
            + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(filename, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
