// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.MarkdownGeneration;

/// <summary>
/// Provides batch processing capabilities for converting multiple HTML/TXT/EPUB files to markdown notes.
/// </summary>
/// <remarks>
/// The <c>MarkdownNoteBatchProcessor</c> class coordinates the processing of multiple HTML, TXT, or EPUB files,
/// leveraging the <see cref="MarkdownNoteProcessor"/> for text extraction, content conversion,
/// and note generation. It supports dry-run mode, output directory management, and eventing
/// for real-time progress tracking. This processor is optimized for simple HTML-to-markdown conversion
/// and skips AI summarization when noSummary is true.
/// </remarks>
public class MarkdownNoteBatchProcessor : DocumentNoteBatchProcessor<MarkdownNoteProcessor>
{
    private readonly MarkdownNoteProcessor markdownProcessor;
    private readonly ILogger<DocumentNoteBatchProcessor<MarkdownNoteProcessor>> batchLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownNoteBatchProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="processor">The markdown note processor instance.</param>
    /// <param name="aiSummarizer">The AI summarizer service.</param>
    public MarkdownNoteBatchProcessor(
        ILogger<DocumentNoteBatchProcessor<MarkdownNoteProcessor>> logger,
        MarkdownNoteProcessor processor,
        IAISummarizer aiSummarizer)
        : base(logger, processor, aiSummarizer)
    {
        markdownProcessor = processor;
        batchLogger = logger;
    }

    /// <summary>
    /// Processes one or more HTML/TXT/EPUB files, generating markdown notes for each.
    /// </summary>
    /// <param name="input">Input file path or directory containing files to process.</param>
    /// <param name="output">Output directory where markdown notes will be saved.</param>
    /// <param name="fileExtensions">List of file extensions to recognize (e.g., .html, .txt, .epub).</param>
    /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
    /// <param name="dryRun">If true, simulates processing without writing output files.</param>
    /// <param name="noSummary">If true, disables OpenAI summary generation.</param>
    /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
    /// <param name="retryFailed">If true, retries only failed files from previous run.</param>
    /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>
    /// <param name="resourcesRoot">Optional override for OneDrive fullpath root directory.</param>
    /// <param name="appConfig">The application configuration object.</param>
    /// <param name="noShareLinks">If true, skips OneDrive share link creation.</param>
    /// <returns>A <see cref="BatchProcessResult"/> containing processing statistics and summary.</returns>
    public async Task<BatchProcessResult> ProcessFilesAsync(
        string input,
        string? output,
        List<string> fileExtensions,
        string? openAiApiKey,
        bool dryRun = false,
        bool noSummary = false,
        bool forceOverwrite = false,
        bool retryFailed = false,
        int? timeoutSeconds = null,
        string? resourcesRoot = null,
        AppConfig? appConfig = null,
        bool noShareLinks = false)
    {
        return await ProcessDocumentsAsync(
            input,
            output,
            fileExtensions,
            openAiApiKey,
            dryRun,
            noSummary,
            forceOverwrite,
            retryFailed,
            timeoutSeconds,
            resourcesRoot,
            appConfig,
            "Markdown Note",
            "failed_markdown.txt",
            noShareLinks).ConfigureAwait(false);
    }

    /// <summary>
    /// Overrides AI summary generation to provide optimized behavior for markdown processing.
    /// For markdown files, when noSummary is true, this sets the processor flag to skip AI processing entirely.
    /// </summary>
    /// <param name="filePath">Path to the file being processed.</param>
    /// <param name="text">Extracted text content.</param>
    /// <param name="metadata">File metadata dictionary.</param>
    /// <param name="queueItem">Current queue item for progress tracking.</param>
    /// <param name="fileIndex">Current file index (1-based).</param>
    /// <param name="totalFiles">Total number of files to process.</param>
    /// <param name="openAiApiKey">OpenAI API key.</param>
    /// <param name="noSummary">Whether to skip AI summary generation.</param>
    /// <param name="timeoutSeconds">API timeout in seconds.</param>
    /// <param name="resourcesRoot">Resources root directory.</param>
    /// <param name="noShareLinks">Whether to skip share link generation.</param>
    /// <param name="templateTypeName">Optional template type name to use for processing.</param>
    /// <param name="promptOverride">Optional prompt file override (name or full path).</param>
    /// <returns>Tuple containing summary text, token count, and processing time.</returns>
    protected override async Task<(string summaryText, int summaryTokens, TimeSpan summaryTime)> GenerateAISummaryAsync(
        string filePath,
        string text,
        Dictionary<string, object> metadata,
        QueueItem? queueItem,
        int fileIndex,
        int totalFiles,
        string? openAiApiKey,
        bool noSummary,
        int? timeoutSeconds,
        string? resourcesRoot,
        bool noShareLinks,
        string? templateTypeName = null,
        string? promptOverride = null)
    {
        var summaryStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Set the SkipAISummarization flag on the processor
        markdownProcessor.SkipAISummarization = noSummary;
        batchLogger.LogInformation("BATCH_SET: Set MarkdownNoteProcessor.SkipAISummarization = {NoSummary} for file: {FileName}",
            noSummary, Path.GetFileName(filePath));        // For markdown processing, when noSummary is true, skip all AI processing
        if (noSummary)
        {
            batchLogger.LogInformation("BATCH_SKIP: Skipping AI processing for markdown file: {FileName}", Path.GetFileName(filePath));
            summaryStopwatch.Stop();
            // Return the extracted text content instead of empty string
            return (text, 0, summaryStopwatch.Elapsed);
        }

        // When summary is enabled, delegate to base implementation
        return await base.GenerateAISummaryAsync(
            filePath, text, metadata, queueItem, fileIndex, totalFiles,
            openAiApiKey, noSummary, timeoutSeconds, resourcesRoot, noShareLinks,
            templateTypeName, promptOverride).ConfigureAwait(false);
    }
}
