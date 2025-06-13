// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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
public class MarkdownNoteBuilder(IYamlHelper yamlHelper)
{
    private readonly IYamlHelper _yamlHelper = yamlHelper;

    /// <summary>
    /// Builds a markdown note containing only YAML frontmatter (no content body).
    /// </summary>
    /// <param name="frontmatter">A dictionary of frontmatter keys and values to serialize as YAML.</param>
    /// <returns>A markdown string containing only the YAML frontmatter block.</returns>
    /// <remarks>
    /// The resulting string will have a YAML frontmatter block delimited by <c>---</c> and two trailing newlines.
    /// </remarks>
    /// <example>
    /// <code>
    /// var frontmatter = new Dictionary&lt;string, object&gt; { ["title"] = "Sample" };    /// string note = builder.CreateMarkdownWithFrontmatter(frontmatter);
    /// </code>
    /// </example>
    public string CreateMarkdownWithFrontmatter(Dictionary<string, object> frontmatter)
    {
        // Only add banner if explicitly requested or if template includes one
        if (ShouldAddDefaultBanner(frontmatter))
        {
            frontmatter["banner"] = "'[[gies-banner.png]]'";
        }

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
    /// <returns>A markdown string containing the YAML frontmatter block followed by the content body.</returns>
    /// <remarks>
    /// The frontmatter is always placed at the top of the note, followed by the markdown body.    /// </remarks>
    /// <example>
    /// <code>
    /// var frontmatter = new Dictionary&lt;string, object&gt; { ["title"] = "Sample" };
    /// string note = builder.BuildNote(frontmatter, "# Heading\nContent");
    /// </code>
    /// </example>
    public string BuildNote(Dictionary<string, object> frontmatter, string body)
    {
        // Only set default banner if explicitly requested by template type
        if (ShouldAddDefaultBanner(frontmatter))
        {
            frontmatter["banner"] = "gies-banner.png";
        }
        // Note: We preserve the banner value exactly as it comes from the template
        // to maintain proper Obsidian wiki link syntax like '[[gies-banner.png]]'

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
}
