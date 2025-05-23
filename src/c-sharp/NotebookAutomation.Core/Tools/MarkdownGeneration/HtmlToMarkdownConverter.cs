// Module: HtmlToMarkdownConverter.cs
// Provides real HTML-to-Markdown conversion using ReverseMarkdown.NET.
using System;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Tools.MarkdownGeneration
{
    /// <summary>
    /// Converts HTML content to Markdown using the ReverseMarkdown.NET library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The HtmlToMarkdownConverter class is responsible for transforming HTML content into well-formatted Markdown
    /// that can be used in Obsidian notes and other Markdown-based documentation systems.
    /// </para>
    /// <para>
    /// This converter is used within the <see cref="MarkdownNoteProcessor"/> system to process HTML content
    /// from various sources such as web pages, EPUB books, or HTML documents and convert them to
    /// the standardized Markdown format used throughout the notebook automation system.
    /// </para>
    /// <para>
    /// The converter uses ReverseMarkdown.NET, which provides proper handling of HTML elements including:
    /// <list type="bullet">
    /// <item><description>Headers (h1-h6)</description></item>
    /// <item><description>Lists (ordered and unordered)</description></item>
    /// <item><description>Tables</description></item>
    /// <item><description>Links and images</description></item>
    /// <item><description>Text formatting (bold, italic, etc.)</description></item>
    /// <item><description>Code blocks and inline code</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The class provides error handling to ensure graceful degradation in case of malformed HTML
    /// or conversion failures.
    /// </para>
    /// </remarks>
    public class HtmlToMarkdownConverter
    {
        /// <summary>
        /// The logger instance used for diagnostic and error reporting.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The ReverseMarkdown converter instance that performs the actual HTML-to-Markdown conversion.
        /// </summary>
        private readonly ReverseMarkdown.Converter _converter;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlToMarkdownConverter"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
        /// <remarks>
        /// Initializes the ReverseMarkdown.NET converter with default settings suitable for
        /// converting HTML content to Markdown compatible with Obsidian and other Markdown processors.
        /// </remarks>
        public HtmlToMarkdownConverter(ILogger logger)
        {
            _logger = logger;
            _converter = new ReverseMarkdown.Converter();
        }

        /// <summary>
        /// Converts HTML content to Markdown format.
        /// </summary>
        /// <param name="html">The HTML string to convert to Markdown.</param>
        /// <returns>
        /// A string containing the Markdown representation of the provided HTML.
        /// If conversion fails, returns the original HTML string.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method transforms HTML content into well-formatted Markdown text that can be used
        /// in Obsidian notes or other Markdown-based documentation systems.
        /// </para>
        /// <para>
        /// In case of conversion errors, the method logs the error details and returns the original
        /// HTML string, ensuring that no content is lost even if the conversion process fails.
        /// </para>
        /// <para>
        /// The conversion process handles common HTML elements like headings, lists, links, images,
        /// tables, and text formatting, preserving the semantic structure of the original content.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var converter = new HtmlToMarkdownConverter(logger);
        /// string html = "&lt;h1&gt;Hello World&lt;/h1&gt;&lt;p&gt;This is a &lt;strong&gt;test&lt;/strong&gt;.&lt;/p&gt;";
        /// string markdown = converter.Convert(html);
        /// // Result: "# Hello World\n\nThis is a **test**."
        /// </code>
        /// </example>
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
