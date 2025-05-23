using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for VaultCommands.
    /// </summary>
    [TestClass]
    public class VaultCommandsTests
    {
        [TestMethod]
        public void VaultCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new VaultCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        [TestMethod]
        public void Register_AddsVaultCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var vaultCommands = new VaultCommands();

            // Act
            vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var vaultCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "vault");
            Assert.IsNotNull(vaultCommand, "vault command should be registered on the root command.");
        }
    }
}
