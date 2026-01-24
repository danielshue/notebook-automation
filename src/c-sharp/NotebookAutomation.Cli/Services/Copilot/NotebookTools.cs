// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;

using Microsoft.Extensions.AI;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Manages registration and access to Notebook Automation CLI tools for Copilot.
/// </summary>
public class NotebookTools : INotebookTools
{
    private readonly ILogger<NotebookTools> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly Dictionary<string, AIFunction> tools = new();
    private readonly Dictionary<string, List<string>> categorizedTools = new();
    private bool isRegistered = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotebookTools"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    public NotebookTools(
        ILogger<NotebookTools> logger,
        IServiceProvider serviceProvider)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public IReadOnlyList<AIFunction> GetAllTools()
    {
        if (!isRegistered)
        {
            RegisterAllTools();
        }

        return tools.Values.ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<AIFunction> GetToolsByCategory(string category)
    {
        if (!isRegistered)
        {
            RegisterAllTools();
        }

        if (categorizedTools.TryGetValue(category.ToLowerInvariant(), out var toolNames))
        {
            return toolNames
                .Select(name => tools.TryGetValue(name, out var tool) ? tool : null)
                .Where(t => t != null)
                .Cast<AIFunction>()
                .ToList();
        }

        return Array.Empty<AIFunction>();
    }

    /// <inheritdoc/>
    public AIFunction? GetTool(string toolName)
    {
        if (!isRegistered)
        {
            RegisterAllTools();
        }

        tools.TryGetValue(toolName, out var tool);
        return tool;
    }

    /// <inheritdoc/>
    public void RegisterAllTools()
    {
        if (isRegistered)
        {
            return;
        }

        logger.LogInformation("Registering Notebook Automation tools for Copilot");

        // Register tools by category
        RegisterVaultTools();
        RegisterTagTools();
        RegisterPdfTools();
        RegisterVideoTools();
        RegisterMarkdownTools();
        RegisterConfigTools();
        RegisterOneDriveTools();

        isRegistered = true;
        logger.LogInformation("Registered {Count} tools across {Categories} categories",
            tools.Count, categorizedTools.Count);
    }

    /// <summary>
    /// Register vault management tools.
    /// </summary>
    private void RegisterVaultTools()
    {
        var category = "vault";

        // vault_generate_index
        RegisterTool(category, "vault_generate_index",
            "Generate index files for each directory in the vault",
            async (string? path) =>
            {
                logger.LogInformation("Executing vault_generate_index with path: {Path}", path ?? "default");
                // Tool implementation will be added when integrating with actual SDK
                return await Task.FromResult($"Generated index files for vault at {path ?? "default location"}");
            });

        // vault_ensure_metadata
        RegisterTool(category, "vault_ensure_metadata",
            "Ensure metadata consistency across markdown files in the vault",
            async (string? path) =>
            {
                logger.LogInformation("Executing vault_ensure_metadata with path: {Path}", path ?? "default");
                return await Task.FromResult($"Ensured metadata consistency for vault at {path ?? "default location"}");
            });

        // vault_clean_index
        RegisterTool(category, "vault_clean_index",
            "Remove index files from the vault directories",
            async (string? path) =>
            {
                logger.LogInformation("Executing vault_clean_index with path: {Path}", path ?? "default");
                return await Task.FromResult($"Cleaned index files from vault at {path ?? "default location"}");
            });

        // vault_sync
        RegisterTool(category, "vault_sync",
            "Synchronize vault with OneDrive",
            async (string? direction) =>
            {
                logger.LogInformation("Executing vault_sync with direction: {Direction}", direction ?? "bidirectional");
                return await Task.FromResult($"Synchronized vault {direction ?? "bidirectionally"}");
            });
    }

    /// <summary>
    /// Register tag management tools.
    /// </summary>
    private void RegisterTagTools()
    {
        var category = "tag";

        // tag_add_nested
        RegisterTool(category, "tag_add_nested",
            "Add nested tags to markdown files",
            async (string path) =>
            {
                logger.LogInformation("Executing tag_add_nested for path: {Path}", path);
                return await Task.FromResult($"Added nested tags to {path}");
            });

        // tag_consolidate
        RegisterTool(category, "tag_consolidate",
            "Consolidate duplicate tags in the vault",
            async (string? path) =>
            {
                logger.LogInformation("Executing tag_consolidate for path: {Path}", path ?? "default");
                return await Task.FromResult($"Consolidated tags in {path ?? "entire vault"}");
            });

        // tag_restructure
        RegisterTool(category, "tag_restructure",
            "Restructure tags according to a new hierarchy",
            async (string? path) =>
            {
                logger.LogInformation("Executing tag_restructure for path: {Path}", path ?? "default");
                return await Task.FromResult($"Restructured tags in {path ?? "entire vault"}");
            });

        // tag_update_frontmatter
        RegisterTool(category, "tag_update_frontmatter",
            "Update frontmatter key-value pairs in markdown files",
            async (string path, string key, string value) =>
            {
                logger.LogInformation("Executing tag_update_frontmatter for {Path}: {Key}={Value}", path, key, value);
                return await Task.FromResult($"Updated frontmatter in {path}: {key}={value}");
            });

        // tag_diagnose_yaml
        RegisterTool(category, "tag_diagnose_yaml",
            "Diagnose YAML frontmatter issues in markdown files",
            async (string? path) =>
            {
                logger.LogInformation("Executing tag_diagnose_yaml for path: {Path}", path ?? "default");
                return await Task.FromResult($"Diagnosed YAML issues in {path ?? "entire vault"}");
            });

        // tag_metadata_check
        RegisterTool(category, "tag_metadata_check",
            "Check metadata consistency in the vault",
            async (string? path) =>
            {
                logger.LogInformation("Executing tag_metadata_check for path: {Path}", path ?? "default");
                return await Task.FromResult($"Checked metadata in {path ?? "entire vault"}");
            });

        // tag_clean_index
        RegisterTool(category, "tag_clean_index",
            "Remove tag information from index files",
            async (string? path) =>
            {
                logger.LogInformation("Executing tag_clean_index for path: {Path}", path ?? "default");
                return await Task.FromResult($"Cleaned tag information from index files in {path ?? "entire vault"}");
            });
    }

    /// <summary>
    /// Register PDF processing tools.
    /// </summary>
    private void RegisterPdfTools()
    {
        var category = "pdf";

        // pdf_convert
        RegisterTool(category, "pdf_convert",
            "Convert PDF files to markdown notes",
            async (string path) =>
            {
                logger.LogInformation("Executing pdf_convert for path: {Path}", path);
                return await Task.FromResult($"Converted PDF at {path} to markdown notes");
            });
    }

    /// <summary>
    /// Register video processing tools.
    /// </summary>
    private void RegisterVideoTools()
    {
        var category = "video";

        // video_create_notes
        RegisterTool(category, "video_create_notes",
            "Create notes from video files with transcripts",
            async (string path) =>
            {
                logger.LogInformation("Executing video_create_notes for path: {Path}", path);
                return await Task.FromResult($"Created notes from video at {path}");
            });

        // video_consolidate_transcripts
        RegisterTool(category, "video_consolidate_transcripts",
            "Consolidate video transcripts into class-level notes",
            async (string path) =>
            {
                logger.LogInformation("Executing video_consolidate_transcripts for path: {Path}", path);
                return await Task.FromResult($"Consolidated transcripts for videos at {path}");
            });
    }

    /// <summary>
    /// Register markdown generation tools.
    /// </summary>
    private void RegisterMarkdownTools()
    {
        var category = "markdown";

        // markdown_generate
        RegisterTool(category, "markdown_generate",
            "Convert HTML or EPUB files to markdown",
            async (string path) =>
            {
                logger.LogInformation("Executing markdown_generate for path: {Path}", path);
                return await Task.FromResult($"Generated markdown from {path}");
            });
    }

    /// <summary>
    /// Register configuration management tools.
    /// </summary>
    private void RegisterConfigTools()
    {
        var category = "config";

        // config_view
        RegisterTool(category, "config_view",
            "View current configuration settings",
            async () =>
            {
                logger.LogInformation("Executing config_view");
                return await Task.FromResult("Displayed current configuration");
            });

        // config_update
        RegisterTool(category, "config_update",
            "Update a configuration setting",
            async (string key, string value) =>
            {
                logger.LogInformation("Executing config_update: {Key}={Value}", key, value);
                return await Task.FromResult($"Updated configuration: {key}={value}");
            });

        // config_validate
        RegisterTool(category, "config_validate",
            "Validate current configuration",
            async () =>
            {
                logger.LogInformation("Executing config_validate");
                return await Task.FromResult("Configuration validation completed");
            });

        // config_list_keys
        RegisterTool(category, "config_list_keys",
            "List all available configuration keys",
            async () =>
            {
                logger.LogInformation("Executing config_list_keys");
                return await Task.FromResult("Listed all configuration keys");
            });

        // config_secrets_status
        RegisterTool(category, "config_secrets_status",
            "Check status of user secrets configuration",
            async () =>
            {
                logger.LogInformation("Executing config_secrets_status");
                return await Task.FromResult("User secrets status checked");
            });
    }

    /// <summary>
    /// Register OneDrive integration tools.
    /// </summary>
    private void RegisterOneDriveTools()
    {
        var category = "onedrive";

        // onedrive_refresh_token
        RegisterTool(category, "onedrive_refresh_token",
            "Refresh OneDrive authentication token",
            async () =>
            {
                logger.LogInformation("Executing onedrive_refresh_token");
                return await Task.FromResult("OneDrive token refreshed");
            });
    }

    /// <summary>
    /// Helper method to register a tool.
    /// </summary>
    private void RegisterTool(string category, string name, string description, Delegate implementation)
    {
        try
        {
            var function = AIFunctionFactory.Create(implementation, name, description);
            tools[name] = function;

            if (!categorizedTools.ContainsKey(category))
            {
                categorizedTools[category] = new List<string>();
            }
            categorizedTools[category].Add(name);

            logger.LogDebug("Registered tool: {ToolName} in category {Category}", name, category);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register tool: {ToolName}", name);
        }
    }
}
