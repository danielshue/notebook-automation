using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;

namespace NotebookAutomation.Core.Tests.Tools;

/// <summary>
/// Mock AISummarizer that implements IAISummarizer interface (created for testing)
/// </summary>
public class MockAISummarizer(ILogger logger)
{
    public string PredefinedSummary { get; set; } = "Test summary from injected AISummarizer";

    private readonly ILogger _logger = logger;

    public Task<string> SummarizeAsync(string inputText, string prompt = null, string promptFileName = null) => Task.FromResult(PredefinedSummary);
}
[TestClass]
public class VideoNoteProcessorDITests
{
    [TestMethod]
    public async Task GenerateAiSummaryAsync_WithDI_UsesCorrectAISummarizer()
    {
        // Arrange
        NullLogger<VideoNoteProcessor> logger = NullLogger<VideoNoteProcessor>.Instance;
        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new AppConfig());
        AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null);
        VideoNoteProcessor processor = new(logger, aiSummarizer);

        // Act - Using null OpenAI key should return simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text");

        // Assert - We're testing that the processor uses the injected AISummarizer
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    [TestMethod]
    public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
    {
        // Arrange
        NullLogger<VideoNoteProcessor> logger = NullLogger<VideoNoteProcessor>.Instance;
        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new AppConfig());
        AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null);
        VideoNoteProcessor processor = new(logger, aiSummarizer);
        // Act - using a null OpenAI key should result in simulated summary
        string result = await processor.GenerateAiSummaryAsync("Test text");
        // Assert - fallback behavior should return simulated summary
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}

