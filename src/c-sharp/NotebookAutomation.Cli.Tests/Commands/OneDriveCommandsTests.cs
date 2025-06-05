using System;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Cli.Commands;

namespace NotebookAutomation.Cli.Tests.Commands;
/// <summary>
/// Unit tests for OneDriveCommands.
/// </summary>
[TestClass]
public class OneDriveCommandsTests
{
    private Mock<ILogger<OneDriveCommands>> _mockLogger;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<OneDriveCommands>>();
    }

    /// <summary>
    /// Verifies that the 'onedrive download' command prints usage/help when required arguments are missing.
    /// </summary>
    [TestMethod]
    public async Task OneDriveDownloadCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(_mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new System.CommandLine.Parsing.Parser(rootCommand);
            // Missing both required args
            await parser.InvokeAsync("onedrive download");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("remote-path") || output.Contains("Usage"), "Should print usage/help when required args are missing.");
    }

    /// <summary>
    /// Verifies that the 'onedrive upload' command prints usage/help when required arguments are missing.
    /// </summary>
    [TestMethod]
    public async Task OneDriveUploadCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(_mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new System.CommandLine.Parsing.Parser(rootCommand);
            // Missing both required args
            await parser.InvokeAsync("onedrive upload");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("local-path") || output.Contains("Usage"), "Should print usage/help when required args are missing.");
    }

    /// <summary>
    /// Verifies that the 'onedrive search' command prints usage/help when required arguments are missing.
    /// </summary>
    [TestMethod]
    public async Task OneDriveSearchCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(_mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new System.CommandLine.Parsing.Parser(rootCommand);
            // Missing required arg
            await parser.InvokeAsync("onedrive search");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("query") || output.Contains("Usage"), "Should print usage/help when required args are missing.");
    }

    /// <summary>
    /// Verifies that the 'onedrive sync' command prints usage/help when required arguments are missing.
    /// </summary>
    [TestMethod]
    public async Task OneDriveSyncCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(_mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new System.CommandLine.Parsing.Parser(rootCommand);
            // Missing required arg
            await parser.InvokeAsync("onedrive sync");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("local-path") || output.Contains("Usage"), "Should print usage/help when required args are missing.");
    }
    /// <summary>
    /// Verifies that the 'onedrive list' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task OneDriveListCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(_mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new System.CommandLine.Parsing.Parser(rootCommand);
            await parser.InvokeAsync("onedrive list");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }
    /// <summary>
    /// Tests that the OneDriveCommands class can be instantiated successfully.
    /// </summary>
    [TestMethod]
    public void OneDriveCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new OneDriveCommands(_mockLogger.Object);
        // Act & Assert
        Assert.IsNotNull(command);
    }

    /// <summary>
    /// Tests that the Register method adds the onedrive command and its subcommands to the root command.
    /// </summary>
    [TestMethod]
    public void Register_AddsOneDriveCommandToRoot()
    {
        // Arrange
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(_mockLogger.Object);

        // Act
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Assert
        var oneDriveCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "onedrive");
        Assert.IsNotNull(oneDriveCommand, "onedrive command should be registered on the root command.");

        // Check subcommands
        var subcommands = oneDriveCommand.Subcommands.Select(c => c.Name).ToList();
        CollectionAssert.Contains(subcommands, "list", "onedrive should have 'list' subcommand");
        CollectionAssert.Contains(subcommands, "download", "onedrive should have 'download' subcommand");
        CollectionAssert.Contains(subcommands, "upload", "onedrive should have 'upload' subcommand");
        CollectionAssert.Contains(subcommands, "search", "onedrive should have 'search' subcommand");
        CollectionAssert.Contains(subcommands, "sync", "onedrive should have 'sync' subcommand");
    }
}
