// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Unit tests for PdfCommands.
/// </summary>
[TestClass]
public class PdfCommandsTests
{
    private Mock<ILogger<PdfCommands>> mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<PdfCommands>>();
    }

    /// <summary>
    /// Verifies that the 'pdf-notes' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task PdfNotesCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose"); var dryRunOption = new Option<bool>("--dry-run");
        var pdfCommands = new PdfCommands(mockLogger.Object);
        pdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("pdf-notes").ConfigureAwait(false);
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
        var command = new PdfCommands(mockLogger.Object);

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
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run"); var pdfCommands = new PdfCommands(mockLogger.Object);            // Act
        pdfCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

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
