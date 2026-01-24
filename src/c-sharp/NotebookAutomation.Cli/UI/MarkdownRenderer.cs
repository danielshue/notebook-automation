// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Converts markdown text to Spectre.Console markup for terminal rendering.
/// </summary>
public static partial class MarkdownRenderer
{
    /// <summary>
    /// Convert markdown text to Spectre.Console markup.
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
        var markup = ToSpectreMarkup(markdown);
        AnsiConsole.Markup(markup);
    }

    /// <summary>
    /// Render markdown as a complete line with newline.
    /// </summary>
    /// <param name="markdown">The markdown text to render.</param>
    public static void RenderLine(string markdown)
    {
        var markup = ToSpectreMarkup(markdown);
        AnsiConsole.MarkupLine(markup);
    }

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
