// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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
    /// Gets or sets full path to the root directory where OneDrive files are stored locally.
    /// </summary>
    /// <remarks>
    /// This path is used to locate the local storage directory for OneDrive files.
    /// </remarks>
    [JsonPropertyName("onedrive_fullpath_root")]
    public virtual string OnedriveFullpathRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets full path to the root directory for the notebook vault.
    /// </summary>
    /// <remarks>
    /// This path is used to locate the directory where notebook vault files are stored.
    /// This property specifies the name of the folder that should be treated as the main program folder
    /// when generating index files. This folder will have template-type: main and the index will be named
    /// whatever is the folder name.
    /// </remarks>
    [JsonPropertyName("notebook_vault_fullpath_root")]
    public virtual string NotebookVaultFullpathRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets path to the metadata file.
    /// </summary>
    /// <remarks>
    /// The metadata file contains structured information about the application's resources.
    /// </remarks>
    [JsonPropertyName("metadata_file")]
    public virtual string MetadataFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets path to the metadata schema file.
    /// </summary>
    /// <remarks>
    /// The metadata schema file contains the schema definitions for template types, universal fields,
    /// type mappings, and reserved tags used by the metadata automation system.
    /// </remarks>
    [JsonPropertyName("metadata_schema_file")]
    public virtual string MetadataSchemaFile { get; set; } = string.Empty;

    /// <summary>    /// Gets or sets base path for OneDrive resources.
    /// </summary>
    /// <remarks>
    /// This path is used to locate the base directory for OneDrive-related resources.
    /// </remarks>
    [JsonPropertyName("onedrive_resources_basepath")]
    public virtual string OnedriveResourcesBasepath { get; set; } = string.Empty;

    /// <summary>    /// Gets or sets directory for log files.
    /// </summary>
    /// <remarks>
    /// This directory is used to store application log files for debugging and monitoring purposes.
    /// </remarks>
    [JsonPropertyName("logging_dir")]
    public virtual string LoggingDir { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets directory containing prompt template files.
    /// </summary>
    /// <remarks>
    /// This directory is used to store template files for generating prompts in the application.
    /// </remarks>
    [JsonPropertyName("prompts_path")]
    public virtual string PromptsPath { get; set; } = string.Empty;
}
