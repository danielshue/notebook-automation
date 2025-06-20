// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Unit tests for VideoCommands.
/// </summary>
[TestClass]
public class VideoCommandsTests
{
    private readonly Mock<ILogger<VideoCommands>> mockLogger = new object();

    /// <summary>
    /// Verifies that the 'video-notes' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task VideoNotesCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose"); var dryRunOption = new Option<bool>("--dry-run");
        var videoCommands = new VideoCommands(mockLogger.Object);
        videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Ensure DI is initialized for handler
        Program.SetupDependencyInjection(null, false);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("video-notes").ConfigureAwait(false);
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
        var command = new VideoCommands(mockLogger.Object);

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
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var videoCommands = new VideoCommands(mockLogger.Object);        // Act
        videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Assert
        var videoNotesCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "video-notes");
        Assert.IsNotNull(videoNotesCommand, "video-notes command should be registered on the root command.");

        // Check options
        var optionNames = videoNotesCommand.Options.SelectMany(o => o.Aliases).ToList();
        CollectionAssert.Contains(optionNames, "--input", "video-notes should have '--input' option");
        CollectionAssert.Contains(optionNames, "-i", "video-notes should have '-i' option");
    }
}
