using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Tools;

[TestClass]
public class VideoNoteProcessorTests
{
    [TestMethod]
    public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
    {
        // Arrange
        Mock<ILogger<VideoNoteProcessor>> loggerMock = new();
        PromptTemplateService promptService = new(
            Mock.Of<ILogger<PromptTemplateService>>(),
            new AppConfig());
        AISummarizer aiSummarizer = new(
            Mock.Of<ILogger<AISummarizer>>(),
            promptService,
            null);
        Mock<IYamlHelper> yamlHelperMock = new();
        VideoNoteProcessor processor = new(loggerMock.Object, aiSummarizer, yamlHelperMock.Object);
        // Act - using a null OpenAI key should result in simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text");
        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}

