// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Represents the result of a batch vault index generation operation.
/// </summary>
/// <remarks>
/// This class encapsulates all the key metrics and status information from a batch
/// index generation operation. It provides detailed statistics about processing
/// outcomes and any errors encountered during the operation.
///
/// The result can be used for:
/// - Progress reporting and monitoring
/// - Error analysis and debugging
/// - Performance metrics collection
/// - Automated retry logic based on failure patterns
/// - User interface status updates
/// </remarks>
/// <example>
/// <code>
/// var result = await processor.GenerateIndexesAsync("/path/to/vault");
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Processed {result.ProcessedFolders} out of {result.TotalFolders} folders");
///     Console.WriteLine($"Skipped: {result.SkippedFolders}, Failed: {result.FailedFolders}");
/// }
/// else
/// {
///     Console.WriteLine($"Operation failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
public class VaultIndexBatchResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the batch operation completed successfully.
    /// </summary>
    /// <remarks>
    /// This flag indicates the overall success of the batch operation. It will be false
    /// if there was a critical error that prevented the operation from completing,
    /// but it remains true even if individual folder processing failed (those are
    /// tracked separately in <see cref="FailedFolders"/>).
    /// </remarks>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message if the batch operation failed completely.
    /// </summary>
    /// <remarks>
    /// This property contains the error message for critical failures that prevented
    /// the batch operation from completing. Individual folder processing errors are
    /// not reflected here - this is reserved for system-level failures like missing
    /// directories, configuration errors, or unhandled exceptions.
    /// </remarks>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the total number of folders discovered and queued for processing.
    /// </summary>
    /// <remarks>
    /// This count represents all folders that were identified for potential index
    /// generation, before any processing occurred. It includes folders that were
    /// later processed, skipped, or failed.
    /// </remarks>
    public int TotalFolders { get; set; }

    /// <summary>
    /// Gets or sets the number of folders that were successfully processed (index files generated or updated).
    /// </summary>
    /// <remarks>
    /// This count includes folders where index files were either newly created or
    /// updated due to changes. It represents successful completion of the index
    /// generation process for each folder.
    /// </remarks>
    public int ProcessedFolders { get; set; }

    /// <summary>
    /// Gets or sets the number of folders that were skipped (no changes needed or no content to index).
    /// </summary>
    /// <remarks>
    /// Folders are typically skipped when:
    /// - An up-to-date index file already exists and forceOverwrite is false
    /// - The folder contains no content that requires indexing
    /// - The folder doesn't match the specified template type filters
    ///
    /// Skipped folders are not considered failures - they simply didn't require processing.
    /// </remarks>
    public int SkippedFolders { get; set; }

    /// <summary>
    /// Gets or sets the number of folders that failed to process due to errors.
    /// </summary>
    /// <remarks>
    /// This count represents folders where index generation encountered errors such as:
    /// - File system access problems
    /// - Template processing errors
    /// - Metadata extraction failures
    /// - Unexpected exceptions during processing
    ///
    /// Failed folders are saved to a separate file for analysis and potential retry.
    /// </remarks>
    public int FailedFolders { get; set; }
}