namespace NotebookAutomation.Core.Models;
/// <summary>
/// Event arguments for queue changes
/// </summary>
/// <remarks>
/// Initializes a new instance of QueueChangedEventArgs
/// </remarks>
/// <param name="queue">The current queue state</param>
/// <param name="changedItem">The item that changed (optional)</param>
public class QueueChangedEventArgs(IReadOnlyList<QueueItem> queue, QueueItem? changedItem = null) : EventArgs
{
    /// <summary>
    /// Gets the current state of the processing queue
    /// </summary>
    public IReadOnlyList<QueueItem> Queue { get; } = queue;

    /// <summary>
    /// Gets the item that changed, if applicable
    /// </summary>
    public QueueItem? ChangedItem { get; } = changedItem;
}
