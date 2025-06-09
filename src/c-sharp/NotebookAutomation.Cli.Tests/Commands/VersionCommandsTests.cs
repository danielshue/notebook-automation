// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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
        var rootCommand = new RootCommand();
        var versionCommands = new VersionCommands();

        // Act
        VersionCommands.Register(rootCommand);

        // Assert
        var versionCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "version");
        Assert.IsNotNull(versionCommand, "version command should be registered on the root command.");
    }
}