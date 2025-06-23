// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Services;

/// <summary>
/// Interface for OneDrive service operations including authentication, file operations, sharing, and path mapping.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides methods for interacting with OneDrive, including:
/// <list type="bullet">
/// <item><description>Authentication and token management</description></item>
/// <item><description>File operations: upload, download, list, and search</description></item>
/// <item><description>Sharing: create and retrieve shareable links</description></item>
/// <item><description>Path mapping between local and OneDrive directories</description></item>
/// </list>
/// </para>
/// <para>
/// Implementations should handle Microsoft Graph API integration and provide robust error handling for network operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var oneDriveService = serviceProvider.GetService&lt;IOneDriveService&gt;();
/// await oneDriveService.AuthenticateAsync();
///
/// var files = await oneDriveService.ListFilesAsync("/Documents", CancellationToken.None);
/// foreach (var file in files)
/// {
///     Console.WriteLine(file);
/// }
/// </code>
/// </example>
public interface IOneDriveService
{
    /// <summary>
    /// Authenticates with Microsoft Graph using device code flow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method initiates the device code flow for authentication, allowing users to authenticate
    /// by entering a code on a Microsoft login page. It retrieves and caches tokens for subsequent API calls.
    /// </para>
    /// <para>
    /// If the authentication fails, an exception is thrown with details about the failure.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await oneDriveService.AuthenticateAsync();
    /// Console.WriteLine("Authentication successful.");
    /// </code>
    /// </example>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AuthenticateAsync();

    /// <summary>
    /// Sets the force refresh flag to bypass cached tokens on next authentication.
    /// </summary>
    /// <param name="forceRefresh">If true, will force refresh authentication tokens ignoring cache.</param>
    /// <remarks>
    /// <para>
    /// Use this method to ensure fresh tokens are retrieved during the next authentication attempt.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// oneDriveService.SetForceRefresh(true);
    /// </code>
    /// </example>
    void SetForceRefresh(bool forceRefresh);

    /// <summary>
    /// Forces a refresh of the authentication tokens by clearing cache and re-authenticating.
    /// </summary>
    /// <returns>Task representing the async refresh operation.</returns>
    /// <remarks>
    /// <para>
    /// This method clears cached tokens and initiates a new authentication flow to retrieve fresh tokens.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await oneDriveService.RefreshAuthenticationAsync();
    /// Console.WriteLine("Tokens refreshed.");
    /// </code>
    /// </example>
    Task RefreshAuthenticationAsync();

