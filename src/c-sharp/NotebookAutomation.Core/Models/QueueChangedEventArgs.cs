// <copyright file="QueueChangedEventArgs.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Models/QueueChangedEventArgs.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Event arguments for queue changes.
/// </summary>
/// <remarks>
/// The <c>QueueChangedEventArgs</c> class provides information about changes to the processing queue,
/// including the current state of the queue and the item that was changed, if applicable.
/// </remarks>
public class QueueChangedEventArgs(IReadOnlyList<QueueItem> queue, QueueItem? changedItem = null) : EventArgs
{
    /// <summary>
    /// Gets the current state of the processing queue.
    /// </summary>
    /// <remarks>
    /// This property provides a snapshot of the queue's state at the time of the event.
    /// </remarks>
    public IReadOnlyList<QueueItem> Queue { get; } = queue;

    /// <summary>
    /// Gets the item that changed, if applicable.
    /// </summary>
    /// <remarks>
    /// This property identifies the specific item in the queue that triggered the event.
    /// It may be null if the event does not pertain to a specific item.
    /// </remarks>
    public QueueItem? ChangedItem { get; } = changedItem;
}
