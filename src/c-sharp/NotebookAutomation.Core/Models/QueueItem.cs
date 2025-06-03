namespace NotebookAutomation.Core.Models
{
    /// <summary>
    /// Represents a document in the processing queue
    /// </summary>
    public class QueueItem
    {
        /// <summary>
        /// Gets the path of the file to be processed
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the current processing status of the file
        /// </summary>
        public DocumentProcessingStatus Status { get; internal set; }

        /// <summary>
        /// Gets the document type (e.g., "PDF", "VIDEO")
        /// </summary>
        public string DocumentType { get; }

        /// <summary>
        /// Gets or sets the current processing stage
        /// </summary>
        public ProcessingStage Stage { get; internal set; } = ProcessingStage.NotStarted;

        /// <summary>
        /// Gets or sets additional information about the file's current state
        /// </summary>
        public string StatusMessage { get; internal set; }

        /// <summary>
        /// Gets or sets the time when processing started for this file
        /// </summary>
        public DateTime? ProcessingStartTime { get; internal set; }

        /// <summary>
        /// Gets or sets the time when processing completed for this file
        /// </summary>
        public DateTime? ProcessingEndTime { get; internal set; }

        /// <summary>
        /// Gets document-specific metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the QueueItem class
        /// </summary>
        /// <param name="filePath">The path of the file to be processed</param>
        /// <param name="documentType">The document type (e.g., "PDF", "VIDEO")</param>
        public QueueItem(string filePath, string documentType)
        {
            FilePath = filePath;
            Status = DocumentProcessingStatus.Waiting;
            DocumentType = documentType;
            StatusMessage = "Waiting to be processed";
        }
    }
}
