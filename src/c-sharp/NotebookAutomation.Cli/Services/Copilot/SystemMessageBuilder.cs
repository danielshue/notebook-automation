// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Builds system messages for Copilot sessions.
/// </summary>
public class SystemMessageBuilder : ISystemMessageBuilder
{
    private readonly AppConfig config;
    private readonly ILogger<SystemMessageBuilder> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemMessageBuilder"/> class.
    /// </summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public SystemMessageBuilder(AppConfig config, ILogger<SystemMessageBuilder> logger)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string BuildDefaultSystemMessage()
    {
        var vaultPath = config.Paths?.NotebookVaultFullpathRoot ?? "Not configured";
        var vaultResourcesPath = config.Paths?.NotebookVaultResourcesBasepath ?? "";
        var oneDrivePath = config.Paths?.OnedriveFullpathRoot ?? "Not configured";
        var oneDriveResourcesPath = config.Paths?.OnedriveResourcesBasepath ?? "";

        return $@"You are an AI assistant for Notebook Automation, a CLI tool for managing Obsidian vaults and educational content.

## Configured Paths

**Obsidian Vault:**
- Root: {vaultPath}
- Resources: {(string.IsNullOrEmpty(vaultResourcesPath) ? "(root)" : vaultResourcesPath)}

**OneDrive:**
- Root: {oneDrivePath}
- Resources: {(string.IsNullOrEmpty(oneDriveResourcesPath) ? "(root)" : oneDriveResourcesPath)}

## Your Capabilities

You have access to tools that can:
- **Browse the vault**: List directories and files, read note contents
- **Search notes**: Find notes by filename, content, or tags
- **Manage notes**: Create, update, and delete markdown files
- **Process content**: Convert PDFs, videos, and HTML to markdown
- **Manage metadata**: Update tags and frontmatter

When a user asks about their notes, folders, or content, use the vault tools to explore and answer their questions.
Paths provided by users may be relative to the vault root or absolute paths.

Be helpful, concise, and accurate. When listing files or folders, format the output clearly.";
    }

    /// <inheritdoc/>
    public string BuildSystemMessageWithTools(IReadOnlyList<string> availableTools)
    {
        var baseMessage = BuildDefaultSystemMessage();

        if (availableTools == null || availableTools.Count == 0)
        {
            return baseMessage;
        }

        var toolList = string.Join("\n", availableTools.Select(t => $"  - {t}"));

        return $@"{baseMessage}

Available tools:
{toolList}

When the user asks you to perform an action, select the most appropriate tool and call it with the correct parameters.";
    }

    /// <inheritdoc/>
    public string BuildCustomSystemMessage(string baseMessage, bool includeToolContext = true)
    {
        if (string.IsNullOrWhiteSpace(baseMessage))
        {
            return BuildDefaultSystemMessage();
        }

        if (!includeToolContext)
        {
            return baseMessage;
        }

        return $@"{baseMessage}

You have access to various tools for managing the Notebook Automation system. Use them appropriately when the user requests actions.";
    }
}
