using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging.Abstractions;
using NotebookAutomation.Core.Tools.MarkdownGeneration;
using NotebookAutomation.Core.Services;
using System.IO;
using Moq;
using Microsoft.Extensions.Logging;

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
    }
}
