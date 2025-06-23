// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Provides utility methods for generating friendly titles from file names or paths.
/// </summary>
/// <remarks>
/// The <c>FriendlyTitleHelper</c> class offers static methods to convert file names or paths
/// into human-friendly titles by removing numbers, structural words, and formatting the result
/// in title case. It also handles special cases such as acronyms and Roman numerals.
/// </remarks>
public static partial class FriendlyTitleHelper
{
    /// <summary>
    /// Returns a friendly title from a file name (removes numbers, underscores, extension, trims, and capitalizes).
    /// </summary>
    /// <param name="fileName">The file name or path to process.</param>
    /// <returns>A cleaned, human-friendly title string.</returns>
    public static string GetFriendlyTitleFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Title";            // Remove leading numbers and separators (handle multiple consecutive patterns)
        }

        string name = fileName;
        while (LeadingNumberPattern().IsMatch(name))
        {
            name = LeadingNumberPattern().Replace(name, string.Empty);
        }

        // Replace dashes/underscores with spaces
        name = SeparatorPattern().Replace(name, " ");

        // Remove common structural words
        string[] wordsToRemove = [
            @"\blesson\b", @"\blessons\b", @"\bmodule\b", @"\bmodules\b",
            @"\bcourse\b", @"\bcourses\b",
            @"\band\b", @"\bto\b", @"\bof\b"
        ];
        string pattern = string.Join("|", wordsToRemove);
        name = Regex.Replace(name, pattern, " ", RegexOptions.IgnoreCase);

        // Clean up whitespace
        name = WhitespacePattern().Replace(name, " ").Trim();

        // Use fallback for short titles
        if (name.Length < 3)
        {
            return "Content";
        }

        // Title case and fix special cases
        string titleCased = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);

        // Fix Roman numerals ("Ii" to "II")
        titleCased = RomanNumeralIIPattern().Replace(titleCased, "II");

        // Fix known acronyms to all caps
        string[] knownAcronyms = ["CVP", "ROI", "KPI", "MBA", "CEO", "CFO", "COO", "CTO", "CIO", "CMO", "IPO"];
        foreach (var acronym in knownAcronyms)
        {
            titleCased = Regex.Replace(
                titleCased,
                $"\\b{acronym}\\b",
                acronym,
                RegexOptions.IgnoreCase);
        }

        // Handle Roman numeral "II" after a word (e.g., "Part Ii" -> "Part II")
        titleCased = Regex.Replace(titleCased, @"\b([A-Za-z]+) Ii\b", "$1 II");

        return titleCased;
    }

    /// <summary>
    /// Matches leading numbers followed by one or more separators (underscore, dash, or whitespace) at the start of a string.
    /// Example: "01-Introduction" or "12_Module".
    /// </summary>
    [GeneratedRegex(@"^[0-9]+([_\-\s]+)", RegexOptions.IgnoreCase, "en-US")]
    internal static partial Regex LeadingNumberPattern();

    /// <summary>
    /// Matches one or more separator characters (underscore or dash) anywhere in the string.
    /// Example: "Lesson_One-Two".
    /// </summary>
    [GeneratedRegex("[_\\-]+")]
    internal static partial Regex SeparatorPattern();

    /// <summary>
    /// Matches one or more whitespace characters (spaces, tabs, etc.).
    /// Used to normalize and trim whitespace in titles.
    /// </summary>
    [GeneratedRegex("\\s+")]
    internal static partial Regex WhitespacePattern();

    /// <summary>
    /// Matches the word "Ii" as a standalone word (case-sensitive by default).
    /// Used to identify and replace Roman numeral II in titles.
    /// </summary>
    [GeneratedRegex(@"\bIi\b")]
    internal static partial Regex RomanNumeralIIPattern();
}
