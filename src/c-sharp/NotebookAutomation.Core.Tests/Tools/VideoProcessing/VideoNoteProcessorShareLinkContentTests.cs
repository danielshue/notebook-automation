using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NotebookAutomation.Core.Tests.Tools.VideoProcessing
{    /// <summary>
     /// Tests for verifying that OneDrive share links appear in markdown content but not in YAML frontmatter.
     /// </summary>
    [TestClass]
    public class VideoNoteProcessorShareLinkContentTests
    {
        private Mock<ILogger<VideoNoteProcessor>> _loggerMock;
        private AISummarizer _aiSummarizer;
        private Mock<IOneDriveService> _oneDriveServiceMock;
        private string _testDir;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<VideoNoteProcessor>>();

            // Create a real AISummarizer with test dependencies
            var mockAiLogger = new LoggerFactory().CreateLogger<AISummarizer>();
            var testPromptService = new TestPromptTemplateService();
            var testTextGenService = new FakeTextGenerationService();
            _aiSummarizer = new AISummarizer(mockAiLogger, testPromptService, null, testTextGenService);

            _oneDriveServiceMock = new Mock<IOneDriveService>();

            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithShareLink_AddsShareLinkToMarkdownContentAndMetadata()
        {
            // Arrange
            string shareLink = "https://onedrive.live.com/view.aspx?cid=test123&page=view&resid=test456&parid=test789";
            _oneDriveServiceMock
            .Setup(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shareLink);

            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                null
            );

            string videoPath = Path.Combine(_testDir, "test-video.mp4");
            File.WriteAllText(videoPath, "fake video content");

            // Act
            var markdown = await processor.GenerateVideoNoteAsync(
                videoPath,
                "test-api-key",
                null, // promptFileName
                false, // noSummary
                null, // timeoutSeconds
                null, // resourcesRoot
                false // noShareLinks - Enable share links
            );

            // Assert
            Assert.IsNotNull(markdown);

            // Verify share link appears in markdown content
            Assert.IsTrue(markdown.Contains("## References"), "Should contain References section");
            Assert.IsTrue(markdown.Contains($"[Video Recording]({shareLink})"), "Should contain share link in References section");

            // Verify share link does NOT appear in YAML frontmatter
            var frontmatterEnd = markdown.IndexOf("---", 4); // Find the closing ---
            if (frontmatterEnd > 0)
            {
                string frontmatter = markdown.Substring(0, frontmatterEnd);
                // Assert that share link is now in the frontmatter metadata as onedrive-shared-link
                Assert.IsTrue(frontmatter.Contains("onedrive-shared-link:"), "Should contain onedrive-shared-link field in metadata");
                Assert.IsTrue(frontmatter.Contains(shareLink), "Share link should appear in YAML frontmatter");
                Assert.IsFalse(frontmatter.Contains("onedrive-sharing-link"), "Should not contain onedrive-sharing-link field in metadata");
                Assert.IsFalse(frontmatter.Contains("share_link"), "Should not contain share_link field in metadata");
            }
        }

        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithNoShareLinks_DoesNotContainReferencesSection()
        {
            // Arrange
            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                null
            );

            string videoPath = Path.Combine(_testDir, "test-video.mp4");
            File.WriteAllText(videoPath, "fake video content");

            // Act
            var markdown = await processor.GenerateVideoNoteAsync(
                videoPath,
                "test-api-key",
                null, // promptFileName
                false, // noSummary
                null, // timeoutSeconds
                null, // resourcesRoot
                true // noShareLinks - Disable share links
            );

            // Assert
            Assert.IsNotNull(markdown);

            // Verify no References section when share links are disabled
            Assert.IsFalse(markdown.Contains("## References"), "Should not contain References section when noShareLinks=true");
            Assert.IsFalse(markdown.Contains("[Video Recording]"), "Should not contain video recording link when noShareLinks=true");            // Verify OneDriveService was not called
            _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithFailedShareLink_DoesNotContainReferencesSection()
        {            // Arrange
            _oneDriveServiceMock
                .Setup(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null); // Simulate failed share link generation

            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                null
            );

            string videoPath = Path.Combine(_testDir, "test-video.mp4");
            File.WriteAllText(videoPath, "fake video content");
            // Act
            var markdown = await processor.GenerateVideoNoteAsync(
                videoPath,
                "test-api-key",
                null, // promptFileName
                false, // noSummary
                null, // timeoutSeconds
                null, // resourcesRoot
                false // noShareLinks - Enable share links, but they will fail
            );

            // Assert
            Assert.IsNotNull(markdown);

            // Verify no References section when share link generation fails
            Assert.IsFalse(markdown.Contains("## References"), "Should not contain References section when share link generation fails");
            Assert.IsFalse(markdown.Contains("[Video Recording]"), "Should not contain video recording link when share link generation fails");            // Verify OneDriveService was called but failed
            _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
