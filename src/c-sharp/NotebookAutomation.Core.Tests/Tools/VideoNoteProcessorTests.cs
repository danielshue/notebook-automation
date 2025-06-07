using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Tools;

[TestClass]
public class VideoNoteProcessorTests
{
    private static MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        return new MetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            new AppConfig());
    }

    [TestMethod]
    public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
    {
        // Arrange
        Mock<ILogger<VideoNoteProcessor>> loggerMock = new();
        PromptTemplateService promptService = new(
            Mock.Of<ILogger<PromptTemplateService>>(),
            new YamlHelper(Mock.Of<ILogger<YamlHelper>>()),
            new AppConfig());
        AISummarizer aiSummarizer = new(
            Mock.Of<ILogger<AISummarizer>>(),
            promptService,
            null);
        Mock<IYamlHelper> yamlHelperMock = new();
        var hierarchyDetector = CreateMetadataHierarchyDetector();
        VideoNoteProcessor processor = new(loggerMock.Object, aiSummarizer, yamlHelperMock.Object, hierarchyDetector);

        // Act - using a null OpenAI key should result in simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text");

        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}

