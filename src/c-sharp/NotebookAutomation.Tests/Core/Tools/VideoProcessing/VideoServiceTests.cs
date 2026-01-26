// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.VideoProcessing;

namespace NotebookAutomation.Tests.Core.Tools.VideoProcessing;

/// <summary>
/// Unit tests for the <see cref="VideoService"/> class.
/// Tests cover result types and DTOs for video processing operations.
/// </summary>
/// <remarks>
/// Note: Constructor and full integration tests are not included here because
/// VideoNoteBatchProcessor and VideoTranscriptConsolidationService have complex
/// constructors that make them difficult to mock directly. These tests focus on
/// the result types and DTOs used by the service.
/// </remarks>
[TestClass]
public class VideoServiceTests
{

    #region VideoOperationResult Tests

    /// <summary>
    /// Verifies VideoOperationResult default values.
    /// </summary>
    [TestMethod]
    public void VideoOperationResult_HasCorrectDefaults()
    {
        // Arrange & Act
        var result = new VideoOperationResult
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
    /// Verifies VideoOperationResult properties can be set.
    /// </summary>
    [TestMethod]
    public void VideoOperationResult_PropertiesCanBeSet()
    {
        // Arrange & Act
        var result = new VideoOperationResult
        {
            Success = false,
            Message = "Error occurred",
            FilesFound = 10,
            NotesCreated = 5,
            Failed = 2,
            DryRun = true,
            ProcessingTime = TimeSpan.FromSeconds(30),
            TotalTokens = 1000,
            ErrorMessage = "Some error"
        };

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual(10, result.FilesFound);
        Assert.AreEqual(5, result.NotesCreated);
        Assert.AreEqual(2, result.Failed);
        Assert.IsTrue(result.DryRun);
        Assert.AreEqual(30, result.ProcessingTime.TotalSeconds);
        Assert.AreEqual(1000, result.TotalTokens);
        Assert.AreEqual("Some error", result.ErrorMessage);
    }

    #endregion

    #region VideoConsolidationResult Tests

    /// <summary>
    /// Verifies VideoConsolidationResult default values.
    /// </summary>
    [TestMethod]
    public void VideoConsolidationResult_HasCorrectDefaults()
    {
        // Arrange & Act
        var result = new VideoConsolidationResult
        {
            Success = true,
            Message = "Test"
        };

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Test", result.Message);
        Assert.AreEqual(0, result.TranscriptsAggregated);
        Assert.IsFalse(result.WasWritten);
    }

    /// <summary>
    /// Verifies VideoConsolidationResult properties can be set.
    /// </summary>
    [TestMethod]
    public void VideoConsolidationResult_PropertiesCanBeSet()
    {
        // Arrange & Act
        var result = new VideoConsolidationResult
        {
            Success = true,
            Message = "Consolidated 5 transcripts",
            OutputPath = "/output/consolidated.md",
            TranscriptsAggregated = 5,
            Skipped = 2,
            WasWritten = true,
            DryRun = false
        };

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("/output/consolidated.md", result.OutputPath);
        Assert.AreEqual(5, result.TranscriptsAggregated);
        Assert.AreEqual(2, result.Skipped);
        Assert.IsTrue(result.WasWritten);
        Assert.IsFalse(result.DryRun);
    }

    #endregion
}
