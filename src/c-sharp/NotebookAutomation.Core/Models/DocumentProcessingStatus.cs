// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Represents the status of a document in the processing queue.
/// </summary>
public enum DocumentProcessingStatus
{
    /// <summary>
    /// The document is waiting to be processed.
    /// </summary>
    Waiting,

    /// <summary>
    /// The document is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// The document has been successfully processed.
    /// </summary>
    Completed,

    /// <summary>
    /// Processing the document failed.
    /// </summary>
    Failed,
}