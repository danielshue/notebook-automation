// <copyright file="AppConfigTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/AppConfigTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
#nullable enable

namespace NotebookAutomation.Core.Tests;

[TestClass]
public class AppConfigCoverageBoostTests
{
    [TestMethod]
    public void Constructor_WithUnderlyingConfiguration_LoadsSections()
    {
        Dictionary<string, string?> configDict = new()
        {
            { "paths:notebook_vault_fullpath_root", "/vault" },
            { "paths:onedrive_resources_basepath", "/base" },
            { "paths:logging_dir", "/logs" },
            { "paths:onedrive_fullpath_root", "/od" },
            { "paths:metadata_file", "/meta.yaml" },
            { "microsoft_graph:client_id", "cid" },
            { "microsoft_graph:api_endpoint", "ep" },
            { "microsoft_graph:authority", "https://login.microsoftonline.com/common" },
            { "aiservice:provider", "openai" },
            { "aiservice:openai:model", "gpt-4" },
            { "aiservice:openai:endpoint", "https://api.openai.com" },
            { "aiservice:azure:model", "az-gpt" },
            { "aiservice:azure:deployment", "az-deploy" },
            { "aiservice:azure:endpoint", "https://az.com" },
            { "aiservice:foundry:model", "foundry-llm" },
            { "aiservice:foundry:endpoint", "http://localhost:8000" },
            { "video_extensions:0", ".mp4" },
            { "video_extensions:1", ".avi" },
        };
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
        Mock<ILogger<AppConfig>> logger = new();
        AppConfig appConfig = new(configuration, logger.Object);
        Assert.AreEqual("/vault", appConfig.Paths.NotebookVaultFullpathRoot);
        Assert.AreEqual("cid", appConfig.MicrosoftGraph.ClientId);
        Assert.AreEqual("gpt-4", appConfig.AiService.OpenAI?.Model);
        Assert.AreEqual("az-gpt", appConfig.AiService.Azure?.Model);
        Assert.AreEqual("foundry-llm", appConfig.AiService.Foundry?.Model);
        CollectionAssert.Contains(appConfig.VideoExtensions, ".mp4");
    }

    [TestMethod]
    public void LoadConfiguration_FileBased_LoadsAndLogs()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        AppConfig config = new()
        {
            Paths = new PathsConfig { LoggingDir = "/logs" },
            MicrosoftGraph = new MicrosoftGraphConfig { ClientId = "cid" },
            AiService = new AIServiceConfig { Provider = "openai", OpenAI = new OpenAiProviderConfig { Model = "gpt-4" } },
            VideoExtensions = [".mp4"],
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(config));
        AppConfig appConfig = AppConfig.LoadFromJsonFile(tempFile);
        StringAssert.EndsWith(appConfig.Paths.LoggingDir.Replace("\\", "/"), "/logs");
        Assert.AreEqual("cid", appConfig.MicrosoftGraph.ClientId);
        Assert.AreEqual("gpt-4", appConfig.AiService.OpenAI?.Model);
        CollectionAssert.Contains(appConfig.VideoExtensions, ".mp4");
        File.Delete(tempFile);
    }

    [TestMethod]
    public void SaveToJsonFile_And_ErrorHandling()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        Mock<ILogger<AppConfig>> logger = new();
        AppConfig appConfig = new() { Paths = new PathsConfig { LoggingDir = "/logs" } };
        appConfig.SaveToJsonFile(tempFile);
        Assert.IsTrue(File.Exists(tempFile));
        File.Delete(tempFile);

        // Error: directory does not exist and cannot be created
        Assert.ThrowsExactly<IOException>(() => appConfig.SaveToJsonFile(Path.Combine("?invalidpath", "bad.json")));
    }

    [TestMethod]
    public void GetSection_And_GetChildren_WithAndWithoutUnderlyingConfig()
    {
        AppConfig appConfig = new();
        IConfigurationSection section = appConfig.GetSection("paths");
        Assert.AreEqual("paths", section.Key);
        List<IConfigurationSection> children = [.. appConfig.GetChildren()];
        Assert.IsTrue(children.Any(c => c.Key.Equals("Paths", StringComparison.OrdinalIgnoreCase)));        // With underlying config
        Dictionary<string, string?> configDict = new() { { "paths:logging_dir", "/logs" } };
        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
        Mock<ILogger<AppConfig>> logger = new();
        AppConfig appConfig2 = new(configuration, logger.Object);
        IConfigurationSection section2 = appConfig2.GetSection("paths");
        Assert.IsNotNull(section2);
        List<IConfigurationSection> children2 = [.. appConfig2.GetChildren()];
        Assert.AreNotEqual(0, children2.Count);
    }

    [TestMethod]
    public void Exists_And_Indexer_ComplexCases()
    {
        AppConfig appConfig = new()
        {
            Paths = new PathsConfig { LoggingDir = "logs" },
        };
        Assert.IsTrue(appConfig.Exists("paths:logging_dir"));
        Assert.IsNull(appConfig["paths:nonexistent"]);
        appConfig["paths:logging_dir"] = "newlogs";
        Assert.AreEqual("newlogs", appConfig.Paths.LoggingDir);
    }
}

