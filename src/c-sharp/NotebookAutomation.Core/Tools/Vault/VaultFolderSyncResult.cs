// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Represents the result of a vault folder synchronization operation.
/// </summary>
/// <remarks>
/// This class encapsulates all the key metrics and status information from a folder
/// synchronization operation between OneDrive and the Vault. It provides detailed statistics
/// about sync outcomes and any errors encountered during the operation.
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
/// var result = await processor.SyncDirectoriesAsync("/onedrive/path", "/vault/path");
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Synchronized {result.SynchronizedFolders} out of {result.TotalFolders} folders");
///     Console.WriteLine($"Created: {result.CreatedVaultFolders}, Skipped: {result.SkippedFolders}, Failed: {result.FailedFolders}");
/// }
/// else
/// {
///     Console.WriteLine($"Operation failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
public class VaultFolderSyncResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the synchronization operation completed successfully.
    /// </summary>
    /// <remarks>
    /// This flag indicates the overall success of the sync operation. It will be false
    /// if there was a critical error that prevented the operation from completing,
    /// but it remains true even if individual folder synchronization failed (those are
    /// tracked separately in <see cref="FailedFolders"/>).
    /// </remarks>
    public bool Success { get; set; } = true;


    /// <summary>
    /// Gets or sets the error message if the synchronization operation failed completely.
    /// </summary>
    /// <remarks>
    /// This property contains the error message for critical failures that prevented
    /// the sync operation from completing. Individual folder sync errors are
    /// not reflected here - this is reserved for system-level failures like missing
    /// directories, configuration errors, or unhandled exceptions.
    /// </remarks>
    public string? ErrorMessage { get; set; }


    /// <summary>
    /// Gets or sets the total number of folders discovered in OneDrive for synchronization.
    /// </summary>
    /// <remarks>
    /// This count represents all folders that were identified in the OneDrive directory
    /// for potential synchronization to the vault. It includes folders that were
    /// later synchronized, created, skipped, or failed.
    /// </remarks>
    public int TotalFolders { get; set; }


    /// <summary>
    /// Gets or sets the number of folders that were successfully synchronized.
    /// </summary>
    /// <remarks>
    /// This count includes folders that already existed in the vault and were verified,
    /// as well as new folders that were successfully created. It represents successful
    /// completion of the synchronization process for each folder.
    /// </remarks>
    public int SynchronizedFolders { get; set; }


    /// <summary>
    /// Gets or sets the number of new folders that were created in the vault.
    /// </summary>
    /// <remarks>
    /// This count specifically tracks folders that did not exist in the vault
    /// and were newly created during the synchronization process from OneDrive. These are
    /// a subset of the <see cref="SynchronizedFolders"/> count.
    /// </remarks>
    public int CreatedVaultFolders { get; set; }


    /// <summary>
    /// Gets or sets the number of new folders that were created in OneDrive.
    /// </summary>
    /// <remarks>
    /// This count specifically tracks folders that did not exist in OneDrive
    /// and were newly created during the bidirectional synchronization process from the vault.
    /// This is only populated when bidirectional synchronization is enabled.
    /// </remarks>
    public int CreatedOneDriveFolders { get; set; }


    /// <summary>
    /// Gets or sets the number of folders that were skipped during synchronization.
    /// </summary>
    /// <remarks>
    /// Folders are typically skipped when:
    /// - They already exist in the vault and are up-to-date
    /// - They don't match the specified synchronization criteria
    /// - They are excluded by configuration filters
    ///
    /// Skipped folders are not considered failures - they simply didn't require processing.
    /// </remarks>
    public int SkippedFolders { get; set; }


    /// <summary>
    /// Gets or sets the number of folders that failed to synchronize due to errors.
    /// </summary>
    /// <remarks>
    /// This count represents folders where synchronization encountered errors such as:
    /// - File system access problems
    /// - Directory creation failures
    /// - Permission issues
    /// - Unexpected exceptions during processing
    ///
    /// Failed folders are logged for analysis and potential retry.
    /// </remarks>
    public int FailedFolders { get; set; }
}
