using Microsoft.Extensions.Configuration;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Provides convenient access to user secrets in the application.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the UserSecretsHelper class.
    /// </remarks>
    /// <param name="configuration">The configuration to use for accessing user secrets.</param>
    public class UserSecretsHelper(IConfiguration configuration)
    {
        private readonly IConfiguration _configuration = configuration;

        /// <summary>
        /// Gets an OpenAI API key from user secrets, if available.
        /// </summary>
        /// <returns>The API key if found in user secrets; otherwise, null.</returns>
        public string? GetOpenAIApiKey()
        {
            return _configuration["UserSecrets:OpenAI:ApiKey"];
        }

        /// <summary>
        /// Gets a Microsoft Graph client ID from user secrets, if available.
        /// </summary>
        /// <returns>The client ID if found in user secrets; otherwise, null.</returns>
        public string? GetMicrosoftGraphClientId()
        {
            return _configuration["UserSecrets:Microsoft:ClientId"];
        }

        /// <summary>
        /// Gets a Microsoft Graph tenant ID from user secrets, if available.
        /// </summary>
        /// <returns>The tenant ID if found in user secrets; otherwise, null.</returns>
        public string? GetMicrosoftGraphTenantId()
        {
            return _configuration["UserSecrets:Microsoft:TenantId"];
        }

        /// <summary>
        /// Gets any user secret by key.
        /// </summary>
        /// <param name="key">The key of the user secret to get.</param>
        /// <returns>The value if found; otherwise, null.</returns>
        public string? GetSecret(string key)
        {
            return _configuration[$"UserSecrets:{key}"];
        }

        /// <summary>
        /// Determines whether a specific user secret exists.
        /// </summary>
        /// <param name="key">The key of the user secret to check.</param>
        /// <returns>True if the user secret exists; otherwise, false.</returns>
        public bool HasSecret(string key)
        {
            return !string.IsNullOrEmpty(_configuration[$"UserSecrets:{key}"]);
        }
    }
}
