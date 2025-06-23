// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides centralized logging capabilities for the notebook automation system.
/// </summary>
/// <remarks>
/// <para>
/// The LoggingService class offers a robust logging infrastructure for the application,
/// supporting both console and file-based logging. It provides factory methods for creating
/// loggers tailored to specific application needs, including general-purpose loggers and
/// specialized loggers for failed operations.
/// </para>
/// <para>
/// Key features include:
/// <list type="bullet">
///   <item><description>Support for Serilog-based logging with configurable levels</description></item>
///   <item><description>Thread-safe initialization of logging resources</description></item>
///   <item><description>Fallback mechanisms for console logging in case of initialization failures</description></item>
///   <item><description>Integration with Microsoft.Extensions.Logging for typed loggers</description></item>
/// </list>
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the LoggingService class with a logging directory.
/// This constructor is used for early initialization before AppConfig is available.
/// </remarks>
/// <param name="loggingDir">The directory where log files should be stored.</param>
/// <param name="debug">Whether debug mode is enabled.</param>
public class LoggingService(string loggingDir, bool debug = false) : ILoggingService
{ // Core properties for logging configuration
    private readonly string loggingDir = loggingDir ?? Path.Combine(AppContext.BaseDirectory, "logs");
    private readonly bool debug = debug;

    // Current log file path (set during initialization)
    private string? currentLogFilePath;

    // The initialized loggers and factory (null until ConfigureLogging is called)
    private global::Serilog.ILogger? serilogLogger;
    private global::Serilog.ILogger? serilogFailedLogger;
    private Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory;
    private Microsoft.Extensions.Logging.ILogger? logger;
    private Microsoft.Extensions.Logging.ILogger? failedLogger;

    // Synchronization object for thread safety
    private readonly object initLock = new();
    private volatile bool isInitialized = false;

    /// <summary>
    /// Gets the main Serilog logger instance used for general logging.
    /// </summary>

    private global::Serilog.ILogger SerilogLogger => EnsureInitialized().serilogLogger!;

    /// <summary>
    /// Gets the specialized Serilog logger instance used for recording failed operations.
    /// </summary>

    private global::Serilog.ILogger SerilogFailedLogger => EnsureInitialized().serilogFailedLogger!;

    /// <summary>
    /// Gets the logger factory used to create typed loggers.
    /// </summary>

    private ILoggerFactory LoggerFactoryInternal => EnsureInitialized().loggerFactory!;

    /// <summary>
    /// Gets the main logger instance used for general application logging.
    /// </summary>

    public Microsoft.Extensions.Logging.ILogger Logger => EnsureInitialized().logger!;

    /// <summary>
    /// Gets the specialized logger instance used for recording failed operations.
    /// </summary>

    public Microsoft.Extensions.Logging.ILogger FailedLogger => EnsureInitialized().failedLogger!;

    /// <summary>
    /// Gets the full path to the current log file.
    /// </summary>
    /// <returns>The absolute path to the current log file, or null if logging is not configured to a file.</returns>
    public string? CurrentLogFilePath => EnsureInitialized().currentLogFilePath;

    /// <summary>
    /// Ensures that the loggers are initialized and returns the current instance.
    /// </summary>
    /// <returns>The current LoggingService instance with initialized loggers.</returns>
    private LoggingService EnsureInitialized()
    {
        if (isInitialized)
        {
            return this;
        }

        lock (initLock)
        {
            if (!isInitialized)
            {
                InitializeLogging();
                isInitialized = true;
            }
        }

        return this;
    }

    /// <summary>
    /// Initializes the logging infrastructure.
    /// </summary>

