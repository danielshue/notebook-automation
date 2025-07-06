// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

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
