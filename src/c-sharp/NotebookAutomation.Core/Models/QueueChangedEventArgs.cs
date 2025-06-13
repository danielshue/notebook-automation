// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Event arguments for queue changes.
/// </summary>
/// <remarks>
/// The <c>QueueChangedEventArgs</c> class provides information about changes to the processing queue,
/// including the current state of the queue and the item that was changed, if applicable.
/// </remarks>
public class QueueChangedEventArgs(IEnumerable<QueueItem> queue, QueueItem? changedItem = null) : EventArgs
{
    /// <summary>
    /// Gets the current state of the processing queue.
    /// </summary>
    /// <remarks>
    /// This property provides a snapshot of the queue's state at the time of the event.
    /// </remarks>
    public IReadOnlyList<QueueItem> Queue { get; } = queue.ToList().AsReadOnly();

    /// <summary>
    /// Gets the item that changed, if applicable.
    /// </summary>
    /// <remarks>
    /// This property identifies the specific item in the queue that triggered the event.
    /// It may be null if the event does not pertain to a specific item.
    /// </remarks>
    public QueueItem? ChangedItem { get; } = changedItem;
}
