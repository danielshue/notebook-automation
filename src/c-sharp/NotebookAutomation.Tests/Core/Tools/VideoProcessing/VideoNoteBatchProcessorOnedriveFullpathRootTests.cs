using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Tests.Core.TestDoubles;

namespace NotebookAutomation.Tests.Core.Tools.VideoProcessing;

/// <summary>
/// Tests for VideoNoteBatchProcessor OneDrive fullpath root parameter override functionality.
/// </summary>
[TestClass]
public class VideoNoteBatchProcessorOnedriveFullpathRootTests
{
    private string _testDir = null!;
    private string _outputDir = null!;
    private AppConfig _testAppConfig = null!;
    private Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>> _loggerMock = null!;

    // Removed unused field:
    // private Mock<AISummarizer> _aiSummarizerMock;
    private Mock<VideoNoteProcessor> _videoNoteProcessorMock = null!;
    private DocumentNoteBatchProcessor<VideoNoteProcessor> _batchProcessor = null!;
    private VideoNoteBatchProcessor _processor = null!;

    private MetadataTemplateManager CreateTestMetadataTemplateManager()
    {
        return MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
    }
    private static MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        // Use the packaged test schema file
        var schemaPath = Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, "config", "metadata-schema.yml");
        return MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(
            NullLogger<MetadataHierarchyDetector>.Instance,
            new AppConfig
            {
                Paths = new PathsConfig
                {
                    MetadataSchemaFile = schemaPath
                }
            });
    }
    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _outputDir = Path.Combine(_testDir, "output");
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(_outputDir);

        // Resolve schema file shipped with test outputs
        var schemaPath = Path.Combine(AppContext.BaseDirectory ?? Environment.CurrentDirectory, "config", "metadata-schema.yml");

        // Create test AppConfig
        _testAppConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = _testDir,
                MetadataSchemaFile = schemaPath,
                LoggingDir = Path.GetTempPath()
            }
        };

        _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>>();

        // Create a TestableAISummarizer that can be used in tests
        TestableAISummarizer testAISummarizer = new(Mock.Of<ILogger<AISummarizer>>());

        // Create a mock for IOneDriveService
        IOneDriveService mockOneDriveService = Mock.Of<IOneDriveService>();

        // Create a mock for IYamlHelper
        IYamlHelper mockYamlHelper = Mock.Of<IYamlHelper>();        // Set up mock with test dependencies
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        _videoNoteProcessorMock = new Mock<VideoNoteProcessor>(
            Mock.Of<ILogger<VideoNoteProcessor>>(),
            testAISummarizer,
            mockYamlHelper,
            CreateMetadataHierarchyDetector(),
            CreateTestMetadataTemplateManager(),
            mockCourseStructureExtractor,
            new MarkdownNoteBuilder(mockYamlHelper, _testAppConfig),
            mockOneDriveService,
            _testAppConfig,  // AppConfig
            new FieldValueResolverRegistry());  // FieldValueResolverRegistry (non-null)

        // Create a custom batch processor that will directly create a file with the resourcesRoot
        // so we can test that the parameter is being passed correctly
        Mock<DocumentNoteBatchProcessor<VideoNoteProcessor>> mockBatchProcessor = new(
            _loggerMock.Object,
            _videoNoteProcessorMock.Object,
            testAISummarizer);

        mockBatchProcessor
            .Setup(b => b.ProcessDocumentsAsync(
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
                It.IsAny<AppConfig>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns((
                string input,
                string output,
                List<string> extensions,
                string openAiApiKey,
                bool dryRun,
                bool noSummary,
                bool forceOverwrite,
                bool retryFailed,
                int? timeoutSeconds,
                string resourcesRoot,
                AppConfig appConfig,
                string noteType,
                string failedFilesListName,
                bool noShareLinks,
                string templateTypeName,
                string promptOverride) =>
            {
                // Write a file with resourcesRoot in its content for testing
                string fileName = Path.GetFileNameWithoutExtension(input);
                string outputPath = Path.Combine(output, $"{fileName}.md");
                File.WriteAllText(outputPath, $"Test note with onedriveFullpathRoot: {resourcesRoot ?? "default"}");

                return Task.FromResult(new BatchProcessResult { Processed = 1, Failed = 0 });
            });

        _batchProcessor = mockBatchProcessor.Object;
        _processor = new VideoNoteBatchProcessor(_batchProcessor);
    }
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    /// <summary>
    /// Verifies that ProcessVideosAsync onedriveFullpathRoot parameter overrides the configuration value.
    /// </summary>
    [TestMethod]
    public async Task ProcessVideosAsync_OnedriveFullpathRoot_OverridesConfigValue()
    {
        // Arrange
        string videoPath = Path.Combine(_testDir, "test.mp4");
        File.WriteAllText(videoPath, "fake video content");
        List<string> extensions = [".mp4"];
        string customResourcesRoot = Path.Combine(_testDir, "custom_resources");

        // Act
        BatchProcessResult result = await _processor.ProcessVideosAsync(
            videoPath,
            _outputDir,
            extensions,
            openAiApiKey: null,
            dryRun: false,
            noSummary: true,
            forceOverwrite: true,
            retryFailed: false,
            timeoutSeconds: null,
            resourcesRoot: customResourcesRoot)
        .ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, result.Processed);
        Assert.AreEqual(0, result.Failed);
        string notePath = Path.Combine(_outputDir, "test.md");
        Assert.IsTrue(File.Exists(notePath));
        string noteContent = File.ReadAllText(notePath);
        StringAssert.Contains(noteContent, customResourcesRoot);
    }
}
