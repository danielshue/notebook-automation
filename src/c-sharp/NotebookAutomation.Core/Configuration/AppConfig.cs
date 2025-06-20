// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Represents the application configuration for Notebook Automation.
/// </summary>
/// <remarks>
/// This class handles loading and providing access to centralized configuration
/// settings for the Notebook Automation system, including paths, API settings,
/// and application defaults.
/// </remarks>
public class AppConfig : IConfiguration
{
    /// <summary>
    /// The logger instance for logging configuration-related messages.
    /// </summary>
    private readonly ILogger<AppConfig>? logger;

    /// <summary>
    /// The underlying configuration provider.
    /// </summary>
    private readonly IConfiguration? underlyingConfiguration;

    /// <summary>
    /// Gets or sets the path to the configuration file used to load this AppConfig.
    /// </summary>
    public virtual string? ConfigFilePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether debug mode is enabled for this configuration.
    /// </summary>
    public virtual bool DebugEnabled { get; set; }

    /// <summary>
    /// Gets or sets the paths configuration section.
    /// </summary>
    [JsonPropertyName("paths")]
    public virtual PathsConfig Paths { get; set; } = new PathsConfig();

    /// <summary>
    /// Gets or sets the Microsoft Graph API configuration section.
    /// </summary>
    [JsonPropertyName("microsoft_graph")]
    public virtual MicrosoftGraphConfig MicrosoftGraph { get; set; } = new MicrosoftGraphConfig();

    /// <summary>
    /// Gets or sets the AI Service configuration section.
    /// </summary>
    [JsonPropertyName("aiservice")]
    public virtual AIServiceConfig AiService { get; set; } = new AIServiceConfig();

