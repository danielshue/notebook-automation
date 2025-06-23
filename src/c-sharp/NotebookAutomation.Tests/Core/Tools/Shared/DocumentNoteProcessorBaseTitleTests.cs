// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Tools.Shared;

/// <summary>
/// Tests for the DocumentNoteProcessorBase title normalization functionality.
/// </summary>
[TestClass]
public class DocumentNoteProcessorBaseTitleTests
{
    private Mock<ILogger<TestDocumentProcessor>> _loggerMock = new Mock<ILogger<TestDocumentProcessor>>();
    private Mock<IAISummarizer> _aiSummarizerMock = new Mock<IAISummarizer>();
    private Mock<IYamlHelper> _yamlHelperMock = new Mock<IYamlHelper>();
    private MarkdownNoteBuilder _markdownNoteBuilder = null!;
    private AppConfig _appConfig = new AppConfig();
    private TestDocumentProcessor _processor = null!;

    [TestInitialize]
    public void Setup()
    {
        _yamlHelperMock.Setup(x => x.UpdateFrontmatter(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns((string content, Dictionary<string, object> metadata) =>
            {
                var yaml = string.Join("\n", metadata.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                return $"---\n{yaml}\n---\n{content}";
            });

        _markdownNoteBuilder = new MarkdownNoteBuilder(_yamlHelperMock.Object, _appConfig);

        _processor = new TestDocumentProcessor(
            _loggerMock.Object,
            _aiSummarizerMock.Object,
            _markdownNoteBuilder,
            _appConfig);
    }

    [TestMethod]
    public void ExtractFirstHeading_WithValidH1_ReturnsHeadingText()
    {
        // Arrange
        const string markdownText = "Some text\n# Test Heading\nMore content\n## Subheading";

        // Act
        string? result = TestDocumentProcessor.ExtractFirstHeadingPublic(markdownText);

        // Assert
        Assert.AreEqual("Test Heading", result);
    }

    [TestMethod]
    public void ExtractFirstHeading_WithNoHeading_ReturnsNull()
    {
        // Arrange
        const string markdownText = "Some text\nMore content\nNo headings here";

        // Act
        string? result = TestDocumentProcessor.ExtractFirstHeadingPublic(markdownText);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractFirstHeading_WithMultipleH1_ReturnsFirst()
    {
        // Arrange
        const string markdownText = "# First Heading\nContent\n# Second Heading";

        // Act
        string? result = TestDocumentProcessor.ExtractFirstHeadingPublic(markdownText);

        // Assert
        Assert.AreEqual("First Heading", result);
    }

    [TestMethod]
    public void ExtractAndNormalizeTitle_WithBodyHeading_UsesFriendlyVersionOfHeading()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>();
        const string bodyText = "# 01-Introduction to Strategic Management\nContent here";
        const string noteType = "PDF Note";

        // Act
        string result = _processor.ExtractAndNormalizeTitlePublic(frontmatter, bodyText, noteType, true);

        // Assert
        Assert.AreEqual("Introduction Strategic Management", result);
    }

    [TestMethod]
    public void ExtractAndNormalizeTitle_WithFrontmatterTitle_UsesFriendlyVersionOfTitle()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["title"] = "02_MBA_Strategy_Module"
        };
        const string bodyText = "Some content without headings";
        const string noteType = "PDF Note";

        // Act
        string result = _processor.ExtractAndNormalizeTitlePublic(frontmatter, bodyText, noteType, true);        // Assert
        Assert.AreEqual("MBA Strategy", result);
    }

    [TestMethod]
    public void ExtractAndNormalizeTitle_WithSourceFilename_GeneratesFriendlyTitle()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["source"] = "/path/to/03-Marketing_Analysis.pdf"
        };
        const string bodyText = "Content without title";
        const string noteType = "PDF Note";

        // Act
        string result = _processor.ExtractAndNormalizeTitlePublic(frontmatter, bodyText, noteType, true);

