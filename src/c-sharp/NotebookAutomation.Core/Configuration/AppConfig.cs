using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Application configuration for Notebook Automation.
    /// 
    /// This class handles loading and providing access to centralized configuration
    /// settings for the Notebook Automation system, including paths, API settings,
    /// and application defaults.
    /// </summary>
    public class AppConfig
    {
        private readonly ILogger<AppConfig>? _logger;
        private readonly IConfiguration? _configuration;

        /// <summary>
        /// Paths configuration section.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("paths")]
        public PathsConfig Paths { get; set; } = new PathsConfig();

        /// <summary>
        /// Microsoft Graph API configuration section.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("microsoft_graph")]
        public MicrosoftGraphConfig MicrosoftGraph { get; set; } = new MicrosoftGraphConfig();

        /// <summary>
        /// OpenAI API configuration section.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("openai")]
        public OpenAiConfig OpenAi { get; set; } = new OpenAiConfig();

        /// <summary>
        /// List of video file extensions to process.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("video_extensions")]
        public List<string> VideoExtensions { get; set; } = new List<string>();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AppConfig() 
        {
            // Default constructor for when manual initialization is needed
        }

        /// <summary>
        /// Constructor with dependency injection for configuration and logging.
        /// </summary>
        /// <param name="configuration">The configuration to use.</param>
        /// <param name="logger">The logger to use.</param>
        public AppConfig(IConfiguration configuration, ILogger<AppConfig> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Load configuration sections
            LoadConfiguration();
        }
        
        /// <summary>
        /// Loads configuration from the configuration provider.
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Use System.Text.Json to deserialize the config file for snake_case compatibility
                var configFilePath = FindConfigFile();
                if (!string.IsNullOrEmpty(configFilePath) && File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var loaded = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json, options);
                    if (loaded != null)
                    {
                        this.Paths = loaded.Paths;
                        this.MicrosoftGraph = loaded.MicrosoftGraph;
                        this.OpenAi = loaded.OpenAi;
                        this.VideoExtensions = loaded.VideoExtensions;
                    }
                }
                else
                {
                    _logger?.LogWarning($"Config file not found at {configFilePath}");
                }
                _logger?.LogDebug("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading configuration");
                throw;
            }
        }

        /// <summary>
        /// Loads configuration from the specified JSON file.
        /// </summary>
        /// <param name="configPath">Path to the configuration JSON file.</param>
        /// <returns>The loaded AppConfig instance.</returns>
        public static AppConfig LoadFromJsonFile(string configPath)
        {
            // Make configPath absolute if it is not already
            if (!Path.IsPathRooted(configPath))
            {
                var baseDir = Directory.GetCurrentDirectory();
                configPath = Path.GetFullPath(Path.Combine(baseDir, configPath));
            }
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }
            var json = File.ReadAllText(configPath);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json, options);
            if (loaded == null)
            {
                throw new InvalidOperationException($"Failed to deserialize configuration from: {configPath}");
            }
            return loaded;
        }
        
        /// <summary>
        /// Attempts to find the configuration file in standard locations.
        /// </summary>
        /// <param name="configFileName">Name of the configuration file to find.</param>
        /// <returns>Path to the configuration file if found, otherwise null.</returns>
        public static string FindConfigFile(string configFileName = "config.json")
        {
            // Check for absolute path first
            if (Path.IsPathRooted(configFileName) && File.Exists(configFileName))
            {
                return configFileName;
            }
            
            // Standard locations to check
            var locations = new[]
            {
                // Current directory
                Path.Combine(Directory.GetCurrentDirectory(), configFileName),
                
                // User's home directory
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".notebook-automation", configFileName),
                
                // Application directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName),
                
                // Parent directory of application
                Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName ?? string.Empty, configFileName),
                
                // Config directory in current directory
                Path.Combine(Directory.GetCurrentDirectory(), "config", configFileName),
                
                // Source directory for development environment
                Path.Combine(Directory.GetCurrentDirectory(), "src", "c-sharp", configFileName)
            };
            
            foreach (var path in locations)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
            
            return string.Empty; // Return empty string instead of null
        }

        public void SaveToJsonFile(string v)
        {   // Implement saving logic here if needed
            // This is a placeholder for the save functionality
            _logger?.LogInformation($"Saving configuration to {v}");

        }

        public void SetVideoExtensions(List<string> list)
        {
            _logger?.LogInformation($"Setting video extensions: {string.Join(", ", list)}");
            VideoExtensions = list ?? new List<string>();
        }
    }
}
