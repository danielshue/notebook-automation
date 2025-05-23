using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for OneDriveCommands.
    /// </summary>
    [TestClass]
    public class OneDriveCommandsTests
    {
        [TestMethod]
        public void OneDriveCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new OneDriveCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        [TestMethod]
        public void Register_AddsOneDriveCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var oneDriveCommands = new OneDriveCommands();

            // Act
            oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var oneDriveCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "onedrive");
            Assert.IsNotNull(oneDriveCommand, "onedrive command should be registered on the root command.");
        }
    }
}
