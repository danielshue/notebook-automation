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
public class VideoNoteBatchProcessorOnedriveFullpathRootTests
{
    private string testDir;
    private string outputDir;
    private string _testMetadataFile;
    private AppConfig _testAppConfig;
    private Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>> loggerMock;

    // Removed unused field:
    // private Mock<AISummarizer> _aiSummarizerMock;
    private Mock<VideoNoteProcessor> videoNoteProcessorMock;
    private DocumentNoteBatchProcessor<VideoNoteProcessor> batchProcessor;
    private VideoNoteBatchProcessor processor;

    private MetadataTemplateManager CreateTestMetadataTemplateManager()
    {
        return new MetadataTemplateManager(
            NullLogger<MetadataTemplateManager>.Instance,
            _testAppConfig,
            Mock.Of<IYamlHelper>());
    }
    private static MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        return new MetadataHierarchyDetector(
            NullLogger<MetadataHierarchyDetector>.Instance,
            new AppConfig
            {
                Paths = new PathsConfig
                {
                    MetadataFile = Path.Combine(Path.GetTempPath(), "temp_metadata.yaml")
                }
            })
        { Logger = NullLogger<MetadataHierarchyDetector>.Instance };
    }
    [TestInitialize]
    public void Setup()
    {
        testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        outputDir = Path.Combine(testDir, "output");
        Directory.CreateDirectory(testDir);
        Directory.CreateDirectory(outputDir);

        // Create temp metadata file
        _testMetadataFile = Path.Combine(Path.GetTempPath(), "test_metadata_batchprocess.yaml");
        var testMetadata = @"
---
template-type: ""video-note""
tags:
  - video
metadata:
  type: ""Video Note""
---";
        File.WriteAllText(_testMetadataFile, testMetadata);

        // Create test AppConfig
        _testAppConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = testDir,
                MetadataFile = _testMetadataFile,
                LoggingDir = Path.GetTempPath()
            }
        };

        loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>>();

        // Create a TestableAISummarizer that can be used in tests
        TestableAISummarizer testAISummarizer = new(Mock.Of<ILogger<AISummarizer>>());

        // Create a mock for IOneDriveService
        IOneDriveService mockOneDriveService = Mock.Of<IOneDriveService>();

        // Create a mock for IYamlHelper
        IYamlHelper mockYamlHelper = Mock.Of<IYamlHelper>();        // Set up mock with test dependencies
        videoNoteProcessorMock = new Mock<VideoNoteProcessor>(
            Mock.Of<ILogger<VideoNoteProcessor>>(),
            testAISummarizer,
            mockYamlHelper,
            CreateMetadataHierarchyDetector(),
            CreateTestMetadataTemplateManager(),
            mockOneDriveService,
            null);  // AppConfig

        // Create a custom batch processor that will directly create a file with the resourcesRoot
        // so we can test that the parameter is being passed correctly
        Mock<DocumentNoteBatchProcessor<VideoNoteProcessor>> mockBatchProcessor = new(
            loggerMock.Object,
            videoNoteProcessorMock.Object,
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

        batchProcessor = mockBatchProcessor.Object;
        processor = new VideoNoteBatchProcessor(batchProcessor);
    }
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, true);
        }

        if (File.Exists(_testMetadataFile))
        {
            File.Delete(_testMetadataFile);
        }
    }

    [TestMethod]
    public async Task ProcessVideosAsync_OnedriveFullpathRoot_OverridesConfigValue()
    {
        // Arrange
        string videoPath = Path.Combine(testDir, "test.mp4");
        File.WriteAllText(videoPath, "fake video content");
        List<string> extensions = [".mp4"];
        string customResourcesRoot = Path.Combine(testDir, "custom_resources");

        // Act
        BatchProcessResult result = await processor.ProcessVideosAsync(
            videoPath,
            outputDir,
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
        string notePath = Path.Combine(outputDir, "test.md");
        Assert.IsTrue(File.Exists(notePath));
        string noteContent = File.ReadAllText(notePath);
        StringAssert.Contains(noteContent, customResourcesRoot);
    }
}

