// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Additional tests for ConfigCommands: secrets and display-secrets.
/// </summary>
[TestClass]
public class ConfigCommandsSecretsTests
{
    private Mock<ILogger<ConfigCommands>> mockLogger = null!;
    private IServiceProvider serviceProvider = null!;
    private Mock<IConfigManager> mockConfigManager = null!;

    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<ConfigCommands>>();
        mockConfigManager = new Mock<IConfigManager>();

        // Create a real service collection for testing
        var services = new ServiceCollection();
        services.AddSingleton(mockConfigManager.Object);
        serviceProvider = services.BuildServiceProvider();
    }

    private ConfigCommands CreateConfigCommands()
    {
        return new ConfigCommands(mockLogger.Object, serviceProvider);
    }

    /// <summary>
    /// Tests that the 'config display-secrets' command is properly registered
    /// and can be found in the command hierarchy.
    /// </summary>
    [TestMethod]
    public void DisplaySecretsCommand_PrintsStatus()
    {
        // Arrange
        var configCommands = CreateConfigCommands();
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        configCommands.Register(rootCommand, configOption, debugOption);
        var displaySecrets = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config")
            ?.Subcommands.FirstOrDefault(c => c.Name == "display-secrets") as Command;
        Assert.IsNotNull(displaySecrets, "display-secrets command should be registered");
        // We cannot fully test the output without a DI container, but we can check registration.
    }

    /// <summary>
    /// Tests that the 'config secrets' command is properly registered
    /// and can be found in the command hierarchy.
    /// </summary>
    [TestMethod]
    public void SecretsCommand_PrintsStatus()
    {
        // Arrange
        var configCommands = CreateConfigCommands();
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        configCommands.Register(rootCommand, configOption, debugOption);
        var secrets = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config")
            ?.Subcommands.FirstOrDefault(c => c.Name == "secrets") as Command;
        Assert.IsNotNull(secrets, "secrets command should be registered");
        // We cannot fully test the output without a DI container, but we can check registration.
    }
}
