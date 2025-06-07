// <copyright file="VideoNoteProcessorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Tools/VideoNoteProcessorTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests.Tools;

[TestClass]
internal class VideoNoteProcessorTests
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
        string result = await processor.GenerateAiSummaryAsync("Test text").ConfigureAwait(false);

        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}
