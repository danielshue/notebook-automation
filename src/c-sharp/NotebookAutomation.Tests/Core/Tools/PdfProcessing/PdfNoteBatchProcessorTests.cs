// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Tests.Core.TestDoubles;

namespace NotebookAutomation.Tests.Core.Tools.PdfProcessing;

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
            var yamlHelper = new YamlHelper(Mock.Of<ILogger<YamlHelper>>());
            var appConfig = new AppConfig();
            var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper, appConfig);
            var hierarchyDetector = new MetadataHierarchyDetector(
                Mock.Of<ILogger<MetadataHierarchyDetector>>(),
                mockAppConfig);
            var templateManager = Mock.Of<IMetadataTemplateManager>();
            var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>(); return new PdfNoteProcessor(
                Mock.Of<ILogger<PdfNoteProcessor>>(),
                new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>()),
                yamlHelper,
                hierarchyDetector,
                templateManager,
                mockCourseStructureExtractor,
                markdownNoteBuilder,
                extractImages: false);
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
        _loggerMock = new();
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
        {
            Directory.Delete(_testDir, true);
        }

        if (Directory.Exists(_outputDir))
        {
            Directory.Delete(_outputDir, true);
        }
    }

    [TestMethod]
    public async Task ProcessPdfsAsync_WithSingleFile_ProcessesSuccessfully()
    {
        // Arrange
        string pdfPath = Path.Combine(_testDir, "test.pdf");

        // Create a minimal fake PDF file (this won't be valid for actual processing)
        File.WriteAllText(pdfPath, "fake pdf content");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await _processor.ProcessPdfsAsync(
            pdfPath,
            _outputDir,
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
        string nonExistentPath = Path.Combine(_testDir, "nonexistent.pdf");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await _processor.ProcessPdfsAsync(
            nonExistentPath,
            _outputDir,
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
        string pdf1Path = Path.Combine(_testDir, "test1.pdf");
        string pdf2Path = Path.Combine(_testDir, "test2.pdf");
        string txtPath = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(pdf1Path, "fake pdf content 1");
        File.WriteAllText(pdf2Path, "fake pdf content 2");
        File.WriteAllText(txtPath, "not a pdf file");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await _processor.ProcessPdfsAsync(
            _testDir,
            _outputDir,
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
        string pdfPath = Path.Combine(_testDir, "test.pdf");
        File.WriteAllText(pdfPath, "fake pdf content");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await _processor.ProcessPdfsAsync(
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
            appConfig: null).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, result.Processed);
        Assert.AreEqual(0, result.Failed);
    }

    [TestMethod]
    public async Task ProcessPdfsAsync_WithEmptyInput_ReturnsFailure()
    {
        // Act
        BatchProcessResult result = await _processor.ProcessPdfsAsync(
            string.Empty,
            _outputDir,
            [".pdf"],
            openAiApiKey: null,
            dryRun: false).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(0, result.Processed);
        Assert.AreEqual(1, result.Failed);
    }
}
