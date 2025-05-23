using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for ConfigCommands.
    /// </summary>
    [TestClass]
    public class ConfigCommandsTests
    {
        [TestMethod]
        public void ConfigCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new ConfigCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        [TestMethod]
        public void Register_AddsConfigCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var configCommands = new ConfigCommands();

            // Act
            configCommands.Register(rootCommand, configOption, debugOption);

            // Assert
            var configCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "config");
            Assert.IsNotNull(configCommand, "config command should be registered on the root command.");
        }
    }
}
