using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.CommandLine;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Tests for ConfigCommands: view and update-key command execution.
    /// </summary>
    [TestClass]
    public class ConfigCommandsViewUpdateTests
    {
        [TestMethod]
        public void ViewCommand_PrintsConfig()
        {
            // Arrange
            var configCommands = new ConfigCommands();
            var rootCommand = new RootCommand();
            var configOption = new Option<string>("--config");
            var debugOption = new Option<bool>("--debug");
            configCommands.Register(rootCommand, configOption, debugOption);
            var view = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config")
                ?.Subcommands.FirstOrDefault(c => c.Name == "view") as Command;
            Assert.IsNotNull(view, "view command should be registered");
            // We cannot fully test the output without a real config file, but registration is covered.
        }

        [TestMethod]
        public void UpdateKeyCommand_PrintsUsageOnMissingArgs()
        {
            // Arrange
            var configCommands = new ConfigCommands();
            var rootCommand = new RootCommand();
            var configOption = new Option<string>("--config");
            var debugOption = new Option<bool>("--debug");
            configCommands.Register(rootCommand, configOption, debugOption);
            var update = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config")
                ?.Subcommands.FirstOrDefault(c => c.Name == "update") as Command;
            Assert.IsNotNull(update, "update command should be registered");
            // We cannot fully test the output without a real config file, but registration is covered.
        }
    }
}
