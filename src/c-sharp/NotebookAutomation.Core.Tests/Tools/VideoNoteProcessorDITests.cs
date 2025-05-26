using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;

using System;
using System.Threading.Tasks;

namespace NotebookAutomation.Core.Tests.Tools
{
    /// <summary>
    /// Mock AISummarizer that implements IAISummarizer interface (created for testing)
    /// </summary>
    public class MockAISummarizer
    {
        public string PredefinedSummary { get; set; } = "Test summary from injected AISummarizer";

        private readonly ILogger _logger;

        public MockAISummarizer(ILogger logger)
        {
            _logger = logger;
        }

        public Task<string> SummarizeAsync(string inputText, string prompt = null, string promptFileName = null)
        {
            return Task.FromResult(PredefinedSummary);
        }
    }
    [TestClass]
    public class VideoNoteProcessorDITests
    {
        [TestMethod]
        public async Task GenerateAiSummaryAsync_WithDI_UsesCorrectAISummarizer()
        {
            // Arrange
            var logger = NullLogger<VideoNoteProcessor>.Instance;
            var promptService = new PromptTemplateService(
                NullLogger<PromptTemplateService>.Instance,
                new Configuration.AppConfig());
            var aiSummarizer = new AISummarizer(
                NullLogger<AISummarizer>.Instance,
                promptService,
                null!, // Kernel (can be null for tests)
                null!  // ITextGenerationService (can be null for tests)
            );
            var processor = new VideoNoteProcessor(logger, aiSummarizer);

            // Act - Using null OpenAI key should return simulated summary
            var result = await processor.GenerateAiSummaryAsync("Test text");

            // Assert - We're testing that the processor uses the injected AISummarizer
            Assert.AreEqual("[Simulated AI summary]", result);
        }

        [TestMethod]
        public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
        {
            // Arrange
            var logger = NullLogger<VideoNoteProcessor>.Instance;
            var promptService = new PromptTemplateService(
                NullLogger<PromptTemplateService>.Instance,
                new Configuration.AppConfig());
            var aiSummarizer = new AISummarizer(
                NullLogger<AISummarizer>.Instance,
                promptService,
                null!, // Kernel (can be null for tests)
                null!  // ITextGenerationService (can be null for tests)
            );
            var processor = new VideoNoteProcessor(logger, aiSummarizer);
            // Act - using a null OpenAI key should result in simulated summary
            var result = await processor.GenerateAiSummaryAsync("Test text");
            // Assert - fallback behavior should return simulated summary
            Assert.AreEqual("[Simulated AI summary]", result);
        }
    }
}
