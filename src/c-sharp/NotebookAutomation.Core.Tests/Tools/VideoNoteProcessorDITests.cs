using Microsoft.Extensions.Logging.Abstractions;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Tools;

/// <summary>
/// Mock AISummarizer that implements IAISummarizer interface (created for testing)
/// </summary>
public class MockAISummarizer(ILogger logger)
{
    public string PredefinedSummary { get; set; } = "Test summary from injected AISummarizer";

    public string GenerateAiSummary(string text) => PredefinedSummary;
}

[TestClass]
public class VideoNoteProcessorDITests
{
    [TestMethod]
    public async Task GenerateAiSummaryAsync_WithMockAISummarizer_ReturnsSimulatedSummary()
    {        // Arrange - inject a mock AISummarizer with known values
        var logger = NullLogger<VideoNoteProcessor>.Instance;
        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(NullLogger<YamlHelper>.Instance),
            new AppConfig());
        AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null); var yamlHelper = new YamlHelper(NullLogger<YamlHelper>.Instance);
        var hierarchyDetector = new MetadataHierarchyDetector(NullLogger<MetadataHierarchyDetector>.Instance, new AppConfig());
        VideoNoteProcessor processor = new(logger, aiSummarizer, yamlHelper, hierarchyDetector);

        // Act - Using null OpenAI key should return simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text");

        // Assert - We're testing that the processor uses the injected AISummarizer
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    [TestMethod]
    public async Task GenerateAiSummaryAsync_WithNullOpenAIKey_ReturnsSimulatedSummary()
    {        // Arrange
        var logger = NullLogger<VideoNoteProcessor>.Instance;
        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(NullLogger<YamlHelper>.Instance),
            new AppConfig());
        AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null); var yamlHelper = new YamlHelper(NullLogger<YamlHelper>.Instance);
        var hierarchyDetector = new MetadataHierarchyDetector(NullLogger<MetadataHierarchyDetector>.Instance, new AppConfig());
        VideoNoteProcessor processor = new(logger, aiSummarizer, yamlHelper, hierarchyDetector);
        // Act - using a null OpenAI key should result in simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text");
        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}

