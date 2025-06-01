using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Unit tests for the HtmlToMarkdownConverter class.
    /// </summary>
    [TestClass]
    public class HtmlToMarkdownConverterTests
    {
        [TestMethod]
        public void Convert_ValidHtml_ReturnsMarkdown()
        {
            var mockLogger = new Mock<ILogger>();
            var converter = new HtmlToMarkdownConverter(mockLogger.Object);
            string html = "<h1>Header</h1><p>This is <strong>bold</strong>.</p>";
            string markdown = converter.Convert(html);
            Assert.IsTrue(markdown.Contains("# Header"));
            Assert.IsTrue(markdown.Contains("**bold**"));
        }

        [TestMethod]
        public void Convert_InvalidHtml_LogsErrorAndReturnsOriginal()
        {
            var mockLogger = new Mock<ILogger>();
            var converter = new HtmlToMarkdownConverter(mockLogger.Object);
            string html = null;
            string result = converter.Convert(html);
            Assert.IsNull(result);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
