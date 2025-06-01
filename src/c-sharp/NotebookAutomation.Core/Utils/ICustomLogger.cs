using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Utils
{
    public interface ICustomLogger
    {
        void LogInformation(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogDebug(string message, params object[] args);
    }

    public class CustomLogger : ICustomLogger
    {
        private readonly ILogger _logger;

        public CustomLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }
    }
}
