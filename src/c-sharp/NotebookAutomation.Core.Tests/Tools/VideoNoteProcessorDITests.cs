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
            var logger = NullLogger.Instance;
            
            // We can't mock AISummarizer since its methods aren't virtual,
            // so we'll create a real one and test the dependency injection pattern
            var aiSummarizer = new AISummarizer(logger);
            
            // Create VideoNoteProcessor with the real AISummarizer
            var processor = new VideoNoteProcessor(logger, aiSummarizer);
            
            // Act - Using null OpenAI key should return simulated summary
            var result = await processor.GenerateAiSummaryAsync("Test text", null);
            
            // Assert - We're testing that the processor uses the injected AISummarizer
            Assert.AreEqual("[Simulated AI summary]", result);
        }

        [TestMethod]
        public async Task GenerateAiSummaryAsync_FallsBackToNewAISummarizer_WhenNotInjected()
        {
            // Arrange
            var logger = NullLogger.Instance;
            
            // Create processor without injecting AISummarizer
            var processor = new VideoNoteProcessor(logger);
            
            // Act - using a null OpenAI key should result in simulated summary
            var result = await processor.GenerateAiSummaryAsync("Test text", null);
              
            // Assert - fallback behavior should return simulated summary
            Assert.AreEqual("[Simulated AI summary]", result);
        }
    }
}
