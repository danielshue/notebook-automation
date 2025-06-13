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

/// <summary>
/// Represents the configuration for the Azure OpenAI provider.
/// </summary>
/// <remarks>
/// <para>
/// This class contains configuration settings specific to Azure OpenAI services.
/// It includes endpoint URL, deployment name, and model name settings that are
/// used when the AI service provider is set to "azure".
/// </para>
/// <para>
/// Azure OpenAI requires a deployment name in addition to the model name, as
/// models are deployed to specific deployment slots within an Azure OpenAI resource.
/// </para>
/// <para>
/// The API key is not stored in this configuration for security reasons.
/// Instead, it should be provided via the AZURE_OPENAI_KEY environment variable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var azureConfig = new AzureProviderConfig
/// {
///     Endpoint = "https://your-resource.cognitiveservices.azure.com/",
///     Deployment = "gpt-4o-deployment",
///     Model = "gpt-4o"
/// };
/// </code>
/// </example>
public class AzureProviderConfig
{
    /// <summary>
    /// Gets or sets the endpoint URL for the Azure OpenAI API.
    /// </summary>
    /// <value>
    /// The complete URL endpoint for the Azure OpenAI service, typically in the format
    /// "https://your-resource.cognitiveservices.azure.com/".
    /// </value>
    /// <remarks>
    /// This endpoint is specific to your Azure OpenAI resource and can be found
    /// in the Azure portal under your OpenAI resource's "Keys and Endpoint" section.
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AzureProviderConfig
    /// {
    ///     Endpoint = "https://my-openai-resource.cognitiveservices.azure.com/"
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }
    /// <summary>
    /// Gets or sets the deployment name for the Azure OpenAI API.
    /// </summary>
    /// <value>
    /// The name of the specific deployment within the Azure OpenAI resource.
    /// This is the custom name you assigned when deploying a model in Azure.
    /// </value>
    /// <remarks>
    /// <para>
    /// In Azure OpenAI, models are deployed to named deployment slots. This property
    /// specifies which deployment to use for API requests. The deployment name is
    /// defined when you create a model deployment in the Azure portal.
    /// </para>
    /// <para>
    /// This is different from the model name - the deployment name is your custom
    /// identifier, while the model name refers to the underlying AI model.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AzureProviderConfig
    /// {
    ///     Deployment = "my-gpt4-deployment"
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("deployment")]
    public string? Deployment { get; set; }

    /// <summary>
    /// Gets or sets the model name for the Azure OpenAI API.
    /// </summary>
    /// <value>
    /// The name of the AI model deployed in Azure OpenAI, such as "gpt-4o", "gpt-4",
    /// "gpt-35-turbo", etc.
    /// </value>
    /// <remarks>
    /// <para>
    /// This should match the actual model type that was deployed to the deployment
    /// specified in the <see cref="Deployment"/> property. The model name affects
    /// the capabilities and cost of API requests.
    /// </para>
    /// <para>
    /// Note that Azure OpenAI may use slightly different naming conventions than
    /// the standard OpenAI API (e.g., "gpt-35-turbo" instead of "gpt-3.5-turbo").
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AzureProviderConfig
    /// {
    ///     Model = "gpt-4o"
    /// };
    /// </code>
    /// </example>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

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
