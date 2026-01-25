// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Commands;
using NotebookAutomation.Cli.Services.Copilot;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Unit tests for the <see cref="CopilotCommands"/> class.
/// </summary>
[TestClass]
public class CopilotCommandsTests
{
    private IServiceProvider? serviceProvider;
    private CopilotCommands? copilotCommands;

    /// <summary>
    /// Initializes test setup before each test method.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        // Create a minimal service provider for testing
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Add minimal AppConfig
        var appConfig = new AppConfig();
        services.AddSingleton(appConfig);

        // Add mock ICopilotService
        var mockCopilotService = new Mock<ICopilotService>();
        mockCopilotService.Setup(x => x.CheckAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CopilotAvailabilityResult(
                IsAvailable: true,
                IsCliInstalled: true,
                IsAuthenticated: true,
                CliVersion: "1.0.0",
                ErrorMessage: null));
        services.AddSingleton(mockCopilotService.Object);

        serviceProvider = services.BuildServiceProvider();

        copilotCommands = new CopilotCommands(
            serviceProvider.GetRequiredService<ILogger<CopilotCommands>>(),
            serviceProvider);
    }

    /// <summary>
    /// Cleans up after each test method.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        (serviceProvider as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Tests that RegisterCommands adds the copilot command to the root command.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_AddsCopilotCommand()
    {
        // Arrange
        var rootCommand = new RootCommand("Test root command");

        // Act
        copilotCommands!.RegisterCommands(rootCommand);

        // Assert
        var copilotCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "copilot");
        Assert.IsNotNull(copilotCommand, "Copilot command should be added to root command");
        Assert.AreEqual("Enter interactive AI chat mode or manage GitHub Copilot", copilotCommand.Description);
    }

    /// <summary>
    /// Tests that copilot command has the expected chat options.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_CopilotCommand_HasChatOptions()
    {
        // Arrange
        var rootCommand = new RootCommand("Test root command");

        // Act
        copilotCommands!.RegisterCommands(rootCommand);

        // Assert
        var copilotCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "copilot");
        Assert.IsNotNull(copilotCommand, "Copilot command should exist");

        var resumeOption = copilotCommand.Options.FirstOrDefault(o => o.Name == "resume");
        Assert.IsNotNull(resumeOption, "Should have --resume option");

        var sessionOption = copilotCommand.Options.FirstOrDefault(o => o.Name == "session");
        Assert.IsNotNull(sessionOption, "Should have --session option");

        var modelOption = copilotCommand.Options.FirstOrDefault(o => o.Name == "model");
        Assert.IsNotNull(modelOption, "Should have --model option");

        var noBannerOption = copilotCommand.Options.FirstOrDefault(o => o.Name == "no-banner");
        Assert.IsNotNull(noBannerOption, "Should have --no-banner option");

        var highContrastOption = copilotCommand.Options.FirstOrDefault(o => o.Name == "high-contrast");
        Assert.IsNotNull(highContrastOption, "Should have --high-contrast option");
    }

    /// <summary>
    /// Tests that copilot command has the expected subcommands.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_CopilotCommand_HasExpectedSubcommands()
    {
        // Arrange
        var rootCommand = new RootCommand("Test root command");

        // Act
        copilotCommands!.RegisterCommands(rootCommand);

        // Assert
        var copilotCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "copilot");
        Assert.IsNotNull(copilotCommand, "Copilot command should exist");

        var statusCommand = copilotCommand.Subcommands.FirstOrDefault(c => c.Name == "status");
        Assert.IsNotNull(statusCommand, "Should have 'status' subcommand");
        Assert.AreEqual("Check GitHub Copilot CLI availability and authentication status", statusCommand.Description);

        var installGuideCommand = copilotCommand.Subcommands.FirstOrDefault(c => c.Name == "install-guide");
        Assert.IsNotNull(installGuideCommand, "Should have 'install-guide' subcommand");
        Assert.AreEqual("Display platform-specific installation instructions for GitHub Copilot CLI", installGuideCommand.Description);

        var installCommand = copilotCommand.Subcommands.FirstOrDefault(c => c.Name == "install");
        Assert.IsNotNull(installCommand, "Should have 'install' subcommand");
        Assert.AreEqual("Attempt to automatically install GitHub Copilot CLI (Windows only)", installCommand.Description);
    }

    /// <summary>
    /// Tests that copilot command does NOT have the deprecated --status option.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_CopilotCommand_DoesNotHaveStatusOption()
    {
        // Arrange
        var rootCommand = new RootCommand("Test root command");

        // Act
        copilotCommands!.RegisterCommands(rootCommand);

        // Assert
        var copilotCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "copilot");
        Assert.IsNotNull(copilotCommand, "Copilot command should exist");

        var statusOption = copilotCommand.Options.FirstOrDefault(o => o.Name == "status");
        Assert.IsNull(statusOption, "Should NOT have --status option (should use 'status' subcommand instead)");
    }

    /// <summary>
    /// Tests that RegisterCommands adds the ask command.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_AddsAskCommand()
    {
        // Arrange
        var rootCommand = new RootCommand("Test root command");

        // Act
        copilotCommands!.RegisterCommands(rootCommand);

        // Assert
        var askCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "ask");
        Assert.IsNotNull(askCommand, "Ask command should be added to root command");
        Assert.AreEqual("Ask a one-shot question to the AI", askCommand.Description);
    }

    /// <summary>
    /// Tests that the deprecated 'chat' command is NOT registered.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_DoesNotRegisterChatCommand()
    {
        // Arrange
        var rootCommand = new RootCommand("Test root command");

        // Act
        copilotCommands!.RegisterCommands(rootCommand);

        // Assert
        var chatCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "chat");
        Assert.IsNull(chatCommand, "Chat command should NOT be registered (deprecated in favor of 'copilot' command)");
    }

    /// <summary>
    /// Tests that RegisterCommands handles null root command gracefully.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_NullRootCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            copilotCommands!.RegisterCommands(null!));
    }
}
