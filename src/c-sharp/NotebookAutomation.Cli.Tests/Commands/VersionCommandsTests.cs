using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Cli.Commands;

namespace NotebookAutomation.Cli.Tests.Commands;
/// <summary>
/// Unit tests for VersionCommands.
/// </summary>
[TestClass]
public class VersionCommandsTests
{
    [TestMethod]
    public void VersionCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new VersionCommands();
        // Act & Assert
        Assert.IsNotNull(command);
    }

    [TestMethod]
    public void Register_AddsVersionCommandToRoot()
    {
        // Arrange
        var rootCommand = new System.CommandLine.RootCommand();
        var versionCommands = new VersionCommands();

        // Act
        VersionCommands.Register(rootCommand);

        // Assert
        var versionCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "version");
        Assert.IsNotNull(versionCommand, "version command should be registered on the root command.");
    }
}
