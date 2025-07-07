// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the retry policy configuration for AI service requests.
/// </summary>
/// <remarks>
/// <para>
/// This class contains settings that control the retry behavior for AI service requests,
/// allowing for automatic retries in case of transient errors or timeouts.
/// </para>
/// <para>
/// Configuring an appropriate retry policy helps improve the resilience of AI service
/// integrations, especially in the presence of intermittent network issues or service
/// availability fluctuations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var retryPolicyConfig = new RetryPolicyConfig
/// {
///     MaxRetryAttempts = 5,
///     DelayBetweenRetries = 2000
/// };
/// </code>
/// </example>
public class RetryPolicyConfig
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for AI requests.
    /// </summary>
    /// <value>
    /// The maximum number of retry attempts. Defaults to 3 if not specified.
    /// </value>
    /// <remarks>
    /// This value determines how many times an AI request will be retried in case of
    /// failure due to transient errors. Set to 0 to disable retries.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new RetryPolicyConfig
    /// {
    ///     MaxRetryAttempts = 5
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("max_retry_attempts")]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts for AI requests, in milliseconds.
    /// </summary>
    /// <value>
    /// The delay duration in milliseconds. Defaults to 1000 milliseconds (1 second) if not specified.
    /// </value>
    /// <remarks>
    /// This value determines the wait time between consecutive retry attempts for an AI request.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new RetryPolicyConfig
    /// {
    ///     DelayBetweenRetries = 2000
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("delay_between_retries")]
    public int DelayBetweenRetries { get; set; } = 1000;
}
