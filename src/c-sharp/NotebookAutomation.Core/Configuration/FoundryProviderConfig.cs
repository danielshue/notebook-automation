// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the configuration for the Foundry provider.
/// </summary>
/// <remarks>
/// <para>
/// This class contains configuration settings specific to Foundry AI services.
/// Foundry is a custom or third-party AI provider that implements an OpenAI-compatible
/// API interface, allowing for alternative AI service providers.
/// </para>
/// <para>
/// The API key is not stored in this configuration for security reasons.
/// Instead, it should be provided via the FOUNDRY_API_KEY environment variable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var foundryConfig = new FoundryProviderConfig
/// {
///     Endpoint = "http://localhost:8000/v1/chat/completions",
///     Model = "foundry-llm-model-name"
/// };
/// </code>
/// </example>
public class FoundryProviderConfig
{
    /// <summary>
    /// Gets or sets the endpoint URL for the Foundry API.
    /// </summary>
    /// <value>
    /// The complete URL endpoint for the Foundry AI service, typically including
    /// the full path to the chat completions endpoint.
    /// </value>
    /// <remarks>
    /// This endpoint should point to a service that implements an OpenAI-compatible
    /// API interface. This allows for custom or self-hosted AI services to be used
    /// as alternatives to OpenAI or Azure OpenAI.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new FoundryProviderConfig
    /// {
    ///     Endpoint = "http://localhost:8000/v1/chat/completions"
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the model name for the Foundry API.
    /// </summary>
    /// <value>
    /// The name of the AI model available through the Foundry service.
    /// This depends on what models are available in the specific Foundry implementation.
    /// </value>
    /// <remarks>
    /// The model name should correspond to a model that is available through the
    /// configured Foundry endpoint. Different Foundry implementations may expose
    /// different models with varying capabilities.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new FoundryProviderConfig
    /// {
    ///     Model = "foundry-llm-model-name"
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
