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
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tests
{
    [TestClass]
    public class VideoNoteBatchProcessorResourcesRootTests
    {
        private string _testDir;
        private string _outputDir;
        private Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>> _loggerMock;
        // Removed unused field:
        // private Mock<AISummarizer> _aiSummarizerMock;
        private Mock<VideoNoteProcessor> _videoNoteProcessorMock;
        private DocumentNoteBatchProcessor<VideoNoteProcessor> _batchProcessor;
        private VideoNoteBatchProcessor _processor; [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _outputDir = Path.Combine(_testDir, "output");
            Directory.CreateDirectory(_outputDir);

            _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>>();

            // Create a TestableAISummarizer that can be used in tests
            var testAISummarizer = new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>());            // Create a mock for IOneDriveService
            var mockOneDriveService = Mock.Of<IOneDriveService>();            // Set up mock with test dependencies (add the optional AppConfig parameter)
            _videoNoteProcessorMock = new Mock<VideoNoteProcessor>(
                MockBehavior.Loose,
                Mock.Of<ILogger<VideoNoteProcessor>>(),
                testAISummarizer,
                mockOneDriveService,
                null as AppConfig);

            // Create a custom batch processor that will directly create a file with the resourcesRoot
            // so we can test that the parameter is being passed correctly
            var mockBatchProcessor = new Mock<DocumentNoteBatchProcessor<VideoNoteProcessor>>(
                _loggerMock.Object,
                _videoNoteProcessorMock.Object,
                testAISummarizer);

            mockBatchProcessor
                .Setup(b => b.ProcessDocumentsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<AppConfig>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync((
                    string input,
                    string output,
                    List<string> extensions,
                    string openAiApiKey,
                    bool dryRun,
                    bool noSummary,
                    bool forceOverwrite,
                    bool retryFailed,
                    int? timeoutSeconds,
                    string resourcesRoot,
                    AppConfig appConfig,
                    string noteType,
                    string failedFilesListName,
                    bool noShareLinks) =>
                {
                    // Write a file with resourcesRoot in its content for testing
                    var fileName = Path.GetFileNameWithoutExtension(input);
                    var outputPath = Path.Combine(output, $"{fileName}.md");
                    File.WriteAllText(outputPath, $"Test note with onedriveFullpathRoot: {resourcesRoot ?? "default"}");

                    return new BatchProcessResult { Processed = 1, Failed = 0 };
                });

            _batchProcessor = mockBatchProcessor.Object;
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
