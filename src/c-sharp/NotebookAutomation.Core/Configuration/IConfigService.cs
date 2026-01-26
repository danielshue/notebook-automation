// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Service interface for configuration management operations.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IConfigService"/> provides a high-level API for viewing, updating,
/// and validating application configuration. It is designed for Copilot tool integration
/// and exposes configuration through simplified data transfer objects.
/// </para>
/// <para>
/// Configuration keys use dot notation for nested values:
/// <list type="bullet">
/// <item><description><c>paths.onedrive_fullpath_root</c> - OneDrive local sync folder</description></item>
/// <item><description><c>paths.notebook_vault_fullpath_root</c> - Obsidian vault root</description></item>
/// <item><description><c>aiservice.provider</c> - AI provider (openai, azure, foundry)</description></item>
/// <item><description><c>aiservice.openai.model</c> - OpenAI model name</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IConfigService
{
    /// <summary>
    /// Gets the current configuration as a structured view.
    /// </summary>
    /// <returns>A <see cref="ConfigView"/> containing all configuration sections.</returns>
    /// <remarks>
    /// <para>
    /// Returns a read-only view of the current configuration. Sensitive values
    /// like API keys are masked for security.
    /// </para>
    /// </remarks>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// config_view()
    /// // Returns: { Paths: { NotebookVaultRoot: "D:\Vault", ... }, AiService: { Provider: "openai", ... } }
    /// </code>
    /// </example>
    ConfigView GetCurrentConfig();

    /// <summary>
    /// Gets a specific configuration value by key.
    /// </summary>
    /// <param name="key">
    /// The configuration key using dot notation (e.g., "paths.onedrive_fullpath_root").
    /// </param>
    /// <returns>
    /// A <see cref="ConfigKeyValue"/> with the key, value, and description;
    /// or null if the key is not found.
    /// </returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// config_get("aiservice.provider")              // Returns: "openai"
    /// config_get("paths.notebook_vault_fullpath_root")  // Returns: "D:\Vault\MBA"
    /// </code>
    /// </example>
    ConfigKeyValue? GetConfigValue(string key);

    /// <summary>
    /// Updates a configuration value.
    /// </summary>
    /// <param name="key">The configuration key to update.</param>
    /// <param name="value">The new value to set.</param>
    /// <returns>A <see cref="ConfigUpdateResult"/> indicating success or failure.</returns>
    /// <remarks>
    /// <para>
    /// Updates are persisted to the configuration file. Some keys may require
    /// application restart to take effect. Invalid keys will return a failure result.
    /// </para>
    /// </remarks>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// config_update("aiservice.provider", "azure")
    /// config_update("paths.notebook_vault_fullpath_root", "D:\NewVault")
    /// config_update("video_extensions", ".mp4,.mov,.avi")
    /// </code>
    /// </example>
    ConfigUpdateResult UpdateConfig(string key, string value);

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="ConfigValidationSummary"/> with validation results.</returns>
    /// <remarks>
    /// <para>
    /// Checks that all required settings are present, paths exist,
    /// and values are valid. Returns errors and warnings for any issues found.
    /// </para>
    /// </remarks>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// config_validate()
    /// // Returns: { IsValid: true, Issues: [], Warnings: ["OneDrive path not set"] }
    /// </code>
    /// </example>
    Task<ConfigValidationSummary> ValidateConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available configuration keys with their descriptions.
    /// </summary>
    /// <returns>A list of <see cref="ConfigKeyInfo"/> describing each available key.</returns>
    /// <remarks>
    /// <para>
    /// Use this to discover what configuration options are available.
    /// Keys are grouped by category (paths, aiservice, microsoft_graph, etc.).
    /// </para>
    /// </remarks>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// config_list_keys()
    /// // Returns list of keys with descriptions and current values
    /// </code>
    /// </example>
    IReadOnlyList<ConfigKeyInfo> ListConfigKeys();

    /// <summary>
    /// Gets the status of user secrets without revealing their values.
    /// </summary>
    /// <returns>A <see cref="SecretsStatus"/> showing which secrets are configured.</returns>
    /// <remarks>
    /// <para>
    /// Checks for the presence of sensitive configuration like API keys
    /// without exposing the actual values. Useful for troubleshooting
    /// authentication and API access issues.
    /// </para>
    /// </remarks>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// config_secrets_status()
    /// // Returns: { HasOpenAiApiKey: true, HasAzureApiKey: false, ... }
    /// </code>
    /// </example>
    SecretsStatus GetSecretsStatus();

    /// <summary>
    /// Gets the path to the active configuration file.
    /// </summary>
    /// <returns>The full path to the configuration file being used.</returns>
    string GetConfigFilePath();
}

/// <summary>
/// A read-only view of the current configuration.
/// </summary>
public record ConfigView
{
    /// <summary>Gets the configuration file path.</summary>
    public required string ConfigFilePath { get; init; }

    /// <summary>Gets the paths configuration.</summary>
    public required ConfigPathsView Paths { get; init; }

    /// <summary>Gets the AI service configuration.</summary>
    public required ConfigAiServiceView AiService { get; init; }

    /// <summary>Gets the Microsoft Graph configuration.</summary>
    public ConfigMicrosoftGraphView? MicrosoftGraph { get; init; }

    /// <summary>Gets the video file extensions as comma-separated string.</summary>
    public string? VideoExtensions { get; init; }

    /// <summary>Gets the PDF file extensions as comma-separated string.</summary>
    public string? PdfExtensions { get; init; }
}

