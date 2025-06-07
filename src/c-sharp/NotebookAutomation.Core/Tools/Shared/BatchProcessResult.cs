// <copyright file="BatchProcessResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Tools/Shared/BatchProcessResult.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tools.Shared;

/// <summary>
/// Represents the result and statistics of a batch document processing operation.
/// </summary>
/// <remarks>
/// <para>
/// This class provides detailed statistics about the batch processing operation, including:
/// <list type="bullet">
/// <item><description>Number of files successfully processed</description></item>
/// <item><description>Number of files that failed to process</description></item>
/// <item><description>Total processing time and summary generation time</description></item>
/// <item><description>Token usage statistics for AI summaries</description></item>
/// <item><description>Average processing times and token counts</description></item>
/// </list>
/// </para>
/// <para>
/// The class is designed for use in CLI or UI applications to provide user-friendly summaries
/// of batch processing results.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = new BatchProcessResult
/// {
///     Processed = 10,
///     Failed = 2,
///     TotalBatchTime = TimeSpan.FromMinutes(15),
///     TotalSummaryTime = TimeSpan.FromMinutes(5),
///     TotalTokens = 5000,
///     AverageFileTimeMs = 900,
///     AverageSummaryTimeMs = 300,
///     AverageTokens = 500,
///     Summary = "Processed 10 files with 2 failures."
/// };
/// Console.WriteLine(result.Summary);
/// </code>
/// </example>
public class BatchProcessResult
{
    /// <summary>
    /// Gets or sets number of files successfully processed.
    /// </summary>
    /// <remarks>
    /// This property represents the count of files that were processed successfully during the batch operation.
    /// </remarks>
    public int Processed { get; set; }

    /// <summary>
    /// Gets or sets number of files that failed to process.
    /// </summary>
    /// <remarks>
    /// This property represents the count of files that encountered errors and could not be processed.
    /// </remarks>
    public int Failed { get; set; }

    /// <summary>
    /// Gets or sets user-friendly summary string for CLI or UI output.
    /// </summary>
    /// <remarks>
    /// This property provides a concise summary of the batch processing results, suitable for display in
    /// command-line interfaces or user interfaces.
    /// </remarks>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets total batch processing time.
    /// </summary>
    /// <remarks>
    /// This property represents the total time taken to process all files in the batch operation.
    /// </remarks>
    public TimeSpan TotalBatchTime { get; set; }

    /// <summary>
    /// Gets or sets total time spent generating summaries.
    /// </summary>
    /// <remarks>
    /// This property represents the cumulative time spent generating AI summaries for all files in the batch.
    /// </remarks>
    public TimeSpan TotalSummaryTime { get; set; }

    /// <summary>
    /// Gets or sets total tokens used for all summaries.
    /// </summary>
    /// <remarks>
    /// This property represents the total number of tokens consumed by the AI summarizer during the batch operation.
    /// </remarks>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets average time per file in milliseconds.
    /// </summary>
    /// <remarks>
    /// This property represents the average time taken to process each file in the batch operation.
    /// </remarks>
    public double AverageFileTimeMs { get; set; }

    /// <summary>
    /// Gets or sets average summary time per file in milliseconds.
    /// </summary>
    /// <remarks>
    /// This property represents the average time spent generating summaries for each file in the batch.
    /// </remarks>
    public double AverageSummaryTimeMs { get; set; }

    /// <summary>
    /// Gets or sets average tokens per summary.
    /// </summary>
    /// <remarks>
    /// This property represents the average number of tokens consumed by the AI summarizer for each file in the batch.
    /// </remarks>
    public double AverageTokens { get; set; }
}
