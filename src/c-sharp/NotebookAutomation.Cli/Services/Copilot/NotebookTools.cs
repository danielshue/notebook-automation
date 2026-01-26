// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Extensions.AI;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.MarkdownGeneration;
using NotebookAutomation.Core.Tools.PdfProcessing;
using NotebookAutomation.Core.Tools.TagManagement;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Tools.VideoProcessing;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Manages registration and access to Notebook Automation CLI tools for Copilot.
/// </summary>
public class NotebookTools : INotebookTools
{
    private readonly ILogger<NotebookTools> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly AppConfig appConfig;
    private readonly IVaultBrowserService? vaultBrowser;
    private readonly IVaultSearchService? vaultSearchService;
    private readonly ITagService? tagService;
    private readonly IConfigService? configService;
    private readonly IVideoService? videoService;
    private readonly IPdfService? pdfService;
    private readonly IMarkdownService? markdownService;
    private readonly IOneDriveToolService? oneDriveToolService;
    private readonly Dictionary<string, AIFunction> tools = new();
    private readonly Dictionary<string, List<string>> categorizedTools = new();
    private bool isRegistered = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotebookTools"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="appConfig">Application configuration.</param>
    /// <param name="vaultBrowser">Optional vault browser service.</param>
    /// <param name="vaultSearchService">Optional vault search service.</param>
    /// <param name="tagService">Optional tag management service.</param>
    /// <param name="configService">Optional configuration service.</param>
    /// <param name="videoService">Optional video processing service.</param>
    /// <param name="pdfService">Optional PDF processing service.</param>
    /// <param name="markdownService">Optional markdown generation service.</param>
    /// <param name="oneDriveToolService">Optional OneDrive tool service.</param>
    public NotebookTools(
        ILogger<NotebookTools> logger,
        IServiceProvider serviceProvider,
        AppConfig appConfig,
        IVaultBrowserService? vaultBrowser = null,
        IVaultSearchService? vaultSearchService = null,
        ITagService? tagService = null,
        IConfigService? configService = null,
        IVideoService? videoService = null,
        IPdfService? pdfService = null,
        IMarkdownService? markdownService = null,
        IOneDriveToolService? oneDriveToolService = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        this.vaultBrowser = vaultBrowser;
        this.vaultSearchService = vaultSearchService;
        this.tagService = tagService;
        this.configService = configService;
        this.videoService = videoService;
        this.pdfService = pdfService;
        this.markdownService = markdownService;
        this.oneDriveToolService = oneDriveToolService;
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
        RegisterSearchTools();
        RegisterOpenTools();
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

        // vault_list_directory - List files and folders in a directory
        RegisterTool(category, "vault_list_directory",
            "List files and subdirectories in a vault directory. Use '/' or empty string for vault root. Returns folders (ending with /) and files.",
            async (string? path) =>
            {
                return await Task.Run(() => ListVaultDirectory(path));
            });

        // vault_list_notes - List only markdown notes in a directory
        RegisterTool(category, "vault_list_notes",
            "List only markdown (.md) files in a vault directory. Optionally recursive.",
            async (string? path, bool recursive) =>
            {
                return await Task.Run(() => ListVaultNotes(path, recursive));
            });

        // vault_read_note - Read the content of a note
        RegisterTool(category, "vault_read_note",
            "Read the content of a markdown note from the vault. Returns frontmatter and content.",
            async (string path) =>
            {
                return await Task.Run(() => ReadVaultNote(path));
            });

        // vault_create_note - Create a new note
        RegisterTool(category, "vault_create_note",
            "Create a new markdown note in the vault. Will not overwrite existing files.",
            async (string path, string content) =>
            {
                return await Task.Run(() => CreateVaultNote(path, content));
            });

        // vault_update_note - Update an existing note
        RegisterTool(category, "vault_update_note",
            "Update the content of an existing markdown note in the vault.",
            async (string path, string content) =>
            {
                return await Task.Run(() => UpdateVaultNote(path, content));
            });

        // vault_append_note - Append content to a note
        RegisterTool(category, "vault_append_note",
            "Append content to the end of an existing markdown note.",
            async (string path, string content) =>
            {
                return await Task.Run(() => AppendToVaultNote(path, content));
            });

        // vault_delete_note - Delete a note
        RegisterTool(category, "vault_delete_note",
            "Delete a markdown note from the vault. Use with caution.",
            async (string path) =>
            {
                return await Task.Run(() => DeleteVaultNote(path));
            });

        // vault_get_note_metadata - Get note frontmatter and stats
        RegisterTool(category, "vault_get_note_metadata",
            "Get metadata (frontmatter, tags, file stats) for a note without full content.",
            async (string path) =>
            {
                return await Task.Run(() => GetNoteMetadata(path));
            });
    }

    /// <summary>
    /// Register search tools.
    /// </summary>
    private void RegisterSearchTools()
    {
        var category = "search";

        // search_notes - Simple text search
        RegisterTool(category, "search_notes",
            "Search for notes containing specific text. Returns matching files with context around matches.",
            async (string query, string? path, int contextLength) =>
            {
                return await Task.Run(() => SearchNotes(query, path, contextLength > 0 ? contextLength : 100));
            });

        // search_by_filename - Search by filename pattern
        RegisterTool(category, "search_by_filename",
            "Search for notes by filename pattern (supports wildcards * and ?). Example: '*Corporate Finance*'",
            async (string pattern, string? path) =>
            {
                return await Task.Run(() => SearchByFilename(pattern, path));
            });

        // search_by_tag - Search notes with a specific tag
        RegisterTool(category, "search_by_tag",
            "Search for notes containing a specific tag in frontmatter or body.",
            async (string tag, string? path) =>
            {
                return await Task.Run(() => SearchByTag(tag, path));
            });

        // search_by_frontmatter - Search by frontmatter field value
        RegisterTool(category, "search_by_frontmatter",
            "Search for notes with a specific frontmatter field value. Example: field='course', value='Finance'",
            async (string field, string value, string? path) =>
            {
                return await Task.Run(() => SearchByFrontmatter(field, value, path));
            });
    }

