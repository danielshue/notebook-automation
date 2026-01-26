// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.PdfProcessing;

namespace NotebookAutomation.Tests.Core.Tools.PdfProcessing;

/// <summary>
/// Unit tests for the <see cref="PdfService"/> class.
/// Tests cover result types and DTOs for PDF processing operations.
/// </summary>
/// <remarks>
/// Note: Constructor and full integration tests are not included here because
/// PdfNoteBatchProcessor has complex constructors that make it difficult to mock directly.
/// These tests focus on the result types and DTOs used by the service.
/// </remarks>
[TestClass]
public class PdfServiceTests
{
    #region PdfOperationResult Tests

    /// <summary>
    /// Verifies PdfOperationResult default values.
    /// </summary>
    [TestMethod]
    public void PdfOperationResult_HasCorrectDefaults()
    {
        // Arrange & Act
        var result = new PdfOperationResult
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
    /// Verifies PdfOperationResult properties can be set.
    /// </summary>
    [TestMethod]
    public void PdfOperationResult_PropertiesCanBeSet()
    {
        // Arrange & Act
        var result = new PdfOperationResult
        {
            Success = false,
            Message = "Error occurred",
            FilesFound = 15,
            NotesCreated = 10,
            Failed = 3,
            DryRun = true,
            ProcessingTime = TimeSpan.FromMinutes(2),
            TotalTokens = 5000,
            ErrorMessage = "Processing error"
        };

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(15, result.FilesFound);
        Assert.AreEqual(10, result.NotesCreated);
        Assert.AreEqual(3, result.Failed);
        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(2, result.ProcessingTime.TotalMinutes);
        Assert.AreEqual(5000, result.TotalTokens);
        Assert.AreEqual("Processing error", result.ErrorMessage);
    }

    /// <summary>
    /// Verifies PdfOperationResult can represent success state.
    /// </summary>
    [TestMethod]
    public void PdfOperationResult_CanRepresentSuccessState()
    {
        // Arrange & Act
        var result = new PdfOperationResult
        {
            Success = true,
            Message = "Processed 5 PDFs successfully",
            FilesFound = 5,
            NotesCreated = 5,
            Failed = 0,
            DryRun = false,
            ProcessingTime = TimeSpan.FromSeconds(45)
        };

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(result.FilesFound, result.NotesCreated);
        Assert.AreEqual(0, result.Failed);
        Assert.IsNull(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies PdfOperationResult can represent partial failure.
    /// </summary>
    [TestMethod]
    public void PdfOperationResult_CanRepresentPartialFailure()
    {
        // Arrange & Act
        var result = new PdfOperationResult
        {
            Success = false,
            Message = "Processed with errors",
            FilesFound = 10,
            NotesCreated = 7,
            Failed = 3,
            ErrorMessage = "3 files failed to process"
        };

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(10, result.FilesFound);
        Assert.AreEqual(7, result.NotesCreated);
        Assert.AreEqual(3, result.Failed);
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion
}
