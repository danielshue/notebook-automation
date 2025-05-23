using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Shared
{
    /// <summary>
    /// Abstract base class for document note processors (PDF, video, etc.).
    /// Provides shared logic for AI summary, markdown generation, and logging.
    /// </summary>
    public abstract class DocumentNoteProcessorBase
    {
        protected readonly ILogger _logger;

        protected DocumentNoteProcessorBase(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Extracts the main text/content and metadata from the document.
        /// </summary>
        /// <param name="filePath">Path to the document file.</param>
        /// <returns>Tuple of extracted text/content and metadata dictionary.</returns>
        public abstract Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string filePath);

        /// <summary>
        /// Generates an AI summary for the given text using OpenAI.
        /// </summary>
        /// <param name="text">The extracted text/content.</param>
        /// <param name="openAiApiKey">The OpenAI API key.</param>
        /// <param name="prompt">Optional prompt for the summary.</param>
        /// <param name="promptFileName">Optional prompt file name for the summary.</param>
        /// <returns>The summary text, or a simulated summary if API key is missing.</returns>
        public virtual async Task<string> GenerateAiSummaryAsync(string text, string? openAiApiKey, string? prompt = null, string? promptFileName = null)
        {
            if (string.IsNullOrWhiteSpace(openAiApiKey))
            {
                _logger.LogWarning("No OpenAI API key provided. Using simulated summary.");
                return "[Simulated AI summary]";
            }
            var summarizer = new OpenAiSummarizer(_logger, openAiApiKey);
            var summary = await summarizer.SummarizeAsync(text, prompt, promptFileName);
            return summary ?? "[AI summary unavailable]";
        }

        /// <summary>
        /// Generates a markdown note from extracted text and metadata.
        /// </summary>
        /// <param name="bodyText">The extracted text/content.</param>
        /// <param name="metadata">Optional metadata for the note.</param>
        /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
        /// <returns>The generated markdown content.</returns>
        public virtual string GenerateMarkdownNote(string bodyText, Dictionary<string, object>? metadata = null, string noteType = "Document Note")
        {
            var frontmatter = metadata ?? new Dictionary<string, object> { { "title", $"Untitled {noteType}" } };
            var markdownBody = $"# {noteType}\n\n{bodyText}";
            var builder = new MarkdownNoteBuilder(_logger);
            return builder.BuildNote(frontmatter, markdownBody);
        }
    }
}
