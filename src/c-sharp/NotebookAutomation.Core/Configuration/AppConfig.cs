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
        public PathsConfig Paths { get; private set; } = new PathsConfig();

        /// <summary>
        /// Microsoft Graph API configuration section.
        /// </summary>
        public MicrosoftGraphConfig MicrosoftGraph { get; private set; } = new MicrosoftGraphConfig();

        /// <summary>
        /// OpenAI API configuration section.
        /// </summary>
        public OpenAiConfig OpenAi { get; private set; } = new OpenAiConfig();

        /// <summary>
        /// List of video file extensions to process.
        /// </summary>
        public List<string> VideoExtensions { get; private set; } = new List<string>();

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
                if (_configuration == null)
                {
                    _logger?.LogWarning("Configuration is null, using default values");
                    return;
                }
                
                // Load paths section
                _configuration.GetSection("paths").Bind(Paths);
                
                // Load Microsoft Graph section
                _configuration.GetSection("microsoft_graph").Bind(MicrosoftGraph);
                
                // Load OpenAI section
                _configuration.GetSection("openAi").Bind(OpenAi);
                
                // Load video extensions
                VideoExtensions = _configuration.GetSection("video_extensions").Get<List<string>>() ?? 
                                  new List<string> { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".mpg", ".mpeg", ".m4v" };
                
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
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .Build();
                
            var loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<AppConfig>();
            
            return new AppConfig(configuration, logger);
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
