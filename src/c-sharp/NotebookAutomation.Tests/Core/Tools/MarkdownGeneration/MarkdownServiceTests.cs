// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Tests.Core.Tools.MarkdownGeneration;

/// <summary>
/// Unit tests for the <see cref="MarkdownService"/> class.
/// Tests cover result types and DTOs for markdown generation operations.
/// </summary>
/// <remarks>
/// Note: Constructor and full integration tests are not included here because
/// MarkdownNoteBatchProcessor has complex constructors that make it difficult to mock directly.
/// These tests focus on the result types and DTOs used by the service.
/// </remarks>
[TestClass]
public class MarkdownServiceTests
{
    #region MarkdownOperationResult Tests

    /// <summary>
    /// Verifies MarkdownOperationResult default values.
    /// </summary>
    [TestMethod]
    public void MarkdownOperationResult_HasCorrectDefaults()
    {
        // Arrange & Act
        var result = new MarkdownOperationResult
        {
            Success = true,
            Message = "Test"
        };

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Test", result.Message);
        Assert.AreEqual(0, result.FilesFound);
        Assert.AreEqual(0, result.NotesCreated);
        Assert.IsFalse(result.DryRun);
    }

    /// <summary>
    /// Verifies MarkdownOperationResult properties can be set.
    /// </summary>
    [TestMethod]
    public void MarkdownOperationResult_PropertiesCanBeSet()
    {
        // Arrange & Act
        var result = new MarkdownOperationResult
        {
            Success = false,
            Message = "Error occurred",
            FilesFound = 20,
            NotesCreated = 15,
            Failed = 5,
            DryRun = true,
            ProcessingTime = TimeSpan.FromMinutes(5),
            TotalTokens = 10000,
            ErrorMessage = "Conversion error"
        };

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(20, result.FilesFound);
        Assert.AreEqual(15, result.NotesCreated);
        Assert.AreEqual(5, result.Failed);
        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(5, result.ProcessingTime.TotalMinutes);
        Assert.AreEqual(10000, result.TotalTokens);
        Assert.AreEqual("Conversion error", result.ErrorMessage);
    }

    /// <summary>
    /// Verifies MarkdownOperationResult can represent dry run state.
    /// </summary>
    [TestMethod]
    public void MarkdownOperationResult_CanRepresentDryRunState()
    {
        // Arrange & Act
        var result = new MarkdownOperationResult
        {
            Success = true,
            Message = "[DRY RUN] Would convert 10 HTML files",
            FilesFound = 10,
            NotesCreated = 0,
            Failed = 0,
            DryRun = true,
            ProcessingTime = TimeSpan.FromSeconds(1)
        };

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(10, result.FilesFound);
        Assert.AreEqual(0, result.NotesCreated);
        Assert.IsTrue(result.Message.Contains("DRY RUN"));
    }

    /// <summary>
    /// Verifies MarkdownOperationResult can represent complete success.
    /// </summary>
    [TestMethod]
    public void MarkdownOperationResult_CanRepresentCompleteSuccess()
    {
        // Arrange & Act
        var result = new MarkdownOperationResult
        {
            Success = true,
            Message = "Converted 8 HTML files to markdown",
            FilesFound = 8,
            NotesCreated = 8,
            Failed = 0,
            DryRun = false
        };

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsFalse(result.DryRun);
        Assert.AreEqual(result.FilesFound, result.NotesCreated);
        Assert.AreEqual(0, result.Failed);
        Assert.IsNull(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies MarkdownOperationResult can represent partial success.
    /// </summary>
    [TestMethod]
    public void MarkdownOperationResult_CanRepresentPartialSuccess()
    {
        // Arrange & Act
        var result = new MarkdownOperationResult
        {
            Success = false,
            Message = "Converted with some failures",
            FilesFound = 12,
            NotesCreated = 9,
            Failed = 3,
            ErrorMessage = "3 EPUB files could not be parsed"
        };

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(12, result.FilesFound);
        Assert.AreEqual(9, result.NotesCreated);
        Assert.AreEqual(3, result.Failed);
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion
}
