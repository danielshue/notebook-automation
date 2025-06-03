namespace NotebookAutomation.Core.Models
{
    /// <summary>
    /// Event arguments for queue changes
    /// </summary>
    public class QueueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the current state of the processing queue
        /// </summary>
        public IReadOnlyList<QueueItem> Queue { get; }

        /// <summary>
        /// Gets the item that changed, if applicable
        /// </summary>
        public QueueItem? ChangedItem { get; }

        /// <summary>
        /// Initializes a new instance of QueueChangedEventArgs
        /// </summary>
        /// <param name="queue">The current queue state</param>
        /// <param name="changedItem">The item that changed (optional)</param>
        public QueueChangedEventArgs(IReadOnlyList<QueueItem> queue, QueueItem? changedItem = null)
        {
            Queue = queue;
            ChangedItem = changedItem;
        }
    }
}
