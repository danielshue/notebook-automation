// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Cli.Cli;

/// <summary>
/// Unit tests for the <see cref="CommandLineBuilder"/> class.
/// </summary>
[TestClass]
public class CommandLineBuilderTests
{
    private CommandLineBuilder? commandLineBuilder;
    private IServiceProvider? serviceProvider;


    /// <summary>
    /// Initializes test setup before each test method.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        commandLineBuilder = new CommandLineBuilder();

        // Create a minimal service provider for testing
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        serviceProvider = services.BuildServiceProvider();
    }


    /// <summary>
    /// Cleans up after each test method.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        (serviceProvider as IDisposable)?.Dispose();
    }


    /// <summary>
    /// Tests that CreateRootCommand returns a valid root command.
    /// </summary>
    [TestMethod]
    public void CreateRootCommand_ReturnsValidRootCommand()
    {
        // Act
        var rootCommand = commandLineBuilder!.CreateRootCommand();

        // Assert
        Assert.IsNotNull(rootCommand, "Should return a valid root command");
        Assert.IsInstanceOfType(rootCommand, typeof(RootCommand), "Should return a RootCommand instance");
        Assert.IsTrue(rootCommand.Description?.Length > 0, "Root command should have a description");
    }


    /// <summary>
    /// Tests that CreateGlobalOptions returns valid command line options.
    /// </summary>
    [TestMethod]
    public void CreateGlobalOptions_ReturnsValidOptions()
    {
        // Act
        var options = commandLineBuilder!.CreateGlobalOptions();

        // Assert
        Assert.IsNotNull(options, "Should return valid options");
        Assert.IsInstanceOfType(options, typeof(CommandLineOptions), "Should return CommandLineOptions instance");
    }

    /// <summary>
    /// Tests that RegisterCommands adds global options to the root command.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_AddsGlobalOptionsToRootCommand()
    {
        // Arrange
        var rootCommand = commandLineBuilder!.CreateRootCommand();
        var options = commandLineBuilder!.CreateGlobalOptions();        // Create a complete service provider that includes all required services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Add minimal AppConfig to satisfy TagCommands dependency
        var appConfig = new NotebookAutomation.Core.Configuration.AppConfig();
        appConfig.Paths.NotebookVaultFullpathRoot = "test-vault";
        appConfig.Paths.OnedriveFullpathRoot = "test-onedrive";
        services.AddSingleton(appConfig);

        // Add IConfigManager service
        services.AddSingleton<NotebookAutomation.Core.Configuration.IConfigManager>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<NotebookAutomation.Core.Configuration.ConfigManager>();
            return new NotebookAutomation.Core.Configuration.ConfigManager(
                new NotebookAutomation.Core.Configuration.FileSystemWrapper(),
                new NotebookAutomation.Core.Configuration.EnvironmentWrapper(),
                logger);
        });

        var completeServiceProvider = services.BuildServiceProvider();

        // Act
        commandLineBuilder!.RegisterCommands(rootCommand, options, completeServiceProvider);

        // Assert
        Assert.IsTrue(rootCommand.Options.Any(o => o.Name == "config" || o.Aliases.Contains("--config")),
            "Should include config option after registration");
        Assert.IsTrue(rootCommand.Options.Any(o => o.Name == "debug" || o.Aliases.Contains("--debug")),
            "Should include debug option after registration");
        Assert.IsTrue(rootCommand.Options.Any(o => o.Name == "verbose" || o.Aliases.Contains("--verbose")),
            "Should include verbose option after registration");
        Assert.IsTrue(rootCommand.Options.Any(o => o.Name == "dry-run" || o.Aliases.Contains("--dry-run")),
            "Should include dry-run option after registration");

        // Cleanup
        completeServiceProvider.Dispose();
    }

    /// <summary>
    /// Tests that RegisterCommands adds commands to the root command.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_AddsCommandsToRootCommand()
    {
        // Arrange
        var rootCommand = commandLineBuilder!.CreateRootCommand();
        var options = commandLineBuilder!.CreateGlobalOptions();
        var initialCommandCount = rootCommand.Subcommands.Count;        // Create a complete service provider that includes all required services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Add minimal AppConfig to satisfy TagCommands dependency
        var appConfig = new NotebookAutomation.Core.Configuration.AppConfig();
        appConfig.Paths.NotebookVaultFullpathRoot = "test-vault";
        appConfig.Paths.OnedriveFullpathRoot = "test-onedrive";
        services.AddSingleton(appConfig);

        // Add IConfigManager service
        services.AddSingleton<NotebookAutomation.Core.Configuration.IConfigManager>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<NotebookAutomation.Core.Configuration.ConfigManager>();
            return new NotebookAutomation.Core.Configuration.ConfigManager(
                new NotebookAutomation.Core.Configuration.FileSystemWrapper(),
                new NotebookAutomation.Core.Configuration.EnvironmentWrapper(),
                logger);
        });

        var completeServiceProvider = services.BuildServiceProvider();

        // Act
        commandLineBuilder!.RegisterCommands(rootCommand, options, completeServiceProvider);

        // Assert
        Assert.IsTrue(rootCommand.Subcommands.Count > initialCommandCount,
            "Should add commands to the root command");

        // Cleanup
        completeServiceProvider.Dispose();
    }

    /// <summary>
    /// Tests that RegisterCommands handles null parameters gracefully.
    /// </summary>
    [TestMethod]
    public void RegisterCommands_NullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var rootCommand = commandLineBuilder!.CreateRootCommand();
        var options = commandLineBuilder!.CreateGlobalOptions();

        // Create a minimal service provider for the non-null tests
        var services = new ServiceCollection();
        services.AddLogging();
        var testServiceProvider = services.BuildServiceProvider();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            commandLineBuilder!.RegisterCommands(null!, options, testServiceProvider));
        Assert.ThrowsException<ArgumentNullException>(() =>
            commandLineBuilder!.RegisterCommands(rootCommand, null!, testServiceProvider));
        Assert.ThrowsException<ArgumentNullException>(() =>
            commandLineBuilder!.RegisterCommands(rootCommand, options, null!));

        // Cleanup
        testServiceProvider.Dispose();
    }


    /// <summary>
    /// Tests that BuildParser returns a valid command line parser.
    /// </summary>
    [TestMethod]
    public void BuildParser_ReturnsValidParser()
    {
        // Arrange
        var rootCommand = commandLineBuilder!.CreateRootCommand();

        // Act
        var parser = commandLineBuilder!.BuildParser(rootCommand, false);

        // Assert
        Assert.IsNotNull(parser, "Should return a valid parser");
        Assert.IsInstanceOfType(parser, typeof(System.CommandLine.Parsing.Parser), "Should return a Parser instance");
    }


    /// <summary>
    /// Tests that BuildParser works with debug mode enabled.
    /// </summary>
    [TestMethod]
    public void BuildParser_DebugModeEnabled_WorksCorrectly()
    {
        // Arrange
        var rootCommand = commandLineBuilder!.CreateRootCommand();

        // Act
        var parser = commandLineBuilder!.BuildParser(rootCommand, true);

        // Assert
        Assert.IsNotNull(parser, "Should return a valid parser in debug mode");
    }


    /// <summary>
    /// Tests that BuildParser handles null root command parameter.
    /// </summary>
    [TestMethod]
    public void BuildParser_NullRootCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            commandLineBuilder!.BuildParser(null!, false));
    }


    /// <summary>
    /// Tests that the same CommandLineBuilder instance can be used multiple times.
    /// </summary>
    [TestMethod]
    public void CommandLineBuilder_MultipleUsage_WorksCorrectly()
    {
        // Act
        var rootCommand1 = commandLineBuilder!.CreateRootCommand();
        var rootCommand2 = commandLineBuilder!.CreateRootCommand();
        var options1 = commandLineBuilder!.CreateGlobalOptions();
        var options2 = commandLineBuilder!.CreateGlobalOptions();

        // Assert
        Assert.IsNotNull(rootCommand1, "First root command should be valid");
        Assert.IsNotNull(rootCommand2, "Second root command should be valid");
        Assert.IsNotNull(options1, "First options should be valid");
        Assert.IsNotNull(options2, "Second options should be valid");

        // Commands should be independent instances
        Assert.AreNotSame(rootCommand1, rootCommand2, "Root commands should be independent instances");
        Assert.AreNotSame(options1, options2, "Options should be independent instances");
    }
}
