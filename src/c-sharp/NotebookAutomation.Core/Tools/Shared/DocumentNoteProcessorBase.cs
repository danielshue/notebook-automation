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
        protected readonly ILogger Logger;
        protected readonly AISummarizer Summarizer;

        /// <summary>
        /// Initializes a new instance of the DocumentNoteProcessorBase class with logger and AISummarizer.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="aiSummarizer">The AISummarizer instance for generating AI-powered summaries.</param>
        protected DocumentNoteProcessorBase(ILogger logger, AISummarizer aiSummarizer)
        {
            Logger = logger;
            Summarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer), "AISummarizer must be provided via DI.");
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
        /// <param name="variables">Optional variables to substitute in the prompt template.</param>
        /// <param name="promptFileName">Optional name of the prompt template file to use.</param>
        /// <returns>The summary text, or a simulated summary if unavailable.</returns>
        public virtual async Task<string> GenerateAiSummaryAsync(string text, Dictionary<string, string>? variables = null, string? promptFileName = null)
        {
            Logger.LogDebug("Using AISummarizer to generate summary.");

            if (Summarizer == null)
            {
                return "[Simulated AI summary]";
            }

            var summary = await Summarizer.SummarizeWithVariablesAsync(text, variables, promptFileName);
            if (string.IsNullOrWhiteSpace(summary))
            {
                Logger.LogWarning("AISummarizer returned an empty summary. Using simulated summary.");
            }
            return summary ?? "[Simulated AI summary]";
        }

        /// <summary>
        /// Generates a markdown note from extracted text and metadata.
        /// </summary>
        /// <param name="bodyText">The extracted text/content.</param>
        /// <param name="metadata">Optional metadata for the note.</param>
        /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
        /// <returns>The generated markdown content.</returns>
        public virtual string GenerateMarkdownNote(string bodyText, Dictionary<string, object>? metadata = null, string noteType = "Document Note", bool suppressBody = false)
        {
            var frontmatter = metadata ?? new Dictionary<string, object> { { "title", $"Untitled {noteType}" } };
            var builder = new MarkdownNoteBuilder(Logger);
            if (suppressBody)
            {
                return builder.CreateMarkdownWithFrontmatter(frontmatter);
            }
            var markdownBody = $"# {noteType}\n\n{bodyText}";
            return builder.BuildNote(frontmatter, markdownBody);
        }
    }
}
