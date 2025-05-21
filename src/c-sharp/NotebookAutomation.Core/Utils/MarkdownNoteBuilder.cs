using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Provides a reusable way to build markdown notes with YAML frontmatter.
    /// </summary>
    public class MarkdownNoteBuilder
    {
        private readonly ILogger? _logger;
        private readonly YamlHelper _yamlHelper;

        public MarkdownNoteBuilder(ILogger? logger = null)
        {
            _logger = logger;
            _yamlHelper = new YamlHelper(logger);
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
