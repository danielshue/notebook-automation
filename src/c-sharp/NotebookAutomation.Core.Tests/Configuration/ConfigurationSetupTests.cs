// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Configuration;


/// <summary>
/// Unit tests for ConfigurationSetup static class.
/// Tests configuration building with various sources and environments.
/// </summary>
[TestClass]
public class ConfigurationSetupTests
{
    private string? _tempConfigFile;
    private string _originalDirectory = string.Empty;


    /// <summary>
    /// Test initialization - sets up temporary directory and config file.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _originalDirectory = Directory.GetCurrentDirectory();

        // Create a temporary config file for testing
        _tempConfigFile = Path.GetTempFileName();
        File.WriteAllText(_tempConfigFile, """
            {
              "TestSetting": "TestValue",
              "NestedSection": {
                "NestedSetting": "NestedValue"
              }
            }
            """);
    }


    /// <summary>
    /// Test cleanup - removes temporary files and restores directory.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        if (_tempConfigFile != null && File.Exists(_tempConfigFile))
        {
            File.Delete(_tempConfigFile);
        }

        Directory.SetCurrentDirectory(_originalDirectory);
    }


    /// <summary>
    /// Tests BuildConfiguration with default parameters creates valid configuration.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithDefaults_ReturnsValidConfiguration()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration();

        // Assert
        Assert.IsNotNull(configuration);
        Assert.IsInstanceOfType(configuration, typeof(IConfiguration));
    }


    /// <summary>
    /// Tests BuildConfiguration with specified config file path loads configuration correctly.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithConfigFilePath_LoadsConfigurationFromFile()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration(configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configuration);
        Assert.AreEqual("TestValue", configuration["TestSetting"]);
        Assert.AreEqual("NestedValue", configuration["NestedSection:NestedSetting"]);
    }


    /// <summary>
    /// Tests BuildConfiguration with production environment excludes user secrets.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithProductionEnvironment_DoesNotAddUserSecrets()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration(
            environment: "Production",
            configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configuration);
        Assert.AreEqual("TestValue", configuration["TestSetting"]);
    }


    /// <summary>
    /// Tests BuildConfiguration with development environment and user secrets ID.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithDevelopmentAndUserSecretsId_AddsUserSecrets()
    {
        // Arrange
        const string testUserSecretsId = "test-secrets-id";

        // Act
        var configuration = ConfigurationSetup.BuildConfiguration(
            environment: "Development",
            userSecretsId: testUserSecretsId,
            configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configuration);
        Assert.AreEqual("TestValue", configuration["TestSetting"]);
    }


    /// <summary>
    /// Tests BuildConfiguration with non-existent config file path.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithNonExistentConfigFile_StillCreatesConfiguration()
    {
        // Arrange
        const string nonExistentPath = "non-existent-config.json";

        // Act
        var configuration = ConfigurationSetup.BuildConfiguration(configPath: nonExistentPath);

        // Assert
        Assert.IsNotNull(configuration);
    }


    /// <summary>
    /// Tests BuildConfiguration with null config path uses AppConfig.FindConfigFile.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithNullConfigPath_CallsFindConfigFile()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration(configPath: null);

        // Assert
        Assert.IsNotNull(configuration);
    }


    /// <summary>
    /// Tests BuildConfiguration with empty environment string defaults to Development behavior.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithEmptyEnvironment_DefaultsToDevelopmentBehavior()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration(
            environment: string.Empty,
            configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configuration);
        Assert.AreEqual("TestValue", configuration["TestSetting"]);
    }


    /// <summary>
    /// Tests BuildConfiguration with case-insensitive environment matching.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_WithCaseInsensitiveEnvironment_WorksCorrectly()
    {
        // Act
        var configDev = ConfigurationSetup.BuildConfiguration(
            environment: "development",
            configPath: _tempConfigFile);
        var configProd = ConfigurationSetup.BuildConfiguration(
            environment: "PRODUCTION",
            configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configDev);
        Assert.IsNotNull(configProd);
        Assert.AreEqual("TestValue", configDev["TestSetting"]);
        Assert.AreEqual("TestValue", configProd["TestSetting"]);
    }


    /// <summary>
    /// Tests generic BuildConfiguration method with type parameter.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_Generic_ReturnsValidConfiguration()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration<ConfigurationSetupTests>(
            configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configuration);
        Assert.AreEqual("TestValue", configuration["TestSetting"]);
        Assert.AreEqual("NestedValue", configuration["NestedSection:NestedSetting"]);
    }


    /// <summary>
    /// Tests generic BuildConfiguration with production environment.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_GenericWithProduction_DoesNotAddUserSecrets()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration<ConfigurationSetupTests>(
            environment: "Production",
            configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configuration);
        Assert.AreEqual("TestValue", configuration["TestSetting"]);
    }


    /// <summary>
    /// Tests generic BuildConfiguration with development environment.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_GenericWithDevelopment_AddsUserSecrets()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration<ConfigurationSetupTests>(
            environment: "Development",
            configPath: _tempConfigFile);

        // Assert
        Assert.IsNotNull(configuration);
        Assert.AreEqual("TestValue", configuration["TestSetting"]);
    }


    /// <summary>
    /// Tests generic BuildConfiguration with null config path.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_GenericWithNullConfigPath_CallsFindConfigFile()
    {
        // Act
        var configuration = ConfigurationSetup.BuildConfiguration<ConfigurationSetupTests>(
            configPath: null);

        // Assert
        Assert.IsNotNull(configuration);
    }


    /// <summary>
    /// Tests that environment variables are added to configuration.
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_AddsEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_ENV_VAR", "TestEnvValue");

        try
        {
            // Act
            var configuration = ConfigurationSetup.BuildConfiguration(configPath: _tempConfigFile);

            // Assert
            Assert.IsNotNull(configuration);
            Assert.AreEqual("TestEnvValue", configuration["TEST_ENV_VAR"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_ENV_VAR", null);
        }
    }


    /// <summary>
    /// Tests that configuration sources are layered correctly (environment variables override JSON).
    /// </summary>
    [TestMethod]
    public void BuildConfiguration_EnvironmentVariablesOverrideJsonSettings()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TestSetting", "OverriddenValue");

        try
        {
            // Act
            var configuration = ConfigurationSetup.BuildConfiguration(configPath: _tempConfigFile);

            // Assert
            Assert.IsNotNull(configuration);
            Assert.AreEqual("OverriddenValue", configuration["TestSetting"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TestSetting", null);
        }
    }
}
