using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Shared
{    /// <summary>
    /// Abstract base class for document note processors (PDF, video, etc.).
    /// Provides shared logic for AI summary, markdown generation, and logging.
    /// </summary>
    public abstract class DocumentNoteProcessorBase
    {
        protected readonly ILogger _logger;
        protected readonly AISummarizer? _aiSummarizer;

        /// <summary>
        /// Initializes a new instance of the DocumentNoteProcessorBase class with logger only.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        protected DocumentNoteProcessorBase(ILogger logger)
        {
            _logger = logger;
            _aiSummarizer = null;
        }

        /// <summary>
        /// Initializes a new instance of the DocumentNoteProcessorBase class with logger and AISummarizer.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="aiSummarizer">The AISummarizer instance for generating AI-powered summaries.</param>
        protected DocumentNoteProcessorBase(ILogger logger, AISummarizer aiSummarizer)
        {
            _logger = logger;
            _aiSummarizer = aiSummarizer;
        }

        /// <summary>
        /// Extracts the main text/content and metadata from the document.
        /// </summary>
        /// <param name="filePath">Path to the document file.</param>
        /// <returns>Tuple of extracted text/content and metadata dictionary.</returns>
        public abstract Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string filePath);        /// <summary>
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
            
            // Use the injected AISummarizer if available, otherwise create a new one
            // Note: Creating a new one will likely fail without proper DI setup
            var summarizer = _aiSummarizer ?? new AISummarizer(_logger);
            
            _logger.LogDebug("Using AISummarizer to generate summary.");
            var summary = await summarizer.SummarizeAsync(text, prompt, promptFileName);
            
            if (summary == null && _aiSummarizer == null)
            {
                _logger.LogWarning("AISummarizer not properly injected, summary generation failed. Please update implementation to use dependency injection.");
            }
            
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
