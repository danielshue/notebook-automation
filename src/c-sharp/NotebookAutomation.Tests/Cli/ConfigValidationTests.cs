// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Cli;

/// <summary>
/// Unit tests for ConfigValidation static helpers.
/// </summary>
[TestClass]
public class ConfigValidationTests
{
    [TestInitialize]
    public void TestInit()
    {
        // Ensure DI is initialized before each test
        NotebookAutomation.Cli.Program.SetupDependencyInjection(null, false);
    }

    [TestMethod]
    public async Task RequireOpenAi_ReturnsFalse_WhenApiKeyMissing()
    {
        // Arrange
        var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        var config = new AppConfig
        {
            AiService = new AIServiceConfig
            {
                Provider = "openai",
            },
        };

        // Act
        var result = await ConfigValidation.RequireOpenAi(config);

        // Assert
        Assert.IsFalse(result);

        // Cleanup
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
    }

    /// <summary>
    /// Verifies that RequireOpenAi returns true when the OpenAI API key is present.
    /// </summary>
    [TestMethod]
    public async Task RequireOpenAi_ReturnsTrue_WhenApiKeyPresent()
    {
        // Arrange
        var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
        var config = new AppConfig
        {
            AiService = new AIServiceConfig
            {
                Provider = "openai",
            },
        };

        // Act
        var result = await ConfigValidation.RequireOpenAi(config);

        // Assert
        Assert.IsTrue(result);

        // Cleanup
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
    }

    /// <summary>
    /// Verifies that RequireAllPaths returns false when Paths configuration is null.
    /// </summary>
    [TestMethod]
    public void RequireAllPaths_ReturnsFalse_WhenPathsIsNull()
    {
        var config = new AppConfig { Paths = null! };
        var result = ConfigValidation.RequireAllPaths(config, out var missing);
        Assert.IsFalse(result);
        Assert.IsTrue(missing.Count > 0);
    }

    /// <summary>
    /// Verifies that RequireAllPaths returns false when all path fields are missing or whitespace.
    /// </summary>
    [TestMethod]
    public void RequireAllPaths_ReturnsFalse_WhenAllFieldsMissingOrWhitespace()
    {
        var config = new AppConfig
        {
            Paths = new PathsConfig
            {
                OnedriveFullpathRoot = " ",
                NotebookVaultFullpathRoot = null!,
                MetadataSchemaFile = string.Empty,
                OnedriveResourcesBasepath = null!,
                LoggingDir = string.Empty,
            },
        };
        var result = ConfigValidation.RequireAllPaths(config, out var missing);
        Assert.IsFalse(result);
        Assert.AreEqual(5, missing.Count);
    }

    /// <summary>
    /// Verifies that RequireMicrosoftGraph returns false when MicrosoftGraph configuration is null.
    /// </summary>
    [TestMethod]
    public async Task RequireMicrosoftGraph_ReturnsFalse_WhenMicrosoftGraphIsNull()
    {
        var config = new AppConfig { MicrosoftGraph = null! };
        var result = await ConfigValidation.RequireMicrosoftGraph(config);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that RequireMicrosoftGraph returns false when Scopes is null or empty.
    /// </summary>
    [TestMethod]
    public async Task RequireMicrosoftGraph_ReturnsFalse_WhenScopesIsNullOrEmpty()
    {
        var config1 = new AppConfig { MicrosoftGraph = new MicrosoftGraphConfig { ClientId = "id", ApiEndpoint = "ep", Authority = "auth", Scopes = null! } };
        var config2 = new AppConfig { MicrosoftGraph = new MicrosoftGraphConfig { ClientId = "id", ApiEndpoint = "ep", Authority = "auth", Scopes = [] } };
        Assert.IsFalse(await ConfigValidation.RequireMicrosoftGraph(config1));
        Assert.IsFalse(await ConfigValidation.RequireMicrosoftGraph(config2));
    }

    /// <summary>
    /// Verifies that RequireOpenAi returns false when AiService configuration is null.
    /// </summary>
    [TestMethod]
    public async Task RequireOpenAi_ReturnsFalse_WhenAiServiceIsNull()
    {
        var config = new AppConfig { AiService = null! };
        var result = await ConfigValidation.RequireOpenAi(config);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that RequireAllPaths returns true when all required paths are present.
    /// </summary>
    [TestMethod]
    public void RequireAllPaths_ReturnsTrue_WhenAllPathsPresent()
    {
        var config = new AppConfig
        {
            Paths = new PathsConfig
            {
                OnedriveFullpathRoot = "C:/resources",
                NotebookVaultFullpathRoot = "C:/vault",
                MetadataSchemaFile = "C:/meta/metadata-schema.yml",
                OnedriveResourcesBasepath = "C:/onedrive",
                LoggingDir = "C:/logs",
            },
        };
        var result = ConfigValidation.RequireAllPaths(config, out var missing);
        Assert.IsTrue(result);
        Assert.AreEqual(0, missing.Count);
    }

    /// <summary>
    /// Verifies that RequireAllPaths returns false and lists missing paths when some paths are missing.
    /// </summary>
    [TestMethod]
    public void RequireAllPaths_ReturnsFalse_AndListsMissing_WhenSomePathsMissing()
    {
        var config = new AppConfig
        {
            Paths = new PathsConfig
            {
                OnedriveFullpathRoot = string.Empty,
                NotebookVaultFullpathRoot = null!,
                MetadataSchemaFile = "schema.yml",
                OnedriveResourcesBasepath = "basepath",
                LoggingDir = null!,
            },
        };
        var result = ConfigValidation.RequireAllPaths(config, out var missing);
        Assert.IsFalse(result);
        CollectionAssert.Contains(missing, "paths.onedrive_fullpath_root");
        CollectionAssert.Contains(missing, "paths.notebook_vault_fullpath_root");
        CollectionAssert.Contains(missing, "paths.logging_dir");
        CollectionAssert.DoesNotContain(missing, "paths.metadata_schema_file");
        CollectionAssert.DoesNotContain(missing, "paths.onedrive_resources_basepath");
    }

    /// <summary>
    /// Verifies that RequireMicrosoftGraph returns false when required values are missing.
    /// </summary>
    [TestMethod]
    public async Task RequireMicrosoftGraph_ReturnsFalse_WhenMissingValues()
    {
        var config = new AppConfig
        {
            MicrosoftGraph = new MicrosoftGraphConfig
            {
                ClientId = null!,
                ApiEndpoint = string.Empty,
                Authority = null!,
                Scopes = [],
            },
        };
        var result = await ConfigValidation.RequireMicrosoftGraph(config);
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that RequireMicrosoftGraph returns true when all required values are present.
    /// </summary>
    [TestMethod]
    public async Task RequireMicrosoftGraph_ReturnsTrue_WhenAllValuesPresent()
    {
        var config = new AppConfig
        {
            MicrosoftGraph = new MicrosoftGraphConfig
            {
                ClientId = "id",
                ApiEndpoint = "endpoint",
                Authority = "authority",
                Scopes = ["scope1"],
            },
        };
        var result = await ConfigValidation.RequireMicrosoftGraph(config);
        Assert.IsTrue(result);
    }
}

