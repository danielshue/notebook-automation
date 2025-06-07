// <copyright file="ProcessingStage.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Models/ProcessingStage.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Represents the current processing stage of a document.
/// </summary>
public enum ProcessingStage
{
    /// <summary>
    /// Processing has not yet started.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Extracting content from the document (PDF text or video transcription).
    /// </summary>
    ContentExtraction,

    /// <summary>
    /// Generating an AI summary of the document.
    /// </summary>
    AISummaryGeneration,

    /// <summary>
    /// Creating a markdown note from the document.
    /// </summary>
    MarkdownCreation,

    /// <summary>
    /// Generating OneDrive share links.
    /// </summary>
    ShareLinkGeneration,

    /// <summary>
    /// Processing is complete.
    /// </summary>
    Completed,
}
