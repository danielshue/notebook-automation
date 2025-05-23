namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// OpenAI API configuration section.
    /// </summary>
    using System.Text.Json.Serialization;

    public class OpenAiConfig
    {
        /// <summary>
        /// The environment variable name for the OpenAI API key.
        /// </summary>
        public const string OpenAiApiKeyEnvVar = "OPENAI_API_KEY";

        /// <summary>
        /// The OpenAI API key is always sourced from the environment variable OPENAI_API_KEY for security.
        /// This property is retained for serialization compatibility but should not be used at runtime.
        /// </summary>
        [JsonPropertyName("api_key")]
        public string ApiKey
        {
            get => Environment.GetEnvironmentVariable(OpenAiApiKeyEnvVar) ?? string.Empty;
            set { Environment.SetEnvironmentVariable(OpenAiApiKeyEnvVar, value); }
        }

        /// <summary>
        /// The OpenAI model to use (e.g., gpt-4, gpt-3.5-turbo).
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }
}
