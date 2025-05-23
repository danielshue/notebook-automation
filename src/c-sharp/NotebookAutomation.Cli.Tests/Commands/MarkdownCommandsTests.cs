using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for MarkdownCommands.
    /// </summary>
    [TestClass]
    public class MarkdownCommandsTests
    {

        /// <summary>
        /// Tests that the MarkdownCommands class can be instantiated successfully.
        /// </summary>
        [TestMethod]
        public void MarkdownCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new MarkdownCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        /// <summary>
        /// Tests that the Register method adds the generate-markdown command to the root command.
        /// </summary>
        [TestMethod]
        public void Register_AddsGenerateMarkdownCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var markdownCommands = new MarkdownCommands();

            // Act
            markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var markdownCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "generate-markdown");
            Assert.IsNotNull(markdownCommand, "generate-markdown command should be registered on the root command.");
        }
    }
}
