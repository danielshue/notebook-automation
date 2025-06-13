// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Interface for YAML helper functionality used to process YAML frontmatter in markdown files.
/// </summary>

public interface IYamlHelper
{
    /// <summary>
    /// Extracts the YAML frontmatter from markdown content.
    /// </summary>
    /// <param name="markdown">The markdown content to parse.</param>
    /// <returns>The YAML frontmatter as a string, or null if not found.</returns>
    string? ExtractFrontmatter(string markdown);

    /// <summary>
    /// Parses YAML frontmatter to a dynamic object.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <returns>The parsed object, or null if parsing failed.</returns>
    object? ParseYaml(string yaml);

    /// <summary>
    /// Parses YAML frontmatter to a dictionary.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <returns>The parsed dictionary, or an empty dictionary if parsing failed.</returns>
    Dictionary<string, object> ParseYamlToDictionary(string yaml);

    /// <summary>
    /// Serializes a dictionary to YAML format.
    /// </summary>
    /// <param name="data">The dictionary to serialize.</param>
    /// <returns>The serialized YAML string.</returns>
    string SerializeToYaml(Dictionary<string, object> data);

    /// <summary>
    /// Replaces the frontmatter in a markdown document with new frontmatter.
    /// </summary>
    /// <param name="markdown">The markdown content.</param>
    /// <param name="newFrontmatter">The new frontmatter as a dictionary.</param>
    /// <returns>The updated markdown content.</returns>
    string ReplaceFrontmatter(string markdown, Dictionary<string, object> newFrontmatter);

    /// <summary>
    /// Updates the YAML frontmatter in markdown content.
    /// </summary>
    /// <param name="markdown">The markdown content.</param>
    /// <param name="newFrontmatter">The new frontmatter as a dictionary.</param>
    /// <returns>The updated markdown content.</returns>
    string UpdateFrontmatter(string markdown, Dictionary<string, object> newFrontmatter);

    /// <summary>
    /// Diagnostics for YAML frontmatter in markdown content.
    /// </summary>
    /// <param name="markdown">The markdown content to diagnose.</param>
    /// <returns>A tuple with success status, message, and parsed data if available.</returns>
    (bool Success, string Message, Dictionary<string, object>? Data) DiagnoseYamlFrontmatter(string markdown);

    /// <summary>
    /// Removes the YAML frontmatter from markdown content.
    /// </summary>
    /// <param name="markdown">The markdown content.</param>
    /// <returns>The markdown content without frontmatter.</returns>
    string RemoveFrontmatter(string markdown);
}
