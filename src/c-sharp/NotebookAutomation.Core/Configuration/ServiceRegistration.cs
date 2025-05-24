// Module: ServiceRegistration.cs
// Provides dependency injection configuration for the Notebook Automation project.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
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

            // Register UserSecretsHelper
            services.AddSingleton<UserSecretsHelper>();

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
            });            // Register singleton services
            services.AddSingleton<LoggingService>();
            services.AddSingleton<IYamlHelper, YamlHelper>();

            // Register core services
            services.AddScoped<Services.PromptTemplateService>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<Services.PromptTemplateService>();
                var appConfig = provider.GetRequiredService<AppConfig>();
                return new Services.PromptTemplateService(logger, appConfig);
            });
            services.AddScoped<TagProcessor>();

            // Register processors with AISummarizer dependency
            services.AddScoped<VideoNoteProcessor>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<VideoNoteProcessor>();
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                return new VideoNoteProcessor(logger, aiSummarizer);
            });

            services.AddScoped<VideoNoteBatchProcessor>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("VideoProcessing");
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                return new VideoNoteBatchProcessor(logger, aiSummarizer);
            });

            services.AddScoped<PdfNoteProcessor>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<PdfNoteProcessor>();
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                return new PdfNoteProcessor(logger, aiSummarizer);
            });

            services.AddScoped<PdfNoteBatchProcessor>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("PdfProcessing");
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                return new PdfNoteBatchProcessor(logger, aiSummarizer);
            });

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
            });              // Register prompt template service is already done above

            // Add AI services conditionally if OpenAI key is available
            var openAiKey = configuration["UserSecrets:OpenAI:ApiKey"] ??
                            Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (!string.IsNullOrEmpty(openAiKey))
            {
                // Register Semantic Kernel if API key is available
                services.AddScoped<Kernel>(provider =>
                {
                    var appConfig = provider.GetRequiredService<AppConfig>();
                    var model = appConfig.AiService?.Model ?? "gpt-4.1";

                    // Build and configure the Semantic Kernel
                    var builder = Kernel.CreateBuilder();
                    builder.AddOpenAIChatCompletion(model, openAiKey);

                    return builder.Build();
                });
            }
            // Register AISummarizer
            services.AddScoped<AISummarizer>(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<AISummarizer>();
                var appConfig = provider.GetRequiredService<AppConfig>();
                var promptService = provider.GetRequiredService<PromptTemplateService>();

                // Add AI services conditionally if OpenAI key is available
                var openAiKey = configuration["UserSecrets:OpenAI:ApiKey"] ??
                                Environment.GetEnvironmentVariable("OPENAI_API_KEY");

                var model = appConfig.AiService?.Model ?? "gpt-4.1";

                // Get semantic kernel if registered (may be null)
                Kernel? semanticKernel = null;
                ITextGenerationService? textGenService = null;

                try
                {
                    semanticKernel = provider.GetService<Kernel>();

                    // Try to get text generation service from kernel
                    if (semanticKernel != null)
                    {
                        try
                        {
                            textGenService = semanticKernel.GetRequiredService<ITextGenerationService>();
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to get text generation service from kernel");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Semantic Kernel is not available");
                }
                return new AISummarizer(
                  logger,
                  promptService,
                  semanticKernel,
                  textGenService);
            });

            return services;
        }
    }
}