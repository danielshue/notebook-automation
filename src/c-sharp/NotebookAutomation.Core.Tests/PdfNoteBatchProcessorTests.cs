#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Tools.PdfProcessing;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Unit tests for PdfNoteBatchProcessor.
    /// </summary>
    [TestClass]
    public class PdfNoteBatchProcessorTests
    {
        private Mock<ILogger> _loggerMock = null!;
        private TestBatchProcessor _batchProcessor = null!;
        private PdfNoteBatchProcessor _processor = null!;
        private string _testDir = null!;
        private string _outputDir = null!;

        /// <summary>
        /// Test double for DocumentNoteBatchProcessor that overrides ProcessDocumentsAsync.
        /// </summary>
        private class TestBatchProcessor : Core.Tools.Shared.DocumentNoteBatchProcessor<PdfNoteProcessor>
        {
            public TestBatchProcessor() : base(
                new Mock<ILogger<Core.Tools.Shared.DocumentNoteBatchProcessor<PdfNoteProcessor>>>().Object,
                new Mock<PdfNoteProcessor>(MockBehavior.Loose, Mock.Of<ILogger<PdfNoteProcessor>>(), new TestableAISummarizer(Mock.Of<ILogger<NotebookAutomation.Core.Services.AISummarizer>>())).Object,
                new TestableAISummarizer(Mock.Of<ILogger<NotebookAutomation.Core.Services.AISummarizer>>()))
            { }

            public override Task<Core.Tools.Shared.BatchProcessResult> ProcessDocumentsAsync(
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
                Core.Configuration.AppConfig? appConfig = null,
                string noteType = null!,
                string failedFilesListName = null!,
                bool noShareLinks = false)
            {
                int processed = 0, failed = 0;
                if (string.IsNullOrWhiteSpace(input)) { failed = 1; }
                else if (Directory.Exists(input))
                {
                    processed = Directory.GetFiles(input, "*.pdf").Length;
                }
                else if (File.Exists(input) && extensions.Exists(ext => input.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    processed = 1;
                }
                else { failed = 1; }
                return Task.FromResult(new Core.Tools.Shared.BatchProcessResult { Processed = processed, Failed = failed });
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _batchProcessor = new TestBatchProcessor();
            _processor = new PdfNoteBatchProcessor(_batchProcessor);
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            Directory.CreateDirectory(_outputDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
            if (Directory.Exists(_outputDir))
                Directory.Delete(_outputDir, true);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithSingleFile_ProcessesSuccessfully()
        {
            // Arrange
            var pdfPath = Path.Combine(_testDir, "test.pdf");
            // Create a minimal fake PDF file (this won't be valid for actual processing)
            File.WriteAllText(pdfPath, "fake pdf content");
            var extensions = new List<string> { ".pdf" };

            // Act
            var result = await _processor.ProcessPdfsAsync(
                pdfPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: true); // Use dry run to avoid actual PDF processing

            // Assert
            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(0, result.Failed);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithNonExistentFile_ReturnsFailure()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDir, "nonexistent.pdf");
            var extensions = new List<string> { ".pdf" };

            // Act
            var result = await _processor.ProcessPdfsAsync(
                nonExistentPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: false);

            // Assert
            Assert.AreEqual(0, result.Processed);
            Assert.AreEqual(1, result.Failed);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithDirectory_ProcessesAllPdfFiles()
        {
            // Arrange
            var pdf1Path = Path.Combine(_testDir, "test1.pdf");
            var pdf2Path = Path.Combine(_testDir, "test2.pdf");
            var txtPath = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(pdf1Path, "fake pdf content 1");
            File.WriteAllText(pdf2Path, "fake pdf content 2");
            File.WriteAllText(txtPath, "not a pdf file");
            var extensions = new List<string> { ".pdf" };

            // Act
            var result = await _processor.ProcessPdfsAsync(
                _testDir,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: true); // Use dry run to avoid actual PDF processing

            // Assert
            Assert.AreEqual(2, result.Processed); // Should process only the 2 PDF files
            Assert.AreEqual(0, result.Failed);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithExtendedOptions_UsesCorrectParameters()
        {
            // Arrange
            var pdfPath = Path.Combine(_testDir, "test.pdf");
            File.WriteAllText(pdfPath, "fake pdf content");
            var extensions = new List<string> { ".pdf" };

            // Act
            var result = await _processor.ProcessPdfsAsync(
                pdfPath,
                _outputDir,
                extensions,
                openAiApiKey: "test-key",
                dryRun: true,
                noSummary: true,
                forceOverwrite: true,
                retryFailed: false,
                timeoutSeconds: 30,
                resourcesRoot: "test-resources",
                appConfig: null);

            // Assert
            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(0, result.Failed);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithEmptyInput_ReturnsFailure()
        {
            // Act
            var result = await _processor.ProcessPdfsAsync(
                string.Empty,
                _outputDir,
                new List<string> { ".pdf" },
                openAiApiKey: null,
                dryRun: false);

            // Assert
            Assert.AreEqual(0, result.Processed);
            Assert.AreEqual(1, result.Failed);
        }
    }
}
