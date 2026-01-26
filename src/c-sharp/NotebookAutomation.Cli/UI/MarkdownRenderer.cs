// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

using Spectre.Console.Rendering;

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Converts markdown text to Spectre.Console markup for terminal rendering.
/// </summary>
public static partial class MarkdownRenderer
{
    /// <summary>
    /// Convert markdown text to Spectre.Console markup (for non-table content).
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>Spectre.Console compatible markup string.</returns>
    public static string ToSpectreMarkup(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var result = markdown;

        // Use placeholder approach: convert markdown to placeholders, escape remaining content, then restore
        var placeholders = new List<(string Placeholder, string Markup)>();
        var index = 0;

        string CreatePlaceholder(string markup)
        {
            var placeholder = $"\x00PH{index++}\x00";
            placeholders.Add((placeholder, markup));
            return placeholder;
        }

        // 1. Process Obsidian wikilinks [[Page|Display Text]] first (more specific pattern)
        result = WikilinkWithDisplayRegex().Replace(result, match =>
        {
            var display = match.Groups[2].Value.EscapeMarkup();
            return CreatePlaceholder($"[magenta underline]{display}[/]");
        });

        // 2. Process simple wikilinks [[Page]]
        result = WikilinkRegex().Replace(result, match =>
        {
            var page = match.Groups[1].Value.EscapeMarkup();
            return CreatePlaceholder($"[magenta underline]{page}[/]");
        });

        // 3. Process standard markdown links [text](url)
        result = MarkdownLinkRegex().Replace(result, match =>
        {
            var text = match.Groups[1].Value.EscapeMarkup();
            var url = match.Groups[2].Value;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return CreatePlaceholder($"[blue underline link={url}]{text}[/]");
            }
            else
            {
                return CreatePlaceholder($"[magenta underline]{text}[/]");
            }
        });

        // 4. Process bare URLs
        result = BareUrlRegex().Replace(result, match =>
        {
            var url = match.Value;
            return CreatePlaceholder($"[blue underline link={url}]{url}[/]");
        });

