// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Services;

/// <summary>
/// Tests for AISummarizer specifically focused on Kernel integration.
/// </summary>
[TestClass]
public class AISummarizerWithKernelTests
{
    private Mock<ILogger<AISummarizer>> _mockLogger = null!;
    private Mock<IPromptService> _mockPromptService = null!;

    [TestInitialize]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<AISummarizer>>();
        _mockPromptService = new Mock<IPromptService>();
        _mockPromptService.Setup(p => p.LoadTemplateAsync(It.IsAny<string>()))
            .Returns(Task.FromResult("Template {{content}}"));
        _mockPromptService
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
            _mockLogger.Object,
            _mockPromptService.Object,
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
            _mockLogger.Object,
            _mockPromptService.Object,
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
            _mockLogger.Object,
            _mockPromptService.Object,
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
            _mockLogger.Object,
            _mockPromptService.Object,
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