// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Cli.Utilities;

/// <summary>
/// Unit tests for the <see cref="CommandLineModeDetector"/> class.
/// </summary>
[TestClass]
public class CommandLineModeDetectorTests
{
    /// <summary>
    /// Tests that IsDebugModeEnabled returns false when no debug arguments are present.
    /// </summary>
    [TestMethod]
    public void IsDebugModeEnabled_NoDebugArgs_ReturnsFalse()
    {
        // Arrange
        var args = new[] { "--help", "--verbose" };

        // Act
        var result = CommandLineModeDetector.IsDebugModeEnabled(args);

        // Assert
        Assert.IsFalse(result, "Should return false when no debug arguments are present");
    }


    /// <summary>
    /// Tests that IsDebugModeEnabled returns true when --debug is present.
    /// </summary>
    [TestMethod]
    public void IsDebugModeEnabled_WithLongDebugArg_ReturnsTrue()
    {
        // Arrange
        var args = new[] { "--help", "--debug", "--config", "test.json" };

        // Act
        var result = CommandLineModeDetector.IsDebugModeEnabled(args);

        // Assert
        Assert.IsTrue(result, "Should return true when --debug is present");
    }


    /// <summary>
    /// Tests that IsDebugModeEnabled returns true when -d is present.
    /// </summary>
    [TestMethod]
    public void IsDebugModeEnabled_WithShortDebugArg_ReturnsTrue()
    {
        // Arrange
        var args = new[] { "-d", "--help" };

        // Act
        var result = CommandLineModeDetector.IsDebugModeEnabled(args);

        // Assert
        Assert.IsTrue(result, "Should return true when -d is present");
    }


    /// <summary>
    /// Tests that IsDebugModeEnabled returns true when DEBUG environment variable is set to true.
    /// </summary>
    [TestMethod]
    public void IsDebugModeEnabled_WithDebugEnvVarTrue_ReturnsTrue()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("DEBUG");
        Environment.SetEnvironmentVariable("DEBUG", "true");
        var args = new[] { "--help" };

        try
        {
            // Act
            var result = CommandLineModeDetector.IsDebugModeEnabled(args);

            // Assert
            Assert.IsTrue(result, "Should return true when DEBUG environment variable is 'true'");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("DEBUG", originalValue);
        }
    }


    /// <summary>
    /// Tests that IsDebugModeEnabled returns true when DEBUG environment variable is set to 1.
    /// </summary>
    [TestMethod]
    public void IsDebugModeEnabled_WithDebugEnvVar1_ReturnsTrue()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("DEBUG");
        Environment.SetEnvironmentVariable("DEBUG", "1");
        var args = new[] { "--help" };

        try
        {
            // Act
            var result = CommandLineModeDetector.IsDebugModeEnabled(args);

            // Assert
            Assert.IsTrue(result, "Should return true when DEBUG environment variable is '1'");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("DEBUG", originalValue);
        }
    }


    /// <summary>
    /// Tests that IsDebugModeEnabled returns true when DEBUG environment variable is set to yes.
    /// </summary>
    [TestMethod]
    public void IsDebugModeEnabled_WithDebugEnvVarYes_ReturnsTrue()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("DEBUG");
        Environment.SetEnvironmentVariable("DEBUG", "yes");
        var args = new[] { "--help" };

        try
        {
            // Act
            var result = CommandLineModeDetector.IsDebugModeEnabled(args);

            // Assert
            Assert.IsTrue(result, "Should return true when DEBUG environment variable is 'yes'");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("DEBUG", originalValue);
        }
    }


    /// <summary>
    /// Tests that IsVerboseModeEnabled returns false when no verbose arguments are present.
    /// </summary>
    [TestMethod]
    public void IsVerboseModeEnabled_NoVerboseArgs_ReturnsFalse()
    {
        // Arrange
        var args = new[] { "--help", "--debug" };

        // Act
        var result = CommandLineModeDetector.IsVerboseModeEnabled(args);

        // Assert
        Assert.IsFalse(result, "Should return false when no verbose arguments are present");
    }


    /// <summary>
    /// Tests that IsVerboseModeEnabled returns true when --verbose is present.
    /// </summary>
    [TestMethod]
    public void IsVerboseModeEnabled_WithLongVerboseArg_ReturnsTrue()
    {
        // Arrange
        var args = new[] { "--help", "--verbose", "--config", "test.json" };

        // Act
        var result = CommandLineModeDetector.IsVerboseModeEnabled(args);

        // Assert
        Assert.IsTrue(result, "Should return true when --verbose is present");
    }


    /// <summary>
    /// Tests that IsVerboseModeEnabled returns true when -v is present.
    /// </summary>
    [TestMethod]
    public void IsVerboseModeEnabled_WithShortVerboseArg_ReturnsTrue()
    {
        // Arrange
        var args = new[] { "-v", "--help" };

        // Act
        var result = CommandLineModeDetector.IsVerboseModeEnabled(args);

        // Assert
        Assert.IsTrue(result, "Should return true when -v is present");
    }


    /// <summary>
    /// Tests that both modes can be enabled simultaneously.
    /// </summary>
    [TestMethod]
    public void BothModes_WithBothArgs_BothReturnTrue()
    {
        // Arrange
        var args = new[] { "--debug", "--verbose", "--help" };

        // Act
        var debugResult = CommandLineModeDetector.IsDebugModeEnabled(args);
        var verboseResult = CommandLineModeDetector.IsVerboseModeEnabled(args);

        // Assert
        Assert.IsTrue(debugResult, "Debug mode should be enabled");
        Assert.IsTrue(verboseResult, "Verbose mode should be enabled");
    }


    /// <summary>
    /// Tests that methods handle empty args array gracefully.
    /// </summary>
    [TestMethod]
    public void ModesDetection_EmptyArgs_ReturnsFalse()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var debugResult = CommandLineModeDetector.IsDebugModeEnabled(args);
        var verboseResult = CommandLineModeDetector.IsVerboseModeEnabled(args);

        // Assert
        Assert.IsFalse(debugResult, "Debug mode should be false for empty args");
        Assert.IsFalse(verboseResult, "Verbose mode should be false for empty args");
    }
}
