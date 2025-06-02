using System.Globalization;
using System.Text.RegularExpressions;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Provides utility methods for generating friendly titles from file names or paths.
    /// </summary>
    public static partial class FriendlyTitleHelper
    {
        /// <summary>
        /// Returns a friendly title from a file name (removes numbers, underscores, extension, trims, and capitalizes).
        /// </summary>
        /// <param name="fileName">The file name or path to process.</param>
        /// <returns>A cleaned, human-friendly title string.</returns>
        public static string GetFriendlyTitleFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "Title";            // Remove leading numbers and separators (handle multiple consecutive patterns)
            string name = fileName;
            while (MyRegex().IsMatch(name))
            {
                name = MyRegex().Replace(name, "");
            }

            // Replace dashes/underscores with spaces
            name = Regex.Replace(name, "[_\\-]+", " ");            // Remove common structural words
            string[] wordsToRemove = [
                @"\blesson\b", @"\blessons\b", @"\bmodule\b", @"\bmodules\b",
                @"\bcourse\b", @"\bcourses\b",
                @"\band\b", @"\bto\b", @"\bof\b"
            ];
            string pattern = string.Join("|", wordsToRemove);
            name = Regex.Replace(name, pattern, " ", RegexOptions.IgnoreCase);

            // Clean up whitespace
            name = Regex.Replace(name, "\\s+", " ").Trim();

            // Use fallback for short titles
            if (name.Length < 3)
                return "Content";

            // Title case and fix special cases
            string titleCased = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);

            // Fix Roman numerals ("Ii" to "II")
            titleCased = Regex.Replace(titleCased, @"\bIi\b", "II");

            // Fix known acronyms to all caps
            string[] knownAcronyms = ["CVP", "ROI", "KPI", "MBA", "CEO", "CFO", "COO", "CTO", "CIO", "CMO"];
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

        [GeneratedRegex(@"^[0-9]+([_\-\s]+)", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex MyRegex();
    }
}
