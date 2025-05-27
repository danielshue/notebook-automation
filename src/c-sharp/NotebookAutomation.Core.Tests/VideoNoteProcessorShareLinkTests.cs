#nullable enable

using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Tests for the VideoNoteProcessor share link functionality.
    /// These tests validate the integration with OneDriveService for creating shareable links.
    /// </summary>
    [TestClass]
    public class VideoNoteProcessorShareLinkTests
    {
        private string _tempDirectory = string.Empty;
        private ILogger<VideoNoteProcessor> _logger = NullLogger<VideoNoteProcessor>.Instance;
        private VideoNoteProcessor _processor = null!;
        private Mock<IOneDriveService> _mockOneDriveService = null!;
        private AISummarizer _aiSummarizer = null!;

        /// <summary>
        /// Sets up the test environment before each test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), $"VideoProcessorShareLinkTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDirectory);

            _logger = NullLogger<VideoNoteProcessor>.Instance;

            var promptService = new PromptTemplateService(
                NullLogger<PromptTemplateService>.Instance,
                new Configuration.AppConfig());

            _aiSummarizer = new AISummarizer(
                NullLogger<AISummarizer>.Instance,
                promptService,
                null!, // Kernel (can be null for tests)
                null!  // ITextGenerationService (can be null for tests)
            );

            // Mock IOneDriveService
            _mockOneDriveService = new Mock<IOneDriveService>();            // Initialize processor with mocked OneDriveService
            _processor = new VideoNoteProcessor(_logger, _aiSummarizer, _mockOneDriveService.Object, null);
        }

        /// <summary>
        /// Cleans up the test environment after each test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Error during cleanup: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Creates a mock video file in the specified directory.
        /// </summary>
        /// <param name="directory">The directory where the file will be created.</param>
        /// <param name="fileName">The name of the video file.</param>
        /// <returns>The full path to the created file.</returns>
        private string CreateTestVideoFile(string directory, string fileName = "test_video.mp4")
        {
            string filePath = Path.Combine(directory, fileName);
            File.WriteAllText(filePath, "Mock video file content");
            return filePath;
        }

        /// <summary>
        /// Tests that a share link is added to the metadata when OneDriveService is available and noShareLinks is false.
        /// </summary>
        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithShareLink_AddsLinkToOutput()
        {
            // Arrange
            string videoPath = CreateTestVideoFile(_tempDirectory);
            string expectedShareLink = "https://example.onedrive.com/sharelink";

            // Setup mock to return a share link
            _mockOneDriveService
                .Setup(m => m.CreateShareLinkAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(expectedShareLink);

            // Act
            string markdown = await _processor.GenerateVideoNoteAsync(
                videoPath,
                "mock-api-key",
                null,              // promptFileName
                true,              // noSummary
                null,              // timeoutSeconds
                null,              // resourcesRoot
                false              // noShareLinks - enable share links
            );

            // Assert
            Assert.IsTrue(markdown.Contains("## Share Link"));
            Assert.IsTrue(markdown.Contains(expectedShareLink));
            Assert.IsTrue(markdown.Contains("Watch test_video.mp4 on OneDrive"));

            // Verify OneDriveService was called
            _mockOneDriveService.Verify(
                m => m.CreateShareLinkAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Tests that no share link is added when noShareLinks is true.
        /// </summary>
        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithNoShareLinks_DoesNotAddLink()
        {
            // Arrange
            string videoPath = CreateTestVideoFile(_tempDirectory);

            // Act
            string markdown = await _processor.GenerateVideoNoteAsync(
                videoPath,
                "mock-api-key",
                null,              // promptFileName
                true,              // noSummary
                null,              // timeoutSeconds
                null,              // resourcesRoot
                true               // noShareLinks - disable share links
            );

            // Assert
            Assert.IsFalse(markdown.Contains("## Share Link"));

            // Verify OneDriveService was not called
            _mockOneDriveService.Verify(
                m => m.CreateShareLinkAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Tests that the processor handles null OneDriveService correctly.
        /// </summary>
        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithNullOneDriveService_DoesNotAddLink()
        {
            // Arrange
            string videoPath = CreateTestVideoFile(_tempDirectory);            // Create processor with null OneDriveService
            var processorWithoutOneDrive = new VideoNoteProcessor(_logger, _aiSummarizer, null, null);

            // Act
            string markdown = await processorWithoutOneDrive.GenerateVideoNoteAsync(
                videoPath,
                "mock-api-key",
                null,              // promptFileName
                true,              // noSummary
                null,              // timeoutSeconds
                null,              // resourcesRoot
                false              // noShareLinks - enable share links, but should be ignored due to null service
            );

            // Assert
            Assert.IsFalse(markdown.Contains("## Share Link"));
        }

        /// <summary>
        /// Tests that the processor handles OneDriveService exceptions gracefully.
        /// </summary>
        [TestMethod]
        public async Task GenerateVideoNoteAsync_WithOneDriveException_HandlesGracefully()
        {
            // Arrange
            string videoPath = CreateTestVideoFile(_tempDirectory);

            // Setup mock to throw an exception
            _mockOneDriveService
                .Setup(m => m.CreateShareLinkAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                .ThrowsAsync(new Exception("Test OneDrive exception"));

            // Act - Should not throw exception
            string markdown = await _processor.GenerateVideoNoteAsync(
                videoPath,
                "mock-api-key",
                null,              // promptFileName
                true,              // noSummary
                null,              // timeoutSeconds
                null,              // resourcesRoot
                false              // noShareLinks - enable share links
            );

            // Assert - Shouldn't have a share link section
            Assert.IsFalse(markdown.Contains("## Share Link"));

            // Verify OneDriveService was called
            _mockOneDriveService.Verify(
                m => m.CreateShareLinkAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>()),
                Times.Once);
        }
    }
}
