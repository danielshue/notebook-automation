// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

using System.Text.RegularExpressions;

using NotebookAutomation.Core.Utils;

/// <summary>
/// Implementation of <see cref="IVaultSearchService"/> for searching notes in a vault.
/// </summary>
/// <remarks>
/// Provides comprehensive search functionality including content search, filename matching,
/// tag filtering, and frontmatter queries.
/// </remarks>
public partial class VaultSearchService : IVaultSearchService
{
    private readonly ILogger<VaultSearchService> _logger;
    private readonly IVaultBrowserService _browserService;
    private readonly IYamlHelper _yamlHelper;

    /// <summary>
    /// Initializes a new instance of <see cref="VaultSearchService"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="browserService">The vault browser service for file access.</param>
    /// <param name="yamlHelper">The YAML helper for parsing frontmatter.</param>
    public VaultSearchService(
        ILogger<VaultSearchService> logger,
        IVaultBrowserService browserService,
        IYamlHelper yamlHelper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
    }

    /// <inheritdoc />
    public VaultBrowserResult<IReadOnlyList<VaultSearchResult>> SearchContent(
        string query,
        string? relativePath = null,
        int maxResults = 20,
        int contextLines = 2)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return VaultBrowserResult<IReadOnlyList<VaultSearchResult>>.Failure("Search query cannot be empty");
        }

        try
        {
            var notesResult = _browserService.ListNotes(relativePath ?? string.Empty, recursive: true);
            if (!notesResult.IsSuccess)
            {
                return VaultBrowserResult<IReadOnlyList<VaultSearchResult>>.Failure(notesResult.Error!);
            }

            var results = new List<VaultSearchResult>();
            var queryLower = query.ToLowerInvariant();

            foreach (var note in notesResult.Value!)
            {
                var contentResult = _browserService.ReadNote(note.RelativePath);
                if (!contentResult.IsSuccess)
                {
                    continue;
                }

                var content = contentResult.Value!.Content;
                var contentLower = content.ToLowerInvariant();

                if (!contentLower.Contains(queryLower))
                {
                    continue;
                }

                var matches = GetMatchContexts(content, query, contextLines);
                if (matches.Count > 0)
                {
                    results.Add(new VaultSearchResult
                    {
                        Note = note,
                        Matches = matches,
                        TotalMatches = CountOccurrences(contentLower, queryLower)
                    });

                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            return VaultBrowserResult<IReadOnlyList<VaultSearchResult>>.Success(
                results.OrderByDescending(r => r.TotalMatches).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching content for: {Query}", query);
            return VaultBrowserResult<IReadOnlyList<VaultSearchResult>>.Failure($"Search error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<IReadOnlyList<VaultNoteInfo>> SearchByFilename(
        string pattern,
        string? relativePath = null,
        int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return VaultBrowserResult<IReadOnlyList<VaultNoteInfo>>.Failure("Search pattern cannot be empty");
        }

        try
        {
            var notesResult = _browserService.ListNotes(relativePath ?? string.Empty, recursive: true);
            if (!notesResult.IsSuccess)
            {
                return VaultBrowserResult<IReadOnlyList<VaultNoteInfo>>.Failure(notesResult.Error!);
            }

            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            var results = new List<VaultNoteInfo>();

            foreach (var note in notesResult.Value!)
            {
                if (regex.IsMatch(note.FileName) || regex.IsMatch(note.Name))
                {
                    results.Add(note);

                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            return VaultBrowserResult<IReadOnlyList<VaultNoteInfo>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by filename: {Pattern}", pattern);
            return VaultBrowserResult<IReadOnlyList<VaultNoteInfo>>.Failure($"Search error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<IReadOnlyList<VaultNoteWithTags>> SearchByTag(
        string tag,
        string? relativePath = null,
        int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return VaultBrowserResult<IReadOnlyList<VaultNoteWithTags>>.Failure("Tag cannot be empty");
        }

        try
        {
            var notesResult = _browserService.ListNotes(relativePath ?? string.Empty, recursive: true);
            if (!notesResult.IsSuccess)
            {
                return VaultBrowserResult<IReadOnlyList<VaultNoteWithTags>>.Failure(notesResult.Error!);
            }

            // Normalize tag - remove # prefix if present
            var normalizedTag = tag.TrimStart('#').ToLowerInvariant();
            var results = new List<VaultNoteWithTags>();

            foreach (var note in notesResult.Value!)
            {
                var metadataResult = _browserService.GetNoteMetadata(note.RelativePath);
                if (!metadataResult.IsSuccess)
                {
                    continue;
                }

                var tags = metadataResult.Value!.Tags;
                var normalizedTags = tags.Select(t => t.TrimStart('#').ToLowerInvariant()).ToHashSet();

                if (normalizedTags.Contains(normalizedTag))
                {
                    results.Add(new VaultNoteWithTags
                    {
                        Note = note,
                        Tags = tags
                    });

                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            return VaultBrowserResult<IReadOnlyList<VaultNoteWithTags>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by tag: {Tag}", tag);
            return VaultBrowserResult<IReadOnlyList<VaultNoteWithTags>>.Failure($"Search error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<IReadOnlyList<VaultNoteWithFrontmatter>> SearchByFrontmatter(
        string field,
        string value,
        string? relativePath = null,
        int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return VaultBrowserResult<IReadOnlyList<VaultNoteWithFrontmatter>>.Failure("Field name cannot be empty");
        }

        try
        {
            var notesResult = _browserService.ListNotes(relativePath ?? string.Empty, recursive: true);
            if (!notesResult.IsSuccess)
            {
                return VaultBrowserResult<IReadOnlyList<VaultNoteWithFrontmatter>>.Failure(notesResult.Error!);
            }

            var valueLower = value?.ToLowerInvariant() ?? string.Empty;
            var results = new List<VaultNoteWithFrontmatter>();

            foreach (var note in notesResult.Value!)
            {
                var metadataResult = _browserService.GetNoteMetadata(note.RelativePath);
                if (!metadataResult.IsSuccess)
                {
                    continue;
                }

                var frontmatter = metadataResult.Value!.Frontmatter;
                if (!frontmatter.TryGetValue(field, out var fieldValue))
                {
                    continue;
                }

                var fieldValueStr = fieldValue?.ToString()?.ToLowerInvariant() ?? string.Empty;

                // Match if value is empty (looking for any value in field) or if values match
                if (string.IsNullOrEmpty(valueLower) || fieldValueStr.Contains(valueLower))
                {
                    results.Add(new VaultNoteWithFrontmatter
                    {
                        Note = note,
                        FieldName = field,
                        FieldValue = fieldValue,
                        Frontmatter = frontmatter.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    });

                    if (results.Count >= maxResults)
                    {
                        break;
                    }
                }
            }

            return VaultBrowserResult<IReadOnlyList<VaultNoteWithFrontmatter>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by frontmatter: {Field}={Value}", field, value);
            return VaultBrowserResult<IReadOnlyList<VaultNoteWithFrontmatter>>.Failure($"Search error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public VaultBrowserResult<IReadOnlyDictionary<string, int>> GetAllTags(string? relativePath = null)
    {
        try
        {
            var notesResult = _browserService.ListNotes(relativePath ?? string.Empty, recursive: true);
            if (!notesResult.IsSuccess)
            {
                return VaultBrowserResult<IReadOnlyDictionary<string, int>>.Failure(notesResult.Error!);
            }

            var tagCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var note in notesResult.Value!)
            {
                var metadataResult = _browserService.GetNoteMetadata(note.RelativePath);
                if (!metadataResult.IsSuccess)
                {
                    continue;
                }

                foreach (var tag in metadataResult.Value!.Tags)
                {
                    var normalizedTag = tag.TrimStart('#');
                    if (tagCounts.ContainsKey(normalizedTag))
                    {
                        tagCounts[normalizedTag]++;
                    }
                    else
                    {
                        tagCounts[normalizedTag] = 1;
                    }
                }
            }

            return VaultBrowserResult<IReadOnlyDictionary<string, int>>.Success(
                tagCounts.OrderByDescending(kvp => kvp.Value)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tags");
            return VaultBrowserResult<IReadOnlyDictionary<string, int>>.Failure($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts match contexts from content showing where query was found.
    /// </summary>
    private static List<MatchContext> GetMatchContexts(string content, string query, int contextLines)
    {
        var contexts = new List<MatchContext>();
        var lines = content.Split('\n');
        var queryLower = query.ToLowerInvariant();

        for (int i = 0; i < lines.Length; i++)
        {
            var lineLower = lines[i].ToLowerInvariant();
            var matchIndex = lineLower.IndexOf(queryLower);

            if (matchIndex >= 0)
            {
                // Build context with surrounding lines
                var startLine = Math.Max(0, i - contextLines);
                var endLine = Math.Min(lines.Length - 1, i + contextLines);
                var contextBuilder = new List<string>();

                for (int j = startLine; j <= endLine; j++)
                {
                    contextBuilder.Add(lines[j].TrimEnd());
                }

                var contextText = string.Join("\n", contextBuilder);

                // Calculate match position in context
                var linesBeforeMatch = i - startLine;
                var matchStart = 0;
                for (int j = 0; j < linesBeforeMatch; j++)
                {
                    matchStart += contextBuilder[j].Length + 1; // +1 for newline
                }
                matchStart += matchIndex;

                contexts.Add(new MatchContext
                {
                    LineNumber = i + 1,
                    Context = contextText,
                    MatchStart = matchStart,
                    MatchLength = query.Length
                });

                // Limit contexts per file
                if (contexts.Count >= 5)
                {
                    break;
                }
            }
        }

        return contexts;
    }

    /// <summary>
    /// Counts occurrences of a substring in text.
    /// </summary>
    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
