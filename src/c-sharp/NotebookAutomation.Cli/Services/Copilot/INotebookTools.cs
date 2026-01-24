// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Interface for managing Notebook Automation CLI tools that can be called by Copilot.
/// </summary>
public interface INotebookTools
{
    /// <summary>
    /// Get all registered tools for Copilot.
    /// </summary>
    /// <returns>Collection of AI function tools.</returns>
    IReadOnlyList<AIFunction> GetAllTools();

    /// <summary>
    /// Get tools by category.
    /// </summary>
    /// <param name="category">Category name (vault, tag, pdf, video, markdown, config, onedrive).</param>
    /// <returns>Collection of AI function tools in the specified category.</returns>
    IReadOnlyList<AIFunction> GetToolsByCategory(string category);

    /// <summary>
    /// Get a specific tool by name.
    /// </summary>
    /// <param name="toolName">Name of the tool.</param>
    /// <returns>The AI function tool, or null if not found.</returns>
    AIFunction? GetTool(string toolName);

    /// <summary>
    /// Register all available tools.
    /// </summary>
    void RegisterAllTools();
}
