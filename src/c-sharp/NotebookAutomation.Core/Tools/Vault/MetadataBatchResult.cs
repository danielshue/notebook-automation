// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Represents the comprehensive result of a batch metadata processing operation with detailed statistics and status information.
/// </summary>
/// <remarks>
/// <para>
/// This class encapsulates all relevant information about a completed batch processing operation,
/// providing detailed statistics for monitoring, reporting, and decision-making purposes.
/// </para>
/// <para>
/// The result object tracks several categories of files:
/// </para>
/// <list type="bullet">
/// <item><description>Total Files: All markdown files discovered in the vault</description></item>
/// <item><description>Processed Files: Files that were successfully updated with metadata</description></item>
/// <item><description>Skipped Files: Files that didn't require changes (metadata already present)</description></item>
/// <item><description>Failed Files: Files that encountered errors during processing</description></item>
/// </list>
/// <para>
/// Success Determination:
/// The Success property indicates whether the overall batch operation completed without critical errors.
/// Individual file failures do not necessarily mark the entire operation as failed unless they prevent
/// the operation from continuing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Analyze batch processing results
/// var result = await batchProcessor.EnsureMetadataAsync(vaultPath);
///
/// if (result.Success)
/// {
///     var successRate = (double)result.ProcessedFiles / result.TotalFiles * 100;
///     Console.WriteLine($"Batch completed: {successRate:F1}% success rate");
///     Console.WriteLine($"Files processed: {result.ProcessedFiles}");
///     Console.WriteLine($"Files skipped: {result.SkippedFiles}");
///
///     if (result.FailedFiles > 0)
///     {
///         Console.WriteLine($"Warning: {result.FailedFiles} files failed");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Batch operation failed: {result.ErrorMessage}");
/// }
///
/// // Calculate processing efficiency
/// var totalAttempted = result.ProcessedFiles + result.FailedFiles;
/// if (totalAttempted > 0)
/// {
///     var efficiency = (double)result.ProcessedFiles / totalAttempted * 100;
///     Console.WriteLine($"Processing efficiency: {efficiency:F1}%");
/// }
/// </code>
/// </example>
public class MetadataBatchResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the overall batch processing operation completed successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property indicates the success of the entire batch operation, not individual file processing.
    /// A batch operation can be considered successful even if some individual files fail, as long as
    /// the overall process completes and the majority of files are processed successfully.
    /// </para>
    /// <para>
    /// Scenarios where Success would be false:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Invalid vault directory path or directory not found</description></item>
    /// <item><description>Insufficient permissions to access the vault directory</description></item>
    /// <item><description>Critical system errors that prevent the batch operation from proceeding</description></item>
    /// <item><description>Unhandled exceptions during the batch processing workflow</description></item>
    /// </list>
    /// </remarks>
    /// <value>
    /// true if the batch operation completed successfully; false if critical errors prevented completion.
    /// Default value is true, assuming success unless explicitly set to false.
    /// </value>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message describing why the batch operation failed, if applicable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property contains detailed error information when the Success property is false.
    /// The error message provides specific details about what caused the batch operation to fail,
    /// enabling proper error handling and user notification.
    /// </para>
    /// <para>
    /// Error Message Content:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Directory access issues: "Vault directory does not exist: C:\path"</description></item>
    /// <item><description>Permission problems: "Access denied to vault directory"</description></item>
    /// <item><description>System errors: Exception messages from underlying operations</description></item>
    /// </list>
    /// <para>
    /// When Success is true, this property is typically null or empty, indicating no critical errors occurred.
    /// </para>
    /// </remarks>
    /// <value>
    /// A descriptive error message when Success is false; null or empty when the operation succeeds.
    /// </value>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the total number of markdown files discovered and considered for processing in the vault directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property represents the complete count of all markdown files found during the directory
    /// scanning phase, regardless of their processing outcome. It serves as the denominator for
    /// calculating processing completion percentages and success rates.
    /// </para>
    /// <para>
    /// File Discovery Rules:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Includes all .md files in the vault directory and subdirectories</description></item>
    /// <item><description>Excludes files in hidden directories (starting with '.')</description></item>
    /// <item><description>Excludes hidden files (filenames starting with '.')</description></item>
    /// <item><description>Counts files even if they later fail during processing</description></item>
    /// </list>
    /// <para>
    /// Mathematical Relationship:
    /// TotalFiles = ProcessedFiles + SkippedFiles + FailedFiles
    /// </para>
    /// </remarks>
    /// <value>
    /// The count of all eligible markdown files found in the vault directory.
    /// Default value is 0, indicating no files were discovered.
    /// </value>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files that were successfully processed and had their metadata updated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property counts files where the metadata processing operation completed successfully
    /// and resulted in actual changes to the file content. These are files where new metadata
    /// was added or existing metadata was updated according to the processing parameters.
    /// </para>
    /// <para>
    /// Processing Criteria:
    /// </para>
    /// <list type="bullet">
    /// <item><description>File was accessible and readable</description></item>
    /// <item><description>Metadata processing completed without errors</description></item>
    /// <item><description>Changes were detected and applied (or would be applied in dry run mode)</description></item>
    /// <item><description>File was successfully written back to disk (unless in dry run mode)</description></item>
    /// </list>
    /// <para>
    /// In dry run mode, this counts files that would have been updated if the operation
    /// were performed with actual file modifications enabled.
    /// </para>
    /// </remarks>
    /// <value>
    /// The count of files that were successfully updated with metadata changes.
    /// Default value is 0, indicating no files were processed.
    /// </value>
    public int ProcessedFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files that were skipped because no metadata changes were needed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property counts files that were examined during the batch operation but did not
    /// require any metadata updates. These files already contained complete and valid metadata
    /// according to the processing parameters, so no modifications were necessary.
    /// </para>
    /// <para>
    /// Skipping Criteria:
    /// </para>
    /// <list type="bullet">
    /// <item><description>File was accessible and successfully parsed</description></item>
    /// <item><description>All required metadata fields were already present and valid</description></item>
    /// <item><description>No force overwrite was requested, preserving existing content</description></item>
    /// <item><description>File structure and metadata met all validation requirements</description></item>
    /// </list>
    /// <para>
    /// Skipped files represent successful processing outcomes where no work was needed,
    /// indicating that the vault's metadata is already well-maintained for those files.
    /// </para>
    /// </remarks>
    /// <value>
    /// The count of files that were examined but required no metadata updates.
    /// Default value is 0, indicating no files were skipped.
    /// </value>
    public int SkippedFiles { get; set; }

    /// <summary>
    /// Gets or sets the number of files that encountered errors during processing and could not be completed successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property counts files where the metadata processing operation encountered errors
    /// that prevented successful completion. Failed files are tracked separately to enable
    /// retry scenarios and error analysis.
    /// </para>
    /// <para>
    /// Common Failure Scenarios:
    /// </para>
    /// <list type="bullet">
    /// <item><description>File access permissions insufficient for reading or writing</description></item>
    /// <item><description>File locked by another process during processing</description></item>
    /// <item><description>Corrupted or malformed markdown content that cannot be parsed</description></item>
    /// <item><description>Disk space insufficient for writing updated content</description></item>
    /// <item><description>Network issues when processing files on network drives</description></item>
    /// <item><description>Unexpected exceptions during metadata analysis or generation</description></item>
    /// </list>
    /// <para>
    /// Failed File Recovery:
    /// Failed files are automatically saved to a "failed_metadata_files.txt" file in the vault
    /// directory, enabling retry operations and manual investigation of problematic files.
    /// </para>
    /// <para>
    /// Batch Operation Impact:
    /// Individual file failures do not stop the overall batch operation. The process continues
    /// with remaining files to maximize the number of successfully processed files.
    /// </para>
    /// </remarks>
    /// <value>
    /// The count of files that could not be processed due to errors.
    /// Default value is 0, indicating no processing failures occurred.
    /// </value>
    public int FailedFiles { get; set; }
}