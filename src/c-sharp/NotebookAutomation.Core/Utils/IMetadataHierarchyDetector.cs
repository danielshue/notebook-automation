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
}