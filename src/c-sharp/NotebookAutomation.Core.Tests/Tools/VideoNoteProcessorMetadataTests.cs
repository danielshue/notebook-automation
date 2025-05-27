using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NotebookAutomation.Core.Tests.Tools
{
    [TestClass]
    public class VideoNoteProcessorMetadataTests
    {
        private Mock<ILogger<VideoNoteProcessor>> _loggerMock;
        private TestAISummarizer _aiSummarizer;
        private Mock<IOneDriveService> _oneDriveServiceMock;
        private AppConfig _appConfig;
        private string _testMetadataFile;
        private string _testVaultRoot;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<VideoNoteProcessor>>();
            _aiSummarizer = new TestAISummarizer();
            _oneDriveServiceMock = new Mock<IOneDriveService>();

            // Create test directories and files
            _testVaultRoot = Path.Combine(Path.GetTempPath(), "TestVault");
            Directory.CreateDirectory(_testVaultRoot);

            var programDir = Path.Combine(_testVaultRoot, "MBA Program");
            Directory.CreateDirectory(programDir);

            var vcmDir = Path.Combine(_testVaultRoot, "Value Chain Management");
            var courseDir = Path.Combine(vcmDir, "Supply Chain");
            var classDir = Path.Combine(courseDir, "Class 1");
            Directory.CreateDirectory(classDir);

            // Create program index file
            File.WriteAllText(Path.Combine(programDir, "program-index.md"),
                "---\ntitle: MBA Program\nindex-type: program-index\n---\nProgram content");            // Create metadata.yaml for testing
            _testMetadataFile = Path.Combine(Path.GetTempPath(), "test_metadata.yaml"); string testMetadata = @"---
template-type: video-reference
auto-generated-state: writable
template-description: Template for video reference notes.
title: Video Note
type: note/video-note
author:
program:
course:
class:
module:
lesson:
comprehension: 0
completion-date:
date-created:
date-modified:
status: unwatched
tags:
  - video
  - reference
video-codec:
video-duration:
video-resolution:
video-size:
video-uploaded:";

            File.WriteAllText(_testMetadataFile, testMetadata);

            var mockPaths = new PathsConfig
            {
                NotebookVaultFullpathRoot = _testVaultRoot,
                MetadataFile = _testMetadataFile
            };

            _appConfig = new AppConfig(null, null)
            {
                Paths = mockPaths
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Delete test files and directories
            if (Directory.Exists(_testVaultRoot))
            {
                try
                {
                    Directory.Delete(_testVaultRoot, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            if (File.Exists(_testMetadataFile))
                File.Delete(_testMetadataFile);
        }
        [TestMethod]
        public void GenerateMarkdownNote_WithPathBasedMetadata_AppliesHierarchyDetection()
        {
            // Arrange
            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                _appConfig
            );

            string videoPath = Path.Combine(_testVaultRoot, "Value Chain Management", "Supply Chain", "Class 1", "lesson.mp4");
            // Create test video file
            Directory.CreateDirectory(Path.GetDirectoryName(videoPath));
            File.WriteAllText(videoPath, "fake video content");

            var metadata = new Dictionary<string, object>
            {
                { "title", "Test Video" },
                { "_internal_path", videoPath }
            };

            string bodyText = "This is a test summary.";

            // Act
            string markdownNote = processor.GenerateMarkdownNote(bodyText, metadata, "Video Note");

            // Assert
            Assert.IsTrue(markdownNote.Contains("program: Value Chain Management"));
            Assert.IsTrue(markdownNote.Contains("course: Supply Chain"));
            Assert.IsTrue(markdownNote.Contains("class: Class 1"));
        }

        [TestMethod]
        public void GenerateMarkdownNote_WithTemplate_AppliesTemplateMetadata()
        {
            // Arrange
            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                _appConfig
            ); var metadata = new Dictionary<string, object>
            {
                { "title", "Test Video" },
                { "_internal_path", "c:/path/to/video.mp4" }
            };

            string bodyText = "This is a test summary.";

            // Act
            string markdownNote = processor.GenerateMarkdownNote(bodyText, metadata, "Video Note");
            System.Diagnostics.Debug.WriteLine("[DEBUG] Markdown output (WithTemplate):\n" + markdownNote);
            // Assert YAML frontmatter contains required fields
            Assert.IsTrue(markdownNote.Contains("type: video-reference"), "Missing type: video-reference");
            Assert.IsTrue(markdownNote.Contains("template-type: video-reference"), "Missing template-type: video-reference");
            // Assert that the note body is present (not suppressed)
            Assert.IsTrue(markdownNote.Contains("This is a test summary."), "Missing summary body");
        }

        [TestMethod]
        public void GenerateMarkdownNote_WithTemplateAndHierarchy_AppliesBoth()
        {
            // Arrange
            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                _appConfig
            );

            string videoPath = Path.Combine(_testVaultRoot, "Value Chain Management", "Supply Chain", "Class 1", "lesson.mp4");
            // Create test video file
            Directory.CreateDirectory(Path.GetDirectoryName(videoPath));
            File.WriteAllText(videoPath, "fake video content"); var metadata = new Dictionary<string, object>
            {
                { "title", "Test Video" },
                { "_internal_path", videoPath }
            };

            string bodyText = "This is a test summary.";

            // Act
            string markdownNote = processor.GenerateMarkdownNote(bodyText, metadata, "Video Note");
            System.Diagnostics.Debug.WriteLine("[DEBUG] Markdown output (WithTemplateAndHierarchy):\n" + markdownNote);
            // Assert hierarchy metadata
            Assert.IsTrue(markdownNote.Contains("program: Value Chain Management"), "Missing program");
            Assert.IsTrue(markdownNote.Contains("course: Supply Chain"), "Missing course");
            Assert.IsTrue(markdownNote.Contains("class: Class 1"), "Missing class");
            // Assert template metadata
            Assert.IsTrue(markdownNote.Contains("type: video-reference"), "Missing type: video-reference");
            Assert.IsTrue(markdownNote.Contains("template-type: video-reference"), "Missing template-type: video-reference");
            // Assert that the note body is present (not suppressed)
            Assert.IsTrue(markdownNote.Contains("This is a test summary."), "Missing summary body");
        }

        [TestMethod]
        public async Task ProcessVideoAsync_AppliesPathBasedMetadataAndTemplate()
        {
            // Arrange
            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                _appConfig
            );

            string videoPath = Path.Combine(_testVaultRoot, "Value Chain Management", "Supply Chain", "Class 1", "lesson.mp4");
            // Create test video file
            Directory.CreateDirectory(Path.GetDirectoryName(videoPath));
            File.WriteAllText(videoPath, "fake video content");

            // Act
            var markdown = await processor.GenerateVideoNoteAsync(
                videoPath,
                openAiApiKey: "test-api-key"
            );
            System.Diagnostics.Debug.WriteLine("[DEBUG] Markdown output (ProcessVideoAsync):\n" + markdown);
            // Assert
            Assert.IsNotNull(markdown);
            // Check that markdown contains both hierarchy info and template data
            Assert.IsTrue(markdown.Contains("program: Value Chain Management"), "Missing program");
            Assert.IsTrue(markdown.Contains("course: Supply Chain"), "Missing course");
            Assert.IsTrue(markdown.Contains("class: Class 1"), "Missing class");
            Assert.IsTrue(markdown.Contains("type: video-reference"), "Missing type: video-reference");
            Assert.IsTrue(markdown.Contains("template-type: video-reference"), "Missing template-type: video-reference");
            // Assert that the note body is present (not suppressed)
            Assert.IsTrue(markdown.Contains("AI summary of the video content"), "Missing summary body");

        }

        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithNoSummary_SuppressesBodyOutputsOnlyFrontmatter()
        {
            // Arrange
            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                _appConfig
            );

            string videoPath = Path.Combine(_testVaultRoot, "Value Chain Management", "Supply Chain", "Class 1", "lesson.mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(videoPath));
            File.WriteAllText(videoPath, "fake video content");

            // Act
            var markdown = await processor.GenerateVideoNoteAsync(
                videoPath,
                openAiApiKey: "test-api-key",
                promptFileName: null,
                noSummary: true
            );
            System.Diagnostics.Debug.WriteLine("[DEBUG] Markdown output (NoSummary):\n" + markdown);
            // Assert
            Assert.IsNotNull(markdown);
            // Should contain YAML frontmatter fields
            Assert.IsTrue(markdown.Contains("program: Value Chain Management"), "Missing program");
            Assert.IsTrue(markdown.Contains("course: Supply Chain"), "Missing course");
            Assert.IsTrue(markdown.Contains("class: Class 1"), "Missing class");
            Assert.IsTrue(markdown.Contains("type: video-reference"), "Missing type: video-reference");
            Assert.IsTrue(markdown.Contains("template-type: video-reference"), "Missing template-type: video-reference");            // Should NOT contain any AI summary text
            Assert.IsFalse(markdown.Contains("AI summary of the video content"), "Should not contain summary");
            Assert.IsFalse(markdown.Contains("This is a test summary."), "Should not contain test summary");
            // Should contain basic structure but no AI summary
            Assert.IsTrue(markdown.Contains("# Video Note"), "Should contain main heading");
            Assert.IsTrue(markdown.Contains("## Note"), "Should contain Note section");
            // Should not end with just frontmatter, should have minimal body
            Assert.IsFalse(markdown.EndsWith("---\n\n"), "Should have body content after frontmatter");
        }

        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithMockedShareLink_AddsReferencesSection()
        {
            // Arrange
            string testShareLink = "https://onedrive.live.com/view.aspx?test=example";            _oneDriveServiceMock
                .Setup(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(testShareLink);

            var processor = new VideoNoteProcessor(
                _loggerMock.Object,
                _aiSummarizer,
                _oneDriveServiceMock.Object,
                _appConfig
            );

            string videoPath = Path.Combine(_testVaultRoot, "test-video.mp4");
            Directory.CreateDirectory(Path.GetDirectoryName(videoPath));
            File.WriteAllText(videoPath, "fake video content");

            // Act
            var markdown = await processor.GenerateVideoNoteAsync(
                videoPath,
                "test-api-key",
                null,
                false,
                null,
                null,
                false
            );

            // Assert
            Assert.IsNotNull(markdown);

            // Verify share link appears in markdown content
            Assert.IsTrue(markdown.Contains("## References"), "Should contain References section");
            Assert.IsTrue(markdown.Contains($"[Video Recording]({testShareLink})"), "Should contain share link in References section");

            // Verify share link does NOT appear in YAML frontmatter
            var frontmatterEnd = markdown.IndexOf("---", 4);
            if (frontmatterEnd > 0)
            {
                string frontmatter = markdown.Substring(0, frontmatterEnd);
                Assert.IsFalse(frontmatter.Contains(testShareLink), "Share link should NOT appear in YAML frontmatter");
                Assert.IsFalse(frontmatter.Contains("onedrive-sharing-link"), "Should not contain onedrive-sharing-link field in metadata");
                Assert.IsFalse(frontmatter.Contains("share_link"), "Should not contain share_link field in metadata");
            }
        }
    }    /// <summary>
    /// Minimal test double for AISummarizer for use in VideoNoteProcessor tests.
    /// </summary>
    internal class TestAISummarizer : AISummarizer
    {        public TestAISummarizer() : base(null, null, null, null) { }
        public override Task<string> SummarizeAsync(string inputText, string prompt = null, string promptFileName = null, System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult("This is an AI summary of the video content.");
        }
    }
}
