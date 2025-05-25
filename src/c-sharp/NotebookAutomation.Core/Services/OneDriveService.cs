// Module: OneDriveService.cs
// Provides OneDrive integration for file/folder sync and access using Microsoft Graph API.
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Azure.Identity;
using Microsoft.Kiota.Abstractions;
using System.Text.Json;
using System.Text;

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Provides methods for authenticating and accessing OneDrive files/folders.
    /// </summary>
    public class OneDriveService
    {
        private readonly ILogger<OneDriveService> _logger;
        private readonly string _clientId;
        private readonly string _tenantId;
        private readonly string[] _scopes;
        private GraphServiceClient? _graphClient;
        private string? _accessToken;

        private string? _localVaultRoot;
        private string? _oneDriveVaultRoot;

        private OneDriveCliOptions _cliOptions = new OneDriveCliOptions();

        public OneDriveService(ILogger<OneDriveService> logger, string clientId, string tenantId, string[] scopes)
        {
            _logger = logger;
            _clientId = clientId;
            _tenantId = tenantId;
            _scopes = scopes;
        }

        /// <summary>
        /// Authenticates with Microsoft Graph using device code flow (cross-platform).
        /// </summary>
        public async Task AuthenticateAsync()
        {
            try
            {
                var app = PublicClientApplicationBuilder
                    .Create(_clientId)
                    .WithTenantId(_tenantId)
                    .WithRedirectUri("http://localhost")
                    .Build();
                var result = await app.AcquireTokenWithDeviceCode(_scopes, callback =>
                {
                    Console.WriteLine($"To authenticate, visit {callback.VerificationUrl} and enter code: {callback.UserCode}");
                    return Task.CompletedTask;
                }).ExecuteAsync();
                _accessToken = result.AccessToken;
                // Use TokenCredential for GraphServiceClient v5+
                var credential = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions
                {
                    ClientId = _clientId,
                    TenantId = _tenantId,
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                });
                _graphClient = new GraphServiceClient(credential, _scopes);
                _logger.LogInformation("Authenticated with Microsoft Graph as {User}", result.Account.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate with Microsoft Graph");
                throw;
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
                    _logger.LogError("File not found in OneDrive: {Path}", oneDrivePath);
                    return;
                }
                using (var fileStream = File.Create(localPath))
                {
                    await stream.CopyToAsync(fileStream, cancellationToken);
                }
                _logger.LogInformation("Downloaded OneDrive file: {Path}", oneDrivePath);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Graph API error downloading file: {Path}", oneDrivePath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file from OneDrive: {Path}", oneDrivePath);
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
                    return new List<string>();
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
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files in OneDrive folder: {Folder}", oneDriveFolder);
                return new List<string>();
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
                using (var stream = File.OpenRead(localPath))
                {
                    var requestInfo = new RequestInformation
                    {
                        HttpMethod = Method.PUT,
                        UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}:/content",
                        PathParameters = new Dictionary<string, object> { { "itemPath", oneDrivePath } }
                    };
                    requestInfo.SetStreamContent(stream, "application/octet-stream");
                    var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
                    if (response == null)
                        throw new Exception($"Upload failed for {oneDrivePath}");
                    _logger.LogInformation("Uploaded file to OneDrive: {Path}", oneDrivePath);
                }
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
        /// Creates a sharing link for a file in OneDrive.
        /// </summary>
        /// <param name="oneDrivePath">The OneDrive file path.</param>
        /// <param name="type">The type of sharing link (e.g., view, edit).</param>
        /// <returns>The sharing link URL, or null if failed.</returns>
        public async Task<string?> CreateSharingLinkAsync(string oneDrivePath, string type = "view", CancellationToken cancellationToken = default)
        {
            if (_graphClient == null)
                throw new InvalidOperationException("Not authenticated. Call AuthenticateAsync first.");
            try
            {
                var requestInfo = new RequestInformation
                {
                    HttpMethod = Method.POST,
                    UrlTemplate = "https://graph.microsoft.com/v1.0/me/drive/root:/{itemPath}:/createLink",
                    PathParameters = new Dictionary<string, object> { { "itemPath", oneDrivePath } }
                };
                var body = JsonSerializer.Serialize(new { type });
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(body));
                requestInfo.Content = ms;
                requestInfo.Headers["Content-Type"] = new[] { "application/json" };
                var response = await _graphClient.RequestAdapter.SendPrimitiveAsync<Stream>(requestInfo, cancellationToken: cancellationToken);
                if (response == null)
                    return null;
                using var doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("link", out var linkProp) && linkProp.TryGetProperty("webUrl", out var urlProp))
                {
                    var link = urlProp.GetString();
                    if (link != null)
                        _logger.LogInformation("Created sharing link for OneDrive file: {Path}", oneDrivePath);
                    else
                        _logger.LogWarning("No sharing link returned for OneDrive file: {Path}", oneDrivePath);
                    return link;
                }
                _logger.LogWarning("No sharing link returned for OneDrive file: {Path}", oneDrivePath);
                return null;
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Graph API error creating sharing link for OneDrive file: {Path}", oneDrivePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create sharing link for OneDrive file: {Path}", oneDrivePath);
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
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search OneDrive for query: {Query}", query);
                return new List<Dictionary<string, object>>();
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
                    return new List<string>();
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
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search files/folders in OneDrive: {Pattern}", searchPattern);
                return new List<string>();
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
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list drives");
                return new List<Dictionary<string, object>>();
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
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list root folder items");
                return new List<Dictionary<string, object>>();
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
                return new List<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files with metadata in OneDrive folder: {Folder}", oneDriveFolder);
                return new List<Dictionary<string, object>>();
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
            var suggestions = FindSimilarNames(baseName, siblings.Select(d => d.ContainsKey("name") ? d["name"]?.ToString() ?? string.Empty : string.Empty).ToList());
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
        private List<string> FindSimilarNames(string target, List<string> candidates, int maxSuggestions = 3)
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
        private int LevenshteinDistance(string a, string b)
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
            var relative = normOneDrivePath.Substring(_oneDriveVaultRoot.Length).TrimStart('/');
            var localPath = Path.Combine(_localVaultRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            return localPath;
        }

        /// <summary>
        /// Sets CLI options for dry-run, retry, force, and verbose/debug output.
        /// </summary>
        public void SetCliOptions(OneDriveCliOptions options)
        {
            _cliOptions = options ?? new OneDriveCliOptions();
        }

        // --- Test Coverage and Documentation ---
        // TODO: Add unit and integration tests for all new methods, including error scenarios and edge cases.
        //       Use dependency injection and mocking for GraphServiceClient and RequestAdapter.
        //       Ensure tests cover search, direct ID access, drive/root listing, recursive traversal, and error handling.
    }
}
