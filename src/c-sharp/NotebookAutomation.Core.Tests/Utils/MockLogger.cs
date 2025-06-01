using Microsoft.Extensions.Logging;
using System;

namespace NotebookAutomation.Core.Tests.Utils
{
    /// <summary>
    /// Simple mock logger for testing purposes.
    /// </summary>
    public class MockLogger<T> : ILogger<T>
    {
        private readonly Action<LogLevel, string> _logAction;

        public MockLogger(Action<LogLevel, string> logAction = null)
        {
            _logAction = logAction ?? ((level, msg) => { });
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            _logAction(logLevel, message);
        }
    }
}
