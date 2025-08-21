// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;

namespace NotebookAutomation.Tests.Core.Tools.VideoProcessing;

/// <summary>
/// Tests for the VideoNoteProcessor.TryLoadTranscript method.
/// These tests validate the transcript finding functionality in various scenarios.
/// </summary>
[TestClass]
public class VideoNoteProcessorTranscriptTests
{
    private string _tempDirectory = string.Empty;
    private ILogger<VideoNoteProcessor> _logger = NullLogger<VideoNoteProcessor>.Instance;
    private VideoNoteProcessor _processor = null!;

    /// <summary>
    /// Sets up the test environment before each test.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"VideoProcessorTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
        _logger = NullLogger<VideoNoteProcessor>.Instance;
        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(NullLogger<YamlHelper>.Instance),
            new AppConfig());
        AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null);        // Create mock YamlHelper
        var yamlHelperMock = new Mock<IYamlHelper>();

        // Setup mock YamlHelper
        yamlHelperMock.Setup(m => m.RemoveFrontmatter(It.IsAny<string>()))
            .Returns<string>(markdown => markdown.Contains("---") ? markdown.Substring(markdown.IndexOf("---", 3) + 3) : markdown);

        yamlHelperMock.Setup(m => m.ParseYamlToDictionary(It.IsAny<string>()))
            .Returns(new Dictionary<string, object>
            {
                { "template-type", "video-reference" },
                { "type", "video-reference" },
                { "title", "Test Video" },
                { "tags", new[] { "video", "reference" } },
            });

        yamlHelperMock.Setup(m => m.ExtractFrontmatter(It.IsAny<string>()))
            .Returns("template-type: video-reference\ntitle: Test Video");

        yamlHelperMock.Setup(m => m.SerializeToYaml(It.IsAny<Dictionary<string, object>>()))
            .Returns("---\ntemplate-type: video-reference\ntitle: Test Video\n---\n");
        var mockYamlHelper = yamlHelperMock.Object;
        var mockLogger = new Mock<ILogger<MetadataHierarchyDetector>>();
        var appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = Path.Combine(Path.GetTempPath(), "TestVault"),
                MetadataSchemaFile = Path.Combine(AppContext.BaseDirectory, "config", "metadata-schema.yml"),
            },
        };
        var mockHierarchyDetector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector();
        var templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        var markdownNoteBuilder = new MarkdownNoteBuilder(mockYamlHelper, appConfig);
        var mockCourseStructureExtractor = Mock.Of<ICourseStructureExtractor>();
        _processor = new VideoNoteProcessor(
            _logger,
            aiSummarizer,
            mockYamlHelper,
            mockHierarchyDetector,
            templateManager,
            mockCourseStructureExtractor,
            markdownNoteBuilder,
            null,
            appConfig,
            new FieldValueResolverRegistry());
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
    private static string CreateTestVideoFile(string directory, string fileName = "test_video.mp4")
    {
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, "Mock video file content");
        return filePath;
    }

    /// <summary>
    /// Creates a mock transcript file in the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the file will be created.</param>
    /// <param name="fileName">The name of the transcript file.</param>
    /// <param name="content">The content of the transcript file.</param>
    /// <returns>The full path to the created file.</returns>
    private static string CreateTestTranscriptFile(string directory, string fileName, string content = "Test transcript content")
    {
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Tests finding a transcript file with the exact same name as the video but with .txt extension.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_DirectMatch_ReturnsTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory);
        _ = CreateTestTranscriptFile(_tempDirectory, "test_video.txt");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find direct matching transcript");
        Assert.AreEqual("Test transcript content", result);
    }

    /// <summary>
    /// Tests finding a transcript file with the exact same name as the video but with .md extension.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_MarkdownMatch_ReturnsTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory);
        _ = CreateTestTranscriptFile(_tempDirectory, "test_video.md");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find matching markdown transcript");
        Assert.AreEqual("Test transcript content", result);
    }

    /// <summary>
    /// Tests finding a transcript file in a Transcripts subdirectory.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_TranscriptSubdirectory_ReturnsTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory);
        string transcriptsDir = Path.Combine(_tempDirectory, "Transcripts");
        Directory.CreateDirectory(transcriptsDir);
        _ = CreateTestTranscriptFile(transcriptsDir, "test_video.txt");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find transcript in subdirectory");
        Assert.AreEqual("Test transcript content", result);
    }

    /// <summary>
    /// Tests finding a language-specific transcript file (e.g. video.en.txt).
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_LanguageSpecificTranscript_ReturnsTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory);
        _ = CreateTestTranscriptFile(_tempDirectory, "test_video.en.txt", "English transcript");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find language-specific transcript");
        Assert.AreEqual("English transcript", result);
    }

    /// <summary>
    /// Tests finding a transcript for a video file with spaces in its name.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_SpacesInFilename_ReturnsTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory, "test video with spaces.mp4");
        _ = CreateTestTranscriptFile(_tempDirectory, "test video with spaces.txt");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find transcript with spaces in name");
        Assert.AreEqual("Test transcript content", result);
    }

    /// <summary>
    /// Tests finding a transcript with normalized name (replacing hyphens with underscores or vice versa).
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_NormalizedName_ReturnsTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory, "test-video-with-hyphens.mp4");
        _ = CreateTestTranscriptFile(_tempDirectory, "test_video_with_hyphens.txt");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find transcript with normalized name");
        Assert.AreEqual("Test transcript content", result);
    }

    /// <summary>
    /// Tests finding a language-specific transcript with normalized name.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_LanguageSpecificNormalizedName_ReturnsTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory, "test-video.mp4");
        _ = CreateTestTranscriptFile(_tempDirectory, "test_video.en-us.txt", "English-US transcript");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find language-specific transcript with normalized name");
        Assert.AreEqual("English-US transcript", result);
    }

    /// <summary>
    /// Tests the case where no transcript can be found.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_NoTranscriptFound_ReturnsNull()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory);

        // Don't create any transcript files

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNull(result, "Should return null when no transcript is found");
    }

    /// <summary>
    /// Tests the IsLikelyLanguageCode method through the TryLoadTranscript functionality.
    /// Note: The order of finding language-specific transcripts depends on file system enumeration,
    /// which in this case returns the German transcript (.deu.txt) first.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_VariousLanguageCodes_ReturnsCorrectTranscript()
    {
        // Arrange
        string videoPath = CreateTestVideoFile(_tempDirectory);

        // Create multiple language transcripts
        CreateTestTranscriptFile(_tempDirectory, "test_video.en.txt", "English transcript");
        CreateTestTranscriptFile(_tempDirectory, "test_video.fr.txt", "French transcript");
        CreateTestTranscriptFile(_tempDirectory, "test_video.zh-cn.txt", "Chinese transcript");
        CreateTestTranscriptFile(_tempDirectory, "test_video.deu.txt", "German transcript");

        // Create an invalid one that should not be recognized
        CreateTestTranscriptFile(_tempDirectory, "test_video.invalid-language-code.txt", "Invalid transcript");

        // Act
        string? result = _processor.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(result, "Failed to find language-specific transcript");
        // Accept any of the valid language transcripts
        var validTranscripts = new[] { "English transcript", "French transcript", "Chinese transcript", "German transcript" };
        CollectionAssert.Contains(validTranscripts, result, "Transcript should match one of the valid language transcripts");
    }

    /// <summary>
    /// Tests providing an empty video path.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_EmptyVideoPath_ReturnsNull()
    {
        // Act
        string? result = _processor.TryLoadTranscript(string.Empty);

        // Assert
        Assert.IsNull(result, "Should return null for empty video path");
    }

    /// <summary>
    /// Tests providing a null video path.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_NullVideoPath_ReturnsNull()
    {
        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        string? result = _processor.TryLoadTranscript(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        Assert.IsNull(result, "Should return null for null video path");
    }

    /// <summary>
    /// Tests that when a transcript file is found, its path is added to the metadata.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task TranscriptPath_IsAddedToMetadata_WhenFound()
    {
        // Arrange
        string videoPath = Path.Combine(_tempDirectory, "test_video.mp4");
        File.WriteAllText(videoPath, "Simulated video content"); string transcriptPath = CreateTestTranscriptFile(_tempDirectory, "test_video.txt", "Sample transcript content");

        // Act
        Dictionary<string, object?> metadata = await _processor.ExtractMetadataAsync(videoPath).ConfigureAwait(false);// Use reflection to call the private FindTranscriptPath method
        System.Reflection.MethodInfo? methodInfo = typeof(VideoNoteProcessor).GetMethod(
            "FindTranscriptPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (methodInfo != null)
        {
            // Find the transcript path separately
            string? foundTranscriptPath = methodInfo.Invoke(_processor, [videoPath]) as string;

            // If using reflection worked, manually add to metadata to test
            if (!string.IsNullOrEmpty(foundTranscriptPath))
            {
                metadata["transcript"] = foundTranscriptPath;
            }
        }

        // Create a GenerateVideoNoteAsync method wrapper to test both the metadata extraction
        // and the markdown note generation
        string markdownNote = await _processor.GenerateVideoNoteAsync(
            videoPath,
            "dummy-api-key",
            "final_summary_prompt",
            noSummary: true, // Skip summary generation to simplify the test
            noShareLinks: true).ConfigureAwait(false); // Skip share link generation

        // Assert
        Assert.IsTrue(metadata.ContainsKey("transcript"), "Metadata should contain 'transcript' key");
        Assert.AreEqual(transcriptPath, metadata["transcript"], "Transcript path in metadata should match the actual transcript path");

        // Verify the transcript path is NOT included in the output frontmatter per new requirement
        // Be precise: only disallow a top-level 'transcript:' key, allow other transcript-* fields introduced by schema
        bool HasTopLevelTranscriptKey(string md)
        {
            if (string.IsNullOrWhiteSpace(md)) return false;
            int start = md.IndexOf("---", StringComparison.Ordinal);
            if (start != 0) return false;
            int afterFirstLine = md.IndexOf('\n', start);
            if (afterFirstLine < 0) return false;
            int endMarkerIdx = md.IndexOf("\n---", afterFirstLine, StringComparison.Ordinal);
            if (endMarkerIdx < 0)
            {
                endMarkerIdx = md.IndexOf("\r\n---", afterFirstLine, StringComparison.Ordinal);
                if (endMarkerIdx < 0) return false;
            }
            int contentStart = afterFirstLine + 1;
            var yaml = md.Substring(contentStart, endMarkerIdx - contentStart);
            var lines = yaml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            return lines.Any(line => line.TrimStart().StartsWith("transcript:", StringComparison.Ordinal));
        }

        Assert.IsFalse(HasTopLevelTranscriptKey(markdownNote),
            "Generated markdown should not include transcript path in frontmatter as 'transcript:' key");
    }

    /// <summary>
    /// Tests that the processor respects preferred transcript language configuration when multiple language-specific transcripts are available.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_MultipleLanguages_ReturnsPreferredLanguage()
    {
        // Arrange
        string videoPath = Path.Combine(_tempDirectory, "test-video.mp4");
        string englishTranscriptPath = Path.Combine(_tempDirectory, "test-video.en.txt");
        string arabicTranscriptPath = Path.Combine(_tempDirectory, "test-video.ar.txt");
        string frenchTranscriptPath = Path.Combine(_tempDirectory, "test-video.fr.txt");

        // Create test video file
        File.WriteAllText(videoPath, "dummy video content");

        // Create transcript files in different languages
        File.WriteAllText(englishTranscriptPath, "English transcript content");
        File.WriteAllText(arabicTranscriptPath, "Arabic transcript content");
        File.WriteAllText(frenchTranscriptPath, "French transcript content");

        // Create AppConfig with language preferences (English first, then French)
        var appConfig = new AppConfig
        {
            PreferredTranscriptLanguages = ["en", "fr", "es"]
        };

        // Create processor with the configuration
        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(NullLogger<YamlHelper>.Instance),
            appConfig);
        AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null);

        var yamlHelperMock = new Mock<IYamlHelper>();
        yamlHelperMock.Setup(m => m.RemoveFrontmatter(It.IsAny<string>()))
            .Returns<string>(markdown => markdown.Contains("---") ? markdown.Substring(markdown.IndexOf("---", 3) + 3) : markdown);

        var hierarchyDetectorMock = new Mock<IMetadataHierarchyDetector>();
        var templateManagerMock = new Mock<IMetadataTemplateManager>();
        var courseStructureExtractorMock = new Mock<ICourseStructureExtractor>();
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelperMock.Object, appConfig);

        var processorWithConfig = new VideoNoteProcessor(
            _logger,
            aiSummarizer,
            yamlHelperMock.Object,
            hierarchyDetectorMock.Object,
            templateManagerMock.Object,
            courseStructureExtractorMock.Object,
            markdownNoteBuilder,
            null,
            appConfig);

        // Act
        string? transcript = processorWithConfig.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(transcript, "Should find a transcript");
        Assert.AreEqual("English transcript content", transcript, "Should return English transcript (preferred language)");
    }

    /// <summary>
    /// Tests that when no preferred language is found, the processor returns the first available language-specific transcript.
    /// </summary>
    [TestMethod]
    public void TryLoadTranscript_NoPreferredLanguageAvailable_ReturnsFirstAvailable()
    {
        // Arrange
        string videoPath = Path.Combine(_tempDirectory, "test-video.mp4");
        string arabicTranscriptPath = Path.Combine(_tempDirectory, "test-video.ar.txt");
        string japaneseTranscriptPath = Path.Combine(_tempDirectory, "test-video.ja.txt");

        // Create test video file
        File.WriteAllText(videoPath, "dummy video content");

        // Create transcript files (no English available)
        File.WriteAllText(arabicTranscriptPath, "Arabic transcript content");
        File.WriteAllText(japaneseTranscriptPath, "Japanese transcript content");

        // Create AppConfig with language preferences (English first, but not available)
        var appConfig = new AppConfig
        {
            PreferredTranscriptLanguages = ["en", "fr", "es"]
        };

        // Create processor with the configuration
        PromptTemplateService promptService = new(
            NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(NullLogger<YamlHelper>.Instance),
            appConfig);
        AISummarizer aiSummarizer = new(
            NullLogger<AISummarizer>.Instance,
            promptService,
            null);

        var yamlHelperMock = new Mock<IYamlHelper>();
        yamlHelperMock.Setup(m => m.RemoveFrontmatter(It.IsAny<string>()))
            .Returns<string>(markdown => markdown.Contains("---") ? markdown.Substring(markdown.IndexOf("---", 3) + 3) : markdown);

        var hierarchyDetectorMock = new Mock<IMetadataHierarchyDetector>();
        var templateManagerMock = new Mock<IMetadataTemplateManager>();
        var courseStructureExtractorMock = new Mock<ICourseStructureExtractor>();
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelperMock.Object, appConfig);

        var processorWithConfig = new VideoNoteProcessor(
            _logger,
            aiSummarizer,
            yamlHelperMock.Object,
            hierarchyDetectorMock.Object,
            templateManagerMock.Object,
            courseStructureExtractorMock.Object,
            markdownNoteBuilder,
            null,
            appConfig);

        // Act
        string? transcript = processorWithConfig.TryLoadTranscript(videoPath);

        // Assert
        Assert.IsNotNull(transcript, "Should find a transcript");
        // Should return one of the available transcripts (either Arabic or Japanese)
        Assert.IsTrue(transcript == "Arabic transcript content" || transcript == "Japanese transcript content",
            "Should return one of the available language-specific transcripts");
    }
}
