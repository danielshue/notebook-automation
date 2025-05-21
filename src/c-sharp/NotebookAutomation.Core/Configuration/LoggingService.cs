// -----------------------------------------------------------------------------
// LoggingService.cs
// Centralized logging service for the Notebook Automation system.
//
// Example usage:
//     var loggingService = new LoggingService(appConfig, debug: true);
//     var logger = loggingService.Logger;
//     logger.LogInformation("Application started");
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Centralized logging service for the Notebook Automation system.
    /// 
    /// This class provides a unified logging system with console coloring,
    /// file output, and configurable verbosity levels. It's designed to be
    /// used across all components of the Notebook Automation system.
    /// </summary>
    public class LoggingService
    {
        private readonly AppConfig _config;
        private readonly bool _debug;
        private readonly string _callerName;
        
        /// <summary>
        /// Standard logger for general application logging.
        /// </summary>
        public Microsoft.Extensions.Logging.ILogger Logger { get; }
        
        /// <summary>
        /// Specialized logger for tracking failed operations.
        /// </summary>
        public Microsoft.Extensions.Logging.ILogger FailedLogger { get; }
        
        /// <summary>
        /// Initializes a new instance of the LoggingService.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        /// <param name="debug">Whether to enable debug-level logging.</param>
        /// <param name="callerAssembly">The assembly that is using the logger, for naming log files.</param>
        public LoggingService(AppConfig config, bool debug = false, Assembly? callerAssembly = null)
        {
            _config = config;
            _debug = debug;
            _callerName = GetCallerName(callerAssembly);
            var serviceProvider = ConfigureServices();
            Logger = serviceProvider.GetRequiredService<ILogger<LoggingService>>();
            FailedLogger = serviceProvider.GetRequiredService<ILogger<FailedOperations>>();
        }
        
        /// <summary>
        /// Configures the logging services with Serilog.
        /// </summary>
        /// <returns>A configured service provider.</returns>
        private ServiceProvider ConfigureServices()
        {
            var loggingDir = string.IsNullOrWhiteSpace(_config.Paths.LoggingDir) 
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs") 
                : _config.Paths.LoggingDir;
            try
            {
                Directory.CreateDirectory(loggingDir);
            }
            catch (Exception ex)
            {
                // If directory creation fails, fallback to current directory
                loggingDir = AppDomain.CurrentDomain.BaseDirectory;
                Console.Error.WriteLine($"[LoggingService] Failed to create log directory: {ex.Message}");
            }
            var logFilePath = Path.Combine(loggingDir, $"{_callerName}_.log");
            var failedLogFilePath = Path.Combine(loggingDir, $"{_callerName}_failed.log");
            // Configure Serilog
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(_debug ? LogEventLevel.Debug : LogEventLevel.Information)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code)
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day);
            // Configure the Failed logger
            var failedLoggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LogEventLevel.Warning)
                .WriteTo.File(failedLogFilePath, rollingInterval: RollingInterval.Day);
            // Create the loggers
            var logger = loggerConfiguration.CreateLogger();
            var failedLogger = failedLoggerConfiguration.CreateLogger();
            // Setup DI container
            var services = new ServiceCollection();
            services.AddLogging(builder => {
                builder.ClearProviders();
                builder.AddSerilog(logger, dispose: true);
            });
            services.AddSingleton<FailedOperations>();
            services.AddSingleton(provider => {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                return loggerFactory.CreateLogger<FailedOperations>();
            });
            return services.BuildServiceProvider();
        }
        
        /// <summary>
        /// Gets a standardized name for the caller assembly to use in log file names.
        /// </summary>
        /// <param name="callerAssembly">The assembly calling the logger setup.</param>
        /// <returns>A standardized name for the caller.</returns>
        private string GetCallerName(Assembly? callerAssembly)
        {
            if (callerAssembly == null)
            {
                // Try to determine the calling assembly
                callerAssembly = Assembly.GetCallingAssembly();
                // If still null, use the entry assembly
                if (callerAssembly == Assembly.GetExecutingAssembly())
                {
                    callerAssembly = Assembly.GetEntryAssembly();
                }
            }
            // Extract a name from the assembly
            var name = callerAssembly?.GetName().Name ?? "app";
            // Standardize the name for file usage
            return name
                .Replace(".", "_")
                .Replace(" ", "_")
                .ToLowerInvariant();
        }
    }
    // The FailedOperations class has been moved to its own file:
    // /workspaces/notebook-automation/src/c-sharp/NotebookAutomation.Core/Configuration/FailedOperations.cs
}
