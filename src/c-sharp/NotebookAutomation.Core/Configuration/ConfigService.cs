// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Configuration.Validation;

namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Service for configuration management operations.
/// </summary>
/// <remarks>
/// This service wraps <see cref="AppConfig"/>, <see cref="UserSecretsHelper"/>,
/// and <see cref="IConfigurationValidationService"/> to provide a unified API
/// for Copilot tool integration.
/// </remarks>
/// <param name="config">The application configuration.</param>
/// <param name="userSecrets">The user secrets helper.</param>
/// <param name="validationService">The configuration validation service.</param>
/// <param name="logger">The logger instance.</param>
public class ConfigService(
    AppConfig config,
    UserSecretsHelper userSecrets,
    IConfigurationValidationService validationService,
    ILogger<ConfigService> logger) : IConfigService
{
    /// <summary>
    /// Configuration key definitions with metadata.
    /// </summary>
    private static readonly Dictionary<string, ConfigKeyDefinition> KeyDefinitions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Paths
        ["paths.notebook_vault_fullpath_root"] = new("paths", "Root directory for the Obsidian vault", false),
        ["paths.notebook_vault_resources_basepath"] = new("paths", "Base path within vault for resources", false),
        ["paths.onedrive_fullpath_root"] = new("paths", "Root directory for OneDrive local sync", false),
        ["paths.onedrive_resources_basepath"] = new("paths", "Base path within OneDrive for resources", false),
        ["paths.metadata_schema_file"] = new("paths", "Path to metadata schema YAML file", false),
        ["paths.prompts_path"] = new("paths", "Directory containing prompt templates", false),
        ["paths.logging_dir"] = new("paths", "Directory for log files", false),
        ["paths.base_block_template_filename"] = new("paths", "Filename of the base block template", false),

        // AI Service
        ["aiservice.provider"] = new("aiservice", "AI provider (openai, azure, foundry)", false),
        ["aiservice.openai.model"] = new("aiservice", "OpenAI model name", false),
        ["aiservice.openai.endpoint"] = new("aiservice", "OpenAI API endpoint", false),
        ["aiservice.azure.model"] = new("aiservice", "Azure OpenAI model name", false),
        ["aiservice.azure.deployment"] = new("aiservice", "Azure OpenAI deployment name", false),
        ["aiservice.azure.endpoint"] = new("aiservice", "Azure OpenAI endpoint URL", false),
        ["aiservice.foundry.model"] = new("aiservice", "Azure AI Foundry model name", false),
        ["aiservice.foundry.endpoint"] = new("aiservice", "Azure AI Foundry endpoint URL", false),

        // Microsoft Graph
        ["microsoft_graph.client_id"] = new("microsoft_graph", "Microsoft Graph application client ID", false),
        ["microsoft_graph.tenant_id"] = new("microsoft_graph", "Microsoft Graph tenant ID", false),
        ["microsoft_graph.authority"] = new("microsoft_graph", "Microsoft Graph authority URL", false),
        ["microsoft_graph.api_endpoint"] = new("microsoft_graph", "Microsoft Graph API endpoint", false),

        // Extensions
        ["video_extensions"] = new("extensions", "Video file extensions to process (comma-separated)", false),
        ["pdf_extensions"] = new("extensions", "PDF file extensions to process (comma-separated)", false),
        ["html_extensions"] = new("extensions", "HTML file extensions to process (comma-separated)", false),

        // Copilot
        ["copilot.enabled"] = new("copilot", "Whether Copilot integration is enabled", false),
        ["copilot.defaultModel"] = new("copilot", "Default Copilot model to use", false),
        ["copilot.autoChatMode"] = new("copilot", "Automatically enter chat mode on startup", false),
        ["copilot.showWelcomeBanner"] = new("copilot", "Show welcome banner on startup", false),
        ["copilot.enableStreaming"] = new("copilot", "Enable streaming responses", false),
        ["copilot.sessionRetentionDays"] = new("copilot", "Days to retain chat sessions", false),
        ["copilot.autoSaveSessions"] = new("copilot", "Automatically save chat sessions", false),
        ["copilot.maxSessions"] = new("copilot", "Maximum number of sessions to retain", false),
        ["copilot.highContrast"] = new("copilot", "Enable high contrast mode", false),
        ["copilot.logLevel"] = new("copilot", "Copilot logging level", false),
    };

    /// <summary>
    /// Sensitive keys that should be masked in output.
    /// </summary>
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "UserSecrets:OpenAI:ApiKey",
        "UserSecrets:Azure:ApiKey",
        "UserSecrets:Azure:Endpoint",
        "UserSecrets:Foundry:ApiKey",
        "UserSecrets:Microsoft:ClientSecret"
    };

    /// <inheritdoc />
    public ConfigView GetCurrentConfig()
    {
        logger.LogDebug("Getting current configuration view");

        return new ConfigView
        {
            ConfigFilePath = config.ConfigFilePath ?? "unknown",
            Paths = new ConfigPathsView
            {
                NotebookVaultRoot = config.Paths.NotebookVaultFullpathRoot,
                NotebookVaultResourcesBase = config.Paths.NotebookVaultResourcesBasepath,
                OnedriveRoot = config.Paths.OnedriveFullpathRoot,
                OnedriveResourcesBase = config.Paths.OnedriveResourcesBasepath,
                MetadataSchemaFile = config.Paths.MetadataSchemaFile,
                PromptsPath = config.Paths.PromptsPath,
                LoggingDir = config.Paths.LoggingDir
            },
            AiService = new ConfigAiServiceView
            {
                Provider = config.AiService.Provider,
                Model = GetActiveModel(),
                Endpoint = GetActiveEndpoint(),
                HasApiKey = HasActiveApiKey(),
                AzureDeployment = config.AiService.Azure?.Deployment
            },
            MicrosoftGraph = new ConfigMicrosoftGraphView
            {
                TenantId = config.MicrosoftGraph.TenantId,
                ClientId = config.MicrosoftGraph.ClientId,
                HasClientSecret = userSecrets.HasSecret("Microsoft:ClientSecret"),
                Authority = config.MicrosoftGraph.Authority,
                ApiEndpoint = config.MicrosoftGraph.ApiEndpoint,
                Scopes = config.MicrosoftGraph.Scopes?.ToList()
            },
            VideoExtensions = config.VideoExtensions != null ? string.Join(",", config.VideoExtensions) : null,
            PdfExtensions = config.PdfExtensions != null ? string.Join(",", config.PdfExtensions) : null
        };
    }

    /// <inheritdoc />
    public ConfigKeyValue? GetConfigValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        logger.LogDebug("Getting configuration value for key: {Key}", key);

        // Normalize key format (allow both dot and colon notation)
        var normalizedKey = key.Replace('.', ':');
        var value = config[normalizedKey];

        if (value == null)
        {
            // Try dot notation directly
            value = config[key];
        }

        if (value == null)
        {
            return null;
        }

        var description = KeyDefinitions.TryGetValue(key, out var def)
            ? def.Description
            : "Configuration setting";

        // Mask sensitive values
        var displayValue = IsSensitiveKey(key) ? "***masked***" : value;

        return new ConfigKeyValue(key, displayValue, description);
    }

    /// <inheritdoc />
    public ConfigUpdateResult UpdateConfig(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ConfigUpdateResult(false, key, null, value, false, "Key cannot be empty");
        }

        logger.LogInformation("Updating configuration: {Key}", key);

        try
        {
            // Get current value for comparison
            var normalizedKey = key.Replace('.', ':');
            var oldValue = config[normalizedKey];
            var displayOldValue = IsSensitiveKey(key) ? "***masked***" : oldValue;

            // Determine if this is a known key
            if (!KeyDefinitions.ContainsKey(key) && !config.Exists(normalizedKey))
            {
                return new ConfigUpdateResult(false, key, null, value, false, $"Unknown configuration key: {key}");
            }

            // Handle special cases for list values
            if (key.Equals("video_extensions", StringComparison.OrdinalIgnoreCase))
            {
                config.SetVideoExtensions(ParseExtensionList(value));
            }
            else if (key.Equals("pdf_extensions", StringComparison.OrdinalIgnoreCase))
            {
                config.SetPdfExtensions(ParseExtensionList(value));
            }
            else
            {
                // Standard key update
                config[normalizedKey] = value;
            }

            // Save to file if config path is known
            if (!string.IsNullOrWhiteSpace(config.ConfigFilePath))
            {
                config.SaveToJsonFile(config.ConfigFilePath);
            }

            var displayNewValue = IsSensitiveKey(key) ? "***masked***" : value;
            var requiresRestart = IsRestartRequired(key);

            logger.LogInformation("Configuration updated: {Key} = {Value}", key, displayNewValue);

            return new ConfigUpdateResult(
                true,
                key,
                displayOldValue,
                displayNewValue,
                requiresRestart,
                null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update configuration key: {Key}", key);
            return new ConfigUpdateResult(false, key, null, value, false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<ConfigValidationSummary> ValidateConfigAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Validating configuration");

        try
        {
            var result = await validationService.ValidateAsync(cancellationToken);

            var issues = result.Issues
                .Where(i => i.Severity == ConfigurationValidationIssueSeverity.Error)
                .Select(i => new ConfigValidationIssue(i.Key, "Error", i.Message))
                .ToList();

            var warnings = result.Issues
                .Where(i => i.Severity == ConfigurationValidationIssueSeverity.Warning)
                .Select(i => i.Message)
                .ToList();

            return new ConfigValidationSummary(result.IsValid, issues, warnings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Configuration validation failed with exception");
            return new ConfigValidationSummary(
                false,
                [new ConfigValidationIssue("unknown", "Error", ex.Message)],
                []);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ConfigKeyInfo> ListConfigKeys()
    {
        logger.LogDebug("Listing all configuration keys");

        var keys = new List<ConfigKeyInfo>();

        foreach (var (key, def) in KeyDefinitions)
        {
            var normalizedKey = key.Replace('.', ':');
            var value = config[normalizedKey];
            var isSensitive = IsSensitiveKey(key);
            var displayValue = isSensitive ? "***masked***" : value;

            keys.Add(new ConfigKeyInfo(
                key,
                def.Description,
                def.Category,
                isSensitive,
                displayValue));
        }

        return keys.OrderBy(k => k.Category).ThenBy(k => k.Key).ToList();
    }

    /// <inheritdoc />
    public SecretsStatus GetSecretsStatus()
    {
        logger.LogDebug("Checking secrets status");

        // Try to find secrets file path
        string? secretsFilePath = null;
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(userProfile))
        {
            var possiblePath = Path.Combine(userProfile, "Microsoft", "UserSecrets", "notebook-automation", "secrets.json");
            if (File.Exists(possiblePath))
            {
                secretsFilePath = possiblePath;
            }
        }

        return new SecretsStatus
        {
            SecretsFileExists = !string.IsNullOrEmpty(secretsFilePath) && File.Exists(secretsFilePath),
            SecretsFilePath = secretsFilePath,
            HasOpenAiApiKey = userSecrets.HasSecret("OpenAI:ApiKey"),
            HasAzureApiKey = userSecrets.HasSecret("Azure:ApiKey"),
            HasAzureEndpoint = userSecrets.HasSecret("Azure:Endpoint"),
            HasGraphClientSecret = userSecrets.HasSecret("Microsoft:ClientSecret"),
            HasFoundryApiKey = userSecrets.HasSecret("Foundry:ApiKey")
        };
    }

    /// <inheritdoc />
    public string GetConfigFilePath()
    {
        return config.ConfigFilePath ?? "unknown";
    }

    /// <summary>
    /// Gets the model name for the active AI provider.
    /// </summary>
    private string? GetActiveModel()
    {
        return config.AiService.Provider?.ToLowerInvariant() switch
        {
            "openai" => config.AiService.OpenAI?.Model,
            "azure" => config.AiService.Azure?.Model,
            "foundry" => config.AiService.Foundry?.Model,
            _ => config.AiService.OpenAI?.Model // Default to OpenAI
        };
    }

    /// <summary>
    /// Gets the endpoint for the active AI provider.
    /// </summary>
    private string? GetActiveEndpoint()
    {
        return config.AiService.Provider?.ToLowerInvariant() switch
        {
            "openai" => config.AiService.OpenAI?.Endpoint,
            "azure" => config.AiService.Azure?.Endpoint,
            "foundry" => config.AiService.Foundry?.Endpoint,
            _ => config.AiService.OpenAI?.Endpoint // Default to OpenAI
        };
    }

    /// <summary>
    /// Checks if an API key is configured for the active provider.
    /// </summary>
    private bool HasActiveApiKey()
    {
        return config.AiService.Provider?.ToLowerInvariant() switch
        {
            "openai" => userSecrets.HasSecret("OpenAI:ApiKey"),
            "azure" => userSecrets.HasSecret("Azure:ApiKey"),
            "foundry" => userSecrets.HasSecret("Foundry:ApiKey"),
            _ => userSecrets.HasSecret("OpenAI:ApiKey") // Default to OpenAI
        };
    }

    /// <summary>
    /// Determines if a key contains sensitive data.
    /// </summary>
    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace('.', ':');
        return SensitiveKeys.Contains(normalized) ||
               key.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
               key.Contains("Password", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if changing a key requires an application restart.
    /// </summary>
    private static bool IsRestartRequired(string key)
    {
        // Keys that require restart
        var restartKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "aiservice.provider",
            "copilot.enabled",
            "paths.logging_dir"
        };

        return restartKeys.Contains(key);
    }

    /// <summary>
    /// Parses a comma-separated list of extensions.
    /// </summary>
    private static List<string> ParseExtensionList(string value)
    {
        return [.. value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrEmpty(e))
            .Select(e => e.StartsWith('.') ? e : $".{e}")];
    }

    /// <summary>
    /// Definition for a configuration key.
    /// </summary>
    private readonly record struct ConfigKeyDefinition(string Category, string Description, bool IsSensitive);
}
