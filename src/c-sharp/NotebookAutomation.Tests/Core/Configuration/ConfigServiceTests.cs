// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Configuration.Validation;

namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the <see cref="ConfigService"/> class.
/// Tests cover configuration retrieval, updates, and validation.
/// </summary>
[TestClass]
public class ConfigServiceTests
{
    private Mock<ILogger<ConfigService>> _loggerMock = null!;
    private Mock<IConfigurationValidationService> _validationServiceMock = null!;
    private UserSecretsHelper _userSecrets = null!;
    private AppConfig _appConfig = null!;
    private Dictionary<string, string?> _secretsData = null!;

    /// <summary>
    /// Set up test dependencies before each test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<ConfigService>>();
        _validationServiceMock = new Mock<IConfigurationValidationService>();

        // Create actual AppConfig with test values
        _appConfig = new AppConfig
        {
            ConfigFilePath = "test-config.json",
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = "C:/test/vault",
                OnedriveFullpathRoot = "C:/test/onedrive",
                PromptsPath = "prompts",
                LoggingDir = "logs"
            },
            AiService = new AIServiceConfig
            {
                Provider = "openai",
                OpenAI = new OpenAiProviderConfig
                {
                    Model = "gpt-4",
                    Endpoint = "https://api.openai.com"
                }
            },
            MicrosoftGraph = new MicrosoftGraphConfig
            {
                TenantId = "test-tenant",
                ClientId = "test-client"
            }
        };

        // Create real UserSecretsHelper with mock IConfiguration
        _secretsData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_secretsData)
            .Build();
        _userSecrets = new UserSecretsHelper(configuration);
    }

    /// <summary>
    /// Helper to set up secrets for testing.
    /// </summary>
    private void SetupSecret(string key, string? value)
    {
        _secretsData[$"UserSecrets:{key}"] = value;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(_secretsData)
            .Build();
        _userSecrets = new UserSecretsHelper(configuration);
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that GetCurrentConfig returns a properly populated ConfigView.
    /// </summary>
    [TestMethod]
    public void GetCurrentConfig_ReturnsPopulatedConfigView()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetCurrentConfig();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test-config.json", result.ConfigFilePath);
        Assert.AreEqual("C:/test/vault", result.Paths.NotebookVaultRoot);
        Assert.AreEqual("openai", result.AiService.Provider);
    }

    /// <summary>
    /// Verifies that GetCurrentConfig includes path information.
    /// </summary>
    [TestMethod]
    public void GetCurrentConfig_IncludesPathInformation()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetCurrentConfig();

        // Assert
        Assert.IsNotNull(result.Paths);
        Assert.AreEqual("C:/test/onedrive", result.Paths.OnedriveRoot);
        Assert.AreEqual("prompts", result.Paths.PromptsPath);
    }

    #endregion

    #region GetConfigValue Tests

    /// <summary>
    /// Verifies that GetConfigValue returns null for empty key.
    /// </summary>
    [TestMethod]
    public void GetConfigValue_ReturnsNullForEmptyKey()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetConfigValue("");

        // Assert
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that GetConfigValue returns null for whitespace key.
    /// </summary>
    [TestMethod]
    public void GetConfigValue_ReturnsNullForWhitespaceKey()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetConfigValue("   ");

        // Assert
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that GetConfigValue returns null for unknown key.
    /// </summary>
    [TestMethod]
    public void GetConfigValue_ReturnsNullForUnknownKey()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetConfigValue("completely.unknown.key");

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region UpdateConfig Tests

    /// <summary>
    /// Verifies that UpdateConfig returns error for empty key.
    /// </summary>
    [TestMethod]
    public void UpdateConfig_ReturnsErrorForEmptyKey()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.UpdateConfig("", "value");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage?.Contains("empty"));
    }

    /// <summary>
    /// Verifies that UpdateConfig returns error for whitespace key.
    /// </summary>
    [TestMethod]
    public void UpdateConfig_ReturnsErrorForWhitespaceKey()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.UpdateConfig("   ", "value");

        // Assert
        Assert.IsFalse(result.Success);
    }

    /// <summary>
    /// Verifies that UpdateConfig returns error for unknown key.
    /// </summary>
    [TestMethod]
    public void UpdateConfig_ReturnsErrorForUnknownKey()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.UpdateConfig("unknown.key.path", "value");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage?.Contains("Unknown"));
    }

    #endregion

    #region ValidateConfigAsync Tests

    /// <summary>
    /// Verifies that ValidateConfigAsync returns valid result when no issues.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigAsync_ReturnsValidWhenNoIssues()
    {
        // Arrange
        var service = CreateService();
        _validationServiceMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfigurationValidationResult.Success("test-config.json"));

        // Act
        var result = await service.ValidateConfigAsync();

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Issues.Count);
    }

    /// <summary>
    /// Verifies that ValidateConfigAsync returns issues when validation fails.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigAsync_ReturnsIssuesWhenValidationFails()
    {
        // Arrange
        var service = CreateService();
        var issues = new List<ConfigurationValidationIssue>
        {
            new(
                ConfigurationValidationIssueSeverity.Error,
                "paths.vault_root",
                "Path does not exist",
                null)
        };
        _validationServiceMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ConfigurationValidationResult.FromIssues(issues, "test-config.json"));

        // Act
        var result = await service.ValidateConfigAsync();

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Count > 0);
    }

    /// <summary>
    /// Verifies that ValidateConfigAsync handles exception gracefully.
    /// </summary>
    [TestMethod]
    public async Task ValidateConfigAsync_HandlesExceptionGracefully()
    {
        // Arrange
        var service = CreateService();
        _validationServiceMock
            .Setup(v => v.ValidateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Validation error"));

        // Act
        var result = await service.ValidateConfigAsync();

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Count > 0);
    }

    #endregion

    #region ListConfigKeys Tests

    /// <summary>
    /// Verifies that ListConfigKeys returns a non-empty list.
    /// </summary>
    [TestMethod]
    public void ListConfigKeys_ReturnsNonEmptyList()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ListConfigKeys();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0);
    }

    /// <summary>
    /// Verifies that ListConfigKeys includes path keys.
    /// </summary>
    [TestMethod]
    public void ListConfigKeys_IncludesPathKeys()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ListConfigKeys();

        // Assert
        Assert.IsTrue(result.Any(k => k.Category == "paths"));
    }

    /// <summary>
    /// Verifies that ListConfigKeys includes AI service keys.
    /// </summary>
    [TestMethod]
    public void ListConfigKeys_IncludesAiServiceKeys()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ListConfigKeys();

        // Assert
        Assert.IsTrue(result.Any(k => k.Category == "aiservice"));
    }

    /// <summary>
    /// Verifies that ListConfigKeys returns keys sorted by category.
    /// </summary>
    [TestMethod]
    public void ListConfigKeys_ReturnsSortedByCategory()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.ListConfigKeys();

        // Assert
        var categories = result.Select(k => k.Category).ToList();
        var sortedCategories = categories.OrderBy(c => c).ToList();
        CollectionAssert.AreEqual(sortedCategories, categories);
    }

    #endregion

    #region GetSecretsStatus Tests

    /// <summary>
    /// Verifies that GetSecretsStatus returns status object.
    /// </summary>
    [TestMethod]
    public void GetSecretsStatus_ReturnsStatusObject()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetSecretsStatus();

        // Assert
        Assert.IsNotNull(result);
    }

    /// <summary>
    /// Verifies that GetSecretsStatus reflects configured secrets.
    /// </summary>
    [TestMethod]
    public void GetSecretsStatus_ReflectsConfiguredSecrets()
    {
        // Arrange
        SetupSecret("OpenAI:ApiKey", "test-api-key");
        // Azure:ApiKey not set, so should return false
        var service = CreateService();

        // Act
        var result = service.GetSecretsStatus();

        // Assert
        Assert.IsTrue(result.HasOpenAiApiKey);
        Assert.IsFalse(result.HasAzureApiKey);
    }

    #endregion

    #region GetConfigFilePath Tests

    /// <summary>
    /// Verifies that GetConfigFilePath returns configured path.
    /// </summary>
    [TestMethod]
    public void GetConfigFilePath_ReturnsConfiguredPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.GetConfigFilePath();

        // Assert
        Assert.AreEqual("test-config.json", result);
    }

    /// <summary>
    /// Verifies that GetConfigFilePath returns unknown when path is null.
    /// </summary>
    [TestMethod]
    public void GetConfigFilePath_ReturnsUnknownWhenNull()
    {
        // Arrange
        _appConfig.ConfigFilePath = null;
        var service = CreateService();

        // Act
        var result = service.GetConfigFilePath();

        // Assert
        Assert.AreEqual("unknown", result);
    }

    #endregion

    #region Helper Methods

    private ConfigService CreateService()
    {
        return new ConfigService(
            _appConfig,
            _userSecrets,
            _validationServiceMock.Object,
            _loggerMock.Object);
    }

    #endregion
}
