// <copyright file="IAISummarizer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Services/IAISummarizer.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Services;

/// <summary>
/// Defines the contract for AI-powered text summarization services.
/// Provides methods for generating summaries with variable substitution and configurable prompt templates.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables dependency injection and mocking for unit testing scenarios.
/// Implementations should support:
/// <list type="bullet">
/// <item><description>Direct summarization for short texts</description></item>
/// <item><description>Chunked processing for large texts</description></item>
/// <item><description>Variable substitution for metadata augmentation</description></item>
/// <item><description>Customizable prompt templates for flexible summarization behavior</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var summarizer = serviceProvider.GetService&lt;IAISummarizer&gt;();
/// var summary = await summarizer.SummarizeWithVariablesAsync(
///     "This is a long text that needs summarization.",
///     new Dictionary&lt;string, string&gt; { { "course", "AI Basics" }, { "type", "lecture" } },
///     "custom_prompt",
///     CancellationToken.None);
///
/// Console.WriteLine(summary);
/// </code>
/// </example>
public interface IAISummarizer
{
    /// <summary>
    /// Generates an AI-powered summary for the given text using the best available AI framework.
    /// Automatically selects between direct summarization and chunked processing based on text length.
    /// Supports variable substitution for metadata augmentation and custom prompt templates.
    /// </summary>
    /// <param name="inputText">The text content to summarize. Cannot be null or empty.</param>
    /// <param name="variables">Optional dictionary of variables for prompt template substitution and metadata enhancement.
    /// Common variables include:
    /// <list type="bullet">
    /// <item><description>course: The course name</description></item>
    /// <item><description>type: The type of content (e.g., lecture, notes)</description></item>
    /// <item><description>onedrivePath: The OneDrive path for related files</description></item>
    /// <item><description>yamlfrontmatter: YAML metadata for the content</description></item>
    /// </list>
    /// </param>
    /// <param name="promptFileName">Optional prompt template filename (without .md extension) to customize summarization behavior.
    /// Defaults to "final_summary_prompt" if not provided.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task that represents the asynchronous summarization operation. The task result contains:
    /// <list type="bullet">
    /// <item><description>The generated summary text for successful operations</description></item>
    /// <item><description>An empty string if the operation fails but the service is available</description></item>
    /// <item><description>null if no AI service is available</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="inputText"/> is null or empty.</exception>
    Task<string?> SummarizeWithVariablesAsync(
        string inputText,
        Dictionary<string, string>? variables = null,
        string? promptFileName = null,
        CancellationToken cancellationToken = default);
}
