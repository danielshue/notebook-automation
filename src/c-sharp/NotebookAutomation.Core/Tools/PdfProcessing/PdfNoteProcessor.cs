using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.PdfProcessing
{
    /// <summary>
    /// Provides functionality for extracting text from PDF files and generating markdown notes.
    /// </summary>
    public class PdfNoteProcessor
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfNoteProcessor"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        public PdfNoteProcessor(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Extracts text and metadata from a PDF file.
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file.</param>
        /// <returns>Tuple of extracted text and metadata dictionary.</returns>
        public async Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string pdfPath)
        {
            var metadata = new Dictionary<string, object>();
            if (!File.Exists(pdfPath))
            {
                _logger.LogError("PDF file not found: {PdfPath}", pdfPath);
                return (string.Empty, metadata);
            }
            string extractedText = string.Empty;
            try
            {
                extractedText = await Task.Run(() =>
                {
                    var sb = new System.Text.StringBuilder();
                    using (PdfDocument document = PdfDocument.Open(pdfPath))
                    {
                        sb.AppendLine();
                        foreach (Page page in document.GetPages())
                        {
                            sb.AppendLine(page.Text);
                        }
                        // Collect metadata after reading pages
                        metadata["page_count"] = document.NumberOfPages;
                        metadata["file_name"] = Path.GetFileName(pdfPath);
                        metadata["source_file"] = pdfPath;
                        metadata["generated"] = DateTime.UtcNow.ToString("u");
                        var info = document.Information;
                        if (!string.IsNullOrWhiteSpace(info?.Title))
                            metadata["title"] = info.Title;
                        if (!string.IsNullOrWhiteSpace(info?.Author))
                            metadata["author"] = info.Author;
                        if (!string.IsNullOrWhiteSpace(info?.Subject))
                            metadata["subject"] = info.Subject;
                        if (!string.IsNullOrWhiteSpace(info?.Keywords))
                            metadata["keywords"] = info.Keywords;
                    }
                    return sb.ToString();
                });
                _logger.LogInformation("Extracted text and metadata from PDF: {PdfPath}", pdfPath);
                return (extractedText, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from PDF: {PdfPath}", pdfPath);
                return (string.Empty, metadata);
            }
        }

        /// <summary>
        /// Generates a markdown note from extracted PDF text and metadata.
        /// </summary>
        /// <param name="pdfText">The extracted PDF text.</param>
        /// <param name="metadata">Optional metadata for the note.</param>
        /// <returns>The generated markdown content.</returns>
        public string GenerateMarkdownNote(string pdfText, Dictionary<string, object>? metadata = null)
        {
            var frontmatter = metadata ?? new Dictionary<string, object> { { "title", "Untitled PDF Note" } };
            var markdownBody = $"# PDF Note\n\n{pdfText}";
            var builder = new MarkdownNoteBuilder(_logger);
            return builder.BuildNote(frontmatter, markdownBody);
        }

        /// <summary>
        /// Generates an AI summary for the given PDF text using OpenAI.
        /// </summary>
        /// <param name="pdfText">The extracted PDF text.</param>
        /// <param name="openAiApiKey">The OpenAI API key.</param>
        /// <param name="prompt">Optional prompt for the summary.</param>
        /// <param name="promptFileName">Optional prompt file name for the summary.</param>
        /// <returns>The summary text, or a simulated summary if API key is missing.</returns>
        public async Task<string> GenerateAiSummaryAsync(string pdfText, string? openAiApiKey, string? prompt = null, string? promptFileName = null)
        {
            if (string.IsNullOrWhiteSpace(openAiApiKey))
            {
                _logger.LogWarning("No OpenAI API key provided. Using simulated summary.");
                return "[Simulated AI summary of PDF]";
            }
            var summarizer = new OpenAiSummarizer(_logger, openAiApiKey);
            var summary = await summarizer.SummarizeAsync(pdfText, prompt, promptFileName);
            return summary ?? "[AI summary unavailable]";
        }
    }
}
