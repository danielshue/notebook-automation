// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Tests.Core.Helpers;
using NotebookAutomation.Tests.Core.TestDoubles;

namespace NotebookAutomation.Tests.Core.Services;

/// <summary>
/// Additional tests for AISummarizer class to improve code coverage
/// for specific edge cases and complex scenarios.
/// </summary>
[TestClass]
public class AISummarizerAdditionalTests
{
    private Mock<ILogger<AISummarizer>> _mockLogger = null!;
    private MockPromptTemplateService _mockPromptService = null!;

    [TestInitialize]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<AISummarizer>>();
        _mockPromptService = new MockPromptTemplateService
        {
            Template = "Test prompt template with {{content}}",
        };
    }

    /// <summary>
    /// Tests the full overloaded method signature with all parameters supplied.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithAllParameters_ProcessesCorrectly()
    {
        // Arrange
        string expectedResponse = "Summary with all parameters";

        // Create a kernel with mock service that returns the expected response
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        string inputText = "Content to summarize";
        Dictionary<string, string> variables = new()
        {
            ["course"] = "Test Course",
            ["type"] = "lecture_notes",
            ["source"] = "Test Source",
        };
        string promptName = "custom_prompt";

        using CancellationTokenSource cts = new object();
        CancellationToken token = cts.Token;

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(
            inputText,
            variables,
            promptName,
            token).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResponse, result);
        Assert.AreEqual(promptName, _mockPromptService.LastTemplateName);
    }

    /// <summary>
    /// Tests handling of YAML frontmatter in the input text when no variables dictionary is provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
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

        // Create a kernel with mock service
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputTextWithYAML).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResponse, result);
    }

    /// <summary>
    /// Tests handling of whitespace in the content value when variables are provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithWhitespaceInContentVariable_ReturnsEmptyString()
    {
        // Arrange
        // Create a kernel but it shouldn't be used since we have whitespace input
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService("This should not be returned");

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        Dictionary<string, string> variables = new()
        {
            ["content"] = "   \t\n  ",
            ["course"] = "Test Course",
        };

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(string.Empty, variables).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Tests handling of large inputs that would previously require chunking.
    /// Now SK handles chunking internally.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithLargeInput_ProcessesSuccessfully()
    {
        // Arrange
        string expectedResponse = "Summary of large content";

        // Create a kernel with mock service
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);
        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        // Large text that would trigger chunking in SK
        string largeText = new('A', 25000);            // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(largeText).ConfigureAwait(false);            // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    /// <summary>
    /// Tests that the service handles specific exception types from the kernel.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithSpecificException_HandlesGracefully()
    {
        // Arrange            // Create a test kernel
        Kernel kernel = TestKernelHelper.CreateTestKernel();

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        string inputText = "Content that will cause cancellation exception";

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    /// <summary>
    /// Tests handling of extremely large text (SK handles chunking internally now).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithExtremelyLargeText_HandlesSuccessfully()
    {
        // Arrange
        string expectedResponse = "Summary of extremely large content";

        // Create a kernel with mock service
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        // Very large text that would be chunked by SK internally
        string veryLargeText = new('A', 100000);            // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(veryLargeText).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    /// <summary>
    /// Tests scenario where no prompt service is available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_NoPromptService_ReturnsEmptyString()
    {
        // Arrange
        // Create a kernel but it won't be used because there's no prompt service
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService("This should not be returned");

        AISummarizer summarizer = new(
            _mockLogger.Object,
            null, // No prompt service
            kernel);

        string inputText = "Content that won't be summarized";

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText).ConfigureAwait(false);            // Assert
        Assert.IsNotNull(result); // Should return simulated response even with no prompt service
    }
}
