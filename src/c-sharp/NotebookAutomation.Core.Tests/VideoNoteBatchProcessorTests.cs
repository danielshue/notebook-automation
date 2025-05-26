using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NotebookAutomation.Core.Tools.VideoProcessing;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Unit tests for VideoNoteBatchProcessor extended options.
    /// </summary>
    [TestClass]
    public class VideoNoteBatchProcessorTests
    {        // Add TestContext property for diagnostic logging
        public TestContext TestContext { get; set; }

        private string _testDir;
        private string _outputDir;
        private Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>> _loggerMock;
        // Removed unused fields:
        // private Mock<AISummarizer> _aiSummarizerMock;
        // private Mock<VideoNoteProcessor> _videoNoteProcessorMock;
        private DocumentNoteBatchProcessor<VideoNoteProcessor> _batchProcessor;
        private VideoNoteBatchProcessor _processor; [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _outputDir = Path.Combine(_testDir, "output");
            Directory.CreateDirectory(_outputDir);

            _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>>();

            // Create a proper TestableAISummarizer that can be used for testing
            var testAISummarizer = new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>());

            // Create real VideoNoteProcessor instead of a mock
            var videoNoteProcessor = new VideoNoteProcessor(Mock.Of<ILogger<VideoNoteProcessor>>(), testAISummarizer);

            _batchProcessor = new DocumentNoteBatchProcessor<VideoNoteProcessor>(_loggerMock.Object, videoNoteProcessor, testAISummarizer);
            _processor = new VideoNoteBatchProcessor(_batchProcessor);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [TestMethod]
        public async Task ProcessVideosAsync_NoSummary_DisablesSummaryGeneration()
        {
            // Arrange
            var videoPath = Path.Combine(_testDir, "test.mp4");
            // Create a real MP4 file with some content
            byte[] mp4Header = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 };
            File.WriteAllBytes(videoPath, mp4Header);

            var extensions = new List<string> { ".mp4" };

            TestContext.WriteLine($"Test directory: {_testDir}");
            TestContext.WriteLine($"Output directory: {_outputDir}");
            TestContext.WriteLine($"Video path: {videoPath}");

            // Act
            var result = await _processor.ProcessVideosAsync(
                videoPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: false,
                noSummary: true
            );

            // Assert
            Assert.AreEqual(1, result.Processed, "Expected 1 file to be processed");
            Assert.AreEqual(0, result.Failed, "Expected 0 failures");

            // Check all .md files in the output directory
            var allFiles = Directory.GetFiles(_outputDir, "*.md");
            Assert.IsTrue(allFiles.Length > 0, $"No .md files found in {_outputDir}");

            bool foundExpectedContent = false;
            foreach (var file in allFiles)
            {
                var content = File.ReadAllText(file);
                TestContext.WriteLine($"File: {Path.GetFileName(file)}, Content length: {content.Length}");
                if (content.Contains("[Summary generation disabled by --no-summary flag.]"))
                {
                    foundExpectedContent = true;
                    break;
                }
            }

            Assert.IsTrue(foundExpectedContent, "No file with expected content was found");
        }

        [TestMethod]
        public async Task ProcessVideosAsync_ForceOverwrite_OverwritesExistingNote()
        {
            // Arrange
            var videoPath = Path.Combine(_testDir, "test2.mp4");
            // Create a real MP4 file with some content
            byte[] mp4Header = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 };
            File.WriteAllBytes(videoPath, mp4Header);

            var extensions = new List<string> { ".mp4" };

            // Use the correct file naming convention as used in the code
            var notePath = Path.Combine(_outputDir, "test2-video.md");
            File.WriteAllText(notePath, "old content");

            // Act
            var result = await _processor.ProcessVideosAsync(
                videoPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: false,
                noSummary: true,
                forceOverwrite: true
            );

            // Assert
            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(0, result.Failed);

            // Read the content again after processing
            var noteContent = File.ReadAllText(notePath);
            StringAssert.Contains(noteContent, "[Summary generation disabled by --no-summary flag.]");
        }

        [TestMethod]
        public async Task ProcessVideosAsync_ForceFalse_DoesNotOverwriteExistingNote()
        {
            // Arrange
            var videoPath = Path.Combine(_testDir, "test3.mp4");
            // Create a real MP4 file with some content
            byte[] mp4Header = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 };
            File.WriteAllBytes(videoPath, mp4Header);

            var extensions = new List<string> { ".mp4" };

            // Use the correct file naming convention as used in the code
            var notePath = Path.Combine(_outputDir, "test3-video.md");
            File.WriteAllText(notePath, "old content");

            TestContext.WriteLine($"Created note file: {notePath}");
            TestContext.WriteLine($"Content before test: {File.ReadAllText(notePath)}");

            // Act
            var result = await _processor.ProcessVideosAsync(
                videoPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: false,
                noSummary: true,
                forceOverwrite: false
            );

            // Assert
            // With forceOverwrite: false, no files should be processed since the file exists
            Assert.AreEqual(0, result.Processed);
            Assert.AreEqual(0, result.Failed);

            // Content should remain unchanged
            var noteContent = File.ReadAllText(notePath);
            TestContext.WriteLine($"Content after test: {noteContent}");
            Assert.AreEqual("old content", noteContent);
        }

        [TestMethod]
        public async Task ProcessVideosAsync_RetryFailed_ProcessesOnlyFailedFiles()
        {
            // Arrange
            var videoPath1 = Path.Combine(_testDir, "fail1.mp4");
            var videoPath2 = Path.Combine(_testDir, "fail2.mp4");

            // Create real MP4 files with some content
            byte[] mp4Header = new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 };
            File.WriteAllBytes(videoPath1, mp4Header);
            File.WriteAllBytes(videoPath2, mp4Header);

            var extensions = new List<string> { ".mp4" };
            var failedListPath = Path.Combine(_outputDir, "failed_videos.txt");
            File.WriteAllLines(failedListPath, new[] { videoPath1 });

            TestContext.WriteLine($"Created failed videos file with content: {videoPath1}");

            // Act
            var result = await _processor.ProcessVideosAsync(
                _testDir,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: false,
                noSummary: true,
                forceOverwrite: true,
                retryFailed: true
            );

            // Assert
            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(0, result.Failed);

            // List all files in output directory for debugging
            TestContext.WriteLine("Files in output directory:");
            foreach (var file in Directory.GetFiles(_outputDir, "*.md"))
            {
                TestContext.WriteLine($"  {Path.GetFileName(file)}");
            }

            var fail1NotePath = Path.Combine(_outputDir, "fail1-video.md");
            var fail2NotePath = Path.Combine(_outputDir, "fail2-video.md");

            Assert.IsTrue(File.Exists(fail1NotePath), $"Expected file not found: {fail1NotePath}");
            Assert.IsFalse(File.Exists(fail2NotePath), $"Unexpected file found: {fail2NotePath}");
        }
    }
}
