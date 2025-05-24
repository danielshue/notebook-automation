using System;

namespace NotebookAutomation.Core.Tools.Shared
{
    /// <summary>
    /// Represents the result and statistics of a batch document processing operation.
    /// </summary>
    public class BatchProcessResult
    {
        /// <summary>
        /// Number of files successfully processed.
        /// </summary>
        public int Processed { get; set; }

        /// <summary>
        /// Number of files that failed to process.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// User-friendly summary string for CLI or UI output.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Total batch processing time.
        /// </summary>
        public TimeSpan TotalBatchTime { get; set; }

        /// <summary>
        /// Total time spent generating summaries.
        /// </summary>
        public TimeSpan TotalSummaryTime { get; set; }

        /// <summary>
        /// Total tokens used for all summaries.
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// Average time per file in milliseconds.
        /// </summary>
        public double AverageFileTimeMs { get; set; }

        /// <summary>
        /// Average summary time per file in milliseconds.
        /// </summary>
        public double AverageSummaryTimeMs { get; set; }

        /// <summary>
        /// Average tokens per summary.
        /// </summary>
        public double AverageTokens { get; set; }
    }
}
