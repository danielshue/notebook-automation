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
    [JsonPropertyName("onedrive_fullpath_root")]
    public virtual string OnedriveFullpathRoot { get; set; } = string.Empty;

    [JsonPropertyName("notebook_vault_fullpath_root")]
    public virtual string NotebookVaultFullpathRoot { get; set; } = string.Empty;

    [Obsolete("Use MetadataSchemaFile instead. This property will be removed in a future version.")]
    [JsonPropertyName("metadata_file")]
    public virtual string MetadataFile { get; set; } = string.Empty;

    [JsonPropertyName("metadata_schema_file")]
    public virtual string MetadataSchemaFile { get; set; } = string.Empty;

    [JsonPropertyName("onedrive_resources_basepath")]
    public virtual string OnedriveResourcesBasepath { get; set; } = string.Empty;

    [JsonPropertyName("logging_dir")]
    public virtual string LoggingDir { get; set; } = string.Empty;

    [JsonPropertyName("prompts_path")]
    public virtual string PromptsPath { get; set; } = string.Empty;

    [JsonPropertyName("base_block_template_filename")]
    public virtual string BaseBlockTemplateFilename { get; set; } = "BaseBlockTemplate.yml";
}
