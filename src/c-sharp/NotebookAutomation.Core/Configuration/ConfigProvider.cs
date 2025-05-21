using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Provides centralized configuration and logging setup for applications.
    /// 
    /// This class helps with initializing configuration and logging services
    /// consistently across all applications in the Notebook Automation suite.
    /// </summary>
    public class ConfigProvider
    {
        /// <summary>
        /// Gets the singleton instance of the ConfigProvider.
        /// </summary>
        public static ConfigProvider? Instance { get; private set; }
        
        /// <summary>
        /// Initializes the singleton instance of the ConfigProvider.
        /// </summary>
        /// <param name="configPath">Optional path to a configuration file.</param>
        /// <param name="debug">Whether to enable debug-level logging.</param>
        /// <returns>The initialized ConfigProvider instance.</returns>
        public static ConfigProvider Initialize(string? configPath = null, bool debug = false)
        {
            Instance = Create(configPath, debug);
            return Instance;
        }
        /// <summary>
        /// The application configuration.
        /// </summary>
        public AppConfig AppConfig { get; private set; }
        
        /// <summary>
        /// The logging service.
        /// </summary>
        public LoggingService LoggingService { get; private set; }
        
        /// <summary>
        /// The service provider for dependency injection.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }
        
    /// <summary>
    /// The configured logger for general logging.
    /// </summary>
    public Microsoft.Extensions.Logging.ILogger Logger => LoggingService.Logger;
    
    /// <summary>
    /// The configured logger for failed operations.
    /// </summary>
    public Microsoft.Extensions.Logging.ILogger FailedLogger => LoggingService.FailedLogger;
        
        /// <summary>
        /// Initializes configuration with the specified options.
        /// </summary>
        /// <param name="configPath">Optional path to a configuration file. If null, will be detected automatically.</param>
        /// <param name="debug">Whether to enable debug-level logging.</param>
        private ConfigProvider(string? configPath = null, bool debug = false)
        {
            // Find and load configuration
            configPath = configPath ?? AppConfig.FindConfigFile();
            
            if (configPath == null || !File.Exists(configPath))
            {
                // Create a default configuration if no file is found
                Console.WriteLine($"Warning: Configuration file not found. Creating default configuration.");
                AppConfig = new AppConfig();
                InitializeDefaultConfiguration();
            }
            else
            {
                // Load from the found configuration file
                AppConfig = AppConfig.LoadFromJsonFile(configPath);
                Console.WriteLine($"Loaded configuration from {configPath}");
            }
            
            // Setup logging
            LoggingService = new LoggingService(AppConfig, debug);
            
            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
        }
        
        /// <summary>
        /// Initializes a minimal default configuration.
        /// </summary>
        private void InitializeDefaultConfiguration()
        {
            AppConfig.Paths.LoggingDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            AppConfig.Paths.ObsidianVaultRoot = Environment.GetEnvironmentVariable("OBSIDIAN_VAULT_PATH") ?? string.Empty;
            AppConfig.Paths.NotebookVaultRoot = Environment.GetEnvironmentVariable("NOTEBOOK_VAULT_PATH") ?? string.Empty;
        }
        
        /// <summary>
        /// Configures dependency injection services.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Register configuration
            services.AddSingleton(AppConfig);
            
            // Register logging
            services.AddSingleton(LoggingService);
            services.AddSingleton(Logger);
            services.AddSingleton(FailedLogger);
            
            // Add additional services here as needed
            // services.AddSingleton<IService, ServiceImplementation>();
        }
        
        /// <summary>
        /// Creates a new ConfigProvider instance.
        /// </summary>
        /// <param name="configPath">Optional path to a configuration file.</param>
        /// <param name="debug">Whether to enable debug-level logging.</param>
        /// <returns>A configured ConfigProvider instance.</returns>
        public static ConfigProvider Create(string? configPath = null, bool debug = false)
        {
            return new ConfigProvider(configPath, debug);
        }
        
        /// <summary>
        /// Gets a logger for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create a logger for.</typeparam>
        /// <returns>A configured logger for the specified type.</returns>
        public ILogger<T> GetLogger<T>()
        {
            return ServiceProvider.GetRequiredService<ILogger<T>>();
        }
        
        /// <summary>
        /// Gets a service from the dependency injection container.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The requested service.</returns>
        public T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
        }
    }
}
