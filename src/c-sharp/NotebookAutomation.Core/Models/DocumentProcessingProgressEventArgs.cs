namespace NotebookAutomation.Core.Models
{
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
        public string FilePath { get; } = filePath;

        /// <summary>
        /// Gets the current processing status message.
        /// </summary>
        public string Status { get; } = status;

        /// <summary>
        /// Gets the current file index being processed.
        /// </summary>
        public int CurrentFile { get; } = currentFile;

        /// <summary>
        /// Gets the total number of files to process.
        /// </summary>
        public int TotalFiles { get; } = totalFiles;
    }
}
