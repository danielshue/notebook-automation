using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace NotebookAutomation.Core.Tools.PdfProcessing
{
    /// <summary>
    /// Provides functionality for extracting text from PDF files and generating markdown notes.
    /// </summary>
    public class PdfNoteProcessor : Shared.DocumentNoteProcessorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfNoteProcessor"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        public PdfNoteProcessor(ILogger logger) : base(logger) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfNoteProcessor"/> class with logger and AI summarizer.
        /// </summary>
        /// <param name="logger">Logger for diagnostics.</param>
        /// <param name="aiSummarizer">The AISummarizer service for generating AI-powered summaries.</param>
        public PdfNoteProcessor(ILogger logger, Services.AISummarizer aiSummarizer) : base(logger, aiSummarizer) { }

        /// <summary>
        /// Extracts text and metadata from a PDF file.
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file.</param>
        /// <returns>Tuple of extracted text and metadata dictionary.</returns>
        public override async Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string pdfPath)
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
            // Use base implementation for consistent formatting
            return base.GenerateMarkdownNote(pdfText, metadata, "PDF Note");
        }
    }
}
