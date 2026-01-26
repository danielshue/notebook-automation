// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Models.Browse;
using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Cli.Services.Browse;

/// <summary>
/// File browser source that wraps the VaultBrowserService.
/// </summary>
public class VaultBrowserSource(IVaultBrowserService vaultBrowser) : IFileBrowserSource
{
    private readonly IVaultBrowserService _vaultBrowser = vaultBrowser ?? throw new ArgumentNullException(nameof(vaultBrowser));
    private string _currentPath = string.Empty;

    /// <inheritdoc/>
    public string SourceName => "Vault";

    /// <inheritdoc/>
    public string CurrentPath => _currentPath;

    /// <inheritdoc/>
    public Task<BrowseResult<DirectoryListing>> ListDirectoryAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var result = _vaultBrowser.ListDirectory(path);

        if (!result.IsSuccess)
        {
            return Task.FromResult(BrowseResult<DirectoryListing>.Failure(result.Error ?? "Unknown error"));
        }

        var listing = result.Value!;
        var items = new List<BrowseItem>();

        // Add directories
        foreach (var dir in listing.Directories)
        {
            items.Add(new BrowseItem(
                Name: dir.Name,
                Path: dir.RelativePath,
                IsDirectory: true,
                SizeBytes: null,
                LastModified: null,
                Tags: null));
        }

        // Add files
        foreach (var file in listing.Files)
        {
            items.Add(new BrowseItem(
                Name: file.Name,
                Path: file.RelativePath,
                IsDirectory: false,
                SizeBytes: file.SizeBytes,
                LastModified: file.LastModified,
                Tags: null));
        }

        var directoryListing = new DirectoryListing(
            CurrentPath: listing.Path,
            Items: items,
            HasParent: !string.IsNullOrEmpty(path) && path != "/");

        _currentPath = listing.Path;

        return Task.FromResult(BrowseResult<DirectoryListing>.Success(directoryListing));
    }

    /// <inheritdoc/>
    public Task<BrowseResult<FileContent>> ReadFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var result = _vaultBrowser.ReadNote(path);

        if (!result.IsSuccess)
        {
            return Task.FromResult(BrowseResult<FileContent>.Failure(result.Error ?? "Unknown error"));
        }

        var noteContent = result.Value!;
        var browseItem = new BrowseItem(
            Name: noteContent.Info.Name,
            Path: noteContent.Info.RelativePath,
            IsDirectory: false,
            SizeBytes: noteContent.Info.SizeBytes,
            LastModified: noteContent.Info.LastModified,
            Tags: null);

        // Parse frontmatter dictionary if needed
        Dictionary<string, object>? frontmatterDict = null;
        if (!string.IsNullOrEmpty(noteContent.Frontmatter))
        {
            // The frontmatter is a YAML string, we would need to parse it
            // For now, we'll pass null and rely on Body
            frontmatterDict = null;
        }

        var fileContent = new FileContent(
            Info: browseItem,
            Content: noteContent.Content,
            Frontmatter: frontmatterDict,
            Body: noteContent.Body);

        return Task.FromResult(BrowseResult<FileContent>.Success(fileContent));
    }

    /// <inheritdoc/>
    public Task<BrowseResult> CreateFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken = default)
    {
        var result = _vaultBrowser.CreateNote(path, content, overwrite: false);

        if (!result.IsSuccess)
        {
            return Task.FromResult(BrowseResult.Failure(result.Error ?? "Unknown error"));
        }

        return Task.FromResult(BrowseResult.Success());
    }

    /// <inheritdoc/>
    public Task<BrowseResult> DeleteFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var result = _vaultBrowser.DeleteNote(path);

        if (!result.IsSuccess)
        {
            return Task.FromResult(BrowseResult.Failure(result.Error ?? "Unknown error"));
        }

        return Task.FromResult(BrowseResult.Success());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> GetTagsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var result = _vaultBrowser.GetNoteMetadata(path);

        if (!result.IsSuccess || result.Value == null)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        // Convert IReadOnlySet to IReadOnlyList
        return Task.FromResult<IReadOnlyList<string>>(result.Value.Tags.ToList());
    }

    /// <inheritdoc/>
    public Task<BrowseResult> UpdateTagsAsync(
        string path,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken = default)
    {
        // Read current note
        var readResult = _vaultBrowser.ReadNote(path);
        if (!readResult.IsSuccess)
        {
            return Task.FromResult(BrowseResult.Failure(readResult.Error ?? "Unknown error"));
        }

        var noteContent = readResult.Value!;

        // Parse frontmatter dictionary
        var metadataResult = _vaultBrowser.GetNoteMetadata(path);
        Dictionary<string, object> frontmatter;
        
        if (metadataResult.IsSuccess && metadataResult.Value != null)
        {
            frontmatter = metadataResult.Value.Frontmatter;
        }
        else
        {
            frontmatter = new Dictionary<string, object>();
        }

        // Update tags in frontmatter
        frontmatter["tags"] = tags.ToList();

        // Reconstruct content with updated frontmatter
        // Note: This is a simplified YAML serialization. For production use,
        // consider using a proper YAML library like YamlDotNet for correct formatting.
        var yamlContent = "---\n";
        foreach (var kvp in frontmatter)
        {
            if (kvp.Value is IEnumerable<string> list)
            {
                yamlContent += $"{kvp.Key}: [{string.Join(", ", list)}]\n";
            }
            else
            {
                yamlContent += $"{kvp.Key}: {kvp.Value}\n";
            }
        }
        yamlContent += "---\n";

        var newContent = yamlContent + noteContent.Body;

        // Update the note
        var updateResult = _vaultBrowser.UpdateNote(path, newContent);

        if (!updateResult.IsSuccess)
        {
            return Task.FromResult(BrowseResult.Failure(updateResult.Error ?? "Unknown error"));
        }

        return Task.FromResult(BrowseResult.Success());
    }
}
