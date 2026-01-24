// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Interface for building system messages for Copilot sessions.
/// </summary>
public interface ISystemMessageBuilder
{
    /// <summary>
    /// Build the default system message for Copilot.
    /// </summary>
    /// <returns>System message content.</returns>
    string BuildDefaultSystemMessage();

    /// <summary>
    /// Build a system message with tool context.
    /// </summary>
    /// <param name="availableTools">List of available tool names.</param>
    /// <returns>System message content with tool descriptions.</returns>
    string BuildSystemMessageWithTools(IReadOnlyList<string> availableTools);

    /// <summary>
    /// Build a custom system message.
    /// </summary>
    /// <param name="baseMessage">Base message content.</param>
    /// <param name="includeToolContext">Whether to include tool context.</param>
    /// <returns>System message content.</returns>
    string BuildCustomSystemMessage(string baseMessage, bool includeToolContext = true);
}
