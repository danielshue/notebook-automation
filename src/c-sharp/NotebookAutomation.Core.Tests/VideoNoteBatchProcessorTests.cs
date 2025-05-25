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
    {
        private string _testDir;
        private string _outputDir;
        private Mock<ILogger<DocumentNoteBatchProcessor<NotebookAutomation.Core.Tools.VideoProcessing.VideoNoteProcessor>>> _loggerMock;
        private Mock<AISummarizer> _aiSummarizerMock;
        private Mock<NotebookAutomation.Core.Tools.VideoProcessing.VideoNoteProcessor> _videoNoteProcessorMock;
        private DocumentNoteBatchProcessor<NotebookAutomation.Core.Tools.VideoProcessing.VideoNoteProcessor> _batchProcessor;
        private VideoNoteBatchProcessor _processor;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _outputDir = Path.Combine(_testDir, "output");
            Directory.CreateDirectory(_outputDir);
            _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<NotebookAutomation.Core.Tools.VideoProcessing.VideoNoteProcessor>>>();
            _aiSummarizerMock = new Mock<AISummarizer>(MockBehavior.Loose, Mock.Of<ILogger<AISummarizer>>());
            _videoNoteProcessorMock = new Mock<NotebookAutomation.Core.Tools.VideoProcessing.VideoNoteProcessor>(MockBehavior.Loose, Mock.Of<ILogger<NotebookAutomation.Core.Tools.VideoProcessing.VideoNoteProcessor>>(), _aiSummarizerMock.Object);
            _batchProcessor = new DocumentNoteBatchProcessor<NotebookAutomation.Core.Tools.VideoProcessing.VideoNoteProcessor>(_loggerMock.Object, _videoNoteProcessorMock.Object, null);
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
            File.WriteAllText(videoPath, "fake video content");
            var extensions = new List<string> { ".mp4" };

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
            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(0, result.Failed);
            var notePath = Path.Combine(_outputDir, "test.md");
            Assert.IsTrue(File.Exists(notePath));
            var noteContent = File.ReadAllText(notePath);
            StringAssert.Contains(noteContent, "Summary generation disabled");
        }

        [TestMethod]
        public async Task ProcessVideosAsync_ForceOverwrite_OverwritesExistingNote()
        {
            // Arrange
            var videoPath = Path.Combine(_testDir, "test2.mp4");
            File.WriteAllText(videoPath, "fake video content");
            var extensions = new List<string> { ".mp4" };
            var notePath = Path.Combine(_outputDir, "test2.md");
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
            var noteContent = File.ReadAllText(notePath);
            StringAssert.Contains(noteContent, "Summary generation disabled");
        }

        [TestMethod]
        public async Task ProcessVideosAsync_ForceFalse_DoesNotOverwriteExistingNote()
        {
            // Arrange
            var videoPath = Path.Combine(_testDir, "test3.mp4");
            File.WriteAllText(videoPath, "fake video content");
            var extensions = new List<string> { ".mp4" };
            var notePath = Path.Combine(_outputDir, "test3.md");
            File.WriteAllText(notePath, "old content");

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
            Assert.AreEqual(0, result.Processed);
            Assert.AreEqual(0, result.Failed);
            var noteContent = File.ReadAllText(notePath);
            Assert.AreEqual("old content", noteContent);
        }

        [TestMethod]
        public async Task ProcessVideosAsync_RetryFailed_ProcessesOnlyFailedFiles()
        {
            // Arrange
            var videoPath1 = Path.Combine(_testDir, "fail1.mp4");
            var videoPath2 = Path.Combine(_testDir, "fail2.mp4");
            File.WriteAllText(videoPath1, "fake video content");
            File.WriteAllText(videoPath2, "fake video content");
            var extensions = new List<string> { ".mp4" };
            var failedListPath = Path.Combine(_outputDir, "failed_videos.txt");
            File.WriteAllLines(failedListPath, new[] { videoPath1 });

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
            Assert.IsTrue(File.Exists(Path.Combine(_outputDir, "fail1.md")));
            Assert.IsFalse(File.Exists(Path.Combine(_outputDir, "fail2.md")));
        }
    }
}
