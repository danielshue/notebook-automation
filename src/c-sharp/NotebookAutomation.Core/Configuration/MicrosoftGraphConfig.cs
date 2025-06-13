// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Configuration for Microsoft Graph API.
/// </summary>
/// <remarks>
/// This class encapsulates the necessary parameters for authenticating and accessing
/// Microsoft Graph API resources, including client credentials, API endpoints, and scopes.
/// It is designed to be serialized and deserialized from JSON configuration files.
/// </remarks>
public class MicrosoftGraphConfig
{
    /// <summary>
    /// Gets or sets client ID for authenticating with Microsoft Graph.
    /// </summary>
    /// <remarks>
    /// The Client ID is a unique identifier assigned to the application by Azure Active Directory.
    /// It is used during the authentication process to identify the application.
    /// </remarks>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets aPI endpoint for Microsoft Graph.
    /// </summary>
    /// <remarks>
    /// The API endpoint specifies the base URL for accessing Microsoft Graph services.
    /// Typically, this is "https://graph.microsoft.com/v1.0" for production environments.
    /// </remarks>
    [JsonPropertyName("api_endpoint")]
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets authority URL for Microsoft Graph authentication.
    /// </summary>
    /// <remarks>
    /// The authority URL is used to direct authentication requests to the appropriate Azure Active Directory.
    /// For example, "https://login.microsoftonline.com/common" for multi-tenant applications.
    /// </remarks>
    [JsonPropertyName("authority")]
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets scopes required for Microsoft Graph API access.
    /// </summary>
    /// <remarks>
    /// Scopes define the permissions that the application needs to access Microsoft Graph resources.
    /// Examples include "User.Read" and "Mail.Send".
    /// </remarks>
    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Gets or sets the tenant ID for Microsoft Graph authentication.
    /// </summary>
    /// <remarks>
    /// The tenant ID is used to identify the directory in Azure Active Directory
    /// that the application belongs to. It can be set to "common" for multi-tenant
    /// applications or a specific tenant ID for single-tenant applications.
    /// </remarks>
    public string? TenantId { get; set; }
}
