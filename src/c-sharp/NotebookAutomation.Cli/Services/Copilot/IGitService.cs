// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Service for detecting Git repository information.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Check if the current directory is a Git repository.
    /// </summary>
    /// <param name="path">Path to check. If null, uses current directory.</param>
    /// <returns>True if the directory is a Git repository.</returns>
    Task<bool> IsGitRepositoryAsync(string? path = null);

    /// <summary>
    /// Get the root directory of the Git repository.
    /// </summary>
    /// <param name="path">Path to start from. If null, uses current directory.</param>
    /// <returns>Git root directory, or null if not a Git repository.</returns>
    Task<string?> GetGitRootAsync(string? path = null);

    /// <summary>
    /// Get the current Git branch name.
    /// </summary>
    /// <param name="path">Path to Git repository. If null, uses current directory.</param>
    /// <returns>Branch name, or null if not a Git repository.</returns>
    Task<string?> GetCurrentBranchAsync(string? path = null);
}
