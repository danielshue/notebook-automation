// <copyright file="VideoNoteBatchProcessorOnedriveFullpathRootTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/VideoNoteBatchProcessorOnedriveFullpathRootTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests;

[TestClass]
internal class VideoNoteBatchProcessorOnedriveFullpathRootTests
{
    private string testDir;
    private string outputDir;
    private Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>> loggerMock;

    // Removed unused field:
    // private Mock<AISummarizer> _aiSummarizerMock;
    private Mock<VideoNoteProcessor> videoNoteProcessorMock;
    private DocumentNoteBatchProcessor<VideoNoteProcessor> batchProcessor;
    private VideoNoteBatchProcessor processor;

    private static MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        return new MetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            new AppConfig());
    }

    [TestInitialize]
    public void Setup()
    {
        this.testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        this.outputDir = Path.Combine(this.testDir, "output");
        Directory.CreateDirectory(this.testDir);
        Directory.CreateDirectory(this.outputDir);

        this.loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>>();

        // Create a TestableAISummarizer that can be used in tests
        TestableAISummarizer testAISummarizer = new(Mock.Of<ILogger<AISummarizer>>());

        // Create a mock for IOneDriveService
        IOneDriveService mockOneDriveService = Mock.Of<IOneDriveService>();

        // Create a mock for IYamlHelper
        IYamlHelper mockYamlHelper = Mock.Of<IYamlHelper>();

        // Set up mock with test dependencies
        this.videoNoteProcessorMock = new Mock<VideoNoteProcessor>(
            Mock.Of<ILogger<VideoNoteProcessor>>(),
            testAISummarizer,
            mockYamlHelper,
            CreateMetadataHierarchyDetector(),
            mockOneDriveService,
            null,  // AppConfig
            null); // LoggingService

        // Create a custom batch processor that will directly create a file with the resourcesRoot
        // so we can test that the parameter is being passed correctly
        Mock<DocumentNoteBatchProcessor<VideoNoteProcessor>> mockBatchProcessor = new(
            this.loggerMock.Object,
            this.videoNoteProcessorMock.Object,
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
                string fileName = Path.GetFileNameWithoutExtension(input);
                string outputPath = Path.Combine(output, $"{fileName}.md");
                File.WriteAllText(outputPath, $"Test note with onedriveFullpathRoot: {resourcesRoot ?? "default"}");

                return new BatchProcessResult { Processed = 1, Failed = 0 };
            });

        this.batchProcessor = mockBatchProcessor.Object;
        this.processor = new VideoNoteBatchProcessor(this.batchProcessor);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(this.testDir))
        {
            Directory.Delete(this.testDir, true);
        }
    }

    [TestMethod]
    public async Task ProcessVideosAsync_OnedriveFullpathRoot_OverridesConfigValue()
    {
        // Arrange
        string videoPath = Path.Combine(this.testDir, "test.mp4");
        File.WriteAllText(videoPath, "fake video content");
        List<string> extensions = [".mp4"];
        string customResourcesRoot = Path.Combine(this.testDir, "custom_resources");

        // Act
        BatchProcessResult result = await this.processor.ProcessVideosAsync(
            videoPath,
            this.outputDir,
            extensions,
            openAiApiKey: null,
            dryRun: false,
            noSummary: true,
            forceOverwrite: true,
            retryFailed: false,
            timeoutSeconds: null,
            resourcesRoot: customResourcesRoot)
        .ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, result.Processed);
        Assert.AreEqual(0, result.Failed);
        string notePath = Path.Combine(this.outputDir, "test.md");
        Assert.IsTrue(File.Exists(notePath));
        string noteContent = File.ReadAllText(notePath);
        StringAssert.Contains(noteContent, customResourcesRoot);
    }
}
