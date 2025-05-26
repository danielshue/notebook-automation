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
        private readonly ILogger<DocumentNoteBatchProcessor<TProcessor>> _logger;
        private readonly TProcessor _processor;
        private readonly AISummarizer _aiSummarizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentNoteBatchProcessor{TProcessor}"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="processor">The document note processor instance.</param>
        /// <param name="aiSummarizer">The AI summarizer instance.</param>
        public DocumentNoteBatchProcessor(ILogger logger, TProcessor processor, AISummarizer aiSummarizer)
        {
            if (logger is ILogger<DocumentNoteBatchProcessor<TProcessor>> genericLogger)
            {
                _logger = genericLogger;
            }
            else
            {
                // Allow any ILogger for testing/mocking, but warn if not the expected type
                _logger = logger as ILogger<DocumentNoteBatchProcessor<TProcessor>> ?? throw new ArgumentException("Logger must be ILogger<DocumentNoteBatchProcessor<TProcessor>> or compatible mock");
                if (logger.GetType().Name.Contains("Mock") || logger.GetType().Name.Contains("Proxy"))
                {
                    // Allow for test mocks
                }
                else
                {
                    throw new ArgumentException("Logger must be ILogger<DocumentNoteBatchProcessor<TProcessor>>");
                }
            }
            _processor = processor;
            _aiSummarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer));
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
        /// <param name="retryFailed">If true, retries only failed files from previous run.</param>        /// <param name="timeoutSeconds">Optional API request timeout in seconds.</param>
        /// <param name="resourcesRoot">Optional override for OneDrive fullpath root directory.</param>
        /// <param name="appConfig">The application configuration object.</param>
        /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
        /// <param name="failedFilesListName">Name of the failed files list file (defaults to "failed_files.txt").</param>
        /// <param name="noShareLinks">If true, skips OneDrive share link creation.</param>
        /// <returns>A tuple containing the count of successfully processed files and the count of failures.</returns>
        /// <summary>
        /// Processes one or more document files, generating markdown notes for each, with extended options.
        /// Returns a BatchProcessResult with summary and statistics for CLI/UI output.
        /// </summary>
        /// <returns>A tuple containing the count of successfully processed files, the count of failures, and a BatchProcessResult with summary/statistics.</returns>
        public virtual async Task<BatchProcessResult> ProcessDocumentsAsync(
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
            string failedFilesListName = "failed_files.txt",
            bool noShareLinks = false)
        {
            var processed = 0;
            var failed = 0;
            var files = new List<string>(); string effectiveInput = input;
            string effectiveOutput = output ?? appConfig?.Paths?.NotebookVaultFullpathRoot ?? "Generated";

            if (string.IsNullOrWhiteSpace(effectiveInput))
            {
                _logger.LogError("Input path is required. Config: {Config}", appConfig?.Paths?.NotebookVaultFullpathRoot);
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
                _logger.LogError("Input must be a file or directory containing valid files: {Input}. Config: {Config}", effectiveInput, appConfig?.Paths?.NotebookVaultFullpathRoot);
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
                var fileStopwatch = Stopwatch.StartNew(); try
                {
                    _logger.LogInformation("Processing file: {FilePath}", filePath);
                    string outputDir = effectiveOutput ?? "Generated";
                    Directory.CreateDirectory(outputDir);

                    // Determine effective resources root first
                    string? effectiveResourcesRoot = resourcesRoot;
                    if (string.IsNullOrWhiteSpace(effectiveResourcesRoot) && appConfig != null)
                    {
                        effectiveResourcesRoot = appConfig.Paths?.OnedriveFullpathRoot;
                    }

                    // Generate output path based on processor type and directory structure
                    string outputPath = GenerateOutputPath(filePath, outputDir, effectiveResourcesRoot);

                    // If not forceOverwrite and file exists, skip
                    if (!forceOverwrite && File.Exists(outputPath))
                    {
                        _logger.LogWarning("Output file exists and --force not set, skipping: {OutputPath}", outputPath);
                        continue;
                    }

                    // Extraction and note generation
                    var (text, metadata) = await _processor.ExtractTextAndMetadataAsync(filePath);
                    if (!string.IsNullOrWhiteSpace(effectiveResourcesRoot))
                    {
                        metadata["resources_root"] = effectiveResourcesRoot;
                    }                    // If this is a VideoNoteProcessor, use the specialized method
                    string markdown = string.Empty;
                    string summaryText = string.Empty;
                    int summaryTokens = 0;
                    var summaryStopwatch = Stopwatch.StartNew();
                    bool usedVideoProcessor = false;

                    if (typeof(TProcessor).Name.Contains("Video"))
                    {
                        // Use reflection to call the specialized GenerateVideoNoteAsync method
                        var generateMethod = _processor.GetType().GetMethod("GenerateVideoNoteAsync");
                        if (generateMethod != null)
                        {
                            _logger.LogDebug("Using specialized GenerateVideoNoteAsync method for video processing");
                            // Pass the noShareLinks parameter to the GenerateVideoNoteAsync method
                            var task = (Task<string>)generateMethod.Invoke(_processor, new object?[]
                            {
                                filePath,
                                openAiApiKey,
                                null, // promptFileName 
                                noSummary,
                                timeoutSeconds,
                                effectiveResourcesRoot,
                                noShareLinks
                            })!;

                            markdown = await task;
                            usedVideoProcessor = true;
                            // Estimate tokens
                            var estimateTokenMethod = _aiSummarizer.GetType().GetMethod("EstimateTokenCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (estimateTokenMethod != null && !noSummary)
                            {
                                // Extract summary part from the markdown for token estimation
                                var testSummary = markdown.Length > 300 ? markdown.Substring(0, 300) : markdown;
                                var tokenResult = estimateTokenMethod.Invoke(_aiSummarizer, new object[] { testSummary });
                                if (tokenResult != null)
                                {
                                    summaryTokens = (int)tokenResult;
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("VideoNoteProcessor found but GenerateVideoNoteAsync method not available. Using base method.");
                            usedVideoProcessor = false;
                        }
                    }

                    // Use the base implementation for non-video processors or if video-specific method wasn't available
                    if (!usedVideoProcessor)
                    {
                        if (noSummary)
                        {
                            summaryText = "[Summary generation disabled by --no-summary flag.]";
                        }
                        else
                        {
                            summaryText = await _processor.GenerateAiSummaryAsync(text);
                            var estimateTokenMethod = _aiSummarizer.GetType().GetMethod("EstimateTokenCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (estimateTokenMethod != null)
                            {
                                var tokenResult = estimateTokenMethod.Invoke(_aiSummarizer, new object[] { summaryText });
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

                        markdown = _processor.GenerateMarkdownNote(summaryText, metadata, noteType);
                    }

                    // Ensure markdown is initialized
                    if (markdown == null)
                    {
                        _logger.LogError("Markdown generation failed for file: {FilePath}", filePath);
                        markdown = "Error generating markdown content";
                    }

                    summaryStopwatch.Stop();
                    totalSummaryTime += summaryStopwatch.Elapsed;
                    totalTokens += summaryTokens;
                    _logger.LogInformation("Summary for file: {FilePath} took {ElapsedMs} ms, tokens: {Tokens}", filePath, summaryStopwatch.ElapsedMilliseconds, summaryTokens);

                    // For video files, handle content preservation after "## Notes"
                    if (typeof(TProcessor).Name.Contains("Video") && File.Exists(outputPath) && !forceOverwrite)
                    {
                        markdown = PreserveUserContentAfterNotes(outputPath, markdown);
                    }

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
        /// <summary>
        /// Generates the output path for a processed file, handling video-specific naming and directory structure.
        /// </summary>
        /// <param name="inputFilePath">The input file path.</param>
        /// <param name="outputDir">The base output directory.</param>
        /// <param name="resourcesRoot">The resources root directory for calculating relative paths.</param>
        /// <returns>The output file path.</returns>
        protected virtual string GenerateOutputPath(string inputFilePath, string outputDir, string? resourcesRoot)
        {
            // Check if this is a video processor by checking the processor type
            bool isVideoProcessor = typeof(TProcessor).Name.Contains("Video");

            if (isVideoProcessor)
            {
                // For video files, use -video.md suffix and preserve directory structure
                string fileName = Path.GetFileNameWithoutExtension(inputFilePath) + "-video.md";

                // If we have a resources root, preserve the directory structure
                if (!string.IsNullOrWhiteSpace(resourcesRoot) && Path.IsPathRooted(resourcesRoot))
                {
                    try
                    {
                        // Calculate relative path from resources root
                        var inputFileInfo = new FileInfo(inputFilePath);
                        var resourcesRootInfo = new DirectoryInfo(resourcesRoot);

                        // Get the relative path from resources root to the input file's directory
                        string relativePath = Path.GetRelativePath(resourcesRootInfo.FullName, inputFileInfo.DirectoryName ?? "");

                        // Log the original relative path for debugging
                        _logger.LogDebug("Original relative path from resources root: {RelativePath}", relativePath);

                        // If the relative path starts with "." it means the file is directly in the resources root
                        // In that case, use empty path to put files directly in output directory
                        if (relativePath == "." || relativePath == ".\\")
                        {
                            relativePath = "";
                        }

                        // Create the output directory structure
                        string targetDir = string.IsNullOrEmpty(relativePath) ? outputDir : Path.Combine(outputDir, relativePath);
                        Directory.CreateDirectory(targetDir);

                        var outputPath = Path.Combine(targetDir, fileName);
                        _logger.LogDebug("Generated output path: {OutputPath}", outputPath);

                        return outputPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to calculate relative path from resources root {ResourcesRoot} to {InputFile}, using flat structure",
                            resourcesRoot, inputFilePath);
                        // Fall back to flat structure
                        return Path.Combine(outputDir, fileName);
                    }
                }
                else
                {
                    // No resources root specified, use flat structure with -video.md suffix
                    return Path.Combine(outputDir, fileName);
                }
            }
            else
            {
                // For non-video files, use standard .md suffix and flat structure
                string fileName = Path.GetFileNameWithoutExtension(inputFilePath) + ".md";
                return Path.Combine(outputDir, fileName);
            }
        }

        /// <summary>
        /// Preserves user content that appears after the "## Notes" section in existing video markdown files.
        /// </summary>
        /// <param name="existingFilePath">Path to the existing markdown file.</param>
        /// <param name="newMarkdown">The newly generated markdown content.</param>
        /// <returns>The new markdown with preserved user content appended.</returns>
        protected virtual string PreserveUserContentAfterNotes(string existingFilePath, string newMarkdown)
        {
            try
            {
                if (!File.Exists(existingFilePath))
                {
                    return newMarkdown;
                }

                string existingContent = File.ReadAllText(existingFilePath);

                // Find the "## Notes" section in the existing file
                var notesRegex = new System.Text.RegularExpressions.Regex(
                    @"^##\s+Notes\s*$",
                    System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                var match = notesRegex.Match(existingContent);
                if (match.Success)
                {
                    // Extract everything after the "## Notes" line
                    int notesIndex = match.Index + match.Length;
                    string userContent = existingContent.Substring(notesIndex).TrimStart('\r', '\n');

                    if (!string.IsNullOrWhiteSpace(userContent))
                    {
                        // Find the "## Notes" section in the new markdown and append the user content
                        var newNotesMatch = notesRegex.Match(newMarkdown);
                        if (newNotesMatch.Success)
                        {
                            int newNotesIndex = newNotesMatch.Index + newNotesMatch.Length;
                            string beforeNotes = newMarkdown.Substring(0, newNotesIndex);

                            // Combine the new content up to ## Notes with the preserved user content
                            return beforeNotes + "\n\n" + userContent;
                        }
                        else
                        {
                            // If somehow the new markdown doesn't have "## Notes", just append it
                            return newMarkdown + "\n\n## Notes\n\n" + userContent;
                        }
                    }
                }

                return newMarkdown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preserve user content from existing file: {FilePath}", existingFilePath);
                return newMarkdown;
            }
        }
    }
}
