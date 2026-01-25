// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.NetworkInformation;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Handles network connectivity detection and offline mode behavior.
/// </summary>
public class NetworkHandler
{
    private readonly ILogger<NetworkHandler> logger;
    private bool? cachedOnlineStatus;
    private DateTime cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Event raised when network status changes.
    /// </summary>
    public event EventHandler<NetworkStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public NetworkHandler(ILogger<NetworkHandler> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Subscribe to network change events
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
    }

    /// <summary>
    /// Gets a value indicating whether the network is currently available.
    /// </summary>
    public bool IsOnline => CheckOnlineStatus();

    /// <summary>
    /// Check if we're currently online.
    /// </summary>
    /// <param name="forceRefresh">Force a fresh check, ignoring cache.</param>
    /// <returns>True if online.</returns>
    public bool CheckOnlineStatus(bool forceRefresh = false)
    {
        if (!forceRefresh && cachedOnlineStatus.HasValue && DateTime.UtcNow < cacheExpiry)
        {
            return cachedOnlineStatus.Value;
        }

        try
        {
            var isOnline = NetworkInterface.GetIsNetworkAvailable();

            if (isOnline)
            {
                // Additional check - try to resolve a known domain
                isOnline = CanResolveHost("api.github.com");
            }

            if (cachedOnlineStatus != isOnline)
            {
                var previous = cachedOnlineStatus;
                cachedOnlineStatus = isOnline;
                cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

                if (previous.HasValue)
                {
                    logger.LogInformation(
                        "Network status changed: {Previous} -> {Current}",
                        previous.Value ? "Online" : "Offline",
                        isOnline ? "Online" : "Offline");

                    StatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(previous.Value, isOnline));
                }
            }
            else
            {
                cachedOnlineStatus = isOnline;
                cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            }

            return isOnline;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check network status, assuming offline");
            cachedOnlineStatus = false;
            cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
            return false;
        }
    }

    /// <summary>
    /// Wait for network to become available.
    /// </summary>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if network became available.</returns>
    public async Task<bool> WaitForNetworkAsync(
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromMinutes(5);
        var deadline = DateTime.UtcNow.Add(timeout.Value);
        var checkInterval = TimeSpan.FromSeconds(5);

        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            if (CheckOnlineStatus(forceRefresh: true))
            {
                return true;
            }

            logger.LogDebug("Network unavailable, waiting {Interval}...", checkInterval);
            await Task.Delay(checkInterval, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Execute an action with offline fallback.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="onlineAction">Action to execute when online.</param>
    /// <param name="offlineFallback">Fallback action when offline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result from either action.</returns>
    public async Task<T> ExecuteWithFallbackAsync<T>(
        Func<CancellationToken, Task<T>> onlineAction,
        Func<CancellationToken, Task<T>> offlineFallback,
        CancellationToken cancellationToken = default)
    {
        if (IsOnline)
        {
            try
            {
                return await onlineAction(cancellationToken);
            }
            catch (Exception ex) when (IsNetworkException(ex))
            {
                logger.LogWarning(ex, "Network error, falling back to offline mode");
                cachedOnlineStatus = false;
            }
        }

        return await offlineFallback(cancellationToken);
    }

    /// <summary>
    /// Execute an action with automatic retry on network errors.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">Action to execute.</param>
    /// <param name="maxRetries">Maximum retry count.</param>
    /// <param name="retryDelay">Delay between retries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Action result.</returns>
    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        retryDelay ??= TimeSpan.FromSeconds(2);
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (attempt > 0 && !IsOnline)
                {
                    logger.LogWarning("Network unavailable, waiting before retry...");
                    await WaitForNetworkAsync(TimeSpan.FromSeconds(30), cancellationToken);
                }

                return await action(cancellationToken);
            }
            catch (Exception ex) when (IsNetworkException(ex) && attempt < maxRetries - 1)
            {
                lastException = ex;
                attempt++;

                var delay = TimeSpan.FromMilliseconds(retryDelay.Value.TotalMilliseconds * Math.Pow(2, attempt - 1));
                logger.LogWarning(
                    "Network error on attempt {Attempt}, retrying in {Delay}...",
                    attempt,
                    delay);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new NetworkException(
            "Network operation failed after maximum retries",
            lastException);
    }

    /// <summary>
    /// Check if an exception is network-related.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if network-related.</returns>
    private static bool IsNetworkException(Exception ex)
    {
        return ex is System.Net.Http.HttpRequestException ||
               ex is System.Net.WebException ||
               ex is System.Net.Sockets.SocketException ||
               ex is TimeoutException ||
               (ex.InnerException != null && IsNetworkException(ex.InnerException));
    }

    /// <summary>
    /// Try to resolve a host to verify connectivity.
    /// </summary>
    private static bool CanResolveHost(string hostname)
    {
        try
        {
            var addresses = System.Net.Dns.GetHostAddresses(hostname);
            return addresses.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Handle network availability changed event.
    /// </summary>
    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        var previous = cachedOnlineStatus;
        cachedOnlineStatus = e.IsAvailable;
        cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

        logger.LogInformation("Network availability changed: {IsAvailable}", e.IsAvailable);

        if (previous.HasValue && previous.Value != e.IsAvailable)
        {
            StatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(previous.Value, e.IsAvailable));
        }
    }

    /// <summary>
    /// Get a user-friendly message for offline status.
    /// </summary>
    /// <returns>Offline message.</returns>
    public static string GetOfflineMessage()
    {
        return "You appear to be offline. Copilot features require an internet connection.\n" +
               "Please check your network connection and try again.";
    }

    /// <summary>
    /// Get suggestions for resolving network issues.
    /// </summary>
    /// <returns>List of suggestions.</returns>
    public static IReadOnlyList<string> GetNetworkTroubleshootingSuggestions()
    {
        return
        [
            "Check your internet connection",
            "Try disconnecting and reconnecting to your network",
            "Check if GitHub services are available at https://www.githubstatus.com/",
            "If using a VPN, try disconnecting temporarily",
            "Check your firewall settings",
            "Try running 'gh auth status' to verify GitHub CLI authentication"
        ];
    }
}

/// <summary>
/// Event arguments for network status changes.
/// </summary>
public class NetworkStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="wasOnline">Previous status.</param>
    /// <param name="isOnline">Current status.</param>
    public NetworkStatusChangedEventArgs(bool wasOnline, bool isOnline)
    {
        WasOnline = wasOnline;
        IsOnline = isOnline;
    }

    /// <summary>
    /// Gets a value indicating whether the network was previously online.
    /// </summary>
    public bool WasOnline { get; }

    /// <summary>
    /// Gets a value indicating whether the network is currently online.
    /// </summary>
    public bool IsOnline { get; }
}

/// <summary>
/// Exception thrown for network-related errors.
/// </summary>
public class NetworkException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    public NetworkException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    public NetworkException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="innerException">Inner exception.</param>
    public NetworkException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
