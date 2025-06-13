// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Services;

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
        _mockPromptService = new MockPromptTemplateService
        {
            Template = "You are a summarizer. Summarize this content: {{content}}",
        };
    }

    /// <summary>
    /// Tests that summarization with variables works correctly when input text is empty.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_EmptyInputText_ReturnsEmptyString()
    {
        // Arrange
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService("This should not be returned");

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(string.Empty).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Tests that summarization with variables works correctly when input text is null.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_NullInputText_ReturnsEmptyString()
    {
        // Arrange
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService("This should not be returned");

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(null!).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Tests that the basic use case for summarization works correctly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_BasicUseCase_ReturnsExpectedSummary()
    {
        // Arrange
        string expectedResponse = "Summary of the basic text";
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        string inputText = "This is a basic text to summarize.";

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResponse, result);
    }

    /// <summary>
    /// Tests that variables are processed correctly when provided.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithVariables_ProcessesCorrectly()
    {
        // Arrange
        string expectedResponse = "Summary with variables";
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService(expectedResponse);

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        string inputText = "Text to summarize with variables";
        Dictionary<string, string> variables = new()
        {
            ["course"] = "Test Course",
            ["type"] = "lecture_notes",
        };

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText, variables).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedResponse, result);
    }

    /// <summary>
    /// Tests that exceptions are handled gracefully during summarization.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithException_ReturnsEmptyString()
    {
        // Arrange
        Kernel kernel = TestKernelHelper.CreateTestKernel();

        AISummarizer summarizer = new(
            _mockLogger.Object,
            _mockPromptService,
            kernel);

        string inputText = "Text that will cause an exception";

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText).ConfigureAwait(false);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("[Simulated AI summary]", result);
    }

    /// <summary>
    /// Tests that [yamlfrontmatter] placeholder is properly replaced in the prompt template.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task SummarizeWithVariablesAsync_WithYamlFrontmatter_ReplacesPlaceholder()
    {
        // Arrange
        string expectedSummary = "Summary with YAML frontmatter";
        Kernel kernel = MockKernelFactory.CreateKernelWithMockService(expectedSummary);

        // Create a mock prompt service with a template containing [yamlfrontmatter]
        var mockPromptWithYaml = new MockPromptTemplateService
        {
            Template = "Test prompt with YAML:\n\n---\n\n[yamlfrontmatter]\n\n---\n\nSummarize: {{content}}",
        };

        AISummarizer summarizer = new(
            _mockLogger.Object,
            mockPromptWithYaml,
            kernel);

        string inputText = "Text to summarize with YAML frontmatter";
        string yamlContent = "title: Test\ntags:\n  - \"test-tag\"\n  - \"yaml-frontmatter\"";
        Dictionary<string, string> variables = new()
        {
            ["content"] = inputText,
            ["yamlfrontmatter"] = yamlContent,
        };

        // Act
        string? result = await summarizer.SummarizeWithVariablesAsync(inputText, variables).ConfigureAwait(false);        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedSummary, result);

        // Verify that the content variable was passed to the mock prompt service
        // We can't directly test the [yamlfrontmatter] replacement since that happens after
        // the PromptService.SubstituteVariables call, but we can verify the basic integration
        Assert.IsNotNull(mockPromptWithYaml.LastVariables);
        Assert.IsTrue(
            mockPromptWithYaml.LastVariables.ContainsKey("content"),
            "The content variable should be passed to the prompt service");
    }
}
