// <copyright file="TestSummarizerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/TestSummarizerTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// A simple implementation of ITextGenerationService for testing.
/// </summary>
internal class TestTextGenerationService : ITextGenerationService
{
    public IReadOnlyDictionary<string, object> Attributes => new Dictionary<string, object>();

    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings executionSettings = null,

        Kernel kernel = null,
        CancellationToken cancellationToken = default)
    {
        List<TextContent> result = [new("Mock summary")];
        return Task.FromResult<IReadOnlyList<TextContent>>(result);
    }

    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings executionSettings = null,
        Kernel kernel = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException("Streaming is not used in this test");

    // This is the method that AISummarizer actually uses
    public static Task<TextContent> GetTextContentAsync(
        string prompt,
        OpenAIPromptExecutionSettings settings,
        Kernel kernel = null,
        CancellationToken cancellationToken = default) => Task.FromResult(new TextContent("Mock summary"));

    public static Task<TextContent> GetTextContentAsync(
        string prompt,
        PromptExecutionSettings settings = null,
        Kernel kernel = null,
        CancellationToken cancellationToken = default) => Task.FromResult(new TextContent("Mock summary"));
}

/// <summary>
/// Tests for verifying that prompt logging works correctly in AISummarizer.
/// </summary>
[TestClass]
internal class TestSummarizerTests
{
    private readonly Mock<ILogger<AISummarizer>> loggerMock;
    private readonly PromptTemplateService promptTemplateService;
    private readonly AISummarizer summarizer;

    public TestSummarizerTests()
    {
        this.loggerMock = new Mock<ILogger<AISummarizer>>();

        // Set up paths for prompt templates
        string projectDir = Directory.GetCurrentDirectory();
        string promptsPath = Path.GetFullPath(Path.Combine(projectDir, "..", "..", "..", "..", "..", "prompts"));

        // Create AppConfig with paths
        PathsConfig pathsConfig = new()
        {
            PromptsPath = promptsPath,
        };

        AppConfig appConfig = new()
        {
            Paths = pathsConfig,
        };        // Create a real PromptTemplateService with the actual prompts directory
        this.promptTemplateService = new PromptTemplateService(
            Mock.Of<ILogger<PromptTemplateService>>(),
            new YamlHelper(Mock.Of<ILogger<YamlHelper>>()),
            appConfig);

        // Create a test implementation of ITextGenerationService
        _ = new

        // Create a test implementation of ITextGenerationService
        TestTextGenerationService();

        // Create AISummarizer with dependencies
        this.summarizer = new AISummarizer(
            this.loggerMock.Object,
            this.promptTemplateService,
            null);
    }
}
