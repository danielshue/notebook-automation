// Module: OneDriveService.cs
// Provides OneDrive integration for file/folder sync and access using Microsoft Graph API.
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Services;
/// <summary>
/// Provides methods for authenticating and accessing OneDrive files/folders.
/// </summary>
public class OneDriveService : IOneDriveService
{
    private readonly ILogger<OneDriveService> _logger;
    private readonly string _clientId;
    private readonly string _tenantId;
    private readonly string[] _scopes;
    private GraphServiceClient? _graphClient;
    private bool _forceRefresh = false;
    private IPublicClientApplication? _msalApp;
    private readonly string _tokenCacheFile;

    private string? _localVaultRoot;
    private string? _oneDriveVaultRoot;

    private OneDriveCliOptions _cliOptions = new();

    public OneDriveService(
        ILogger<OneDriveService> logger,
        string clientId,
        string tenantId,
        string[] scopes,
        IPublicClientApplication? msalApp = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _msalApp = msalApp;

        // Setup token cache file in the user's local application data folder
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NotebookAutomation");

        // Create directory if it doesn't exist
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        _tokenCacheFile = Path.Combine(appDataPath, "msal_token_cache.dat");
        _logger.LogDebug("Token cache file path: {TokenCachePath}", _tokenCacheFile);
    }
    /// <summary>
    /// Authenticates with Microsoft Graph API using token caching for persistent authentication.
    /// Uses the same approach as the Python implementation for consistent behavior.
    /// </summary>
    /// <returns>Task representing the authentication process</returns>
    public async Task AuthenticateAsync()
    {


        try
        {
            // Use injected MSAL app if provided, otherwise create
            if (_msalApp == null || _forceRefresh)
            {
                _msalApp = PublicClientApplicationBuilder
                    .Create(_clientId)
                    .WithTenantId(_tenantId)
                    .WithRedirectUri("http://localhost")
                    .Build();

                // Set up token cache
                await ConfigureTokenCacheAsync();
            }

            AuthenticationResult? authResult;
            IAccount? account = null;

            // Try to get existing account from cache
            var accounts = await _msalApp.GetAccountsAsync();
            if (accounts.Any() && !_forceRefresh)
            {
                _logger.LogInformation("Account found in token cache, attempting to use existing token");
                account = accounts.FirstOrDefault();
            }

            // Try silent authentication first if account exists
            if (account != null && !_forceRefresh)
            {
                try
                {
                    _logger.LogDebug("Attempting silent token acquisition");
                    authResult = await _msalApp.AcquireTokenSilent(_scopes, account)
                        .ExecuteAsync();

                    _logger.LogInformation("Successfully acquired token silently for account: {Account}",
                        account.Username);
                }
                catch (MsalUiRequiredException)
                {
                    _logger.LogInformation("Silent token acquisition failed, falling back to interactive authentication");
                    authResult = await InteractiveAuthenticationAsync();
                }
            }
            else
            {
                // No account found or forced refresh - perform interactive authentication
                authResult = await InteractiveAuthenticationAsync();
            }                // Create GraphClient with the token
            if (authResult != null)
            {
                // Create authentication provider with the MSAL token
                var authProvider = new BaseBearerTokenAuthenticationProvider(
                    new TokenProvider(_msalApp, _scopes, _logger));
                _graphClient = new GraphServiceClient(authProvider);

                // Test the authentication by making a simple call to the Drive
                try
                {
                    var drive = await _graphClient.Me.Drive.GetAsync();
                    _logger.LogInformation("Authenticated with Microsoft Graph successfully - Drive ID: {DriveId}",
                        drive?.Id ?? "Unknown");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve user drive info, but authentication may still be valid");
                }
            }
            else
            {
                throw new InvalidOperationException("Failed to obtain authentication token");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to authenticate with Microsoft Graph");
            throw;
        }
        finally
        {
            // Reset force refresh flag
            if (_forceRefresh)
            {
                _forceRefresh = false;
            }
        }
    }

    /// <summary>
    /// Performs interactive authentication with browser prompt
    /// </summary>
    private async Task<AuthenticationResult> InteractiveAuthenticationAsync()
    {
        _logger.LogInformation("Initiating interactive browser authentication...");
        Console.WriteLine("A browser window will open for you to sign in to Microsoft Graph.");

        try
        {
            var result = await _msalApp!.AcquireTokenInteractive(_scopes)
                .WithPrompt(Prompt.SelectAccount) // Force account selection for better user experience
                .ExecuteAsync();

            _logger.LogInformation("Interactive authentication successful for account: {Account}",
                result.Account?.Username ?? "Unknown");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Interactive authentication failed");
            throw;
        }
    }

    /// <summary>
    /// Configures the token cache serialization
    /// </summary>
    private async Task ConfigureTokenCacheAsync()
    {
        try
        {
            // Use token cache helpers from MSAL extensions                // Configure storage properties - simplified to work across platforms
            var storageProperties = new StorageCreationPropertiesBuilder(
                "msal.cache",
                _tokenCacheFile)
                .WithUnprotectedFile()  // For simplicity - uses unprotected storage
                .Build();

            // If token cache file exists, create parent directory
            var cacheParentDir = Path.GetDirectoryName(_tokenCacheFile);
            if (cacheParentDir != null && !Directory.Exists(cacheParentDir))
            {
                Directory.CreateDirectory(cacheParentDir);
            }

            // Setup the cache
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(_msalApp!.UserTokenCache);

            _logger.LogDebug("Token cache configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure token cache - authentication will not be persisted");
        }
    }

    /// <summary>
    /// Sets whether to force refresh authentication tokens ignoring cache.
    /// </summary>
    /// <param name="forceRefresh">If true, will force refresh authentication tokens ignoring cache.</param>
    public void SetForceRefresh(bool forceRefresh)
    {
        _forceRefresh = forceRefresh;
        _logger.LogInformation("Force refresh set to: {ForceRefresh}", forceRefresh);
    }        /// <summary>
             /// Forces a refresh of the authentication tokens by clearing cache and re-authenticating.
             /// </summary>
             /// <returns>Task representing the async refresh operation.</returns>
    public async Task RefreshAuthenticationAsync()
    {
        _logger.LogInformation("Refreshing OneDrive authentication tokens");

        try
        {
            // Clear existing authentication state
            _graphClient = null;

            // Force refresh the authentication
            var originalForceRefresh = _forceRefresh;
            _forceRefresh = true;

            await AuthenticateAsync();

            // Restore original force refresh setting
            _forceRefresh = originalForceRefresh;

            _logger.LogInformation("OneDrive authentication refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh OneDrive authentication");
            throw;
        }
    }

    /// <summary>
    /// Ensures the service is authenticated, automatically refreshing if needed.
    /// </summary>
    /// <returns>Task representing the async authentication check.</returns>
    private async Task EnsureAuthenticatedAsync()
    {
        if (_graphClient == null || _forceRefresh)
        {
            _logger.LogInformation("Authentication required - initializing or refreshing OneDrive authentication");
            if (_forceRefresh)
            {
                await RefreshAuthenticationAsync();
            }
            else
            {
                await AuthenticateAsync();
            }
        }
    }

    /// <summary>
    /// Downloads a file from OneDrive to a local path.
    /// </summary>
    /// <param name="oneDrivePath">The OneDrive file path.</param>
    /// <param name="localPath">The local destination path.</param>
    public async Task DownloadFileAsync(string oneDrivePath, string localPath, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}:/content",
                PathParameters = new Dictionary<string, object> { { "itemPath", oneDrivePath } }
            };
            var stream = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            if (stream == null)
            {
                _logger.LogErrorWithPath("File not found in OneDrive: {FilePath}", oneDrivePath);
                return;
            }
            using (var fileStream = File.Create(localPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            _logger.LogInformationWithPath("Downloaded OneDrive file: {FilePath}", oneDrivePath);
        }
        catch (ServiceException ex)
        {
            _logger.LogErrorWithPath(ex, "Graph API error downloading file: {FilePath}", oneDrivePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithPath(ex, "Failed to download file from OneDrive: {FilePath}", oneDrivePath);
            throw;
        }
    }

    /// <summary>
    /// Lists files in a OneDrive folder.
    /// </summary>
    /// <param name="oneDriveFolder">The OneDrive folder path.</param>
    /// <returns>List of file names.</returns>
    public async Task<List<string>> ListFilesAsync(string oneDriveFolder, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}:/children",
                PathParameters = new Dictionary<string, object> { { "itemPath", oneDriveFolder } }
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            if (response == null)
                return [];
            using var doc = JsonDocument.Parse(response);
            var items = doc.RootElement.GetProperty("value");
            var result = new List<string>();
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var nameProp))
                    result.Add(nameProp.GetString() ?? string.Empty);
            }
            return result;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error listing files in OneDrive folder: {Folder}", oneDriveFolder);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files in OneDrive folder: {Folder}", oneDriveFolder);
            return [];
        }
    }

    /// <summary>
    /// Uploads a local file to OneDrive at the specified path.
    /// </summary>
    /// <param name="localPath">The local file path.</param>
    /// <param name="oneDrivePath">The OneDrive destination path (including filename).</param>
    public async Task UploadFileAsync(string localPath, string oneDrivePath, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            using var stream = File.OpenRead(localPath);
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.PUT,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}:/content",
                PathParameters = new Dictionary<string, object> { { "itemPath", oneDrivePath } }
            };
            requestInfo.SetStreamContent(stream, "application/octet-stream");
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken) ?? throw new Exception($"Upload failed for {oneDrivePath}");
            _logger.LogInformation("Uploaded file to OneDrive: {Path}", oneDrivePath);
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error uploading file to OneDrive: {Path}", oneDrivePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to OneDrive: {Path}", oneDrivePath);
            throw;
        }
    }

    /// <summary>
    /// Creates a shareable link for a file in OneDrive.
    /// </summary>
    /// <param name="filePath">The local file path or OneDrive file path. If it's a local path, it will be converted to a OneDrive-relative path.</param>
    /// <param name="linkType">The type of sharing link to create. Default is "view".</param>
    /// <param name="scope">The scope of the sharing link. Default is "anonymous".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The shareable link URL if successful, null otherwise.</returns>
    public async Task<string?> CreateShareLinkAsync(string filePath, string linkType = "view", string scope = "anonymous", CancellationToken cancellationToken = default)
    {
        // Ensure we're authenticated before proceeding
        await EnsureAuthenticatedAsync();            // Declare oneDrivePath outside try block so it's accessible in catch blocks
        string oneDrivePath = filePath;              // Initialize with original path
        try
        {
            // Convert local path to OneDrive path if necessary
            if (Path.IsPathRooted(filePath))
            {
                // This is a local file path, convert it to OneDrive path
                oneDrivePath = MapLocalToOneDrivePath(filePath);
                _logger.LogDebug("Converted local path '{LocalPath}' to OneDrive path '{OneDrivePath}'", filePath, oneDrivePath);
            }
            else
            {
                // This is already a OneDrive-relative path
                oneDrivePath = filePath;
            }

            // Normalize the OneDrive file path
            oneDrivePath = oneDrivePath.Replace('\\', '/');
            if (oneDrivePath.StartsWith('/'))
                oneDrivePath = oneDrivePath[1..];
            _logger.LogInformationWithPath(oneDrivePath, "Creating sharing link for OneDrive file");

            // Prepare the request
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.POST,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}:/createLink",
                PathParameters = new Dictionary<string, object> { { "itemPath", oneDrivePath } }
            };

            // Set the content type header and body
            requestInfo.Headers.Add("Content-Type", "application/json");                // Create the request body
            var jsonContent = $"{{\"type\":\"{linkType}\",\"scope\":\"{scope}\"}}";
            requestInfo.Content = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));

            // Send the request and parse the response
            if (_graphClient?.RequestAdapter == null)
            {
                _logger.LogErrorWithPath(oneDrivePath, "Graph client or request adapter is null when creating sharing link for OneDrive file");
                return null;
            }

            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            if (response == null)
            {
                _logger.LogErrorWithPath(oneDrivePath, "Received null response when creating sharing link for OneDrive file");
                return null;
            }

            // Parse the JSON response
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.TryGetProperty("link", out var linkElement) && linkElement.TryGetProperty("webUrl", out var webUrlElement))
            {
                string? sharingLink = webUrlElement.GetString();
                if (!string.IsNullOrEmpty(sharingLink))
                {
                    _logger.LogInformationWithPath(oneDrivePath, $"Sharing link created successfully for OneDrive file (original: {filePath})");
                    return sharingLink;
                }
            }

            _logger.LogError("Sharing link not found in response for OneDrive file: {OneDrivePath} (original: {OriginalPath})", oneDrivePath, filePath);
            _logger.LogErrorWithPath(oneDrivePath, $"Sharing link not found in response for OneDrive file (original: {filePath})");
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogErrorWithPath(oneDrivePath, $"Failed to create sharing link for OneDrive file (original: {filePath}). Exception: {ex.Message}");

            // Check if the exception message contains 404 error code
            if (ex.Message.Contains("404") || ex.Message.Contains("not found"))
            {
                _logger.LogWarningWithPath(oneDrivePath, "The file might not exist. Check the file path and try again.");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithPath(oneDrivePath, $"Failed to create sharing link for OneDrive file (original: {filePath}). Exception: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Searches for files or folders in OneDrive by name or pattern.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <returns>List of file/folder metadata matching the query.</returns>
    public async Task<List<Dictionary<string, object>>> SearchFilesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root/search(q='{q}')",
                PathParameters = new Dictionary<string, object> { { "q", query } }
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            var results = new List<Dictionary<string, object>>();
            if (response == null)
                return results;
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("value", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in item.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ToString();
                    }
                    results.Add(dict);
                }
            }
            return results;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error searching OneDrive for query: {Query}", query);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search OneDrive for query: {Query}", query);
            return [];
        }
    }

    /// <summary>
    /// Gets file or folder metadata by OneDrive item ID.
    /// </summary>
    /// <param name="itemId">The OneDrive item ID.</param>
    /// <returns>Dictionary of file/folder metadata, or null if not found.</returns>
    public async Task<Dictionary<string, object>?> GetFileByIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/items/{itemId}",
                PathParameters = new Dictionary<string, object> { { "itemId", itemId } }
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            if (response == null)
                return null;
            using var doc = JsonDocument.Parse(response);
            var dict = new Dictionary<string, object>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ToString();
            }
            return dict;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error getting OneDrive item by ID: {Id}", itemId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OneDrive item by ID: {Id}", itemId);
            return null;
        }
    }

    /// <summary>
    /// Searches for files and folders in OneDrive by name or pattern.
    /// </summary>
    /// <param name="searchPattern">The search pattern (e.g., file name or wildcard pattern).</param>
    /// <returns>List of matching file/folder names.</returns>
    public async Task<List<string>> SearchAsync(string searchPattern, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/search(q='{searchPattern}')",
                PathParameters = new Dictionary<string, object> { { "searchPattern", searchPattern } }
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            if (response == null)
                return [];
            using var doc = JsonDocument.Parse(response);
            var items = doc.RootElement.GetProperty("value");
            var result = new List<string>();
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var nameProp))
                    result.Add(nameProp.GetString() ?? string.Empty);
            }
            _logger.LogInformation("Search completed for pattern: {Pattern}, found: {Count} items", searchPattern, result.Count);
            return result;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error searching files/folders in OneDrive: {Pattern}", searchPattern);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search files/folders in OneDrive: {Pattern}", searchPattern);
            return [];
        }
    }

    /// <summary>
    /// Gets a file or folder by its OneDrive item ID.
    /// </summary>
    /// <param name="itemId">The OneDrive item ID.</param>
    /// <returns>The file or folder name, or null if not found.</returns>
    public async Task<string?> GetItemByIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/items/{itemId}",
                PathParameters = new Dictionary<string, object> { { "itemId", itemId } }
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            if (response == null)
                return null;
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("name", out var nameProp))
            {
                var name = nameProp.GetString();
                _logger.LogInformation("Retrieved item by ID: {Id}, Name: {Name}", itemId, name);
                return name;
            }
            _logger.LogWarning("Item ID not found: {Id}", itemId);
            return null;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error getting item by ID from OneDrive: {Id}", itemId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get item by ID from OneDrive: {Id}", itemId);
            return null;
        }
    }

    /// <summary>
    /// Lists all available OneDrive drives for the authenticated user.
    /// </summary>
    /// <returns>List of drive metadata dictionaries.</returns>
    public async Task<List<Dictionary<string, object>>> ListDrivesAsync(CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drives"
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            var results = new List<Dictionary<string, object>>();
            if (response == null)
                return results;
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("value", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in item.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ToString();
                    }
                    results.Add(dict);
                }
            }
            return results;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error listing drives");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list drives");
            return [];
        }
    }

    /// <summary>
    /// Lists items in the root folder of the default OneDrive.
    /// </summary>
    /// <returns>List of file/folder metadata dictionaries in the root folder.</returns>
    public async Task<List<Dictionary<string, object>>> ListRootItemsAsync(CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root/children"
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            var results = new List<Dictionary<string, object>>();
            if (response == null)
                return results;
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("value", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in item.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ToString();
                    }
                    results.Add(dict);
                }
            }
            return results;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error listing root folder items");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list root folder items");
            return [];
        }
    }

    /// <summary>
    /// Recursively lists all files and folders under a given OneDrive folder path, with optional filtering.
    /// </summary>
    /// <param name="oneDriveFolder">The OneDrive folder path to start from.</param>
    /// <param name="fileExtensionFilter">Optional file extension filter (e.g., ".pdf").</param>
    /// <returns>List of file/folder metadata dictionaries.</returns>
    public async Task<List<Dictionary<string, object>>> ListFilesRecursiveAsync(string oneDriveFolder, string? fileExtensionFilter = null, CancellationToken cancellationToken = default)
    {
        var results = new List<Dictionary<string, object>>();
        async Task TraverseAsync(string folderPath)
        {
            var items = await ListFilesWithMetadataAsync(folderPath, cancellationToken);
            foreach (var item in items)
            {
                if (item.TryGetValue("folder", out _))
                {
                    // It's a folder, recurse
                    if (item.TryGetValue("name", out var nameObj) && nameObj is string name)
                    {
                        var subfolderPath = string.IsNullOrEmpty(folderPath) ? name : $"{folderPath}/{name}";
                        await TraverseAsync(subfolderPath);
                    }
                }
                else if (fileExtensionFilter == null || (item.TryGetValue("name", out var fnameObj) && fnameObj is string fname && fname.EndsWith(fileExtensionFilter, StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add(item);
                }
            }
        }
        await TraverseAsync(oneDriveFolder);
        return results;
    }

    /// <summary>
    /// Lists files in a OneDrive folder, returning full metadata for each item.
    /// </summary>
    /// <param name="oneDriveFolder">The OneDrive folder path.</param>
    /// <returns>List of file/folder metadata dictionaries.</returns>
    public async Task<List<Dictionary<string, object>>> ListFilesWithMetadataAsync(string oneDriveFolder, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}:/children",
                PathParameters = new Dictionary<string, object> { { "itemPath", oneDriveFolder } }
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            var results = new List<Dictionary<string, object>>();
            if (response == null)
                return results;
            using var doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("value", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in item.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ToString();
                    }
                    results.Add(dict);
                }
            }
            return results;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error listing files with metadata in OneDrive folder: {Folder}", oneDriveFolder);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files with metadata in OneDrive folder: {Folder}", oneDriveFolder);
            return [];
        }
    }

    /// <summary>
    /// Attempts to resolve a OneDrive path using alternative formats if the initial request fails.
    /// If not found, suggests similar files/folders in the parent directory.
    /// </summary>
    /// <param name="oneDrivePath">The original OneDrive path.</param>
    /// <returns>Resolved path or null if not found. Logs suggestions if not found.</returns>
    public async Task<string?> TryAlternativePathFormatsAsync(string oneDrivePath, CancellationToken cancellationToken = default)
    {
        // Try with and without leading slash, different separators, and case-insensitive
        var candidates = new List<string>
        {
            oneDrivePath,
            oneDrivePath.TrimStart('/'),
            oneDrivePath.Replace("\\", "/"),
            oneDrivePath.Replace("/", "\\")
        };
        var lower = oneDrivePath.ToLowerInvariant();
        if (!candidates.Contains(lower)) candidates.Add(lower);
        foreach (var candidate in candidates.Distinct())
        {
            try
            {
                var meta = await GetFileByPathAsync(candidate, cancellationToken);
                if (meta != null)
                    return candidate;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Path format candidate failed: {Path}", candidate);
            }
        }
        // Fuzzy search for similar files/folders in the parent directory
        var parent = Path.GetDirectoryName(oneDrivePath.Replace("\\", "/")) ?? string.Empty;
        var baseName = Path.GetFileName(oneDrivePath.Replace("\\", "/"));
        var siblings = await ListFilesWithMetadataAsync(parent, cancellationToken);
        var suggestions = FindSimilarNames(baseName, [.. siblings.Select(d => d.TryGetValue("name", out object? value) ? value?.ToString() ?? string.Empty : string.Empty)]);
        if (suggestions.Count > 0)
        {
            _logger.LogWarning("Path not found: {Path}. Did you mean: {Suggestions}", oneDrivePath, string.Join(", ", suggestions));
        }
        else
        {
            _logger.LogWarning("All alternative path format attempts failed for: {Path}", oneDrivePath);
        }
        return null;
    }

    /// <summary>
    /// Finds similar names to a target using Levenshtein distance.
    /// </summary>
    private static List<string> FindSimilarNames(string target, List<string> candidates, int maxSuggestions = 3)
    {
        var ranked = candidates
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => new { Name = n, Distance = LevenshteinDistance(target, n) })
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Name)
            .Take(maxSuggestions)
            .Select(x => x.Name)
            .ToList();
        return ranked;
    }

    /// <summary>
    /// Computes the Levenshtein distance between two strings.
    /// </summary>
    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[a.Length, b.Length];
    }

    /// <summary>
    /// Gets file or folder metadata by OneDrive path.
    /// </summary>
    /// <param name="oneDrivePath">The OneDrive file or folder path.</param>
    /// <returns>Dictionary of metadata, or null if not found.</returns>
    public async Task<Dictionary<string, object>?> GetFileByPathAsync(string oneDrivePath, CancellationToken cancellationToken = default)
    {
        if (_graphClient == null)
            throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
        try
        {
            var requestInfo = new RequestInformation
            {
                HttpMethod = Method.GET,
                UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}",
                PathParameters = new Dictionary<string, object> { { "itemPath", oneDrivePath } }
            };
            var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
            if (response == null)
                return null;
            using var doc = JsonDocument.Parse(response);
            var dict = new Dictionary<string, object>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ToString();
            }
            return dict;
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error getting OneDrive item by path: {Path}", oneDrivePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OneDrive item by path: {Path}", oneDrivePath);
            return null;
        }
    }

    // --- Path Mapping and CLI Option Support (Implemented) ---

    /// <summary>
    /// Configures the root directories for local and OneDrive vaults for path mapping.
    /// </summary>
    /// <param name="localVaultRoot">The root directory of the local vault.</param>
    /// <param name="oneDriveVaultRoot">The root directory of the OneDrive vault (relative to OneDrive root).</param>
    public void ConfigureVaultRoots(string localVaultRoot, string oneDriveVaultRoot)
    {
        _localVaultRoot = localVaultRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _oneDriveVaultRoot = oneDriveVaultRoot.TrimEnd('/', '\\');
    }

    /// <summary>
    /// Maps a local path to a OneDrive path, preserving structure.
    /// </summary>
    /// <param name="localPath">The local file or folder path.</param>
    /// <returns>Corresponding OneDrive path.</returns>
    public string MapLocalToOneDrivePath(string localPath)
    {
        if (string.IsNullOrEmpty(_localVaultRoot) || string.IsNullOrEmpty(_oneDriveVaultRoot))
            throw new InvalidOperationException("Vault roots not configured. Call ConfigureVaultRoots first.");
        var fullLocalPath = Path.GetFullPath(localPath);
        var fullVaultRoot = Path.GetFullPath(_localVaultRoot);
        if (!fullLocalPath.StartsWith(fullVaultRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Local path '{localPath}' is not under the configured vault root '{_localVaultRoot}'.");
        var relative = Path.GetRelativePath(fullVaultRoot, fullLocalPath);
        var oneDrivePath = _oneDriveVaultRoot + "/" + relative.Replace(Path.DirectorySeparatorChar, '/');
        return oneDrivePath;
    }

    /// <summary>
    /// Maps a OneDrive path to a local path, preserving structure.
    /// </summary>
    /// <param name="oneDrivePath">The OneDrive file or folder path.</param>
    /// <returns>Corresponding local path.</returns>
    public string MapOneDriveToLocalPath(string oneDrivePath)
    {
        if (string.IsNullOrEmpty(_localVaultRoot) || string.IsNullOrEmpty(_oneDriveVaultRoot))
            throw new InvalidOperationException("Vault roots not configured. Call ConfigureVaultRoots first.");
        var normOneDrivePath = oneDrivePath.Replace("\\", "/");
        if (!normOneDrivePath.StartsWith(_oneDriveVaultRoot, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"OneDrive path '{oneDrivePath}' is not under the configured OneDrive vault root '{_oneDriveVaultRoot}'.");
        var relative = normOneDrivePath[_oneDriveVaultRoot.Length..].TrimStart('/');
        var localPath = Path.Combine(_localVaultRoot, relative.Replace('/', Path.DirectorySeparatorChar)); return localPath;
    }

    /// <summary>
    /// Gets a share link for a file in OneDrive.
    /// </summary>
    /// <param name="filePath">The path of the file to get a share link for.</param>
    /// <param name="forceRefresh">Whether to force refresh the share link.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The share link information as a JSON string.</returns>
    async Task<string> IOneDriveService.GetShareLinkAsync(string filePath, bool forceRefresh, CancellationToken cancellationToken)
    {
        try
        {
            await AuthenticateAsync();

            if (_graphClient == null)
            {
                throw new InvalidOperationException("Graph client not initialized. Authentication failed.");
            }

            // For now, just return the same result as CreateShareLinkAsync
            string? shareLink = await CreateShareLinkAsync(filePath, "view", "anonymous", cancellationToken);
            return shareLink ?? "{}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting share link for file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Sets CLI options for dry-run, retry, force, and verbose/debug output.
    /// </summary>
    public void SetCliOptions(OneDriveCliOptions options)
    {
        _cliOptions = options ?? new OneDriveCliOptions();
    }

    // --- Test Coverage and Documentation ---        // TODO: Add unit and integration tests for all new methods, including error scenarios and edge cases.
    //       Use dependency injection and mocking for GraphServiceClient and RequestAdapter.
    //       Ensure tests cover search, direct ID access, drive/root listing, recursive traversal, and error handling.
}

/// <summary>
/// Token provider for Microsoft Graph authentication using MSAL
/// </summary>
/// <remarks>
/// Initializes a new instance of the TokenProvider class
/// </remarks>
/// <param name="msalApp">The MSAL public client application</param>
/// <param name="scopes">Authentication scopes</param>
/// <param name="logger">Logger instance</param>
internal class TokenProvider(IPublicClientApplication msalApp, string[] scopes, ILogger logger) : IAccessTokenProvider
{
    private readonly IPublicClientApplication _msalApp = msalApp ?? throw new ArgumentNullException(nameof(msalApp));
    private readonly string[] _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets the authentication provider unique ID
    /// </summary>
    public static string? CachedSerializationId => null;

    /// <summary>
    /// Obtains a token from the MSAL client
    /// </summary>
    /// <param name="uri">The URI for the request (ignored)</param>
    /// <param name="additionalAuthenticationContext">Additional context (ignored)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The access token</returns>
    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all accounts
            var accounts = await _msalApp.GetAccountsAsync();
            AuthenticationResult? result = null;

            // Try silent authentication first with any available account
            if (accounts.Any())
            {
                try
                {
                    _logger.LogDebug("Attempting silent token acquisition for Graph request");
                    result = await _msalApp.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                        .ExecuteAsync(cancellationToken);
                }
                catch (MsalUiRequiredException)
                {
                    // Token expired or requires interaction
                    _logger.LogInformation("Silent token acquisition failed, falling back to interactive");
                    result = await _msalApp.AcquireTokenInteractive(_scopes)
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync(cancellationToken);
                }
            }
            else
            {
                // No accounts, must do interactive auth
                _logger.LogInformation("No accounts found, performing interactive authentication");
                result = await _msalApp.AcquireTokenInteractive(_scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(cancellationToken);
            }

            if (result != null)
            {
                return result.AccessToken;
            }

            throw new InvalidOperationException("Failed to obtain access token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining access token");
            throw;
        }
    }        /// <summary>
             /// Determines if this provider can authenticate the request
             /// </summary>
             /// <param name="uri">The URI being requested</param>
             /// <returns>Whether this provider can authenticate</returns>
    public AllowedHostsValidator AllowedHostsValidator => new(_validHosts);
    internal static readonly string[] _validHosts = ["graph.microsoft.com"];
}
