// <copyright file="SimpleTextGenerationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/SimpleTextGenerationService.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
#nullable enable
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// A simple implementation of ITextGenerationService for testing.
/// </summary>
internal class SimpleTextGenerationService : ITextGenerationService
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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        // Throw exception if configured to do so
        if (this.ExceptionToThrow != null)
        {
            throw this.ExceptionToThrow;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // In a real service this would use the prompt to generate the response
        // For testing purposes, we just return the predefined response
        return Task.FromResult<IReadOnlyList<TextContent>>(
            [new TextContent(this.Response)]);
    }

    /// <summary>
    /// Gets streaming text contents from the service.
    /// </summary>
    /// <returns></returns>
    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) => throw new NotImplementedException("Streaming not used in tests");

    /// <summary>
    /// Gets aI Service attributes.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();
}
