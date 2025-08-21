// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using ReverseMarkdown;

namespace NotebookAutomation.Core.Tools.MarkdownGeneration;

/// <summary>
/// Provides functionality for converting HTML, TXT, or EPUB files to markdown notes with optional AI summary.
/// Inherits from DocumentNoteProcessorBase to provide consistent processing patterns across document types.
/// </summary>
public partial class MarkdownNoteProcessor(
    ILogger<MarkdownNoteProcessor> logger,
    IAISummarizer aiSummarizer,
    MarkdownNoteBuilder markdownNoteBuilder,
    AppConfig appConfig,
    IYamlHelper? yamlHelper = null,
    IMetadataHierarchyDetector? hierarchyDetector = null,
    IMetadataTemplateManager? templateManager = null,
    FieldValueResolverRegistry? resolverRegistry = null) : DocumentNoteProcessorBase(logger, aiSummarizer, markdownNoteBuilder, appConfig, yamlHelper, hierarchyDetector, templateManager, resolverRegistry)
{
    /// <summary>
    /// Controls whether AI summarization should be skipped for this processor instance.
    /// Used by the batch processor to disable AI processing for simple HTML-to-markdown conversion.
    /// </summary>
    public bool SkipAISummarization { get; set; } = false;

    /// <summary>
    /// Overrides the base AI summary generation to respect the SkipAISummarization flag.
    /// When SkipAISummarization is true, returns empty string instead of calling the AI service.
    /// </summary>
    /// <param name="text">The text to summarize.</param>
    /// <param name="variables">Optional variables for prompt template substitution.</param>
    /// <param name="promptFileName">Optional prompt template filename.</param>
    /// <returns>AI summary text or empty string if summarization is disabled.</returns>
    public override async Task<string> GenerateAiSummaryAsync(string? text, Dictionary<string, string>? variables = null, string? promptFileName = null)
    {
        if (SkipAISummarization)
        {
            Logger.LogInformation("MARKDOWN_SKIP: Skipping AI summarization for markdown processor (SkipAISummarization = true)");
            return string.Empty;
        }

        Logger.LogInformation("MARKDOWN_PROCESS: Calling base AI summarization for markdown processor");
        return await base.GenerateAiSummaryAsync(text, variables, promptFileName).ConfigureAwait(false);
    }

    /// <summary>
    /// Regular expression pattern for stripping HTML tags from content.
    /// Used by the test framework to validate HTML processing capabilities.
    /// </summary>
    [GeneratedRegex(@"<.*?>", RegexOptions.Compiled)]
    internal static partial Regex HtmlTagStripperRegex();

    /// <summary>
    /// Converts a TXT, HTML, or EPUB file to markdown, with optional AI summary.
    /// Supports multiple input formats and provides AI-enhanced content processing.
    /// </summary>
    /// <param name="inputPath">Path to the input file (TXT, HTML, HTM, or EPUB)</param>
    /// <param name="openAiApiKey">OpenAI API key for AI summarization (optional - can be configured in app settings)</param>
    /// <param name="promptFileName">Name of the prompt file to use for AI summarization (optional)</param>
    /// <param name="noSummary">If true, skips AI summarization and uses raw content</param>
    /// <returns>Generated markdown content as a string, or empty string if processing fails</returns>
    /// <exception cref="ArgumentException">Thrown when inputPath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the input file does not exist</exception>
    public async Task<string> ConvertToMarkdownAsync(string inputPath, string? openAiApiKey = null, string? promptFileName = null, bool noSummary = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        if (!File.Exists(inputPath))
        {
            Logger.LogError($"Input file not found: {inputPath}");
            return string.Empty;
        }

        try
        {
            string extension = Path.GetExtension(inputPath).ToLowerInvariant();
            string rawText = await ExtractTextFromFileAsync(inputPath, extension);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                Logger.LogWarning($"No content extracted from file: {inputPath}");

                // Still generate markdown with metadata for empty files
                var emptyMetadata = CreateMetadata(inputPath, extension);
                return Builder.BuildNote(emptyMetadata, string.Empty);
            }

            // Use AI summarization if available and not disabled
            string processedContent = rawText;
            if (!noSummary && !string.IsNullOrWhiteSpace(openAiApiKey))
            {
                try
                {
                    var summary = await Summarizer.SummarizeWithVariablesAsync(rawText, null, promptFileName);
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        processedContent = summary;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, $"AI summarization failed for {inputPath}, using original content");
                }
            }

            var metadata = CreateMetadata(inputPath, extension);
            return Builder.BuildNote(metadata, processedContent);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to process file: {inputPath}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Extracts text content from different file formats (TXT, HTML, EPUB).
    /// Handles format-specific parsing and converts content to plain text.
    /// </summary>
    /// <param name="inputPath">Path to the input file</param>
    /// <param name="extension">File extension (lowercase)</param>
    /// <returns>Extracted text content</returns>
    private async Task<string> ExtractTextFromFileAsync(string inputPath, string extension)
    {
        return extension switch
        {
            ".txt" => await File.ReadAllTextAsync(inputPath),
            ".html" or ".htm" => await ProcessHtmlFileAsync(inputPath),
            ".epub" => await ProcessEpubFileAsync(inputPath),
            _ => throw new NotSupportedException($"Unsupported file type: {extension}")
        };
    }

    /// <summary>
    /// Processes HTML files by converting HTML content to markdown.
    /// Uses ReverseMarkdown library for HTML-to-markdown conversion.
    /// </summary>
    /// <param name="inputPath">Path to the HTML file</param>
    /// <returns>Markdown content converted from HTML</returns>
    private async Task<string> ProcessHtmlFileAsync(string inputPath)
    {
        Logger.LogDebug($"ProcessHtmlFileAsync: Starting HTML processing for {inputPath}");

        string htmlContent = await File.ReadAllTextAsync(inputPath);
        Logger.LogDebug($"ProcessHtmlFileAsync: Read {htmlContent.Length} characters from HTML file");

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            Logger.LogWarning($"ProcessHtmlFileAsync: HTML file is empty or whitespace: {inputPath}");
            return string.Empty;
        }

        // Pre-process HTML to handle custom tags
        htmlContent = PreprocessCustomHtml(htmlContent);
        Logger.LogDebug($"ProcessHtmlFileAsync: After preprocessing: {htmlContent.Length} characters");

        // Use ReverseMarkdown to convert HTML to Markdown
        var config = new Config
        {
            // Remove unknown tags rather than leaving them
            UnknownTags = Config.UnknownTagsOption.Drop,
            // Convert <br> tags to line breaks
            GithubFlavored = true,
            // Remove empty paragraph tags
            RemoveComments = true,
            // Convert tables to markdown format
            TableWithoutHeaderRowHandling = Config.TableWithoutHeaderRowHandlingOption.EmptyRow
        };

        var converter = new Converter(config);
        string markdownContent = converter.Convert(htmlContent);

        // Post-process markdown to clean up formatting issues
        markdownContent = PostProcessMarkdown(markdownContent);

        Logger.LogDebug("ProcessHtmlFileAsync: Converted HTML to {MarkdownLength} characters of markdown", markdownContent.Length);

        if (markdownContent.Length > 0)
        {
            Logger.LogDebug("ProcessHtmlFileAsync: First 200 chars of markdown: {MarkdownPreview}",
                markdownContent.Length > 200 ? markdownContent[..200] + "..." : markdownContent);
        }
        else
        {
            Logger.LogWarning("ProcessHtmlFileAsync: ReverseMarkdown conversion produced empty result");
            Logger.LogDebug("ProcessHtmlFileAsync: First 500 chars of HTML input: {HtmlPreview}",
                htmlContent.Length > 500 ? htmlContent[..500] + "..." : htmlContent);
        }

        return markdownContent;
    }

    /// <summary>
    /// Pre-process HTML to handle custom tags and convert them to standard HTML tags
    /// that ReverseMarkdown can understand.
    /// </summary>
    private string PreprocessCustomHtml(string htmlContent)
    {
        // Handle custom co-content tags - convert to div
        htmlContent = htmlContent.Replace("<co-content>", "<div>").Replace("</co-content>", "</div>");

        // Handle custom heading tags with level attributes
        htmlContent = System.Text.RegularExpressions.Regex.Replace(
            htmlContent,
            @"<h(\d+)\s+level=""(\d+)"">",
            "<h$2>");

        // Handle custom paragraph tags with variant attributes
        htmlContent = System.Text.RegularExpressions.Regex.Replace(
            htmlContent,
            @"<p\s+variant=""[^""]*"">",
            "<p>");

        // Add basic HTML structure if missing
        if (!htmlContent.Contains("<html") && !htmlContent.Contains("<body"))
        {
            htmlContent = $"<html><body>{htmlContent}</body></html>";
        }

        return htmlContent;
    }

    /// <summary>
    /// Post-process markdown to clean up formatting issues from ReverseMarkdown conversion.
    /// </summary>
    /// <param name="markdownContent">The markdown content to clean up</param>
    /// <returns>Cleaned markdown content</returns>
    private string PostProcessMarkdown(string markdownContent)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            return markdownContent;
        }

        // Fix heading formatting - remove line breaks between # symbols and heading text
        // Pattern: "# \nHeading Text" -> "# Heading Text"
        markdownContent = System.Text.RegularExpressions.Regex.Replace(
            markdownContent,
            @"^(#{1,6})\s*\n\s*(.+)$",
            "$1 $2",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        // Clean up excessive whitespace between headings and content
        markdownContent = System.Text.RegularExpressions.Regex.Replace(
            markdownContent,
            @"(#{1,6}[^\n]+)\n{3,}",
            "$1\n\n",
            System.Text.RegularExpressions.RegexOptions.Multiline);

        // Clean up multiple consecutive blank lines
        markdownContent = System.Text.RegularExpressions.Regex.Replace(
            markdownContent,
            @"\n{4,}",
            "\n\n\n");

        // Trim any leading/trailing whitespace
        markdownContent = markdownContent.Trim();

        return markdownContent;
    }

    /// <summary>
    /// Processes EPUB files by extracting text content from all reading order items.
    /// Uses VersOne.Epub library for EPUB parsing and content extraction.
    /// </summary>
    /// <param name="inputPath">Path to the EPUB file</param>
    /// <returns>Combined text content from all EPUB chapters</returns>
    private async Task<string> ProcessEpubFileAsync(string inputPath)
    {
        try
        {
            // Use VersOne.Epub for EPUB parsing
            // Install-Package VersOne.Epub
            var epubText = new StringBuilder();
            var book = await VersOne.Epub.EpubReader.ReadBookAsync(inputPath);

            if (book?.ReadingOrder != null)
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

            return epubText.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to parse EPUB file: {inputPath}");
            throw;
        }
    }

    /// <summary>
    /// Creates metadata dictionary for the markdown note based on the input file.
    /// Includes information about the source file, generation timestamp, and file type.
    /// </summary>
    /// <param name="inputPath">Path to the input file</param>
    /// <param name="extension">File extension</param>
    /// <returns>Dictionary containing metadata for the markdown note</returns>
    private Dictionary<string, object> CreateMetadata(string inputPath, string extension)
    {
        string templateType = extension switch
        {
            ".txt" => "resource-reading",
            ".html" or ".htm" => "resource-reading", // HTML files use resource-reading template
            ".epub" => "resource-reading", // EPUB files also use resource-reading template  
            _ => "resource-reading" // Default fallback
        };

        var metadata = new Dictionary<string, object>
        {
            { "title", Path.GetFileNameWithoutExtension(inputPath) },
            { "template-type", templateType },
            { "filePath", inputPath }, // Provide file path for resolvers (primary key they expect)
            { "_internal_path", inputPath }, // Provide file path for hierarchy resolvers (backup key)
            { "generated", DateTime.UtcNow.ToString("u") }
        };

        // Add onedrive_relative_path following the same pattern as VideoNoteProcessor
        try
        {
            // Convert to resources-relative path for consistency with video processor
            string relative = MakeRelativeToOnedriveResourcesIfPossible(inputPath);
            metadata["onedrive_relative_path"] = string.IsNullOrWhiteSpace(relative) ? inputPath : relative;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to generate OneDrive relative path; using original path");
            metadata["onedrive_relative_path"] = inputPath;
        }

        return metadata;
    }

    /// <summary>
    /// Strips HTML tags from content using regular expressions.
    /// Simple HTML tag stripper for placeholder; should be replaced with a real converter for production.
    /// </summary>
    /// <param name="html">HTML content to process</param>
    /// <returns>Plain text with HTML tags removed</returns>
    private static string StripHtmlTags(string html)
    {
        return HtmlTagStripperRegex().Replace(html, string.Empty);
    }

    /// <summary>
    /// Extracts text content and metadata from the specified file.
    /// Implements the base class method to provide text extraction functionality.
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <returns>A tuple containing the extracted text and metadata dictionary</returns>
    public override async Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            Logger.LogError($"Input file not found: {filePath}");
            throw new FileNotFoundException($"Input file not found: {filePath}");
        }

        try
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            string text = await ExtractTextFromFileAsync(filePath, extension);
            var metadata = CreateMetadata(filePath, extension);

            return (text, metadata);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to extract text and metadata from file: {filePath}");
            throw;
        }
    }
}
