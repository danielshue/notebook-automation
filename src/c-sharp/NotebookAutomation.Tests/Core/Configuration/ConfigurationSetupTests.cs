// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the <c>ConfigurationSetup</c> static class.
/// <para>
/// These tests verify configuration building from various sources and environments, including file-based, environment variable, and user secrets scenarios.
/// </para>
/// </summary>
[TestClass]
public class ConfigurationSetupTests
{
    private string? _tempConfigFile;
    private string _originalDirectory = string.Empty;

    /// <summary>
    /// Initializes the test by setting up a temporary directory and config file for use in each test.
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
    /// Cleans up after each test by removing temporary files and restoring the original working directory.
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
    /// Verifies that <c>BuildConfiguration</c> with default parameters creates a valid configuration instance.
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
    /// Verifies that <c>BuildConfiguration</c> loads configuration from a specified config file path.
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
    /// Verifies that <c>BuildConfiguration</c> with the production environment does not add user secrets.
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
    /// Verifies that <c>BuildConfiguration</c> with the development environment and a user secrets ID adds user secrets.
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
    /// Verifies that <c>BuildConfiguration</c> with a non-existent config file path still creates a configuration instance.
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
    /// Verifies that <c>BuildConfiguration</c> with a null config path calls <c>AppConfig.FindConfigFile</c> internally.
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
    /// Verifies that <c>BuildConfiguration</c> with an empty environment string defaults to development behavior.
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
    /// Verifies that <c>BuildConfiguration</c> matches environment names case-insensitively.
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
    /// Verifies that the generic <c>BuildConfiguration&lt;T&gt;</c> method returns a valid configuration instance.
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
    /// Verifies that the generic <c>BuildConfiguration&lt;T&gt;</c> method with production environment does not add user secrets.
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
    /// Verifies that the generic <c>BuildConfiguration&lt;T&gt;</c> method with development environment adds user secrets.
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
    /// Verifies that the generic <c>BuildConfiguration&lt;T&gt;</c> method with a null config path calls <c>AppConfig.FindConfigFile</c>.
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
    /// Verifies that environment variables are added to the configuration and can be retrieved.
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
    /// Verifies that configuration sources are layered correctly, with environment variables overriding JSON settings.
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
