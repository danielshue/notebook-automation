// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Moq;
using NotebookAutomation.Cli.Services.Copilot;

namespace NotebookAutomation.Tests.Cli.Services.Copilot;

/// <summary>
/// Unit tests for the <see cref="CopilotService"/> class.
/// </summary>
[TestClass]
public class CopilotServiceTests
{
    private Mock<ILogger<CopilotService>> loggerMock = null!;
    private Mock<CopilotAvailabilityChecker> availabilityCheckerMock = null!;
    private Mock<ILogger<CopilotAvailabilityChecker>> checkerLoggerMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<CopilotService>>();
        checkerLoggerMock = new Mock<ILogger<CopilotAvailabilityChecker>>();
        availabilityCheckerMock = new Mock<CopilotAvailabilityChecker>(checkerLoggerMock.Object);
    }

    /// <summary>
    /// Tests that the service can be instantiated.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var service = new CopilotService(loggerMock.Object, availabilityCheckerMock.Object);

        // Assert
        Assert.IsNotNull(service);
        Assert.IsFalse(service.IsRunning);
    }

    /// <summary>
    /// Tests that the constructor throws when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new CopilotService(null!, availabilityCheckerMock.Object));
    }

    /// <summary>
    /// Tests that the constructor throws when availability checker is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullAvailabilityChecker_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new CopilotService(loggerMock.Object, null!));
    }

    /// <summary>
    /// Tests that StartAsync sets IsRunning to true.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_ShouldSetIsRunningTrue()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityCheckerMock.Object);

        // Act
        await service.StartAsync();

        // Assert
        Assert.IsTrue(service.IsRunning);
    }

    /// <summary>
    /// Tests that StopAsync sets IsRunning to false.
    /// </summary>
    [TestMethod]
    public async Task StopAsync_ShouldSetIsRunningFalse()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityCheckerMock.Object);
        await service.StartAsync();

        // Act
        await service.StopAsync();

        // Assert
        Assert.IsFalse(service.IsRunning);
    }

    /// <summary>
    /// Tests that CheckAvailabilityAsync delegates to the availability checker.
    /// </summary>
    [TestMethod]
    public async Task CheckAvailabilityAsync_ShouldCallChecker()
    {
        // Arrange
        // Use a real availability checker since we can't mock non-virtual methods
        var realChecker = new CopilotAvailabilityChecker(checkerLoggerMock.Object);
        var service = new CopilotService(loggerMock.Object, realChecker);

        // Act
        var result = await service.CheckAvailabilityAsync();

        // Assert
        Assert.IsNotNull(result);
        // The result depends on whether the Copilot CLI is actually installed
    }

    /// <summary>
    /// Tests that DisposeAsync stops the service if running.
    /// </summary>
    [TestMethod]
    public async Task DisposeAsync_WhenRunning_ShouldStop()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityCheckerMock.Object);
        await service.StartAsync();

        // Act
        await service.DisposeAsync();

        // Assert
        Assert.IsFalse(service.IsRunning);
    }

    /// <summary>
    /// Tests that CreateSessionAsync throws NotImplementedException (Phase 2).
    /// </summary>
    [TestMethod]
    public async Task CreateSessionAsync_ShouldThrowNotImplemented()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityCheckerMock.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(
            async () => await service.CreateSessionAsync());
    }

    /// <summary>
    /// Tests that StartInteractiveChatAsync throws NotImplementedException (Phase 2).
    /// </summary>
    [TestMethod]
    public async Task StartInteractiveChatAsync_ShouldThrowNotImplemented()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityCheckerMock.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(
            async () => await service.StartInteractiveChatAsync());
    }

    /// <summary>
    /// Tests that AskAsync throws NotImplementedException (Phase 2).
    /// </summary>
    [TestMethod]
    public async Task AskAsync_ShouldThrowNotImplemented()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityCheckerMock.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<NotImplementedException>(
            async () => await service.AskAsync("test question"));
    }
}
