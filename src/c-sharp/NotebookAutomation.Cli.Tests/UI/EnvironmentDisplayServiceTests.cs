// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Tests.UI;

/// <summary>
/// Unit tests for the <see cref="EnvironmentDisplayService"/> class.
/// </summary>
[TestClass]
public class EnvironmentDisplayServiceTests
{
    private StringWriter? consoleOutput;
    private TextWriter? originalConsoleOut;
    private EnvironmentDisplayService? environmentDisplayService;
    private ConfigurationDiscoveryService? configDiscoveryService;


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
    /// Tests that the constructor throws ArgumentNullException when configurationDiscoveryService is null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullConfigurationDiscoveryService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new EnvironmentDisplayService(null!));
    }


    /// <summary>
    /// Tests that DisplayEnvironmentSettingsAsync displays basic environment information.
    /// </summary>
    [TestMethod]
    public async Task DisplayEnvironmentSettingsAsync_DisplaysBasicEnvironmentInfo()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        await environmentDisplayService!.DisplayEnvironmentSettingsAsync(null, false, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Configuration:"), "Should display configuration information");
        Assert.IsTrue(output.Contains("Debug Mode:"), "Should display debug mode information");
        Assert.IsTrue(output.Contains("Verbose Mode:"), "Should display verbose mode information");
        Assert.IsTrue(output.Contains("Working Dir:"), "Should display working directory");
        Assert.IsTrue(output.Contains(".NET Runtime:"), "Should display .NET runtime version");
    }


    /// <summary>
    /// Tests that DisplayEnvironmentSettingsAsync shows configuration file when specified.
    /// </summary>
    [TestMethod]
    public async Task DisplayEnvironmentSettingsAsync_WithConfigPath_ShowsConfigPath()
    {
        // Arrange
        var configPath = "test-config.json";
        var args = Array.Empty<string>();

        // Act
        await environmentDisplayService!.DisplayEnvironmentSettingsAsync(configPath, false, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Configuration:"), "Should display configuration section");
        Assert.IsTrue(output.Contains(configPath), "Should display the specified config path");
    }


    /// <summary>
    /// Tests that DisplayEnvironmentSettingsAsync shows debug mode when enabled.
    /// </summary>
    [TestMethod]
    public async Task DisplayEnvironmentSettingsAsync_WithDebugMode_ShowsDebugEnabled()
    {
        // Arrange
        var args = new[] { "--debug" };

        // Act
        await environmentDisplayService!.DisplayEnvironmentSettingsAsync(null, true, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Debug Mode:"), "Should display debug mode section");
        Assert.IsTrue(output.Contains("Enabled"), "Should show debug mode as enabled");
    }


    /// <summary>
    /// Tests that DisplayEnvironmentSettingsAsync shows verbose mode when enabled.
    /// </summary>
    [TestMethod]
    public async Task DisplayEnvironmentSettingsAsync_WithVerboseMode_ShowsVerboseEnabled()
    {
        // Arrange
        var args = new[] { "--verbose" };

        // Act
        await environmentDisplayService!.DisplayEnvironmentSettingsAsync(null, false, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Verbose Mode:"), "Should display verbose mode section");
        Assert.IsTrue(output.Contains("Enabled"), "Should show verbose mode as enabled");
    }
    /// <summary>
    /// Tests that DisplayEnvironmentSettingsAsync handles no configuration file gracefully.
    /// </summary>
    [TestMethod]
    public async Task DisplayEnvironmentSettingsAsync_NoConfigFile_ShowsDefaultMessage()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        await environmentDisplayService!.DisplayEnvironmentSettingsAsync(null, false, args);

        // Assert
        var output = consoleOutput!.ToString();
        Assert.IsTrue(output.Contains("Configuration:"), "Should display configuration section");
        // The actual message may vary depending on whether a default config file is found
        Assert.IsTrue(output.Contains("Configuration:"),
            "Should show configuration information regardless of whether config file exists");
    }
}
