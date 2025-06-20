// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Configuration for banner settings in generated markdown files.
/// </summary>
/// <remarks>
/// <para>
/// This class provides configuration options for controlling when and how banners are displayed
/// in generated markdown files. It supports different banner formats, conditional inclusion
/// based on template types and filename patterns, and per-project customization.
/// </para>
/// <para>
/// Banner formats supported include text, markdown, and HTML content that can be inserted
/// at the top of generated markdown files to provide visual branding or contextual information.
/// </para>
/// </remarks>
public class BannerConfig
{
    /// <summary>
    /// Gets or sets the default banner content to use when no specific configuration matches.
    /// </summary>
    /// <remarks>
    /// This banner is used as a fallback when template-type specific or filename pattern
    /// configurations don't match. Can be plain text, markdown, or HTML.
    /// </remarks>
    [JsonPropertyName("default")]
    public string? DefaultBanner { get; set; } = "gies-banner.png";

    /// <summary>
    /// Gets or sets the banner format type.
    /// </summary>
    /// <remarks>
    /// Supported formats:
    /// - "image" (default): Obsidian-style image reference
    /// - "text": Plain text content
    /// - "markdown": Markdown formatted content
    /// - "html": HTML formatted content
    /// </remarks>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "image";

    /// <summary>
    /// Gets or sets banner configurations specific to template types.
    /// </summary>
    /// <remarks>
    /// Maps template-type values (like "main", "program", "course") to specific banner content.
    /// This allows different banner content based on the type of markdown being generated.
    /// </remarks>
    [JsonPropertyName("template_banners")]
    public Dictionary<string, string> TemplateBanners { get; set; } = new()
    {
        ["main"] = "gies-banner.png",
        ["program"] = "gies-banner.png", 
        ["course"] = "gies-banner.png"
    };

    /// <summary>
    /// Gets or sets banner configurations based on filename patterns.
    /// </summary>
    /// <remarks>
    /// Maps filename patterns (using wildcards) to specific banner content.
    /// Patterns are checked in order, with the first match being used.
    /// Examples: "*.course.*", "index.*", "*assignment*"
    /// </remarks>
    [JsonPropertyName("filename_patterns")]
    public Dictionary<string, string> FilenamePatterns { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether banners should be enabled globally.
    /// </summary>
    /// <remarks>
    /// When false, no banners will be added regardless of other configuration.
    /// This provides a global disable switch for banner functionality.
    /// </remarks>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}