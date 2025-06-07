// <copyright file="MarkdownNoteProcessor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Tools/MarkdownGeneration/MarkdownNoteProcessor.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.MarkdownGeneration;

/// <summary>
/// Provides functionality for converting HTML, TXT, or EPUB files to markdown notes, with optional AI-generated summaries.
/// </summary>
/// <remarks>
/// <para>
/// This class integrates with the AI summarizer and markdown note builder to process input files and generate
/// markdown notes. It supports:
/// <list type="bullet">
/// <item><description>TXT file conversion</description></item>
/// <item><description>HTML file conversion (basic tag stripping)</description></item>
/// <item><description>EPUB file parsing and conversion</description></item>
/// <item><description>Optional AI summarization using OpenAI API</description></item>
/// </list>
/// </para>
/// <para>
/// The class logs errors for unsupported file types or failed operations and provides detailed diagnostic information.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var processor = new MarkdownNoteProcessor(logger, aiSummarizer);
/// var markdown = await processor.ConvertToMarkdownAsync("input.html", openAiApiKey: "your-api-key", promptFileName: "summary_prompt");
/// Console.WriteLine(markdown);
/// </code>
/// </example>
public partial class MarkdownNoteProcessor
{
    private readonly ILogger<MarkdownNoteProcessor> logger;
    private readonly MarkdownNoteBuilder noteBuilder;
    private readonly AISummarizer aiSummarizer;
    private readonly MetadataHierarchyDetector hierarchyDetector;
    private readonly AppConfig? appConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownNoteProcessor"/> class.
    /// </summary>    /// <param name="logger">The logger instance.</param>
    /// <param name="aiSummarizer">The AI summarizer instance.</param>
    /// <param name="hierarchyDetector">The metadata hierarchy detector for extracting metadata from directory structure.</param>
    /// <param name="appConfig">Optional application configuration for advanced hierarchy detection.</param>
    /// <remarks>
    /// <para>
    /// This constructor initializes the markdown note builder and AI summarizer, ensuring all dependencies are valid.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var processor = new MarkdownNoteProcessor(logger, aiSummarizer, hierarchyDetector);
    /// </code>
    /// </example>
    public MarkdownNoteProcessor(ILogger logger, AISummarizer aiSummarizer, MetadataHierarchyDetector hierarchyDetector, AppConfig? appConfig = null)
    {
        if (logger is ILogger<MarkdownNoteProcessor> genericLogger)
        {
            this.logger = genericLogger;
        }
        else
        {
            // Allow any ILogger for testing/mocking, but warn if not the expected type
            this.logger = logger as ILogger<MarkdownNoteProcessor> ?? throw new ArgumentException("Logger must be ILogger<MarkdownNoteProcessor> or compatible mock");
            if (logger.GetType().Name.Contains("Mock") || logger.GetType().Name.Contains("Proxy"))
            {
                // Allow for test mocks
            }
            else
            {
                throw new ArgumentException("Logger must be ILogger<MarkdownNoteProcessor>");
            }
        }

        this.noteBuilder = new MarkdownNoteBuilder(logger);
        this.aiSummarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer));
        this.hierarchyDetector = hierarchyDetector ?? throw new ArgumentNullException(nameof(hierarchyDetector));
        this.appConfig = appConfig;
    }

    /// <summary>
    /// Converts a TXT, HTML, or EPUB file to markdown, with optional AI-generated summary.
    /// </summary>
    /// <param name="inputPath">Path to the input file.</param>
    /// <param name="openAiApiKey">OpenAI API key (optional).</param>
    /// <param name="promptFileName">Prompt file for AI summary (optional).</param>
    /// <returns>Markdown note as a string.</returns>
    /// <remarks>
    /// <para>
    /// This method processes the input file based on its extension and converts it to markdown. Supported file types:
    /// <list type="bullet">
    /// <item><description>TXT: Reads the file content directly</description></item>
    /// <item><description>HTML: Strips HTML tags to extract text</description></item>
    /// <item><description>EPUB: Parses the EPUB file and extracts text from its reading order</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If the OpenAI API key and prompt file name are provided, the method generates an AI summary for the content.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var markdown = await processor.ConvertToMarkdownAsync("input.html", openAiApiKey: "your-api-key", promptFileName: "summary_prompt");
    /// Console.WriteLine(markdown);
    /// </code>
    /// </example>
    public async Task<string> ConvertToMarkdownAsync(string inputPath, string? openAiApiKey = null, string? promptFileName = null)
    {
        if (!File.Exists(inputPath))
        {
            this.logger.LogError("Input file not found: {InputPath}", inputPath);
            return string.Empty;
        }

        string ext = Path.GetExtension(inputPath).ToLowerInvariant();
        string rawText;
        if (ext == ".txt")
        {
            rawText = await File.ReadAllTextAsync(inputPath).ConfigureAwait(false);
        }
        else if (ext == ".html" || ext == ".htm")
        {
            rawText = await File.ReadAllTextAsync(inputPath).ConfigureAwait(false);

            // TODO: Use a real HTML-to-markdown converter (e.g., ReverseMarkdown)
            rawText = StripHtmlTags(rawText);
        }
        else if (ext == ".epub")
        {
            try
            {
                // Use VersOne.Epub for EPUB parsing
                // Install-Package VersOne.Epub
                var epubText = new StringBuilder();
                var book = await VersOne.Epub.EpubReader.ReadBookAsync(inputPath).ConfigureAwait(false);
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
                this.logger.LogError(ex, "Failed to parse EPUB file: {InputPath}", inputPath);
                return string.Empty;
            }
        }
        else
        {
            this.logger.LogError("Unsupported file type: {Ext}", ext);
            return string.Empty;
        }

        string aiSummary = rawText;
        if (!string.IsNullOrWhiteSpace(openAiApiKey))
        {
            // Use the new method name to avoid ambiguity
            Dictionary<string, string>? noVariables = null;
            aiSummary = await this.aiSummarizer.SummarizeWithVariablesAsync(rawText, noVariables, promptFileName).ConfigureAwait(false) ?? rawText;
        }

        var metadata = new Dictionary<string, object>
        {
            { "title", Path.GetFileNameWithoutExtension(inputPath) },
            { "source_file", inputPath },
            { "generated", DateTime.UtcNow.ToString("u") },
        };

        // Extract hierarchy information using injected MetadataHierarchyDetector
        this.logger.LogDebug("Extracting hierarchy information from file path: {FilePath}", inputPath);
        var hierarchyInfo = this.hierarchyDetector.FindHierarchyInfo(inputPath);
        MetadataHierarchyDetector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, "module");

        return this.noteBuilder.BuildNote(metadata, aiSummary);
    }

    /// <summary>
    /// Removes HTML tags from the provided text, leaving only the inner content.
    /// </summary>
    /// <param name="html">The HTML string to process.</param>
    /// <returns>A plain text string with all HTML tags removed.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the <see cref="HtmlTagStripperRegex"/> to match and remove HTML tags.
    /// It is intended as a placeholder and should be replaced with a more robust HTML-to-markdown converter
    /// for production use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string result = StripHtmlTags("<div>Hello, <span>world!</span></div>");
    /// Console.WriteLine(result); // Outputs: "Hello, world!"
    /// </code>
    /// </example>
    private static string StripHtmlTags(string html)
    {
        // Simple HTML tag stripper for placeholder; replace with a real converter for production
        return HtmlTagStripperRegex().Replace(html, string.Empty);
    }

    /// <summary>
    /// Matches HTML tags (e.g., ".<div>", "<span>") for stripping them from text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This regex is used to remove HTML tags from text content, leaving only the inner text.
    /// It matches any text enclosed within angle brackets.
    /// </para>
    /// </remarks>
    private static Regex HtmlTagStripperRegex() => new Regex("<.*?>");
}
