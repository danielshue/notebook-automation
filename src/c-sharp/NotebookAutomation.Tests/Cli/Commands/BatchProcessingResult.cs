// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Represents the result of a batch processing operation.
/// </summary>
/// <remarks>
/// Used as a simple result container for testing batch processing operations.
/// </remarks>
public class BatchProcessingResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the processing operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets an optional error message if the operation was not successful.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the count of processed items.
    /// </summary>
    public int ProcessedCount { get; set; }
}
