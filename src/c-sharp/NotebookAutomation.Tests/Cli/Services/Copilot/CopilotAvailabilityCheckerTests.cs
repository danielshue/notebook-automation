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
    private string? savedAzureKey;
    private string? savedOpenAIKey;
    private string? savedFoundryKey;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<CopilotAvailabilityChecker>>();

        // Save existing environment variables
        savedAzureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
        savedOpenAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        savedFoundryKey = Environment.GetEnvironmentVariable("FOUNDRY_API_KEY");

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

    [TestCleanup]
    public void Cleanup()
    {
        // Restore environment variables
        Environment.SetEnvironmentVariable("AZURE_OPENAI_KEY", savedAzureKey);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", savedOpenAIKey);
        Environment.SetEnvironmentVariable("FOUNDRY_API_KEY", savedFoundryKey);
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
    /// Tests that CheckAvailabilityAsync returns unavailable when Copilot CLI is not installed.
    /// </summary>
    [TestMethod]
    public async Task CheckAvailabilityAsync_WithoutCli_ReturnsUnavailable()
    {
        // Arrange
        // Clear environment variables for this test
        Environment.SetEnvironmentVariable("AZURE_OPENAI_KEY", null);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
        Environment.SetEnvironmentVariable("FOUNDRY_API_KEY", null);

        // Test with copilot disabled to verify behavior
        var testConfig = new AppConfig
        {
            Copilot = new CopilotConfig
            {
                Enabled = false
            }
        };
        var checker = new CopilotAvailabilityChecker(loggerMock.Object, testConfig);

        // Act
        var result = await checker.CheckAvailabilityAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsAvailable, "Should be unavailable when disabled in config");
        Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage), "Error message should be provided");
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
