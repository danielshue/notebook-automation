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
}
