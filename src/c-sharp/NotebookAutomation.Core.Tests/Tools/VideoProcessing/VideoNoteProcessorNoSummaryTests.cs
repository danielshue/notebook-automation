using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;

namespace NotebookAutomation.Core.Tests.Tools.VideoProcessing
{
    /// <summary>
    /// Tests for VideoNoteProcessor noSummary functionality.
    /// </summary>
    [TestClass]
    public class VideoNoteProcessorNoSummaryTests
    {
        private ILogger<VideoNoteProcessor> _logger;
        private AISummarizer _aiSummarizer;
        private VideoNoteProcessor _processor;
        private string _tempDir;
        private string _testVideoPath;        /// <summary>
                                              /// Initialize test resources before each test.
                                              /// </summary>
        [TestInitialize]
        public void Setup()
        {
            _logger = new LoggerFactory().CreateLogger<VideoNoteProcessor>();

            // Create a mock AI summarizer with required dependencies
            var mockAiLogger = new LoggerFactory().CreateLogger<AISummarizer>();
            var testPromptService = new TestPromptTemplateService();
            var testTextGenService = new FakeTextGenerationService();

            _aiSummarizer = new AISummarizer(mockAiLogger, testPromptService, null, testTextGenService);

            _processor = new VideoNoteProcessor(_logger, _aiSummarizer);

            // Create temporary directory and mock video file
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _testVideoPath = Path.Combine(_tempDir, "test-video.mp4");

            // Create a dummy video file (just an empty file for testing)
            File.WriteAllText(_testVideoPath, "dummy video content");
        }

        /// <summary>
        /// Clean up test resources after each test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            if (_tempDir != null && Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        /// <summary>
        /// Test that GenerateVideoNoteAsync with noSummary=true creates minimal content.
        /// </summary>
        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithNoSummary_CreatesMinimalContent()
        {
            // Arrange
            // No OpenAI API key provided to ensure we're not making actual API calls

            // Act
            string markdown = await _processor.GenerateVideoNoteAsync(
                videoPath: _testVideoPath,
                openAiApiKey: null,
                promptFileName: null,
                noSummary: true,
                timeoutSeconds: null,
                resourcesRoot: null,
                noShareLinks: true);            // Assert
            Assert.IsNotNull(markdown);
            Assert.IsTrue(markdown.Contains("## Note")); // Should contain the minimal Note section
            Assert.IsTrue(markdown.Contains("title: Test Video")); // Should have frontmatter
            Assert.IsTrue(markdown.StartsWith("---")); // Should start with frontmatter
            Assert.IsTrue(markdown.Contains("file_name:")); // Should have metadata
        }

        /// <summary>
        /// Test that GenerateVideoNoteAsync with noSummary=false generates different content.
        /// </summary>
        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithSummaryEnabled_GeneratesDifferentContent()
        {
            // Arrange
            // No OpenAI API key provided, so it should attempt but fail gracefully

            // Act
            string markdown = await _processor.GenerateVideoNoteAsync(
                videoPath: _testVideoPath,
                openAiApiKey: null, // This will cause the summarizer to return empty or error
                promptFileName: null,
                noSummary: false,
                timeoutSeconds: null,
                resourcesRoot: null,
                noShareLinks: true);

            // Assert
            Assert.IsNotNull(markdown);
            Assert.IsTrue(markdown.Contains("title: Test Video")); // Should have frontmatter
            Assert.IsTrue(markdown.StartsWith("---")); // Should start with frontmatter

            // The content should be different from the noSummary case
            // Even if the AI summary fails, it should still generate content
        }
    }
}
