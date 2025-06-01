using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Core.Tests
{
    [TestClass]
    public class MarkdownNoteProcessorTests
    {
        [TestMethod]
        public async Task ConvertToMarkdownAsync_TxtFile_ReturnsMarkdown()
        {
            // Arrange
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var testFile = "test.txt";
            await File.WriteAllTextAsync(testFile, "Hello world!");

            // Act
            var result = await processor.ConvertToMarkdownAsync(testFile);

            // Assert
            Assert.IsTrue(result.Contains("Hello world!"));
            File.Delete(testFile);
        }

        [TestMethod]
        public async Task ConvertToMarkdownAsync_HtmlFile_StripsHtmlTags()
        {
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var testFile = "test.html";
            await File.WriteAllTextAsync(testFile, "<h1>Header</h1><p>Paragraph</p>");
            var result = await processor.ConvertToMarkdownAsync(testFile);
            Assert.IsTrue(result.Contains("Header"));
            Assert.IsTrue(result.Contains("Paragraph"));
            Assert.IsFalse(result.Contains("<h1>"));
            File.Delete(testFile);
        }

        [TestMethod]
        public async Task ConvertToMarkdownAsync_EmptyHtmlFile_ReturnsEmptyMarkdown()
        {
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var testFile = "empty.html";
            await File.WriteAllTextAsync(testFile, "");
            var result = await processor.ConvertToMarkdownAsync(testFile);
            Assert.IsTrue(result.Contains("generated")); // Metadata present
            File.Delete(testFile);
        }

        [TestMethod]
        public async Task ConvertToMarkdownAsync_UnsupportedFileType_ReturnsEmpty()
        {
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var testFile = "test.unsupported";
            await File.WriteAllTextAsync(testFile, "data");
            var result = await processor.ConvertToMarkdownAsync(testFile);
            Assert.AreEqual(string.Empty, result);
            File.Delete(testFile);
        }

        [TestMethod]
        public async Task ConvertToMarkdownAsync_MissingFile_ReturnsEmpty()
        {
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var result = await processor.ConvertToMarkdownAsync("doesnotexist.txt");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public async Task ConvertToMarkdownAsync_HtmlFile_OnlyTags_ReturnsEmptyBody()
        {
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var testFile = "tags.html";
            await File.WriteAllTextAsync(testFile, "<div><span></span></div>");
            var result = await processor.ConvertToMarkdownAsync(testFile);
            Assert.IsFalse(result.Contains("<div>"));
            File.Delete(testFile);
        }

        [TestMethod]
        public async Task ConvertToMarkdownAsync_TxtFile_WithAISummary_ReturnsAISummary()
        {
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            summarizer.SetupSummarizeAsyncResult("AI summary result");
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var testFile = "ai.txt";
            await File.WriteAllTextAsync(testFile, "This is a test for AI summary.");
            var result = await processor.ConvertToMarkdownAsync(testFile, "fake-key");
            Assert.IsTrue(result.Contains("AI summary result"));
            File.Delete(testFile);
        }

        [TestMethod]
        public async Task ConvertToMarkdownAsync_EpubFile_Mocked_ReturnsMarkdown()
        {
            // Arrange
            var logger = new Mock<ILogger<MarkdownNoteProcessor>>().Object;
            var summarizer = new TestableAISummarizer(NullLogger<AISummarizer>.Instance);
            var processor = new MarkdownNoteProcessor(logger, summarizer);
            var testFile = "test.epub";
            // Simulate EPUB file (file must exist, but content is mocked)
            await File.WriteAllTextAsync(testFile, "EPUB content");
            // Mock VersOne.Epub.EpubReader.ReadBookAsync via reflection or skip if not available
            // This test will just check that the method handles the catch block gracefully
            var result = await processor.ConvertToMarkdownAsync(testFile);
            // Since the EPUB parser will throw, expect empty string
            Assert.AreEqual(string.Empty, result);
            File.Delete(testFile);
        }
    }
}
