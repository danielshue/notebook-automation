/// <summary>
/// Configuration for Microsoft Graph API.
/// </summary>
using System.Text.Json.Serialization;

namespace NotebookAutomation.Core.Configuration;
public class MicrosoftGraphConfig
{
    /// <summary>
    /// Client ID for authenticating with Microsoft Graph.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint for Microsoft Graph.
    /// </summary>
    [JsonPropertyName("api_endpoint")]
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Authority URL for Microsoft Graph authentication.
    /// </summary>
    [JsonPropertyName("authority")]
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Scopes required for Microsoft Graph API access.
    /// </summary>
    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = [];


    public string? TenantId { get; set; }

}
