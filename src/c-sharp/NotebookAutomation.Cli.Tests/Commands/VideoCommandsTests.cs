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
/// Unit tests for VideoCommands.
/// </summary>
[TestClass]
public class VideoCommandsTests
{
    private readonly Mock<ILogger<VideoCommands>> _mockLogger = new();

    /// <summary>
    /// Verifies that the 'video-notes' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task VideoNotesCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        _ = new VideoCommands(_mockLogger.Object);
        VideoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Ensure DI is initialized for handler
        NotebookAutomation.Cli.Program.SetupDependencyInjection(null, false);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("video-notes");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }
    /// <summary>
    /// Tests that the VideoCommands class can be instantiated successfully.
    /// </summary>
    [TestMethod]
    public void VideoCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new VideoCommands(_mockLogger.Object);
        // Act & Assert
        Assert.IsNotNull(command);
    }

    /// <summary>
    /// Tests that the Register method adds the video-notes command and its options to the root command.
    /// </summary>
    [TestMethod]
    public void Register_AddsVideoNotesCommandToRoot()
    {
        // Arrange
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var videoCommands = new VideoCommands(_mockLogger.Object);

        // Act
        VideoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Assert
        var videoNotesCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "video-notes");
        Assert.IsNotNull(videoNotesCommand, "video-notes command should be registered on the root command.");

        // Check options
        var optionNames = videoNotesCommand.Options.SelectMany(o => o.Aliases).ToList();
        CollectionAssert.Contains(optionNames, "--input", "video-notes should have '--input' option");
        CollectionAssert.Contains(optionNames, "-i", "video-notes should have '-i' option");
    }
}
