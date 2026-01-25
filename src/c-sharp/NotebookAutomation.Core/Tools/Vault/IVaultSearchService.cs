// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Interface for vault search operations including content, filename, tag, and frontmatter search.
/// </summary>
/// <remarks>
/// Provides search functionality for finding notes within an Obsidian vault or similar
/// file-based knowledge management system based on various criteria.
/// </remarks>
public interface IVaultSearchService
{
    /// <summary>
    /// Searches notes by content.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="relativePath">Optional path to limit search scope.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="contextLines">Number of context lines to include around matches.</param>
    /// <returns>A result containing matching notes with context.</returns>
    VaultBrowserResult<IReadOnlyList<VaultSearchResult>> SearchContent(
        string query,
        string? relativePath = null,
        int maxResults = 20,
        int contextLines = 2);

    /// <summary>
    /// Searches notes by filename.
    /// </summary>
    /// <param name="pattern">The filename pattern to match (supports * and ? wildcards).</param>
    /// <param name="relativePath">Optional path to limit search scope.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>A result containing matching notes.</returns>
    VaultBrowserResult<IReadOnlyList<VaultNoteInfo>> SearchByFilename(
        string pattern,
        string? relativePath = null,
        int maxResults = 50);

    /// <summary>
    /// Searches notes by tag.
    /// </summary>
    /// <param name="tag">The tag to search for (without #).</param>
    /// <param name="relativePath">Optional path to limit search scope.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>A result containing notes with the specified tag.</returns>
    VaultBrowserResult<IReadOnlyList<VaultNoteWithTags>> SearchByTag(
        string tag,
        string? relativePath = null,
        int maxResults = 50);

    /// <summary>
    /// Searches notes by frontmatter field value.
    /// </summary>
    /// <param name="field">The frontmatter field name.</param>
    /// <param name="value">The value to search for.</param>
    /// <param name="relativePath">Optional path to limit search scope.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>A result containing notes matching the frontmatter criteria.</returns>
    VaultBrowserResult<IReadOnlyList<VaultNoteWithFrontmatter>> SearchByFrontmatter(
        string field,
        string value,
        string? relativePath = null,
        int maxResults = 50);

    /// <summary>
    /// Gets all unique tags in the vault.
    /// </summary>
    /// <param name="relativePath">Optional path to limit scope.</param>
    /// <returns>A result containing all unique tags and their counts.</returns>
    VaultBrowserResult<IReadOnlyDictionary<string, int>> GetAllTags(string? relativePath = null);
}

/// <summary>
/// A search result with match context.
/// </summary>
public class VaultSearchResult
{
    /// <summary>
    /// Gets the note information.
    /// </summary>
    public required VaultNoteInfo Note { get; init; }

    /// <summary>
    /// Gets the match contexts showing where the query was found.
    /// </summary>
    public required IReadOnlyList<MatchContext> Matches { get; init; }

    /// <summary>
    /// Gets the total number of matches in this note.
    /// </summary>
    public int TotalMatches { get; init; }
}

/// <summary>
/// Context around a match.
/// </summary>
public class MatchContext
{
    /// <summary>
    /// Gets the line number (1-based) where the match was found.
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// Gets the text around the match including context.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Gets the position of the match start within the context.
    /// </summary>
    public int MatchStart { get; init; }

    /// <summary>
    /// Gets the length of the match.
    /// </summary>
    public int MatchLength { get; init; }
}

/// <summary>
/// A note with its tags.
/// </summary>
public class VaultNoteWithTags
{
    /// <summary>
    /// Gets the note information.
    /// </summary>
    public required VaultNoteInfo Note { get; init; }

    /// <summary>
    /// Gets all tags in this note.
    /// </summary>
    public required IReadOnlySet<string> Tags { get; init; }
}

/// <summary>
/// A note with selected frontmatter fields.
/// </summary>
public class VaultNoteWithFrontmatter
{
    /// <summary>
    /// Gets the note information.
    /// </summary>
    public required VaultNoteInfo Note { get; init; }

    /// <summary>
    /// Gets the matched frontmatter field name.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Gets the matched frontmatter field value.
    /// </summary>
    public object? FieldValue { get; init; }

    /// <summary>
    /// Gets all frontmatter as a dictionary.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Frontmatter { get; init; }
}
