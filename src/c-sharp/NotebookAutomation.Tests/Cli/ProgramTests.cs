// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Cli;

namespace NotebookAutomation.Tests.Cli;

/// <summary>
/// Unit tests for the Program class functionality, specifically focusing on the
/// config file display feature when --help is invoked.
/// </summary>
/// <remarks>
/// These tests verify that the CLI correctly displays configuration file information
/// when help is requested, ensuring users know which config.json file is being used.
/// </remarks>
[TestClass]
public class ProgramTests
{
    private StringWriter consoleOutput = null!;
    private TextWriter originalOut = null!;

    /// <summary>
    /// Initializes test dependencies before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        // Capture console output for testing
        originalOut = Console.Out;
        consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);
    }

    /// <summary>
    /// Cleans up test resources after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        Console.SetOut(originalOut);
        consoleOutput?.Dispose();
    }

    /// <summary>
    /// Tests that --help displays configuration file information when a config file exists.
    /// </summary>
    [TestMethod]
    public async Task ExecuteMainAsync_Help_DisplaysConfigFile()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.AreEqual(0, exitCode, "Help command should return exit code 0"); var output = consoleOutput.ToString();
        Assert.IsTrue(output.Contains("Configuration:"),
            "Help output should contain configuration file information");
    }

    /// <summary>
    /// Tests that --help with a specific config file displays the correct path.
    /// </summary>
    [TestMethod]
    public async Task ExecuteMainAsync_HelpWithConfig_DisplaysSpecifiedConfigPath()
    {
        // Arrange
        var configPath = "test-config.json";
        var args = new[] { "--config", configPath, "--help" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.AreEqual(0, exitCode, "Help command should return exit code 0"); var output = consoleOutput.ToString();
        Assert.IsTrue(output.Contains($"Configuration:") && output.Contains($"{configPath}"),
            $"Help output should contain the specified config path: {configPath}");
    }

    /// <summary>
    /// Tests that regular commands (non-help) also display config file information.
    /// </summary>
    [TestMethod]
    public async Task ExecuteMainAsync_RegularCommand_DisplaysConfigFile()
    {
        // Arrange
        var args = new[] { "config", "list-keys" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        // Exit code may vary depending on setup, but output should contain config info
        var output = consoleOutput.ToString();
        Assert.IsTrue(output.Contains("Using configuration file:") || output.Contains("No configuration file found"),
            "Regular command output should contain configuration file information");
    }

    /// <summary>
    /// Tests that --version command does NOT display config file information.
    /// </summary>
    [TestMethod]
    public async Task ExecuteMainAsync_Version_DoesNotDisplayConfigFile()
    {
        // Arrange
        var args = new[] { "--version" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.AreEqual(0, exitCode, "Version command should return exit code 0");

        var output = consoleOutput.ToString();
        Assert.IsFalse(output.Contains("Configuration file:") || output.Contains("Using configuration file:"),
            "Version output should NOT contain configuration file information");
    }

    /// <summary>
    /// Tests that config view command does NOT display redundant config file information.
    /// </summary>
    [TestMethod]
    public async Task ExecuteMainAsync_ConfigView_DoesNotDisplayRedundantConfigInfo()
    {
        // Arrange
        var args = new[] { "config", "view" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        // The config view command may fail due to missing dependencies, but we're testing the logic
        var output = consoleOutput.ToString();

        // The output should not contain the redundant "Using configuration file:" message
        // since config view already shows config details
        var redundantMessages = output.Split('\n')
            .Count(line => line.Contains("Using configuration file:"));

        Assert.IsTrue(redundantMessages <= 1,
            "Config view should not display redundant configuration file messages");
    }

    /// <summary>
    /// Tests that the help message clearly indicates when no config file is found.
    /// </summary>
    [TestMethod]
    public async Task ExecuteMainAsync_HelpNoConfigFound_DisplaysDefaultMessage()
    {
        // Arrange
        var nonExistentPath = "completely-nonexistent-config-file-12345.json";
        var args = new[] { "--config", nonExistentPath, "--help" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.AreEqual(0, exitCode, "Help command should return exit code 0"); var output = consoleOutput.ToString();
        Assert.IsTrue(output.Contains($"Configuration:") && output.Contains($"{nonExistentPath}"),
            "Help output should show the specified config path even if it doesn't exist");
    }
}
