namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Configuration for file paths used in the application.
    /// </summary>
    using System.Text.Json.Serialization;

    public class PathsConfig
    {
        /// <summary>
        /// Root directory for resources.
        /// </summary>
        [JsonPropertyName("resources_root")]
        public string ResourcesRoot { get; set; } = string.Empty;
        
        /// <summary>
        /// Root directory for the notebook vault.
        /// </summary>
        [JsonPropertyName("notebook_vault_root")]
        public string NotebookVaultRoot { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to the metadata file.
        /// </summary>
        [JsonPropertyName("metadata_file")]
        public string MetadataFile { get; set; } = string.Empty;
        
        /// <summary>
        /// Root directory for the Obsidian vault.
        /// </summary>
        [JsonPropertyName("obsidian_vault_root")]
        public string ObsidianVaultRoot { get; set; } = string.Empty;
        
        /// <summary>
        /// Base path for OneDrive resources.
        /// </summary>
        [JsonPropertyName("onedrive_resources_basepath")]
        public string OnedriveResourcesBasepath { get; set; } = string.Empty;
        
        /// <summary>
        /// Directory for log files.
        /// </summary>
        [JsonPropertyName("logging_dir")]
        public string LoggingDir { get; set; } = string.Empty;
    }
}
