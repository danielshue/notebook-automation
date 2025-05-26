using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;

using System.Threading.Tasks;

namespace NotebookAutomation.Core.Tests.Tools
{
    [TestClass]
    public class VideoNoteProcessorTests
    {
        [TestMethod]
        public async Task GenerateAiSummaryAsync_UsesInjectedAISummarizer()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<VideoNoteProcessor>>();

            // Create an instance of TestableAISummarizer instead of trying to mock it
            var testableAiSummarizer = new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>());

            // Setup using stub method to make test predictable
            testableAiSummarizer.SetupSummarizeAsyncResult("Test summary from injected AISummarizer");

            // Create VideoNoteProcessor with the TestableAISummarizer
            var processor = new VideoNoteProcessor(loggerMock.Object, testableAiSummarizer);

            // Act
            var result = await processor.GenerateAiSummaryAsync("Test text");

            // Assert
            Assert.AreEqual("Test summary from injected AISummarizer", result);
        }

        [TestMethod]
        public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<VideoNoteProcessor>>();
            var promptService = new PromptTemplateService(
                Mock.Of<ILogger<PromptTemplateService>>(),
                new Configuration.AppConfig());
            var aiSummarizer = new AISummarizer(
                Mock.Of<ILogger<AISummarizer>>(),
                promptService,
                null!, // Kernel (can be null for tests)
                null!  // ITextGenerationService (can be null for tests)
            );
            var processor = new VideoNoteProcessor(loggerMock.Object, aiSummarizer);
            // Act - using a null OpenAI key should result in simulated summary
            var result = await processor.GenerateAiSummaryAsync("Test text");
            // Assert - fallback behavior should return simulated summary
            Assert.AreEqual("[Simulated AI summary]", result);
        }
    }
}
