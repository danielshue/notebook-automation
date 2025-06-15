// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Tools.VideoProcessing;

[TestClass]
public class VideoNoteBatchProcessorResourcesRootTests
{
    private string _testDir = null!;
    private string _outputDir = null!;
    private Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>> _loggerMock = null!;

    // Removed unused field:
    // private Mock<AISummarizer> _aiSummarizerMock;
    private Mock<VideoNoteProcessor> _videoNoteProcessorMock = null!;
    private DocumentNoteBatchProcessor<VideoNoteProcessor> _batchProcessor = null!;
    private VideoNoteBatchProcessor _processor = null!;
    private string _tempMetadataFile = null!;
    private AppConfig _testAppConfig = null!;

    private static MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        return new MetadataHierarchyDetector(
            NullLogger<MetadataHierarchyDetector>.Instance,
            new AppConfig());
    }
    private MetadataTemplateManager CreateTestMetadataTemplateManager()
    {
        return new MetadataTemplateManager(
            Mock.Of<ILogger<MetadataTemplateManager>>(),
            _testAppConfig,
            Mock.Of<IYamlHelper>());
    }
    [TestInitialize]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
        _outputDir = Path.Combine(_testDir, "output");
        Directory.CreateDirectory(_outputDir);

        // Create temp metadata file
        _tempMetadataFile = Path.Combine(_testDir, "metadata.yaml");
        File.WriteAllText(_tempMetadataFile, "template-name: test-template\nfields: []");        // Create test AppConfig
        _testAppConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataFile = _tempMetadataFile
            }
        };

        _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>>();

        // Create a TestableAISummarizer that can be used in tests
        TestableAISummarizer testAISummarizer = new(Mock.Of<ILogger<AISummarizer>>());

        // Create a mock for IOneDriveService
        IOneDriveService mockOneDriveService = Mock.Of<IOneDriveService>();

        // Create mock YamlHelper
        var mockYamlHelper = Mock.Of<IYamlHelper>();        // Set up mock with test dependencies and updated constructor signature
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        _videoNoteProcessorMock = new Mock<VideoNoteProcessor>(
            Mock.Of<ILogger<VideoNoteProcessor>>(),
            testAISummarizer,
            mockYamlHelper, // Required YamlHelper parameter
            CreateMetadataHierarchyDetector(), // Required MetadataHierarchyDetector parameter
            CreateTestMetadataTemplateManager(), // Required MetadataTemplateManager parameter
            mockCourseStructureExtractor, // Required ICourseStructureExtractor parameter
            new MarkdownNoteBuilder(mockYamlHelper), // Required MarkdownNoteBuilder parameter
            mockOneDriveService, // Optional OneDriveService
            _testAppConfig); // Optional AppConfig

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
                It.IsAny<bool>()))
            .ReturnsAsync((
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
                bool noShareLinks) =>
            {
                // Write a file with resourcesRoot in its content for testing
                string fileName = Path.GetFileNameWithoutExtension(input);
                string outputPath = Path.Combine(output, $"{fileName}.md");
                File.WriteAllText(outputPath, $"Test note with onedriveFullpathRoot: {resourcesRoot ?? "default"}");

                return new BatchProcessResult { Processed = 1, Failed = 0 };
            });

        _batchProcessor = mockBatchProcessor.Object;
        _processor = new VideoNoteBatchProcessor(_batchProcessor);
    }
    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (!string.IsNullOrEmpty(_testDir) && Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }

            if (!string.IsNullOrEmpty(_tempMetadataFile) && File.Exists(_tempMetadataFile))
            {
                File.Delete(_tempMetadataFile);
            }
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
    }

    [TestMethod]
    public async Task ProcessVideosAsync_ResourcesRoot_OverridesConfigValue()
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
        .ConfigureAwait(false);        // Assert
        Assert.AreEqual(1, result.Processed);
        Assert.AreEqual(0, result.Failed);
        string notePath = Path.Combine(_outputDir, "test.md");
        Assert.IsTrue(File.Exists(notePath));
        string noteContent = File.ReadAllText(notePath);
        StringAssert.Contains(noteContent, customResourcesRoot);
    }
}
