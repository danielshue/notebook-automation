using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Interface for OneDrive service operations including authentication, file operations, and sharing.
    /// </summary>
    public interface IOneDriveService
    {
        /// <summary>
        /// Authenticates with Microsoft Graph using device code flow.
        /// </summary>
        Task AuthenticateAsync();

        /// <summary>
        /// Downloads a file from OneDrive to a local path.
        /// </summary>
        /// <param name="oneDrivePath">The OneDrive file path.</param>
        /// <param name="localPath">The local destination path.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task DownloadFileAsync(string oneDrivePath, string localPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists files in a OneDrive folder.
        /// </summary>
        /// <param name="oneDriveFolder">The OneDrive folder path.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of file names.</returns>
        Task<List<string>> ListFilesAsync(string oneDriveFolder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a local file to OneDrive at the specified path.
        /// </summary>
        /// <param name="localPath">The local file path.</param>
        /// <param name="oneDrivePath">The OneDrive destination path (including filename).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task UploadFileAsync(string localPath, string oneDrivePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a sharing link for a file in OneDrive.
        /// </summary>
        /// <param name="oneDrivePath">The OneDrive file path.</param>
        /// <param name="type">The type of sharing link (e.g., view, edit).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The sharing link URL, or null if failed.</returns>
        Task<string?> CreateSharingLinkAsync(string oneDrivePath, string type = "view", CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a shareable link for a file in OneDrive.
        /// </summary>
        /// <param name="filePath">The OneDrive file path. Should be relative to the OneDrive root.</param>
        /// <param name="linkType">The type of sharing link to create. Default is "view".</param>
        /// <param name="scope">The scope of the sharing link. Default is "anonymous".</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The shareable link URL if successful, null otherwise.</returns>
        Task<string?> CreateShareLinkAsync(string filePath, string linkType = "view", string scope = "anonymous", CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for files or folders in OneDrive by name or pattern.
        /// </summary>
        /// <param name="query">The search query string.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of file/folder metadata matching the query.</returns>
        Task<List<Dictionary<string, object>>> SearchFilesAsync(string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures the local and OneDrive vault root directories for path mapping.
        /// </summary>
        /// <param name="localVaultRoot">The local vault root directory path.</param>
        /// <param name="oneDriveVaultRoot">The OneDrive vault root directory path.</param>
        void ConfigureVaultRoots(string localVaultRoot, string oneDriveVaultRoot);

        /// <summary>
        /// Maps a local file path to its corresponding OneDrive path.
        /// </summary>
        /// <param name="localPath">The local file path to map.</param>
        /// <returns>The corresponding OneDrive path.</returns>
        string MapLocalToOneDrivePath(string localPath);

        /// <summary>
        /// Maps an OneDrive file path to its corresponding local path.
        /// </summary>
        /// <param name="oneDrivePath">The OneDrive file path to map.</param>
        /// <returns>The corresponding local path.</returns>
        string MapOneDriveToLocalPath(string oneDrivePath);
    }
}
