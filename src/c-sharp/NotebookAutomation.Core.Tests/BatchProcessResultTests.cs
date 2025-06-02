using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.PdfProcessing;
using NotebookAutomation.Core.Tools.Shared;
// Duplicate using removed

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// Unit tests for BatchProcessResult and timing/token statistics in batch processing.
/// </summary>
[TestClass]
public class BatchProcessResultTests
{
    private Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>> _loggerMock;
    private PdfNoteBatchProcessor _processor;
    private Mock<ILogger<PdfNoteProcessor>> _pdfLoggerMock;
    private Mock<ILogger<AISummarizer>> _aiLoggerMock;
    private Mock<AISummarizer> _aiSummarizerMock;
    private string _testDir;
    private string _outputDir;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>>();
        _pdfLoggerMock = new Mock<ILogger<PdfNoteProcessor>>();
        _aiLoggerMock = new Mock<ILogger<AISummarizer>>();
        _aiSummarizerMock = new Mock<AISummarizer>(_aiLoggerMock.Object, null, null, null);
        PdfNoteProcessor pdfNoteProcessor = new PdfNoteProcessor(_pdfLoggerMock.Object, _aiSummarizerMock.Object);
        DocumentNoteBatchProcessor<PdfNoteProcessor> batchProcessor = new DocumentNoteBatchProcessor<PdfNoteProcessor>(_loggerMock.Object, pdfNoteProcessor, _aiSummarizerMock.Object);
        _processor = new PdfNoteBatchProcessor(batchProcessor);
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_outputDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }

        if (Directory.Exists(_outputDir))
        {
            Directory.Delete(_outputDir, true);
        }
    }

    [TestMethod]
    // Duplicate method removed. Only one definition of BatchProcessResult_ReportsTimingAndTokens remains.
    public async Task BatchProcessResult_ReportsTimingAndTokens()
    {
        // Arrange
        string pdfPath = Path.Combine(_testDir, "test.pdf");
        File.WriteAllText(pdfPath, "fake pdf content");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await _processor.ProcessPdfsAsync(
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