    /// <summary>
    /// Downloads a file from OneDrive to a local path.
    /// </summary>
    /// <param name="oneDrivePath">The OneDrive file path.</param>
    /// <param name="localPath">The local destination path.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Task representing the async download operation.</returns>
    /// <remarks>
    /// <para>
    /// This method downloads a file from OneDrive to the specified local path. If the operation is canceled,
    /// the task is marked as canceled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await oneDriveService.DownloadFileAsync("/Documents/file.txt", "C:\Downloads\file.txt", CancellationToken.None);
    /// Console.WriteLine("File downloaded.");
    /// </code>
    /// </example>
    Task DownloadFileAsync(string oneDrivePath, string localPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in a OneDrive folder.
    /// </summary>
    /// <param name="oneDriveFolder">The OneDrive folder path.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>List of file names.</returns>
    /// <remarks>
    /// <para>
    /// This method retrieves a list of file names in the specified OneDrive folder. If the folder does not exist,
    /// an empty list is returned.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var files = await oneDriveService.ListFilesAsync("/Documents", CancellationToken.None);
    /// foreach (var file in files)
    /// {
    ///     Console.WriteLine(file);
    /// }
    /// </code>
    /// </example>
    Task<List<string>> ListFilesAsync(string oneDriveFolder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a local file to OneDrive at the specified path.
    /// </summary>
    /// <param name="localPath">The local file path.</param>
    /// <param name="oneDrivePath">The OneDrive destination path (including filename).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Task representing the async upload operation.</returns>
    /// <remarks>
    /// <para>
    /// This method uploads a local file to the specified OneDrive path. If the operation is canceled,
    /// the task is marked as canceled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await oneDriveService.UploadFileAsync("C:\Documents\file.txt", "/Documents/file.txt", CancellationToken.None);
    /// Console.WriteLine("File uploaded.");
    /// </code>
    /// </example>
    Task UploadFileAsync(string localPath, string oneDrivePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a shareable link for a file in OneDrive.
    /// </summary>
    /// <param name="filePath">The local file path or OneDrive file path. If it's a local path, it will be converted to a OneDrive-relative path.</param>
    /// <param name="linkType">The type of sharing link to create. Default is "view".</param>
    /// <param name="scope">The scope of the sharing link. Default is "anonymous".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shareable link URL if successful, null otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a shareable link for the specified file in OneDrive. The link type and scope can be customized.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var link = await oneDriveService.CreateShareLinkAsync("/Documents/file.txt", "edit", "organization", CancellationToken.None);
    /// Console.WriteLine(link);
    /// </code>
    /// </example>
    Task<string?> CreateShareLinkAsync(string filePath, string linkType = "view", string scope = "anonymous", CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for files or folders in OneDrive by name or pattern.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>List of file/folder metadata matching the query.</returns>
    /// <remarks>
    /// <para>
    /// This method searches for files or folders in OneDrive that match the specified query string.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var results = await oneDriveService.SearchFilesAsync("report", CancellationToken.None);
    /// foreach (var result in results)
    /// {
    ///     Console.WriteLine(result);
    /// }
    /// </code>
    /// </example>
    Task<List<Dictionary<string, object>>> SearchFilesAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the local and OneDrive vault root directories for path mapping.
    /// </summary>
    /// <param name="localVaultRoot">The local vault root directory path.</param>
    /// <param name="oneDriveVaultRoot">The OneDrive vault root directory path.</param>
    /// <remarks>
    /// <para>
    /// This method sets up the root directories for mapping between local and OneDrive paths.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// oneDriveService.ConfigureVaultRoots("C:\Vault", "/Vault");
    /// </code>
    /// </example>
    void ConfigureVaultRoots(string localVaultRoot, string oneDriveVaultRoot);

    /// <summary>
    /// Maps a local file path to its corresponding OneDrive path.
    /// </summary>
    /// <param name="localPath">The local file path to map.</param>
    /// <returns>The corresponding OneDrive path.</returns>
    /// <remarks>
    /// <para>
    /// This method converts a local file path to its corresponding OneDrive path based on the configured vault roots.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var oneDrivePath = oneDriveService.MapLocalToOneDrivePath("C:\Vault\file.txt");
    /// Console.WriteLine(oneDrivePath);
    /// </code>
    /// </example>
    string MapLocalToOneDrivePath(string localPath);

    /// <summary>
    /// Maps an OneDrive file path to its corresponding local path.
    /// </summary>
    /// <param name="oneDrivePath">The OneDrive file path to map.</param>
    /// <returns>The corresponding local path.</returns>
    /// <remarks>
    /// <para>
    /// This method converts a OneDrive file path to its corresponding local path based on the configured vault roots.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var localPath = oneDriveService.MapOneDriveToLocalPath("/Vault/file.txt");
    /// Console.WriteLine(localPath);
    /// </code>
    /// </example>
    string MapOneDriveToLocalPath(string oneDrivePath);

    /// <summary>
    /// Gets a share link for a file in OneDrive.
    /// </summary>
    /// <param name="filePath">The path of the file to get a share link for.</param>
    /// <param name="forceRefresh">Whether to force refresh the share link.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The share link information as a JSON string.</returns>
    /// <remarks>
    /// <para>
    /// This method retrieves a share link for the specified file in OneDrive. If <paramref name="forceRefresh"/> is true,
    /// a new link is generated.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var shareLink = await oneDriveService.GetShareLinkAsync("/Documents/file.txt", true, CancellationToken.None);
    /// Console.WriteLine(shareLink);
    /// </code>
    /// </example>
    Task<string> GetShareLinkAsync(string filePath, bool forceRefresh = false, CancellationToken cancellationToken = default);
}
