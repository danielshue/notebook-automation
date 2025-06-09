// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Represents the progress of document processing, including the current file and status.
/// </summary>
/// <remarks>
/// Initializes a new instance of the DocumentProcessingProgressEventArgs class.
/// </remarks>
/// <param name="filePath">The path of the file being processed.</param>
/// <param name="status">The current processing status message.</param>
/// <param name="currentFile">The current file index being processed.</param>
/// <param name="totalFiles">The total number of files to process.</param>
public class DocumentProcessingProgressEventArgs(string filePath, string status, int currentFile, int totalFiles) : EventArgs
{
    /// <summary>
    /// Gets the path of the file being processed.
    /// </summary>
    /// <remarks>
    /// This property provides the full path to the file currently being processed.
    /// It is useful for logging and tracking the progress of document processing tasks.
    /// </remarks>
    public string FilePath { get; } = filePath;

    /// <summary>
    /// Gets the current processing status message.
    /// </summary>
    /// <remarks>
    /// This property contains a descriptive message about the current status of the
    /// document processing operation, such as "Processing" or "Completed".
    /// </remarks>
    public string Status { get; } = status;

    /// <summary>
    /// Gets the current file index being processed.
    /// </summary>
    /// <remarks>
    /// This property indicates the index of the file currently being processed in the
    /// batch, starting from 1. It is useful for displaying progress to the user.
    /// </remarks>
    public int CurrentFile { get; } = currentFile;

    /// <summary>
    /// Gets the total number of files to process.
    /// </summary>
    /// <remarks>
    /// This property specifies the total number of files in the batch that are being
    /// processed. It is useful for calculating the overall progress percentage.
    /// </remarks>
    public int TotalFiles { get; } = totalFiles;
}