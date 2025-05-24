using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace NotebookAutomation.Core.Configuration
{    /// <summary>
    /// OpenAI API configuration section.
    /// </summary>
    public class AIServiceConfig
    {
        /// <summary>
        /// The default OpenAI API endpoint.
        /// </summary>
        public const string DefaultApiEndpoint = "https://api.openai.com/v1/";

        /// <summary>
        /// Environment variable name for the OpenAI API key.
        /// </summary>
        public const string AiApiKeyEnvVar = "OPENAI_API_KEY";
        
        private IConfiguration? _configuration;
        
        /// <summary>
        /// Sets the configuration for accessing user secrets
        /// </summary>
        /// <param name="configuration">The IConfiguration instance</param>
        public void SetConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        /// <summary>
        /// Gets the API key from user secrets, then falls back to environment variables.
        /// This keeps the API key out of config files for security.
        /// </summary>
        public string? GetApiKey()
        {
            // First try user secrets if configuration is available
            if (_configuration != null)
            {
                var secretKey = _configuration["UserSecrets:OpenAI:ApiKey"];
                if (!string.IsNullOrEmpty(secretKey))
                {
                    return secretKey;
                }
            }
            
            // Then try environment variable
            return Environment.GetEnvironmentVariable(AiApiKeyEnvVar);
        }

        /// <summary>
        /// The OpenAI model to use (e.g., gpt-4, gpt-3.5-turbo).
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// The deployment name for Azure OpenAI. This is used when the OpenAI API is hosted on Azure. 
        /// </summary>
        [JsonPropertyName("deployment")]
        public string? Deployment { get; set; }

        /// <summary>
        /// The OpenAI API endpoint URL. If not specified, the default OpenAI API endpoint will be used.
        /// This can be set to use Azure OpenAI or other compatible API endpoints.
        /// </summary>
        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; set; }
        
        /// <summary>
        /// Gets the effective endpoint to use, defaulting to the standard OpenAI API if not specified.
        /// </summary>
        public string GetEffectiveEndpoint() => !string.IsNullOrEmpty(Endpoint) ? Endpoint : DefaultApiEndpoint;
    }
}
