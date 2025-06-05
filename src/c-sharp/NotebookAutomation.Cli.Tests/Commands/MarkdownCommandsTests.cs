using System;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Cli.Commands;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Cli.Tests.Commands;
/// <summary>
/// Unit tests for MarkdownCommands.
/// </summary>
[TestClass]
public class MarkdownCommandsTests
{
    private readonly Mock<ILogger<MarkdownCommands>> _mockLogger = new();
    private readonly Mock<AppConfig> _mockAppConfig = new();
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();

    /// <summary>
    /// Verifies that the 'generate-markdown' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task GenerateMarkdownCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var markdownCommands = new MarkdownCommands(_mockLogger.Object, _mockAppConfig.Object, _mockServiceProvider.Object);
        markdownCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Ensure DI is initialized for handler
        NotebookAutomation.Cli.Program.SetupDependencyInjection(null, false);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new System.CommandLine.Parsing.Parser(rootCommand);
            await parser.InvokeAsync("generate-markdown");
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
        var command = new MarkdownCommands(_mockLogger.Object, _mockAppConfig.Object, _mockServiceProvider.Object);
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
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var markdownCommands = new MarkdownCommands(_mockLogger.Object, _mockAppConfig.Object, _mockServiceProvider.Object);

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

    [TestInitialize]
    public void Setup()
    {
        // Mock the logger extensions for information, error, and debug logging
        // No logger method setups; just pass the mock to the command.
    }
}
