using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Shared
{
    /// <summary>
    /// Abstract base class for document note processors (PDF, video, etc.).
    /// Provides shared logic for AI summary, markdown generation, and logging.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the DocumentNoteProcessorBase class with logger and AISummarizer.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="aiSummarizer">The AISummarizer instance for generating AI-powered summaries.</param>
    public abstract class DocumentNoteProcessorBase
    {
        protected readonly ILogger Logger;
        protected readonly AISummarizer Summarizer;

        protected DocumentNoteProcessorBase(ILogger logger, AISummarizer aiSummarizer)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger must be provided via DI.");
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
        /// </summary>        /// <param name="text">The extracted text/content.</param>
        /// <param name="variables">Optional variables to substitute in the prompt template.</param>
        /// <param name="promptFileName">Optional name of the prompt template file to use.</param>
        /// <returns>The summary text, or a simulated summary if unavailable.</returns>
        public virtual async Task<string> GenerateAiSummaryAsync(string? text, Dictionary<string, string>? variables = null, string? promptFileName = null)
        {            Logger.LogInformation("Starting AI summary generation process");
            Logger.LogDebug("Using AISummarizer to generate summary.");
            
            // Check for null text
            if (text == null)
            {
                Logger.LogWarning("Null text provided to summarizer");
                return "[No content to summarize]";
            }
            
            // Log content size
            int textSize = text.Length;
            int estimatedTokens = textSize / 4; // Rough estimate: ~4 characters per token
            Logger.LogInformation("Text to summarize: {CharCount:N0} characters (~{TokenCount:N0} estimated tokens)", 
                textSize, estimatedTokens);

            // Enhanced debug logging for yaml-frontmatter
            if (variables != null)
            {
                Logger.LogInformation("Preparing {Count} variables for prompt template", variables.Count);
                foreach (var kvp in variables)
                {
                    var preview = kvp.Value?.Length > 50 ? kvp.Value[..50] + "..." : kvp.Value;
                    Logger.LogInformation("  Variable {Key}: {Length:N0} chars - {ValuePreview}", 
                        kvp.Key, kvp.Value?.Length ?? 0, preview);
                }

                if (variables.TryGetValue("yaml-frontmatter", out var yamlValue))
                {
                    Logger.LogInformation("Found yaml-frontmatter ({Length:N0} chars): {ValuePreview}",
                        yamlValue?.Length ?? 0,
                        yamlValue?.Length > 100 ? yamlValue[..100] + "..." : yamlValue ?? "null");
                }
                else
                {
                    Logger.LogWarning("YAML frontmatter variable not found in variables dictionary!");
                }
            }
            else
            {
                Logger.LogWarning("No variables provided to summarizer, yaml-frontmatter will not be substituted!");
            }

            if (Summarizer == null)
            {
                Logger.LogWarning("AI summarizer not available - returning simulated summary");
                return "[Simulated AI summary]";
            }

            Logger.LogInformation("Sending content to AI service for summarization (prompt: {PromptFile})", 
                promptFileName ?? "default");
            
            var summary = await Summarizer.SummarizeWithVariablesAsync(text, variables, promptFileName);
            
            if (string.IsNullOrWhiteSpace(summary))
            {
                Logger.LogWarning("AISummarizer returned an empty summary. Using simulated summary.");
                return "[Simulated AI summary]";
            }
            
            int summaryLength = summary.Length;
            int summaryEstimatedTokens = summaryLength / 4;
            Logger.LogInformation("Successfully generated AI summary: {CharCount:N0} characters (~{TokenCount:N0} estimated tokens)",
                summaryLength, summaryEstimatedTokens);
                
            return summary;
        }

        /// <summary>
        /// Generates a markdown note from extracted text and metadata.
        /// </summary>
        /// <param name="bodyText">The extracted text/content.</param>
        /// <param name="metadata">Optional metadata for the note.</param>
        /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
        /// <param name="suppressBody">Whether to suppress the body text and only include frontmatter.</param>
        /// <param name="includeNoteTypeTitle">Whether to include the note type as a title in the markdown.</param>
        /// <returns>The generated markdown content.</returns>
        public virtual string GenerateMarkdownNote(
            string bodyText,
            Dictionary<string, object>? metadata = null,
            string noteType = "Document Note",
            bool suppressBody = false,
            bool includeNoteTypeTitle = false)
        {
            var frontmatter = metadata ?? new Dictionary<string, object> { { "title", $"Untitled {noteType}" } };
            var builder = new MarkdownNoteBuilder(Logger);
            if (suppressBody)
            {
                return builder.CreateMarkdownWithFrontmatter(frontmatter);
            }
            string markdownBody;

            // For the title, use the friendly title from the frontmatter if available
            if (includeNoteTypeTitle && frontmatter.TryGetValue("title", out var titleObj) && titleObj != null)
            {
                string title = titleObj.ToString() ?? noteType;
                markdownBody = $"# {title}\n\n{bodyText}";
                Logger?.LogDebug("Using frontmatter title for heading: {Title}", title);
            }
            else if (includeNoteTypeTitle)
            {
                markdownBody = $"# {noteType}\n\n{bodyText}";
                Logger?.LogDebug("No frontmatter title found, using note type: {NoteType}", noteType);
            }
            else
            {
                markdownBody = bodyText;
                Logger?.LogDebug("No title added to markdown body");
            }

            return builder.BuildNote(frontmatter, markdownBody);
        }
    }
}