    /// <summary>
    /// Gets or sets the list of video file extensions to process.
    /// </summary>
    [JsonPropertyName("video_extensions")]
    public virtual List<string> VideoExtensions { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of PDF file extensions to process.
    /// </summary>
    [JsonPropertyName("pdf_extensions")]
    public virtual List<string> PdfExtensions { get; set; } = [".pdf"];

    /// <summary>
    /// Gets or sets a value indicating whether images should be extracted from PDFs by default.
    /// </summary>
    [JsonPropertyName("pdf_extract_images")]
    public virtual bool PdfExtractImages { get; set; } = false;

    /// <summary>
    /// Gets or sets the banner configuration for generated markdown files.
    /// </summary>
    [JsonPropertyName("banners")]
    public virtual BannerConfig Banners { get; set; } = new BannerConfig();

    /// <summary>
    /// Initializes a new instance of the <see cref="AppConfig"/> class.
    /// Default constructor for manual initialization.
    /// </summary>
    /// <remarks>
    /// This constructor is used when manual initialization is required without dependency injection.
    /// Configuration must be loaded manually using the LoadConfiguration method or by setting properties directly.
    /// </remarks>
    public AppConfig()
    {
        // Default constructor for when manual initialization is needed
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppConfig"/> class with dependency injection.
    /// Constructor with dependency injection for configuration and logging.
    /// </summary>
    /// <param name="configuration">The configuration provider to use for loading settings.</param>
    /// <param name="logger">The logger instance for logging configuration operations.</param>
    /// <param name="configFilePath">Optional path to the configuration file that was loaded.</param>
    /// <param name="debugEnabled">Indicates whether debug mode is enabled for enhanced logging.</param>
    /// <remarks>
    /// This constructor automatically loads configuration from the provided IConfiguration instance.
    /// It's the preferred constructor when using dependency injection in the application.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when configuration or logger is null.</exception>
    public AppConfig(IConfiguration configuration, ILogger<AppConfig> logger, string? configFilePath = null, bool debugEnabled = false)
    {
        underlyingConfiguration = configuration;
        this.logger = logger;
        ConfigFilePath = configFilePath;
        DebugEnabled = debugEnabled;

        // Load configuration sections
        LoadConfiguration();
    }

    /// <summary>
    /// Loads configuration from the configuration provider.
    /// </summary>
    /// <remarks>
    /// This method attempts to load configuration settings from the underlying configuration provider.
    /// If the provider is unavailable, it falls back to file-based configuration loading using the
    /// ConfigurationSetup.DiscoverConfigurationFile method to locate the configuration file.
    /// The method populates all configuration sections including paths, Microsoft Graph settings,
    /// AI service configurations, and file extension lists.
    /// </remarks>
    /// <exception cref="Exception">Thrown when configuration loading fails due to file access issues or JSON parsing errors.</exception>
    internal virtual void LoadConfiguration()
    {
        string? loadedConfigPath = null;
        try
        {
            // First, try to load from underlying configuration if available
            if (underlyingConfiguration != null)
            {
                // Load paths configuration
                var pathsSection = underlyingConfiguration.GetSection("paths");
                if (pathsSection.Exists())
                {
                    Paths = new PathsConfig
                    {
                        NotebookVaultFullpathRoot = pathsSection["notebook_vault_fullpath_root"] ?? string.Empty,
                        OnedriveResourcesBasepath = pathsSection["onedrive_resources_basepath"] ?? string.Empty,
                        LoggingDir = pathsSection["logging_dir"] ?? string.Empty,
                        OnedriveFullpathRoot = pathsSection["onedrive_fullpath_root"] ?? string.Empty,
                        MetadataFile = pathsSection["metadata_file"] ?? string.Empty,
                    };
                } // Load Microsoft Graph configuration

                var graphSection = underlyingConfiguration.GetSection("microsoft_graph");
                if (graphSection.Exists())
                {
                    MicrosoftGraph = new MicrosoftGraphConfig
                    {
                        ClientId = graphSection["client_id"] ?? string.Empty,
                        ApiEndpoint = graphSection["api_endpoint"] ?? string.Empty,
                        Authority = graphSection["authority"] ?? string.Empty,
                        Scopes = [.. graphSection.GetSection("scopes")
                            .GetChildren()
                            .Select(x => x.Value)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .Select(x => x!)],

                        // Extract tenant ID from authority URL if needed
                        TenantId = ExtractTenantIdFromAuthority(graphSection["authority"] ?? string.Empty),
                    };
                }

                // Load OpenAI configuration
                var aiSection = underlyingConfiguration.GetSection("aiservice");
                if (aiSection.Exists())
                {
                    AiService = new AIServiceConfig
                    {
                        Provider = aiSection["provider"] ?? "openai",
                        OpenAI = new OpenAiProviderConfig
                        {
                            Model = aiSection.GetSection("openai")["model"] ?? aiSection["model"] ?? "gpt-4o",
                            Endpoint = aiSection.GetSection("openai")["endpoint"] ?? aiSection["endpoint"] ?? string.Empty,
                        },
                        Azure = new AzureProviderConfig
                        {
                            Model = aiSection.GetSection("azure")["model"] ?? string.Empty,
                            Deployment = aiSection.GetSection("azure")["deployment"] ?? string.Empty,
                            Endpoint = aiSection.GetSection("azure")["endpoint"] ?? string.Empty,
                        },
                        Foundry = new FoundryProviderConfig
                        {
                            Model = aiSection.GetSection("foundry")["model"] ?? string.Empty,
                            Endpoint = aiSection.GetSection("foundry")["endpoint"] ?? string.Empty,
                        },
                    };
                } // Load video extensions

                var videoExtensionsSection = underlyingConfiguration.GetSection("video_extensions");
                if (videoExtensionsSection.Exists())
                {
                    VideoExtensions = [.. videoExtensionsSection
                        .GetChildren()
                        .Select(x => x.Value)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => x!)];
                }

                // Load PDF extensions
                var pdfExtensionsSection = underlyingConfiguration.GetSection("pdf_extensions");
                if (pdfExtensionsSection.Exists())
                {
                    PdfExtensions = [.. pdfExtensionsSection
                        .GetChildren()
                        .Select(x => x.Value)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => x!)];
                }
            }
            else
            {
                // Fall back to the original file-based configuration loading
                var configFilePath = ConfigurationSetup.DiscoverConfigurationFile();
                loadedConfigPath = configFilePath;
                if (!string.IsNullOrEmpty(configFilePath) && File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    var loaded = JsonSerializer.Deserialize<AppConfig>(json, options);
                    if (loaded != null)
                    {
                        Paths = loaded.Paths;
                        MicrosoftGraph = loaded.MicrosoftGraph;
                        AiService = loaded.AiService;
                        VideoExtensions = loaded.VideoExtensions;
                        PdfExtensions = loaded.PdfExtensions;
                    }
                }
                else
                {
                    logger?.LogWarning($"Config file not found at {configFilePath}");
                }
            }

            // Prefer the explicit ConfigFilePath property, then loadedConfigPath, then environment variable, then unknown
            string configPathHint = Environment.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG_PATH") ?? string.Empty;
            string configPathToLog = ConfigFilePath ?? loadedConfigPath ?? (!string.IsNullOrEmpty(configPathHint) ? configPathHint : "unknown");
            logger?.LogInformation($"Configuration loaded successfully - {configPathToLog}, Debug: {DebugEnabled}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error loading configuration");
            throw;
        }
    }

