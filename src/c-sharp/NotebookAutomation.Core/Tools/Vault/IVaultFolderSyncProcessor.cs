// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Interface for synchronizing directory structures between OneDrive and Obsidian vault.
/// </summary>
/// <remarks>
/// This interface defines the contract for vault folder synchronization operations,
/// providing methods to synchronize directory structures from OneDrive to vault locations
/// while supporting progress tracking, dry-run scenarios, and comprehensive error handling.
/// </remarks>
public interface IVaultFolderSyncProcessor
{
    /// <summary>
    /// Event triggered when processing progress changes during synchronization.
    /// </summary>
    event EventHandler<DocumentProcessingProgressEventArgs>? ProcessingProgressChanged;


    /// <summary>
    /// Synchronizes directory structures between OneDrive and vault locations.
    /// </summary>
    /// <param name="oneDrivePath">
    /// The relative path within OneDrive to synchronize from.
    /// This path is combined with the configured onedrive_fullpath_root and 
    /// notebook_vault_resources_basepath to form the complete source path.
    /// </param>
    /// <param name="vaultPath">
    /// The target vault path where directories should be synchronized to.
    /// This should be an absolute path within the vault structure.
    /// If not provided, uses the configured notebook_vault_fullpath_root.
    /// </param>
    /// <param name="dryRun">
    /// When true, simulates the synchronization process without creating actual directories.
    /// Useful for previewing changes, validation, and testing scenarios.
    /// Default is false for normal operation.
    /// </param>
    /// <param name="bidirectional">
    /// When true, performs bidirectional synchronization - creates missing directories
    /// in both OneDrive and vault. When false, only creates missing vault directories.
    /// Default is true for bidirectional synchronization to keep both locations in sync.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous synchronization operation.
    /// The task result contains statistics about the synchronization process including
    /// success status, counts of processed, created, skipped, and failed directories.
    /// </returns>
    Task<VaultFolderSyncResult> SyncDirectoriesAsync(
        string oneDrivePath,
        string? vaultPath,
        bool dryRun = false,
        bool bidirectional = true);
}
