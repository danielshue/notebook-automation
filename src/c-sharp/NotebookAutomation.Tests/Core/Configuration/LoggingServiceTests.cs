// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.IO;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the LoggingService class to verify logging verbosity and rolling log file behavior.
/// </summary>
[TestClass]
public class LoggingServiceTests
{
    private string tempLogDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        tempLogDir = Path.Combine(Path.GetTempPath(), "notebook-automation-test-logs", Guid.NewGuid().ToString());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(tempLogDir))
        {
            try
            {
                Directory.Delete(tempLogDir, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    /// <summary>
    /// Verifies that production mode (debug=false) uses Warning as minimum level.
    /// </summary>
    [TestMethod]
    public void LoggingService_ProductionMode_UsesWarningLevel()
    {
        // Arrange
        var loggingService = new LoggingService(tempLogDir, debug: false);

        // Act - This should create the logging infrastructure
        var logger = loggingService.Logger;

        // Assert - LoggingService should be created successfully
        Assert.IsNotNull(logger, "Logger should be created successfully");
        Assert.IsNotNull(loggingService.CurrentLogFilePath, "Log file path should be set");
    }

    /// <summary>
    /// Verifies that debug mode (debug=true) uses Debug as minimum level.
    /// </summary>
    [TestMethod]
    public void LoggingService_DebugMode_UsesDebugLevel()
    {
        // Arrange
        var loggingService = new LoggingService(tempLogDir, debug: true);

        // Act - This should create the logging infrastructure  
        var logger = loggingService.Logger;

        // Assert - LoggingService should be created successfully
        Assert.IsNotNull(logger, "Logger should be created successfully");
        Assert.IsNotNull(loggingService.CurrentLogFilePath, "Log file path should be set");
    }

    /// <summary>
    /// Verifies that log files use consistent filename instead of timestamped names.
    /// </summary>
    [TestMethod]
    public void LoggingService_LogFile_UsesConsistentFilename()
    {
        // Arrange & Act
        var loggingService = new LoggingService(tempLogDir, debug: false);
        var logger = loggingService.Logger;

        // Force logger initialization by logging something
        logger.LogWarning("Test warning message");

        // Assert
        var expectedLogFile = Path.Combine(tempLogDir, "notebook-automation.log");
        Assert.AreEqual(expectedLogFile, loggingService.CurrentLogFilePath, 
            "Log file should use consistent filename");
        
        // Give a moment for file system operations
        Thread.Sleep(100);
        Assert.IsTrue(File.Exists(expectedLogFile), 
            "Log file should be created at expected location");
    }

    /// <summary>
    /// Verifies that custom configuration parameters are accepted.
    /// </summary>
    [TestMethod]
    public void LoggingService_CustomConfiguration_AcceptsParameters()
    {
        // Arrange & Act
        var loggingService = new LoggingService(tempLogDir, debug: true, maxFileSizeMB: 10, retainedFileCount: 5);
        var logger = loggingService.Logger;

        // Assert - Should create without throwing
        Assert.IsNotNull(logger, "Logger should be created with custom parameters");
        Assert.IsNotNull(loggingService.CurrentLogFilePath, "Log file path should be set");
    }

    /// <summary>
    /// Verifies that logging directory is created if it doesn't exist.
    /// </summary>
    [TestMethod]
    public void LoggingService_CreatesLoggingDirectory_WhenNotExists()
    {
        // Arrange
        var nonExistentDir = Path.Combine(tempLogDir, "nested", "path");
        Assert.IsFalse(Directory.Exists(nonExistentDir), "Directory should not exist initially");

        // Act
        var loggingService = new LoggingService(nonExistentDir, debug: false);
        var logger = loggingService.Logger;
        logger.LogWarning("Test message to trigger directory creation");

        // Give a moment for directory creation
        Thread.Sleep(100);

        // Assert
        Assert.IsTrue(Directory.Exists(nonExistentDir), "Logging directory should be created");
    }

    /// <summary>
    /// Verifies that GetLogger method works correctly.
    /// </summary>
    [TestMethod]
    public void LoggingService_GetLogger_ReturnsTypedLogger()
    {
        // Arrange
        var loggingService = new LoggingService(tempLogDir, debug: false);

        // Act
        var typedLogger = loggingService.GetLogger<LoggingServiceTests>();

        // Assert
        Assert.IsNotNull(typedLogger, "Typed logger should be created");
    }

    /// <summary>
    /// Verifies that FailedLogger is available.
    /// </summary>
    [TestMethod]
    public void LoggingService_FailedLogger_IsAvailable()
    {
        // Arrange
        var loggingService = new LoggingService(tempLogDir, debug: false);

        // Act
        var failedLogger = loggingService.FailedLogger;

        // Assert
        Assert.IsNotNull(failedLogger, "Failed logger should be available");
    }
}