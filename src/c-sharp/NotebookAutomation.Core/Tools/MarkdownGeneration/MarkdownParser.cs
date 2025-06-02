using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.MarkdownGeneration
{
    /// <summary>
    /// Parser for markdown content with frontmatter handling.
    /// 
    /// This class provides functionality for parsing and manipulating markdown content,
    /// including frontmatter extraction, content formatting, and structure analysis.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the MarkdownParser.
    /// </remarks>
    /// <param name="logger">Logger for diagnostics.</param>
    public partial class MarkdownParser(ILogger logger)
    {
        private readonly ILogger _logger = logger;
        private readonly YamlHelper _yamlHelper = new YamlHelper(logger);

        /// <summary>
        /// Regular expression for detecting YAML frontmatter.
        /// </summary>
        public static readonly Regex FrontmatterRegex = MyRegex();

        /// <summary>
        /// Extracts the frontmatter and content from a markdown file.
        /// </summary>
        /// <param name="filePath">Path to the markdown file.</param>
        /// <returns>A tuple containing the frontmatter dictionary and the content body.</returns>
        public async Task<(Dictionary<string, object> Frontmatter, string Content)> ParseFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return (new Dictionary<string, object>(), string.Empty);
            }

            try
            {
                string text = await File.ReadAllTextAsync(filePath);
                return ParseMarkdown(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing markdown file: {FilePath}", filePath);
                return (new Dictionary<string, object>(), string.Empty);
            }
        }

        /// <summary>
        /// Parses markdown text into frontmatter and content.
        /// </summary>
        /// <param name="markdownText">The full markdown text.</param>
        /// <returns>A tuple containing the frontmatter dictionary and the content body.</returns>
        public (Dictionary<string, object> Frontmatter, string Content) ParseMarkdown(string markdownText)
        {
            if (string.IsNullOrEmpty(markdownText))
            {
                return (new Dictionary<string, object>(), string.Empty);
            }

            var match = FrontmatterRegex.Match(markdownText);
            if (!match.Success)
            {
                return (new Dictionary<string, object>(), markdownText);
            }

            var frontmatterYaml = match.Groups[1].Value;
            var content = markdownText[match.Length..];
            var frontmatter = _yamlHelper.ParseYamlToDictionary(frontmatterYaml);

            return (frontmatter, content);
        }

        /// <summary>
        /// Combines frontmatter and content into a complete markdown document.
        /// </summary>
        /// <param name="frontmatter">The frontmatter dictionary.</param>
        /// <param name="content">The content body.</param>
        /// <returns>The complete markdown document.</returns>
        public string CombineMarkdown(Dictionary<string, object> frontmatter, string content)
        {
            return _yamlHelper.UpdateFrontmatter(content, frontmatter);
        }

        /// <summary>
        /// Writes a markdown document to a file.
        /// </summary>
        /// <param name="filePath">The target file path.</param>
        /// <param name="frontmatter">The frontmatter dictionary.</param>
        /// <param name="content">The content body.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public async Task<bool> WriteFileAsync(string filePath, Dictionary<string, object> frontmatter, string content)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                var fullContent = CombineMarkdown(frontmatter, content);
                await File.WriteAllTextAsync(filePath, fullContent, Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing markdown file: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Extracts the header level and title from a markdown header line.
        /// </summary>
        /// <param name="headerLine">The header line to parse.</param>
        /// <returns>A tuple containing the header level and title.</returns>
        public static (int Level, string Title) ParseHeader(string headerLine)
        {
            if (string.IsNullOrEmpty(headerLine))
            {
                return (0, string.Empty);
            }

            var match = Regex.Match(headerLine, @"^(#+)\s+(.+)$");
            if (!match.Success)
            {
                return (0, headerLine.Trim());
            }

            var level = match.Groups[1].Value.Length;
            var title = match.Groups[2].Value.Trim();

            return (level, title);
        }

        /// <summary>
        /// Extracts all headers from markdown content.
        /// </summary>
        /// <param name="content">The markdown content to parse.</param>
        /// <returns>A list of header level and title tuples.</returns>
        public static List<(int Level, string Title, int LineNumber)> ExtractHeaders(string content)
        {
            var result = new List<(int Level, string Title, int LineNumber)>();
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimStart();
                if (line.StartsWith("#"))
                {
                    var (level, title) = ParseHeader(line);
                    if (level > 0)
                    {
                        result.Add((level, title, i));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Sanitizes a string for use in a filename.
        /// </summary>
        /// <param name="input">The input string to sanitize.</param>
        /// <returns>A sanitized filename-safe string.</returns>
        public static string SanitizeForFilename(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "unnamed";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(input);

            foreach (var c in invalidChars)
            {
                sb.Replace(c, '-');
            }

            return sb.ToString()
                .Replace(' ', '-')
                .Replace('.', '-')
                .ToLowerInvariant();
        }

        [GeneratedRegex(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline)]
        private static partial Regex MyRegex();
    }
}
