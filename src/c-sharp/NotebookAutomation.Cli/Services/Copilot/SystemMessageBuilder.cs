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
        return @"You are an AI assistant for Notebook Automation, a CLI tool for managing Obsidian vaults and educational content.

Your capabilities include:
- Managing Obsidian vault structure and metadata
- Processing PDFs and extracting annotations
- Processing videos and generating summaries
- Managing tags and frontmatter
- Converting content to markdown
- Synchronizing with OneDrive

You have access to tools that can perform these operations. When a user asks you to do something, use the appropriate tool to accomplish the task.

Be helpful, concise, and accurate. Provide step-by-step explanations when needed.";
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
