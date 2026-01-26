// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Models.Browse;

/// <summary>
/// Represents the state of a browse session.
/// </summary>
public record BrowseSession(
    string? SelectedPath = null,
    BrowseAction LastAction = BrowseAction.None);

/// <summary>
/// Actions that can result from a browse session.
/// </summary>
public enum BrowseAction
{
    /// <summary>
    /// No action taken.
    /// </summary>
    None,

    /// <summary>
    /// File or directory was selected.
    /// </summary>
    Selected,

    /// <summary>
    /// Browse session was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// User requested to switch source.
    /// </summary>
    SwitchSource
}
