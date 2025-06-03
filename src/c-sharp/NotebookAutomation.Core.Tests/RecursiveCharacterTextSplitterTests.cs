using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services.Text;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// Test suite for the RecursiveCharacterTextSplitter class.
/// </summary>
[TestClass]
public class RecursiveCharacterTextSplitterTests
{
    private Mock<ILogger> _mockLogger = null!;

    [TestInitialize]
    public void SetUp() => _mockLogger = new Mock<ILogger>();

    /// <summary>
    /// Tests that the splitter handles empty text correctly.
    /// </summary>
    [TestMethod]
    public void SplitText_WithEmptyText_ReturnsEmptyList()
    {
        // Arrange
        RecursiveCharacterTextSplitter splitter = new(_mockLogger.Object);

        // Act
        System.Collections.Generic.List<string> result = splitter.SplitText(string.Empty);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Tests that the splitter handles small text that fits within a single chunk.
    /// </summary>
    [TestMethod]
    public void SplitText_WithSmallText_ReturnsSingleChunk()
    {
        // Arrange
        RecursiveCharacterTextSplitter splitter = new(_mockLogger.Object, 1000);
        string smallText = "This is a small text that should fit in a single chunk.";

        // Act
        System.Collections.Generic.List<string> result = splitter.SplitText(smallText);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(smallText, result[0]);
    }

    /// <summary>
    /// Tests that the splitter correctly handles code blocks in markdown.
    /// </summary>
    [TestMethod]
    public void SplitText_WithCodeBlocks_KeepsCodeBlocksTogether()
    {
        // Arrange
        RecursiveCharacterTextSplitter splitter = RecursiveCharacterTextSplitter.CreateForMarkdown(_mockLogger.Object, 200, 50);

        string markdown = "# Code Example\n\n" +
                         "Here is an example code block:\n\n" +
                         "```csharp\n" +
                         "public class Example\n" +
                         "{\n" +
                         "    public void DoSomething()\n" +
                         "    {\n" +
                         "        Console.WriteLine(\"Hello, World!\");\n" +
                         "    }\n" +
                         "}\n" +
                         "```\n\n" +
                         "And some more text after the code block.";

        // Act
        System.Collections.Generic.List<string> result = splitter.SplitText(markdown);

        // Assert

        // Find the chunk that has the code block
        string codeChunk = result.FirstOrDefault(chunk => chunk.Contains("```csharp"));

        // Make sure the code block is kept together
        Assert.IsNotNull(codeChunk);
        Assert.IsTrue(codeChunk.Contains("public class Example"));
        Assert.IsTrue(codeChunk.Contains("Console.WriteLine"));
        Assert.IsTrue(codeChunk.Contains("```\n\n"));
    }

    /// <summary>
    /// Tests that the splitter correctly handles very large text by splitting it into many chunks.
    /// </summary>
    [TestMethod]
    public void SplitText_WithVeryLargeText_CreatesMultipleChunks()
    {
        // Arrange
        int maxTokens = 100;
        RecursiveCharacterTextSplitter splitter = new(_mockLogger.Object, maxTokens, 20);

        // Create a large text with many paragraphs
        System.Text.StringBuilder largeText = new();
        for (int i = 0; i < 50; i++)
        {
            largeText.AppendLine($"This is paragraph {i + 1} with some content that should be considered as one logical unit.");
            largeText.AppendLine(); // Add a blank line between paragraphs
        }

        // Act
        System.Collections.Generic.List<string> result = splitter.SplitText(largeText.ToString());

        // Assert
        Assert.IsTrue(result.Count > 5); // We should have several chunks

        // Each chunk should be reasonable in size
        foreach (string chunk in result)
        {
            // Verify each chunk is within a reasonable size range
            // We're using a simple approximation for token count here
            int approxTokens = chunk.Split(' ').Length;

            // Allow some tolerance above maxTokens due to overlap and chunk boundaries
            Assert.IsTrue(approxTokens <= maxTokens * 2, $"Chunk size {approxTokens} exceeds twice the max token limit {maxTokens}");
        }
    }
}
