// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Models.Browse;

/// <summary>
/// Represents a single item (file or directory) in the file browser.
/// </summary>
public record BrowseItem(
    string Name,
    string Path,
    bool IsDirectory,
    long? SizeBytes,
    DateTime? LastModified,
    IReadOnlyList<string>? Tags = null)
{
    /// <summary>
    /// Gets a formatted size string for display (e.g., "2.3 KB").
    /// </summary>
    public string SizeFormatted =>
        SizeBytes.HasValue
            ? FormatFileSize(SizeBytes.Value)
            : string.Empty;

    /// <summary>
    /// Gets the icon for this item based on its type.
    /// </summary>
    public string Icon => IsDirectory ? "📁" : "📄";

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
}
