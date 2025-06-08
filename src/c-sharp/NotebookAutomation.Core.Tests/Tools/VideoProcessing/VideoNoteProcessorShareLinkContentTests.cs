// <copyright file="VideoNoteProcessorShareLinkContentTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Tools/VideoProcessing/VideoNoteProcessorShareLinkContentTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests.Tools.VideoProcessing;

/// <summary>
/// Tests for verifying that OneDrive share links appear in markdown content but not in YAML frontmatter.
/// </summary>
[TestClass]
public class VideoNoteProcessorShareLinkContentTests
{
    private Mock<ILogger<VideoNoteProcessor>> loggerMock;
    private AISummarizer aiSummarizer;
    private Mock<IOneDriveService> oneDriveServiceMock;
    private Mock<IYamlHelper> yamlHelperMock;
    private string testDir;
    private string _testMetadataFile;
    private AppConfig _testAppConfig;

    [TestInitialize]
    public void Setup()
    {
        loggerMock = new Mock<ILogger<VideoNoteProcessor>>();

        // Create temp metadata file
        _testMetadataFile = Path.Combine(Path.GetTempPath(), "test_metadata_sharelink.yaml");
        var testMetadata = @"
---
template-type: ""video-note""
tags:
  - video
metadata:
  type: ""Video Note""
---";
        File.WriteAllText(_testMetadataFile, testMetadata);

        // Create test directory
        testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        // Create shared AppConfig
        _testAppConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = testDir,
                MetadataFile = _testMetadataFile,
                LoggingDir = Path.GetTempPath()
            }
        };

        // Create a real AISummarizer with test dependencies
        ILogger<AISummarizer> mockAiLogger = new LoggerFactory().CreateLogger<AISummarizer>();
        TestPromptTemplateService testPromptService = new();
        Microsoft.SemanticKernel.Kernel kernel = MockKernelFactory.CreateKernelWithMockService("Test summary");
        aiSummarizer = new AISummarizer(mockAiLogger, testPromptService, kernel);
        oneDriveServiceMock = new Mock<IOneDriveService>();
        oneDriveServiceMock.Setup(s => s.GetShareLinkAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/share-link");
        yamlHelperMock = new Mock<IYamlHelper>();

        // Setup YamlHelper mock
        yamlHelperMock.Setup(m => m.RemoveFrontmatter(It.IsAny<string>()))
            .Returns<string>(markdown => markdown.Contains("---") ? markdown.Substring(markdown.IndexOf("---", 3) + 3) : markdown);

        yamlHelperMock.Setup(m => m.ParseYamlToDictionary(It.IsAny<string>()))
            .Returns(new Dictionary<string, object>
            {
                { "template-type", "video-reference" },
                { "type", "video-reference" },
                { "title", "Test Video" },
                { "tags", new[] { "video", "reference" } },            });

        yamlHelperMock.Setup(m => m.ExtractFrontmatter(It.IsAny<string>()))
            .Returns("template-type: video-reference\ntitle: Test Video");

        yamlHelperMock.Setup(m => m.SerializeToYaml(It.IsAny<Dictionary<string, object>>()))
            .Returns("---\ntemplate-type: video-reference\ntitle: Test Video\n---\n");

        // Setup the mock for OneDrive service to return share links
        oneDriveServiceMock.Setup(m => m.GetShareLinkAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"webUrl\": \"https://example.com/share-link\"}");

        oneDriveServiceMock.Setup(m => m.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/share-link");
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
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
        catch
        {
            // Ignore cleanup errors
        }
    }

    [TestMethod]
    public async Task GenerateVideoNoteAsync_WithShareLink_AddsShareLinkToMarkdownContentAndMetadata()
    {
        // Arrange
        string shareLink = "https://onedrive.live.com/view.aspx?cid=test123&page=view&resid=test456&parid=test789"; oneDriveServiceMock
        .Setup(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(shareLink);

        VideoNoteProcessor processor = new(
            loggerMock.Object,
            aiSummarizer,
            yamlHelperMock.Object,
            CreateMetadataHierarchyDetector(),
            new MetadataTemplateManager(NullLogger<MetadataTemplateManager>.Instance, _testAppConfig, yamlHelperMock.Object),
            oneDriveServiceMock.Object,
            _testAppConfig);

        string videoPath = Path.Combine(testDir, "test-video.mp4");
        File.WriteAllText(videoPath, "fake video content");

        // Act
        string markdown = await processor.GenerateVideoNoteAsync(
            videoPath,
            "test-api-key",
            null, // promptFileName
            false, // noSummary
            null, // timeoutSeconds
            null, // resourcesRoot
            false) // noShareLinks - Enable share links
        .ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(markdown);

        // Verify share link appears in markdown content
        Assert.IsTrue(markdown.Contains("## References"), "Should contain References section");
        Assert.IsTrue(markdown.Contains($"[Video Recording]({shareLink})"), "Should contain share link in References section");

        // Verify share link does NOT appear in YAML frontmatter
        int frontmatterEnd = markdown.IndexOf("---", 4); // Find the closing ---
        if (frontmatterEnd > 0)
        {
            string frontmatter = markdown[..frontmatterEnd];

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
        VideoNoteProcessor processor = new(
            loggerMock.Object,
            aiSummarizer,
            yamlHelperMock.Object,
            CreateMetadataHierarchyDetector(),
            new MetadataTemplateManager(NullLogger<MetadataTemplateManager>.Instance, _testAppConfig, yamlHelperMock.Object),
            oneDriveServiceMock.Object,
            null);

        string videoPath = Path.Combine(testDir, "test-video.mp4");
        File.WriteAllText(videoPath, "fake video content");

        // Act
        string markdown = await processor.GenerateVideoNoteAsync(
            videoPath,
            "test-api-key",
            null, // promptFileName
            false, // noSummary
            null, // timeoutSeconds
            null, // resourcesRoot
            true) // noShareLinks - Disable share links
        .ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(markdown);

        // Verify no References section when share links are disabled
        Assert.IsFalse(markdown.Contains("## References"), "Should not contain References section when noShareLinks=true");
        Assert.IsFalse(markdown.Contains("[Video Recording]"), "Should not contain video recording link when noShareLinks=true");            // Verify OneDriveService was not called
        oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GenerateVideoNoteAsync_WithFailedShareLink_DoesNotContainReferencesSection()
    { // Arrange
        oneDriveServiceMock
            .Setup(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null); // Simulate failed share link generation

        VideoNoteProcessor processor = new(
            loggerMock.Object,
            aiSummarizer,
            yamlHelperMock.Object,
            CreateMetadataHierarchyDetector(),
            new MetadataTemplateManager(NullLogger<MetadataTemplateManager>.Instance, _testAppConfig, yamlHelperMock.Object),
            oneDriveServiceMock.Object,
            null);

        string videoPath = Path.Combine(testDir, "test-video.mp4");
        File.WriteAllText(videoPath, "fake video content");

        // Act
        string markdown = await processor.GenerateVideoNoteAsync(
            videoPath,
            "test-api-key",
            null, // promptFileName
            false, // noSummary
            null, // timeoutSeconds
            null, // resourcesRoot
            false) // noShareLinks - Enable share links, but they will fail
        .ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(markdown);

        // Verify no References section when share link generation fails
        Assert.IsFalse(markdown.Contains("## References"), "Should not contain References section when share link generation fails");
        Assert.IsFalse(markdown.Contains("[Video Recording]"), "Should not contain video recording link when share link generation fails");            // Verify OneDriveService was called but failed
        oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GetShareLink_OneDriveEnabled_ReturnsShareLink()
    {
        // Arrange
        Directory.CreateDirectory(testDir);
        string expectedShareLink = "https://example.com/share-link";
        string shareLink = $"{{\"webUrl\": \"{expectedShareLink}\"}}";

        oneDriveServiceMock.Setup(m => m.GetShareLinkAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(shareLink);

        VideoNoteProcessor processor = new(
            loggerMock.Object,
            aiSummarizer,
            yamlHelperMock.Object,
            CreateMetadataHierarchyDetector(),
            new MetadataTemplateManager(NullLogger<MetadataTemplateManager>.Instance, _testAppConfig, yamlHelperMock.Object),
            oneDriveServiceMock.Object,
            null);

        string videoPath = Path.Combine(testDir, "test-video.mp4");
        File.WriteAllText(videoPath, "fake video content");

        // Act
        string markdown = await processor.GenerateVideoNoteAsync(
            videoPath,
            "test-api-key",
            null, // promptFileName
            false, // noSummary
            null, // timeoutSeconds
            null, // resourcesRoot
            false) // noShareLinks - Enable share links
        .ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(markdown);
        Assert.IsTrue(markdown.Contains("## References"), "Should contain References section");
        Assert.IsTrue(markdown.Contains($"[Video Recording]({expectedShareLink})"), "Should contain share link in References section");
    }

    [TestMethod]
    public async Task GetShareLink_OneDriveDisabled_ReturnsNull()
    {
        // Arrange
        Directory.CreateDirectory(testDir);        // Use another instance to ensure we're not using the mock (OneDriveService is null)
        VideoNoteProcessor processor = new(
            loggerMock.Object,
            aiSummarizer,
            yamlHelperMock.Object,
            CreateMetadataHierarchyDetector(),
            new MetadataTemplateManager(
                Mock.Of<ILogger<MetadataTemplateManager>>(),
                _testAppConfig,
                yamlHelperMock.Object),
            null, // OneDriveService is null to disable OneDrive functionality
            _testAppConfig); // AppConfig

        string videoPath = Path.Combine(testDir, "test-video.mp4");
        File.WriteAllText(videoPath, "fake video content");

        // Act
        string markdown = await processor.GenerateVideoNoteAsync(
            videoPath,
            "test-api-key",
            null, // promptFileName
            false, // noSummary
            null, // timeoutSeconds
            null, // resourcesRoot
            false) // noShareLinks - Enable share links
        .ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(markdown);
        Assert.IsFalse(markdown.Contains("## References"), "Should not contain References section when OneDrive is disabled");
    }

    [TestMethod]
    public async Task GenerateShareLinkMarkdown_HasLink_IncludesLinkInNote()
    {
        // Arrange
        Directory.CreateDirectory(testDir);
        string expectedShareLink = "https://example.com/share-link-in-note";

        // Use CreateShareLinkAsync instead of GetShareLinkAsync since VideoNoteProcessor uses it
        oneDriveServiceMock.Setup(m => m.CreateShareLinkAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedShareLink);

        VideoNoteProcessor processor = new(
            loggerMock.Object,
            aiSummarizer,
            yamlHelperMock.Object,
            CreateMetadataHierarchyDetector(),
            new MetadataTemplateManager(NullLogger<MetadataTemplateManager>.Instance, _testAppConfig, yamlHelperMock.Object),
            oneDriveServiceMock.Object,
            null);

        string videoPath = Path.Combine(testDir, "test-video.mp4");
        File.WriteAllText(videoPath, "fake video content");

        // Act
        string markdown = await processor.GenerateVideoNoteAsync(
            videoPath,
            "test-api-key",
            null, // promptFileName
            false, // noSummary
            null, // timeoutSeconds
            null, // resourcesRoot
            false) // noShareLinks - Enable share links
        .ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(markdown);
        Assert.IsTrue(markdown.Contains("## References"), "Should contain References section");
        Assert.IsTrue(markdown.Contains($"[Video Recording]({expectedShareLink})"), "Should contain share link in References section");
    }
    private MetadataHierarchyDetector CreateMetadataHierarchyDetector()
    {
        return new MetadataHierarchyDetector(
            NullLogger<MetadataHierarchyDetector>.Instance,
            _testAppConfig)
        { Logger = NullLogger<MetadataHierarchyDetector>.Instance };
    }
}


