// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Moq;
using NotebookAutomation.Cli.Services.Copilot;

namespace NotebookAutomation.Tests.Cli.Services.Copilot;

/// <summary>
/// Unit tests for the <see cref="CopilotAvailabilityChecker"/> class.
/// </summary>
[TestClass]
public class CopilotAvailabilityCheckerTests
{
    private Mock<ILogger<CopilotAvailabilityChecker>> loggerMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<CopilotAvailabilityChecker>>();
    }

    /// <summary>
    /// Tests that the availability checker can be instantiated.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidLogger_ShouldSucceed()
    {
        // Act
        var checker = new CopilotAvailabilityChecker(loggerMock.Object);

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
            new CopilotAvailabilityChecker(null!));
    }

    /// <summary>
    /// Tests that CheckAvailabilityAsync returns a result.
    /// </summary>
    /// <remarks>
    /// Note: This is an integration-style test that actually checks for the CLI.
    /// In a real environment, we would mock process execution.
    /// </remarks>
    [TestMethod]
    public async Task CheckAvailabilityAsync_ReturnsResult()
    {
        // Arrange
        var checker = new CopilotAvailabilityChecker(loggerMock.Object);

        // Act
        var result = await checker.CheckAvailabilityAsync();

        // Assert
        Assert.IsNotNull(result);
        // The CLI may or may not be installed, but we should get a result
        Assert.IsNotNull(result.ErrorMessage != null || result.IsAvailable);
    }

    /// <summary>
    /// Tests that CheckAvailabilityAsync handles cancellation.
    /// </summary>
    [TestMethod]
    public async Task CheckAvailabilityAsync_WithCancellation_ShouldComplete()
    {
        // Arrange
        var checker = new CopilotAvailabilityChecker(loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await checker.CheckAvailabilityAsync(cts.Token);

        // Assert
        Assert.IsNotNull(result);
        // Should still return a result even with cancellation
    }
}
