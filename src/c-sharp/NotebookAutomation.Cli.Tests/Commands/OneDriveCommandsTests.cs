// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Unit tests for OneDriveCommands.
/// </summary>
[TestClass]
public class OneDriveCommandsTests
{
    private Mock<ILogger<OneDriveCommands>> mockLogger = null!;

    /// <summary>
    /// Initializes the test environment before each test method runs.
    /// Sets up mock objects for the OneDrive commands logger.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<OneDriveCommands>>();
    }

    /// <summary>
    /// Verifies that the 'onedrive download' command prints usage/help when required arguments are missing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task OneDriveDownloadCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);

            // Missing both required args
            await parser.InvokeAsync("onedrive download").ConfigureAwait(false);
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
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task OneDriveUploadCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);

            // Missing both required args
            await parser.InvokeAsync("onedrive upload").ConfigureAwait(false);
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
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task OneDriveSearchCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);

            // Missing required arg
            await parser.InvokeAsync("onedrive search").ConfigureAwait(false);
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
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task OneDriveSyncCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);

            // Missing required arg
            await parser.InvokeAsync("onedrive sync").ConfigureAwait(false);
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
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(mockLogger.Object);
        oneDriveCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("onedrive list").ConfigureAwait(false);
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
        var command = new OneDriveCommands(mockLogger.Object);

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
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var oneDriveCommands = new OneDriveCommands(mockLogger.Object);

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