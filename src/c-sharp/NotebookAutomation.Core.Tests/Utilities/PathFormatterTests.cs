// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Utils;

[TestClass]
public class PathFormatterTests
{
    [TestMethod]
    public void Format_WithDebugLogLevel_ReturnsFullPath()
    {
        // Arrange
        string path = @"D:\very\long\path\to\some\deeply\nested\directory\structure\with\a\very\long\filename.txt";

        // Act
        string formatted = PathFormatter.Format(path, LogLevel.Debug);

        // Assert
        Assert.AreEqual(path, formatted); // For Debug level, we expect the full path
    }

    [TestMethod]
    public void Format_WithTraceLogLevel_ReturnsFullPath()
    {
        // Arrange
        string path = @"D:\very\long\path\to\some\deeply\nested\directory\structure\with\a\very\long\filename.txt";

        // Act
        string formatted = PathFormatter.Format(path, LogLevel.Trace);

        // Assert
        Assert.AreEqual(path, formatted); // For Trace level, we expect the full path
    }

    [TestMethod]
    public void Format_WithInfoLogLevel_ReturnsShortenedPath()
    {
        // Arrange
        string path = @"D:\very\long\path\to\some\deeply\nested\directory\structure\with\a\very\long\filename.txt";

        // Act
        string formatted = PathFormatter.Format(path, LogLevel.Information);

        // Assert
        Assert.AreNotEqual(path, formatted); // For Info level, we expect a shortened path
        Assert.IsTrue(formatted.Length <= 80); // Should not exceed max length
        StringAssert.Contains(formatted, "filename.txt"); // Should still contain the filename
        Assert.IsTrue(formatted.StartsWith("...")); // Should start with ellipsis
    }

    [TestMethod]
    public void Format_WithShortPath_ReturnsOriginalPath()
    {
        // Arrange
        string path = @"C:\short\path\file.txt";

        // Act
        string formatted = PathFormatter.Format(path, LogLevel.Information);

        // Assert
        Assert.AreEqual(path, formatted); // Path is already short, shouldn't be changed
    }
    [TestMethod]
    public void Format_WithNullPath_ReturnsEmptyString()
    {
        // Arrange
        string? path = null;

        // Act
        string formatted = PathFormatter.Format(path!, LogLevel.Information);

        // Assert
        Assert.AreEqual(string.Empty, formatted);
    }

    [TestMethod]
    public void Format_WithEmptyPath_ReturnsEmptyString()
    {
        // Arrange
        string path = string.Empty;

        // Act
        string formatted = PathFormatter.Format(path, LogLevel.Information);

        // Assert
        Assert.AreEqual(string.Empty, formatted);
    }

    [TestMethod]
    public void ShortenPath_WithLongPath_KeepsFileNameAndShortenedPath()
    {
        // Arrange
        string path = @"D:\very\long\path\to\some\deeply\nested\directory\structure\with\a\very\long\filename.txt";
        int maxLength = 40;

        // Act
        string shortened = PathFormatter.ShortenPath(path, maxLength);

        // Assert
        Assert.IsTrue(shortened.Length <= maxLength);
        StringAssert.Contains(shortened, "filename.txt"); // Filename should be preserved
        Assert.IsTrue(shortened.StartsWith("...")); // Should start with ellipsis
    }

    [TestMethod]
    public void ShortenPath_WithVeryLongFilename_TruncatesFilename()
    {
        // Arrange
        string path = @"D:\path\to\extremelylongfilenamethatexceedsthemaximumlengthallowedfortheoutput.txt";
        int maxLength = 30;

        // Act
        string shortened = PathFormatter.ShortenPath(path, maxLength);

        // Assert
        Assert.IsTrue(shortened.Length <= maxLength);
        Assert.IsTrue(shortened.StartsWith("...")); // Should start with ellipsis
    }
    [TestMethod]
    public void LoggerExtensions_LogWithFormattedPath_AppliesCorrectFormatting()
    {
        // Arrange
        string? logMessage = null;
        MockLogger<PathFormatterTests> logger = new((level, msg) => logMessage = msg);
        string path = @"D:\very\long\path\to\some\deeply\nested\directory\structure\with\a\very\long\filename.txt";            // Act
        NotebookAutomation.Core.Utils.LoggerExtensions.LogWithFormattedPath(
            logger,
            LogLevel.Information,
            0,
            null,
            "Processing file: {FilePath}",
            path);

        // Assert
        Assert.IsNotNull(logMessage);
        Assert.IsTrue(logMessage.Contains("..."));
        Assert.IsTrue(logMessage.Contains("filename.txt"));
    }

    [TestMethod]
    public void LoggerExtensions_LogWithFormattedPath_WithDebugLevel_ShowsFullPath()
    {
        // Arrange
        string? logMessage = null;
        MockLogger<PathFormatterTests> logger = new((level, msg) => logMessage = msg);
        string path = @"D:\very\long\path\to\some\deeply\nested\directory\structure\with\a\very\long\filename.txt";

        // Act
        NotebookAutomation.Core.Utils.LoggerExtensions.LogWithFormattedPath(
            logger,
            LogLevel.Debug,
            0,
            null,
            "Processing file: {FilePath}",
            path);

        // Assert
        Assert.IsNotNull(logMessage);
        Assert.IsTrue(logMessage.Contains(path)); // Should contain the full path
    }
}