    protected virtual void InitializeLogging()
    {
        try
        {
            // Create Serilog loggers
            serilogLogger = CreateSerilogLogger(loggingDir, debug);

            // Create the failed logger with the same configuration but a different source context
            var failedLoggerName = typeof(FailedOperations).FullName ?? "FailedOperations";
            serilogFailedLogger = serilogLogger.ForContext("SourceContext", failedLoggerName);

            // Create Microsoft.Extensions.Logging factory and loggers
            var appAssemblyName = GetAssemblyName();
            loggerFactory = new LoggerFactory().AddSerilog(serilogLogger, dispose: false);
            logger = loggerFactory.CreateLogger(appAssemblyName);
            failedLogger = loggerFactory.CreateLogger(failedLoggerName);
        }
        catch (Exception ex)
        {
            // Create a fallback console logger in case initialization fails
            var fallbackFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = false;
                    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                }));

            var fallbackLogger = fallbackFactory.CreateLogger("LoggingService.Fallback");
            fallbackLogger.LogError(ex, "Failed to initialize logging. Using fallback console logger.");

            // Create minimal working loggers
            serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            serilogFailedLogger = serilogLogger;
            loggerFactory = fallbackFactory;
            logger = fallbackLogger;
            failedLogger = fallbackLogger;
        }
    }

    /// <summary>
    /// Configures the logging builder with the appropriate providers asynchronously.
    /// </summary>
    /// <param name="builder">The logging builder to configure.</param>
    public void ConfigureLogging(ILoggingBuilder builder)
    {
        if (!Directory.Exists(loggingDir))
        {
            Directory.CreateDirectory(loggingDir);
        }

        // Ensure loggers are initialized
        EnsureInitialized();

        // Configure the builder with our Serilog logger
        builder.AddSerilog(SerilogLogger, dispose: true);
    }

    /// <summary>
    /// Creates a configured Serilog logger for the application.
    /// </summary>
    /// <param name="loggingDir">Directory for log files.</param>
    /// <param name="debug">Whether debug logging is enabled.</param>
    /// <returns>A configured Serilog logger instance.</returns>
    private global::Serilog.ILogger CreateSerilogLogger(string loggingDir, bool debug)
    {
        try
        {
            // Ensure directory exists
            if (!Directory.Exists(loggingDir))
            {
                Directory.CreateDirectory(loggingDir);
            }

            var appAssemblyName = GetAssemblyName();
            var minLevel = debug ? LogEventLevel.Debug : LogEventLevel.Information;            // Console shows debug/info when debug mode is enabled, otherwise warnings and errors
            var consoleMinLevel = debug ? LogEventLevel.Debug : LogEventLevel.Warning;
            var date = DateTime.Now.ToString("yyyyMMdd");
            var time = DateTime.Now.ToString("HHmmss");

            // Use a consistent filename to avoid creating multiple log files per session
            var logFilePath = Path.Combine(loggingDir, $"{appAssemblyName.ToLower()}_{date}.log");

            // Store the log file path for external access
            currentLogFilePath = logFilePath;

            // Configure different console output templates based on debug mode
            // Debug mode: Show full exception details including stack traces
            // Non-debug mode: Show only the message to maintain user-friendly output
            var consoleTemplate = debug
                ? "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
                : "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}";

            // Configure and create Serilog logger
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(minLevel)
                .WriteTo.Console(
                    restrictedToMinimumLevel: consoleMinLevel,
                    outputTemplate: consoleTemplate,
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    restrictedToMinimumLevel: minLevel,
                    shared: true);  // Add shared flag to allow multiple processes to write to the same file

            return loggerConfig.CreateLogger().ForContext("SourceContext", appAssemblyName);
        }
        catch (Exception ex)
        {
            // If we can't create the logger, use a default Serilog console logger
            var fallbackConfig = new LoggerConfiguration()
                .WriteTo.Console();
            var fallbackLogger = fallbackConfig.CreateLogger();
            fallbackLogger.Error(ex, "Failed to initialize Serilog logger. Using fallback console logger.");
            return fallbackLogger;
        }
    }

    /// <summary>
    /// Gets a typed ILogger instance for the specified type T from this LoggingService instance.
    /// </summary>
    /// <typeparam name="T">The type to create the logger for.</typeparam>
    /// <returns>An ILogger{T} configured for the specified type.</returns>
    /// <remarks>
    /// This is an instance method for creating a typed logger, which uses the type name
    /// as the category name. This is the preferred way to create loggers for classes
    /// when you have a LoggingService instance.
    /// </remarks>
    public virtual Microsoft.Extensions.Logging.ILogger<T> GetLogger<T>()
    {
        return LoggerFactoryInternal.CreateLogger<T>();
    }

    /// <summary>
    /// Gets the assembly name for the executing assembly.
    /// </summary>
    /// <returns>The name of the executing assembly, or "Unknown" if it cannot be determined.</returns>
    private static string GetAssemblyName()
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
