// <copyright file="AIServiceConfig.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Configuration/AIServiceConfig.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using System.Text.Json.Serialization;

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the configuration for AI services, supporting multiple providers such as OpenAI, Azure, and Foundry.
/// </summary>
/// <remarks>
/// API keys are not stored in configuration files. Use environment variables or user-secrets:
/// <list type="bullet">
/// <item><description>OpenAI: OPENAI_API_KEY</description></item>
/// <item><description>Azure: AZURE_OPEN_AI_API_KEY</description></item>
/// <item><description>Foundry: FOUNDRY_API_KEY</description></item>
/// </list>
/// </remarks>
public class AIServiceConfig
{
    /// <summary>
    /// Gets or sets the provider name (e.g., OpenAI, Azure, Foundry).
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    /// <summary>
    /// Gets or sets the configuration for OpenAI provider.
    /// </summary>
    [JsonPropertyName("openai")]
    public OpenAiProviderConfig? OpenAI { get; set; }

    /// <summary>
    /// Gets or sets the configuration for Azure provider.
    /// </summary>
    [JsonPropertyName("azure")]
    public AzureProviderConfig? Azure { get; set; }

    /// <summary>
    /// Gets or sets the configuration for Foundry provider.
    /// </summary>
    [JsonPropertyName("foundry")]
    public FoundryProviderConfig? Foundry { get; set; }

    /// <summary>
    /// Returns the API key for the configured AI provider.
    /// </summary>
    /// <returns>The API key string, or null if not set.</returns>
    public string? GetApiKey()
    {
        var providerType = this.Provider?.ToLowerInvariant() ?? "openai";
        return providerType switch
        {
            "openai" => Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            "azure" => Environment.GetEnvironmentVariable("AZURE_OPEN_AI_API_KEY"),
            "foundry" => Environment.GetEnvironmentVariable("FOUNDRY_API_KEY"),
            _ => null,
        };
    }

    /// <summary>
    /// Gets the model for the active provider.
    /// </summary>
    /// <remarks>
    /// This allows direct access via indexer like appConfig["aiservice:Model"].
    /// </remarks>
    [JsonIgnore]
    public string? Model
    {
        get
        {
            var providerType = this.Provider?.ToLowerInvariant() ?? "openai";
            return providerType switch
            {
                "openai" => this.OpenAI?.Model,
                "azure" => this.Azure?.Model,
                "foundry" => this.Foundry?.Model,
                _ => null,
            };
        }
    }

    /// <summary>
    /// Gets the API key for the active provider.
    /// </summary>
    /// <remarks>
    /// This allows direct access via indexer like appConfig["aiservice:api_key"].
    /// </remarks>
    [JsonPropertyName("api_key")]
    [JsonIgnore]
    public string? ApiKey => this.GetApiKey();
}

/// <summary>
/// Represents the configuration for the OpenAI provider.
/// </summary>
public class OpenAiProviderConfig
{
    /// <summary>
    /// Gets or sets the endpoint URL for the OpenAI API.
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the model name for the OpenAI API.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

/// <summary>
/// Represents the configuration for the Azure provider.
/// </summary>
public class AzureProviderConfig
{
    /// <summary>
    /// Gets or sets the endpoint URL for the Azure OpenAI API.
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the deployment name for the Azure OpenAI API.
    /// </summary>
    [JsonPropertyName("deployment")]
    public string? Deployment { get; set; }

    /// <summary>
    /// Gets or sets the model name for the Azure OpenAI API.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

/// <summary>
/// Represents the configuration for the Foundry provider.
/// </summary>
public class FoundryProviderConfig
{
    /// <summary>
    /// Gets or sets the endpoint URL for the Foundry API.
    /// </summary>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the model name for the Foundry API.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
