// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tests.Tools;

[TestClass]
public class VideoNoteProcessorTests
{
    private static MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        return new MetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            new AppConfig())
        { Logger = Mock.Of<ILogger<MetadataHierarchyDetector>>() };
    }

    [TestMethod]
    public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
    {
        // Arrange
        Mock<ILogger<VideoNoteProcessor>> loggerMock = new(); var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataFile = Path.Combine(Path.GetTempPath(), "test-metadata.yaml")
            }
        };
        PromptTemplateService promptService = new(
            Mock.Of<ILogger<PromptTemplateService>>(),
            new YamlHelper(Mock.Of<ILogger<YamlHelper>>()),
            appConfig);
        AISummarizer aiSummarizer = new(
            Mock.Of<ILogger<AISummarizer>>(),
            promptService,
            null);
        Mock<IYamlHelper> yamlHelperMock = new();
        var hierarchyDetector = CreateMetadataHierarchyDetector();
        var templateManager = new MetadataTemplateManager(
            Mock.Of<ILogger<MetadataTemplateManager>>(),
            appConfig,
            yamlHelperMock.Object);
        VideoNoteProcessor processor = new(loggerMock.Object, aiSummarizer, yamlHelperMock.Object, hierarchyDetector, templateManager);

        // Act - using a null OpenAI key should result in simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text").ConfigureAwait(false);

        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}
