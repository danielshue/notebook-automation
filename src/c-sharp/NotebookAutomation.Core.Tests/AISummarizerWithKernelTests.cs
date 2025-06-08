// <copyright file="AISummarizerWithKernelTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/AISummarizerWithKernelTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
#nullable enable
using Microsoft.SemanticKernel;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// Tests for AISummarizer specifically focused on Kernel integration.
/// </summary>
[TestClass]
public class AISummarizerWithKernelTests
{
    private Mock<ILogger<AISummarizer>> mockLogger = null!;
    private Mock<IPromptService> mockPromptService = null!;

    [TestInitialize]
    public void SetUp()
    {
        mockLogger = new Mock<ILogger<AISummarizer>>();
        mockPromptService = new Mock<IPromptService>();
        mockPromptService.Setup(p => p.LoadTemplateAsync(It.IsAny<string>()))
            .Returns(Task.FromResult("Template {{content}}"));
        mockPromptService
            .Setup(p => p.ProcessTemplateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns((string template, Dictionary<string, string>? vars) =>
            {
                string result = template;
                if (vars != null && vars.TryGetValue("content", out string? value))
                {
                    result = template.Replace("{{content}}", value);
                }

                return Task.FromResult(result);
            });
    }

    /// <summary>
    /// Tests that the summarizer works correctly with a real Kernel instance.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithRealKernel_ProcessesCorrectly()
    {
        // Arrange
        string expectedSummary = "Kernel-generated summary";            // Create a test kernel with simulated response
        Kernel kernel = TestKernelHelper.CreateKernelWithSimulatedResponse(expectedSummary);

        AISummarizer summarizer = new(
            mockLogger.Object,
            mockPromptService.Object,
            kernel);

        string inputText = "Text to be summarized by the kernel";

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    /// <summary>
    /// Tests that the summarizer handles null Kernel gracefully.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithNullKernel_ReturnsNull()
    {
        // Arrange
        AISummarizer summarizer = new(
            mockLogger.Object,
            mockPromptService.Object,
            null); // Null kernel

        string inputText = "Text that won't be summarized due to null kernel";

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText).ConfigureAwait(false);

        // Assert
        Assert.IsNull(result);
    }

    /// <summary>
    /// Tests that the summarizer correctly utilizes the Kernel for function calls.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>        [TestMethod]
    public async Task SummarizeWithVariablesAsync_KernelFunctionCalls_ProcessedCorrectly()
    {
        // Arrange
        string expectedSummary = "Summary from function call";            // Create a test kernel with simulated response
        Kernel kernel = TestKernelHelper.CreateKernelWithSimulatedResponse(expectedSummary);

        AISummarizer summarizer = new(
            mockLogger.Object,
            mockPromptService.Object,
            kernel);

        string inputText = "Text for kernel function call";

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    /// <summary>
    /// Tests summarization with large texts using Semantic Kernel chunking capabilities.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_LargeText_UsesSemanticKernel()
    {
        // Arrange
        string expectedSummary = "Summary of large text";

        // Create a kernel specifically for handling large texts
        Kernel kernel = MockKernelFactory.CreateKernelForChunkingTests(expectedSummary);

        AISummarizer summarizer = new(
            mockLogger.Object,
            mockPromptService.Object,
            kernel);

        // Generate a large text that will require chunking
        string largeText = new('X', 50000);

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(largeText).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);
    }
}
