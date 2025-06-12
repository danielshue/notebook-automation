// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.TestDoubles;

/// <summary>
/// Simple mock logger for testing purposes.
/// </summary>

internal class MockLogger<T>(Action<LogLevel, string>? logAction = null) : ILogger<T>
{
    private readonly Action<LogLevel, string> logAction = logAction ?? ((level, msg) => { });

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        logAction(logLevel, message);
    }
}