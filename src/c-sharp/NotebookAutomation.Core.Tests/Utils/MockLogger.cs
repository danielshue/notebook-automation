namespace NotebookAutomation.Core.Tests.Utils;

/// <summary>
/// Simple mock logger for testing purposes.
/// </summary>
public class MockLogger<T>(Action<LogLevel, string> logAction = null) : ILogger<T>
{
    private readonly Action<LogLevel, string> _logAction = logAction ?? ((level, msg) => { });

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        string message = formatter(state, exception);
        _logAction(logLevel, message);
    }
}
