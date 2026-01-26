// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Models.Browse;

/// <summary>
/// Represents the result of a browse operation.
/// </summary>
/// <typeparam name="T">The type of data in the result.</typeparam>
public record BrowseResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the result data if successful.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Gets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static BrowseResult<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static BrowseResult<T> Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Represents the result of a browse operation with no data.
/// </summary>
public record BrowseResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if unsuccessful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static BrowseResult Success() => new()
    {
        IsSuccess = true
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static BrowseResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
