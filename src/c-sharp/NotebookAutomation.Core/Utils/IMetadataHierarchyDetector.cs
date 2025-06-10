// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Interface for detecting and updating metadata hierarchy information in vault files.
/// </summary>
public interface IMetadataHierarchyDetector
{
    /// <summary>
    /// Gets the root path of the notebook vault.
    /// </summary>
    string? VaultRoot { get; }
    /// <summary>
    /// Updates the provided metadata dictionary with hierarchy information (program, course, class, etc.).
    /// </summary>
    /// <param name="metadata">The metadata dictionary to update.</param>
    /// <param name="hierarchyInfo">The hierarchy information to apply.</param>
    /// <param name="templateType">Optional template type to determine which levels to include.</param>
    /// <returns>The updated metadata dictionary.</returns>
    Dictionary<string, object?> UpdateMetadataWithHierarchy(
        Dictionary<string, object?> metadata,
        Dictionary<string, string> hierarchyInfo,
        string? templateType = null);

    /// <summary>
    /// Finds hierarchy information for a given file path.
    /// </summary>
    /// <param name="filePath">The file path to analyze.</param>
    /// <returns>A dictionary containing hierarchy information.</returns>
    Dictionary<string, string> FindHierarchyInfo(string filePath);

    /// <summary>
    /// Calculates the hierarchy level of a folder relative to the vault root.
    /// </summary>
    /// <param name="folderPath">The folder path to analyze.</param>
    /// <param name="vaultPath">The vault root path. If null, uses the instance VaultRoot.</param>
    /// <returns>The hierarchy level (0 = vault root, 1 = program, 2 = course, 3 = class, 4 = module, 5 = lesson, etc.).</returns>
    int CalculateHierarchyLevel(string folderPath, string? vaultPath = null);

    /// <summary>
    /// Gets the template type based on hierarchy level.
    /// </summary>
    /// <param name="hierarchyLevel">The hierarchy level.</param>
    /// <returns>The template type (main, program, course, class, module, lesson).</returns>
    string GetTemplateTypeFromHierarchyLevel(int hierarchyLevel);

    /// <summary>
    /// Calculates the hierarchy level of a folder relative to a base path, with optional level offset.
    /// This is useful when using --override-vault-root to maintain correct hierarchy relationships.
    /// </summary>
    /// <param name="folderPath">The folder path to analyze.</param>
    /// <param name="basePath">The base path to calculate relative to.</param>
    /// <param name="baseHierarchyLevel">The hierarchy level of the base path (default: 0).</param>
    /// <returns>The adjusted hierarchy level.</returns>
    int CalculateHierarchyLevelWithOffset(string folderPath, string basePath, int baseHierarchyLevel = 0);
}