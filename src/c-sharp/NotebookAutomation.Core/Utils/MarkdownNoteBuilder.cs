using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Provides a reusable way to build markdown notes with YAML frontmatter.
    /// </summary>
    public class MarkdownNoteBuilder(ILogger? logger = null)
    {
        private readonly ILogger? _logger = logger;
        private readonly YamlHelper _yamlHelper = new(logger);

        /// <summary>
        /// Builds a markdown note with only YAML frontmatter (no body).
        /// </summary>
        /// <param name="frontmatter">Frontmatter dictionary.</param>
        /// <returns>Markdown note as a string with only frontmatter.</returns>
        public string CreateMarkdownWithFrontmatter(Dictionary<string, object> frontmatter)
        {
            var yaml = _yamlHelper.UpdateFrontmatter(string.Empty, frontmatter);
            // Remove any trailing newlines or content after frontmatter
            int end = yaml.IndexOf("---", 3, StringComparison.Ordinal);
            if (end > 0)
            {
                return yaml[..(end + 3)] + "\n\n";
            }
            return yaml.TrimEnd() + "\n\n";
        }

        /// <summary>
        /// Builds a markdown note with YAML frontmatter and content body.
        /// </summary>
        /// <param name="frontmatter">Frontmatter dictionary.</param>
        /// <param name="body">Markdown body content.</param>
        /// <returns>Markdown note as a string.</returns>
        public string BuildNote(Dictionary<string, object> frontmatter, string body)
        {
            return _yamlHelper.UpdateFrontmatter(body, frontmatter);
        }
    }
}
