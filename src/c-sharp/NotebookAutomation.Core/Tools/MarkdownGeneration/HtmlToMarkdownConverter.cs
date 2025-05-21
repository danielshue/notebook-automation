// Module: HtmlToMarkdownConverter.cs
// Provides real HTML-to-Markdown conversion using ReverseMarkdown.NET.
using System;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Tools.MarkdownGeneration
{
    /// <summary>
    /// Converts HTML content to Markdown using ReverseMarkdown.NET.
    /// </summary>
    public class HtmlToMarkdownConverter
    {
        private readonly ILogger _logger;
        private readonly ReverseMarkdown.Converter _converter;

        public HtmlToMarkdownConverter(ILogger logger)
        {
            _logger = logger;
            _converter = new ReverseMarkdown.Converter();
        }

        /// <summary>
        /// Converts HTML to Markdown.
        /// </summary>
        /// <param name="html">HTML string.</param>
        /// <returns>Markdown string.</returns>
        public string Convert(string html)
        {
            try
            {
                return _converter.Convert(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert HTML to Markdown");
                return html;
            }
        }
    }
}