        // Assert
        Assert.AreEqual("Marketing Analysis", result);
    }

    [TestMethod]
    public void ExtractAndNormalizeTitle_WithNoValidTitle_UsesNoteTypeFallback()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>();
        const string bodyText = "Content without headings or useful metadata";
        const string noteType = "Video Note";

        // Act
        string result = _processor.ExtractAndNormalizeTitlePublic(frontmatter, bodyText, noteType, true);

        // Assert
        Assert.AreEqual("Video Note", result);
    }

    [TestMethod]
    public void GenerateMarkdownNote_EnsuresTitleConsistency()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["author"] = "Test Author"
        };
        const string bodyText = "# 01-Strategic_Planning\nThis is the content of the note.";
        const string noteType = "PDF Note";

        // Act
        string result = _processor.GenerateMarkdownNote(bodyText, metadata, noteType, false, true);

        // Assert
        // The result should have the normalized title in both frontmatter and heading
        Assert.IsTrue(result.Contains("title: Strategic Planning"), "Should contain normalized title in frontmatter");
        Assert.IsTrue(result.Contains("# Strategic Planning"), "Should contain normalized title as heading");
        Assert.IsTrue(result.Contains("author: Test Author"), "Should preserve other metadata");
    }
    [TestMethod]
    public void ExtractFirstHeading_WithFilenameBasedHeading_AppliesFriendlyTitleHelper()
    {
        // Arrange - simulate AI-generated content with filename-based heading
        const string markdownText = "# 02_01__BAMD 567 MOOC 1 Module 3 Word Transcript\n\nThis is the AI-generated summary content.";

        // Act
        string? result = TestDocumentProcessor.ExtractFirstHeadingPublic(markdownText);

        // Assert
        // FriendlyTitleHelper should clean up the filename format
        // Note: "Module" is removed but "3" remains, which is the expected behavior
        Assert.AreEqual("BAMD 567 MOOC 1 3 Word Transcript", result);
    }

    [TestMethod]
    public void ExtractAndNormalizeTitle_WithAIGeneratedFilenameHeading_UsesFriendlyVersion()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>();
        const string bodyText = "# 02_01__BAMD 567 MOOC 1 Module 3 Word Transcript\n\nSummary content here";
        const string noteType = "PDF Note";

        // Act
        string result = _processor.ExtractAndNormalizeTitlePublic(frontmatter, bodyText, noteType, true);

        // Assert
        // Should extract the heading and apply FriendlyTitleHelper
        // Note: "Module" is removed but "3" remains, which is the expected behavior
        Assert.AreEqual("BAMD 567 MOOC 1 3 Word Transcript", result);
    }

    /// <summary>
    /// Test implementation of DocumentNoteProcessorBase for testing purposes.
    /// </summary>
    public class TestDocumentProcessor : DocumentNoteProcessorBase
    {
        public TestDocumentProcessor(
            ILogger logger,
            IAISummarizer aiSummarizer,
            MarkdownNoteBuilder markdownNoteBuilder,
            AppConfig appConfig,
            IYamlHelper? yamlHelper = null,
            IMetadataHierarchyDetector? hierarchyDetector = null,
            IMetadataTemplateManager? templateManager = null)
            : base(logger, aiSummarizer, markdownNoteBuilder, appConfig, yamlHelper, hierarchyDetector, templateManager)
        {
        }

        public override Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string filePath)
        {
            // Simple test implementation
            return Task.FromResult(("Test content", new Dictionary<string, object>()));
        }

        // Public wrapper for testing protected methods
        public static string? ExtractFirstHeadingPublic(string markdownText) => ExtractFirstHeading(markdownText);

        public string ExtractAndNormalizeTitlePublic(
            Dictionary<string, object> frontmatter,
            string bodyText,
            string noteType,
            bool includeNoteTypeTitle) =>
            ExtractAndNormalizeTitle(frontmatter, bodyText, noteType, includeNoteTypeTitle);
    }
}
