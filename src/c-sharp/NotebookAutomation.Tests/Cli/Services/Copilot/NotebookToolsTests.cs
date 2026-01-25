// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Moq;

using NotebookAutomation.Cli.Services.Copilot;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Tests.Cli.Services.Copilot;

/// <summary>
/// Unit tests for the <see cref="NotebookTools"/> class.
/// </summary>
[TestClass]
public class NotebookToolsTests
{
    private Mock<ILogger<NotebookTools>> loggerMock = null!;
    private Mock<IServiceProvider> serviceProviderMock = null!;
    private AppConfig appConfig = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<NotebookTools>>();
        serviceProviderMock = new Mock<IServiceProvider>();
        appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = "C:\\TestVault",
                OnedriveFullpathRoot = "C:\\TestOneDrive"
            }
        };
    }

    /// <summary>
    /// Tests that the NotebookTools can be instantiated.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);

        // Assert
        Assert.IsNotNull(tools);
    }

    /// <summary>
    /// Tests that the constructor throws when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new NotebookTools(null!, serviceProviderMock.Object, appConfig));
    }

    /// <summary>
    /// Tests that the constructor throws when service provider is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new NotebookTools(loggerMock.Object, null!, appConfig));
    }

    /// <summary>
    /// Tests that the constructor throws when appConfig is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullAppConfig_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new NotebookTools(loggerMock.Object, serviceProviderMock.Object, null!));
    }

    /// <summary>
    /// Tests that GetAllTools returns tools after registration.
    /// </summary>
    [TestMethod]
    public void GetAllTools_AfterRegistration_ShouldReturnTools()
    {
        // Arrange
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);

        // Act
        var allTools = tools.GetAllTools();

        // Assert
        Assert.IsNotNull(allTools);
        Assert.IsTrue(allTools.Count > 0, "Should have registered tools");
    }

    /// <summary>
    /// Tests that GetToolsByCategory returns tools for valid category.
    /// </summary>
    [TestMethod]
    public void GetToolsByCategory_WithValidCategory_ShouldReturnTools()
    {
        // Arrange
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);

        // Act
        var vaultTools = tools.GetToolsByCategory("vault");

        // Assert
        Assert.IsNotNull(vaultTools);
        Assert.IsTrue(vaultTools.Count >= 4, "Should have at least 4 vault tools");
    }

    /// <summary>
    /// Tests that GetToolsByCategory returns empty for invalid category.
    /// </summary>
    [TestMethod]
    public void GetToolsByCategory_WithInvalidCategory_ShouldReturnEmpty()
    {
        // Arrange
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);

        // Act
        var unknownTools = tools.GetToolsByCategory("unknown");

        // Assert
        Assert.IsNotNull(unknownTools);
        Assert.AreEqual(0, unknownTools.Count);
    }

    /// <summary>
    /// Tests that GetTool returns a tool by name.
    /// </summary>
    [TestMethod]
    public void GetTool_WithValidName_ShouldReturnTool()
    {
        // Arrange
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);

        // Act
        var tool = tools.GetTool("vault_list_directory");

        // Assert
        Assert.IsNotNull(tool);
    }

    /// <summary>
    /// Tests that GetTool returns null for invalid name.
    /// </summary>
    [TestMethod]
    public void GetTool_WithInvalidName_ShouldReturnNull()
    {
        // Arrange
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);

        // Act
        var tool = tools.GetTool("nonexistent_tool");

        // Assert
        Assert.IsNull(tool);
    }

    /// <summary>
    /// Tests that RegisterAllTools can be called multiple times safely.
    /// </summary>
    [TestMethod]
    public void RegisterAllTools_CalledMultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);

        // Act
        tools.RegisterAllTools();
        var count1 = tools.GetAllTools().Count;
        tools.RegisterAllTools();
        var count2 = tools.GetAllTools().Count;

        // Assert
        Assert.AreEqual(count1, count2, "Tool count should not change on re-registration");
    }

    /// <summary>
    /// Tests that all expected tool categories are registered.
    /// </summary>
    [TestMethod]
    public void RegisterAllTools_ShouldRegisterAllCategories()
    {
        // Arrange
        var tools = new NotebookTools(loggerMock.Object, serviceProviderMock.Object, appConfig);
        var expectedCategories = new[] { "vault", "search", "open", "tag", "pdf", "video", "markdown", "config", "onedrive" };

        // Act
        tools.RegisterAllTools();

        // Assert
        foreach (var category in expectedCategories)
        {
            var categoryTools = tools.GetToolsByCategory(category);
            Assert.IsTrue(categoryTools.Count > 0, $"Category '{category}' should have tools");
        }
    }
}
