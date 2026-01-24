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
    private CopilotAvailabilityChecker availabilityChecker = null!;
    private Mock<ILogger<CopilotAvailabilityChecker>> checkerLoggerMock = null!;
    private Mock<ISessionManager> sessionManagerMock = null!;
    private Mock<INotebookTools> notebookToolsMock = null!;
    private Mock<ISystemMessageBuilder> systemMessageBuilderMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<CopilotService>>();
        checkerLoggerMock = new Mock<ILogger<CopilotAvailabilityChecker>>();
        availabilityChecker = new CopilotAvailabilityChecker(checkerLoggerMock.Object);
        sessionManagerMock = new Mock<ISessionManager>();
        notebookToolsMock = new Mock<INotebookTools>();
        systemMessageBuilderMock = new Mock<ISystemMessageBuilder>();
    }

    /// <summary>
    /// Tests that the service can be instantiated.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var service = new CopilotService(
            loggerMock.Object,
            availabilityChecker,
            sessionManagerMock.Object,
            notebookToolsMock.Object,
            systemMessageBuilderMock.Object);

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
            new CopilotService(null!, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object));
    }

    /// <summary>
    /// Tests that the constructor throws when availability checker is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullAvailabilityChecker_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new CopilotService(loggerMock.Object, null!, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object));
    }

    /// <summary>
    /// Tests that StartAsync sets IsRunning to true.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_ShouldSetIsRunningTrue()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object);

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
        var service = new CopilotService(loggerMock.Object, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object);
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
        var service = new CopilotService(loggerMock.Object, realChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object);

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
        var service = new CopilotService(loggerMock.Object, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object);
        await service.StartAsync();

        // Act
        await service.DisposeAsync();

        // Assert
        Assert.IsFalse(service.IsRunning);
    }

    /// <summary>
    /// Tests that CreateSessionAsync throws when service not started.
    /// </summary>
    [TestMethod]
    public async Task CreateSessionAsync_WhenNotStarted_ShouldThrow()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await service.CreateSessionAsync());
    }

    /// <summary>
    /// Tests that StartInteractiveChatAsync throws InvalidOperationException.
    /// </summary>
    [TestMethod]
    public async Task StartInteractiveChatAsync_ShouldThrowInvalidOperation()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await service.StartInteractiveChatAsync());
    }

    /// <summary>
    /// Tests that AskAsync throws when service not started.
    /// </summary>
    [TestMethod]
    public async Task AskAsync_WhenNotStarted_ShouldThrow()
    {
        // Arrange
        var service = new CopilotService(loggerMock.Object, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await service.AskAsync("test question"));
    }
}
