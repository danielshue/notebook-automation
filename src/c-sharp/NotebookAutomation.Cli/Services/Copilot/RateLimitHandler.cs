// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Handles rate limiting for Copilot API calls with exponential backoff.
/// </summary>
public class RateLimitHandler
{
    private readonly ILogger<RateLimitHandler> logger;
    private readonly SemaphoreSlim requestSemaphore;
    private readonly Queue<DateTime> requestHistory = new();
    private readonly object lockObj = new();

    private DateTime? rateLimitResetTime;
    private int remainingRequests = int.MaxValue;
    private bool isRateLimited;

    /// <summary>
    /// Maximum requests per minute.
    /// </summary>
    public int MaxRequestsPerMinute { get; init; } = 60;

    /// <summary>
    /// Maximum concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; init; } = 5;

    /// <summary>
    /// Base delay for exponential backoff.
    /// </summary>
    public TimeSpan BaseBackoffDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay for exponential backoff.
    /// </summary>
    public TimeSpan MaxBackoffDelay { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Event raised when rate limit is hit.
    /// </summary>
    public event EventHandler<RateLimitEventArgs>? RateLimitHit;

    /// <summary>
    /// Event raised when rate limit is recovered.
    /// </summary>
    public event EventHandler? RateLimitRecovered;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public RateLimitHandler(ILogger<RateLimitHandler> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        requestSemaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
    }

    /// <summary>
    /// Gets a value indicating whether we are currently rate limited.
    /// </summary>
    public bool IsRateLimited
    {
        get
        {
            if (!isRateLimited)
            {
                return false;
            }

            if (rateLimitResetTime.HasValue && DateTime.UtcNow >= rateLimitResetTime.Value)
            {
                isRateLimited = false;
                RateLimitRecovered?.Invoke(this, EventArgs.Empty);
            }

            return isRateLimited;
        }
    }

    /// <summary>
    /// Gets the estimated time until rate limit resets.
    /// </summary>
    public TimeSpan? TimeUntilReset
    {
        get
        {
            if (!rateLimitResetTime.HasValue || DateTime.UtcNow >= rateLimitResetTime.Value)
            {
                return null;
            }

            return rateLimitResetTime.Value - DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets the remaining requests in the current window.
    /// </summary>
    public int RemainingRequests => remainingRequests;

    /// <summary>
    /// Acquire permission to make a request.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Disposable to release the request slot.</returns>
    public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken = default)
    {
        // Wait if we're rate limited
        if (IsRateLimited && rateLimitResetTime.HasValue)
        {
            var waitTime = rateLimitResetTime.Value - DateTime.UtcNow;
            if (waitTime > TimeSpan.Zero)
            {
                logger.LogWarning("Rate limited, waiting {WaitTime}", waitTime);
                await Task.Delay(waitTime, cancellationToken);
            }
        }

        // Check local rate limiting
        await WaitForLocalRateLimitAsync(cancellationToken);

        // Acquire semaphore for concurrent request limiting
        await requestSemaphore.WaitAsync(cancellationToken);

        // Record the request
        RecordRequest();

        return new ReleaseHandle(requestSemaphore);
    }

    /// <summary>
    /// Wait for local rate limit window to allow request.
    /// </summary>
    private async Task WaitForLocalRateLimitAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (lockObj)
            {
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-1);

                // Remove old requests from history
                while (requestHistory.Count > 0 && requestHistory.Peek() < windowStart)
                {
                    requestHistory.Dequeue();
                }

                // Check if we can proceed
                if (requestHistory.Count < MaxRequestsPerMinute)
                {
                    return;
                }

                // Calculate wait time until oldest request falls out of window
                var oldestRequest = requestHistory.Peek();
                var waitUntil = oldestRequest.AddMinutes(1);
                var waitTime = waitUntil - now;

                if (waitTime <= TimeSpan.Zero)
                {
                    return;
                }

                logger.LogDebug(
                    "Local rate limit reached ({Count}/{Max}), waiting {WaitTime}",
                    requestHistory.Count,
                    MaxRequestsPerMinute,
                    waitTime);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
    }

    /// <summary>
    /// Record a request in the history.
    /// </summary>
    private void RecordRequest()
    {
        lock (lockObj)
        {
            requestHistory.Enqueue(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Process rate limit information from HTTP response headers.
    /// </summary>
    /// <param name="headers">Response headers.</param>
    public void ProcessResponseHeaders(HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) &&
            int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
        {
            remainingRequests = remaining;
            logger.LogDebug("Rate limit remaining: {Remaining}", remaining);
        }

        if (headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
            long.TryParse(resetValues.FirstOrDefault(), out var resetTimestamp))
        {
            rateLimitResetTime = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).UtcDateTime;
            logger.LogDebug("Rate limit resets at: {ResetTime}", rateLimitResetTime);
        }
    }

    /// <summary>
    /// Process a rate limit error (HTTP 429).
    /// </summary>
    /// <param name="response">HTTP response.</param>
    public void ProcessRateLimitResponse(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.TooManyRequests)
        {
            return;
        }

        isRateLimited = true;

        // Try to get retry-after header
        if (response.Headers.RetryAfter?.Delta.HasValue == true)
        {
            rateLimitResetTime = DateTime.UtcNow.Add(response.Headers.RetryAfter.Delta.Value);
        }
        else if (response.Headers.RetryAfter?.Date.HasValue == true)
        {
            rateLimitResetTime = response.Headers.RetryAfter.Date.Value.UtcDateTime;
        }
        else
        {
            // Default to 60 seconds if no header
            rateLimitResetTime = DateTime.UtcNow.AddSeconds(60);
        }

        remainingRequests = 0;

        var waitTime = TimeUntilReset ?? TimeSpan.FromSeconds(60);
        logger.LogWarning(
            "Rate limit hit (HTTP 429). Waiting until {ResetTime} ({WaitTime})",
            rateLimitResetTime,
            waitTime);

        RateLimitHit?.Invoke(this, new RateLimitEventArgs(waitTime, rateLimitResetTime.Value));
    }

    /// <summary>
    /// Execute a function with rate limiting and exponential backoff.
    /// </summary>
    /// <typeparam name="T">Return type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="maxRetries">Maximum retries on rate limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Action result.</returns>
    public async Task<T> ExecuteWithBackoffAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxRetries = 5,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = BaseBackoffDelay;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (await AcquireAsync(cancellationToken))
            {
                try
                {
                    return await action(cancellationToken);
                }
                catch (HttpRequestException ex) when (IsRateLimitException(ex) && attempt < maxRetries)
                {
                    attempt++;

                    // Calculate exponential backoff with jitter
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                    var backoff = TimeSpan.FromTicks(Math.Min(
                        delay.Ticks * (long)Math.Pow(2, attempt - 1),
                        MaxBackoffDelay.Ticks));
                    var totalDelay = backoff + jitter;

                    logger.LogWarning(
                        "Rate limit error on attempt {Attempt}/{MaxRetries}, backing off for {Delay}",
                        attempt,
                        maxRetries,
                        totalDelay);

                    await Task.Delay(totalDelay, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Check if an exception is a rate limit error.
    /// </summary>
    private static bool IsRateLimitException(HttpRequestException ex)
    {
        return ex.StatusCode == HttpStatusCode.TooManyRequests ||
               (ex.Message?.Contains("429") == true);
    }

    /// <summary>
    /// Get a user-friendly message about rate limiting.
    /// </summary>
    /// <returns>Rate limit message.</returns>
    public string GetRateLimitMessage()
    {
        if (!IsRateLimited)
        {
            return "No rate limit currently active.";
        }

        var waitTime = TimeUntilReset;
        if (waitTime.HasValue)
        {
            var formatted = waitTime.Value.TotalMinutes >= 1
                ? $"{waitTime.Value.Minutes} minute(s) and {waitTime.Value.Seconds} second(s)"
                : $"{waitTime.Value.Seconds} second(s)";

            return $"Rate limit active. Please wait {formatted} before making more requests.";
        }

        return "Rate limit active. Please wait before making more requests.";
    }

    /// <summary>
    /// Disposable handle to release semaphore.
    /// </summary>
    private sealed class ReleaseHandle : IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private bool disposed;

        public ReleaseHandle(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                semaphore.Release();
                disposed = true;
            }
        }
    }
}

/// <summary>
/// Event arguments for rate limit events.
/// </summary>
public class RateLimitEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitEventArgs"/> class.
    /// </summary>
    /// <param name="waitTime">Time to wait.</param>
    /// <param name="resetTime">When rate limit resets.</param>
    public RateLimitEventArgs(TimeSpan waitTime, DateTime resetTime)
    {
        WaitTime = waitTime;
        ResetTime = resetTime;
    }

    /// <summary>
    /// Gets the time to wait before retrying.
    /// </summary>
    public TimeSpan WaitTime { get; }

    /// <summary>
    /// Gets the time when the rate limit resets.
    /// </summary>
    public DateTime ResetTime { get; }
}
