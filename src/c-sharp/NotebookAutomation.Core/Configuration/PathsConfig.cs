// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.Json.Serialization;
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the configuration settings for various file paths used in the application.
/// </summary>
/// <remarks>
/// This class encapsulates paths for directories and files that are essential for the application's
/// operation, including paths for OneDrive resources, notebook vaults, metadata files, logging,
/// and prompt templates. It is designed to be serialized and deserialized from JSON configuration files.
/// </remarks>
public class PathsConfig
{
    /// <summary>
    /// Gets or sets the full path to the root directory of OneDrive storage.
    /// </summary>
    /// <value>
    /// The absolute path to the OneDrive root directory (e.g., "C:\Users\username\OneDrive\").
    /// This path serves as the base for all OneDrive-related file operations.
    /// </value>
    /// <remarks>
    /// This property is mapped from the "onedrive_fullpath_root" JSON configuration key.
    /// The path should include a trailing directory separator for consistency.
    /// </remarks>
    [JsonPropertyName("onedrive_fullpath_root")]
    public virtual string OnedriveFullpathRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path within OneDrive where educational resources are stored.
    /// </summary>
    /// <value>
    /// A relative path from the OneDrive root to the educational resources directory
    /// (e.g., "Education\MBA-Resources").
    /// </value>
    /// <remarks>
    /// This property is mapped from the "onedrive_resources_basepath" JSON configuration key.
    /// This path is combined with OnedriveFullpathRoot to create the complete path to resources.
    /// </remarks>
    [JsonPropertyName("onedrive_resources_basepath")]
    public virtual string OnedriveResourcesBasepath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path to the root directory of the Obsidian notebook vault.
    /// </summary>
    /// <value>
    /// The absolute path to the Obsidian vault root directory 
    /// (e.g., "D:\source\notebook-automation\tests\obsidian-vault\Obsidian Vault Test").
    /// </value>
    /// <remarks>
    /// This property is mapped from the "notebook_vault_fullpath_root" JSON configuration key.
    /// This serves as the base directory for all vault-related operations.
    /// </remarks>
    [JsonPropertyName("notebook_vault_fullpath_root")]
    public virtual string NotebookVaultFullpathRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path within the notebook vault where resources are organized.
    /// </summary>
    /// <value>
    /// A relative path from the vault root to the resources directory 
    /// (e.g., "01_Projects\MBA").
    /// </value>
    /// <remarks>
    /// This property is mapped from the "notebook_vault_resources_basepath" JSON configuration key.
    /// This path is combined with NotebookVaultFullpathRoot to create the complete path to vault resources.
    /// Used for organizing content within the vault structure.
    /// </remarks>
    [JsonPropertyName("notebook_vault_resources_basepath")]
    public virtual string NotebookVaultResourcesBasepath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the metadata file for backward compatibility.
    /// </summary>
    /// <value>
    /// The path to the legacy metadata file.
    /// </value>
    /// <remarks>
    /// This property is obsolete and will be removed in a future version.
    /// Use <see cref="MetadataSchemaFile"/> instead.
    /// Mapped from the "metadata_file" JSON configuration key.
    /// </remarks>
    [Obsolete("Use MetadataSchemaFile instead. This property will be removed in a future version.")]
    [JsonPropertyName("metadata_file")]
    public virtual string MetadataFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path to the metadata schema file.
    /// </summary>
    /// <value>
    /// The absolute path to the YAML metadata schema file 
    /// (e.g., "D:\source\notebook-automation\config\metadata-schema.yml").
    /// </value>
    /// <remarks>
    /// This property is mapped from the "metadata_schema_file" JSON configuration key.
    /// The schema file defines the structure and validation rules for notebook metadata.
    /// </remarks>
    [JsonPropertyName("metadata_schema_file")]
    public virtual string MetadataSchemaFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory path where application logs are stored.
    /// </summary>
    /// <value>
    /// The absolute path to the logging directory 
    /// (e.g., "D:\source\notebook-automation\logs").
    /// </value>
    /// <remarks>
    /// This property is mapped from the "logging_dir" JSON configuration key.
    /// The directory will be created if it doesn't exist when logging is initialized.
    /// </remarks>
    [JsonPropertyName("logging_dir")]
    public virtual string LoggingDir { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory path where AI prompt templates are stored.
    /// </summary>
    /// <value>
    /// The absolute path to the prompts directory 
    /// (e.g., "D:\source\notebook-automation\prompts").
    /// </value>
    /// <remarks>
    /// This property is mapped from the "prompts_path" JSON configuration key.
    /// Contains template files used for AI service interactions and content generation.
    /// </remarks>
    [JsonPropertyName("prompts_path")]
    public virtual string PromptsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filename of the base block template used for content generation.
    /// </summary>
    /// <value>
    /// The filename of the base template file (default: "BaseBlockTemplate.yml").
    /// </value>
    /// <remarks>
    /// This property is mapped from the "base_block_template_filename" JSON configuration key.
    /// The template file defines the structure for generating content blocks in notebooks.
    /// If not specified in configuration, defaults to "BaseBlockTemplate.yml".
    /// </remarks>
    [JsonPropertyName("base_block_template_filename")]
    public virtual string BaseBlockTemplateFilename { get; set; } = "BaseBlockTemplate.yml";

    /// <summary>
    /// Gets the effective vault root path by combining the vault root with the resources base path.
    /// </summary>
    /// <returns>
    /// The combined path of NotebookVaultFullpathRoot and NotebookVaultResourcesBasepath,
    /// or just the vault root if no resources base path is configured.
    /// Returns empty string if vault root is not configured.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides a single, consistent way to calculate the effective vault root
    /// throughout the application. It handles path normalization and ensures consistent
    /// directory separator usage across platforms.
    /// </para>
    /// <para>
    /// Examples:
    /// - NotebookVaultFullpathRoot: "C:\vault", NotebookVaultResourcesBasepath: "01_Projects\MBA"
    ///   Result: "C:\vault\01_Projects\MBA"
    /// - NotebookVaultFullpathRoot: "C:\vault", NotebookVaultResourcesBasepath: ""
    ///   Result: "C:\vault"
    /// - NotebookVaultFullpathRoot: "", NotebookVaultResourcesBasepath: "01_Projects\MBA"
    ///   Result: ""
    /// </para>
    /// </remarks>
    public virtual string GetEffectiveVaultRoot()
    {
        if (string.IsNullOrEmpty(NotebookVaultFullpathRoot))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(NotebookVaultResourcesBasepath))
        {
            return NotebookVaultFullpathRoot;
        }

        // Normalize the resources base path by removing leading and trailing separators and converting to platform separators
        string normalizedResourcesPath = NotebookVaultResourcesBasepath
            .Trim('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        return Path.Combine(NotebookVaultFullpathRoot, normalizedResourcesPath);
    }
}
