// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Tests.Core.Configuration;

[TestClass]
public class MetadataSchemaConfigMigrationTests
{
    [TestMethod]
    public void AppConfig_Should_Load_MetadataSchemaFile_From_Configuration()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:metadata_schema_file", "config/metadata-schema.yml" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();
        var appConfig = new AppConfig(configuration, logger.Object);

        Assert.AreEqual("config/metadata-schema.yml", appConfig.Paths.MetadataSchemaFile);
        Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }

    [TestMethod]
    public void PathsConfig_Should_Serialize_And_Deserialize_MetadataSchemaFile()
    {
        var originalConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = "/test/vault",
            MetadataSchemaFile = "config/metadata-schema.yml"
        };

        var json = JsonSerializer.Serialize(originalConfig);
        var deserializedConfig = JsonSerializer.Deserialize<PathsConfig>(json);

        Assert.IsNotNull(deserializedConfig);
        Assert.AreEqual(originalConfig.NotebookVaultFullpathRoot, deserializedConfig.NotebookVaultFullpathRoot);
        Assert.AreEqual(originalConfig.MetadataSchemaFile, deserializedConfig.MetadataSchemaFile);
    }

    [TestMethod]
    public void ServiceRegistration_Should_Use_MetadataSchemaFile_From_Configuration()
    {
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

        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetRequiredService<AppConfig>();

        Assert.AreEqual("config/test-schema.yml", config.Paths.MetadataSchemaFile);
    }

    [TestMethod]
    public void AppConfig_Should_Load_NotebookVaultResourcesBasepath_From_Configuration()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "paths:notebook_vault_fullpath_root", "/test/vault" },
            { "paths:notebook_vault_resources_basepath", "01_Projects\\MBA" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var logger = new Mock<ILogger<AppConfig>>();
        var appConfig = new AppConfig(configuration, logger.Object);

        Assert.AreEqual("01_Projects\\MBA", appConfig.Paths.NotebookVaultResourcesBasepath);
        Assert.AreEqual("/test/vault", appConfig.Paths.NotebookVaultFullpathRoot);
    }

    [TestMethod]
    public void PathsConfig_Should_Serialize_And_Deserialize_NotebookVaultResourcesBasepath()
    {
        // Arrange
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        string tempOneDriveRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "onedrive") + Path.DirectorySeparatorChar;

        var originalConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = tempVaultRoot,
            NotebookVaultResourcesBasepath = "01_Projects\\MBA",
            OnedriveFullpathRoot = tempOneDriveRoot,
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

    [TestMethod]
    public void PathsConfig_Should_Map_Json_Property_Name_For_NotebookVaultResourcesBasepath()
    {
        // Arrange - Create JSON with the exact property name used in config files
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        string tempOneDriveRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "onedrive") + Path.DirectorySeparatorChar;

        var jsonObject = new Dictionary<string, object?>
        {
            ["notebook_vault_fullpath_root"] = tempVaultRoot,
            ["notebook_vault_resources_basepath"] = "01_Projects\\MBA",
            ["onedrive_fullpath_root"] = tempOneDriveRoot,
            ["onedrive_resources_basepath"] = "Education\\MBA-Resources",
        };
        var json = JsonSerializer.Serialize(jsonObject);

        var deserializedConfig = JsonSerializer.Deserialize<PathsConfig>(json);

        Assert.IsNotNull(deserializedConfig);
        Assert.AreEqual(tempVaultRoot, deserializedConfig.NotebookVaultFullpathRoot);
        Assert.AreEqual("01_Projects\\MBA", deserializedConfig.NotebookVaultResourcesBasepath);
        Assert.AreEqual(tempOneDriveRoot, deserializedConfig.OnedriveFullpathRoot);
        Assert.AreEqual("Education\\MBA-Resources", deserializedConfig.OnedriveResourcesBasepath);
    }

    [TestMethod]
    public void AppConfig_Should_Load_NotebookVaultResourcesBasepath_From_JsonFile()
    {
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        string tempOneDriveRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "onedrive") + Path.DirectorySeparatorChar;
        string metadataSchema = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "config", "metadata-schema.yml");
        string logsDir = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "logs");
        string promptsDir = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "prompts");

        var jsonObject = new
        {
            ConfigFilePath = "test-config.json",
            paths = new Dictionary<string, string?>
            {
                ["onedrive_fullpath_root"] = tempOneDriveRoot,
                ["onedrive_resources_basepath"] = "Education\\MBA-Resources",
                ["notebook_vault_fullpath_root"] = tempVaultRoot,
                ["notebook_vault_resources_basepath"] = "01_Projects\\MBA",
                ["metadata_schema_file"] = metadataSchema,
                ["logging_dir"] = logsDir,
                ["prompts_path"] = promptsDir,
            },
        };
        var configJson = JsonSerializer.Serialize(jsonObject);

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, configJson);

        try
        {
            var appConfig = AppConfig.LoadFromJsonFile(tempFile);

            Assert.AreEqual(tempVaultRoot, appConfig.Paths.NotebookVaultFullpathRoot);
            Assert.AreEqual("01_Projects\\MBA", appConfig.Paths.NotebookVaultResourcesBasepath);
            Assert.AreEqual(tempOneDriveRoot, appConfig.Paths.OnedriveFullpathRoot);
            Assert.AreEqual("Education\\MBA-Resources", appConfig.Paths.OnedriveResourcesBasepath);
            Assert.AreEqual(metadataSchema, appConfig.Paths.MetadataSchemaFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #region GetEffectiveVaultRoot Tests

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Return_VaultRoot_When_No_ResourcesBasePath()
    {
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = tempVaultRoot,
            NotebookVaultResourcesBasepath = ""
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        Assert.AreEqual(tempVaultRoot, result);
    }

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Combine_VaultRoot_And_ResourcesBasePath()
    {
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = tempVaultRoot,
            NotebookVaultResourcesBasepath = @"01_Projects\MBA"
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        string expected = Path.Combine(tempVaultRoot, "01_Projects", "MBA");
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Return_Empty_When_VaultRoot_Empty()
    {
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = "",
            NotebookVaultResourcesBasepath = @"01_Projects\MBA"
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Return_Empty_When_VaultRoot_Null()
    {
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = null!,
            NotebookVaultResourcesBasepath = @"01_Projects\MBA"
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Handle_Leading_Separators_In_ResourcesBasePath()
    {
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = tempVaultRoot,
            NotebookVaultResourcesBasepath = @"\\01_Projects\MBA"
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        string expected = Path.Combine(tempVaultRoot, "01_Projects", "MBA");
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Handle_Forward_Slashes_In_ResourcesBasePath()
    {
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = tempVaultRoot,
            NotebookVaultResourcesBasepath = "01_Projects/MBA"
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        string expected = Path.Combine(tempVaultRoot, "01_Projects", "MBA");
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Handle_Mixed_Separators_In_ResourcesBasePath()
    {
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = tempVaultRoot,
            NotebookVaultResourcesBasepath = "/01_Projects\\MBA/"
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        string expected = Path.Combine(tempVaultRoot, "01_Projects", "MBA");
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetEffectiveVaultRoot_Should_Return_VaultRoot_When_ResourcesBasePath_Null()
    {
        string tempVaultRoot = Path.Combine(Path.GetTempPath(), "na-tests", Guid.NewGuid().ToString("N"), "vault");
        var pathsConfig = new PathsConfig
        {
            NotebookVaultFullpathRoot = tempVaultRoot,
            NotebookVaultResourcesBasepath = null!
        };

        string result = pathsConfig.GetEffectiveVaultRoot();
        Assert.AreEqual(tempVaultRoot, result);
    }

    #endregion
}
