// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the configuration for the OpenAI provider.
/// </summary>
/// <remarks>
/// <para>
/// This class contains configuration settings specific to the OpenAI API service.
/// It includes endpoint URL and model name settings that are used when the
/// AI service provider is set to "openai".
/// </para>
/// <para>
/// The API key is not stored in this configuration for security reasons.
/// Instead, it should be provided via the OPENAI_API_KEY environment variable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var openAiConfig = new OpenAiProviderConfig
/// {
///     Endpoint = "https://api.openai.com/v1/chat/completions",
///     Model = "gpt-4o"
/// };
/// </code>
/// </example>
public class OpenAiProviderConfig
{
    /// <summary>
    /// Gets or sets the endpoint URL for the OpenAI API.
    /// </summary>
    /// <value>
    /// The complete URL endpoint for the OpenAI API service.
    /// Defaults to "https://api.openai.com/v1/chat/completions" if not specified.
    /// </value>
    /// <remarks>
    /// This property allows for custom OpenAI-compatible endpoints, enabling the use
    /// of alternative services that implement the OpenAI API interface.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new OpenAiProviderConfig
    /// {
    ///     Endpoint = "https://api.openai.com/v1/chat/completions"
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the model name for the OpenAI API.
    /// </summary>
    /// <value>
    /// The name of the OpenAI model to use for API requests.
    /// Common values include "gpt-4o", "gpt-4", "gpt-3.5-turbo", etc.
    /// </value>
    /// <remarks>
    /// The model name determines which OpenAI language model will be used for
    /// generating responses. Different models have varying capabilities, costs,
    /// and token limits.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new OpenAiProviderConfig
    /// {
    ///     Model = "gpt-4o"
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
