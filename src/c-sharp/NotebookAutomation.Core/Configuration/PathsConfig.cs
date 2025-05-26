namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Configuration for file paths used in the application.
    /// </summary>
    using System.Text.Json.Serialization;

    public class PathsConfig
    {        /// <summary>
             /// Full path to the root directory where OneDrive files are stored locally.
             /// </summary>
        [JsonPropertyName("onedrive_fullpath_root")]
        public string OnedriveFullpathRoot { get; set; } = string.Empty;
        /// <summary>
        /// Full path to the root directory for the notebook vault.
        /// </summary>
        [JsonPropertyName("notebook_vault_fullpath_root")]
        public string NotebookVaultFullpathRoot { get; set; } = string.Empty;

        /// <summary>
        /// Path to the metadata file.
        /// </summary>
        [JsonPropertyName("metadata_file")]
        public string MetadataFile { get; set; } = string.Empty;

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

        /// <summary>
        /// Directory containing prompt template files.
        /// </summary>
        [JsonPropertyName("prompts_path")]
        public string PromptsPath { get; set; } = string.Empty;
    }
}
