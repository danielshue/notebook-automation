using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using System.Diagnostics;
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tools.Shared
{
    /// <summary>
    /// Generic batch processor for document note processors (PDF, video, etc.).
    /// Handles file discovery, error handling, and output for any DocumentNoteProcessorBase subclass.
    /// </summary>
    public class DocumentNoteBatchProcessor<TProcessor> where TProcessor : DocumentNoteProcessorBase
    {
        private readonly ILogger _logger;
        private readonly TProcessor _processor;

        public DocumentNoteBatchProcessor(ILogger logger, TProcessor processor)
        {
            _logger = logger;
            _processor = processor;
        }

        /// <summary>
        /// Processes one or more document files, generating markdown notes for each, with extended options.
        /// </summary>
        /// <param name="input">Input file path or directory containing files.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="fileExtensions">List of file extensions to recognize as valid files.</param>
        /// <param name="openAiApiKey">Optional OpenAI API key for generating summaries.</param>
        /// <param name="dryRun">If true, simulates processing without writing output files.</param>
        /// <param name="noSummary">If true, disables OpenAI summary generation.</param>
        /// <param name="forceOverwrite">If true, overwrites existing notes.</param>
        /// <param name="retryFailed">If true, retries only failed files from previous run.</param>
        /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>
        /// <param name="resourcesRoot">Optional override for resources root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
        /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
        /// <param name="failedFilesListName">Name of the failed files list file (defaults to "failed_files.txt").</param>
        /// <returns>A tuple containing the count of successfully processed files and the count of failures.</returns>
        /// <summary>
        /// Processes one or more document files, generating markdown notes for each, with extended options.
        /// Returns a BatchProcessResult with summary and statistics for CLI/UI output.
        /// </summary>
        /// <returns>A tuple containing the count of successfully processed files, the count of failures, and a BatchProcessResult with summary/statistics.</returns>
        public async Task<BatchProcessResult> ProcessDocumentsAsync(
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
            string noteType = "Document Note",
            string failedFilesListName = "failed_files.txt")
        {
            var processed = 0;
            var failed = 0;
            var files = new List<string>();
            string effectiveInput = input;
            string effectiveOutput = output ?? appConfig?.Paths?.NotebookVaultRoot ?? "Generated";

            if (string.IsNullOrWhiteSpace(effectiveInput))
            {
                _logger.LogError("Input path is required. Config: {Config}", appConfig?.Paths?.NotebookVaultRoot);
                return new BatchProcessResult
                {
                    Processed = 0,
                    Failed = 1,
                    Summary = "Error: Input path is required.",
                    TotalBatchTime = TimeSpan.Zero,
                    TotalSummaryTime = TimeSpan.Zero,
                    TotalTokens = 0,
                    AverageFileTimeMs = 0,
                    AverageSummaryTimeMs = 0,
                    AverageTokens = 0
                };
            }

            if (Directory.Exists(effectiveInput))
            {
                foreach (var ext in fileExtensions)
                {
                    files.AddRange(Directory.GetFiles(effectiveInput, "*" + ext, SearchOption.AllDirectories));
                }
                _logger.LogInformation("Found {Count} files in directory: {Dir}", files.Count, effectiveInput);
            }
            else if (File.Exists(effectiveInput) && fileExtensions.Exists(ext => effectiveInput.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                files.Add(effectiveInput);
            }
            else
            {
                _logger.LogError("Input must be a file or directory containing valid files: {Input}. Config: {Config}", effectiveInput, appConfig?.Paths?.NotebookVaultRoot);
                return new BatchProcessResult
                {
                    Processed = 0,
                    Failed = 1,
                    Summary = "Error: Input must be a file or directory containing valid files.",
                    TotalBatchTime = TimeSpan.Zero,
                    TotalSummaryTime = TimeSpan.Zero,
                    TotalTokens = 0,
                    AverageFileTimeMs = 0,
                    AverageSummaryTimeMs = 0,
                    AverageTokens = 0
                };
            }

            // If retryFailed is set, filter files to only those that failed in previous run
            if (retryFailed)
            {
                var failedListPath = Path.Combine(effectiveOutput ?? "Generated", failedFilesListName);
                if (File.Exists(failedListPath))
                {
                    var failedFiles = new HashSet<string>(File.ReadAllLines(failedListPath));
                    files = files.FindAll(f => failedFiles.Contains(f));
                    _logger.LogInformation("Retrying {Count} previously failed files.", files.Count);
                }
                else
                {
                    _logger.LogWarning("No {FileName} found for retry; processing all files.", failedFilesListName);
                }
            }

            var failedFilesForRetry = new List<string>();

            // Timing and token stats
            var batchStopwatch = Stopwatch.StartNew();
            var totalSummaryTime = TimeSpan.Zero;
            var totalTokens = 0;

            foreach (var filePath in files)
            {
                var fileStopwatch = Stopwatch.StartNew();
                try
                {
                    _logger.LogInformation("Processing file: {FilePath}", filePath);
                    string outputDir = effectiveOutput ?? "Generated";
                    Directory.CreateDirectory(outputDir);
                    string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(filePath) + ".md");

                    // If not forceOverwrite and file exists, skip
                    if (!forceOverwrite && File.Exists(outputPath))
                    {
                        _logger.LogWarning("Output file exists and --force not set, skipping: {OutputPath}", outputPath);
                        continue;
                    }

                    // Determine effective resources root
                    string? effectiveResourcesRoot = resourcesRoot;
                    if (string.IsNullOrWhiteSpace(effectiveResourcesRoot) && appConfig != null)
                    {
                        effectiveResourcesRoot = appConfig.Paths?.ResourcesRoot;
                    }

                    // Extraction and note generation
                    var (text, metadata) = await _processor.ExtractTextAndMetadataAsync(filePath);
                    if (!string.IsNullOrWhiteSpace(effectiveResourcesRoot))
                    {
                        metadata["resources_root"] = effectiveResourcesRoot;
                    }

                    // Summary timing and token counting
                    string summaryText;
                    int summaryTokens = 0;
                    var summaryStopwatch = Stopwatch.StartNew();
                    if (noSummary)
                    {
                        summaryText = "[Summary generation disabled by --no-summary flag.]";
                    }
                    else
                    {
                        summaryText = await _processor.GenerateAiSummaryAsync(text, openAiApiKey);
                        // Token counting: use injected or new AISummarizer
                        var summarizer = _processor.GetType().GetProperty("_aiSummarizer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_processor) as AISummarizer;
                        if (summarizer == null)
                        {
                            summarizer = new AISummarizer(_logger);
                        }
                        var estimateTokenMethod = summarizer.GetType().GetMethod("EstimateTokenCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (estimateTokenMethod != null)
                        {
                            var tokenResult = estimateTokenMethod.Invoke(summarizer, new object[] { summaryText });
                            if (tokenResult != null)
                            {
                                summaryTokens = (int)tokenResult;
                            }
                            else
                            {
                                summaryTokens = 0;
                                _logger.LogWarning("EstimateTokenCount returned null for summary.");
                            }
                        }
                    }
                    summaryStopwatch.Stop();
                    totalSummaryTime += summaryStopwatch.Elapsed;
                    totalTokens += summaryTokens;
                    _logger.LogInformation("Summary for file: {FilePath} took {ElapsedMs} ms, tokens: {Tokens}", filePath, summaryStopwatch.ElapsedMilliseconds, summaryTokens);

                    string markdown = _processor.GenerateMarkdownNote(summaryText, metadata, noteType);

                    if (!dryRun)
                    {
                        await File.WriteAllTextAsync(outputPath, markdown);
                        _logger.LogInformation("Markdown note saved to: {OutputPath}", outputPath);
                    }
                    else
                    {
                        _logger.LogInformation("[DRY RUN] Markdown note would be generated for: {FilePath}", filePath);
                    }
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process file: {FilePath}", filePath);
                    failedFilesForRetry.Add(filePath);
                    failed++;
                }
                fileStopwatch.Stop();
                _logger.LogInformation("Processing file: {FilePath} took {ElapsedMs} ms", filePath, fileStopwatch.ElapsedMilliseconds);
            }
            batchStopwatch.Stop();

            // Write failed files for retry if any
            if (failedFilesForRetry.Count > 0 && !dryRun)
            {
                var failedListPath = Path.Combine(effectiveOutput ?? "Generated", failedFilesListName);
                File.WriteAllLines(failedListPath, failedFilesForRetry);
                _logger.LogInformation("Wrote failed file list to: {Path}", failedListPath);
            }

            _logger.LogInformation("Document processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            _logger.LogInformation("Total batch processing time: {ElapsedMs} ms", batchStopwatch.ElapsedMilliseconds);
            _logger.LogInformation("Total summary time: {ElapsedMs} ms", totalSummaryTime.TotalMilliseconds);
            _logger.LogInformation("Total tokens for all summaries: {TotalTokens}", totalTokens);


            double avgFileTime = processed > 0 ? batchStopwatch.Elapsed.TotalMilliseconds / processed : 0;
            double avgSummaryTime = processed > 0 ? totalSummaryTime.TotalMilliseconds / processed : 0;
            double avgTokens = processed > 0 ? (double)totalTokens / processed : 0;

            // Helper for formatting time
            string FormatTime(TimeSpan ts)
            {
                if (ts.TotalHours >= 1)
                    return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
                if (ts.TotalMinutes >= 1)
                    return $"{ts.Minutes}m {ts.Seconds}s";
                if (ts.TotalSeconds >= 1)
                    return $"{ts.Seconds}s {ts.Milliseconds}ms";
                return $"{ts.Milliseconds}ms";
            }

            string FormatMs(double ms)
            {
                if (ms >= 60000)
                {
                    var ts = TimeSpan.FromMilliseconds(ms);
                    return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
                }
                if (ms >= 1000)
                {
                    return $"{(ms / 1000):F2}s";
                }
                return $"{ms:F0}ms";
            }

            // Prepare summary string for CLI or UI output
            string summary = "\n================ Batch Processing Summary ================\n"
                + $"Files processed: {processed}\n"
                + $"Files failed: {failed}\n"
                + $"Total batch time: {FormatTime(batchStopwatch.Elapsed)}\n"
                + $"Average time per file: {FormatMs(avgFileTime)}\n"
                + $"Total summary time: {FormatTime(totalSummaryTime)}\n"
                + $"Average summary time per file: {FormatMs(avgSummaryTime)}\n"
                + $"Total tokens for all summaries: {totalTokens}\n"
                + $"Average tokens per summary: {avgTokens:F2}\n"
                + "==========================================================\n";

            // Return a tuple with the summary string as a third value (for CLI/UI)
            return new BatchProcessResult
            {
                Processed = processed,
                Failed = failed,
                Summary = summary,
                TotalBatchTime = batchStopwatch.Elapsed,
                TotalSummaryTime = totalSummaryTime,
                TotalTokens = totalTokens,
                AverageFileTimeMs = avgFileTime,
                AverageSummaryTimeMs = avgSummaryTime,
                AverageTokens = avgTokens
            };
        }
    }
}
