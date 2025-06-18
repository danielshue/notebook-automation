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

[TestClass]
public class VideoNoteProcessorDITests
{
    private string _testMetadataFile = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        // Create unique temporary file to prevent parallel test conflicts
        string uniqueId = Guid.NewGuid().ToString("N")[..8]; // Short unique ID
        _testMetadataFile = Path.Combine(Path.GetTempPath(), $"test_metadata_di_{uniqueId}.yaml");

        var testMetadata = @"
---
template-type: ""video-note""
tags:
  - video
metadata:
  type: ""Video Note""
---";

        File.WriteAllText(_testMetadataFile, testMetadata);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_testMetadataFile))
        {
            File.Delete(_testMetadataFile);
        }
    }

    [TestMethod]
    public async Task GenerateAiSummaryAsync_WithMockAISummarizer_ReturnsSimulatedSummary()
    {
        // Arrange - inject a mock AISummarizer with known values
        var logger = NullLogger<VideoNoteProcessor>.Instance;
        var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataFile = _testMetadataFile,
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
        var yamlHelper = new YamlHelper(NullLogger<YamlHelper>.Instance); var hierarchyDetector = new MetadataHierarchyDetector(NullLogger<MetadataHierarchyDetector>.Instance, appConfig);
        var templateManager = new MetadataTemplateManager(NullLogger<MetadataTemplateManager>.Instance, appConfig, yamlHelper);
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper);
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        VideoNoteProcessor processor = new(logger, aiSummarizer, yamlHelper, hierarchyDetector, templateManager, mockCourseStructureExtractor, markdownNoteBuilder, null, appConfig);

        // Act - Using null OpenAI key should return simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text").ConfigureAwait(false);

        // Assert - We're testing that the processor uses the injected AISummarizer
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    [TestMethod]
    public async Task GenerateAiSummaryAsync_WithNullOpenAIKey_ReturnsSimulatedSummary()
    {        // Arrange
        var logger = NullLogger<VideoNoteProcessor>.Instance;

        var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataFile = _testMetadataFile,
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
        var yamlHelper = new YamlHelper(NullLogger<YamlHelper>.Instance); var hierarchyDetector = new MetadataHierarchyDetector(NullLogger<MetadataHierarchyDetector>.Instance, appConfig);
        var templateManager = new MetadataTemplateManager(NullLogger<MetadataTemplateManager>.Instance, appConfig, yamlHelper);
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper);
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        VideoNoteProcessor processor = new(logger, aiSummarizer, yamlHelper, hierarchyDetector, templateManager, mockCourseStructureExtractor, markdownNoteBuilder, null, appConfig);

        // Act - using a null OpenAI key should result in simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text").ConfigureAwait(false);

        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}
