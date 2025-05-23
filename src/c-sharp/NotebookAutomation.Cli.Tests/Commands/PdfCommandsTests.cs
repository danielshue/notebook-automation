using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for PdfCommands.
    /// </summary>
    [TestClass]
    public class PdfCommandsTests
    {
        [TestMethod]
        public void PdfCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new PdfCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        [TestMethod]
        public void Register_AddsPdfNotesCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var pdfCommands = new PdfCommands();

            // Act
            pdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var pdfNotesCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "pdf-notes");
            Assert.IsNotNull(pdfNotesCommand, "pdf-notes command should be registered on the root command.");
        }
    }
}