/// <summary>
/// Tests for the AppConfig class, especially focusing on its implementation of IConfiguration.
/// </summary>
[TestClass]
public class AppConfigTests
{
    private Mock<ILogger<AppConfig>> loggerMock = null!;
    private Mock<IConfiguration> configurationMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<AppConfig>>();
        configurationMock = new Mock<IConfiguration>();
    }

    /// <summary>
    /// Helper method to set up API key configuration for testing.
    /// </summary>
    private static void SetupApiKeyConfigurationForTesting(AppConfig config, string apiKey) =>

        // Set the environment variable directly for the provider
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", apiKey);

    /// <summary>
    /// Tests the basic property indexer access with both direct and nested keys.
    /// </summary>
    [TestMethod]
    public void Indexer_ShouldReturnValueForExistingKeys()
    {
        // Arrange
        AppConfig appConfig = new()
        {
            Paths = new PathsConfig
            {
                LoggingDir = "logs",
                OnedriveFullpathRoot = "/resources",
            },
            AiService = new AIServiceConfig
            {
                Provider = "openai",
                OpenAI = new OpenAiProviderConfig { Model = "gpt-4" },
            },
        };
        SetupApiKeyConfigurationForTesting(appConfig, "test-api-key");

        // Act & Assert
        Assert.AreEqual("logs", appConfig["paths:LoggingDir"]);
        Assert.AreEqual("/resources", appConfig["paths:OnedriveFullpathRoot"]);
        Assert.AreEqual("gpt-4", appConfig["aiservice:Model"]);

        // Check using JsonPropertyName value
        Assert.AreEqual("test-api-key", appConfig["aiservice:api_key"]);
        Assert.AreEqual("logs", appConfig["paths:logging_dir"]);
    }

    /// <summary>
    /// Tests that null is returned for non-existent keys.
    /// </summary>
    [TestMethod]
    public void Indexer_ShouldReturnNullForNonExistentKeys()
    {
        // Arrange
        AppConfig appConfig = new();

        // Act & Assert
        Assert.IsNull(appConfig["nonExistentKey"]);
        Assert.IsNull(appConfig["paths:nonExistentKey"]);
    }

    /// <summary>
    /// Tests setting values through the indexer.
    /// </summary>
    [TestMethod]
    public void Indexer_ShouldSetValueForExistingKeys()
    {
        // Arrange
        AppConfig appConfig = new()
        {
            Paths = new PathsConfig(),
        };

        // Act
        appConfig["paths:LoggingDir"] = "new-logs";

        // Assert
        Assert.AreEqual("new-logs", appConfig.Paths.LoggingDir);
        Assert.AreEqual("new-logs", appConfig["paths:LoggingDir"]);
    }

    /// <summary>
    /// Tests getting configuration sections.
    /// </summary>
    [TestMethod]
    public void GetSection_ShouldReturnValidSection()
    {
        // Arrange
        AppConfig appConfig = new()
        {
            Paths = new PathsConfig
            {
                LoggingDir = "logs",
                OnedriveFullpathRoot = "/resources",
            },
        };

        // Act
        IConfigurationSection pathsSection = appConfig.GetSection("paths");

        // Assert
        Assert.IsNotNull(pathsSection);
        Assert.AreEqual("paths", pathsSection.Key);
        Assert.AreEqual("paths", pathsSection.Path);
        Assert.AreEqual("logs", pathsSection["LoggingDir"]);
        Assert.AreEqual("/resources", pathsSection["OnedriveFullpathRoot"]);
    }

    /// <summary>
    /// Tests the Exists method.
    /// </summary>
    [TestMethod]
    public void Exists_ShouldReturnCorrectResults()
    {
        // Arrange
        AppConfig appConfig = new()
        {
            Paths = new PathsConfig
            {
                LoggingDir = "logs",
            },
        };

        // Act & Assert
        Assert.IsTrue(appConfig.Exists("paths"));
        Assert.IsTrue(appConfig.Exists("paths:LoggingDir"));
        Assert.IsTrue(appConfig.Exists("Paths")); // Case-insensitive
        Assert.IsTrue(appConfig.Exists("paths:logging_dir")); // JsonPropertyName
        Assert.IsFalse(appConfig.Exists("nonExistentKey"));
        Assert.IsFalse(appConfig.Exists("paths:nonExistentKey"));
    }

    /// <summary>
    /// Tests getting child sections.
    /// </summary>
    [TestMethod]
    public void GetChildren_ShouldReturnAllTopLevelSections()
    {
        // Arrange
        AppConfig appConfig = new();

        // Act
        List<IConfigurationSection> children = [.. appConfig.GetChildren()];

        // Assert
        Assert.IsTrue(children.Count > 0);
        Assert.IsTrue(children.Any(c => c.Key.Equals("Paths", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(children.Any(c => c.Key.Equals("AiService", StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(children.Any(c => c.Key.Equals("MicrosoftGraph", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Tests loading configuration from JSON file.
    /// </summary>
    [TestMethod]
    public void LoadFromJsonFile_ShouldLoadConfigurationCorrectly()
    {
        // Arrange - Create a temporary config file
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        var config = new
        {
            paths = new
            {
                onedrive_fullpath_root = "/resources",
                logging_dir = "/logs",
            },
            aiservice = new
            {
                provider = "openai",
                openai = new
                {
                    model = "gpt-4",
                },
            },
        };

        File.WriteAllText(tempFile, JsonSerializer.Serialize(config));

        try
        { // Act
            AppConfig appConfig = AppConfig.LoadFromJsonFile(tempFile);

            // Assert
            Assert.IsNotNull(appConfig);
            Assert.AreEqual("/resources", appConfig.Paths.OnedriveFullpathRoot);
            Assert.AreEqual("/logs", appConfig.Paths.LoggingDir);
            Assert.AreEqual("gpt-4", appConfig.AiService.Model);

            // Note: GetApiKey() reads from environment variables, not configuration,
            // so we don't test it here since this test focuses on JSON file loading
        }
        finally
        {
            // Clean up
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Tests saving configuration to JSON file.
    /// </summary>
    [TestMethod]
    public void SaveToJsonFile_ShouldSaveConfigurationCorrectly()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

        // Save the existing environment variable to restore later
        string? originalApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        try
        {
            // Set the test API key - ensuring it's set both for the current process and test
            const string testApiKey = "test-api-key";
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", testApiKey, EnvironmentVariableTarget.Process);

            AppConfig appConfig = new()
            {
                Paths = new PathsConfig
                {
                    LoggingDir = "/logs",
                    OnedriveFullpathRoot = "/resources",
                },
                AiService = new AIServiceConfig
                {
                    Provider = "openai",
                    OpenAI = new OpenAiProviderConfig { Model = "gpt-4" }, // API key is managed at AIServiceConfig level
                },
            };

            // Act
            appConfig.SaveToJsonFile(tempFile);

            // Verify API key is set correctly
            Assert.AreEqual(testApiKey, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            // Assert - Load it back and verify
            AppConfig loadedConfig = AppConfig.LoadFromJsonFile(tempFile);
            Assert.IsNotNull(loadedConfig);
            Assert.AreEqual("/resources", loadedConfig.Paths.OnedriveFullpathRoot);
            Assert.AreEqual("/logs", loadedConfig.Paths.LoggingDir);

            // The API key should be retrieved from the environment variable
            Assert.AreEqual(testApiKey, loadedConfig.AiService.GetApiKey());
            Assert.AreEqual("gpt-4", loadedConfig.AiService.OpenAI?.Model);
        }
        finally
        {
            // Restore the original API key
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", originalApiKey);

            // Clean up
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Tests configuration with underlying IConfiguration.
    /// </summary>    [TestMethod]
    public void WithUnderlyingConfiguration_ShouldUseUnderlyingValues()
    {
        // Arrange
        Dictionary<string, string?> configValues = new()
        {
            { "paths:notebook_vault_fullpath_root", "/config-vault" },
            { "paths:onedrive_resources_basepath", "/config-resources-base" },
            { "paths:logging_dir", "/config-logs" },
            { "paths:onedrive_fullpath_root", "/config-resources" },
            { "paths:metadata_file", "/config-meta.yaml" },
            { "microsoft_graph:client_id", "config-client-id" },
            { "microsoft_graph:api_endpoint", "config-endpoint" },
            { "microsoft_graph:authority", "https://login.microsoftonline.com/common" },
            { "aiservice:provider", "openai" },
            { "aiservice:openai:model", "gpt-4-turbo" },
            { "aiservice:openai:endpoint", "https://api.openai.com" },
            { "video_extensions:0", ".mp4" },
        };

        // Create a real configuration implementation for testing
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();        // Create test configuration using constructor with the real configuration
        AppConfig appConfig = new(configuration, loggerMock.Object); // Act & Assert
        Assert.AreEqual("/config-vault", appConfig.Paths.NotebookVaultFullpathRoot);
        Assert.AreEqual("/config-resources", appConfig.Paths.OnedriveFullpathRoot);
        Assert.AreEqual("/config-logs", appConfig.Paths.LoggingDir);
        Assert.AreEqual("config-client-id", appConfig.MicrosoftGraph.ClientId);
        Assert.AreEqual("gpt-4-turbo", appConfig.AiService.OpenAI?.Model);
    }
}

[TestClass]
public class AppConfigAdditionalTests
{
    [TestMethod]
    public void SetVideoExtensions_ShouldUpdateList()
    {
        AppConfig appConfig = new();
        List<string> list = [".mp4", ".avi"];
        appConfig.SetVideoExtensions(list);
        CollectionAssert.AreEqual(list, appConfig.VideoExtensions);
    }

    [TestMethod]
    public void FindConfigFile_ShouldReturnEmptyIfNotFound()
    {
        string result = AppConfig.FindConfigFile("nonexistent_config_file.json");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Exists_ShouldHandleJsonPropertyNameAndDirectProperty()
    {
        AppConfig appConfig = new()
        {
            Paths = new PathsConfig { LoggingDir = "logs" },
        };
        Assert.IsTrue(appConfig.Exists("paths:logging_dir")); // JsonPropertyName in nested config
        Assert.IsTrue(appConfig.Exists("Paths")); // Direct property
    }

    [TestMethod]
    public void GetReloadToken_ShouldReturnToken()
    {
        AppConfig appConfig = new();
        Microsoft.Extensions.Primitives.IChangeToken token = appConfig.GetReloadToken();
        Assert.IsNotNull(token);
    }

    [TestMethod]
    public void Indexer_SetNestedAndDirectProperties_ShouldUpdateValues()
    {
        AppConfig appConfig = new();
        appConfig["Paths:LoggingDir"] = "logs";
        appConfig["DebugEnabled"] = "true";
        Assert.AreEqual("logs", appConfig.Paths.LoggingDir);
        Assert.AreEqual("True", appConfig["DebugEnabled"], ignoreCase: true);
    }

    [TestMethod]
    public void SetPropertyValue_ShouldHandleVariousTypes()
    {
        AppConfig appConfig = new();
        appConfig["DebugEnabled"] = "true";
        appConfig["Paths:LoggingDir"] = "logs";
        appConfig["Paths:NotebookVaultFullpathRoot"] = "vault";
        Assert.IsTrue(appConfig.DebugEnabled);
        Assert.AreEqual("logs", appConfig.Paths.LoggingDir);
        Assert.AreEqual("vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }

    [TestMethod]
    public void ExtractTenantIdFromAuthority_ShouldReturnExpectedResults()
    {
        System.Reflection.MethodInfo? method = typeof(AppConfig).GetMethod("ExtractTenantIdFromAuthority", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(method);
        Assert.AreEqual("common", method.Invoke(null, ["https://login.microsoftonline.com/common"]));
        Assert.AreEqual("12345678-1234-1234-1234-123456789abc", method.Invoke(null, ["https://login.microsoftonline.com/12345678-1234-1234-1234-123456789abc"]));
        Assert.AreEqual("common", method.Invoke(null, [string.Empty]));
    }
}
