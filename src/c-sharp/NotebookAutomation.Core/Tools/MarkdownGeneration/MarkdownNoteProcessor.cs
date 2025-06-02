using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.MarkdownGeneration
{
    /// <summary>
    /// Provides functionality for converting HTML or TXT files to markdown notes, with optional AI summary.
    /// </summary>
    public partial class MarkdownNoteProcessor
    {
        private readonly ILogger<MarkdownNoteProcessor> _logger;
        private readonly MarkdownNoteBuilder _noteBuilder;
        private readonly AISummarizer _aiSummarizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownNoteProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="aiSummarizer">The AI summarizer instance.</param>
        public MarkdownNoteProcessor(ILogger logger, AISummarizer aiSummarizer)
        {
            if (logger is ILogger<MarkdownNoteProcessor> genericLogger)
            {
                _logger = genericLogger;
            }
            else
            {
                // Allow any ILogger for testing/mocking, but warn if not the expected type
                _logger = logger as ILogger<MarkdownNoteProcessor> ?? throw new ArgumentException("Logger must be ILogger<MarkdownNoteProcessor> or compatible mock");
                if (logger.GetType().Name.Contains("Mock") || logger.GetType().Name.Contains("Proxy"))
                {
                    // Allow for test mocks
                }
                else
                {
                    throw new ArgumentException("Logger must be ILogger<MarkdownNoteProcessor>");
                }
            }
            _noteBuilder = new MarkdownNoteBuilder(logger);
            _aiSummarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer));
        }

        /// <summary>
        /// Converts a TXT or HTML file to markdown, with optional AI summary.
        /// </summary>
        /// <param name="inputPath">Path to the input file.</param>
        /// <param name="openAiApiKey">OpenAI API key (optional).</param>
        /// <param name="promptFileName">Prompt file for AI summary (optional).</param>
        /// <returns>Markdown note as a string.</returns>
        public async Task<string> ConvertToMarkdownAsync(string inputPath, string? openAiApiKey = null, string? promptFileName = null)
        {
            if (!File.Exists(inputPath))
            {
                _logger.LogError("Input file not found: {InputPath}", inputPath);
                return string.Empty;
            }
            string ext = Path.GetExtension(inputPath).ToLowerInvariant();
            string rawText;
            if (ext == ".txt")
            {
                rawText = await File.ReadAllTextAsync(inputPath);
            }
            else if (ext == ".html" || ext == ".htm")
            {
                rawText = await File.ReadAllTextAsync(inputPath);
                // TODO: Use a real HTML-to-markdown converter (e.g., ReverseMarkdown)
                rawText = StripHtmlTags(rawText);
            }
            else if (ext == ".epub")
            {
                try
                {
                    // Use VersOne.Epub for EPUB parsing
                    // Install-Package VersOne.Epub
                    var epubText = new System.Text.StringBuilder();
                    var book = await VersOne.Epub.EpubReader.ReadBookAsync(inputPath);
                    if (book != null && book.ReadingOrder != null)
                    {
                        foreach (var htmlContentFile in book.ReadingOrder)
                        {
                            if (!string.IsNullOrWhiteSpace(htmlContentFile.Content))
                            {
                                // TODO: Use a real HTML-to-markdown converter
                                epubText.AppendLine(StripHtmlTags(htmlContentFile.Content));
                            }
                        }
                    }
                    rawText = epubText.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse EPUB file: {InputPath}", inputPath);
                    return string.Empty;
                }
            }
            else
            {
                _logger.LogError("Unsupported file type: {Ext}", ext);
                return string.Empty;
            }
            string aiSummary = rawText;
            if (!string.IsNullOrWhiteSpace(openAiApiKey))
            {                // Use the new method name to avoid ambiguity
                Dictionary<string, string>? noVariables = null;
                aiSummary = await _aiSummarizer.SummarizeWithVariablesAsync(rawText, noVariables, promptFileName) ?? rawText;
            }
            var metadata = new Dictionary<string, object>
            {
                { "title", Path.GetFileNameWithoutExtension(inputPath) },
                { "source_file", inputPath },
                { "generated", DateTime.UtcNow.ToString("u") }
            };
            return _noteBuilder.BuildNote(metadata, aiSummary);
        }

        private static string StripHtmlTags(string html)
        {
            // Simple HTML tag stripper for placeholder; replace with a real converter for production
            return MyRegex().Replace(html, string.Empty);
        }

        [System.Text.RegularExpressions.GeneratedRegex("<.*?>")]
        private static partial System.Text.RegularExpressions.Regex MyRegex();
    }
}
