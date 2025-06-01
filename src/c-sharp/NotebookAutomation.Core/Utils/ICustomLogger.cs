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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.Log<string>(LogLevel.Information, new EventId(), message, null, (s, e) => string.Format(s, args));
        }

        public void LogError(string message, params object[] args)
        {
            _logger.Log<string>(LogLevel.Error, new EventId(), message, null, (s, e) => string.Format(s, args));
        }

        public void LogDebug(string message, params object[] args)
        {
            _logger.Log<string>(LogLevel.Debug, new EventId(), message, null, (s, e) => string.Format(s, args));
        }
    }
}
