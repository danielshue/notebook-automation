// <copyright file="BatchProcessResultTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/BatchProcessResultTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
// Duplicate using removed

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// Unit tests for BatchProcessResult and timing/token statistics in batch processing.
/// </summary>
[TestClass]
internal class BatchProcessResultTests
{
    private Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>> loggerMock;
    private PdfNoteBatchProcessor processor;
    private Mock<ILogger<PdfNoteProcessor>> pdfLoggerMock;
    private Mock<ILogger<AISummarizer>> aiLoggerMock;
    private Mock<AISummarizer> aiSummarizerMock;
    private string testDir;
    private string outputDir;

    [TestInitialize]
    public void Setup()
    {
        this.loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>>();
        this.pdfLoggerMock = new Mock<ILogger<PdfNoteProcessor>>();
        this.aiLoggerMock = new Mock<ILogger<AISummarizer>>();
        this.aiSummarizerMock = new Mock<AISummarizer>(this.aiLoggerMock.Object, null, null, null);

        // Create a mock AppConfig for MetadataHierarchyDetector
        var mockAppConfig = new Mock<AppConfig>();
        mockAppConfig.Object.Paths = new PathsConfig { NotebookVaultFullpathRoot = Path.GetTempPath() };

        // Create a real MetadataHierarchyDetector instead of mocking it
        var hierarchyDetector = new MetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            mockAppConfig.Object);

        PdfNoteProcessor pdfNoteProcessor = new(this.pdfLoggerMock.Object, this.aiSummarizerMock.Object, Mock.Of<IYamlHelper>(), hierarchyDetector);
        DocumentNoteBatchProcessor<PdfNoteProcessor> batchProcessor = new(this.loggerMock.Object, pdfNoteProcessor, this.aiSummarizerMock.Object);
        this.processor = new PdfNoteBatchProcessor(batchProcessor);
        this.testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        this.outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(this.testDir);
        Directory.CreateDirectory(this.outputDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(this.testDir))
        {
            Directory.Delete(this.testDir, true);
        }

        if (Directory.Exists(this.outputDir))
        {
            Directory.Delete(this.outputDir, true);
        }
    }

    [TestMethod]

    // Duplicate method removed. Only one definition of BatchProcessResult_ReportsTimingAndTokens remains.
    public async Task BatchProcessResult_ReportsTimingAndTokens()
    {
        // Arrange
        string pdfPath = Path.Combine(this.testDir, "test.pdf");
        File.WriteAllText(pdfPath, "fake pdf content");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await this.processor.ProcessPdfsAsync(
            pdfPath,
            this.outputDir,
            extensions,
            openAiApiKey: null,
            dryRun: true) // Use dry run to avoid actual PDF processing
        .ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.TotalBatchTime.TotalMilliseconds >= 0);
        Assert.IsTrue(result.TotalSummaryTime.TotalMilliseconds >= 0);
        Assert.IsTrue(result.TotalTokens >= 0);
        Assert.IsTrue(result.AverageFileTimeMs >= 0);
        Assert.IsTrue(result.AverageSummaryTimeMs >= 0);
        Assert.IsTrue(result.AverageTokens >= 0);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Summary));
    }
}
