using NotebookAutomation.Core.Tools.Shared;

namespace NotebookAutomation.Core.Tools.PdfProcessing
{
    /// <summary>
    /// Provides batch processing capabilities for converting multiple PDF files to markdown notes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The PdfNoteBatchProcessor class coordinates the processing of multiple PDF files,
    /// either from a specified directory or a single file path. It leverages the 
    /// <see cref="PdfNoteProcessor"/> to handle the details of text extraction
    /// and note generation for each PDF.
    /// </para>
    /// <para>
    /// This batch processor is responsible for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Identifying PDF files based on their extensions</description></item>
    /// <item><description>Coordinating the processing of each PDF file</description></item>
    /// <item><description>Managing output directory creation and file writing</description></item>
    /// <item><description>Tracking success and failure counts</description></item>
    /// <item><description>Supporting dry run mode for testing without file writes</description></item>
    /// </list>
    /// <para>
    /// The class is designed to be used by both CLI and API interfaces, providing a central
    /// point for PDF batch processing operations with appropriate logging and error handling.
    /// This implementation delegates all batch processing logic to the generic 
    /// <see cref="DocumentNoteBatchProcessor{TProcessor}"/> for maintainability and code reuse.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PdfNoteBatchProcessor"/> class with a batch processor.
    /// </remarks>
    /// <param name="batchProcessor">The batch processor to use for PDF note processing.</param>
    public class PdfNoteBatchProcessor(DocumentNoteBatchProcessor<PdfNoteProcessor> batchProcessor)
    {
        /// <summary>
        /// The generic batch processor that handles the actual batch processing logic.
        /// </summary>
        private readonly DocumentNoteBatchProcessor<PdfNoteProcessor> _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));

        /// <summary>
        /// Event triggered when processing progress changes.
        /// </summary>
        public event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged
        {
            add => _batchProcessor.ProcessingProgressChanged += value;
            remove => _batchProcessor.ProcessingProgressChanged -= value;
        }

        /// <summary>
        /// Processes one or more PDF files, generating markdown notes for each.
        /// </summary>
        /// <param name="input">Input file path or directory containing PDF files.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="pdfExtensions">List of file extensions to recognize as PDF files (defaults to [".pdf"]).</param>
        /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
        /// <param name="dryRun">If true, simulates processing without writing output files.</param>
        /// <returns>
        /// A tuple containing the count of successfully processed files and the count of failures.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method delegates to the generic <see cref="DocumentNoteBatchProcessor{TProcessor}"/>
        /// for all batch processing operations while maintaining backward compatibility with 
        /// existing PDF-specific API.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Process all PDF files in a directory
        /// var processor = new PdfNoteBatchProcessor(logger);
        /// var result = await processor.ProcessPdfsAsync(
        ///     "path/to/pdfs",
        ///     "path/to/notes",
        ///     new List&lt;string&gt; { ".pdf" },
        ///     "sk-yourapikeyhere");
        /// 
        /// Console.WriteLine($"Processed: {result.processed}, Failed: {result.failed}");
        /// </code>
        /// </example>
        /// <summary>
        /// Processes one or more PDF files, generating markdown notes for each.
        /// </summary>
        /// <param name="input">Input file path or directory containing PDF files.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="pdfExtensions">List of file extensions to recognize as PDF files (defaults to [".pdf"]).</param>
        /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
        /// <param name="dryRun">If true, simulates processing without writing output files.</param>
        /// <returns>A <see cref="BatchProcessResult"/> containing processing statistics and summary.</returns>
        public async Task<BatchProcessResult> ProcessPdfsAsync(
            string input,
            string? output,
            List<string>? pdfExtensions = null,
            string? openAiApiKey = null,
            bool dryRun = false)
        {
            var extensions = pdfExtensions ?? [".pdf"];
            return await _batchProcessor.ProcessDocumentsAsync(
                input,
                output,
                extensions,
                openAiApiKey,
                dryRun,
                noSummary: false,
                forceOverwrite: false,
                retryFailed: false,
                timeoutSeconds: null,
                resourcesRoot: null,
                appConfig: null,
                "PDF Note",
                "failed_pdfs.txt");
        }

        /// <summary>
        /// Processes one or more PDF files, generating markdown notes for each, with extended options.
        /// </summary>
        /// <param name="input">Input file path or directory containing PDF files.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="pdfExtensions">List of file extensions to recognize as PDF files (defaults to [".pdf"]).</param>
        /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
        /// <param name="dryRun">If true, simulates processing without writing output files.</param>
        /// <param name="noSummary">If true, disables OpenAI summary generation.</param>
        /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
        /// <param name="retryFailed">If true, retries only failed files from previous run.</param>
        /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>        /// <param name="resourcesRoot">Optional override for OneDrive fullpath root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
        /// <returns>A tuple containing the count of successfully processed files and the count of failures.</returns>
        /// <summary>
        /// Processes one or more PDF files, generating markdown notes for each, with extended options.
        /// </summary>
        /// <param name="input">Input file path or directory containing PDF files.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="pdfExtensions">List of file extensions to recognize as PDF files (defaults to [".pdf"]).</param>
        /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
        /// <param name="dryRun">If true, simulates processing without writing output files.</param>
        /// <param name="noSummary">If true, disables OpenAI summary generation.</param>
        /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
        /// <param name="retryFailed">If true, retries only failed files from previous run.</param>
        /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>        /// <param name="resourcesRoot">Optional override for OneDrive fullpath root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
        /// <returns>A <see cref="BatchProcessResult"/> containing processing statistics and summary.</returns>
        public async Task<BatchProcessResult> ProcessPdfsAsync(
            string input,
            string? output,
            List<string>? pdfExtensions,
            string? openAiApiKey,
            bool dryRun = false,
            bool noSummary = false,
            bool forceOverwrite = false,
            bool retryFailed = false,
            int? timeoutSeconds = null,
            string? resourcesRoot = null,
            Configuration.AppConfig? appConfig = null)
        {
            var extensions = pdfExtensions ?? [".pdf"];
            return await _batchProcessor.ProcessDocumentsAsync(
                input,
                output,
                extensions,
                openAiApiKey,
                dryRun,
                noSummary,
                forceOverwrite,
                retryFailed,
                timeoutSeconds,
                resourcesRoot,
                appConfig,
                "PDF Note",
                "failed_pdfs.txt");
        }
    }
}
