using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine.Parsing;
using Moq;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for PdfCommands.
    /// </summary>
    [TestClass]
    public class PdfCommandsTests
    {
        private Mock<ILogger<PdfCommands>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<PdfCommands>>();
        }

        /// <summary>
        /// Verifies that the 'pdf-notes' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task PdfNotesCommand_PrintsUsage_WhenNoArgs()

        {
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            _ = new PdfCommands(_mockLogger.Object);
            PdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            var originalOut = Console.Out;
            var stringWriter = new System.IO.StringWriter();
            Console.SetOut(stringWriter);
            try
            {
                var parser = new System.CommandLine.Parsing.Parser(rootCommand);
                await parser.InvokeAsync("pdf-notes");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }

        /// <summary>
        /// Tests that the PdfCommands class can be instantiated successfully.
        /// </summary>
        [TestMethod]
        public void PdfCommand_Initialization_ShouldSucceed()

        {
            // Arrange
            var command = new PdfCommands(_mockLogger.Object);

            // Act & Assert
            Assert.IsNotNull(command);
        }

        /// <summary>
        /// Tests that the Register method adds the pdf-notes command and its options to the root command.
        /// </summary>
        [TestMethod]
        public void Register_AddsPdfNotesCommandToRoot()

        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var pdfCommands = new PdfCommands(_mockLogger.Object);            // Act
            PdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var pdfNotesCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "pdf-notes");
            Assert.IsNotNull(pdfNotesCommand, "pdf-notes command should be registered on the root command.");            // Check options
            var optionNames = pdfNotesCommand.Options.SelectMany(o => o.Aliases).ToList();
            CollectionAssert.Contains(optionNames, "--input", "pdf-notes should have '--input' option");
            CollectionAssert.Contains(optionNames, "-i", "pdf-notes should have '-i' option");
            CollectionAssert.Contains(optionNames, "--overwrite-output-dir", "pdf-notes should have '--overwrite-output-dir' option");
            CollectionAssert.Contains(optionNames, "-o", "pdf-notes should have '-o' option");
        }
    }
}
