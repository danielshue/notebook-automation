using NotebookAutomation.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Tools.Shared;

namespace NotebookAutomation.Core.Tests
{
    [TestClass]
    public class VideoNoteBatchProcessorResourcesRootTests
    {
        private string _testDir;
        private string _outputDir;
        private Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>> _loggerMock;
        private Mock<AISummarizer> _aiSummarizerMock;
        private Mock<VideoNoteProcessor> _videoNoteProcessorMock;
        private DocumentNoteBatchProcessor<VideoNoteProcessor> _batchProcessor;
        private VideoNoteBatchProcessor _processor;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _outputDir = Path.Combine(_testDir, "output");
            Directory.CreateDirectory(_outputDir);
            _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>>();
            _aiSummarizerMock = new Mock<AISummarizer>(MockBehavior.Loose, Mock.Of<ILogger<AISummarizer>>());
            _videoNoteProcessorMock = new Mock<VideoNoteProcessor>(MockBehavior.Loose, Mock.Of<ILogger<VideoNoteProcessor>>(), _aiSummarizerMock.Object);
            _batchProcessor = new DocumentNoteBatchProcessor<VideoNoteProcessor>(_loggerMock.Object, _videoNoteProcessorMock.Object, null);
            _processor = new VideoNoteBatchProcessor(_batchProcessor);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [TestMethod]
        public async Task ProcessVideosAsync_ResourcesRoot_OverridesConfigValue()
        {
            // Arrange
            var videoPath = Path.Combine(_testDir, "test.mp4");
            File.WriteAllText(videoPath, "fake video content");
            var extensions = new List<string> { ".mp4" };
            string customResourcesRoot = Path.Combine(_testDir, "custom_resources");

            // Act
            var result = await _processor.ProcessVideosAsync(
                videoPath,
                _outputDir,
                extensions,
                openAiApiKey: null,
                dryRun: false,
                noSummary: true,
                forceOverwrite: true,
                retryFailed: false,
                timeoutSeconds: null,
                resourcesRoot: customResourcesRoot
            );

            // Assert
            Assert.AreEqual(1, result.Processed);
            Assert.AreEqual(0, result.Failed);
            var notePath = Path.Combine(_outputDir, "test.md");
            Assert.IsTrue(File.Exists(notePath));
            var noteContent = File.ReadAllText(notePath);
            StringAssert.Contains(noteContent, customResourcesRoot);
        }
    }

}
