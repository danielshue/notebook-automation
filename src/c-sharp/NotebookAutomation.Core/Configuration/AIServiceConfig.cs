using System.Text.Json.Serialization;

namespace NotebookAutomation.Core.Configuration
{    /// <summary>
     /// OpenAI API configuration section.
     /// </summary>
     /// <summary>
     /// AIServiceConfig supports multiple providers (OpenAI, Azure, Foundry).
     /// API keys are NOT stored in config files. Use environment variables or user-secrets:
     ///   - OpenAI:   OPENAI_API_KEY
     ///   - Azure:    AZURE_OPEN_AI_API_KEY
     ///   - Foundry:  FOUNDRY_API_KEY (if required)
     /// </summary>
    public class AIServiceConfig
    {
        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("openai")]
        public OpenAiProviderConfig? OpenAI { get; set; }

        [JsonPropertyName("azure")]
        public AzureProviderConfig? Azure { get; set; }

        [JsonPropertyName("foundry")]
        public FoundryProviderConfig? Foundry { get; set; }        /// <summary>
                                                                   /// Returns the API key for the configured AI provider.
                                                                   /// </summary>
                                                                   /// <returns>The API key string, or null if not set.</returns>
        public string? GetApiKey()
        {
            var providerType = Provider?.ToLowerInvariant() ?? "openai";
            return providerType switch
            {
                "openai" => Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
                "azure" => Environment.GetEnvironmentVariable("AZURE_OPEN_AI_API_KEY"),
                "foundry" => Environment.GetEnvironmentVariable("FOUNDRY_API_KEY"),
                _ => null
            };
        }

        /// <summary>
        /// Convenience property to get the model for the active provider.
        /// This allows direct access via indexer like appConfig["aiservice:Model"]
        /// </summary>
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
                    _ => null
                };
            }
        }

        /// <summary>
        /// Convenience property to get the API key for the active provider.
        /// This allows direct access via indexer like appConfig["aiservice:api_key"]
        /// </summary>
        [JsonPropertyName("api_key")]
        [JsonIgnore]
        public string? ApiKey => GetApiKey();
    }

    public class OpenAiProviderConfig
    {
        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }

    public class AzureProviderConfig
    {
        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }
        [JsonPropertyName("deployment")]
        public string? Deployment { get; set; }
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }

    public class FoundryProviderConfig
    {
        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }
}
