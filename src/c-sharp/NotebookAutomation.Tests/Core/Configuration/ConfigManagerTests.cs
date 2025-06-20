// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the ConfigManager class.
/// </summary>
/// <remarks>
/// These tests verify the configuration discovery, loading, and validation functionality
/// of the ConfigManager, including proper dependency injection, error handling, and
/// prioritized discovery strategy.
/// </remarks>
[TestClass]
public class ConfigManagerTests
{
    private Mock<IFileSystemWrapper> _mockFileSystem = null!;
    private Mock<IEnvironmentWrapper> _mockEnvironment = null!;
    private Mock<ILogger<ConfigManager>> _mockLogger = null!;
    private ConfigManager _configManager = null!;

    /// <summary>
    /// Initializes test dependencies before each test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _mockFileSystem = new Mock<IFileSystemWrapper>();
        _mockEnvironment = new Mock<IEnvironmentWrapper>();
        _mockLogger = new Mock<ILogger<ConfigManager>>();
        _configManager = new ConfigManager(_mockFileSystem.Object, _mockEnvironment.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the ConfigManager constructor properly validates null file system wrapper.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new ConfigManager(null!, _mockEnvironment.Object, _mockLogger.Object));

        Assert.AreEqual("fileSystem", exception.ParamName);
    }

    /// <summary>
    /// Tests that the ConfigManager constructor properly validates null environment wrapper.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullEnvironment_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new ConfigManager(_mockFileSystem.Object, null!, _mockLogger.Object));

        Assert.AreEqual("environment", exception.ParamName);
    }

    /// <summary>
    /// Tests that the ConfigManager constructor properly validates null logger.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
            new ConfigManager(_mockFileSystem.Object, _mockEnvironment.Object, null!));

        Assert.AreEqual("logger", exception.ParamName);
    }

    /// <summary>
    /// Tests that the ConfigManager constructor succeeds with valid dependencies.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var configManager = new ConfigManager(_mockFileSystem.Object, _mockEnvironment.Object, _mockLogger.Object);

        // Assert
        Assert.IsNotNull(configManager);
    }

    #endregion

    #region LoadConfigurationAsync Tests

    /// <summary>
    /// Tests that LoadConfigurationAsync throws ArgumentNullException for null options.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            _configManager.LoadConfigurationAsync(null!));
    }

    /// <summary>
    /// Tests successful configuration loading from CLI-specified path.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithValidCliPath_ReturnsSuccessResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "config.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe",
            Debug = true
        };

        var configJson = """
        {
            "paths": {
                "logging_dir": "/logs"
            },
            "aiservice": {
                "provider": "openai",
                "openai": {
                    "model": "gpt-4"
                }
            }
        }
        """;

        _mockFileSystem.Setup(fs => fs.FileExists("config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("config.json")).ReturnsAsync(configJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("config.json", result.ConfigurationPath);
        Assert.IsNull(result.ErrorMessage);
        Assert.IsNull(result.Exception);
        Assert.AreEqual("/logs", result.Configuration.Paths.LoggingDir);
        Assert.AreEqual("openai", result.Configuration.AiService.Provider);
        Assert.AreEqual("gpt-4", result.Configuration.AiService.OpenAI?.Model);
    }

    /// <summary>
    /// Tests configuration loading failure when CLI path doesn't exist.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithInvalidCliPath_ReturnsFailureResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "nonexistent.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        _mockFileSystem.Setup(fs => fs.FileExists("nonexistent.json")).Returns(false);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Configuration);
        Assert.IsNull(result.ConfigurationPath);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsNull(result.Exception);
    }

    /// <summary>
    /// Tests configuration loading from environment variable when CLI path is not specified.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithEnvironmentVariable_ReturnsSuccessResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe",
            Debug = false
        };

        var configJson = """
        {
            "paths": {
                "logging_dir": "/env-logs"
            }
        }
        """;

        _mockEnvironment.Setup(env => env.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG"))
            .Returns("/env/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/env/config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("/env/config.json")).ReturnsAsync(configJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("/env/config.json", result.ConfigurationPath);
        Assert.AreEqual("/env-logs", result.Configuration.Paths.LoggingDir);
    }

    /// <summary>
    /// Tests configuration loading from working directory when higher priority options are not available.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_FromWorkingDirectory_ReturnsSuccessResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        var configJson = """
        {
            "paths": {
                "logging_dir": "/work-logs"
            }
        }
        """;

        _mockEnvironment.Setup(env => env.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG"))
            .Returns((string?)null);
        _mockFileSystem.Setup(fs => fs.CombinePath("/work", "config.json")).Returns("/work/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/work/config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("/work/config.json")).ReturnsAsync(configJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("/work/config.json", result.ConfigurationPath);
        Assert.AreEqual("/work-logs", result.Configuration.Paths.LoggingDir);
    }

    /// <summary>
    /// Tests configuration loading from executable directory when working directory doesn't have config.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_FromExecutableDirectory_ReturnsSuccessResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        var configJson = """
        {
            "paths": {
                "logging_dir": "/exe-logs"
            }
        }
        """;

        _mockEnvironment.Setup(env => env.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG"))
            .Returns((string?)null);
        _mockFileSystem.Setup(fs => fs.CombinePath("/work", "config.json")).Returns("/work/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/work/config.json")).Returns(false);
        _mockFileSystem.Setup(fs => fs.CombinePath("/exe", "config.json")).Returns("/exe/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/exe/config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("/exe/config.json")).ReturnsAsync(configJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("/exe/config.json", result.ConfigurationPath);
        Assert.AreEqual("/exe-logs", result.Configuration.Paths.LoggingDir);
    }

    /// <summary>
    /// Tests configuration loading from executable config subdirectory as last resort.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_FromExecutableConfigSubdirectory_ReturnsSuccessResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        var configJson = """
        {
            "paths": {
                "logging_dir": "/exe-config-logs"
            }
        }
        """;

        _mockEnvironment.Setup(env => env.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG"))
            .Returns((string?)null);
        _mockFileSystem.Setup(fs => fs.CombinePath("/work", "config.json")).Returns("/work/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/work/config.json")).Returns(false);
        _mockFileSystem.Setup(fs => fs.CombinePath("/exe", "config.json")).Returns("/exe/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/exe/config.json")).Returns(false);
        _mockFileSystem.Setup(fs => fs.CombinePath("/exe", "config", "config.json")).Returns("/exe/config/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/exe/config/config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("/exe/config/config.json")).ReturnsAsync(configJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("/exe/config/config.json", result.ConfigurationPath);
        Assert.AreEqual("/exe-config-logs", result.Configuration.Paths.LoggingDir);
    }

    /// <summary>
    /// Tests that no configuration found returns failure result.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_NoConfigurationFound_ReturnsFailureResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        _mockEnvironment.Setup(env => env.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG"))
            .Returns((string?)null);
        _mockFileSystem.Setup(fs => fs.CombinePath(It.IsAny<string>(), It.IsAny<string>())).Returns((string[] paths) => string.Join("/", paths));
        _mockFileSystem.Setup(fs => fs.CombinePath(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((string[] paths) => string.Join("/", paths));
        _mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Configuration);
        Assert.IsNull(result.ConfigurationPath);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Contains("No configuration file found"));
    }

    /// <summary>
    /// Tests that JSON deserialization failure returns failure result with exception.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithInvalidJson_ReturnsFailureResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "invalid.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        var invalidJson = "{ invalid json }";

        _mockFileSystem.Setup(fs => fs.FileExists("invalid.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("invalid.json")).ReturnsAsync(invalidJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Configuration);
        Assert.IsNull(result.ConfigurationPath);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsNotNull(result.Exception);
        Assert.IsTrue(result.ErrorMessage.Contains("Failed to load configuration"));
    }

    /// <summary>
    /// Tests that file reading failure returns failure result with exception.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithFileReadFailure_ReturnsFailureResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "config.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        _mockFileSystem.Setup(fs => fs.FileExists("config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("config.json"))
            .ThrowsAsync(new IOException("File access denied"));

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Configuration);
        Assert.IsNull(result.ConfigurationPath);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsNotNull(result.Exception);
        Assert.IsInstanceOfType(result.Exception, typeof(IOException));
    }

    /// <summary>
    /// Tests debug logging when debug mode is enabled.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithDebugEnabled_LogsDebugMessages()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "config.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe",
            Debug = true
        };

        var configJson = """{"paths": {"logging_dir": "/logs"}}""";

        _mockFileSystem.Setup(fs => fs.FileExists("config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("config.json")).ReturnsAsync(configJson);

        // Act
        await _configManager.LoadConfigurationAsync(options);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting configuration discovery process")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading configuration from")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ValidateConfigurationAsync Tests    /// <summary>
    /// Tests that ValidateConfigurationAsync throws ArgumentNullException for null path.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigurationAsync_WithNullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            _configManager.ValidateConfigurationAsync(null!));
    }

    /// <summary>
    /// Tests that ValidateConfigurationAsync throws ArgumentException for empty path.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigurationAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            _configManager.ValidateConfigurationAsync(string.Empty));
    }

    /// <summary>
    /// Tests that ValidateConfigurationAsync returns false for non-existent file.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigurationAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists("nonexistent.json")).Returns(false);

        // Act
        var result = await _configManager.ValidateConfigurationAsync("nonexistent.json");

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that ValidateConfigurationAsync returns true for valid JSON file.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigurationAsync_WithValidJsonFile_ReturnsTrue()
    {
        // Arrange
        var validJson = """{"paths": {"logging_dir": "/logs"}}""";

        _mockFileSystem.Setup(fs => fs.FileExists("valid.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("valid.json")).ReturnsAsync(validJson);

        // Act
        var result = await _configManager.ValidateConfigurationAsync("valid.json");

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests that ValidateConfigurationAsync returns false for invalid JSON file.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigurationAsync_WithInvalidJsonFile_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        _mockFileSystem.Setup(fs => fs.FileExists("invalid.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("invalid.json")).ReturnsAsync(invalidJson);

        // Act
        var result = await _configManager.ValidateConfigurationAsync("invalid.json");

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that ValidateConfigurationAsync returns false when file read fails.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigurationAsync_WithFileReadFailure_ReturnsFalse()
    {
        // Arrange
        _mockFileSystem.Setup(fs => fs.FileExists("config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("config.json"))
            .ThrowsAsync(new IOException("File access denied"));

        // Act
        var result = await _configManager.ValidateConfigurationAsync("config.json");

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that ValidateConfigurationAsync logs warning for validation failure.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigurationAsync_WithValidationFailure_LogsWarning()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        _mockFileSystem.Setup(fs => fs.FileExists("invalid.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("invalid.json")).ReturnsAsync(invalidJson);

        // Act
        await _configManager.ValidateConfigurationAsync("invalid.json");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Configuration validation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Tests complete configuration discovery workflow with complex JSON structure.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_ComplexConfiguration_ParsesCorrectly()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "complex.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe",
            Debug = false
        };

        var complexJson = """
        {
            "paths": {
                "notebook_vault_fullpath_root": "/vault",
                "onedrive_resources_basepath": "/onedrive/resources",
                "logging_dir": "/logs",
                "onedrive_fullpath_root": "/onedrive",
                "metadata_file": "/metadata.yaml"
            },            "microsoft_graph": {
                "client_id": "test-client-id",
                "api_endpoint": "https://graph.microsoft.com",
                "authority": "https://login.microsoftonline.com/tenant-id",
                "tenant_id": "tenant-id",
                "scopes": ["User.Read", "Files.Read"]
            },
            "aiservice": {
                "provider": "azure",
                "openai": {
                    "model": "gpt-4",
                    "endpoint": "https://api.openai.com"
                },
                "azure": {
                    "model": "gpt-4",
                    "deployment": "gpt-4-deployment",
                    "endpoint": "https://test.openai.azure.com"
                },
                "foundry": {
                    "model": "llama2",
                    "endpoint": "http://localhost:8000"
                }
            },
            "video_extensions": [".mp4", ".avi", ".mkv"],
            "pdf_extensions": [".pdf"],
            "pdf_extract_images": true,
            "banners": {
                "enabled": true
            }
        }
        """;

        _mockFileSystem.Setup(fs => fs.FileExists("complex.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("complex.json")).ReturnsAsync(complexJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);

        // Verify paths configuration
        Assert.AreEqual("/vault", result.Configuration.Paths.NotebookVaultFullpathRoot);
        Assert.AreEqual("/onedrive/resources", result.Configuration.Paths.OnedriveResourcesBasepath);
        Assert.AreEqual("/logs", result.Configuration.Paths.LoggingDir);
        Assert.AreEqual("/onedrive", result.Configuration.Paths.OnedriveFullpathRoot);
        Assert.AreEqual("/metadata.yaml", result.Configuration.Paths.MetadataFile);

        // Verify Microsoft Graph configuration
        Assert.AreEqual("test-client-id", result.Configuration.MicrosoftGraph.ClientId);
        Assert.AreEqual("https://graph.microsoft.com", result.Configuration.MicrosoftGraph.ApiEndpoint);
        Assert.AreEqual("https://login.microsoftonline.com/tenant-id", result.Configuration.MicrosoftGraph.Authority);
        Assert.AreEqual("tenant-id", result.Configuration.MicrosoftGraph.TenantId);
        CollectionAssert.Contains(result.Configuration.MicrosoftGraph.Scopes, "User.Read");
        CollectionAssert.Contains(result.Configuration.MicrosoftGraph.Scopes, "Files.Read");

        // Verify AI service configuration
        Assert.AreEqual("azure", result.Configuration.AiService.Provider);
        Assert.AreEqual("gpt-4", result.Configuration.AiService.OpenAI?.Model);
        Assert.AreEqual("https://api.openai.com", result.Configuration.AiService.OpenAI?.Endpoint);
        Assert.AreEqual("gpt-4", result.Configuration.AiService.Azure?.Model);
        Assert.AreEqual("gpt-4-deployment", result.Configuration.AiService.Azure?.Deployment);
        Assert.AreEqual("https://test.openai.azure.com", result.Configuration.AiService.Azure?.Endpoint);
        Assert.AreEqual("llama2", result.Configuration.AiService.Foundry?.Model);
        Assert.AreEqual("http://localhost:8000", result.Configuration.AiService.Foundry?.Endpoint);

        // Verify file extensions
        CollectionAssert.Contains(result.Configuration.VideoExtensions, ".mp4");
        CollectionAssert.Contains(result.Configuration.VideoExtensions, ".avi");
        CollectionAssert.Contains(result.Configuration.VideoExtensions, ".mkv");
        CollectionAssert.Contains(result.Configuration.PdfExtensions, ".pdf");

        // Verify other settings
        Assert.IsTrue(result.Configuration.PdfExtractImages);
    }

    /// <summary>
    /// Tests that configuration discovery respects priority order.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_PriorityOrder_UsesHighestPriority()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "cli.json", // Highest priority
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        var cliJson = """{"paths": {"logging_dir": "/cli-logs"}}""";
        var envJson = """{"paths": {"logging_dir": "/env-logs"}}""";
        var workJson = """{"paths": {"logging_dir": "/work-logs"}}""";

        // Setup CLI path (should be used)
        _mockFileSystem.Setup(fs => fs.FileExists("cli.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("cli.json")).ReturnsAsync(cliJson);

        // Setup environment variable
        _mockEnvironment.Setup(env => env.GetEnvironmentVariable("NOTEBOOKAUTOMATION_CONFIG"))
            .Returns("/env/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/env/config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("/env/config.json")).ReturnsAsync(envJson);

        // Setup working directory
        _mockFileSystem.Setup(fs => fs.CombinePath("/work", "config.json")).Returns("/work/config.json");
        _mockFileSystem.Setup(fs => fs.FileExists("/work/config.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("/work/config.json")).ReturnsAsync(workJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("cli.json", result.ConfigurationPath);
        Assert.AreEqual("/cli-logs", result.Configuration.Paths.LoggingDir);

        // Verify environment and working directory configs were not used
        _mockFileSystem.Verify(fs => fs.ReadAllTextAsync("/env/config.json"), Times.Never);
        _mockFileSystem.Verify(fs => fs.ReadAllTextAsync("/work/config.json"), Times.Never);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    /// <summary>
    /// Tests configuration loading with empty JSON object.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithEmptyJson_ReturnsSuccessWithDefaults()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "empty.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        var emptyJson = "{}";

        _mockFileSystem.Setup(fs => fs.FileExists("empty.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("empty.json")).ReturnsAsync(emptyJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("empty.json", result.ConfigurationPath);

        // Verify default values are set
        Assert.IsNotNull(result.Configuration.Paths);
        Assert.IsNotNull(result.Configuration.MicrosoftGraph);
        Assert.IsNotNull(result.Configuration.AiService);
        Assert.IsNotNull(result.Configuration.VideoExtensions);
        Assert.IsNotNull(result.Configuration.PdfExtensions);
        Assert.IsNotNull(result.Configuration.Banners);
    }

    /// <summary>
    /// Tests configuration loading with null deserialization result.
    /// </summary>
    [TestMethod]
    public async Task LoadConfigurationAsync_WithNullDeserialization_ReturnsFailureResult()
    {
        // Arrange
        var options = new ConfigDiscoveryOptions
        {
            ConfigPath = "null.json",
            WorkingDirectory = "/work",
            ExecutableDirectory = "/exe"
        };

        var nullJson = "null";

        _mockFileSystem.Setup(fs => fs.FileExists("null.json")).Returns(true);
        _mockFileSystem.Setup(fs => fs.ReadAllTextAsync("null.json")).ReturnsAsync(nullJson);

        // Act
        var result = await _configManager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsNull(result.Configuration);
        Assert.IsNull(result.ConfigurationPath);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsNotNull(result.Exception);
        Assert.IsTrue(result.ErrorMessage.Contains("Failed to load configuration"));
    }

    #endregion
}
