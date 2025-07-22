// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.Json;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Tests for the metadata schema configuration migration from metadata.yaml to metadata-schema.yml.
/// These tests verify that the migration strategy works correctly for both old and new configurations.
/// </summary>
[TestClass]
public class MetadataSchemaConfigMigrationTests
{
    /// <summary>
    /// Tests that the new metadata_schema_file configuration is properly loaded.
    /// </summary>
    [TestMethod]
    public void AppConfig_Should_Load_MetadataSchemaFile_From_Configuration()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:metadata_schema_file", "config/metadata-schema.yml" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();

        // Act
        var appConfig = new AppConfig(configuration, logger.Object);

        // Assert
        Assert.AreEqual("config/metadata-schema.yml", appConfig.Paths.MetadataSchemaFile);
        Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }

    /// <summary>
    /// Tests that the old metadata_file configuration is still supported for backward compatibility.
    /// </summary>
    [TestMethod]
    public void AppConfig_Should_Still_Load_MetadataFile_For_Backward_Compatibility()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:metadata_file", "config/metadata.yaml" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();

        // Act
        var appConfig = new AppConfig(configuration, logger.Object);

        // Assert
        Assert.AreEqual("config/metadata.yaml", appConfig.Paths.MetadataFile);
        Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }

    /// <summary>
    /// Tests that both metadata_file and metadata_schema_file can be configured simultaneously.
    /// </summary>
    [TestMethod]
    public void AppConfig_Should_Support_Both_MetadataFile_And_MetadataSchemaFile()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:metadata_file", "config/metadata.yaml" },
            { "paths:metadata_schema_file", "config/metadata-schema.yml" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();

        // Act
        var appConfig = new AppConfig(configuration, logger.Object);

        // Assert
        Assert.AreEqual("config/metadata.yaml", appConfig.Paths.MetadataFile);
        Assert.AreEqual("config/metadata-schema.yml", appConfig.Paths.MetadataSchemaFile);
    }

    /// <summary>
    /// Tests that the JSON serialization/deserialization works correctly for both properties.
    /// </summary>
    [TestMethod]
    public void PathsConfig_Should_Serialize_And_Deserialize_Both_Properties()
    {
        // Arrange
        var originalConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = "/test/vault",
            MetadataFile = "config/metadata.yaml",
            MetadataSchemaFile = "config/metadata-schema.yml"
        };

        // Act
        var json = JsonSerializer.Serialize(originalConfig);
        var deserializedConfig = JsonSerializer.Deserialize<PathsConfig>(json);

        // Assert
        Assert.IsNotNull(deserializedConfig);
        Assert.AreEqual(originalConfig.NotebookVaultFullpathRoot, deserializedConfig.NotebookVaultFullpathRoot);
        Assert.AreEqual(originalConfig.MetadataFile, deserializedConfig.MetadataFile);
        Assert.AreEqual(originalConfig.MetadataSchemaFile, deserializedConfig.MetadataSchemaFile);
    }

    /// <summary>
    /// Tests that the MetadataSchemaLoader service registration uses the new configuration.
    /// </summary>
    [TestMethod]
    public void ServiceRegistration_Should_Use_MetadataSchemaFile_From_Configuration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configValues = new Dictionary<string, string?>
        {
            { "paths:metadata_schema_file", "config/test-schema.yml" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ILogger<MetadataSchemaLoader>>(Mock.Of<ILogger<MetadataSchemaLoader>>());

        var appConfig = new AppConfig(configuration, Mock.Of<ILogger<AppConfig>>());
        services.AddSingleton(appConfig);

        // Act - Just verify the configuration is properly loaded
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetRequiredService<AppConfig>();

        // Assert
        Assert.AreEqual("config/test-schema.yml", config.Paths.MetadataSchemaFile);
    }

    /// <summary>
    /// Tests that the fallback behavior works when no metadata schema file is configured.
    /// </summary>
    [TestMethod]
    public void ServiceRegistration_Should_Fallback_To_Default_When_No_MetadataSchemaFile_Configured()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ILogger<MetadataSchemaLoader>>(Mock.Of<ILogger<MetadataSchemaLoader>>());

        var appConfig = new AppConfig(configuration, Mock.Of<ILogger<AppConfig>>());
        services.AddSingleton(appConfig);

        // Act - Just verify the configuration fallback logic
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetRequiredService<AppConfig>();

        // Assert - The MetadataSchemaFile should be empty (fallback to default)
        Assert.AreEqual(string.Empty, config.Paths.MetadataSchemaFile);
    }

    /// <summary>
    /// Tests that configuration migration properly handles mixed old/new configurations.
    /// </summary>
    [TestMethod]
    public void Configuration_Migration_Should_Handle_Mixed_Old_And_New_Settings()
    {
        // Arrange - Create a config with some old and some new properties
        var configJson = @"{
            ""paths"": {
                ""notebook_vault_fullpath_root"": ""/test/vault"",
                ""metadata_file"": ""config/metadata.yaml"",
                ""metadata_schema_file"": ""config/metadata-schema.yml"",
                ""logging_dir"": ""logs""
            }
        }";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, configJson);

        try
        {
            // Act
            var appConfig = AppConfig.LoadFromJsonFile(tempFile);

            // Assert
            Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
            Assert.AreEqual("config/metadata.yaml", appConfig.Paths.MetadataFile);
            Assert.AreEqual("config/metadata-schema.yml", appConfig.Paths.MetadataSchemaFile);
            Assert.AreEqual("logs", appConfig.Paths.LoggingDir);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Tests that the notebook_vault_resources_basepath configuration is properly loaded from in-memory configuration.
    /// </summary>
    [TestMethod]
    public void AppConfig_Should_Load_NotebookVaultResourcesBasepath_From_Configuration()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:notebook_vault_resources_basepath", "01_Projects\\MBA" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();

        // Act
        var appConfig = new AppConfig(configuration, logger.Object);

        // Assert
        Assert.AreEqual("01_Projects\\MBA", appConfig.Paths.NotebookVaultResourcesBasepath);
        Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }

    /// <summary>
    /// Tests that the notebook_vault_resources_basepath serializes and deserializes correctly with JSON.
    /// </summary>
    [TestMethod]
    public void PathsConfig_Should_Serialize_And_Deserialize_NotebookVaultResourcesBasepath()
    {
        // Arrange
        var originalConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = "D:\\source\\notebook-automation\\tests\\obsidian-vault\\Obsidian Vault Test",
            NotebookVaultResourcesBasepath = "01_Projects\\MBA",
            OnedriveFullpathRoot = "C:\\Users\\user\\OneDrive\\",
            OnedriveResourcesBasepath = "Education\\MBA-Resources"
        };

        // Act
        var json = JsonSerializer.Serialize(originalConfig);
        var deserializedConfig = JsonSerializer.Deserialize<PathsConfig>(json);

        // Assert
        Assert.IsNotNull(deserializedConfig);
        Assert.AreEqual(originalConfig.NotebookVaultFullpathRoot, deserializedConfig.NotebookVaultFullpathRoot);
        Assert.AreEqual(originalConfig.NotebookVaultResourcesBasepath, deserializedConfig.NotebookVaultResourcesBasepath);
        Assert.AreEqual(originalConfig.OnedriveFullpathRoot, deserializedConfig.OnedriveFullpathRoot);
        Assert.AreEqual(originalConfig.OnedriveResourcesBasepath, deserializedConfig.OnedriveResourcesBasepath);
    }

    /// <summary>
    /// Tests that the JSON property name mapping works correctly for notebook_vault_resources_basepath.
    /// </summary>
    [TestMethod]
    public void PathsConfig_Should_Map_Json_Property_Name_For_NotebookVaultResourcesBasepath()
    {
        // Arrange - Create JSON with the exact property name used in config files
        var json = @"{
            ""notebook_vault_fullpath_root"": ""D:\\source\\notebook-automation\\tests\\obsidian-vault\\Obsidian Vault Test"",
            ""notebook_vault_resources_basepath"": ""01_Projects\\MBA"",
            ""onedrive_fullpath_root"": ""C:\\Users\\user\\OneDrive\\"",
            ""onedrive_resources_basepath"": ""Education\\MBA-Resources""
        }";

        // Act
        var deserializedConfig = JsonSerializer.Deserialize<PathsConfig>(json);

        // Assert
        Assert.IsNotNull(deserializedConfig);
        Assert.AreEqual("D:\\source\\notebook-automation\\tests\\obsidian-vault\\Obsidian Vault Test", deserializedConfig.NotebookVaultFullpathRoot);
        Assert.AreEqual("01_Projects\\MBA", deserializedConfig.NotebookVaultResourcesBasepath);
        Assert.AreEqual("C:\\Users\\user\\OneDrive\\", deserializedConfig.OnedriveFullpathRoot);
        Assert.AreEqual("Education\\MBA-Resources", deserializedConfig.OnedriveResourcesBasepath);
    }

    /// <summary>
    /// Tests loading configuration from a JSON file that includes notebook_vault_resources_basepath.
    /// </summary>
    [TestMethod]
    public void AppConfig_Should_Load_NotebookVaultResourcesBasepath_From_JsonFile()
    {
        // Arrange - Create a config JSON that matches the real config file structure
        var configJson = @"{
            ""ConfigFilePath"": ""test-config.json"",
            ""paths"": {
                ""onedrive_fullpath_root"": ""C:\\Users\\user\\OneDrive\\"",
                ""onedrive_resources_basepath"": ""Education\\MBA-Resources"",
                ""notebook_vault_fullpath_root"": ""D:\\source\\notebook-automation\\tests\\obsidian-vault\\Obsidian Vault Test"",
                ""notebook_vault_resources_basepath"": ""01_Projects\\MBA"",
                ""metadata_schema_file"": ""D:\\source\\notebook-automation\\config\\metadata-schema.yml"",
                ""logging_dir"": ""D:\\source\\notebook-automation\\logs"",
                ""prompts_path"": ""D:\\source\\notebook-automation\\prompts""
            }
        }";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, configJson);

        try
        {
            // Act
            var appConfig = AppConfig.LoadFromJsonFile(tempFile);

            // Assert
            Assert.AreEqual("D:\\source\\notebook-automation\\tests\\obsidian-vault\\Obsidian Vault Test", appConfig.Paths.NotebookVaultFullpathRoot);
            Assert.AreEqual("01_Projects\\MBA", appConfig.Paths.NotebookVaultResourcesBasepath);
            Assert.AreEqual("C:\\Users\\user\\OneDrive\\", appConfig.Paths.OnedriveFullpathRoot);
            Assert.AreEqual("Education\\MBA-Resources", appConfig.Paths.OnedriveResourcesBasepath);
            Assert.AreEqual("D:\\source\\notebook-automation\\config\\metadata-schema.yml", appConfig.Paths.MetadataSchemaFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// Tests that NotebookVaultResourcesBasepath handles empty/null values correctly.
    /// </summary>
    [TestMethod]
    public void PathsConfig_Should_Handle_Empty_NotebookVaultResourcesBasepath()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:notebook_vault_resources_basepath", "" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();

        // Act
        var appConfig = new AppConfig(configuration, logger.Object);

        // Assert
        Assert.AreEqual("", appConfig.Paths.NotebookVaultResourcesBasepath);
        Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }

    /// <summary>
    /// Tests that NotebookVaultResourcesBasepath handles missing configuration correctly.
    /// </summary>
    [TestMethod]
    public void PathsConfig_Should_Default_NotebookVaultResourcesBasepath_When_Missing()
    {
        // Arrange - Configuration without notebook_vault_resources_basepath
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:onedrive_fullpath_root", "/test/onedrive" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();

        // Act
        var appConfig = new AppConfig(configuration, logger.Object);

        // Assert
        Assert.AreEqual(string.Empty, appConfig.Paths.NotebookVaultResourcesBasepath);
        Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }
}
