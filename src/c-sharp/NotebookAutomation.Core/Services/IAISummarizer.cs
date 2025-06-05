#nullable enable

namespace NotebookAutomation.Core.Services;
/// <summary>
/// Defines the contract for AI-powered text summarization services.
/// Provides methods for generating summaries with variable substitution and configurable prompt templates.
/// </summary>
/// <remarks>
/// This interface enables dependency injection and mocking for unit testing scenarios.
/// Implementations should support both direct summarization and chunked processing for large texts.
/// </remarks>
public interface IAISummarizer
{
    /// <summary>
    /// Generates an AI-powered summary for the given text using the best available AI framework.
    /// Automatically selects between direct summarization and chunked processing based on text length.
    /// Supports variable substitution for metadata augmentation and custom prompt templates.
    /// </summary>
    /// <param name="inputText">The text content to summarize. Cannot be null or empty.</param>
    /// <param name="variables">Optional dictionary of variables for prompt template substitution and metadata enhancement.
    /// Common variables include: course, type, onedrivePath, yamlfrontmatter.</param>
    /// <param name="promptFileName">Optional prompt template filename (without .md extension) to customize summarization behavior.
    /// Defaults to "final_summary_prompt" if not provided.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous summarization operation. The task result contains:
    /// - The generated summary text for successful operations
    /// - An empty string if the operation fails but the service is available
    /// - null if no AI service is available
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when inputText is null or empty</exception>
    Task<string?> SummarizeWithVariablesAsync(
        string inputText,
        Dictionary<string, string>? variables = null,
        string? promptFileName = null,
        CancellationToken cancellationToken = default);
}
