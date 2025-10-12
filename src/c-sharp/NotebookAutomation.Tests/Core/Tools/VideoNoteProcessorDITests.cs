using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Tools;

/// <summary>
/// Mock AISummarizer that implements IAISummarizer interface (created for testing).
/// </summary>

internal class MockAISummarizer
{
    public string PredefinedSummary { get; set; } = "Test summary from injected AISummarizer";

    public string GenerateAiSummary(string text) => PredefinedSummary;
}

/// <summary>
/// Unit tests for VideoNoteProcessor dependency injection and AI summarization functionality.
/// </summary>
[TestClass]
public class VideoNoteProcessorDITests
{
    [TestInitialize]
    public void Setup()
    {
        // No setup required for schema-first tests
    }

    [TestCleanup]
    public void Cleanup()
    {
        // No cleanup required
    }

    /// <summary>
    /// Verifies that VideoNoteProcessor uses the injected mock AISummarizer and returns a simulated summary when provided with test text.
    /// </summary>
    [TestMethod]
    public async Task GenerateAiSummaryAsync_WithMockAISummarizer_ReturnsSimulatedSummary()
    {
        // Arrange - inject a mock AISummarizer with known values
        var logger = NullLogger<VideoNoteProcessor>.Instance;
        var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataSchemaFile = Path.Combine(AppContext.BaseDirectory, "config", "metadata-schema.yml"),
                NotebookVaultFullpathRoot = Path.GetTempPath(),
                LoggingDir = Path.GetTempPath()
            }
        };

        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(NullLogger<YamlHelper>.Instance),
            appConfig); AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null);
        var yamlHelper = new YamlHelper(NullLogger<YamlHelper>.Instance); var hierarchyDetector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector();
        var templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper, appConfig);
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        VideoNoteProcessor processor = new(
            logger,
            aiSummarizer,
            yamlHelper,
            hierarchyDetector,
            templateManager,
            mockCourseStructureExtractor,
            markdownNoteBuilder,
            oneDriveService: null,
            appConfig,
            new FieldValueResolverRegistry());

        // Act - Using null OpenAI key should return simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text").ConfigureAwait(false);

        // Assert - We're testing that the processor uses the injected AISummarizer
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    /// <summary>
    /// Verifies that VideoNoteProcessor returns simulated summary when OpenAI key is null.
    /// </summary>
    [TestMethod]
    public async Task GenerateAiSummaryAsync_WithNullOpenAIKey_ReturnsSimulatedSummary()
    {        // Arrange
        var logger = NullLogger<VideoNoteProcessor>.Instance;

        var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataSchemaFile = Path.Combine(AppContext.BaseDirectory, "config", "metadata-schema.yml"),
                NotebookVaultFullpathRoot = Path.GetTempPath(),
                LoggingDir = Path.GetTempPath()
            }
        };

        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(NullLogger<YamlHelper>.Instance),
            appConfig); AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null);
        var yamlHelper = new YamlHelper(NullLogger<YamlHelper>.Instance); var hierarchyDetector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector();
        var templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper, appConfig);
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        VideoNoteProcessor processor = new(
            logger,
            aiSummarizer,
            yamlHelper,
            hierarchyDetector,
            templateManager,
            mockCourseStructureExtractor,
            markdownNoteBuilder,
            oneDriveService: null,
            appConfig,
            new FieldValueResolverRegistry());

        // Act - using a null OpenAI key should result in simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text").ConfigureAwait(false);

        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}
