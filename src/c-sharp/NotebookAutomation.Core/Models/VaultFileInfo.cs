// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Represents information about a file within a vault for index generation.
/// </summary>

public class VaultFileInfo
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path from the vault root.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly title for display.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type (reading, video, transcript, etc.).
    /// </summary>
    public string ContentType { get; set; } = "note";

    /// <summary>
    /// Gets or sets the course name.
    /// </summary>
    public string? Course { get; set; }

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// Gets or sets the full file path.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
}
