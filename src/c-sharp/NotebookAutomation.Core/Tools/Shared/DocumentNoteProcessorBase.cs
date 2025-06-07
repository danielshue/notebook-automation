// <copyright file="DocumentNoteProcessorBase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Tools/Shared/DocumentNoteProcessorBase.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Shared;

/// <summary>
/// Abstract base class for document note processors (PDF, video, etc.).
/// </summary>
/// <remarks>
/// The <c>DocumentNoteProcessorBase</c> class provides shared logic for processing
/// document notes, including AI summary generation, markdown creation, and logging.
/// It serves as a foundation for specialized processors that handle specific document
/// types, such as PDFs and videos.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="aiSummarizer">The AISummarizer instance for generating AI-powered summaries.</param>
public abstract class DocumentNoteProcessorBase(ILogger logger, AISummarizer aiSummarizer)
{
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger must be provided via DI.");
    protected readonly AISummarizer Summarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer), "AISummarizer must be provided via DI.");

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
    {
        this.Logger.LogDebug("Using AISummarizer to generate summary.");

        // Check for null text
        if (text == null)
        {
            this.Logger.LogWarning("Null text provided to summarizer");
            return "[No content to summarize]";
        }

        // Log content size
        int textSize = text.Length;
        int estimatedTokens = textSize / 4; // Rough estimate: ~4 characters per token
        this.Logger.LogInformation(
            "Text to summarize: {CharCount:N0} characters (~{TokenCount:N0} estimated tokens)",
            textSize, estimatedTokens);

        // Enhanced debug logging for yaml-frontmatter
        if (variables != null)
        {
            this.Logger.LogInformation("Preparing {Count} variables for prompt template", variables.Count);
            foreach (var kvp in variables)
            {
                var preview = kvp.Value?.Length > 50 ? kvp.Value[..50] + "..." : kvp.Value;
                this.Logger.LogInformation(
                    "  Variable {Key}: {Length:N0} chars - {ValuePreview}",
                    kvp.Key, kvp.Value?.Length ?? 0, preview);
            }

            if (variables.TryGetValue("yamlfrontmatter", out var yamlValue))
            {
                this.Logger.LogInformation(
                    "Found yamlfrontmatter ({Length:N0} chars): {ValuePreview}",
                    yamlValue?.Length ?? 0,
                    yamlValue?.Length > 100 ? yamlValue[..100] + "..." : yamlValue ?? "null");
            }
            else
            {
                this.Logger.LogWarning("YAML frontmatter variable not found in variables dictionary!");
            }
        }
        else
        {
            this.Logger.LogWarning("No variables provided to summarizer, yaml-frontmatter will not be substituted!");
        }

        if (this.Summarizer == null)
        {
            this.Logger.LogWarning("AI summarizer not available - returning simulated summary");
            return "[Simulated AI summary]";
        }

        this.Logger.LogInformation(
            "Sending content to AI service for summarization (prompt: {PromptFile})",
            promptFileName ?? "default");

        var summary = await this.Summarizer.SummarizeWithVariablesAsync(text, variables, promptFileName).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(summary))
        {
            this.Logger.LogWarning("AISummarizer returned an empty summary. Using simulated summary.");
            return "[Simulated AI summary]";
        }

        int summaryLength = summary.Length;
        int summaryEstimatedTokens = summaryLength / 4;
        this.Logger.LogInformation(
            "Successfully generated AI summary: {CharCount:N0} characters (~{TokenCount:N0} estimated tokens)",
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
        var builder = new MarkdownNoteBuilder(this.Logger);
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
            this.Logger?.LogDebug("Using frontmatter title for heading: {Title}", title);
        }
        else if (includeNoteTypeTitle)
        {
            markdownBody = $"# {noteType}\n\n{bodyText}";
            this.Logger?.LogDebug("No frontmatter title found, using note type: {NoteType}", noteType);
        }
        else
        {
            markdownBody = bodyText;
            this.Logger?.LogDebug("No title added to markdown body");
        }

        return builder.BuildNote(frontmatter, markdownBody);
    }
}
