// Licensed under the MIT License. See LICENSE file in the project root for full license information.


namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the timeout and retry configuration for AI service requests.
/// </summary>
/// <remarks>
/// <para>
/// This class contains settings that control the timeout and retry behavior for
/// AI service requests, including long-running operations like summarization.
/// </para>
/// <para>
/// These settings help manage network reliability issues and ensure that requests
/// have appropriate timeouts and retry attempts configured.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var timeoutConfig = new TimeoutConfig
/// {
///     Timeout = 300,
///     RetryPolicy = new RetryPolicyConfig
///     {
///         MaxRetryAttempts = 5,
///         DelayBetweenRetries = 2000
///     }
/// };
/// </code>
/// </example>
public class TimeoutConfig
{
    private TimeSpan timeSpan1;
    private TimeSpan timeSpan2;

    public TimeoutConfig()
    {

    }

    public TimeoutConfig(TimeSpan timeSpan1, TimeSpan timeSpan2)
    {
        this.timeSpan1 = timeSpan1;
        this.timeSpan2 = timeSpan2;
    }


    /// <summary>
    /// Gets or sets the timeout for individual AI requests in seconds.
    /// </summary>
    /// <value>
    /// The number of seconds to wait for an AI request to complete before timing out.
    /// Defaults to 300 seconds (5 minutes) if not specified.
    /// </value>
    /// <remarks>
    /// This timeout applies to individual AI API calls. For chunked summarization,
    /// each chunk request will use this timeout value.
    /// </remarks>
    [JsonPropertyName("request_timeout_seconds")]
    public int RequestTimeoutSeconds { get; set; } = 300;


    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed requests.
    /// </summary>
    /// <value>
    /// The maximum number of times to retry a failed AI request.
    /// Defaults to 3 if not specified. Set to 0 to disable retries.
    /// </value>
    /// <remarks>
    /// Retries are performed for transient failures such as timeouts or network errors.
    /// The retry mechanism uses exponential backoff to avoid overwhelming the service.
    /// </remarks>
    [JsonPropertyName("max_retry_attempts")]
    public int MaxRetryAttempts { get; set; } = 3;


    /// <summary>
    /// Gets or sets the base delay between retry attempts in seconds.
    /// </summary>
    /// <value>
    /// The initial delay in seconds before the first retry attempt.
    /// Defaults to 2 seconds if not specified.
    /// </value>
    /// <remarks>
    /// This is the base delay used in the exponential backoff calculation.
    /// Actual delays will be: baseDelay, baseDelay * 2, baseDelay * 4, etc.
    /// </remarks>
    [JsonPropertyName("base_retry_delay_seconds")]
    public int BaseRetryDelaySeconds { get; set; } = 2;


    /// <summary>
    /// Gets or sets the maximum delay between retry attempts in seconds.
    /// </summary>
    /// <value>
    /// The maximum delay in seconds between retry attempts.
    /// Defaults to 60 seconds if not specified.
    /// </value>
    /// <remarks>
    /// This caps the exponential backoff delay to prevent excessively long wait times.
    /// Even with exponential backoff, delays will not exceed this value.
    /// </remarks>
    [JsonPropertyName("max_retry_delay_seconds")]
    public int MaxRetryDelaySeconds { get; set; } = 60;


    /// <summary>
    /// Gets or sets the maximum degree of parallelism for chunk processing.
    /// </summary>
    /// <value>
    /// The maximum number of chunks to process simultaneously.
    /// Defaults to 3 if not specified. Set to 1 to disable parallel processing.
    /// </value>
    /// <remarks>
    /// This controls how many AI requests can run concurrently when processing
    /// large texts that are split into chunks. Higher values can improve throughput
    /// but may increase API rate limiting risks and memory usage.
    /// </remarks>
    [JsonPropertyName("max_chunk_parallelism")]
    public int MaxChunkParallelism { get; set; } = 3;


    /// <summary>
    /// Gets or sets the rate limiting delay between chunk requests in milliseconds.
    /// </summary>
    /// <value>
    /// The minimum delay in milliseconds between starting chunk processing requests.
    /// Defaults to 100 milliseconds. Set to 0 to disable rate limiting.
    /// </value>
    /// <remarks>
    /// This helps prevent overwhelming AI services with too many concurrent requests.
    /// Each chunk request will be delayed by at least this amount from the previous one.
    /// </remarks>
    [JsonPropertyName("chunk_rate_limit_ms")]
    public int ChunkRateLimitMs { get; set; } = 100;


    /// <summary>
    /// Gets or sets the maximum number of files to process in parallel during batch operations.
    /// </summary>
    /// <value>
    /// The maximum number of concurrent file processing operations.
    /// Defaults to 2 if not specified. Set to 1 to disable parallel processing.
    /// </value>
    /// <remarks>
    /// This controls how many document files can be processed concurrently during
    /// batch operations. Higher values can improve throughput but may increase
    /// system resource usage and API rate limiting risks. Consider your system's
    /// memory, CPU capacity, and AI service rate limits when configuring this value.
    /// </remarks>
    [JsonPropertyName("max_file_parallelism")]
    public int MaxFileParallelism { get; set; } = 4;


    /// <summary>
    /// Gets or sets the rate limiting delay between file processing starts in milliseconds.
    /// </summary>
    /// <value>
    /// The minimum delay in milliseconds between starting file processing operations.
    /// Defaults to 200 milliseconds. Set to 0 to disable rate limiting.
    /// </value>
    /// <remarks>
    /// This helps prevent overwhelming the system and AI services when processing
    /// multiple files concurrently. Each file processing operation will be delayed
    /// by at least this amount from the previous one when parallel processing is enabled.
    /// </remarks>
    [JsonPropertyName("file_rate_limit_ms")]
    public int FileRateLimitMs { get; set; } = 200;
}
