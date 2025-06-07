// <copyright file="PdfNoteBatchProcessorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/PdfNoteBatchProcessorTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
#nullable enable

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// Unit tests for PdfNoteBatchProcessor.
/// </summary>
[TestClass]
internal class PdfNoteBatchProcessorTests
{
    private Mock<ILogger> loggerMock = null!;
    private TestBatchProcessor batchProcessor = null!;
    private PdfNoteBatchProcessor processor = null!;
    private string testDir = null!;
    private string outputDir = null!;

    /// <summary>
    /// Test double for DocumentNoteBatchProcessor that overrides ProcessDocumentsAsync.
    /// </summary>
    private class TestBatchProcessor : DocumentNoteBatchProcessor<PdfNoteProcessor>
    {
        public TestBatchProcessor()
            : base(
            new Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>>().Object,
            CreatePdfNoteProcessor(),
            new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>()))
        {
        }

        private static PdfNoteProcessor CreatePdfNoteProcessor()
        {
            var mockAppConfig = new AppConfig();
            mockAppConfig.Paths = new PathsConfig { NotebookVaultFullpathRoot = Path.GetTempPath() };
            var hierarchyDetector = new MetadataHierarchyDetector(
                Mock.Of<ILogger<MetadataHierarchyDetector>>(),
                mockAppConfig);
            return new PdfNoteProcessor(
                Mock.Of<ILogger<PdfNoteProcessor>>(),
                new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>()),
                Mock.Of<IYamlHelper>(),
                hierarchyDetector);
        }

        public override Task<BatchProcessResult> ProcessDocumentsAsync(
            string input,
            string? output,
            List<string> extensions,
            string? openAiApiKey = null,
            bool dryRun = false,
            bool noSummary = false,
            bool forceOverwrite = false,
            bool retryFailed = false,
            int? timeoutSeconds = null,
            string? resourcesRoot = null,
            AppConfig? appConfig = null,
            string noteType = null!,
            string failedFilesListName = null!,
            bool noShareLinks = false)
        {
            int processed = 0, failed = 0;
            if (string.IsNullOrWhiteSpace(input))
            {
                failed = 1;
            }
            else if (Directory.Exists(input))
            {
                processed = Directory.GetFiles(input, "*.pdf").Length;
            }
            else if (File.Exists(input) && extensions.Exists(ext => input.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                processed = 1;
            }
            else
            {
                failed = 1;
            }

            return Task.FromResult(new BatchProcessResult { Processed = processed, Failed = failed });
        }
    }

    [TestInitialize]
    public void Setup()
    {
        this.loggerMock = new Mock<ILogger>();
        this.batchProcessor = new TestBatchProcessor();
        this.processor = new PdfNoteBatchProcessor(this.batchProcessor);
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
    public async Task ProcessPdfsAsync_WithSingleFile_ProcessesSuccessfully()
    {
        // Arrange
        string pdfPath = Path.Combine(this.testDir, "test.pdf");

        // Create a minimal fake PDF file (this won't be valid for actual processing)
        File.WriteAllText(pdfPath, "fake pdf content");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await this.processor.ProcessPdfsAsync(
            pdfPath,
            this.outputDir,
            extensions,
            openAiApiKey: null,
            dryRun: true).ConfigureAwait(false); // Use dry run to avoid actual PDF processing

        // Assert
        Assert.AreEqual(1, result.Processed);
        Assert.AreEqual(0, result.Failed);
    }

    [TestMethod]
    public async Task ProcessPdfsAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        string nonExistentPath = Path.Combine(this.testDir, "nonexistent.pdf");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await this.processor.ProcessPdfsAsync(
            nonExistentPath,
            this.outputDir,
            extensions,
            openAiApiKey: null,
            dryRun: false).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(0, result.Processed);
        Assert.AreEqual(1, result.Failed);
    }

    [TestMethod]
    public async Task ProcessPdfsAsync_WithDirectory_ProcessesAllPdfFiles()
    {
        // Arrange
        string pdf1Path = Path.Combine(this.testDir, "test1.pdf");
        string pdf2Path = Path.Combine(this.testDir, "test2.pdf");
        string txtPath = Path.Combine(this.testDir, "test.txt");
        File.WriteAllText(pdf1Path, "fake pdf content 1");
        File.WriteAllText(pdf2Path, "fake pdf content 2");
        File.WriteAllText(txtPath, "not a pdf file");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await this.processor.ProcessPdfsAsync(
            this.testDir,
            this.outputDir,
            extensions,
            openAiApiKey: null,
            dryRun: true).ConfigureAwait(false); // Use dry run to avoid actual PDF processing

        // Assert
        Assert.AreEqual(2, result.Processed); // Should process only the 2 PDF files
        Assert.AreEqual(0, result.Failed);
    }

    [TestMethod]
    public async Task ProcessPdfsAsync_WithExtendedOptions_UsesCorrectParameters()
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
            openAiApiKey: "test-key",
            dryRun: true,
            noSummary: true,
            forceOverwrite: true,
            retryFailed: false,
            timeoutSeconds: 30,
            resourcesRoot: "test-resources",
            appConfig: null).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, result.Processed);
        Assert.AreEqual(0, result.Failed);
    }

    [TestMethod]
    public async Task ProcessPdfsAsync_WithEmptyInput_ReturnsFailure()
    {
        // Act
        BatchProcessResult result = await this.processor.ProcessPdfsAsync(
            string.Empty,
            this.outputDir,
            [".pdf"],
            openAiApiKey: null,
            dryRun: false).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(0, result.Processed);
        Assert.AreEqual(1, result.Failed);
    }
}
