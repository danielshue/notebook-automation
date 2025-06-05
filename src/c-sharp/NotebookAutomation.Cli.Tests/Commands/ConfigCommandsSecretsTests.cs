using System.CommandLine;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Cli.Commands;

namespace NotebookAutomation.Cli.Tests.Commands;
/// <summary>
/// Additional tests for ConfigCommands: secrets and display-secrets.
/// </summary>
[TestClass]
public class ConfigCommandsSecretsTests
{
    [TestMethod]
    public void DisplaySecretsCommand_PrintsStatus()
    {
        // Arrange
        var configCommands = new ConfigCommands();
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        ConfigCommands.Register(rootCommand, configOption, debugOption);
        var displaySecrets = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config")
            ?.Subcommands.FirstOrDefault(c => c.Name == "display-secrets") as Command;
        Assert.IsNotNull(displaySecrets, "display-secrets command should be registered");
        // We cannot fully test the output without a DI container, but we can check registration.
    }

    [TestMethod]
    public void SecretsCommand_PrintsStatus()
    {
        // Arrange
        var configCommands = new ConfigCommands();
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        ConfigCommands.Register(rootCommand, configOption, debugOption);
        var secrets = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config")
            ?.Subcommands.FirstOrDefault(c => c.Name == "secrets") as Command;
        Assert.IsNotNull(secrets, "secrets command should be registered");
        // We cannot fully test the output without a DI container, but we can check registration.
    }
}
