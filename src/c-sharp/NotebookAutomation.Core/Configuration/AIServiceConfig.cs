// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the configuration for AI services, supporting multiple providers such as OpenAI, Azure, and Foundry.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a unified configuration interface for multiple AI service providers.
/// The configuration supports switching between different providers through the Provider property,
/// with each provider having its own specific configuration section.
/// </para>
/// <para>
/// API keys are not stored in configuration files for security reasons. Use environment variables or user-secrets:
/// </para>
/// <list type="bullet">
/// <item><description>OpenAI: OPENAI_API_KEY</description></item>
/// <item><description>Azure: AZURE_OPENAI_KEY</description></item>
/// <item><description>Foundry: FOUNDRY_API_KEY</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var config = new AIServiceConfig
/// {
///     Provider = "azure",
///     Azure = new AzureProviderConfig
///     {
///         Endpoint = "https://your-resource.cognitiveservices.azure.com/",
///         Deployment = "gpt-4o",
///         Model = "gpt-4o"
///     }
/// };
///
/// // API key is retrieved from environment variable
/// string? apiKey = config.GetApiKey();
/// </code>
/// </example>
public class AIServiceConfig
{    /// <summary>
     /// Gets or sets the provider name (e.g., OpenAI, Azure, Foundry).
     /// </summary>
     /// <value>
     /// A string indicating which AI provider to use. Valid values are "openai", "azure", and "foundry".
     /// Defaults to "openai" if not specified.
     /// </value>
     /// <remarks>
     /// The provider name is case-insensitive and determines which configuration section
     /// and environment variable will be used for API key retrieval.
     /// </remarks>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the configuration for OpenAI provider.
    /// </summary>
    /// <value>
    /// An <see cref="OpenAiProviderConfig"/> object containing OpenAI-specific settings,
    /// or <c>null</c> if OpenAI provider is not configured.
    /// </value>
    /// <remarks>
    /// This configuration is used when the Provider is set to "openai".
    /// Contains settings such as endpoint URL and model name.
    /// </remarks>
    [JsonPropertyName("openai")]
    public OpenAiProviderConfig? OpenAI { get; set; }

    /// <summary>
    /// Gets or sets the configuration for Azure provider.
    /// </summary>
    /// <value>
    /// An <see cref="AzureProviderConfig"/> object containing Azure OpenAI-specific settings,
    /// or <c>null</c> if Azure provider is not configured.
    /// </value>
    /// <remarks>
    /// This configuration is used when the Provider is set to "azure".
    /// Contains settings such as endpoint URL, deployment name, and model name.
    /// </remarks>
    [JsonPropertyName("azure")]
    public AzureProviderConfig? Azure { get; set; }

    /// <summary>
    /// Gets or sets the configuration for Foundry provider.
    /// </summary>
    /// <value>
    /// A <see cref="FoundryProviderConfig"/> object containing Foundry-specific settings,
    /// or <c>null</c> if Foundry provider is not configured.
    /// </value>
    /// <remarks>
    /// This configuration is used when the Provider is set to "foundry".
    /// Contains settings such as endpoint URL and model name.
    /// </remarks>
    [JsonPropertyName("foundry")]
    public FoundryProviderConfig? Foundry { get; set; }


    /// <summary>
    /// Gets or sets the timeout configuration for AI requests.
    /// </summary>
    /// <value>
    /// A <see cref="TimeoutConfig"/> object containing timeout and retry settings,
    /// or <c>null</c> to use default timeout values.
    /// </value>
    /// <remarks>
    /// This configuration controls how long AI requests wait before timing out
    /// and how many retry attempts are made for failed requests.
    /// </remarks>
    [JsonPropertyName("timeout")]
    public TimeoutConfig? Timeout { get; set; }


    /// <summary>
    /// Returns the API key for the configured AI provider.
    /// </summary>
    /// <returns>
    /// The API key string retrieved from the appropriate environment variable,
    /// or <c>null</c> if the environment variable is not set or the provider is not recognized.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retrieves the API key from environment variables based on the current provider:
    /// </para>
    /// <list type="bullet">
    /// <item><description>OpenAI: Reads from OPENAI_API_KEY environment variable</description></item>
    /// <item><description>Azure: Reads from AZURE_OPENAI_KEY environment variable</description></item>
    /// <item><description>Foundry: Reads from FOUNDRY_API_KEY environment variable</description></item>
    /// </list>
    /// <para>
    /// The provider name comparison is case-insensitive. If no provider is specified,
    /// it defaults to "openai".
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AIServiceConfig { Provider = "azure" };
    ///
    /// // This will read from AZURE_OPENAI_KEY environment variable
    /// string? apiKey = config.GetApiKey();
    ///
    /// if (apiKey != null)
    /// {
    ///     // Use the API key for Azure OpenAI service
    ///     Console.WriteLine("Azure OpenAI API key configured");
    /// }
    /// </code>
    /// </example>
    public string? GetApiKey()
    {
        var providerType = Provider?.ToLowerInvariant() ?? "openai";
        return providerType switch
        {
            "openai" => Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            "azure" => Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY"),
            "foundry" => Environment.GetEnvironmentVariable("FOUNDRY_API_KEY"),
            _ => null,
        };
    }

    /// <summary>
    /// Gets the model for the active provider.
    /// </summary>
    /// <value>
    /// The model name for the currently configured provider, or <c>null</c> if not configured.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides convenient access to the model name based on the current provider.
    /// It automatically selects the appropriate model from the corresponding provider configuration.
    /// </para>
    /// <para>
    /// This allows direct access via indexer like appConfig["aiservice:Model"].
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AIServiceConfig
    /// {
    ///     Provider = "azure",
    ///     Azure = new AzureProviderConfig { Model = "gpt-4o" }
    /// };
    ///
    /// string? model = config.Model; // Returns "gpt-4o"
    /// </code>
    /// </example>
    [JsonIgnore]
    public string? Model
    {
        get
        {
            var providerType = Provider?.ToLowerInvariant() ?? "openai";
            return providerType switch
            {
                "openai" => OpenAI?.Model,
                "azure" => Azure?.Model,
                "foundry" => Foundry?.Model,
                _ => null,
            };
        }
    }

    /// <summary>
    /// Gets the API key for the active provider.
    /// </summary>
    /// <value>
    /// The API key string retrieved from the appropriate environment variable,
    /// or <c>null</c> if not configured.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides convenient access to the API key and delegates to the
    /// <see cref="GetApiKey"/> method. It's marked with <see cref="JsonIgnoreAttribute"/>
    /// to prevent the API key from being serialized in JSON output for security reasons.
    /// </para>
    /// <para>
    /// This allows direct access via indexer like appConfig["aiservice:api_key"].
    /// </para>
    /// </remarks>
    [JsonPropertyName("api_key")]
    [JsonIgnore]
    public string? ApiKey => GetApiKey();
}