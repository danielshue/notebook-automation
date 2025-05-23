namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Configuration for file paths used in the application.
    /// </summary>
    public class PathsConfig
    {
        /// <summary>
        /// Root directory for resources.
        /// </summary>
        public string ResourcesRoot { get; set; } = string.Empty;
        
        /// <summary>
        /// Root directory for the notebook vault.
        /// </summary>
        public string NotebookVaultRoot { get; set; } = string.Empty;
        
        /// <summary>
        /// Path to the metadata file.
        /// </summary>
        public string MetadataFile { get; set; } = string.Empty;
        
        /// <summary>
        /// Root directory for the Obsidian vault.
        /// </summary>
        public string ObsidianVaultRoot { get; set; } = string.Empty;
        
        /// <summary>
        /// Base path for OneDrive resources.
        /// </summary>
        public string OnedriveResourcesBasepath { get; set; } = string.Empty;
        
        /// <summary>
        /// Directory for log files.
        /// </summary>
        public string LoggingDir { get; set; } = string.Empty;
    }
}
