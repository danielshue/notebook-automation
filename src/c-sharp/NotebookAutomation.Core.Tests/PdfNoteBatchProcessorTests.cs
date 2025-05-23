using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NotebookAutomation.Core.Tools.PdfProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Unit tests for PdfNoteBatchProcessor.
    /// </summary>
    [TestClass]
    public class PdfNoteBatchProcessorTests
    {
        private Mock<ILogger> _loggerMock;
        private PdfNoteBatchProcessor _processor;
        private string _testDir;
        private string _outputDir;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _processor = new PdfNoteBatchProcessor(_loggerMock.Object);
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
            var (processed, failed) = await _processor.ProcessPdfsAsync(
                pdfPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: true); // Use dry run to avoid actual PDF processing

            // Assert
            Assert.AreEqual(1, processed);
            Assert.AreEqual(0, failed);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithNonExistentFile_ReturnsFailure()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDir, "nonexistent.pdf");
            var extensions = new List<string> { ".pdf" };

            // Act
            var (processed, failed) = await _processor.ProcessPdfsAsync(
                nonExistentPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: false);

            // Assert
            Assert.AreEqual(0, processed);
            Assert.AreEqual(1, failed);
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
            var (processed, failed) = await _processor.ProcessPdfsAsync(
                _testDir,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: true); // Use dry run to avoid actual PDF processing

            // Assert
            Assert.AreEqual(2, processed); // Should process only the 2 PDF files
            Assert.AreEqual(0, failed);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithExtendedOptions_UsesCorrectParameters()
        {
            // Arrange
            var pdfPath = Path.Combine(_testDir, "test.pdf");
            File.WriteAllText(pdfPath, "fake pdf content");
            var extensions = new List<string> { ".pdf" };

            // Act
            var (processed, failed) = await _processor.ProcessPdfsAsync(
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
            Assert.AreEqual(1, processed);
            Assert.AreEqual(0, failed);
        }

        [TestMethod]
        public async Task ProcessPdfsAsync_WithEmptyInput_ReturnsFailure()
        {
            // Act
            var (processed, failed) = await _processor.ProcessPdfsAsync(
                string.Empty,
                _outputDir,
                new List<string> { ".pdf" },
                openAiApiKey: null,
                dryRun: false);

            // Assert
            Assert.AreEqual(0, processed);
            Assert.AreEqual(1, failed);
        }
    }
}
