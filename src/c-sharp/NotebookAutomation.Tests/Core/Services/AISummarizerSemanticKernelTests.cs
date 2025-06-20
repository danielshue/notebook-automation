// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Core.Services;

/// <summary>
/// Unit tests for the AISummarizer Semantic Kernel function creation fixes.
/// These tests focus on the template variable substitution logic without mocking the Kernel.
/// </summary>
[TestClass]
public class AISummarizerSemanticKernelTests
{    /// <summary>
     /// Tests that SummarizeWithChunkingAsync handles null kernel gracefully.
     /// </summary>
    [TestMethod]
    public async Task SummarizeWithChunkingAsync_WithNullKernel_ShouldReturnSimulatedSummary()
    {
        // Arrange
        var logger = new Mock<ILogger<AISummarizer>>();
        var mockPromptService = new();
        var mockTimeoutConfig = new TimeoutConfig();

        var summarizer = new AISummarizer(logger.Object, mockPromptService.Object, null, null, mockTimeoutConfig);

        var variables = new Dictionary<string, string>
        {
            ["onedrivePath"] = "/test/path",
            ["course"] = "Test Course"
        };

        // Act
        string? result = await summarizer.SummarizeWithChunkingAsync(
            "This is a test content that should be chunked",
            "test prompt",
            variables,
            CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);

        // Verify that warning was logged about missing kernel
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Semantic kernel is not available")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify that prompt templates are NOT loaded when kernel is null (early return)
        mockPromptService.Verify(s => s.LoadTemplateAsync(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// Integration test that verifies the complete flow works end-to-end with null kernel.
    /// This tests the chunking logic and fallback behavior.
    /// </summary>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_LargeText_ShouldUseChunkingFlow()
    {
        // Arrange
        var logger = new Mock<ILogger<AISummarizer>>();
        var mockPromptService = new();

        // Create a large text that will trigger chunking (over 8000 characters)
        string largeText = new string('A', 9000);

        // Mock template loading
        mockPromptService
            .Setup(s => s.LoadTemplateAsync(It.IsAny<string>()))
            .ReturnsAsync("Test template with {{$input}}");

        mockPromptService
            .Setup(s => s.SubstituteVariables(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Returns<string, Dictionary<string, string>>((template, vars) => template);

        var summarizer = new AISummarizer(logger.Object, mockPromptService.Object, null, null, null);

        var variables = new Dictionary<string, string>
        {
            ["course"] = "Test Course",
            ["onedrivePath"] = "/test/path"
        };

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(
            largeText,
            variables,
            "test_prompt");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);

        // Verify that chunking was triggered (information logged)
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Input text is large") && o.ToString()!.Contains("Using chunking strategy")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that the AISummarizer handles template variable substitution correctly
    /// by verifying the prompt service is called with the right parameters.
    /// </summary>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithVariables_ShouldCallSubstituteVariables()
    {
        // Arrange
        var logger = new Mock<ILogger<AISummarizer>>();
        var mockPromptService = new();

        string originalTemplate = "Course: {{$course}}, Path: {{$onedrivePath}}, Content: {{$content}}";
        string substitutedTemplate = "Course: Test Course, Path: /test/path, Content: {{$content}}";

        mockPromptService
            .Setup(s => s.LoadTemplateAsync("test_prompt"))
            .ReturnsAsync(originalTemplate);

        mockPromptService
            .Setup(s => s.SubstituteVariables(originalTemplate, It.IsAny<Dictionary<string, string>>()))
            .Returns(substitutedTemplate);

        var summarizer = new AISummarizer(logger.Object, mockPromptService.Object, null, null, null);

        var variables = new Dictionary<string, string>
        {
            ["course"] = "Test Course",
            ["onedrivePath"] = "/test/path"
        };

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(
            "Small text",
            variables,
            "test_prompt");

        // Assert
        Assert.IsNull(result); // Should be null because no kernel and text is small (no chunking)

        // Verify SubstituteVariables was called with correct parameters
        mockPromptService.Verify(
            s => s.SubstituteVariables(
                originalTemplate,
                It.Is<Dictionary<string, string>>(dict =>
                    dict["course"] == "Test Course" &&
                    dict["onedrivePath"] == "/test/path")),
            Times.Once);
    }
}
