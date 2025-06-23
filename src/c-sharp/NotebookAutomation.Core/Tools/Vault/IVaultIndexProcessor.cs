// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Interface for the vault index processor which generates index files for folders in a vault structure.
/// </summary>
/// <remarks>
/// The vault index processor analyzes folder structure, determines appropriate template types,
/// generates markdown index files based on folder content, and integrates with obsidian vault structure.
/// </remarks>
public interface IVaultIndexProcessor
{
    /// <summary>
    /// Generates an index file for the specified folder.
    /// </summary>
    /// <param name="folderPath">Path to the folder to generate an index for.</param>
    /// <param name="vaultPath">Path to the vault root directory.</param>
    /// <param name="forceOverwrite">If true, regenerates the index even if it already exists.</param>
    /// <param name="dryRun">If true, simulates the operation without making actual changes.</param>
    /// <returns>True if an index was generated or would be generated, false otherwise.</returns>
    Task<bool> GenerateIndexAsync(string folderPath, string vaultPath, bool forceOverwrite = false, bool dryRun = false);

    /// <summary>
    /// Determines the appropriate template type based on hierarchy level and folder name.
    /// </summary>
    /// <param name="hierarchyLevel">The hierarchy level of the folder relative to vault root.</param>
    /// <param name="folderName">Optional folder name for special case detection.</param>
    /// <returns>Template type identifier (e.g., "main", "program", "course", "module", "lesson").</returns>
    string DetermineTemplateType(int hierarchyLevel, string? folderName = null);
}
