// <copyright file="MockLogger.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Utils/MockLogger.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests.Utils;

/// <summary>
/// Simple mock logger for testing purposes.
/// </summary>
internal class MockLogger<T>(Action<LogLevel, string> logAction = null) : ILogger<T>
{
    private readonly Action<LogLevel, string> logAction = logAction ?? ((level, msg) => { });

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        string message = formatter(state, exception);
        this.logAction(logLevel, message);
    }
}
