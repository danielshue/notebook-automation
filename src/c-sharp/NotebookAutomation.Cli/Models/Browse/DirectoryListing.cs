// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Models.Browse;

/// <summary>
/// Represents the contents of a directory.
/// </summary>
public record DirectoryListing(
    string CurrentPath,
    IReadOnlyList<BrowseItem> Items,
    bool HasParent)
{
    /// <summary>
    /// Gets the items sorted with directories first, then files.
    /// </summary>
    public IReadOnlyList<BrowseItem> SortedItems
    {
        get
        {
            return Items
                .OrderByDescending(i => i.IsDirectory)
                .ThenBy(i => i.Name)
                .ToList();
        }
    }

    /// <summary>
    /// Gets only the directories in this listing.
    /// </summary>
    public IReadOnlyList<BrowseItem> Directories =>
        Items.Where(i => i.IsDirectory).OrderBy(i => i.Name).ToList();

    /// <summary>
    /// Gets only the files in this listing.
    /// </summary>
    public IReadOnlyList<BrowseItem> Files =>
        Items.Where(i => !i.IsDirectory).OrderBy(i => i.Name).ToList();
}
