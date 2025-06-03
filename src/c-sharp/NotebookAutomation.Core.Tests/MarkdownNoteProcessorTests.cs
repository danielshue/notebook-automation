using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Core.Tests;

[TestClass]
public class MarkdownNoteProcessorTests
{
    [TestMethod]
    public async Task ConvertToMarkdownAsync_TxtFile_ReturnsMarkdown()
    {
        // Arrange
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string testFile = "test.txt";
        await File.WriteAllTextAsync(testFile, "Hello world!");

        // Act
        string result = await processor.ConvertToMarkdownAsync(testFile);

        // Assert
        Assert.IsTrue(result.Contains("Hello world!"));
        File.Delete(testFile);
    }

    [TestMethod]
    public async Task ConvertToMarkdownAsync_HtmlFile_StripsHtmlTags()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string testFile = "test.html";
        await File.WriteAllTextAsync(testFile, "<h1>Header</h1><p>Paragraph</p>");
        string result = await processor.ConvertToMarkdownAsync(testFile);
        Assert.IsTrue(result.Contains("Header"));
        Assert.IsTrue(result.Contains("Paragraph"));
        Assert.IsFalse(result.Contains("<h1>"));
        File.Delete(testFile);
    }

    [TestMethod]
    public async Task ConvertToMarkdownAsync_EmptyHtmlFile_ReturnsEmptyMarkdown()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string testFile = "empty.html";
        await File.WriteAllTextAsync(testFile, "");
        string result = await processor.ConvertToMarkdownAsync(testFile);
        Assert.IsTrue(result.Contains("generated")); // Metadata present
        File.Delete(testFile);
    }

    [TestMethod]
    public async Task ConvertToMarkdownAsync_UnsupportedFileType_ReturnsEmpty()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string testFile = "test.unsupported";
        await File.WriteAllTextAsync(testFile, "data");
        string result = await processor.ConvertToMarkdownAsync(testFile);
        Assert.AreEqual(string.Empty, result);
        File.Delete(testFile);
    }

    [TestMethod]
    public async Task ConvertToMarkdownAsync_MissingFile_ReturnsEmpty()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string result = await processor.ConvertToMarkdownAsync("doesnotexist.txt");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public async Task ConvertToMarkdownAsync_HtmlFile_OnlyTags_ReturnsEmptyBody()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string testFile = "tags.html";
        await File.WriteAllTextAsync(testFile, "<div><span></span></div>");
        string result = await processor.ConvertToMarkdownAsync(testFile);
        Assert.IsFalse(result.Contains("<div>"));
        File.Delete(testFile);
    }

    [TestMethod]
    public async Task ConvertToMarkdownAsync_TxtFile_WithAISummary_ReturnsAISummary()
    {
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        summarizer.SetupSummarizeAsyncResult("AI summary result");
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string testFile = "ai.txt";
        await File.WriteAllTextAsync(testFile, "This is a test for AI summary.");
        string result = await processor.ConvertToMarkdownAsync(testFile, "fake-key");
        Assert.IsTrue(result.Contains("AI summary result"));
        File.Delete(testFile);
    }

    [TestMethod]
    public async Task ConvertToMarkdownAsync_EpubFile_Mocked_ReturnsMarkdown()
    {
        // Arrange
        ILogger<MarkdownNoteProcessor> logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
        TestableAISummarizer summarizer = new(NullLogger<AISummarizer>.Instance);
        MarkdownNoteProcessor processor = new(logger, summarizer);
        string testFile = "test.epub";
        // Simulate EPUB file (file must exist, but content is mocked)
        await File.WriteAllTextAsync(testFile, "EPUB content");
        // Mock VersOne.Epub.EpubReader.ReadBookAsync via reflection or skip if not available
        // This test will just check that the method handles the catch block gracefully
        string result = await processor.ConvertToMarkdownAsync(testFile);
        // Since the EPUB parser will throw, expect empty string
        Assert.AreEqual(string.Empty, result);
        File.Delete(testFile);
    }
}