/// <summary>View of paths configuration.</summary>
public record ConfigPathsView
{
    /// <summary>Obsidian vault root path.</summary>
    public string? NotebookVaultRoot { get; init; }

    /// <summary>Vault resources base path within the vault.</summary>
    public string? NotebookVaultResourcesBase { get; init; }

    /// <summary>OneDrive local sync folder path.</summary>
    public string? OnedriveRoot { get; init; }

    /// <summary>OneDrive resources base path within the sync folder.</summary>
    public string? OnedriveResourcesBase { get; init; }

    /// <summary>Metadata schema file path.</summary>
    public string? MetadataSchemaFile { get; init; }

    /// <summary>Prompts directory path.</summary>
    public string? PromptsPath { get; init; }

    /// <summary>Logging directory path.</summary>
    public string? LoggingDir { get; init; }
}

/// <summary>View of AI service configuration.</summary>
public record ConfigAiServiceView
{
    /// <summary>Selected AI provider (openai, azure, foundry).</summary>
    public string? Provider { get; init; }

    /// <summary>Model name for the selected provider.</summary>
    public string? Model { get; init; }

    /// <summary>Endpoint URL for the selected provider.</summary>
    public string? Endpoint { get; init; }

    /// <summary>Whether an API key is configured (true/false, not the actual key).</summary>
    public bool HasApiKey { get; init; }

    /// <summary>Azure deployment name (if using Azure provider).</summary>
    public string? AzureDeployment { get; init; }
}

/// <summary>View of Microsoft Graph configuration.</summary>
public record ConfigMicrosoftGraphView
{
    /// <summary>Microsoft Graph tenant ID.</summary>
    public string? TenantId { get; init; }

    /// <summary>Microsoft Graph client ID.</summary>
    public string? ClientId { get; init; }

    /// <summary>Whether client secret is configured.</summary>
    public bool HasClientSecret { get; init; }

    /// <summary>Microsoft Graph authority URL.</summary>
    public string? Authority { get; init; }

    /// <summary>Microsoft Graph API endpoint.</summary>
    public string? ApiEndpoint { get; init; }

    /// <summary>Required scopes.</summary>
    public IReadOnlyList<string>? Scopes { get; init; }

    /// <summary>Whether OneDrive token is valid.</summary>
    public bool? OneDriveTokenValid { get; init; }
}

/// <summary>
/// Configuration key-value with metadata.
/// </summary>
/// <param name="Key">The configuration key.</param>
/// <param name="Value">The current value (sensitive values are masked).</param>
/// <param name="Description">Description of the setting.</param>
public record ConfigKeyValue(string Key, string? Value, string Description);

/// <summary>
/// Result of a configuration update operation.
/// </summary>
/// <param name="Success">Whether the update succeeded.</param>
/// <param name="Key">The key that was updated.</param>
/// <param name="OldValue">The previous value (masked if sensitive).</param>
/// <param name="NewValue">The new value (masked if sensitive).</param>
/// <param name="RequiresRestart">Whether a restart is required for changes to take effect.</param>
/// <param name="ErrorMessage">Error message if update failed.</param>
public record ConfigUpdateResult(
    bool Success,
    string Key,
    string? OldValue,
    string? NewValue,
    bool RequiresRestart,
    string? ErrorMessage);

/// <summary>
/// Information about an available configuration key.
/// </summary>
/// <param name="Key">The configuration key.</param>
/// <param name="Description">Description of the setting.</param>
/// <param name="Category">Category grouping (paths, aiservice, microsoft_graph, etc.).</param>
/// <param name="IsSensitive">Whether the value is sensitive (API keys, etc.).</param>
/// <param name="CurrentValue">Current value of the setting (masked if sensitive).</param>
public record ConfigKeyInfo(
    string Key,
    string Description,
    string Category,
    bool IsSensitive,
    string? CurrentValue);

/// <summary>
/// Summary of configuration validation.
/// </summary>
/// <param name="IsValid">Whether configuration is valid (no errors).</param>
/// <param name="Issues">List of validation issues (errors).</param>
/// <param name="Warnings">List of validation warnings.</param>
public record ConfigValidationSummary(
    bool IsValid,
    IReadOnlyList<ConfigValidationIssue> Issues,
    IReadOnlyList<string> Warnings);

/// <summary>
/// A validation issue (error or warning).
/// </summary>
/// <param name="Key">Configuration key with the issue.</param>
/// <param name="Severity">Severity of the issue (Error or Warning).</param>
/// <param name="Message">Description of the issue.</param>
public record ConfigValidationIssue(
    string Key,
    string Severity,
    string Message);

/// <summary>
/// Status of user secrets configuration.
/// </summary>
public record SecretsStatus
{
    /// <summary>Whether the secrets file exists.</summary>
    public bool SecretsFileExists { get; init; }

    /// <summary>Path to the secrets file.</summary>
    public string? SecretsFilePath { get; init; }

    /// <summary>Whether OpenAI API key is set.</summary>
    public bool HasOpenAiApiKey { get; init; }

    /// <summary>Whether Azure API key is set.</summary>
    public bool HasAzureApiKey { get; init; }

    /// <summary>Whether Azure endpoint is set.</summary>
    public bool HasAzureEndpoint { get; init; }

    /// <summary>Whether Microsoft Graph client secret is set.</summary>
    public bool HasGraphClientSecret { get; init; }

    /// <summary>Whether Foundry API key is set.</summary>
    public bool HasFoundryApiKey { get; init; }
}
