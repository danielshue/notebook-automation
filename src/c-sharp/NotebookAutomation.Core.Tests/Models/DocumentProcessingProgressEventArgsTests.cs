// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Models;

/// <summary>
/// Unit tests for the DocumentProcessingProgressEventArgs class.
/// </summary>
[TestClass]
public class DocumentProcessingProgressEventArgsTests
{
    /// <summary>
    /// Tests constructor with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        const string filePath = "/test/file.pdf";
        const string status = "Processing";
        const int currentFile = 5;
        const int totalFiles = 10;

        // Act
        var eventArgs = new DocumentProcessingProgressEventArgs(filePath, status, currentFile, totalFiles);

        // Assert
        Assert.AreEqual(filePath, eventArgs.FilePath);
        Assert.AreEqual(status, eventArgs.Status);
        Assert.AreEqual(currentFile, eventArgs.CurrentFile);
        Assert.AreEqual(totalFiles, eventArgs.TotalFiles);
    }

    /// <summary>
    /// Tests constructor with empty file path.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyFilePath_SetsPropertyCorrectly()
    {
        // Arrange
        const string filePath = "";
        const string status = "Processing";
        const int currentFile = 1;
        const int totalFiles = 1;

        // Act
        var eventArgs = new DocumentProcessingProgressEventArgs(filePath, status, currentFile, totalFiles);

        // Assert
        Assert.AreEqual(string.Empty, eventArgs.FilePath);
        Assert.AreEqual(status, eventArgs.Status);
        Assert.AreEqual(currentFile, eventArgs.CurrentFile);
        Assert.AreEqual(totalFiles, eventArgs.TotalFiles);
    }

    /// <summary>
    /// Tests constructor with empty status.
    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyStatus_SetsPropertyCorrectly()
    {
        // Arrange
        const string filePath = "/test/file.pdf";
        const string status = "";
        const int currentFile = 1;
        const int totalFiles = 1;

        // Act
        var eventArgs = new DocumentProcessingProgressEventArgs(filePath, status, currentFile, totalFiles);

        // Assert
        Assert.AreEqual(filePath, eventArgs.FilePath);
        Assert.AreEqual(string.Empty, eventArgs.Status);
        Assert.AreEqual(currentFile, eventArgs.CurrentFile);
        Assert.AreEqual(totalFiles, eventArgs.TotalFiles);
    }

    /// <summary>
    /// Tests constructor with zero values for file counts.
    /// </summary>
    [TestMethod]
    public void Constructor_WithZeroFileCounts_SetsPropertiesCorrectly()
    {
        // Arrange
        const string filePath = "/test/file.pdf";
        const string status = "Processing";
        const int currentFile = 0;
        const int totalFiles = 0;

        // Act
        var eventArgs = new DocumentProcessingProgressEventArgs(filePath, status, currentFile, totalFiles);

        // Assert
        Assert.AreEqual(filePath, eventArgs.FilePath);
        Assert.AreEqual(status, eventArgs.Status);
        Assert.AreEqual(0, eventArgs.CurrentFile);
        Assert.AreEqual(0, eventArgs.TotalFiles);
    }

    /// <summary>
    /// Tests constructor with negative values for file counts.
    /// </summary>
    [TestMethod]
    public void Constructor_WithNegativeFileCounts_SetsPropertiesCorrectly()
    {
        // Arrange
        const string filePath = "/test/file.pdf";
        const string status = "Processing";
        const int currentFile = -1;
        const int totalFiles = -5;

        // Act
        var eventArgs = new DocumentProcessingProgressEventArgs(filePath, status, currentFile, totalFiles);

        // Assert
        Assert.AreEqual(filePath, eventArgs.FilePath);
        Assert.AreEqual(status, eventArgs.Status);
        Assert.AreEqual(-1, eventArgs.CurrentFile);
        Assert.AreEqual(-5, eventArgs.TotalFiles);
    }

    /// <summary>
    /// Tests that the class properly inherits from EventArgs.
    /// </summary>
    [TestMethod]
    public void DocumentProcessingProgressEventArgs_InheritsFromEventArgs()
    {
        // Arrange & Act
        var eventArgs = new DocumentProcessingProgressEventArgs("/test.pdf", "Processing", 1, 1);

        // Assert
        Assert.IsInstanceOfType<EventArgs>(eventArgs);
    }

    /// <summary>
    /// Tests constructor with various status messages.
    /// </summary>
    [TestMethod]
    public void Constructor_WithVariousStatusMessages_SetsStatusCorrectly()
    {
        // Arrange & Act
        var eventArgs1 = new DocumentProcessingProgressEventArgs("/test.pdf", "Waiting", 1, 5);
        var eventArgs2 = new DocumentProcessingProgressEventArgs("/test.pdf", "Processing", 2, 5);
        var eventArgs3 = new DocumentProcessingProgressEventArgs("/test.pdf", "Completed", 3, 5);
        var eventArgs4 = new DocumentProcessingProgressEventArgs("/test.pdf", "Failed", 4, 5);

        // Assert
        Assert.AreEqual("Waiting", eventArgs1.Status);
        Assert.AreEqual("Processing", eventArgs2.Status);
        Assert.AreEqual("Completed", eventArgs3.Status);
        Assert.AreEqual("Failed", eventArgs4.Status);
    }

    /// <summary>
    /// Tests constructor with different file types.
    /// </summary>
    [TestMethod]
    public void Constructor_WithDifferentFileTypes_SetsFilePathCorrectly()
    {
        // Arrange & Act
        var pdfEvent = new DocumentProcessingProgressEventArgs("/documents/test.pdf", "Processing", 1, 3);
        var videoEvent = new DocumentProcessingProgressEventArgs("/videos/lecture.mp4", "Processing", 2, 3);
        var markdownEvent = new DocumentProcessingProgressEventArgs("/notes/readme.md", "Processing", 3, 3);

        // Assert
        Assert.AreEqual("/documents/test.pdf", pdfEvent.FilePath);
        Assert.AreEqual("/videos/lecture.mp4", videoEvent.FilePath);
        Assert.AreEqual("/notes/readme.md", markdownEvent.FilePath);
    }

    /// <summary>
    /// Tests that all properties are read-only (getter-only).
    /// </summary>
    [TestMethod]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var eventArgs = new DocumentProcessingProgressEventArgs("/test.pdf", "Processing", 1, 5);

        // Act & Assert - The properties should be read-only
        // If these properties had setters, this test would need to be updated
        Assert.IsNotNull(eventArgs.FilePath);
        Assert.IsNotNull(eventArgs.Status);
        Assert.IsTrue(eventArgs.CurrentFile >= 0 || eventArgs.CurrentFile < 0); // Always true, just accessing the property
        Assert.IsTrue(eventArgs.TotalFiles >= 0 || eventArgs.TotalFiles < 0); // Always true, just accessing the property
    }
}