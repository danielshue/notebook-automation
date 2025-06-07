// <copyright file="DocumentProcessingStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Models/DocumentProcessingStatus.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
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
