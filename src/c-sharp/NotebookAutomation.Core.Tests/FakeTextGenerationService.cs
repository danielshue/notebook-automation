// <copyright file="FakeTextGenerationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/FakeTextGenerationService.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
// Enable nullable reference types for this file
#nullable enable
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// A fake implementation of ITextGenerationService for testing AISummarizer.
/// </summary>
internal class FakeTextGenerationService : ITextGenerationService
{
    // Implement IAIService.Attributes
    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    // Implement ITextGenerationService.GetStreamingTextContentsAsync
    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default) =>

        // For testing, just return an empty async enumerable
        GetEmptyAsyncEnumerable();

    private static async IAsyncEnumerable<StreamingTextContent> GetEmptyAsyncEnumerable()
    {
        await Task.Yield();
        yield break;
    }

    /// <summary>
    /// Gets or sets the expected prompt text that the service expects to receive.
    /// </summary>
    public string? ExpectedPrompt { get; set; }

    /// <summary>
    /// Gets or sets the response text that the service will return.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets a queue of responses for sequential calls.
    /// </summary>
    public Queue<string>? Responses { get; set; }

    /// <summary>
    /// Gets or sets an exception to throw when the service is called.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// Returns the configured response text or throws the configured exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (this.ExceptionToThrow != null)
        {
            throw this.ExceptionToThrow;
        }

        string responseText;
        if (this.Responses != null && this.Responses.Count > 0)
        {
            responseText = this.Responses.Dequeue();
        }
        else
        {
            responseText = this.Response ?? string.Empty;
        }

        List<TextContent> result = [new(responseText)];
        return Task.FromResult<IReadOnlyList<TextContent>>(result);
    }

    /// <summary>
    /// Returns a single TextContent containing the configured response.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task<TextContent> GetTextContentAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (this.ExceptionToThrow != null)
        {
            throw this.ExceptionToThrow;
        }

        string responseText;
        if (this.Responses != null && this.Responses.Count > 0)
        {
            responseText = this.Responses.Dequeue();
        }
        else
        {
            responseText = this.Response ?? string.Empty;
        }

        return Task.FromResult(new TextContent(responseText));
    }
}
