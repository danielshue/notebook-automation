using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Tools;

/// <summary>
/// Unit tests for VideoNoteProcessor AI summary generation and fallback behavior.
/// </summary>
[TestClass]
public class VideoNoteProcessorTests
{
    private static MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        return MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            new AppConfig());
    }

    /// <summary>
    /// Verifies that VideoNoteProcessor falls back to creating a new AISummarizer when one is not injected.
    /// </summary>
    [TestMethod]
    public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
    {
        // Arrange
        Mock<ILogger<VideoNoteProcessor>> loggerMock = new();
        var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                // Use packaged schema for tests (schema-first)
                MetadataSchemaFile = Path.Combine(AppContext.BaseDirectory, "config", "metadata-schema.yml"),
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
        var hierarchyDetector = CreateMetadataHierarchyDetector(); var templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelperMock.Object, appConfig);
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        VideoNoteProcessor processor = new(
            loggerMock.Object,
            aiSummarizer,
            yamlHelperMock.Object,
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
