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
            var loggerMock = new Mock<ILogger>();
            var aiSummarizerMock = new Mock<AISummarizer>(MockBehavior.Strict, loggerMock.Object);

            // Setup the mock to return a specific value when SummarizeAsync is called
            aiSummarizerMock
                .Setup(s => s.SummarizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .ReturnsAsync("Test summary from injected AISummarizer");
            
            // Create VideoNoteProcessor with the mock AISummarizer
            var processor = new VideoNoteProcessor(loggerMock.Object, aiSummarizerMock.Object);
            
            // Act
            var result = await processor.GenerateAiSummaryAsync("Test text", "dummy-api-key");
              // Assert
            Assert.AreEqual("Test summary from injected AISummarizer", result);
            
            // Verify that the mock's SummarizeAsync was called
            aiSummarizerMock.Verify(s => s.SummarizeAsync(
                "Test text", 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                default), 
                Times.Once);
        }

        [TestMethod]
        public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            
            // Create processor without injecting AISummarizer
            var processor = new VideoNoteProcessor(loggerMock.Object);
            
            // Act - using a null OpenAI key should result in simulated summary
            var result = await processor.GenerateAiSummaryAsync("Test text", null);
              // Assert - fallback behavior should return simulated summary
            Assert.AreEqual("[Simulated AI summary]", result);
        }
    }
}
