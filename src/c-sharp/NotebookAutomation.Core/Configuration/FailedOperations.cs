using System;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Helper class for logging failed operations.
    /// 
    /// This class serves as a container for logging failures across
    /// the application to a specialized log file.
    /// </summary>
    public class FailedOperations
    {
        private readonly ILogger<FailedOperations> _logger;

        /// <summary>
        /// Initializes a new instance of the FailedOperations class.
        /// </summary>
        /// <param name="logger">Logger instance for recording failed operations.</param>
        public FailedOperations(ILogger<FailedOperations> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Logs a failed operation with the provided details.
        /// </summary>
        /// <param name="operation">The name of the operation that failed.</param>
        /// <param name="item">The specific item (e.g., file path, ID) that caused the failure.</param>
        /// <param name="exception">The exception that was thrown, if any.</param>
        public void LogFailure(string operation, string item, Exception? exception = null)
        {
            if (exception != null)
            {
                _logger.LogError(exception, "Failed operation: {Operation} on {Item}", operation, item);
            }
            else
            {
                _logger.LogError("Failed operation: {Operation} on {Item}", operation, item);
            }
        }

        /// <summary>
        /// Logs a failed operation with detailed reason.
        /// </summary>
        /// <param name="operation">The name of the operation that failed.</param>
        /// <param name="item">The specific item (e.g., file path, ID) that caused the failure.</param>
        /// <param name="reason">The reason for the failure.</param>
        public void LogFailureWithReason(string operation, string item, string reason)
        {
            _logger.LogError("Failed operation: {Operation} on {Item}. Reason: {Reason}", 
                operation, item, reason);
        }
    }
}
