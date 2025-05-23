namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Configuration for Microsoft Graph API.
    /// </summary>
    public class MicrosoftGraphConfig
    {
        /// <summary>
        /// Client ID for authenticating with Microsoft Graph.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// API endpoint for Microsoft Graph.
        /// </summary>
        public string ApiEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Authority URL for Microsoft Graph authentication.
        /// </summary>
        public string Authority { get; set; } = string.Empty;

        /// <summary>
        /// Scopes required for Microsoft Graph API access.
        /// </summary>
        public List<string> Scopes { get; set; } = new List<string>();


        public string? TenantId { get; internal set; }
        
    }
}
