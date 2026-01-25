// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Moq;

using NotebookAutomation.Cli.Services.Copilot;

namespace NotebookAutomation.Tests.Cli.Services.Copilot;

/// <summary>
/// Unit tests for the <see cref="SystemMessageBuilder"/> class.
/// </summary>
[TestClass]
public class SystemMessageBuilderTests
{
    private Mock<ILogger<SystemMessageBuilder>> loggerMock = null!;
    private AppConfig config = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<SystemMessageBuilder>>();
        config = new AppConfig();
    }

    /// <summary>
    /// Tests that the SystemMessageBuilder can be instantiated.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var builder = new SystemMessageBuilder(config, loggerMock.Object);

        // Assert
        Assert.IsNotNull(builder);
    }

    /// <summary>
    /// Tests that the constructor throws when config is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullConfig_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new SystemMessageBuilder(null!, loggerMock.Object));
    }

    /// <summary>
    /// Tests that the constructor throws when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new SystemMessageBuilder(config, null!));
    }

    /// <summary>
    /// Tests that BuildDefaultSystemMessage returns a non-empty message.
    /// </summary>
    [TestMethod]
    public void BuildDefaultSystemMessage_ShouldReturnNonEmptyMessage()
    {
        // Arrange
        var builder = new SystemMessageBuilder(config, loggerMock.Object);

        // Act
        var message = builder.BuildDefaultSystemMessage();

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(message));
        Assert.IsTrue(message.Contains("Notebook Automation"));
    }

    /// <summary>
    /// Tests that BuildSystemMessageWithTools includes tool list.
    /// </summary>
    [TestMethod]
    public void BuildSystemMessageWithTools_WithTools_ShouldIncludeToolList()
    {
        // Arrange
        var builder = new SystemMessageBuilder(config, loggerMock.Object);
        var tools = new List<string> { "vault_generate_index", "tag_consolidate" };

        // Act
        var message = builder.BuildSystemMessageWithTools(tools);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(message));
        Assert.IsTrue(message.Contains("vault_generate_index"));
        Assert.IsTrue(message.Contains("tag_consolidate"));
        Assert.IsTrue(message.Contains("Available tools"));
    }

    /// <summary>
    /// Tests that BuildSystemMessageWithTools handles empty tool list.
    /// </summary>
    [TestMethod]
    public void BuildSystemMessageWithTools_WithEmptyList_ShouldReturnBaseMessage()
    {
        // Arrange
        var builder = new SystemMessageBuilder(config, loggerMock.Object);
        var tools = new List<string>();

        // Act
        var message = builder.BuildSystemMessageWithTools(tools);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(message));
        Assert.IsFalse(message.Contains("Available tools"));
    }

    /// <summary>
    /// Tests that BuildCustomSystemMessage returns the custom message.
    /// </summary>
    [TestMethod]
    public void BuildCustomSystemMessage_WithCustomText_ShouldIncludeIt()
    {
        // Arrange
        var builder = new SystemMessageBuilder(config, loggerMock.Object);
        var customMessage = "You are a specialized assistant for testing.";

        // Act
        var message = builder.BuildCustomSystemMessage(customMessage, includeToolContext: true);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(message));
        Assert.IsTrue(message.Contains("specialized assistant for testing"));
    }

    /// <summary>
    /// Tests that BuildCustomSystemMessage without tool context excludes it.
    /// </summary>
    [TestMethod]
    public void BuildCustomSystemMessage_WithoutToolContext_ShouldExcludeIt()
    {
        // Arrange
        var builder = new SystemMessageBuilder(config, loggerMock.Object);
        var customMessage = "Custom message only.";

        // Act
        var message = builder.BuildCustomSystemMessage(customMessage, includeToolContext: false);

        // Assert
        Assert.AreEqual(customMessage, message);
    }

    /// <summary>
    /// Tests that BuildCustomSystemMessage with empty message returns default.
    /// </summary>
    [TestMethod]
    public void BuildCustomSystemMessage_WithEmptyMessage_ShouldReturnDefault()
    {
        // Arrange
        var builder = new SystemMessageBuilder(config, loggerMock.Object);

        // Act
        var message = builder.BuildCustomSystemMessage(string.Empty);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(message));
        Assert.IsTrue(message.Contains("Notebook Automation"));
    }
}
