using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for TagCommands.
    /// </summary>
    [TestClass]
    public class TagCommandsTests
    {
        [TestMethod]
        public void TagCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new TagCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        [TestMethod]
        public void Register_AddsTagCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var tagCommands = new TagCommands();

            // Act
            tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var tagCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "tag");
            Assert.IsNotNull(tagCommand, "tag command should be registered on the root command.");
        }
    }
}
