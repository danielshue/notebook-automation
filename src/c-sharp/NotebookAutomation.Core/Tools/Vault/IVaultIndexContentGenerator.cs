// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Interface for generating comprehensive index content for vault files with intelligent hierarchy-based organization.
/// </summary>
/// <remarks>
/// <para>
/// The IVaultIndexContentGenerator defines the contract for creating rich, structured index content
/// that adapts to different hierarchy levels within an Obsidian vault. This interface enables
/// separation of content generation logic from file management operations.
/// </para>
/// <para>
/// Key Responsibilities:
/// </para>
/// <list type="bullet">
/// <item><description>Generate frontmatter with hierarchy-aware metadata</description></item>
/// <item><description>Create contextual navigation based on vault structure</description></item>
/// <item><description>Organize content by type and hierarchy level</description></item>
/// <item><description>Integrate Obsidian Bases blocks for advanced querying</description></item>
/// <item><description>Provide consistent formatting and visual organization</description></item>
/// </list>
/// <para>
/// Implementation Considerations:
/// Implementations should handle different hierarchy levels appropriately, generate proper
/// Obsidian-compatible markdown, and maintain consistency with vault navigation patterns.
/// </para>
/// </remarks>
public interface IVaultIndexContentGenerator
{
    /// <summary>
    /// Generates comprehensive index content for a vault folder with intelligent hierarchy detection and content organization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates a complete index document including frontmatter, navigation, and content sections
    /// tailored to the specific hierarchy level and content types present in the folder.
    /// </para>
    /// <para>
    /// Content Generation Strategy:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Applies hierarchy-specific metadata and templates</description></item>
    /// <item><description>Generates contextual navigation for different levels</description></item>
    /// <item><description>Organizes content by type (readings, videos, assignments, etc.)</description></item>
    /// <item><description>Integrates Obsidian Bases blocks for class-level indices</description></item>
    /// <item><description>Creates consistent visual hierarchy with icons and formatting</description></item>
    /// </list>
    /// </remarks>
    /// <param name="folderPath">
    /// The absolute path to the folder for which to generate index content.
    /// Used for content discovery and hierarchy calculation.
    /// </param>
    /// <param name="vaultPath">
    /// The absolute path to the vault root directory for navigation context.
    /// Used for generating proper home links and relative navigation.
    /// </param>
    /// <param name="template">
    /// The metadata template dictionary containing frontmatter structure and defaults.
    /// Will be cloned and enhanced with hierarchy-specific metadata.
    /// </param>
    /// <param name="files">
    /// The list of analyzed vault files to include in the index.
    /// Files are categorized by content type for organized presentation.
    /// </param>
    /// <param name="hierarchyInfo">
    /// Dictionary containing hierarchy-specific metadata (program, course, class, module).
    /// Used for template population and Bases block generation.
    /// </param>
    /// <param name="hierarchyLevel">
    /// The 1-based hierarchy level for template selection and content organization.
    /// Determines navigation style and content sections.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous content generation operation.
    /// The task result contains the complete markdown content ready for file output.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when required parameters are null or invalid.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when required template files (like BaseBlockTemplate.yaml) cannot be located.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when content generation fails due to invalid hierarchy information or template processing errors.
    /// </exception>
    Task<string> GenerateIndexContentAsync(
        string folderPath,
        string vaultPath,
        Dictionary<string, object> template,
        List<VaultFileInfo> files,
        Dictionary<string, string> hierarchyInfo,
        int hierarchyLevel);
}
