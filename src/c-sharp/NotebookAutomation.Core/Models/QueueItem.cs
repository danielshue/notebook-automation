// <copyright file="QueueItem.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Models/QueueItem.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Represents a document in the processing queue.
/// </summary>
/// <remarks>
/// The <c>QueueItem</c> class encapsulates information about a document that is
/// queued for processing, including its file path, type, status, stage, and metadata.
/// It provides properties to track the progress and state of the document throughout
/// the processing lifecycle.
/// </remarks>
/// <param name="filePath">The path of the file to be processed.</param>
/// <param name="documentType">The document type (e.g., "PDF", "VIDEO").</param>
public class QueueItem(string filePath, string documentType)
{
    /// <summary>
    /// Gets the path of the file to be processed.
    /// </summary>
    /// <remarks>
    /// This property specifies the full path to the file that is queued for processing.
    /// </remarks>
    public string FilePath { get; } = filePath;

    /// <summary>
    /// Gets the current processing status of the file.
    /// </summary>
    /// <remarks>
    /// This property indicates the overall status of the file, such as "Waiting" or "Completed".
    /// </remarks>
    public DocumentProcessingStatus Status { get; internal set; } = DocumentProcessingStatus.Waiting;

    /// <summary>
    /// Gets the document type (e.g., "PDF", "VIDEO").
    /// </summary>
    /// <remarks>
    /// This property specifies the type of document being processed, which determines
    /// the processing logic to be applied.
    /// </remarks>
    public string DocumentType { get; } = documentType;

    /// <summary>
    /// Gets the current processing stage.
    /// </summary>
    /// <remarks>
    /// This property tracks the specific stage of processing, such as "ContentExtraction"
    /// or "MarkdownCreation".
    /// </remarks>
    public ProcessingStage Stage { get; internal set; } = ProcessingStage.NotStarted;

    /// <summary>
    /// Gets additional information about the file's current state.
    /// </summary>
    /// <remarks>
    /// This property provides a descriptive message about the file's current processing state.
    /// </remarks>
    public string StatusMessage { get; internal set; } = "Waiting to be processed";

    /// <summary>
    /// Gets the time when processing started for this file.
    /// </summary>
    /// <remarks>
    /// This property records the timestamp when processing began for the file.
    /// </remarks>
    public DateTime? ProcessingStartTime { get; internal set; }

    /// <summary>
    /// Gets the time when processing completed for this file.
    /// </summary>
    /// <remarks>
    /// This property records the timestamp when processing finished for the file.
    /// </remarks>
    public DateTime? ProcessingEndTime { get; internal set; }

    /// <summary>
    /// Gets document-specific metadata.
    /// </summary>
    /// <remarks>
    /// This property contains additional metadata related to the document, such as
    /// extracted content or processing results.
    /// </remarks>
    public Dictionary<string, object> Metadata { get; } = [];
}
