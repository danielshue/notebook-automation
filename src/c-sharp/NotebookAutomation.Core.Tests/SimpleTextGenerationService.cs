#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// A simple implementation of ITextGenerationService for testing.
/// </summary>
public class SimpleTextGenerationService : ITextGenerationService
{
    /// <summary>
    /// Gets or sets the response this mock will return.
    /// </summary>
    public string Response { get; set; } = "Mock AI response";

    /// <summary>
    /// Gets or sets the exception to throw during method calls.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; } = null;

    /// <summary>
    /// Gets text contents from the service.
    /// </summary>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        // Throw exception if configured to do so
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // In a real service this would use the prompt to generate the response
        // For testing purposes, we just return the predefined response
        return Task.FromResult<IReadOnlyList<TextContent>>(
            new[] { new TextContent(Response) });
    }

    /// <summary>
    /// Gets streaming text contents from the service.
    /// </summary>
    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException("Streaming not used in tests");

    /// <summary>
    /// AI Service attributes
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();
}
