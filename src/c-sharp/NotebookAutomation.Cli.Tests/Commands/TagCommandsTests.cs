using System;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Cli.Commands;
using NotebookAutomation.Core.Configuration;


namespace NotebookAutomation.Cli.Tests.Commands;
/// <summary>
/// Unit tests for TagCommands.
/// </summary>
[TestClass]
public class TagCommandsTests
{
    private Mock<ILogger<TagCommands>> _mockLogger;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<AppConfig> _mockAppConfig;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<TagCommands>>(); _mockServiceProvider = new Mock<IServiceProvider>();
        _mockAppConfig = new Mock<AppConfig>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig))).Returns(_mockAppConfig.Object);
    }

    private TagCommands CreateTagCommands()
    {
        return new TagCommands(_mockLogger.Object, _mockServiceProvider.Object);
    }

    /// <summary>
    /// Verifies that the 'tag clean-index' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task CleanIndexCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var tagCommands = CreateTagCommands();
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("tag clean-index");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }

    /// <summary>
    /// Verifies that the 'tag consolidate' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task ConsolidateCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var tagCommands = CreateTagCommands();
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("tag consolidate");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }

    /// <summary>
    /// Verifies that the 'tag restructure' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task RestructureCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var tagCommands = CreateTagCommands();
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("tag restructure");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }

    /// <summary>
    /// Verifies that the 'tag add-example' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task AddExampleCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var tagCommands = CreateTagCommands();
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("tag add-example");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }

    /// <summary>
    /// Verifies that the 'tag metadata-check' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task MetadataCheckCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var tagCommands = CreateTagCommands();
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("tag metadata-check");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }

    /// <summary>
    /// Verifies that the 'tag update-frontmatter' command prints usage/help when required arguments are missing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task UpdateFrontmatterCommand_PrintsUsage_WhenArgsMissing()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var tagCommands = CreateTagCommands();
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            // Only provide the subcommand, missing all required arguments
            await parser.InvokeAsync("tag update-frontmatter");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when required args are missing.");
    }

    /// <summary>
    /// Verifies that the 'tag diagnose-yaml' command prints usage/help when no arguments are provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [TestMethod]
    public async Task DiagnoseYamlCommand_PrintsUsage_WhenNoArgs()
    {
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var tagCommands = CreateTagCommands();
        tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("tag diagnose-yaml");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }
}
