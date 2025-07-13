using NotebookAutomation.Tests.Core.Helpers;
using NotebookAutomation.Core.Tools;
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Models;

/// <summary>
/// Unit tests for BatchProcessResult and timing/token statistics in batch processing.
/// </summary>
[TestClass]
public class BatchProcessResultTests
{
    private Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>>? _loggerMock;
    private PdfNoteBatchProcessor? _processor;
    private Mock<ILogger<PdfNoteProcessor>>? _pdfLoggerMock;
    private Mock<ILogger<AISummarizer>>? _aiLoggerMock;
    private Mock<AISummarizer>? _aiSummarizerMock;
    private string? _testDir;
    private string? _outputDir;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>>();
        _pdfLoggerMock = new Mock<ILogger<PdfNoteProcessor>>();
        _aiLoggerMock = new Mock<ILogger<AISummarizer>>();
        _aiSummarizerMock = new Mock<AISummarizer>(_aiLoggerMock!.Object, null!, null!, null!);        // Create a mock AppConfig for MetadataHierarchyDetector
        var mockAppConfig = new Mock<AppConfig>();
        mockAppConfig.Setup(config => config.Paths).Returns(new PathsConfig { NotebookVaultFullpathRoot = Path.GetTempPath() });        // Create a real MetadataHierarchyDetector instead of mocking it
        var yamlHelper = new YamlHelper(Mock.Of<ILogger<YamlHelper>>());
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper, mockAppConfig.Object);
        var hierarchyDetector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            mockAppConfig.Object);

        PdfNoteProcessor pdfNoteProcessor = new(_pdfLoggerMock.Object, _aiSummarizerMock.Object, Mock.Of<IYamlHelper>(), hierarchyDetector, Mock.Of<IMetadataTemplateManager>(), Mock.Of<ICourseStructureExtractor>(), markdownNoteBuilder);
        DocumentNoteBatchProcessor<PdfNoteProcessor> batchProcessor = new(_loggerMock.Object, pdfNoteProcessor, _aiSummarizerMock.Object);
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
    public async Task BatchProcessResult_ReportsTimingAndTokens()
    {
        // Arrange
        string pdfPath = Path.Combine(_testDir!, "test.pdf");
        File.WriteAllText(pdfPath, "fake pdf content");
        List<string> extensions = [".pdf"];

        // Act
        BatchProcessResult result = await _processor!.ProcessPdfsAsync(
            pdfPath,
            _outputDir!,
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
