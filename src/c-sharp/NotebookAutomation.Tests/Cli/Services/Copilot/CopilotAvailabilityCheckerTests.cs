// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Moq;

using NotebookAutomation.Cli.Services.Copilot;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Tests.Cli.Services.Copilot;

/// <summary>
/// Unit tests for the <see cref="CopilotAvailabilityChecker"/> class.
/// </summary>
[TestClass]
public class CopilotAvailabilityCheckerTests
{
    private Mock<ILogger<CopilotAvailabilityChecker>> loggerMock = null!;
    private AppConfig appConfig = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<CopilotAvailabilityChecker>>();

        // Create a basic AppConfig with Copilot enabled but no API key
        appConfig = new AppConfig
        {
            Copilot = new CopilotConfig
            {
                Enabled = true,
                AutoChatMode = false
            },
            AiService = new AIServiceConfig
            {
                Provider = "openai"
            }
        };
    }

    /// <summary>
    /// Tests that the availability checker can be instantiated.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidLogger_ShouldSucceed()
    {
        // Act
        var checker = new CopilotAvailabilityChecker(loggerMock.Object, appConfig);

        // Assert
        Assert.IsNotNull(checker);
    }

    /// <summary>
    /// Tests that the constructor throws when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new CopilotAvailabilityChecker(null!, appConfig));
    }

    /// <summary>
    /// Tests that CheckAvailabilityAsync returns unavailable when no API key is set.
    /// </summary>
    [TestMethod]
    public async Task CheckAvailabilityAsync_WithoutApiKey_ReturnsUnavailable()
    {
        // Arrange
        var checker = new CopilotAvailabilityChecker(loggerMock.Object, appConfig);

        // Act
        var result = await checker.CheckAvailabilityAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsAvailable);
        Assert.IsTrue(result.ErrorMessage?.Contains("API key") == true);
    }

    /// <summary>
    /// Tests that CheckAvailabilityAsync handles cancellation.
    /// </summary>
    [TestMethod]
    public async Task CheckAvailabilityAsync_WithCancellation_ShouldComplete()
    {
        // Arrange
        var checker = new CopilotAvailabilityChecker(loggerMock.Object, appConfig);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await checker.CheckAvailabilityAsync(cts.Token);

        // Assert
        Assert.IsNotNull(result);
        // Should still return a result even with cancellation
    }
}
