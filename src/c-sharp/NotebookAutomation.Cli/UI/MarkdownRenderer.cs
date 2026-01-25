// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

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

        // First, escape any existing Spectre markup characters to prevent injection
        result = result.Replace("[", "[[").Replace("]", "]]");

        // Convert markdown bold **text** to Spectre bold
        result = BoldRegex().Replace(result, "[bold]$1[/]");

        // Convert markdown italic *text* (but not inside bold)
        result = ItalicRegex().Replace(result, "[italic]$1[/]");

        // Convert markdown inline code `text` to cyan
        result = InlineCodeRegex().Replace(result, "[cyan]$1[/]");

        // Convert markdown headers
        result = Header1Regex().Replace(result, "\n[bold underline]$1[/]\n");
        result = Header2Regex().Replace(result, "\n[bold]$1[/]\n");
        result = Header3Regex().Replace(result, "\n[underline]$1[/]\n");

        // Convert markdown bullet points
        result = BulletRegex().Replace(result, "  • ");

        // Convert markdown numbered lists (simple conversion)
        result = NumberedListRegex().Replace(result, "  $1. ");

        return result;
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
    /// Render a markdown table using Spectre.Console Table.
    /// </summary>
    private static void RenderTable(string tableMarkdown)
    {
        var lines = tableMarkdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            // Not enough lines for a valid table
            AnsiConsole.Markup(ToSpectreMarkup(tableMarkdown));
            return;
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

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
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
}
