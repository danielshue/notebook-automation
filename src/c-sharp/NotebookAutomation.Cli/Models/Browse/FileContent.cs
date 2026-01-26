// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Models.Browse;

/// <summary>
/// Represents the content of a file including metadata.
/// </summary>
public record FileContent(
    BrowseItem Info,
    string Content,
    Dictionary<string, object>? Frontmatter = null,
    string? Body = null);
