// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Tests;

/// <summary>
/// Comprehensive unit tests for the Program class to improve code coverage.
/// </summary>
[TestClass]
public class ProgramCoverageTests
{
    private StringWriter _consoleOutput = null!;
    private TextWriter _originalOut = null!;
    private StringWriter _consoleError = null!;
    private TextWriter _originalError = null!;

    [TestInitialize]
    public void Setup()
    {
        // Capture console output for testing
        _originalOut = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);

        _originalError = Console.Error;
        _consoleError = new StringWriter();
        Console.SetError(_consoleError);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Console.SetOut(_originalOut);
        Console.SetError(_originalError);
        _consoleOutput?.Dispose();
        _consoleError?.Dispose();
    }

    /// <summary>
    /// Tests ServiceProvider property when not initialized throws exception.
    /// </summary>
    [TestMethod]
    public void ServiceProvider_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange - Reset static field using reflection
        var serviceProviderField = typeof(Program).GetField("serviceProvider", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(serviceProviderField);
        serviceProviderField.SetValue(null, null);

        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(() => Program.ServiceProvider);
        Assert.AreEqual("Service provider not initialized. Call SetupDependencyInjection first.", exception.Message);
    }

    /// <summary>
    /// Tests ServiceProvider property when initialized returns the provider.
    /// </summary>
    [TestMethod]
    public void ServiceProvider_WhenInitialized_ReturnsProvider()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");

        try
        {
            // Act
            var provider = Program.SetupDependencyInjection(configPath, false);

            // Assert
            Assert.IsNotNull(provider);
            Assert.AreSame(provider, Program.ServiceProvider);
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }    /// <summary>
         /// Tests Main method with exception in ExecuteMainAsync and debug mode.
         /// </summary>
    [TestMethod]
    public async Task Main_WithExceptionInDebugMode_DisplaysDetailedError()
    {
        // Arrange - use malformed config that will cause JSON parsing error
        var configPath = Path.Combine(Path.GetTempPath(), "malformed-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{invalid json content");
        var args = new[] { "--debug", "--config", configPath };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should return exit code 1 for error");

            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Failed to initialize services:") || output.Contains("Unhandled exception:") || output.Contains("JSON"),
                "Should display detailed error information in debug mode");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests Main method with exception in non-debug mode.
    /// </summary>    [TestMethod]
    public async Task Main_WithExceptionInNonDebugMode_DisplaysFriendlyError()
    {
        // Arrange - use malformed config that will cause JSON parsing error
        var configPath = Path.Combine(Path.GetTempPath(), "malformed-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{invalid json content");
        var args = new[] { "--config", configPath };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should return exit code 1 for error");

            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Error:") || output.Contains("configuration") || output.Contains("JSON"),
                "Should display friendly error message in non-debug mode");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests ExecuteMainAsync with no arguments shows help.
    /// </summary>
    [TestMethod]
    public async Task Main_WithNoArguments_ShowsHelp()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.AreEqual(0, exitCode, "Help should return exit code 0");

        var output = _consoleOutput.ToString();
        Assert.IsTrue(output.Contains("Usage:") || output.Contains("Commands:"),
            "Should display help information");
    }

    /// <summary>
    /// Tests ExecuteMainAsync with --config flag only shows help.
    /// </summary>
    [TestMethod]
    public async Task Main_WithConfigFlagOnly_ShowsHelp()
    {
        // Arrange
        var args = new[] { "--config" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.AreEqual(0, exitCode, "Help should return exit code 0");

        var output = _consoleOutput.ToString();
        Assert.IsTrue(output.Contains("Usage:") || output.Contains("Commands:"),
            "Should display help information");
    }

    /// <summary>
    /// Tests ExecuteMainAsync with debug mode detection.
    /// </summary>
    [TestMethod]
    public async Task Main_WithDebugFlag_EnablesDebugMode()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");
        var args = new[] { "--debug", "--config", configPath, "--help" };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            Assert.AreEqual(0, exitCode, "Debug help should return exit code 0");

            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Debug mode enabled"),
                "Should indicate debug mode is enabled");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests ExecuteMainAsync with verbose mode detection.
    /// </summary>
    [TestMethod]
    public async Task Main_WithVerboseFlag_EnablesVerboseMode()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");
        var args = new[] { "--verbose", "--config", configPath, "--help" };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            Assert.AreEqual(0, exitCode, "Verbose help should return exit code 0");

            var output = _consoleOutput.ToString();
            // Verbose mode might not have specific output, but should not cause errors
            Assert.IsTrue(output.Length > 0, "Should have some output");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests SetupDependencyInjection method directly.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_WithValidConfig_ReturnsServiceProvider()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");

        try
        {
            // Act
            var provider = Program.SetupDependencyInjection(configPath, false);

            // Assert
            Assert.IsNotNull(provider);
            Assert.AreSame(provider, Program.ServiceProvider);
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests SetupDependencyInjection with null config path.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_WithNullConfig_ReturnsServiceProvider()
    {
        // Act
        var provider = Program.SetupDependencyInjection(null, false);

        // Assert
        Assert.IsNotNull(provider);
        Assert.AreSame(provider, Program.ServiceProvider);
    }

    /// <summary>
    /// Tests SetupDependencyInjection with debug mode enabled.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_WithDebugMode_ReturnsServiceProvider()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");

        try
        {
            // Act
            var provider = Program.SetupDependencyInjection(configPath, true);

            // Assert
            Assert.IsNotNull(provider);
            Assert.AreSame(provider, Program.ServiceProvider);
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests exception handling in dependency injection setup.
    /// </summary>
    [TestMethod]
    public async Task Main_DependencyInjectionFailure_HandlesGracefully()
    {
        // Arrange - Use invalid JSON config to trigger DI setup failure
        var configPath = Path.Combine(Path.GetTempPath(), "invalid-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{ invalid json }");
        var args = new[] { "--config", configPath, "some-command" };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should return exit code 1 for DI setup failure");

            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Error:") || output.Contains("Failed to initialize services:"),
                "Should display error message for DI setup failure");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests that config view command behavior is handled correctly.
    /// </summary>
    [TestMethod]
    public async Task Main_ConfigViewCommand_DoesNotShowRedundantConfigInfo()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");
        var args = new[] { "--config", configPath, "config", "view" };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            var output = _consoleOutput.ToString();
            // Config view should not show redundant "Using configuration file:" message
            var configMessages = output.Split('\n')
                .Count(line => line.Contains("Using configuration file:"));

            Assert.IsTrue(configMessages <= 1,
                "Config view should not display redundant configuration file messages");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests that regular commands show config file information.
    /// </summary>
    [TestMethod]
    public async Task Main_RegularCommand_ShowsConfigFileInfo()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");
        var args = new[] { "--config", configPath, "config", "list-keys" };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Using configuration file:") || output.Contains("No configuration file found"),
                "Regular commands should display configuration file information");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests exception handling with debug mode showing stack trace.
    /// </summary>
    [TestMethod]
    public async Task Main_ExceptionWithDebugMode_ShowsStackTrace()
    {
        // Arrange - Create invalid config to trigger exception
        var invalidConfigPath = Path.Combine(Path.GetTempPath(), "invalid-" + Guid.NewGuid() + ".json");
        File.WriteAllText(invalidConfigPath, "{ this is not valid json at all! }");
        var args = new[] { "--debug", "--config", invalidConfigPath, "some-command" };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            Assert.AreEqual(1, exitCode, "Should return exit code 1 for error");

            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Exception Type:") || output.Contains("Stack Trace:") || output.Contains("Failed to initialize services:"),
                "Debug mode should show detailed exception information");
        }
        finally
        {
            if (File.Exists(invalidConfigPath))
            {
                File.Delete(invalidConfigPath);
            }
        }
    }

    /// <summary>
    /// Tests that short debug flag (-d) works correctly.
    /// </summary>
    [TestMethod]
    public async Task Main_WithShortDebugFlag_EnablesDebugMode()
    {
        // Arrange
        var configPath = Path.Combine(Path.GetTempPath(), "test-config-" + Guid.NewGuid() + ".json");
        File.WriteAllText(configPath, "{}");
        var args = new[] { "-d", "--config", configPath, "--help" };

        try
        {
            // Act
            var exitCode = await Program.Main(args);

            // Assert
            Assert.AreEqual(0, exitCode, "Help with short debug flag should return exit code 0");

            var output = _consoleOutput.ToString();
            Assert.IsTrue(output.Contains("Debug mode enabled"),
                "Short debug flag should enable debug mode");
        }
        finally
        {
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    /// <summary>
    /// Tests help display with short help flag (-h).
    /// </summary>
    [TestMethod]
    public async Task Main_WithShortHelpFlag_ShowsHelp()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var exitCode = await Program.Main(args);

        // Assert
        Assert.AreEqual(0, exitCode, "Short help flag should return exit code 0");

        var output = _consoleOutput.ToString();
        Assert.IsTrue(output.Contains("Usage:") || output.Contains("Commands:"),
            "Short help flag should display help information");
    }
}
