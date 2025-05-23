// Module: ServiceRegistration.cs
// Provides dependency injection configuration for the Notebook Automation project.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.TagManagement;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Tools.PdfProcessing;
using NotebookAutomation.Core.Utils;
using Serilog;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Provides extension methods for setting up dependency injection in the application.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Adds core notebook automation services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="debug">Whether debug mode is enabled.</param>
        /// <returns>The configured service collection.</returns>
        public static IServiceCollection AddNotebookAutomationServices(
            this IServiceCollection services,
            IConfiguration configuration,
            bool debug = false)
        {
            // Register configuration
            services.AddSingleton(provider => 
            {
                var appConfig = new AppConfig(configuration, 
                    provider.GetRequiredService<ILogger<AppConfig>>());
                return appConfig;
            });

            // Register logging services
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(debug ? LogLevel.Debug : LogLevel.Information);
                
                // Add Serilog file logging
                var loggingDir = configuration.GetSection("paths:loggingDir")?.Value;
                if (!string.IsNullOrEmpty(loggingDir))
                {
                    Directory.CreateDirectory(loggingDir);
                    
                    // Configure Serilog
                    var loggerConfig = new LoggerConfiguration()
                        .MinimumLevel.Is(debug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
                        .WriteTo.Console()
                        .WriteTo.File(
                            Path.Combine(loggingDir, $"notebookautomation_cli_{DateTime.Now:yyyyMMdd}.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7);
                    
                    // Add Serilog to the logging pipeline
                    builder.AddSerilog(loggerConfig.CreateLogger(), dispose: true);
                }
            });

            // Register singleton services
            services.AddSingleton<LoggingService>();
            services.AddSingleton<IYamlHelper, YamlHelper>();            // Register core services
            services.AddScoped<PromptTemplateService>();
            services.AddScoped<TagProcessor>();
            services.AddScoped<VideoNoteProcessor>();
            services.AddScoped(provider => {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("VideoProcessing");
                return new VideoNoteBatchProcessor(logger);
            });
            services.AddScoped<PdfNoteProcessor>();
            
            // Register OneDrive service
            services.AddScoped(provider => 
            {
                var logger = provider.GetRequiredService<ILogger<OneDriveService>>();
                var microsoftGraph = provider.GetRequiredService<AppConfig>().MicrosoftGraph;
                return new OneDriveService(
                    logger,
                    microsoftGraph?.ClientId ?? string.Empty,
                    microsoftGraph?.TenantId ?? string.Empty,
                    microsoftGraph?.Scopes?.ToArray() ?? Array.Empty<string>()
                );
            });
            
            // Register OpenAI summarizer
            services.AddScoped(provider => 
            {
                var logger = provider.GetRequiredService<ILogger<OpenAiSummarizer>>();
                var openAi = provider.GetRequiredService<AppConfig>().OpenAi;
                return new OpenAiSummarizer(
                    logger,
                    openAi?.ApiKey ?? string.Empty,
                    openAi?.Model ?? "gpt-4.1"
                );
            });

            // Path utilities and helpers
            services.AddSingleton<PathUtils>();

            return services;
        }
    }
}