    /// <summary>
    /// Extracts tenant ID from authority URL or returns "common" for multi-tenant scenarios.
    /// </summary>
    /// <param name="authority">The authority URL (e.g., "https://login.microsoftonline.com/common").</param>
    /// <returns>Tenant ID extracted from URL, "common" for multi-tenant, or empty string if not found.</returns>
    private static string ExtractTenantIdFromAuthority(string authority)
    {
        if (string.IsNullOrEmpty(authority))
        {
            return "common"; // Default for multi-tenant scenarios
        }

        try
        {
            var uri = new Uri(authority);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length > 0)
            {
                var lastSegment = segments[^1];

                // If it's "common" or a GUID, return it
                if (lastSegment.Equals("common", StringComparison.OrdinalIgnoreCase) ||
                    Guid.TryParse(lastSegment, out _))
                {
                    return lastSegment;
                }
            }
        }
        catch (Exception)
        {
            // If URL parsing fails, fall back to common
        }

        return "common"; // Default fallback for multi-tenant scenarios
    }

    /// <summary>
    /// Loads configuration from the specified JSON file.
    /// </summary>
    /// <param name="configPath">Path to the configuration JSON file. Can be relative or absolute.</param>
    /// <returns>The loaded AppConfig instance with ConfigFilePath set to the absolute path.</returns>
    /// <remarks>
    /// This static method provides a convenient way to load configuration directly from a JSON file
    /// without requiring dependency injection. If a relative path is provided, it will be resolved
    /// relative to the current working directory. The returned instance will have its ConfigFilePath
    /// property set to the absolute path of the loaded file.
    /// </remarks>
    /// <exception cref="FileNotFoundException">Thrown when the specified configuration file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when JSON deserialization fails.</exception>
    /// <exception cref="JsonException">Thrown when the JSON file contains invalid syntax.</exception>
    /// <example>
    /// <code>
    /// var config = AppConfig.LoadFromJsonFile("config.json");
    /// var config2 = AppConfig.LoadFromJsonFile(@"C:\MyApp\config.json");
    /// </code>
    /// </example>
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
            PropertyNameCaseInsensitive = true,
        };
        var loaded = JsonSerializer.Deserialize<AppConfig>(json, options) ?? throw new InvalidOperationException($"Failed to deserialize configuration from: {configPath}");
        loaded.ConfigFilePath = configPath;
        return loaded;
    }

    /// <summary>
    /// Saves the current configuration to the specified JSON file.
    /// </summary>
    /// <param name="configPath">Path where the configuration should be saved. Can be relative or absolute.</param>
    /// <remarks>
    /// This method serializes the current configuration instance to JSON format and saves it to the specified file.
    /// If a relative path is provided, it will be resolved relative to the current working directory.
    /// The target directory will be created if it doesn't exist. The JSON output is formatted with indentation
    /// for readability and null values are excluded from the output.
    /// </remarks>
    /// <exception cref="IOException">Thrown when the file cannot be written to due to permissions or disk space issues.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the file path is denied.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the target directory cannot be created.</exception>
    /// <example>
    /// <code>
    /// var config = new AppConfig();
    /// config.SaveToJsonFile("config.json");
    /// config.SaveToJsonFile(@"C:\MyApp\config.json");
    /// </code>
    /// </example>
    public virtual void SaveToJsonFile(string configPath)
    {
        logger?.LogInformation($"Saving configuration to {configPath}");

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
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(configPath, json);

            logger?.LogInformation($"Configuration successfully saved to {configPath}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"Error saving configuration to {configPath}");
            throw;
        }
    }

    /// <summary>
    /// Sets the video file extensions to process.
    /// </summary>
    /// <param name="list">List of video file extensions (e.g., [".mp4", ".avi", ".mkv"]). Null values are converted to empty list.</param>
    /// <remarks>
    /// This method updates the VideoExtensions property with the provided list of file extensions.
    /// The extensions should include the leading dot (e.g., ".mp4" not "mp4"). If null is passed,
    /// an empty list will be assigned. The operation is logged for debugging purposes.
    /// </remarks>
    /// <example>
    /// <code>
    /// config.SetVideoExtensions([".mp4", ".avi", ".mkv", ".mov"]);
    /// </code>
    /// </example>
    public void SetVideoExtensions(List<string> list)
    {
        logger?.LogInformation($"Setting video extensions: {string.Join(", ", list)}");
        VideoExtensions = list ?? [];
    }    /// <summary>
         /// Sets the PDF file extensions to process.
         /// </summary>
         /// <param name="list">List of PDF file extensions (typically just [".pdf"]). Null values default to [".pdf"].</param>
         /// <remarks>
         /// This method updates the PdfExtensions property with the provided list of file extensions.
         /// The extensions should include the leading dot (e.g., ".pdf" not "pdf"). If null is passed,
         /// the default value [".pdf"] will be assigned. The operation is logged for debugging purposes.
         /// </remarks>
         /// <example>
         /// <code>
         /// config.SetPdfExtensions([".pdf"]);
         /// </code>
         /// </example>
    public void SetPdfExtensions(List<string> list)
    {
        logger?.LogInformation($"Setting PDF extensions: {string.Join(", ", list)}");
        PdfExtensions = list ?? [".pdf"];
    }

    /// <summary>
    /// Gets a value indicating if this configuration contains the specified key.
    /// </summary>
    /// <param name="key">The configuration key to check. Can use colon-separated syntax for nested properties (e.g., "paths:logging_dir").</param>
    /// <returns>True if the configuration contains the specified key, otherwise false.</returns>
    /// <remarks>
    /// This method first checks the underlying configuration provider if available. For nested keys,
    /// it supports colon-separated syntax to navigate through configuration sections. The method also
    /// checks for properties using both direct property names and JsonPropertyName attributes for
    /// case-insensitive matching.
    /// </remarks>
    /// <example>
    /// <code>
    /// bool hasLoggingDir = config.Exists("paths:logging_dir");
    /// bool hasDebugMode = config.Exists("DebugEnabled");
    /// bool hasAiService = config.Exists("aiservice");
    /// </code>
    /// </example>
    public bool Exists(string key)
    {
        // Check underlying configuration first
        if (underlyingConfiguration != null && underlyingConfiguration.GetSection(key).Exists())
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
                    .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase)) ?? currentObj?.GetType().GetProperties()
                        .FirstOrDefault(p =>
                        {
                            return p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                                .FirstOrDefault() is JsonPropertyNameAttribute attr && string.Equals(attr.Name, part, StringComparison.OrdinalIgnoreCase);
                        });
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
                return p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                    .FirstOrDefault() is JsonPropertyNameAttribute attr && string.Equals(attr.Name, key, StringComparison.OrdinalIgnoreCase);
            });

        return directProperty != null;
    }

    /// <summary>
    /// Gets a configuration sub-section with the specified key.
    /// </summary>
    /// <param name="key">The key of the configuration section to retrieve.</param>
    /// <returns>The configuration sub-section as an IConfigurationSection instance.</returns>
    /// <remarks>
    /// This method implements the IConfiguration interface. If an underlying configuration provider
    /// is available, it delegates to that provider. Otherwise, it creates a custom ConfigurationSection
    /// that reflects the properties of this AppConfig instance. This allows the AppConfig to be used
    /// as a drop-in replacement for standard IConfiguration instances.
    /// </remarks>
    /// <example>
    /// <code>
    /// var pathsSection = config.GetSection("paths");
    /// var aiSection = config.GetSection("aiservice");
    /// </code>
    /// </example>
    public IConfigurationSection GetSection(string key)
    {
        // If we have an underlying configuration, use it
        if (underlyingConfiguration != null)
        {
            return underlyingConfiguration.GetSection(key);
        }

        // Create a new ConfigurationSection using reflection based on our properties
        return new ConfigurationSection(this, key);
    }

    /// <summary>
    /// Gets the immediate descendant configuration sub-sections.
    /// </summary>
    /// <returns>The configuration sub-sections as an enumerable collection of IConfigurationSection instances.</returns>
    /// <remarks>
    /// This method implements the IConfiguration interface. If an underlying configuration provider
    /// is available, it delegates to that provider. Otherwise, it creates ConfigurationSection instances
    /// for each property declared in the AppConfig class, allowing for enumeration of all top-level
    /// configuration sections.
    /// </remarks>
    /// <example>
    /// <code>
    /// foreach (var section in config.GetChildren())
    /// {
    ///     Console.WriteLine($"Section: {section.Key}");
    /// }
    /// </code>
    /// </example>
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        // If we have an underlying configuration, use it
        if (underlyingConfiguration != null)
        {
            return underlyingConfiguration.GetChildren();
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
    /// <returns>A change token that triggers when the configuration changes.</returns>
    /// <remarks>
    /// This method implements the IConfiguration interface. If an underlying configuration provider
    /// is available, it returns that provider's reload token which can notify consumers when the
    /// configuration changes. If no underlying provider exists, it returns a non-reloading token
    /// since this AppConfig instance doesn't support automatic reloading from external sources.
    /// </remarks>
    /// <example>
    /// <code>
    /// var token = config.GetReloadToken();
    /// token.RegisterChangeCallback(state => Console.WriteLine("Config changed!"), null);
    /// </code>
    /// </example>
    public IChangeToken GetReloadToken()
    {
        // If we have an underlying configuration, use its reload token
        if (underlyingConfiguration != null)
        {
            return underlyingConfiguration.GetReloadToken();
        }

        // Otherwise return a non-reloading token
        return new ConfigurationReloadToken();
    }

    /// <summary>
    /// Gets or sets a configuration value for the specified key.
    /// </summary>
    /// <param name="key">The key of the configuration value to get or set. Supports colon-separated syntax for nested properties.</param>
    /// <returns>The configuration value as a string, or null if the key doesn't exist.</returns>
    /// <remarks>
    /// This indexer implements the IConfiguration interface and provides access to configuration values
    /// using key-based lookup. It supports both simple keys and nested keys using colon-separated syntax
    /// (e.g., "paths:logging_dir"). The getter first checks the underlying configuration provider if available,
    /// then falls back to reflection-based property access. The setter allows modification of configuration
    /// values at runtime, creating intermediate objects as needed for nested paths.
    /// </remarks>
    /// <example>
    /// <code>
    /// string loggingDir = config["paths:logging_dir"];
    /// config["DebugEnabled"] = "true";
    /// config["aiservice:provider"] = "openai";
    /// </code>
    /// </example>
    public string? this[string key]
    {
        get
        {
            // First try to get value from underlying configuration if available
            if (underlyingConfiguration != null && underlyingConfiguration[key] != null)
            {
                return underlyingConfiguration[key];
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
                        .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase)) ?? currentObj?.GetType().GetProperties()
                            .FirstOrDefault(p =>
                            {
                                return p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                                    .FirstOrDefault() is JsonPropertyNameAttribute attr && string.Equals(attr.Name, part, StringComparison.OrdinalIgnoreCase);
                            });
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
                    return p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                        .FirstOrDefault() is JsonPropertyNameAttribute attr && string.Equals(attr.Name, key, StringComparison.OrdinalIgnoreCase);
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
                        .FirstOrDefault(p => string.Equals(p.Name, part, StringComparison.OrdinalIgnoreCase)) ?? currentObj?.GetType().GetProperties()
                            .FirstOrDefault(p =>
                            {
                                return p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                                    .FirstOrDefault() is JsonPropertyNameAttribute attr && string.Equals(attr.Name, part, StringComparison.OrdinalIgnoreCase);
                            });
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
                    var finalPart = parts[^1];
                    var finalProperty = currentObj.GetType().GetProperties()
                        .FirstOrDefault(p => string.Equals(p.Name, finalPart, StringComparison.OrdinalIgnoreCase)) ?? currentObj.GetType().GetProperties()
                            .FirstOrDefault(p =>
                            {
                                return p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                                    .FirstOrDefault() is JsonPropertyNameAttribute attr && string.Equals(attr.Name, finalPart, StringComparison.OrdinalIgnoreCase);
                            });
                    if (finalProperty != null && finalProperty.CanWrite)
                    {
                        SetPropertyValue(finalProperty, currentObj, value);
                    }
                }

                return;
            }

            // Direct property access
            var directProperty = GetType().GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase)) ?? GetType().GetProperties()
                    .FirstOrDefault(p =>
                    {
                        return p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                            .FirstOrDefault() is JsonPropertyNameAttribute attr && string.Equals(attr.Name, key, StringComparison.OrdinalIgnoreCase);
                    });
            if (directProperty != null && directProperty.CanWrite)
            {
                SetPropertyValue(directProperty, this, value);
            }
        }
    }

    /// <summary>
    /// Helper method to set a property value with proper type conversion.
    /// </summary>
    /// <param name="property">The PropertyInfo object representing the target property.</param>
    /// <param name="target">The target object instance on which to set the property.</param>
    /// <param name="value">The string value to convert and set on the property.</param>
    /// <remarks>
    /// This method handles type conversion from string values to appropriate property types including
    /// primitive types (string, bool, int, double, DateTime), nullable types, and enums. It uses
    /// reflection to safely set property values with appropriate error handling. If conversion fails
    /// or the property cannot be set, the operation is silently ignored to maintain robustness.
    /// </remarks>
    private static void SetPropertyValue(PropertyInfo property, object target, string? value)
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
    }    /// <summary>
         /// Helper class to implement ConfigurationSection for AppConfig instances.
         /// </summary>
         /// <remarks>
         /// This internal class provides IConfigurationSection implementation that allows AppConfig
         /// to be used as a drop-in replacement for standard IConfiguration instances. It delegates
         /// configuration access to the parent AppConfig instance using reflection-based property access.
         /// </remarks>
         /// <param name="configuration">The parent configuration instance.</param>
         /// <param name="key">The key name for this configuration section.</param>
         /// <param name="parentPath">Optional parent path for building the full configuration path.</param>
    private class ConfigurationSection(IConfiguration configuration, string key, string? parentPath = null) : IConfigurationSection
    {
        private readonly IConfiguration configuration = configuration;
        private readonly string key = key;
        private readonly string path = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}:{key}";

        /// <summary>
        /// Gets the key of the configuration section.
        /// </summary>
        public string Key => key;

        /// <summary>
        /// Gets the path of the configuration section.
        /// </summary>
        public string Path => path;

        /// <summary>
        /// Gets or sets the value of the configuration section.
        /// </summary>
        public string? Value
        {
            get => configuration[path];
            set => configuration[path] = value;
        }

        /// <summary>
        /// Indexer to get or set a configuration value by key.
        /// </summary>
        /// <param name="key">The key of the configuration value.</param>
        /// <returns>The configuration value.</returns>
        public string? this[string key]
        {
            get => configuration[$"{path}:{key}"];
            set => configuration[$"{path}:{key}"] = value;
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The configuration sub-section.</returns>
        public IConfigurationSection GetSection(string key) => new ConfigurationSection(configuration, key, path);

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            // Get properties of the object this section represents
            var children = new List<IConfigurationSection>();

            // Check for properties related to this path
            foreach (var propKey in GetPropertyKeys())
            {
                children.Add(new ConfigurationSection(configuration, propKey, path));
            }

            return children;
        }

        // Helper method to find potential property keys
        private static IEnumerable<string> GetPropertyKeys()
        {
            // This is a simplified implementation
            // In a real scenario, you would query the underlying configuration
            // to get the actual child keys
            return [];
        }

        /// <summary>
        /// Gets a change token that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A change token.</returns>
        public IChangeToken GetReloadToken() => configuration.GetReloadToken();
    }
}
