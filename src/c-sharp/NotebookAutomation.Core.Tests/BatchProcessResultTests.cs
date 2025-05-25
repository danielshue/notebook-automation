using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NotebookAutomation.Core.Tools.PdfProcessing;
using NotebookAutomation.Core.Tools.Shared;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Unit tests for BatchProcessResult and timing/token statistics in batch processing.
    /// </summary>
    [TestClass]
    public class BatchProcessResultTests
    {
        private Mock<ILogger> _loggerMock;
        private Mock<DocumentNoteBatchProcessor<PdfNoteProcessor>> _batchProcessorMock;
        private PdfNoteBatchProcessor _processor;
        private string _testDir;
        private string _outputDir;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger>();
            _batchProcessorMock = new Mock<DocumentNoteBatchProcessor<PdfNoteProcessor>>(MockBehavior.Strict, null, null, null);
            _batchProcessorMock
                .Setup(x => x.ProcessDocumentsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<Configuration.AppConfig>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((string input, string output, List<string> extensions, string openAiApiKey, bool dryRun, bool noSummary, bool forceOverwrite, bool retryFailed, int? timeoutSeconds, string resourcesRoot, Configuration.AppConfig appConfig, string noteType, string failedFilesListName) =>
                {
                    // Simulate a BatchProcessResult with timing/token stats for assertions
                    return new BatchProcessResult
                    {
                        Processed = 1,
                        Failed = 0,
                        TotalBatchTime = TimeSpan.FromMilliseconds(100),
                        TotalSummaryTime = TimeSpan.FromMilliseconds(50),
                        TotalTokens = 123,
                        AverageFileTimeMs = 100,
                        AverageSummaryTimeMs = 50,
                        AverageTokens = 123,
                        Summary = "Test summary"
                    };
                });
            _processor = new PdfNoteBatchProcessor(_batchProcessorMock.Object);
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
        public async Task BatchProcessResult_ReportsTimingAndTokens()
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
                openAiApiKey: null,
                dryRun: true // Use dry run to avoid actual PDF processing
            );

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
}
