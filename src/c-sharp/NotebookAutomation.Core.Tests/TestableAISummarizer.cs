// <copyright file="TestableAISummarizer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/TestableAISummarizer.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
#nullable enable

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// A testable version of AISummarizer that exposes private methods for testing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the TestableAISummarizer class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
internal class TestableAISummarizer(ILogger<AISummarizer> logger) : AISummarizer(logger,
          new PromptTemplateService(
                  NullLogger<PromptTemplateService>.Instance,
                  new YamlHelper(NullLogger<YamlHelper>.Instance),
                  new AppConfig()),
          null!)
{
    private string summarizeAsyncResult = "[Simulated AI summary]";

    /// <summary>
    /// Sets up a predefined result to be returned by SummarizeAsync method.
    /// </summary>
    /// <param name="result">The result string to return from SummarizeAsync.</param>
    public void SetupSummarizeAsyncResult(string result) => this.summarizeAsyncResult = result;

    /// <summary>
    /// Override the SummarizeWithVariablesAsync method to return the predefined result in tests.
    /// </summary>
    /// <param name="inputText">The text to summarize (ignored in test).</param>
    /// <param name="variables">Optional variables to substitute in the prompt template.</param>
    /// <param name="promptFileName">Optional prompt file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The predefined summary result.</returns>
    public override Task<string?> SummarizeWithVariablesAsync(
        string inputText,
        Dictionary<string, string>? variables = null,
        string? promptFileName = null,
        CancellationToken cancellationToken = default)
    {
        // Return the configured test result, optionally including some variable information
        string result = this.summarizeAsyncResult;
        if (variables != null && variables.Count > 0)
        {
            // Append variable info to show substitution worked
            result += $" [Variables: {string.Join(", ", variables.Select(kvp => $"{kvp.Key}={kvp.Value}"))}]";
        }

        return Task.FromResult<string?>(result);
    }

    /// <summary>
    /// Exposes the private EstimateTokenCount method for testing.
    /// </summary>
    /// <param name="text">Text to estimate token count for.</param>
    /// <returns>Estimated token count.</returns>
    public int PublicEstimateTokenCount(string text)
    {
        // Use reflection to call the private method
        System.Reflection.MethodInfo methodInfo = typeof(AISummarizer).GetMethod(
            "EstimateTokenCount",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ?? throw new InvalidOperationException("EstimateTokenCount method not found in AISummarizer");
        object? result = methodInfo.Invoke(this, [text]);
        return result != null ? (int)result : 0;
    }
}
