using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging.Abstractions;
using NotebookAutomation.Core.Tools.MarkdownGeneration;
using System.IO;

namespace NotebookAutomation.Core.Tests
{
    [TestClass]
    public class MarkdownNoteProcessorTests
    {
        [TestMethod]
        public async Task ConvertToMarkdownAsync_TxtFile_ReturnsMarkdown()
        {
            // Arrange
            var logger = NullLogger.Instance;
            var processor = new MarkdownNoteProcessor(logger);
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