    /// <summary>
    /// Register open/navigation tools.
    /// </summary>
    private void RegisterOpenTools()
    {
        var category = "open";

        // open_in_explorer - Open folder in file explorer
        RegisterTool(category, "open_in_explorer",
            "Open a vault folder in the system file explorer.",
            async (string? path) =>
            {
                return await Task.Run(() => OpenInExplorer(path));
            });

        // open_note - Open a note with the default application
        RegisterTool(category, "open_note",
            "Open a note with the system default application (usually a text editor).",
            async (string path) =>
            {
                return await Task.Run(() => OpenNote(path));
            });

        // get_vault_info - Get vault configuration info
        RegisterTool(category, "get_vault_info",
            "Get information about the configured vault and OneDrive paths.",
            async () =>
            {
                return await Task.Run(() => GetVaultInfo());
            });
    }

    /// <summary>
    /// Register tag management tools.
    /// </summary>
    private void RegisterTagTools()
    {
        var category = "tag";

        // tag_add_nested - Add nested tags to markdown files based on directory structure
        RegisterTool(category, "tag_add_nested",
            "Add nested tags to markdown files based on their directory structure. " +
            "Path can be relative to vault root (e.g., 'Notes/Classes') or absolute. " +
            "Set dryRun=true to preview changes without modifying files.",
            async (string path, bool dryRun) =>
            {
                if (tagService == null)
                {
                    return "Error: Tag service is not available.";
                }

                logger.LogInformation("Executing tag_add_nested for path: {Path}", path);
                var result = await tagService.AddNestedTagsAsync(path, dryRun);
                return FormatTagOperationResult(result, "add_nested");
            });

        // tag_consolidate - Consolidate duplicate tags in the vault
        RegisterTool(category, "tag_consolidate",
            "Consolidate duplicate tags in the vault by normalizing tag formats. " +
            "Path is optional; defaults to entire vault if not specified. " +
            "Set dryRun=true to preview changes.",
            async (string? path, bool dryRun) =>
            {
                if (tagService == null)
                {
                    return "Error: Tag service is not available.";
                }

                logger.LogInformation("Executing tag_consolidate for path: {Path}", path ?? "default");
                var result = await tagService.ConsolidateTagsAsync(path, dryRun);
                return FormatTagOperationResult(result, "consolidate");
            });

        // tag_restructure - Restructure tags according to a new hierarchy
        RegisterTool(category, "tag_restructure",
            "Restructure tags according to predefined hierarchy rules. " +
            "Path is optional; defaults to entire vault. Set dryRun=true to preview.",
            async (string? path, bool dryRun) =>
            {
                if (tagService == null)
                {
                    return "Error: Tag service is not available.";
                }

                logger.LogInformation("Executing tag_restructure for path: {Path}", path ?? "default");
                var result = await tagService.RestructureTagsAsync(path, dryRun);
                return FormatTagOperationResult(result, "restructure");
            });

        // tag_update_frontmatter - Update frontmatter key-value pairs in markdown files
        RegisterTool(category, "tag_update_frontmatter",
            "Update a frontmatter key across markdown files in a directory. " +
            "Path is the directory to process. Key and value are required. " +
            "Set dryRun=true to preview changes.",
            async (string path, string key, string value, bool dryRun) =>
            {
                if (tagService == null)
                {
                    return "Error: Tag service is not available.";
                }

                logger.LogInformation("Executing tag_update_frontmatter for {Path}: {Key}={Value}", path, key, value);
                var result = await tagService.UpdateFrontmatterAsync(path, key, value, dryRun);
                return FormatTagOperationResult(result, "update_frontmatter");
            });

        // tag_diagnose_yaml - Diagnose YAML frontmatter issues in markdown files
        RegisterTool(category, "tag_diagnose_yaml",
            "Diagnose YAML frontmatter issues in markdown files. " +
            "Reports invalid YAML, missing required fields, and formatting problems. " +
            "Path is optional; defaults to entire vault.",
            async (string? path) =>
            {
                if (tagService == null)
                {
                    return "Error: Tag service is not available.";
                }

                logger.LogInformation("Executing tag_diagnose_yaml for path: {Path}", path ?? "default");
                var result = await tagService.DiagnoseYamlAsync(path);
                return FormatYamlDiagnosisResult(result);
            });

        // tag_metadata_check - Check metadata consistency in the vault
        RegisterTool(category, "tag_metadata_check",
            "Check metadata consistency against the metadata schema. " +
            "Reports missing required fields and type mismatches. " +
            "Path is optional; defaults to entire vault.",
            async (string? path) =>
            {
                if (tagService == null)
                {
                    return "Error: Tag service is not available.";
                }

                logger.LogInformation("Executing tag_metadata_check for path: {Path}", path ?? "default");
                var result = await tagService.CheckMetadataAsync(path);
                return FormatTagOperationResult(result, "metadata_check");
            });

        // tag_clean_index - Remove tag information from index files
        RegisterTool(category, "tag_clean_index",
            "Remove tag information from index files (e.g., _index.md). " +
            "Path is optional; defaults to entire vault. Set dryRun=true to preview.",
            async (string? path, bool dryRun) =>
            {
                if (tagService == null)
                {
                    return "Error: Tag service is not available.";
                }

                logger.LogInformation("Executing tag_clean_index for path: {Path}", path ?? "default");
                var result = await tagService.CleanIndexFilesAsync(path, dryRun);
                return FormatTagOperationResult(result, "clean_index");
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
            "Convert PDF files to markdown notes. Parameters: path (required), outputPath (optional), " +
            "dryRun (optional, default false), noSummary (optional, default false), forceOverwrite (optional, default false)",
            async (string path, string? outputPath = null, bool dryRun = false, bool noSummary = false, bool forceOverwrite = false) =>
            {
                if (pdfService == null)
                {
                    return "Error: PDF service is not available.";
                }

                logger.LogInformation("Executing pdf_convert for path: {Path}", path);
                var result = await pdfService.ConvertAsync(path, outputPath, dryRun, noSummary, forceOverwrite);
                return FormatPdfResult(result);
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
            "Create notes from video files with transcripts. Parameters: path (required), outputPath (optional), " +
            "dryRun (optional, default false), noSummary (optional, default false), forceOverwrite (optional, default false)",
            async (string path, string? outputPath = null, bool dryRun = false, bool noSummary = false, bool forceOverwrite = false) =>
            {
                if (videoService == null)
                {
                    return "Error: Video service is not available.";
                }

                logger.LogInformation("Executing video_create_notes for path: {Path}", path);
                var result = await videoService.CreateNotesAsync(path, outputPath, dryRun, noSummary, forceOverwrite);
                return FormatVideoResult(result);
            });

        // video_consolidate_transcripts
        RegisterTool(category, "video_consolidate_transcripts",
            "Consolidate video transcripts into class-level notes. Parameters: path (required), " +
            "recursive (optional, default false), force (optional, default false), dryRun (optional, default false)",
            async (string path, bool recursive = false, bool force = false, bool dryRun = false) =>
            {
                if (videoService == null)
                {
                    return "Error: Video service is not available.";
                }

                logger.LogInformation("Executing video_consolidate_transcripts for path: {Path}", path);
                var result = await videoService.ConsolidateTranscriptsAsync(path, recursive, force, dryRun);
                return FormatConsolidationResult(result);
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
            "Convert HTML or EPUB files to markdown. Parameters: path (required), outputPath (optional), " +
            "dryRun (optional, default false), noSummary (optional, default true), forceOverwrite (optional, default false)",
            async (string path, string? outputPath = null, bool dryRun = false, bool noSummary = true, bool forceOverwrite = false) =>
            {
                if (markdownService == null)
                {
                    return "Error: Markdown service is not available.";
                }

                logger.LogInformation("Executing markdown_generate for path: {Path}", path);
                var result = await markdownService.GenerateAsync(path, outputPath, dryRun, noSummary, forceOverwrite);
                return FormatMarkdownResult(result);
            });
    }

    /// <summary>
    /// Register configuration management tools.
    /// </summary>
    private void RegisterConfigTools()
    {
        var category = "config";

        // config_view - View current configuration settings
        RegisterTool(category, "config_view",
            "View current configuration settings including paths, AI service, and Graph settings. " +
            "Sensitive values like API keys are masked.",
            async () =>
            {
                if (configService == null)
                {
                    return "Error: Configuration service is not available.";
                }

                logger.LogInformation("Executing config_view");
                var config = configService.GetCurrentConfig();
                return FormatConfigView(config);
            });

        // config_get - Get a specific configuration value
        RegisterTool(category, "config_get",
            "Get a specific configuration value by key. " +
            "Keys use dot notation: 'paths.notebook_vault_fullpath_root', 'aiservice.provider', etc.",
            async (string key) =>
            {
                if (configService == null)
                {
                    return "Error: Configuration service is not available.";
                }

                logger.LogInformation("Executing config_get for key: {Key}", key);
                var result = configService.GetConfigValue(key);
                if (result == null)
                {
                    return $"Configuration key not found: {key}";
                }

                return $"{result.Key}: {result.Value ?? "(not set)"}\nDescription: {result.Description}";
            });

        // config_update - Update a configuration setting
        RegisterTool(category, "config_update",
            "Update a configuration setting. Key uses dot notation. " +
            "Example: config_update('aiservice.provider', 'azure'). " +
            "Some settings may require application restart to take effect.",
            async (string key, string value) =>
            {
                if (configService == null)
                {
                    return "Error: Configuration service is not available.";
                }

                logger.LogInformation("Executing config_update: {Key}={Value}", key, value);
                var result = configService.UpdateConfig(key, value);
                return FormatConfigUpdateResult(result);
            });

        // config_validate - Validate current configuration
        RegisterTool(category, "config_validate",
            "Validate current configuration settings. " +
            "Checks for missing required values, invalid paths, and inconsistencies.",
            async () =>
            {
                if (configService == null)
                {
                    return "Error: Configuration service is not available.";
                }

                logger.LogInformation("Executing config_validate");
                var result = await configService.ValidateConfigAsync();
                return FormatConfigValidationResult(result);
            });

        // config_list_keys - List all available configuration keys
        RegisterTool(category, "config_list_keys",
            "List all available configuration keys with their descriptions and current values. " +
            "Useful for discovering what settings can be configured.",
            async () =>
            {
                if (configService == null)
                {
                    return "Error: Configuration service is not available.";
                }

                logger.LogInformation("Executing config_list_keys");
                var keys = configService.ListConfigKeys();
                return FormatConfigKeyList(keys);
            });

        // config_secrets_status - Check status of user secrets configuration
        RegisterTool(category, "config_secrets_status",
            "Check status of user secrets configuration. Shows which API keys and secrets are configured " +
            "without revealing their actual values.",
            async () =>
            {
                if (configService == null)
                {
                    return "Error: Configuration service is not available.";
                }

                logger.LogInformation("Executing config_secrets_status");
                var status = configService.GetSecretsStatus();
                return FormatSecretsStatus(status);
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
            "Refresh OneDrive authentication token. Returns status of the token refresh operation.",
            async () =>
            {
                if (oneDriveToolService == null)
                {
                    return "Error: OneDrive service is not available.";
                }

                logger.LogInformation("Executing onedrive_refresh_token");
                var result = await oneDriveToolService.RefreshTokenAsync();
                return FormatOneDriveTokenResult(result);
            });

        // onedrive_status
        RegisterTool(category, "onedrive_status",
            "Check OneDrive configuration and authentication status.",
            async () =>
            {
                if (oneDriveToolService == null)
                {
                    return "Error: OneDrive service is not available.";
                }

                logger.LogInformation("Executing onedrive_status");
                var result = await oneDriveToolService.GetStatusAsync();
                return FormatOneDriveStatusResult(result);
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

    #region Vault Directory Operations

    /// <summary>
    /// Lists files and directories in a vault path.
    /// </summary>
    private string ListVaultDirectory(string? path)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Listing directory: {Path}", path ?? "/");

            var result = vaultBrowser.ListDirectory(path ?? string.Empty);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var listing = result.Value!;
            var output = new StringBuilder();
            output.AppendLine($"Contents of: {listing.Path}");
            output.AppendLine();

            if (listing.Directories.Count > 0)
            {
                output.AppendLine("## Folders");
                foreach (var dir in listing.Directories)
                {
                    output.AppendLine($"  📁 {dir.Name}/ ({dir.ItemCount} items)");
                }
                output.AppendLine();
            }

            if (listing.Files.Count > 0)
            {
                output.AppendLine("## Files");
                foreach (var file in listing.Files)
                {
                    var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                    var icon = extension == ".md" ? "📝" : "📄";
                    output.AppendLine($"  {icon} {file.Name} ({file.SizeFormatted})");
                }
            }

            if (listing.Directories.Count == 0 && listing.Files.Count == 0)
            {
                output.AppendLine("(Empty directory)");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing directory: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Lists only markdown notes in a directory.
    /// </summary>
    private string ListVaultNotes(string? path, bool recursive)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Listing notes in: {Path}, recursive: {Recursive}", path ?? "/", recursive);

            var result = vaultBrowser.ListNotes(path ?? string.Empty, recursive);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var notes = result.Value!;
            var output = new StringBuilder();
            output.AppendLine($"Notes in: {path ?? "/"} {(recursive ? "(recursive)" : "")}");
            output.AppendLine($"Found: {notes.Count} note(s)");
            output.AppendLine();

            foreach (var note in notes)
            {
                output.AppendLine($"  📝 {note.RelativePath} ({note.SizeFormatted})");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing notes: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Vault File Operations

    /// <summary>
    /// Reads a note from the vault.
    /// </summary>
    private string ReadVaultNote(string path)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Reading note: {Path}", path);

            var result = vaultBrowser.ReadNote(path);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var note = result.Value!;
            var output = new StringBuilder();
            output.AppendLine($"# {note.Info.Name}");
            output.AppendLine();
            output.AppendLine($"**Path:** {note.Info.RelativePath}");
            output.AppendLine($"**Size:** {note.Info.SizeFormatted}");
            output.AppendLine($"**Modified:** {note.Info.LastModified:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine();
            output.AppendLine("---");
            output.AppendLine();
            output.AppendLine(note.Content);

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading note: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates a new note in the vault.
    /// </summary>
    private string CreateVaultNote(string path, string content)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Creating note: {Path}", path);

            var result = vaultBrowser.CreateNote(path, content, overwrite: false);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            return $"✅ Created note: {result.Value!.RelativePath}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating note: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates an existing note in the vault.
    /// </summary>
    private string UpdateVaultNote(string path, string content)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Updating note: {Path}", path);

            var result = vaultBrowser.UpdateNote(path, content);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            return $"✅ Updated note: {result.Value!.RelativePath}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating note: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Appends content to a note in the vault.
    /// </summary>
    private string AppendToVaultNote(string path, string content)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Appending to note: {Path}", path);

            var result = vaultBrowser.AppendToNote(path, content);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            return $"✅ Appended content to: {result.Value!.RelativePath}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error appending to note: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Deletes a note from the vault.
    /// </summary>
    private string DeleteVaultNote(string path)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Deleting note: {Path}", path);

            var result = vaultBrowser.DeleteNote(path);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            return $"✅ Deleted note: {path}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting note: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets metadata for a note without full content.
    /// </summary>
    private string GetNoteMetadata(string path)
    {
        try
        {
            if (vaultBrowser == null)
            {
                return "Error: Vault browser service is not available.";
            }

            logger.LogInformation("Getting metadata for: {Path}", path);

            var result = vaultBrowser.GetNoteMetadata(path);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var metadata = result.Value!;
            var output = new StringBuilder();
            output.AppendLine($"# Metadata: {metadata.Info.Name}");
            output.AppendLine();
            output.AppendLine("## File Info");
            output.AppendLine($"- **Path:** {metadata.Info.RelativePath}");
            output.AppendLine($"- **Size:** {metadata.Info.SizeFormatted}");
            output.AppendLine($"- **Created:** {metadata.Created:yyyy-MM-dd HH:mm:ss}");
            output.AppendLine($"- **Modified:** {metadata.Info.LastModified:yyyy-MM-dd HH:mm:ss}");

            if (metadata.Frontmatter.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("## Frontmatter");
                foreach (var kvp in metadata.Frontmatter)
                {
                    output.AppendLine($"- **{kvp.Key}:** {kvp.Value}");
                }
            }

            if (metadata.Tags.Count > 0)
            {
                output.AppendLine();
                output.AppendLine("## Tags");
                output.AppendLine(string.Join(", ", metadata.Tags.Select(t => $"`{t}`")));
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting note metadata: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Search Operations

    /// <summary>
    /// Searches notes by content.
    /// </summary>
    private string SearchNotes(string query, string? path, int contextLength)
    {
        try
        {
            if (vaultSearchService == null)
            {
                return "Error: Vault search service is not available.";
            }

            logger.LogInformation("Searching for '{Query}' in: {Path}", query, path ?? "/");

            // Convert context length to lines (rough approximation)
            var contextLines = Math.Max(1, contextLength / 50);
            var result = vaultSearchService.SearchContent(query, path, maxResults: 20, contextLines: contextLines);

            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var matches = result.Value!;
            var output = new StringBuilder();
            output.AppendLine($"Search results for: \"{query}\"");
            output.AppendLine($"Found: {matches.Count} matching note(s)");
            output.AppendLine();

            foreach (var match in matches)
            {
                output.AppendLine($"### {match.Note.RelativePath}");
                output.AppendLine($"({match.TotalMatches} matches)");
                foreach (var context in match.Matches.Take(3))
                {
                    output.AppendLine($"  > Line {context.LineNumber}: ...{context.Context.Replace('\n', ' ')}...");
                }
                output.AppendLine();
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching notes: {Query}", query);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches notes by filename pattern.
    /// </summary>
    private string SearchByFilename(string pattern, string? path)
    {
        try
        {
            if (vaultSearchService == null)
            {
                return "Error: Vault search service is not available.";
            }

            logger.LogInformation("Searching by filename pattern '{Pattern}' in: {Path}", pattern, path ?? "/");

            var result = vaultSearchService.SearchByFilename(pattern, path);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var notes = result.Value!;
            var output = new StringBuilder();
            output.AppendLine($"Filename search: \"{pattern}\"");
            output.AppendLine($"Found: {notes.Count} note(s)");
            output.AppendLine();

            foreach (var note in notes)
            {
                output.AppendLine($"  📝 {note.RelativePath} ({note.SizeFormatted})");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching by filename: {Pattern}", pattern);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches notes by tag.
    /// </summary>
    private string SearchByTag(string tag, string? path)
    {
        try
        {
            if (vaultSearchService == null)
            {
                return "Error: Vault search service is not available.";
            }

            logger.LogInformation("Searching for tag '{Tag}' in: {Path}", tag, path ?? "/");

            var result = vaultSearchService.SearchByTag(tag, path);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var matches = result.Value!;
            var tagToFind = tag.TrimStart('#');
            var output = new StringBuilder();
            output.AppendLine($"Notes with tag: #{tagToFind}");
            output.AppendLine($"Found: {matches.Count} note(s)");
            output.AppendLine();

            foreach (var match in matches)
            {
                output.AppendLine($"  📝 {match.Note.RelativePath}");
                var otherTags = match.Tags.Where(t => !t.Equals(tagToFind, StringComparison.OrdinalIgnoreCase)).Take(5);
                if (otherTags.Any())
                {
                    output.AppendLine($"     Also tagged: {string.Join(", ", otherTags.Select(t => $"#{t}"))}");
                }
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching by tag: {Tag}", tag);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches notes by frontmatter field value.
    /// </summary>
    private string SearchByFrontmatter(string field, string value, string? path)
    {
        try
        {
            if (vaultSearchService == null)
            {
                return "Error: Vault search service is not available.";
            }

            logger.LogInformation("Searching for frontmatter {Field}={Value} in: {Path}", field, value, path ?? "/");

            var result = vaultSearchService.SearchByFrontmatter(field, value, path);
            if (!result.IsSuccess)
            {
                return $"Error: {result.Error}";
            }

            var matches = result.Value!;
            var output = new StringBuilder();
            output.AppendLine($"Notes with {field} containing \"{value}\"");
            output.AppendLine($"Found: {matches.Count} note(s)");
            output.AppendLine();

            foreach (var match in matches)
            {
                output.AppendLine($"  📝 {match.Note.RelativePath}");
                output.AppendLine($"     {field}: {match.FieldValue}");
            }

            return output.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching by frontmatter: {Field}={Value}", field, value);
            return $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Open Operations

    /// <summary>
    /// Opens a folder in the file explorer.
    /// </summary>
    private string OpenInExplorer(string? path)
    {
        try
        {
            string fullPath;
            if (vaultBrowser != null)
            {
                fullPath = vaultBrowser.ResolveFullPath(path ?? string.Empty);
            }
            else
            {
                fullPath = ResolveVaultPath(path);
            }

            logger.LogInformation("Opening in explorer: {Path}", fullPath);

            if (!Directory.Exists(fullPath))
            {
                return $"Error: Directory not found: {path ?? "/"}";
            }

            if (OperatingSystem.IsWindows())
            {
                Process.Start("explorer.exe", fullPath);
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", fullPath);
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", fullPath);
            }
            else
            {
                return $"Cannot open explorer on this platform. Path: {fullPath}";
            }

            return $"✅ Opened folder: {path ?? "/"}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening explorer: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Opens a note with the default application.
    /// </summary>
    private string OpenNote(string path)
    {
        try
        {
            string fullPath;
            if (vaultBrowser != null)
            {
                fullPath = vaultBrowser.ResolveFullPath(path);
            }
            else
            {
                fullPath = ResolveVaultPath(path);
            }

            logger.LogInformation("Opening note: {Path}", fullPath);

            if (!File.Exists(fullPath))
            {
                return $"Error: Note not found: {path}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            };
            Process.Start(psi);

            return $"✅ Opened note: {path}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening note: {Path}", path);
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets vault configuration information.
    /// </summary>
    private string GetVaultInfo()
    {
        if (vaultBrowser != null)
        {
            var result = vaultBrowser.GetVaultInfo();
            if (result.IsSuccess)
            {
                var info = result.Value!;
                var output = new StringBuilder();
                output.AppendLine("# Vault Configuration");
                output.AppendLine();
                output.AppendLine("## Obsidian Vault");
                output.AppendLine($"- **Name:** {info.Name}");
                output.AppendLine($"- **Root:** {info.RootPath}");
                output.AppendLine($"- **Total Notes:** {info.TotalNotes}");
                output.AppendLine($"- **Total Folders:** {info.TotalFolders}");
                output.AppendLine($"- **Total Size:** {info.TotalSizeFormatted}");

                output.AppendLine();
                output.AppendLine("## OneDrive");
                output.AppendLine($"- **Root:** {appConfig.Paths?.OnedriveFullpathRoot ?? "(not configured)"}");
                output.AppendLine($"- **Resources Base:** {appConfig.Paths?.OnedriveResourcesBasepath ?? "(root)"}");

                return output.ToString();
            }
        }

        // Fallback to config-based info
        var fallback = new StringBuilder();
        fallback.AppendLine("# Vault Configuration");
        fallback.AppendLine();

        fallback.AppendLine("## Obsidian Vault");
        fallback.AppendLine($"- **Root:** {appConfig.Paths?.NotebookVaultFullpathRoot ?? "(not configured)"}");
        fallback.AppendLine($"- **Resources Base:** {appConfig.Paths?.NotebookVaultResourcesBasepath ?? "(root)"}");

        fallback.AppendLine();
        fallback.AppendLine("## OneDrive");
        fallback.AppendLine($"- **Root:** {appConfig.Paths?.OnedriveFullpathRoot ?? "(not configured)"}");
        fallback.AppendLine($"- **Resources Base:** {appConfig.Paths?.OnedriveResourcesBasepath ?? "(root)"}");

        return fallback.ToString();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Resolves a path relative to the vault root. Fallback for when vaultBrowser is unavailable.
    /// </summary>
    private string ResolveVaultPath(string? relativePath)
    {
        var vaultRoot = appConfig.Paths?.NotebookVaultFullpathRoot;
        if (string.IsNullOrEmpty(vaultRoot))
        {
            throw new InvalidOperationException("Vault root path is not configured. Please set 'notebook_vault_fullpath_root' in config.");
        }

        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/" || relativePath == "\\")
        {
            return vaultRoot;
        }

        // Handle absolute paths
        if (Path.IsPathRooted(relativePath))
        {
            // Verify the path is within the vault
            var normalizedPath = Path.GetFullPath(relativePath);
            var normalizedVault = Path.GetFullPath(vaultRoot);
            if (!normalizedPath.StartsWith(normalizedVault, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Path '{relativePath}' is outside the vault root.");
            }

            return normalizedPath;
        }

        // Combine with vault root
        return Path.GetFullPath(Path.Combine(vaultRoot, relativePath.TrimStart('/', '\\')));
    }

    /// <summary>
    /// Formats a TagOperationResult for display.
    /// </summary>
    private static string FormatTagOperationResult(TagOperationResult result, string operation)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Tag Operation: {operation}");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.Success ? "✅ Success" : "❌ Failed")}");

        if (result.DryRun)
        {
            sb.AppendLine("**Mode:** Dry Run (no files modified)");
        }

        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Files processed: {result.FilesProcessed}");
        sb.AppendLine($"- Files modified: {result.FilesModified}");
        sb.AppendLine($"- Tags added/updated: {result.TagsAdded}");

        if (result.FilesWithErrors > 0)
        {
            sb.AppendLine($"- Files with errors: {result.FilesWithErrors}");
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Message:** {result.Message}");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a YamlDiagnosisResult for display.
    /// </summary>
    private static string FormatYamlDiagnosisResult(YamlDiagnosisResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# YAML Diagnosis Report");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.Success ? "✅ No issues found" : "⚠️ Issues detected")}");
        sb.AppendLine($"- Files scanned: {result.FilesScanned}");
        sb.AppendLine($"- Files with issues: {result.FilesWithIssues}");

        if (result.Issues is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine("## Issues Found");

            foreach (var issue in result.Issues.Take(20)) // Limit to first 20 issues
            {
                sb.AppendLine();
                sb.AppendLine($"### {issue.FilePath}");
                if (issue.LineNumber > 0)
                {
                    sb.AppendLine($"- Line: {issue.LineNumber}");
                }

                sb.AppendLine($"- Issue: {issue.Description}");
                if (!string.IsNullOrEmpty(issue.SuggestedFix))
                {
                    sb.AppendLine($"- Suggested fix: {issue.SuggestedFix}");
                }
            }

            if (result.Issues.Count > 20)
            {
                sb.AppendLine();
                sb.AppendLine($"_... and {result.Issues.Count - 20} more issues_");
            }
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Note:** {result.Message}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a ConfigView for display.
    /// </summary>
    private static string FormatConfigView(ConfigView config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Current Configuration");
        sb.AppendLine();
        sb.AppendLine($"**Config File:** {config.ConfigFilePath}");
        sb.AppendLine();

        sb.AppendLine("## Paths");
        sb.AppendLine($"- Vault Root: {config.Paths.NotebookVaultRoot ?? "(not set)"}");
        sb.AppendLine($"- Vault Resources Base: {config.Paths.NotebookVaultResourcesBase ?? "(not set)"}");
        sb.AppendLine($"- OneDrive Root: {config.Paths.OnedriveRoot ?? "(not set)"}");
        sb.AppendLine($"- OneDrive Resources Base: {config.Paths.OnedriveResourcesBase ?? "(not set)"}");
        sb.AppendLine($"- Metadata Schema: {config.Paths.MetadataSchemaFile ?? "(not set)"}");
        sb.AppendLine($"- Prompts Path: {config.Paths.PromptsPath ?? "(not set)"}");
        sb.AppendLine($"- Logging Dir: {config.Paths.LoggingDir ?? "(not set)"}");

        sb.AppendLine();
        sb.AppendLine("## AI Service");
        sb.AppendLine($"- Provider: {config.AiService.Provider ?? "(not set)"}");
        sb.AppendLine($"- Model: {config.AiService.Model ?? "(not set)"}");
        sb.AppendLine($"- Endpoint: {config.AiService.Endpoint ?? "(not set)"}");
        sb.AppendLine($"- API Key: {(config.AiService.HasApiKey ? "✅ Configured" : "❌ Not configured")}");
        if (!string.IsNullOrEmpty(config.AiService.AzureDeployment))
        {
            sb.AppendLine($"- Azure Deployment: {config.AiService.AzureDeployment}");
        }

        if (config.MicrosoftGraph != null)
        {
            sb.AppendLine();
            sb.AppendLine("## Microsoft Graph");
            sb.AppendLine($"- Tenant ID: {config.MicrosoftGraph.TenantId ?? "(not set)"}");
            sb.AppendLine($"- Client ID: {config.MicrosoftGraph.ClientId ?? "(not set)"}");
            sb.AppendLine($"- Client Secret: {(config.MicrosoftGraph.HasClientSecret ? "✅ Configured" : "❌ Not configured")}");
            sb.AppendLine($"- Authority: {config.MicrosoftGraph.Authority ?? "(not set)"}");
        }

        sb.AppendLine();
        sb.AppendLine("## File Extensions");
        sb.AppendLine($"- Video: {config.VideoExtensions ?? "(default)"}");
        sb.AppendLine($"- PDF: {config.PdfExtensions ?? "(default)"}");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a ConfigUpdateResult for display.
    /// </summary>
    private static string FormatConfigUpdateResult(ConfigUpdateResult result)
    {
        var sb = new StringBuilder();
        if (result.Success)
        {
            sb.AppendLine("✅ Configuration Updated");
            sb.AppendLine();
            sb.AppendLine($"**Key:** {result.Key}");
            sb.AppendLine($"**Old Value:** {result.OldValue ?? "(not set)"}");
            sb.AppendLine($"**New Value:** {result.NewValue}");

            if (result.RequiresRestart)
            {
                sb.AppendLine();
                sb.AppendLine("⚠️ **Note:** This change requires an application restart to take effect.");
            }
        }
        else
        {
            sb.AppendLine("❌ Configuration Update Failed");
            sb.AppendLine();
            sb.AppendLine($"**Key:** {result.Key}");
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a ConfigValidationSummary for display.
    /// </summary>
    private static string FormatConfigValidationResult(ConfigValidationSummary result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Configuration Validation");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.IsValid ? "✅ Valid" : "❌ Invalid")}");

        if (result.Issues.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Errors");
            foreach (var issue in result.Issues)
            {
                sb.AppendLine($"- **{issue.Key}:** {issue.Message}");
            }
        }

        if (result.Warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Warnings");
            foreach (var warning in result.Warnings)
            {
                sb.AppendLine($"- {warning}");
            }
        }

        if (result.IsValid && result.Issues.Count == 0 && result.Warnings.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("All configuration settings are valid.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a list of ConfigKeyInfo for display.
    /// </summary>
    private static string FormatConfigKeyList(IReadOnlyList<ConfigKeyInfo> keys)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Available Configuration Keys");
        sb.AppendLine();

        string? currentCategory = null;
        foreach (var key in keys)
        {
            if (key.Category != currentCategory)
            {
                if (currentCategory != null)
                {
                    sb.AppendLine();
                }

                sb.AppendLine($"## {char.ToUpper(key.Category[0])}{key.Category[1..]}");
                currentCategory = key.Category;
            }

            var valueDisplay = key.IsSensitive ? "***masked***" : (key.CurrentValue ?? "(not set)");
            sb.AppendLine($"- **{key.Key}**");
            sb.AppendLine($"  - {key.Description}");
            sb.AppendLine($"  - Current: `{valueDisplay}`");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a SecretsStatus for display.
    /// </summary>
    private static string FormatSecretsStatus(SecretsStatus status)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# User Secrets Status");
        sb.AppendLine();
        sb.AppendLine($"**Secrets File:** {(status.SecretsFileExists ? "✅ Exists" : "❌ Not found")}");
        if (!string.IsNullOrEmpty(status.SecretsFilePath))
        {
            sb.AppendLine($"**Path:** {status.SecretsFilePath}");
        }

        sb.AppendLine();
        sb.AppendLine("## API Keys");
        sb.AppendLine($"- OpenAI API Key: {(status.HasOpenAiApiKey ? "✅ Configured" : "❌ Not configured")}");
        sb.AppendLine($"- Azure API Key: {(status.HasAzureApiKey ? "✅ Configured" : "❌ Not configured")}");
        sb.AppendLine($"- Azure Endpoint: {(status.HasAzureEndpoint ? "✅ Configured" : "❌ Not configured")}");
        sb.AppendLine($"- Foundry API Key: {(status.HasFoundryApiKey ? "✅ Configured" : "❌ Not configured")}");

        sb.AppendLine();
        sb.AppendLine("## Microsoft Graph");
        sb.AppendLine($"- Client Secret: {(status.HasGraphClientSecret ? "✅ Configured" : "❌ Not configured")}");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a VideoOperationResult for display.
    /// </summary>
    private static string FormatVideoResult(VideoOperationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Video Processing Result");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.Success ? "✅ Success" : "❌ Failed")}");
        if (result.DryRun)
        {
            sb.AppendLine("**Mode:** 🔍 Dry Run (no files modified)");
        }

        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Files Found: {result.FilesFound}");
        sb.AppendLine($"- Notes Created: {result.NotesCreated}");
        sb.AppendLine($"- Failed: {result.Failed}");
        if (result.TotalTokens > 0)
        {
            sb.AppendLine($"- Total Tokens: {result.TotalTokens}");
        }

        if (result.ProcessingTime != TimeSpan.Zero)
        {
            sb.AppendLine($"- Processing Time: {result.ProcessingTime.TotalSeconds:F1}s");
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Message:** {result.Message}");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a VideoConsolidationResult for display.
    /// </summary>
    private static string FormatConsolidationResult(VideoConsolidationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Transcript Consolidation Result");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.Success ? "✅ Success" : "❌ Failed")}");
        if (result.DryRun)
        {
            sb.AppendLine("**Mode:** 🔍 Dry Run (no files modified)");
        }

        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Transcripts Aggregated: {result.TranscriptsAggregated}");
        sb.AppendLine($"- Skipped: {result.Skipped}");
        sb.AppendLine($"- File Written: {(result.WasWritten ? "Yes" : "No")}");
        if (!string.IsNullOrEmpty(result.OutputPath))
        {
            sb.AppendLine($"- Output Path: {result.OutputPath}");
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Message:** {result.Message}");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a PdfOperationResult for display.
    /// </summary>
    private static string FormatPdfResult(PdfOperationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# PDF Processing Result");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.Success ? "✅ Success" : "❌ Failed")}");
        if (result.DryRun)
        {
            sb.AppendLine("**Mode:** 🔍 Dry Run (no files modified)");
        }

        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Files Found: {result.FilesFound}");
        sb.AppendLine($"- Notes Created: {result.NotesCreated}");
        sb.AppendLine($"- Failed: {result.Failed}");
        if (result.TotalTokens > 0)
        {
            sb.AppendLine($"- Total Tokens: {result.TotalTokens}");
        }

        if (result.ProcessingTime != TimeSpan.Zero)
        {
            sb.AppendLine($"- Processing Time: {result.ProcessingTime.TotalSeconds:F1}s");
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Message:** {result.Message}");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a MarkdownOperationResult for display.
    /// </summary>
    private static string FormatMarkdownResult(MarkdownOperationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Markdown Generation Result");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.Success ? "✅ Success" : "❌ Failed")}");
        if (result.DryRun)
        {
            sb.AppendLine("**Mode:** 🔍 Dry Run (no files modified)");
        }

        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Files Found: {result.FilesFound}");
        sb.AppendLine($"- Notes Created: {result.NotesCreated}");
        sb.AppendLine($"- Failed: {result.Failed}");
        if (result.TotalTokens > 0)
        {
            sb.AppendLine($"- Total Tokens: {result.TotalTokens}");
        }

        if (result.ProcessingTime != TimeSpan.Zero)
        {
            sb.AppendLine($"- Processing Time: {result.ProcessingTime.TotalSeconds:F1}s");
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Message:** {result.Message}");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats an OneDriveTokenResult for display.
    /// </summary>
    private static string FormatOneDriveTokenResult(OneDriveTokenResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# OneDrive Token Refresh Result");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(result.Success ? "✅ Success" : "❌ Failed")}");
        sb.AppendLine($"**Token Valid:** {(result.TokenValid ? "✅ Yes" : "❌ No")}");

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Message:** {result.Message}");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats an OneDriveStatusResult for display.
    /// </summary>
    private static string FormatOneDriveStatusResult(OneDriveStatusResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# OneDrive Status");
        sb.AppendLine();
        sb.AppendLine($"**Configured:** {(result.IsConfigured ? "✅ Yes" : "❌ No")}");
        sb.AppendLine($"**Token Valid:** {(result.TokenValid ? "✅ Yes" : "❌ No")}");

        if (!string.IsNullOrEmpty(result.OneDriveRoot))
        {
            sb.AppendLine($"**OneDrive Root:** {result.OneDriveRoot}");
        }

        if (!string.IsNullOrEmpty(result.Message))
        {
            sb.AppendLine();
            sb.AppendLine($"**Message:** {result.Message}");
        }

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine();
            sb.AppendLine($"**Error:** {result.ErrorMessage}");
        }

        return sb.ToString();
    }

    #endregion
}
