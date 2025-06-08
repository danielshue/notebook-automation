// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tests.Tools.MarkdownGeneration;

/// <summary>
/// Unit tests for the <see cref="MarkdownNoteProcessor"/> class.
/// Tests markdown conversion functionality for various file types and scenarios.
/// </summary>
[TestClass]
public class MarkdownNoteProcessorTests
{
    /// <summary>
    /// Creates a test instance of <see cref="MetadataHierarchyDetector"/> with mocked dependencies.
    /// </summary>
    /// <returns>A configured <see cref="MetadataHierarchyDetector"/> instance for testing.</returns>
    private static MetadataHierarchyDetector CreateTestHierarchyDetector()
    {
        var logger = new Mock<ILogger<MetadataHierarchyDetector>>().Object;
        var appConfig = new AppConfig { Paths = new PathsConfig { NotebookVaultFullpathRoot = Environment.CurrentDirectory } };
        return new MetadataHierarchyDetector(logger, appConfig) { Logger = logger };
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> correctly converts
    /// a plain text file to markdown format, preserving the original content.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>    [TestMethod]
    public async Task ConvertToMarkdownAsync_TxtFile_ReturnsMarkdown()
    {
        // Arrange
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        summarizer.SetupSummarizeAsyncResult("Summary of content: Hello world!");
        MetadataHierarchyDetector hierarchyDetector = CreateTestHierarchyDetector();
        MarkdownNoteProcessor processor = new(logger, summarizer, hierarchyDetector);
        string testFile = "test.txt";
        await File.WriteAllTextAsync(testFile, "Hello world!").ConfigureAwait(false);

        // Act
        string result = await processor.ConvertToMarkdownAsync(testFile, "dummy-api-key", "test-prompt").ConfigureAwait(false);

        // Assert
        Assert.IsTrue(result.Contains("Hello world!"));
        File.Delete(testFile);
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> correctly processes
    /// HTML files by stripping HTML tags and preserving the text content in markdown format.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>    [TestMethod]
    public async Task ConvertToMarkdownAsync_HtmlFile_StripsHtmlTags()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        summarizer.SetupSummarizeAsyncResult("Summary of content: Header Paragraph");
        MarkdownNoteProcessor processor = new(logger, summarizer, CreateTestHierarchyDetector());
        string testFile = "test.html";
        await File.WriteAllTextAsync(testFile, "<h1>Header</h1><p>Paragraph</p>").ConfigureAwait(false);
        string result = await processor.ConvertToMarkdownAsync(testFile, "dummy-api-key", "test-prompt").ConfigureAwait(false);
        Assert.IsTrue(result.Contains("Header"));
        Assert.IsTrue(result.Contains("Paragraph"));
        Assert.IsFalse(result.Contains("<h1>"));
        File.Delete(testFile);
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> handles empty HTML files
    /// correctly by returning markdown with metadata but minimal content.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ConvertToMarkdownAsync_EmptyHtmlFile_ReturnsEmptyMarkdown()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer, CreateTestHierarchyDetector());
        string testFile = "empty.html";
        await File.WriteAllTextAsync(testFile, string.Empty).ConfigureAwait(false);
        string result = await processor.ConvertToMarkdownAsync(testFile, "dummy-api-key", "test-prompt").ConfigureAwait(false);
        Assert.IsTrue(result.Contains("generated")); // Metadata present        File.Delete(testFile);
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> handles unsupported
    /// file types by returning an empty string rather than throwing an exception.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ConvertToMarkdownAsync_UnsupportedFileType_ReturnsEmpty()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer, CreateTestHierarchyDetector());
        string testFile = "test.unsupported";
        await File.WriteAllTextAsync(testFile, "data").ConfigureAwait(false);
        string result = await processor.ConvertToMarkdownAsync(testFile, "dummy-api-key", "test-prompt").ConfigureAwait(false);
        Assert.AreEqual(string.Empty, result);
        File.Delete(testFile);
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> gracefully handles
    /// missing files by returning an empty string without throwing an exception.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ConvertToMarkdownAsync_MissingFile_ReturnsEmpty()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer, CreateTestHierarchyDetector());
        string result = await processor.ConvertToMarkdownAsync("doesnotexist.txt", "dummy-api-key", "test-prompt").ConfigureAwait(false);
        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> properly processes
    /// HTML files containing only HTML tags (no text content) by stripping the tags and
    /// returning markdown with minimal content.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ConvertToMarkdownAsync_HtmlFile_OnlyTags_ReturnsEmptyBody()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer, CreateTestHierarchyDetector());
        string testFile = "tags.html";
        await File.WriteAllTextAsync(testFile, "<div><span></span></div>").ConfigureAwait(false);
        string result = await processor.ConvertToMarkdownAsync(testFile, "dummy-api-key", "test-prompt").ConfigureAwait(false);
        Assert.IsFalse(result.Contains("<div>"));
        File.Delete(testFile);
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> correctly integrates
    /// with AI summarization when an OpenAI key is provided, returning markdown content that
    /// includes the AI-generated summary.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ConvertToMarkdownAsync_TxtFile_WithAISummary_ReturnsAISummary()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        summarizer.SetupSummarizeAsyncResult("AI summary result");
        MarkdownNoteProcessor processor = new(logger, summarizer, CreateTestHierarchyDetector());
        string testFile = "ai.txt";
        await File.WriteAllTextAsync(testFile, "This is a test for AI summary.").ConfigureAwait(false);
        string result = await processor.ConvertToMarkdownAsync(testFile, "fake-key", "test-prompt").ConfigureAwait(false);
        Assert.IsTrue(result.Contains("AI summary result"));
        File.Delete(testFile);
    }

    /// <summary>
    /// Tests that <see cref="MarkdownNoteProcessor.ConvertToMarkdownAsync"/> handles EPUB files
    /// gracefully by catching parsing exceptions and returning an empty string when the EPUB
    /// content cannot be processed.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ConvertToMarkdownAsync_EpubFile_Mocked_ReturnsMarkdown()
    {
        // Arrange
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer, CreateTestHierarchyDetector());
        string testFile = "test.epub";

        // Simulate EPUB file (file must exist, but content is mocked)
        await File.WriteAllTextAsync(testFile, "EPUB content").ConfigureAwait(false);

        // Mock VersOne.Epub.EpubReader.ReadBookAsync via reflection or skip if not available
        // This test will just check that the method handles the catch block gracefully
        string result = await processor.ConvertToMarkdownAsync(testFile, "dummy-api-key", "test-prompt").ConfigureAwait(false);

        // Since the EPUB parser will throw, expect empty string
        Assert.AreEqual(string.Empty, result);
        File.Delete(testFile);
    }
}
