// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Cli.UI;

/// <summary>
/// Unit tests for the <see cref="HelpDisplayService"/> class.
/// </summary>
[TestClass]
public class HelpDisplayServiceTests
{
    private StringWriter? consoleOutput;
    private TextWriter? originalConsoleOut;
    private HelpDisplayService? helpDisplayService;
    private ConfigurationDiscoveryService? configDiscoveryService;
    private EnvironmentDisplayService? environmentDisplayService;


    /// <summary>
    /// Initializes test setup before each test method.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        // Redirect console output to capture it for testing
        consoleOutput = new StringWriter();
        originalConsoleOut = Console.Out;
        Console.SetOut(consoleOutput);

        // Create dependencies
        configDiscoveryService = new ConfigurationDiscoveryService();
        environmentDisplayService = new EnvironmentDisplayService(configDiscoveryService);
        helpDisplayService = new HelpDisplayService(environmentDisplayService);
    }


    /// <summary>
    /// Cleans up after each test method.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        // Restore original console output
        if (originalConsoleOut != null)
        {
            Console.SetOut(originalConsoleOut);
        }

        consoleOutput?.Dispose();
    }


    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when environmentDisplayService is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullEnvironmentDisplayService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new HelpDisplayService(null!));
    }    /// <summary>
         /// Tests that ShowVersionInfo displays version information correctly.
         /// </summary>
    [TestMethod]
    public void ShowVersionInfo_DisplaysVersionInformation()
    {
        // Act
        helpDisplayService!.ShowVersionInfo();

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Notebook Automation"), "Should display application name");
        Assert.IsTrue(output.Contains("Copyright"), "Should display copyright information");
        Assert.IsTrue(output.Contains("Dan Shue"), "Should display author information");
        // The version format should include version number and commit hash
        Assert.IsTrue(output.Contains("version"), "Should display version information");
        Assert.IsTrue(output.Length > 0, "Should produce some output");
    }


    /// <summary>
    /// Tests that DisplayCustomHelpAsync displays help information correctly.
    /// </summary>
    [TestMethod]
    public async Task DisplayCustomHelpAsync_DisplaysHelpInformation()
    {
        // Arrange
        var rootCommand = new RootCommand("Test description");
        rootCommand.AddOption(new Option<string>("--test", "Test option"));
        rootCommand.AddCommand(new Command("testcommand", "Test command"));
        var args = new[] { "--help" };

        // Act
        await helpDisplayService!.DisplayCustomHelpAsync(rootCommand, null, false, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Description:"), "Should display description section");
        Assert.IsTrue(output.Contains("Usage:"), "Should display usage section");
        Assert.IsTrue(output.Contains("Current Environment:"), "Should display environment section");
        Assert.IsTrue(output.Contains("Options:"), "Should display options section");
        Assert.IsTrue(output.Contains("Commands:"), "Should display commands section");
        Assert.IsTrue(output.Contains("Test description"), "Should display root command description");
        Assert.IsTrue(output.Contains("--test"), "Should display options");
        Assert.IsTrue(output.Contains("testcommand"), "Should display commands");
    }


    /// <summary>
    /// Tests that DisplayCustomHelpAsync includes configuration path when provided.
    /// </summary>
    [TestMethod]
    public async Task DisplayCustomHelpAsync_WithConfigPath_IncludesConfigPath()
    {
        // Arrange
        var rootCommand = new RootCommand("Test description");
        var configPath = "test-config.json";
        var args = new[] { "--config", configPath, "--help" };

        // Act
        await helpDisplayService!.DisplayCustomHelpAsync(rootCommand, configPath, false, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Configuration:"), "Should display configuration information");
    }


    /// <summary>
    /// Tests that DisplayCustomHelpAsync handles debug mode correctly.
    /// </summary>
    [TestMethod]
    public async Task DisplayCustomHelpAsync_WithDebugMode_ShowsDebugInformation()
    {
        // Arrange
        var rootCommand = new RootCommand("Test description");
        var args = new[] { "--debug", "--help" };

        // Act
        await helpDisplayService!.DisplayCustomHelpAsync(rootCommand, null, true, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Debug Mode:"), "Should display debug mode information");
    }
}
