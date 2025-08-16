// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Provides contract for building markdown notes with YAML frontmatter.
/// </summary>
public interface IMarkdownNoteBuilder
{
    /// <summary>
    /// Builds a markdown note containing only YAML frontmatter (no content body).
    /// </summary>
    /// <param name="frontmatter">A dictionary of frontmatter keys and values to serialize as YAML.</param>
    /// <param name="filename">Optional filename for banner pattern matching.</param>
    /// <returns>A markdown string containing only the YAML frontmatter block.</returns>
    string CreateMarkdownWithFrontmatter(Dictionary<string, object> frontmatter, string? filename = null);

    /// <summary>
    /// Builds a markdown note with both YAML frontmatter and a markdown content body.
    /// </summary>
    /// <param name="frontmatter">A dictionary of frontmatter keys and values to serialize as YAML.</param>
    /// <param name="body">The markdown content body to append after the frontmatter.</param>
    /// <param name="filename">Optional filename for banner pattern matching.</param>
    /// <returns>A markdown string containing the YAML frontmatter block followed by the content body.</returns>
    string BuildNote(Dictionary<string, object> frontmatter, string body, string? filename = null);
}
