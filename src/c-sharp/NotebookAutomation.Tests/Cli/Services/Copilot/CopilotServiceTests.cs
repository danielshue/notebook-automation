// Licensed to the MIT License. See LICENSE file in the project root for full license information.

using Moq;

using NotebookAutomation.Cli.Services.Copilot;
using NotebookAutomation.Core.Configuration;

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
    private Mock<ILoggerFactory> loggerFactoryMock = null!;
    private AppConfig appConfig = null!;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<CopilotService>>();
        checkerLoggerMock = new Mock<ILogger<CopilotAvailabilityChecker>>();

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

        availabilityChecker = new CopilotAvailabilityChecker(checkerLoggerMock.Object, appConfig);
        sessionManagerMock = new Mock<ISessionManager>();
        notebookToolsMock = new Mock<INotebookTools>();
        systemMessageBuilderMock = new Mock<ISystemMessageBuilder>();
        loggerFactoryMock = new Mock<ILoggerFactory>();

        // Setup logger factory to return a logger mock
        var sessionLoggerMock = new Mock<ILogger<CopilotSessionAdapter>>();
        loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(sessionLoggerMock.Object);
    }

    private CopilotService CreateService()
    {
        return new CopilotService(
            loggerMock.Object,
            availabilityChecker,
            sessionManagerMock.Object,
            notebookToolsMock.Object,
            systemMessageBuilderMock.Object,
            loggerFactoryMock.Object,
            appConfig);
    }

    /// <summary>
    /// Tests that the service can be instantiated.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Act
        var service = CreateService();

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
            new CopilotService(null!, availabilityChecker, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object, loggerFactoryMock.Object, appConfig));
    }

    /// <summary>
    /// Tests that the constructor throws when availability checker is null.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullAvailabilityChecker_ShouldThrow()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new CopilotService(loggerMock.Object, null!, sessionManagerMock.Object, notebookToolsMock.Object, systemMessageBuilderMock.Object, loggerFactoryMock.Object, appConfig));
    }

    /// <summary>
    /// Tests that StartAsync handles SDK unavailability gracefully.
    /// Note: In test environments without the Copilot CLI, StartAsync will fail but not throw.
    /// </summary>
    [TestMethod]
    public async Task StartAsync_HandlesUnavailableSDKGracefully()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.StartAsync();

        // Assert
        // Service may or may not be running depending on whether SDK is available
        // The important thing is that it doesn't throw and handles the error gracefully
        // In CI/test environments where Copilot CLI isn't available, IsRunning will be false
        Assert.IsNotNull(service);
    }

    /// <summary>
    /// Tests that StopAsync handles cleanup gracefully even if not started.
    /// </summary>
    [TestMethod]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act - Stop without starting first
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
        var service = CreateService();

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
        var service = CreateService();
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
        var service = CreateService();

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
        var service = CreateService();

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
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            async () => await service.AskAsync("test question"));
    }
}
