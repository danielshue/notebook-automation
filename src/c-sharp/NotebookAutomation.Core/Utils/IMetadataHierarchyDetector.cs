// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Interface for detecting and updating metadata hierarchy information in vault files using schema-driven processing.
/// </summary>
/// <remarks>
/// This interface provides schema-driven hierarchy detection capabilities by integrating with the 
/// <see cref="IMetadataSchemaLoader"/> to provide template mapping, reserved tag injection, and
/// hierarchical metadata processing based on the defined schema.
/// </remarks>
public interface IMetadataHierarchyDetector
{
    /// <summary>
    /// Gets the root path of the notebook vault.
    /// </summary>
    string? VaultRoot { get; }

    /// <summary>
    /// Gets the metadata schema loader instance used by this hierarchy detector.
    /// </summary>
    /// <remarks>
    /// Provides access to the underlying schema loader for template mapping, reserved tag handling,
    /// and hierarchy-based template type resolution.
    /// </remarks>
    IMetadataSchemaLoader SchemaLoader { get; }

    /// <summary>
    /// Updates the provided metadata dictionary with hierarchy information (program, course, class, etc.) using schema-driven processing.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to update.</param>
    /// <param name="hierarchyInfo">The hierarchy information to apply.</param>
    /// <param name="templateType">Optional template type to determine which levels to include.</param>
    /// <returns>The updated metadata dictionary with reserved tags and universal fields from schema.</returns>
    /// <remarks>
    /// Uses the schema loader to inject reserved tags and universal fields based on the template type
    /// and hierarchy level. The hierarchy information is applied according to the schema definition.
    /// </remarks>
    Dictionary<string, object?> UpdateMetadataWithHierarchy(
        Dictionary<string, object?> metadata,
        Dictionary<string, string> hierarchyInfo,
        string? templateType = null);

    /// <summary>
    /// Finds hierarchy information for a given file path with schema-driven template type resolution.
    /// </summary>
    /// <param name="filePath">The file path to analyze.</param>
    /// <returns>A dictionary containing hierarchy information.</returns>
    /// <remarks>
    /// Uses the schema loader to determine appropriate template types based on hierarchy levels
    /// and provides hierarchy information compatible with the schema definition.
    /// </remarks>
    Dictionary<string, string> FindHierarchyInfo(string filePath);

    /// <summary>
    /// Calculates the hierarchy level of a folder relative to the vault root.
    /// </summary>
    /// <param name="folderPath">The folder path to analyze.</param>
    /// <param name="vaultPath">The vault root path. If null, uses the instance VaultRoot.</param>
    /// <returns>The hierarchy level (0 = vault root, 1 = program, 2 = course, 3 = class, 4 = module, 5 = lesson, etc.).</returns>
    int CalculateHierarchyLevel(string folderPath, string? vaultPath = null);

    /// <summary>
    /// Gets the template type based on hierarchy level using schema-driven mapping.
    /// </summary>
    /// <param name="hierarchyLevel">The hierarchy level.</param>
    /// <returns>The template type (main, program, course, class, module, lesson) based on schema mapping.</returns>
    /// <remarks>
    /// Uses the schema loader to determine appropriate template types for hierarchy levels,
    /// ensuring consistency with the schema definition and available templates.
    /// </remarks>
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

    /// <summary>
    /// Injects reserved tags from the schema loader into the metadata based on template type.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to update.</param>
    /// <param name="templateType">The template type for tag injection.</param>
    /// <returns>The updated metadata dictionary with reserved tags injected.</returns>
    /// <remarks>
    /// Uses the schema loader's reserved tags list to inject appropriate tags based on
    /// the template type and hierarchy level. Reserved tags are added according to schema rules.
    /// </remarks>
    Dictionary<string, object?> InjectReservedTags(Dictionary<string, object?> metadata, string templateType);

    /// <summary>
    /// Maps hierarchy level to template type using schema-driven validation.
    /// </summary>
    /// <param name="hierarchyLevel">The hierarchy level to map.</param>
    /// <param name="validateWithSchema">Whether to validate the template type exists in schema.</param>
    /// <returns>The template type mapped from hierarchy level, validated against schema if requested.</returns>
    /// <remarks>
    /// Uses the schema loader to validate that the mapped template type exists in the schema,
    /// providing fallback behavior for unmapped hierarchy levels.
    /// </remarks>
    string MapHierarchyLevelToTemplateType(int hierarchyLevel, bool validateWithSchema = true);
}
