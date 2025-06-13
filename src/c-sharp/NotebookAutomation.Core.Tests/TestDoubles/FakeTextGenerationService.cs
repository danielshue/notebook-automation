// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.TestDoubles;

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
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        string responseText;
        if (Responses != null && Responses.Count > 0)
        {
            responseText = Responses.Dequeue();
        }
        else
        {
            responseText = Response ?? string.Empty;
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
        if (ExceptionToThrow != null)
        {
            throw ExceptionToThrow;
        }

        string responseText;
        if (Responses != null && Responses.Count > 0)
        {
            responseText = Responses.Dequeue();
        }
        else
        {
            responseText = Response ?? string.Empty;
        }

        return Task.FromResult(new TextContent(responseText));
    }
}
