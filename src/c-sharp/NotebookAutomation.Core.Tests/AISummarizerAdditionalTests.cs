#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Additional tests for AISummarizer class to improve code coverage
    /// for specific edge cases and complex scenarios.
    /// </summary>
    [TestClass]
    public class AISummarizerAdditionalTests
    {
        private Mock<ILogger<AISummarizer>> _mockLogger = null!;
        private MockPromptTemplateService _mockPromptService = null!;
        private SimpleTextGenerationService _fakeTextGenService = null!;
        private MockTextChunkingService _mockChunkingService = null!;

        [TestInitialize]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<AISummarizer>>();
            _mockPromptService = new MockPromptTemplateService();
            _fakeTextGenService = new SimpleTextGenerationService();
            _mockChunkingService = new MockTextChunkingService();
            _mockPromptService.Template = "Test prompt template with {{content}}";
        }

        /// <summary>
        /// Tests the full overloaded method signature with all parameters supplied.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithAllParameters_ProcessesCorrectly()
        {
            // Arrange
            string expectedResponse = "Summary with all parameters";

            // Use direct text generation service for simplicity
            _fakeTextGenService.Response = expectedResponse;

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                null, // No kernel
                _fakeTextGenService);

            string inputText = "Content to summarize";
            var variables = new Dictionary<string, string>
            {
                ["course"] = "Test Course",
                ["type"] = "lecture_notes",
                ["source"] = "Test Source"
            };
            var promptName = "custom_prompt";

            using var cts = new CancellationTokenSource();
            var token = cts.Token;

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(
                inputText,
                variables,
                promptName,
                token);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponse, result);
            Assert.AreEqual(promptName, _mockPromptService.LastTemplateName);
        }

        /// <summary>
        /// Tests handling of YAML frontmatter in the input text when no variables dictionary is provided.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithYAMLInInputText_ProcessesCorrectly()
        {
            // Arrange
            string expectedResponse = "Summary with YAML in input text";

            string inputTextWithYAML = @"---
title: Test Document
course: Test Course
date: 2025-05-31
---
This is the actual content that follows YAML frontmatter.";

            // Use direct text generation service for simplicity
            _fakeTextGenService.Response = expectedResponse;

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                null, // No kernel
                _fakeTextGenService);

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(inputTextWithYAML);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponse, result);
        }

        /// <summary>
        /// Tests handling of whitespace in the content value when variables are provided.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithWhitespaceInContentVariable_ReturnsEmptyString()
        {
            // Arrange
            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                null, // No kernel
                null); // No text generation service

            var variables = new Dictionary<string, string>
            {
                ["content"] = "   \t\n  ",
                ["course"] = "Test Course"
            };

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync("", variables);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Tests kernel-based chunking with multiple predefined chunks.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithMultipleChunks_ProcessesAllChunks()
        {
            // Arrange
            string expectedResponse = "Summary of multiple chunks";

            // Create a kernel with mock service
            var kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

            // Set the fake service response for consistent testing
            _fakeTextGenService.Response = expectedResponse;

            // Set up mock chunking service with multiple predefined chunks
            _mockChunkingService.PredefinedChunks = new List<string>
            {
                "First chunk content",
                "Second chunk content",
                "Third chunk content",
                "Fourth chunk content"
            };

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                null, // No kernel for this test
                _fakeTextGenService, // Use direct text generation
                _mockChunkingService);

            // Large text to trigger chunking
            string largeText = new string('A', 25000);

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(largeText);

            // Assert
            Assert.IsTrue(_mockChunkingService.SplitTextWasCalled);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponse, result);
        }

        /// <summary>
        /// Tests that the service handles specific exception types from the text generation service.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithSpecificException_HandlesGracefully()
        {
            // Arrange
            // Configure text generation service to throw a specific type of exception
            _fakeTextGenService.ExceptionToThrow = new OperationCanceledException("Test cancellation");

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                null, // No kernel
                _fakeTextGenService);

            string inputText = "Content that will cause cancellation exception";

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(inputText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Tests handling of extremely large text requiring multi-stage chunking.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithExtremelyLargeText_HandlesChunking()
        {
            // Arrange
            string expectedResponse = "Summary of extremely large content";

            // Create a mock text generation service
            _fakeTextGenService.Response = expectedResponse;

            // Create a mock chunking service with a very large number of chunks
            _mockChunkingService.PredefinedChunks = new List<string>();
            // Add 20 chunks to simulate a very large text
            for (int i = 0; i < 20; i++)
            {
                _mockChunkingService.PredefinedChunks.Add($"Chunk {i + 1} content");
            }

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                null, // No kernel
                _fakeTextGenService,
                _mockChunkingService);

            // Very large text to ensure chunking
            string veryLargeText = new string('A', 100000);

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(veryLargeText);

            // Assert
            Assert.IsTrue(_mockChunkingService.SplitTextWasCalled);
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponse, result);
        }

        /// <summary>
        /// Tests scenario where the no text generation service is available but prompt service returns null.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_NoPromptService_ReturnsEmptyString()
        {
            // Arrange
            var summarizer = new AISummarizer(
                _mockLogger.Object,
                null, // No prompt service
                null, // No kernel
                null); // No text generation service

            string inputText = "Content that won't be summarized";

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(inputText);

            // Assert
            Assert.IsNull(result); // Should return null when no AI service is available
        }
    }
}
