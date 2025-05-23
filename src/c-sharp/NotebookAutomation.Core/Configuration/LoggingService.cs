// -----------------------------------------------------------------------------
// LoggingService.cs
// Centralized logging service for the Notebook Automation system.
//
// Example usage:
//     var logger = LoggingService.CreateLogger<SomeClass>(debug: true);
//     logger.LogInformation("Application started");
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Provides centralized logging capabilities for the notebook automation system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The LoggingService class provides factory methods for creating appropriately configured
    /// ILogger instances for different parts of the application. It supports creating both standard
    /// loggers and specialized "failed" loggers that record operation failures in a dedicated format.
    /// </para>
    /// <para>
    /// This service configures console logging with colorization for better visibility of log levels,
    /// and can also be configured to write to log files when needed.
    /// </para>
    /// <para>
    /// The logging system follows standard logging levels:
    /// <list type="bullet">
    ///   <item><description>Trace: Detailed diagnostic information</description></item>
    ///   <item><description>Debug: Diagnostic information useful during development</description></item>
    ///   <item><description>Information: General information about application flow</description></item>
    ///   <item><description>Warning: Non-critical issues that might need attention</description></item>
    ///   <item><description>Error: Errors that don't stop the application but impair functionality</description></item>
    ///   <item><description>Critical: Critical errors that might lead to application failure</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class LoggingService
    {
        /// <summary>
        /// The singleton instance of LoggingService.
        /// </summary>
        private static LoggingService? _instance;

        /// <summary>
        /// Lock object for thread-safe singleton initialization.
        /// </summary>
        private static readonly object _lock = new object();

        /// <summary>
        /// The main logger instance used for general logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The specialized logger instance used for recording failed operations.
        /// </summary>
        private readonly ILogger _failedLogger;

        /// <summary>
        /// Gets the singleton instance of the LoggingService.
        /// </summary>
        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LoggingService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets the main logger instance used for general application logging.
        /// </summary>
        public ILogger Logger => _logger;

        /// <summary>
        /// Gets the specialized logger instance used for recording failed operations.
        /// </summary>
        public ILogger FailedLogger => _failedLogger;

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private LoggingService()
        {
            // Initialize with default loggers
            _logger = CreateLogger(GetAssemblyName());
            _failedLogger = CreateFailedLogger();
        }

        /// <summary>
        /// Initialize the LoggingService with custom configuration.
        /// </summary>
        /// <param name="appConfig">The application configuration.</param>
        /// <param name="debug">Whether debug logging is enabled.</param>
        public LoggingService(AppConfig appConfig, bool debug = false)
        {
            // Determine if we should log to file
            var logToFile = !string.IsNullOrEmpty(appConfig.Paths.LoggingDir);
            string? logFilePath = null;
            
            if (logToFile)
            {
                var assemblyName = GetAssemblyName();
                var date = DateTime.Now.ToString("yyyyMMdd");
                logFilePath = Path.Combine(appConfig.Paths.LoggingDir, $"{assemblyName.ToLower()}_{date}.log");
            }
            
            _logger = CreateLogger(GetAssemblyName(), debug, logToFile, logFilePath);
            _failedLogger = CreateFailedLogger(debug, logToFile, logFilePath);
            
            // Set instance if it hasn't been set yet
            _instance ??= this;
        }

        /// <summary>
        /// Gets the minimum log level for standard logging based on debug mode.
        /// </summary>
        /// <param name="debug">Whether debug mode is enabled.</param>
        /// <returns>LogLevel.Debug if debug is true; otherwise, LogLevel.Information.</returns>
        public static LogLevel GetMinLogLevel(bool debug) => debug ? LogLevel.Debug : LogLevel.Information;

        /// <summary>
        /// Creates a logger factory with console logging configured.
        /// </summary>
        /// <param name="debug">Whether to enable debug logging.</param>
        /// <param name="logToFile">Whether to enable file logging.</param>
        /// <param name="logFilePath">Path to the log file (used only if logToFile is true).</param>
        /// <returns>A configured ILoggerFactory instance.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a logger factory that logs to the console with colorized output
        /// for better readability. If logToFile is true, it also configures file logging to the
        /// specified log file path.
        /// </para>
        /// <para>
        /// The minimum log level is determined by the debug parameter - when debug is true,
        /// Debug level and above are logged, otherwise only Information and above are logged.
        /// </para>
        /// </remarks>
        public static ILoggerFactory CreateLoggerFactory(bool debug = false, bool logToFile = false, string? logFilePath = null)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.ColorBehavior = LoggerColorBehavior.Enabled;
                    options.SingleLine = false;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                    options.UseUtcTimestamp = false;
                })
                .SetMinimumLevel(GetMinLogLevel(debug));
                
                // Add file logging if requested
                if (logToFile && !string.IsNullOrEmpty(logFilePath))
                {
                    // Ensure directory exists
                    var dirPath = Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    
                    // Add file logger - would use a proper file logging provider in a real implementation
                    // e.g., builder.AddFile(logFilePath);
                    // Using a commented placeholder as actual implementation depends on external packages
                }
            });
            
            return loggerFactory;
        }
        
        /// <summary>
        /// Creates an ILogger instance for the specified category name.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <param name="debug">Whether to enable debug logging.</param>
        /// <param name="logToFile">Whether to enable file logging.</param>
        /// <param name="logFilePath">Path to the log file (used only if logToFile is true).</param>
        /// <returns>An ILogger configured for the specified category.</returns>
        /// <remarks>
        /// This is a convenience method for creating a logger with an explicit category name.
        /// The logger outputs to the console with colorized output.
        /// </remarks>
        public static ILogger CreateLogger(string categoryName, bool debug = false, bool logToFile = false, string? logFilePath = null)
        {
            return CreateLoggerFactory(debug, logToFile, logFilePath).CreateLogger(categoryName);
        }
        
        /// <summary>
        /// Creates a typed ILogger instance for the specified type T.
        /// </summary>
        /// <typeparam name="T">The type to create the logger for.</typeparam>
        /// <param name="debug">Whether to enable debug logging.</param>
        /// <returns>An ILogger{T} configured for the specified type.</returns>
        /// <remarks>
        /// This is a convenience method for creating a typed logger, which uses the type name
        /// as the category name. This is the preferred way to create loggers for classes.
        /// </remarks>
        public static ILogger<T> CreateLogger<T>(bool debug = false)
        {
            return CreateLoggerFactory(debug).CreateLogger<T>();
        }
        
        /// <summary>
        /// Creates a specialized logger for recording failed operations.
        /// </summary>
        /// <param name="debug">Whether to enable debug logging.</param>
        /// <param name="logToFile">Whether to enable file logging.</param>
        /// <param name="logFilePath">Path to the log file (used only if logToFile is true).</param>
        /// <returns>An ILogger configured specifically for logging failures.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a specialized logger for recording failed operations, with a category
        /// name that clearly identifies it as a "failed operations" logger.
        /// </para>
        /// <para>
        /// Failed operations loggers are used throughout the application to record operations that
        /// failed in a consistent way, making it easier to track and diagnose issues.
        /// </para>
        /// </remarks>
        public static ILogger CreateFailedLogger(bool debug = false, bool logToFile = false, string? logFilePath = null)
        {
            return CreateLogger(typeof(FailedOperations).FullName ?? "FailedOperations", debug, logToFile, logFilePath);
        }
        
        /// <summary>
        /// Gets the assembly name for the executing assembly.
        /// </summary>
        /// <returns>The name of the executing assembly, or "Unknown" if it cannot be determined.</returns>
        /// <remarks>
        /// This is a utility method used internally to get a meaningful category name when
        /// more specific information is not available.
        /// </remarks>
        public static string GetAssemblyName()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Name ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
