// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Cli;

namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Unit tests for MarkdownCommands.
/// </summary>
[TestClass]
public class MarkdownCommandsTests
{
    private readonly Mock<ILogger<MarkdownCommands>> mockLogger = new Mock<ILogger<MarkdownCommands>>();
    private readonly Mock<AppConfig> mockAppConfig = new Mock<AppConfig>();
    private readonly Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();

    /// <summary>
    /// Verifies that the 'generate-markdown' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task GenerateMarkdownCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var markdownCommands = new MarkdownCommands(mockLogger.Object, mockAppConfig.Object, mockServiceProvider.Object);
        markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Ensure DI is initialized for handler
        Program.SetupDependencyInjection(null, false);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("generate-markdown").ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }

    /// <summary>
    /// Tests that the MarkdownCommands class can be instantiated successfully.
    /// </summary>
    [TestMethod]
    public void MarkdownCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new MarkdownCommands(mockLogger.Object, mockAppConfig.Object, mockServiceProvider.Object);

        // Act & Assert
        Assert.IsNotNull(command);
    }

    /// <summary>
    /// Tests that the Register method adds the generate-markdown command and its options to the root command.
    /// </summary>
    [TestMethod]
    public void Register_AddsGenerateMarkdownCommandToRoot()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var markdownCommands = new MarkdownCommands(mockLogger.Object, mockAppConfig.Object, mockServiceProvider.Object);

        // Act
        markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Assert
        var markdownCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "generate-markdown");
        Assert.IsNotNull(markdownCommand, "generate-markdown command should be registered on the root command.");

        // Check options
        var optionNames = markdownCommand.Options.SelectMany(o => o.Aliases).ToList();
        CollectionAssert.Contains(optionNames, "--src-dirs", "generate-markdown should have '--src-dirs' option");
        CollectionAssert.Contains(optionNames, "-s", "generate-markdown should have '-s' option");
        CollectionAssert.Contains(optionNames, "--dest-dir", "generate-markdown should have '--dest-dir' option");
        CollectionAssert.Contains(optionNames, "-d", "generate-markdown should have '-d' option");
    }

    /// <summary>
    /// Initializes the test environment before each test method runs.
    /// Sets up mock objects and prepares the testing context.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
    }
}
