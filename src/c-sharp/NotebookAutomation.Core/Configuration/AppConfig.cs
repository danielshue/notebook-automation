using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Application configuration for Notebook Automation.
    /// 
    /// This class handles loading and providing access to centralized configuration
    /// settings for the Notebook Automation system, including paths, API settings,
    /// and application defaults.
    /// 
    /// Implements IConfiguration to provide standard configuration access patterns.
    /// </summary>
    public class AppConfig : IConfiguration
    {
        /// <summary>
        /// The path to the configuration file used to load this AppConfig.
        /// </summary>
        public string? ConfigFilePath { get; set; }

        /// <summary>
        /// Whether debug mode is enabled for this configuration.
        /// </summary>
        public bool DebugEnabled { get; set; }

        private readonly ILogger<AppConfig>? _logger;
        private readonly IConfiguration? _underlyingConfiguration;

        /// <summary>
        /// Paths configuration section.
        /// </summary>
        [JsonPropertyName("paths")]
        public PathsConfig Paths { get; set; } = new PathsConfig();

        /// <summary>
        /// Microsoft Graph API configuration section.
        /// </summary>
        [JsonPropertyName("microsoft_graph")]
        public MicrosoftGraphConfig MicrosoftGraph { get; set; } = new MicrosoftGraphConfig();

        /// <summary>
        /// AI Service configuration section.
        /// </summary>
        [JsonPropertyName("aiservice")]
        public AIServiceConfig AiService { get; set; } = new AIServiceConfig();

        /// <summary>
        /// List of video file extensions to process.
        /// </summary>
        [JsonPropertyName("video_extensions")]
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
        /// <param name="configFilePath">The path to the configuration file.</param>
        /// <param name="debugEnabled">Whether debug mode is enabled.</param>
        public AppConfig(IConfiguration configuration, ILogger<AppConfig> logger, string? configFilePath = null, bool debugEnabled = false)
        {
            _underlyingConfiguration = configuration;
            _logger = logger;
            ConfigFilePath = configFilePath;
            DebugEnabled = debugEnabled;
            // Load configuration sections
            LoadConfiguration();
        }
        /// <summary>
        /// Loads configuration from the configuration provider.
        /// </summary>
        private void LoadConfiguration()
        {
            string? loadedConfigPath = null;
            try
            {
                // First, try to load from underlying configuration if available
                if (_underlyingConfiguration != null)
                {
                    // Load paths configuration
                    var pathsSection = _underlyingConfiguration.GetSection("paths"); if (pathsSection.Exists())
                    {
                        Paths = new PathsConfig
                        {
                            NotebookVaultFullpathRoot = pathsSection["notebook_vault_fullpath_root"] ?? string.Empty,
                            OnedriveResourcesBasepath = pathsSection["onedrive_resources_basepath"] ?? string.Empty,
                            LoggingDir = pathsSection["logging_dir"] ?? string.Empty,
                            OnedriveFullpathRoot = pathsSection["onedrive_fullpath_root"] ?? string.Empty,
                            MetadataFile = pathsSection["metadata_file"] ?? string.Empty
                        };
                    }

                    // Load Microsoft Graph configuration
                    var graphSection = _underlyingConfiguration.GetSection("microsoft_graph");
                    if (graphSection.Exists())
                    {
                        MicrosoftGraph = new MicrosoftGraphConfig
                        {
                            ClientId = graphSection["clientId"] ?? string.Empty,
                            TenantId = graphSection["tenantId"] ?? string.Empty,
                            Authority = graphSection["authority"] ?? string.Empty,
                            Scopes = graphSection.GetSection("scopes")
                                .GetChildren()
                                .Select(x => x.Value)
                                .Where(x => !string.IsNullOrEmpty(x))
                                .Select(x => x!) // Suppress nullability and cast to non-nullable string
                                .ToList()
                        };
                    }
                    // Load OpenAI configuration
                    var aiSection = _underlyingConfiguration.GetSection("aiservice");
                    if (aiSection.Exists())
                    {
                        AiService = new AIServiceConfig
                        {
                            Model = aiSection["model"] ?? "gpt-4",
                            Deployment = aiSection["deployment"] ?? string.Empty,
                            Endpoint = aiSection["endpoint"] ?? string.Empty
                        };

                        // Set the configuration for accessing secrets
                        AiService.SetConfiguration(_underlyingConfiguration);
                    }

                    // Load video extensions
                    var videoExtensionsSection = _underlyingConfiguration.GetSection("video_extensions");
                    if (videoExtensionsSection.Exists())
                    {
                        VideoExtensions = videoExtensionsSection
                            .GetChildren()
                            .Select(x => x.Value)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .Select(x => x!) // Suppress nullability and cast to non-nullable string
                            .ToList();
                    }
                }
                else
                {
                    // Fall back to the original file-based configuration loading
                    var configFilePath = FindConfigFile();
                    loadedConfigPath = configFilePath;
                    if (!string.IsNullOrEmpty(configFilePath) && File.Exists(configFilePath))
                    {
                        var json = File.ReadAllText(configFilePath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var loaded = JsonSerializer.Deserialize<AppConfig>(json, options);
                        if (loaded != null)
                        {
                            Paths = loaded.Paths;
                            MicrosoftGraph = loaded.MicrosoftGraph;
                            AiService = loaded.AiService;
                            VideoExtensions = loaded.VideoExtensions;
                        }
                    }
                    else
                    {
                        _logger?.LogWarning($"Config file not found at {configFilePath}");
                    }
                }
                // Prefer the explicit ConfigFilePath property, then loadedConfigPath, then environment variable, then unknown
                string configPathHint = Environment.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG_PATH") ?? string.Empty;
                string configPathToLog = ConfigFilePath ?? loadedConfigPath ?? (!string.IsNullOrEmpty(configPathHint) ? configPathHint : "unknown");
                _logger?.LogInformation($"Configuration loaded successfully - {configPathToLog}, Debug: {DebugEnabled}");
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
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var loaded = JsonSerializer.Deserialize<AppConfig>(json, options);
            if (loaded == null)
            {
                throw new InvalidOperationException($"Failed to deserialize configuration from: {configPath}");
            }
            loaded.ConfigFilePath = configPath;
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

        /// <summary>
        /// Saves the current configuration to the specified JSON file.
        /// </summary>
        /// <param name="configPath">Path where the configuration should be saved.</param>
        /// <exception cref="System.IO.IOException">Thrown when the file cannot be written to.</exception>
        public void SaveToJsonFile(string configPath)
        {
            _logger?.LogInformation($"Saving configuration to {configPath}");

            try
            {
                // Make configPath absolute if it is not already
                if (!Path.IsPathRooted(configPath))
                {
                    var baseDir = Directory.GetCurrentDirectory();
                    configPath = Path.GetFullPath(Path.Combine(baseDir, configPath));
                }

                // Ensure directory exists
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configPath, json);

                _logger?.LogInformation($"Configuration successfully saved to {configPath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error saving configuration to {configPath}");
                throw;
            }
        }

        public void SetVideoExtensions(List<string> list)
        {
            _logger?.LogInformation($"Setting video extensions: {string.Join(", ", list)}");
            VideoExtensions = list ?? new List<string>();
        }

        /// <summary>
        /// Gets a value indicating if this configuration contains the specified key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the configuration contains the specified key, otherwise false.</returns>
        public bool Exists(string key)
        {
            // Check underlying configuration first
            if (_underlyingConfiguration != null && _underlyingConfiguration.GetSection(key).Exists())
            {
                return true;
            }

            // If key contains sections, navigate through them
            if (key.Contains(':'))
            {
                var parts = key.Split(':');
                object? currentObj = this;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    var property = currentObj?.GetType().GetProperties()
                        .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase));

                    if (property == null)
                    {
                        // Try to get by JsonPropertyName attribute
                        property = currentObj?.GetType().GetProperties()
                            .FirstOrDefault(p =>
                            {
                                var attr = p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                                    .FirstOrDefault() as JsonPropertyNameAttribute;
                                return attr != null && string.Equals(attr.Name, part, StringComparison.OrdinalIgnoreCase);
                            });
                    }

                    if (property == null)
                    {
                        return false;
                    }

                    // Last part of the key - property exists
                    if (i == parts.Length - 1)
                    {
                        return true;
                    }

                    // Navigate to next object
                    currentObj = property.GetValue(currentObj);
                    if (currentObj == null)
                    {
                        return false;
                    }
                }

                return false;
            }

            // Check direct property
            var directProperty = GetType().GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

            if (directProperty != null)
            {
                return true;
            }

            // Check by JsonPropertyName attribute
            directProperty = GetType().GetProperties()
                .FirstOrDefault(p =>
                {
                    var attr = p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                        .FirstOrDefault() as JsonPropertyNameAttribute;
                    return attr != null && string.Equals(attr.Name, key, StringComparison.OrdinalIgnoreCase);
                });

            return directProperty != null;
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The configuration sub-section.</returns>
        public IConfigurationSection GetSection(string key)
        {
            // If we have an underlying configuration, use it
            if (_underlyingConfiguration != null)
            {
                return _underlyingConfiguration.GetSection(key);
            }

            // Create a new ConfigurationSection using reflection based on our properties
            return new ConfigurationSection(this, key);
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            // If we have an underlying configuration, use it
            if (_underlyingConfiguration != null)
            {
                return _underlyingConfiguration.GetChildren();
            }

            // Create sections from our properties
            var sections = new List<IConfigurationSection>();
            foreach (var property in GetType().GetProperties().Where(p => p.DeclaringType == typeof(AppConfig)))
            {
                var section = new ConfigurationSection(this, property.Name);
                sections.Add(section);
            }

            return sections;
        }

        /// <summary>
        /// Gets a change token that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A change token.</returns>
        public IChangeToken GetReloadToken()
        {
            // If we have an underlying configuration, use its reload token
            if (_underlyingConfiguration != null)
            {
                return _underlyingConfiguration.GetReloadToken();
            }

            // Otherwise return a non-reloading token
            return new ConfigurationReloadToken();
        }

        #region IConfiguration Implementation

        /// <summary>
        /// Gets or sets a configuration value for the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration value to get or set.</param>
        /// <returns>The configuration value.</returns>
        public string? this[string key]
        {
            get
            {
                // First try to get value from underlying configuration if available
                if (_underlyingConfiguration != null && _underlyingConfiguration[key] != null)
                {
                    return _underlyingConfiguration[key];
                }

                // If key contains sections (colon-separated), navigate through them
                if (key.Contains(':'))
                {
                    var parts = key.Split(':');
                    object? currentObj = this;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        var part = parts[i];

                        // Try to get property
                        var property = currentObj?.GetType().GetProperties()
                            .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase));

                        if (property == null)
                        {
                            // Try to get JsonPropertyName attribute
                            property = currentObj?.GetType().GetProperties()
                                .FirstOrDefault(p =>
                                {
                                    var attr = p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), true)
                                        .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;
                                    return attr != null && string.Equals(attr.Name, part, StringComparison.OrdinalIgnoreCase);
                                });
                        }

                        if (property == null)
                        {
                            return null;
                        }

                        // Last part of the key - return value
                        if (i == parts.Length - 1)
                        {
                            var value = property.GetValue(currentObj);
                            return value?.ToString();
                        }

                        // Navigate to next object
                        currentObj = property.GetValue(currentObj);
                        if (currentObj == null)
                        {
                            return null;
                        }
                    }

                    return null;
                }

                // Direct property access
                var directProperty = GetType().GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

                if (directProperty != null)
                {
                    var value = directProperty.GetValue(this);
                    return value?.ToString();
                }

                // Try to get by JsonPropertyName attribute
                directProperty = GetType().GetProperties()
                    .FirstOrDefault(p =>
                    {
                        var attr = p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), true)
                            .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;
                        return attr != null && string.Equals(attr.Name, key, StringComparison.OrdinalIgnoreCase);
                    });

                if (directProperty != null)
                {
                    var value = directProperty.GetValue(this);
                    return value?.ToString();
                }

                return null;
            }
            set
            {
                // If key contains sections (colon-separated), navigate through them
                if (key.Contains(':'))
                {
                    var parts = key.Split(':');
                    object? currentObj = this;

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        var part = parts[i];

                        var property = currentObj?.GetType().GetProperties()
                            .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase));

                        if (property == null)
                        {
                            // Try to get by JsonPropertyName attribute
                            property = currentObj?.GetType().GetProperties()
                                .FirstOrDefault(p =>
                                {
                                    var attr = p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), true)
                                        .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;
                                    return attr != null && string.Equals(attr.Name, part, StringComparison.OrdinalIgnoreCase);
                                });
                        }

                        if (property == null || !property.CanRead)
                        {
                            return;
                        }

                        var nextObj = property.GetValue(currentObj);
                        if (nextObj == null && property.CanWrite)
                        {
                            // Create a new instance if null
                            nextObj = Activator.CreateInstance(property.PropertyType);
                            property.SetValue(currentObj, nextObj);
                        }

                        currentObj = nextObj;
                    }

                    // Set the value on the final object
                    if (currentObj != null)
                    {
                        var finalPart = parts[parts.Length - 1];
                        var finalProperty = currentObj.GetType().GetProperties()
                            .FirstOrDefault(p => string.Equals(p.Name, finalPart, StringComparison.OrdinalIgnoreCase));

                        if (finalProperty == null)
                        {
                            // Try to get by JsonPropertyName attribute
                            finalProperty = currentObj.GetType().GetProperties()
                                .FirstOrDefault(p =>
                                {
                                    var attr = p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), true)
                                        .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;
                                    return attr != null && string.Equals(attr.Name, finalPart, StringComparison.OrdinalIgnoreCase);
                                });
                        }

                        if (finalProperty != null && finalProperty.CanWrite)
                        {
                            SetPropertyValue(finalProperty, currentObj, value);
                        }
                    }

                    return;
                }

                // Direct property access
                var directProperty = GetType().GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

                if (directProperty == null)
                {
                    // Try to get by JsonPropertyName attribute
                    directProperty = GetType().GetProperties()
                        .FirstOrDefault(p =>
                        {
                            var attr = p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), true)
                                .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;
                            return attr != null && string.Equals(attr.Name, key, StringComparison.OrdinalIgnoreCase);
                        });
                }

                if (directProperty != null && directProperty.CanWrite)
                {
                    SetPropertyValue(directProperty, this, value);
                }
            }
        }

        /// <summary>
        /// Helper method to set a property value with proper type conversion.
        /// </summary>
        private static void SetPropertyValue(System.Reflection.PropertyInfo property, object target, string? value)
        {
            if (property == null || !property.CanWrite)
            {
                return;
            }

            try
            {
                var propertyType = property.PropertyType;

                // Handle nullable types
                var nullableType = Nullable.GetUnderlyingType(propertyType);
                if (nullableType != null)
                {
                    propertyType = nullableType;

                    // If value is null or empty and property is nullable, set to null
                    if (string.IsNullOrEmpty(value))
                    {
                        property.SetValue(target, null);
                        return;
                    }
                }

                // Handle common types
                if (propertyType == typeof(string))
                {
                    property.SetValue(target, value);
                }
                else if (propertyType.IsEnum)
                {
                    if (!string.IsNullOrEmpty(value) && Enum.TryParse(propertyType, value, true, out var enumValue))
                    {
                        property.SetValue(target, enumValue);
                    }
                }
                else if (propertyType == typeof(bool))
                {
                    if (bool.TryParse(value, out var boolValue))
                    {
                        property.SetValue(target, boolValue);
                    }
                }
                else if (propertyType == typeof(int))
                {
                    if (int.TryParse(value, out var intValue))
                    {
                        property.SetValue(target, intValue);
                    }
                }
                else if (propertyType == typeof(double))
                {
                    if (double.TryParse(value, out var doubleValue))
                    {
                        property.SetValue(target, doubleValue);
                    }
                }
                else if (propertyType == typeof(DateTime))
                {
                    if (DateTime.TryParse(value, out var dateValue))
                    {
                        property.SetValue(target, dateValue);
                    }
                }
                else
                {
                    // Try general conversion
                    try
                    {
                        if (value != null)
                        {
                            var convertedValue = Convert.ChangeType(value, propertyType);
                            property.SetValue(target, convertedValue);
                        }
                    }
                    catch
                    {
                        // Failed to convert
                    }
                }
            }
            catch
            {
                // Failed to set value
            }
        }

        #endregion

        /// <summary>
        /// Helper class to implement ConfigurationSection
        /// </summary>
        private class ConfigurationSection : IConfigurationSection
        {
            private readonly IConfiguration _configuration;
            private readonly string _key;
            private readonly string _path;

            public ConfigurationSection(IConfiguration configuration, string key, string? parentPath = null)
            {
                _configuration = configuration;
                _key = key;
                _path = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}:{key}";
            }

            public string Key => _key;
            public string Path => _path;
            public string? Value
            {
                get => _configuration[_path];
                set => _configuration[_path] = value;
            }

            public string? this[string key]
            {
                get => _configuration[$"{_path}:{key}"];
                set => _configuration[$"{_path}:{key}"] = value;
            }

            public IConfigurationSection GetSection(string key) => new ConfigurationSection(_configuration, key, _path);

            public IEnumerable<IConfigurationSection> GetChildren()
            {
                // Get properties of the object this section represents
                var children = new List<IConfigurationSection>();

                // Check for properties related to this path
                foreach (var propKey in GetPropertyKeys())
                {
                    children.Add(new ConfigurationSection(_configuration, propKey, _path));
                }

                return children;
            }

            // Helper method to find potential property keys
            private IEnumerable<string> GetPropertyKeys()
            {
                // This is a simplified implementation
                // In a real scenario, you would query the underlying configuration 
                // to get the actual child keys
                return new List<string>();
            }

            public IChangeToken GetReloadToken() => _configuration.GetReloadToken();
        }
    }
}
