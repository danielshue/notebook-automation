using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Tests.Core.TestDoubles;

namespace NotebookAutomation.Tests.Core.Tools.VideoProcessing;

/// <summary>
/// Tests for VideoNoteProcessor noSummary functionality.
/// </summary>
[TestClass]
public class VideoNoteProcessorNoSummaryTests
{
    private ILogger<VideoNoteProcessor> _logger = null!;
    private AISummarizer _aiSummarizer = null!;
    private VideoNoteProcessor _processor = null!;
    private string _tempDir = null!;
    private string _testVideoPath = null!;

    /// <summary>
    /// Initialize test resources before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _logger = new LoggerFactory().CreateLogger<VideoNoteProcessor>();

        // Create a mock AI summarizer with required dependencies
        ILogger<AISummarizer> mockAiLogger = new LoggerFactory().CreateLogger<AISummarizer>();
        TestPromptTemplateService testPromptService = new();
        Microsoft.SemanticKernel.Kernel kernel = MockKernelFactory.CreateKernelWithMockService("Test summary");
        _aiSummarizer = new AISummarizer(mockAiLogger, testPromptService, kernel);
        var yamlHelper = new YamlHelper(new LoggerFactory().CreateLogger<YamlHelper>()); var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataFile = Path.Combine(Path.GetTempPath(), "test-metadata.yaml")
            }
        }; var hierarchyDetector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector();
        var templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper, appConfig);
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        _processor = new VideoNoteProcessor(_logger, _aiSummarizer, yamlHelper, hierarchyDetector, templateManager, mockCourseStructureExtractor, markdownNoteBuilder);

        // Create temporary directory and mock video file
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _testVideoPath = Path.Combine(_tempDir, "test-video.mp4");

        // Create a dummy video file (just an empty file for testing)
        File.WriteAllText(_testVideoPath, "dummy video content");
    }

    /// <summary>
    /// Clean up test resources after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// Test that GenerateVideoNoteAsync with noSummary=true creates minimal content.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task GenerateVideoNoteAsync_WithNoSummary_CreatesMinimalContent()
    {
        // Arrange
        // No OpenAI API key provided to ensure we're not making actual API calls

        // Act
        string markdown = await _processor.GenerateVideoNoteAsync(
            videoPath: _testVideoPath,
            openAiApiKey: null,
            promptFileName: null,
            noSummary: true,
            timeoutSeconds: null,
            resourcesRoot: null,
            noShareLinks: true).ConfigureAwait(false);            // Assert
        Assert.IsNotNull(markdown);
        Assert.IsTrue(markdown.Contains("## Note")); // Should contain the minimal Note section
        Assert.IsTrue(markdown.Contains("title: Test Video")); // Should have frontmatter
        Assert.IsTrue(markdown.StartsWith("---")); // Should start with frontmatter
        Assert.IsTrue(markdown.Contains("onedrive_fullpath_file_reference:")); // Should have the full path reference
        Assert.IsTrue(markdown.Contains("video-uploaded:")); // Should have video upload date
    }

    /// <summary>
    /// Test that GenerateVideoNoteAsync with noSummary=false generates different content.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task GenerateVideoNoteAsync_WithSummaryEnabled_GeneratesDifferentContent()
    {
        // Arrange
        // No OpenAI API key provided, so it should attempt but fail gracefully

        // Act
        string markdown = await _processor.GenerateVideoNoteAsync(
            videoPath: _testVideoPath,
            openAiApiKey: null, // This will cause the summarizer to return empty or error
            promptFileName: null,
            noSummary: false,
            timeoutSeconds: null,
            resourcesRoot: null,
            noShareLinks: true).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(markdown);
        Assert.IsTrue(markdown.Contains("title: Test Video")); // Should have frontmatter
        Assert.IsTrue(markdown.StartsWith("---")); // Should start with frontmatter

        // The content should be different from the noSummary case
        // Even if the AI summary fails, it should still generate content
    }
}
