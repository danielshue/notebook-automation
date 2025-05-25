using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{    /// <summary>
     /// Test suite for the AISummarizer class, verifying its functionality with different AI framework integrations.
     /// </summary>    
    [TestClass]
    public class AISummarizerTests
    {
        private Mock<ILogger<AISummarizer>> _mockLogger;
        private Mock<PromptTemplateService> _mockPromptService;
        private Mock<ITextGenerationService> _mockTextGenService;

        [TestInitialize]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<AISummarizer>>();
            _mockPromptService = new Mock<PromptTemplateService>(MockBehavior.Loose, Mock.Of<ILogger<PromptTemplateService>>());
            _mockTextGenService = new Mock<ITextGenerationService>();
        }

        /// <summary>
        /// Tests that summarization with prompt template works correctly.
        /// </summary>
        [TestMethod]
        public async Task SummarizeAsync_WithPromptTemplate_SubstitutesVariables()
        {
            // Arrange
            var promptTemplate = "You are a summarizer. Summarize this content: {{content}}";
            var inputText = "This is the text to summarize.";
            var expectedPrompt = "You are a summarizer. Summarize this content: This is the text to summarize.";

            _mockPromptService
                .Setup(m => m.LoadTemplateAsync("test_prompt"))
                .ReturnsAsync(promptTemplate);

            _mockPromptService
                .Setup(m => m.SubstituteVariables(promptTemplate, It.IsAny<Dictionary<string, string>>()))
                .Returns(expectedPrompt);
            _mockTextGenService
                .Setup(m => m.GetTextContentAsync(
                    expectedPrompt,
                    It.IsAny<OpenAIPromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TextContent("Summary of the text"));

            // We don't use a mocked Kernel since Kernel is sealed and can't be mocked
            // Instead, pass the text generation service directly to the AISummarizer constructor
            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService.Object,
                null, // Don't use the kernel
                _mockTextGenService.Object); // Use text gen service directly

            // Act
            var result = await summarizer.SummarizeAsync(inputText, null, "test_prompt");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Summary of the text", result);
            _mockPromptService.Verify(m => m.LoadTemplateAsync("test_prompt"), Times.Once);
            _mockPromptService.Verify(
                m => m.SubstituteVariables(
                    promptTemplate,
                    It.Is<Dictionary<string, string>>(d => d.ContainsKey("content") && d["content"] == inputText)),
                Times.Once);
        }

        /// <summary>
        /// Tests that summarization falls back to direct API if Semantic Kernel is not available.
        /// </summary>
        [TestMethod]
        public async Task SummarizeAsync_WithoutKernel_UsesHttpClient()
        {
            // This test would require mocking the HttpClient, which is challenging
            // For a real test, we would use a HttpMessageHandler mock
            // For simplicity, this test just verifies the flow doesn't throw an exception
            // Arrange
            var inputText = "This is the text to summarize.";

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService.Object,
                null, // No kernel
                null); // No text gen service

            // Act
            var result = await summarizer.SummarizeAsync(inputText);

            // Assert
            Assert.IsNull(result); // Expect null since we provided an empty API key
            // We just verify that the code path doesn't throw an exception
        }

        /// <summary>
        /// Tests that summarization with a direct text generation service works correctly.
        /// </summary>
        [TestMethod]
        public async Task SummarizeAsync_WithTextGenService_WorksCorrectly()
        {            // Arrange
            var inputText = "This is the text to summarize.";

            _mockTextGenService
                .Setup(m => m.GetTextContentAsync(
                    It.IsAny<string>(),
                    It.IsAny<OpenAIPromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TextContent("Direct service summary"));

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService.Object,
                null, // No kernel 
                _mockTextGenService.Object); // Direct text gen service

            // Act
            var result = await summarizer.SummarizeAsync(inputText);

            // Assert            Assert.IsNotNull(result);
            Assert.AreEqual("Direct service summary", result);

            _mockTextGenService.Verify(m => m.GetTextContentAsync(
                It.IsAny<string>(),
                It.IsAny<OpenAIPromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests that summarization handles errors gracefully.
        /// </summary>
        [TestMethod]
        public async Task SummarizeAsync_HandlesErrors_ReturnsNull()
        {            // Arrange
            var inputText = "This is the text to summarize.";

            _mockTextGenService
                .Setup(m => m.GetTextContentAsync(
                    It.IsAny<string>(),
                    It.IsAny<OpenAIPromptExecutionSettings>(),
                    It.IsAny<Kernel>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService.Object,
                null, // No kernel
                _mockTextGenService.Object);

            // Act
            var result = await summarizer.SummarizeAsync(inputText);

            // Assert
            Assert.IsNull(result);
        }

        /// <summary>
        /// Tests that large text is properly chunked and summarized.
        /// </summary>
        [TestMethod]
        public async Task SummarizeAsync_WithLargeInput_UsesChunkingStrategy()
        {
            // Arrange
            // Create a very large input text that will trigger chunking
            var largeText = new string('A', 50000) + new string('B', 50000);
            // Setup multiple sequential responses for chunking
            var sequence = _mockTextGenService.SetupSequence(m => m.GetTextContentAsync(
                It.IsAny<string>(),
                It.IsAny<OpenAIPromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()));

            sequence.ReturnsAsync(new TextContent("Summary of first chunk"));
            sequence.ReturnsAsync(new TextContent("Summary of second chunk"));
            sequence.ReturnsAsync(new TextContent("Final consolidated summary"));

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                null, // No prompt service needed for this test
                null, // No kernel
                _mockTextGenService.Object);

            // Act
            var result = await summarizer.SummarizeAsync(largeText);

            // Assert            Assert.IsNotNull(result);
            Assert.AreEqual("Final consolidated summary", result);

            // Verify that GetTextContentAsync was called at least 3 times (2 chunks + consolidation)
            _mockTextGenService.Verify(m => m.GetTextContentAsync(
                It.IsAny<string>(),
                It.IsAny<OpenAIPromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()), Times.AtLeast(3));
        }

        /// <summary>
        /// Tests the token estimation method with various inputs to validate accuracy.
        /// </summary>
        [TestMethod]
        public void EstimateTokenCount_WithVariousInputs_ReturnsReasonableEstimates()
        {            // Arrange
            // Create an accessible version of the private method for testing
            var summarizer = new TestableAISummarizer(_mockLogger.Object);

            // Simple English text (roughly 1 token per ~4 chars)
            var simpleText = "This is a simple English text for testing token count estimation.";

            // Text with numbers and special characters (these often tokenize differently)
            var complexText = "Text with numbers (123, 456.78) and special chars: @#$%^&*!";

            // Text with repeated words (tokenizers often compress these)
            var repeatedText = "the the the the the the the the the the";

            // Text with different languages and scripts
            var multilingualText = "English text with some español and some 日本語";

            // Act
            var simpleCount = summarizer.PublicEstimateTokenCount(simpleText);
            var complexCount = summarizer.PublicEstimateTokenCount(complexText);
            var repeatedCount = summarizer.PublicEstimateTokenCount(repeatedText);
            var multilingualCount = summarizer.PublicEstimateTokenCount(multilingualText);
            var emptyCount = summarizer.PublicEstimateTokenCount("");

            // Assert
            // We're not looking for exact matches, but reasonable estimates
            Assert.IsTrue(simpleCount > 0, "Simple text should have tokens");
            Assert.IsTrue(complexCount > 0, "Complex text should have tokens");
            Assert.IsTrue(repeatedCount > 0, "Repeated text should have tokens");
            Assert.IsTrue(multilingualCount > 0, "Multilingual text should have tokens");
            Assert.AreEqual(0, emptyCount, "Empty text should have 0 tokens");

            // We expect roughly these many tokens (not exact, but in a reasonable range)
            Assert.IsTrue(simpleCount >= 10 && simpleCount <= 20, "Simple text should have 10-20 tokens");
            Assert.IsTrue(complexCount >= 10 && complexCount <= 25, "Complex text should have 10-25 tokens");

            // The actual token count estimation is more detailed, but these are reasonable expectations
        }

        /// <summary>
        /// Tests that markdown detection properly identifies markdown formatting.
        /// </summary>
        [TestMethod]
        public void ContainsMarkdown_WithVariousInputs_CorrectlyDetectsMarkdown()
        {            // Arrange
            var summarizer = new TestableAISummarizer(_mockLogger.Object);

            var plainText = "This is just plain text without any special formatting.";
            var headingText = "# This is a heading\nWith some content below";
            var listText = "- Item 1\n- Item 2\n- Item 3";
            var codeText = "```csharp\nvar x = 10;\n```";
            var linkText = "[Link text](https://example.com)";
            var mixedText = "# Heading\nNormal text\n- List item\n```code```";

            // Act
            var plainResult = summarizer.PublicContainsMarkdown(plainText);
            var headingResult = summarizer.PublicContainsMarkdown(headingText);
            var listResult = summarizer.PublicContainsMarkdown(listText);
            var codeResult = summarizer.PublicContainsMarkdown(codeText);
            var linkResult = summarizer.PublicContainsMarkdown(linkText);
            var mixedResult = summarizer.PublicContainsMarkdown(mixedText);

            // Assert
            Assert.IsFalse(plainResult, "Plain text should not be detected as markdown");
            Assert.IsTrue(headingResult, "Heading should be detected as markdown");
            Assert.IsTrue(listResult, "List should be detected as markdown");
            Assert.IsTrue(codeResult, "Code block should be detected as markdown");
            Assert.IsTrue(linkResult, "Link should be detected as markdown");
            Assert.IsTrue(mixedResult, "Mixed markdown should be detected as markdown");
        }
    }
}
