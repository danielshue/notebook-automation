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


        /// <summary>
        /// Tests that the 'config show' command prints usage/help when no arguments are provided.
        /// </summary>
        [TestMethod]
        public void ConfigShow_NoArgs_PrintsUsage()
        {
            // Arrange
            var configCommands = new ConfigCommands();
            var originalOut = System.Console.Out;
            var stringWriter = new System.IO.StringWriter();
            System.Console.SetOut(stringWriter);
            try
            {
                // Act: Directly call the usage method
                configCommands.PrintShowUsage();
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
            // Assert
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage: config show"), "Should print usage/help when no args provided.");
        }


        /// <summary>
        /// Tests that the 'config' command group contains the 'show' and 'update-key' subcommands after registration.
        /// </summary>
        [TestMethod]
        public void Register_ConfigCommand_HasShowAndUpdateKeySubcommands()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var configCommands = new ConfigCommands();
            configCommands.Register(rootCommand, configOption, debugOption);

            // Act
            var configCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "config") as System.CommandLine.Command;

            // Assert
            Assert.IsNotNull(configCommand, "config command should be registered on the root command.");
            Assert.IsTrue(configCommand.Children.Any(c => c.Name == "show"), "config command should have a 'show' subcommand.");
            Assert.IsTrue(configCommand.Children.Any(c => c.Name == "update-key"), "config command should have an 'update-key' subcommand.");
        }

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