        // 5. Process markdown bold **text**
        result = BoldRegex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return CreatePlaceholder($"[bold]{text}[/]");
        });

        // 6. Process markdown italic *text* (but not inside bold)
        result = ItalicRegex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return CreatePlaceholder($"[italic]{text}[/]");
        });

        // 7. Process inline code `text`
        result = InlineCodeRegex().Replace(result, match =>
        {
            var text = match.Groups[1].Value.EscapeMarkup();
            return CreatePlaceholder($"[cyan]{text}[/]");
        });

        // 8. Process headers
        result = Header1Regex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return CreatePlaceholder($"\n[bold underline]{text}[/]\n");
        });

        result = Header2Regex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return CreatePlaceholder($"\n[bold]{text}[/]\n");
        });

        result = Header3Regex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return CreatePlaceholder($"\n[underline]{text}[/]\n");
        });

        // 9. Convert markdown bullet points
        result = BulletRegex().Replace(result, "  • ");

        // 10. Convert markdown numbered lists
        result = NumberedListRegex().Replace(result, "  $1. ");

        // 11. Escape any remaining brackets in non-placeholder content
        result = result.EscapeMarkup();

        // 12. Restore all placeholders (markup is already properly formatted)
        foreach (var (placeholder, markup) in placeholders)
        {
            result = result.Replace(placeholder, markup);
        }

        return result;
    }

    /// <summary>
    /// Convert markdown to plain text suitable for Terminal.Gui TextView display.
    /// Strips markdown formatting while preserving content structure.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>Plain text without markdown formatting.</returns>
    public static string ToPlainText(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var result = markdown;

        // 1. Remove Obsidian wikilinks [[Page|Display Text]] -> Display Text
        result = WikilinkWithDisplayRegex().Replace(result, match => match.Groups[2].Value);

        // 2. Remove simple wikilinks [[Page]] -> Page
        result = WikilinkRegex().Replace(result, match => match.Groups[1].Value);

        // 3. Remove standard markdown links [text](url) -> text
        result = MarkdownLinkRegex().Replace(result, match => match.Groups[1].Value);

        // 4. Keep bare URLs as-is (they're readable)
        // No change needed for bare URLs

        // 5. Remove markdown bold **text** -> text
        result = BoldRegex().Replace(result, match => match.Groups[1].Value);

        // 6. Remove markdown italic *text* -> text
        result = ItalicRegex().Replace(result, match => match.Groups[1].Value);

        // 7. Remove inline code `text` -> text
        result = InlineCodeRegex().Replace(result, match => match.Groups[1].Value);

        // 8. Format headers with visual separation
        result = Header1Regex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return $"\n═══ {text.ToUpperInvariant()} ═══\n";
        });

        result = Header2Regex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return $"\n─── {text} ───\n";
        });

        result = Header3Regex().Replace(result, match =>
        {
            var text = match.Groups[1].Value;
            return $"\n• {text}\n";
        });

        // 9. Convert markdown bullet points to Unicode bullets
        result = BulletRegex().Replace(result, "  • ");

        // 10. Keep numbered lists as-is (they're already readable)
        // No change needed

        // 11. Clean up horizontal rules
        result = Regex.Replace(result, @"^\s*[-*_]{3,}\s*$", new string('─', 40), RegexOptions.Multiline);

        // 12. Clean up code blocks (remove triple backticks)
        result = Regex.Replace(result, @"```[a-z]*\n", "┌─ Code ─────────────────\n", RegexOptions.Multiline);
        result = Regex.Replace(result, @"```", "└────────────────────────", RegexOptions.Multiline);

        // 13. Convert blockquotes > text
        result = Regex.Replace(result, @"^>\s*(.*)$", "│ $1", RegexOptions.Multiline);

        return result;
    }

    /// <summary>
    /// Converts markdown to a Spectre.Console IRenderable with full support for tables and links.
    /// Use this for embedding in Panels or other Spectre containers.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>A renderable that can be used in Spectre.Console layouts.</returns>
    public static IRenderable ToRenderable(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return new Text(string.Empty);
        }

        // Split content into table and non-table sections
        var sections = SplitByTables(markdown);
        var rows = new Rows();

        foreach (var section in sections)
        {
            if (section.IsTable)
            {
                var table = BuildTable(section.Content);
                if (table != null)
                {
                    rows = new Rows(rows, table);
                }
            }
            else
            {
                var markup = ToSpectreMarkup(section.Content);
                if (!string.IsNullOrWhiteSpace(markup))
                {
                    rows = new Rows(rows, new Markup(markup));
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// Render markdown directly to the console with proper formatting.
    /// </summary>
    /// <param name="markdown">The markdown text to render.</param>
    public static void Render(string markdown)
    {
        RenderWithTables(markdown, addNewline: false);
    }

    /// <summary>
    /// Render markdown as a complete line with newline.
    /// </summary>
    /// <param name="markdown">The markdown text to render.</param>
    public static void RenderLine(string markdown)
    {
        RenderWithTables(markdown, addNewline: true);
    }

    /// <summary>
    /// Render markdown content, handling tables separately from other content.
    /// </summary>
    private static void RenderWithTables(string markdown, bool addNewline)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            if (addNewline)
            {
                AnsiConsole.WriteLine();
            }

            return;
        }

        // Split content into table and non-table sections
        var sections = SplitByTables(markdown);

        foreach (var section in sections)
        {
            if (section.IsTable)
            {
                RenderTable(section.Content);
            }
            else
            {
                var markup = ToSpectreMarkup(section.Content);
                if (!string.IsNullOrWhiteSpace(markup))
                {
                    AnsiConsole.Markup(markup);
                }
            }
        }

        if (addNewline)
        {
            AnsiConsole.WriteLine();
        }
    }

    /// <summary>
    /// Split markdown content into table and non-table sections.
    /// </summary>
    private static List<ContentSection> SplitByTables(string markdown)
    {
        var sections = new List<ContentSection>();
        var lines = markdown.Split('\n');
        var currentNonTable = new List<string>();
        var currentTable = new List<string>();
        var inTable = false;

        foreach (var line in lines)
        {
            var isTableLine = IsTableLine(line);

            if (isTableLine)
            {
                if (!inTable)
                {
                    // Starting a new table - flush non-table content
                    if (currentNonTable.Count > 0)
                    {
                        sections.Add(new ContentSection(string.Join('\n', currentNonTable), IsTable: false));
                        currentNonTable.Clear();
                    }

                    inTable = true;
                }

                currentTable.Add(line);
            }
            else
            {
                if (inTable)
                {
                    // Ending a table - flush table content
                    if (currentTable.Count > 0)
                    {
                        sections.Add(new ContentSection(string.Join('\n', currentTable), IsTable: true));
                        currentTable.Clear();
                    }

                    inTable = false;
                }

                currentNonTable.Add(line);
            }
        }

        // Flush remaining content
        if (currentTable.Count > 0)
        {
            sections.Add(new ContentSection(string.Join('\n', currentTable), IsTable: true));
        }

        if (currentNonTable.Count > 0)
        {
            sections.Add(new ContentSection(string.Join('\n', currentNonTable), IsTable: false));
        }

        return sections;
    }

    /// <summary>
    /// Check if a line is part of a markdown table.
    /// </summary>
    private static bool IsTableLine(string line)
    {
        var trimmed = line.Trim();
        // A markdown table line starts and ends with | or is a separator line
        return trimmed.StartsWith('|') && trimmed.EndsWith('|');
    }

    /// <summary>
    /// Build a markdown table as a Spectre.Console Table renderable.
    /// </summary>
    private static Table? BuildTable(string tableMarkdown)
    {
        var lines = tableMarkdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return null;
        }

        var table = new Table();
        table.Border(TableBorder.Rounded);

        // Parse header row
        var headerCells = ParseTableRow(lines[0]);
        foreach (var header in headerCells)
        {
            table.AddColumn(new TableColumn(header.EscapeMarkup()).Centered());
        }

        // Skip separator row (line with dashes: |---|---|)
        var dataStartIndex = 1;
        if (lines.Length > 1 && IsSeparatorLine(lines[1]))
        {
            dataStartIndex = 2;
        }

        // Parse data rows
        for (int i = dataStartIndex; i < lines.Length; i++)
        {
            var cells = ParseTableRow(lines[i]);
            if (cells.Length > 0)
            {
                // Ensure we have enough cells to match columns
                var paddedCells = new string[headerCells.Length];
                for (int j = 0; j < headerCells.Length; j++)
                {
                    paddedCells[j] = j < cells.Length ? cells[j].EscapeMarkup() : string.Empty;
                }

                table.AddRow(paddedCells);
            }
        }

        return table;
    }

    /// <summary>
    /// Render a markdown table using Spectre.Console Table.
    /// </summary>
    private static void RenderTable(string tableMarkdown)
    {
        var table = BuildTable(tableMarkdown);
        if (table != null)
        {
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.Markup(ToSpectreMarkup(tableMarkdown));
        }
    }

    /// <summary>
    /// Parse a markdown table row into cells.
    /// </summary>
    private static string[] ParseTableRow(string row)
    {
        var trimmed = row.Trim();

        // Remove leading and trailing pipes
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|'))
        {
            trimmed = trimmed[..^1];
        }

        // Split by pipe and trim each cell
        return trimmed.Split('|')
            .Select(cell => cell.Trim())
            .ToArray();
    }

    /// <summary>
    /// Check if a line is a table separator (contains dashes).
    /// </summary>
    private static bool IsSeparatorLine(string line)
    {
        var trimmed = line.Trim();
        // Separator lines contain mostly dashes, pipes, and colons
        return trimmed.StartsWith('|') &&
               trimmed.Replace("|", "").Replace("-", "").Replace(":", "").Replace(" ", "").Length == 0;
    }

    /// <summary>
    /// Represents a section of content that may or may not be a table.
    /// </summary>
    private record ContentSection(string Content, bool IsTable);

    // Regex patterns for markdown conversion
    [GeneratedRegex(@"\*\*([^*]+)\*\*", RegexOptions.Compiled)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\*)\*([^*]+)\*(?!\*)", RegexOptions.Compiled)]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"^# (.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex Header1Regex();

    [GeneratedRegex(@"^## (.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex Header2Regex();

    [GeneratedRegex(@"^### (.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex Header3Regex();

    [GeneratedRegex(@"^- ", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"^(\d+)\. ", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex NumberedListRegex();

    // Wikilink with display text: [[Page|Display Text]]
    [GeneratedRegex(@"\[\[([^\]|]+)\|([^\]]+)\]\]", RegexOptions.Compiled)]
    private static partial Regex WikilinkWithDisplayRegex();

    // Simple wikilink: [[Page]]
    [GeneratedRegex(@"\[\[([^\]|]+)\]\]", RegexOptions.Compiled)]
    private static partial Regex WikilinkRegex();

    // Standard markdown link: [text](url)
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();

    // Bare URLs (http:// or https://)
    [GeneratedRegex(@"(?<!\()https?://[^\s\)\]]+", RegexOptions.Compiled)]
    private static partial Regex BareUrlRegex();
}
