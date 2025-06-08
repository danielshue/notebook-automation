// <copyright file="MockTextGenerationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/MockTextGenerationService.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
// Enable nullable reference types for this file
#nullable enable
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// A mock implementation of ITextGenerationService for testing AISummarizer.
/// </summary>
/// <remarks>
/// This class provides more extensive configuration options than previous test doubles
/// and specifically tracks the calls made to the service.
/// </remarks>
internal class MockTextGenerationService : ITextGenerationService
{
    /// <summary>
    /// Gets or sets the response that will be returned by the service.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// Gets or sets the responses to return for multiple consecutive calls.
    /// </summary>
    public Queue<string>? ResponseQueue { get; set; }

    /// <summary>
    /// Gets the last prompt that was sent to the service.
    /// </summary>
    public string? LastPrompt { get; private set; }

    /// <summary>
    /// Gets the number of times GetTextContentsAsync was called.
    /// </summary>
    public int CallCount { get; private set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets a flag indicating if the service should track calls.
    /// </summary>
    public bool TrackCalls { get; set; } = true;

    /// <summary>
    /// Gets or sets an exception to throw when called.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// Gets the attributes of the service.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    /// <summary>
    /// Returns streaming text content.
    /// </summary>
    /// <param name="prompt">The text prompt to process.</param>
    /// <param name="executionSettings">Optional execution settings.</param>
    /// <param name="kernel">Optional kernel for context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable sequence of streaming content.</returns>
    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (TrackCalls)
        {
            CallCount++;
            LastPrompt = prompt;
        }

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        return GetEmptyAsyncEnumerable();
    }

    private static async IAsyncEnumerable<StreamingTextContent> GetEmptyAsyncEnumerable()
    {
        await Task.Yield();
        yield break;
    }

    /// <summary>
    /// Returns a list of text content.
    /// </summary>
    /// <param name="prompt">The text prompt to process.</param>
    /// <param name="executionSettings">Optional execution settings.</param>
    /// <param name="kernel">Optional kernel for context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of text content responses.</returns>
    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (TrackCalls)
        {
            CallCount++;
            LastPrompt = prompt;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        string responseText;
        if (ResponseQueue != null && ResponseQueue.Count > 0)
        {
            responseText = ResponseQueue.Dequeue();
        }
        else
        {
            responseText = Response ?? string.Empty;
        }

        List<TextContent> result = [new(responseText)];
        return Task.FromResult<IReadOnlyList<TextContent>>(result);
    }

    /// <summary>
    /// Returns a single text content.
    /// </summary>
    /// <param name="prompt">The text prompt to process.</param>
    /// <param name="executionSettings">Optional execution settings.</param>
    /// <param name="kernel">Optional kernel for context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A text content response.</returns>
    public Task<TextContent> GetTextContentAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        if (TrackCalls)
        {
            CallCount++;
            LastPrompt = prompt;
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        string responseText;
        if (ResponseQueue != null && ResponseQueue.Count > 0)
        {
            responseText = ResponseQueue.Dequeue();
        }
        else
        {
            responseText = Response ?? string.Empty;
        }

        return Task.FromResult(new TextContent(responseText));
    }
}
