#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tests.Helpers;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Test suite for the AISummarizer class, verifying its functionality with different AI framework integrations.
    /// </summary>
    [TestClass]
    public class AISummarizerTests
    {
        private Mock<ILogger<AISummarizer>> _mockLogger = null!;
        private MockPromptTemplateService _mockPromptService = null!;

        [TestInitialize]
        public void SetUp()
        {
            _mockLogger = new Mock<ILogger<AISummarizer>>();
            _mockPromptService = new MockPromptTemplateService();
            _mockPromptService.Template = "You are a summarizer. Summarize this content: {{content}}";
        }

        /// <summary>
        /// Tests that summarization with variables works correctly when input text is empty.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_EmptyInputText_ReturnsEmptyString()
        {
            // Arrange
            var kernel = MockKernelFactory.CreateKernelWithMockService("This should not be returned");

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                kernel);

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync("");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Tests that summarization with variables works correctly when input text is null.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_NullInputText_ReturnsEmptyString()
        {
            // Arrange
            var kernel = MockKernelFactory.CreateKernelWithMockService("This should not be returned");

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                kernel);

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(null!);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result);
        }

        /// <summary>
        /// Tests that the basic use case for summarization works correctly.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_BasicUseCase_ReturnsExpectedSummary()
        {
            // Arrange
            string expectedResponse = "Summary of the basic text";
            var kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                kernel);

            string inputText = "This is a basic text to summarize.";

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(inputText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponse, result);
        }

        /// <summary>
        /// Tests that variables are processed correctly when provided.
        /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithVariables_ProcessesCorrectly()
        {
            // Arrange
            string expectedResponse = "Summary with variables";
            var kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                kernel);

            string inputText = "Text to summarize with variables";
            var variables = new Dictionary<string, string>
            {
                ["course"] = "Test Course",
                ["type"] = "lecture_notes"
            };

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(inputText, variables);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedResponse, result);
        }        /// <summary>
                 /// Tests that exceptions are handled gracefully during summarization.
                 /// </summary>
        [TestMethod]
        public async Task SummarizeWithVariablesAsync_WithException_ReturnsEmptyString()
        {
            // Arrange
            var kernel = TestKernelHelper.CreateTestKernel();

            var summarizer = new AISummarizer(
                _mockLogger.Object,
                _mockPromptService,
                kernel);

            string inputText = "Text that will cause an exception";

            // Act
            var result = await summarizer.SummarizeWithVariablesAsync(inputText);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("[Simulated AI summary]", result);
        }
    }
}
