using NotebookAutomation.Core.Tools.Shared;
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
    /// Provides centralized registration of all core services, dependency injection, and logging for the Notebook Automation application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>ServiceRegistration</c> static class defines extension methods for configuring dependency injection (DI)
    /// and logging infrastructure. It ensures that all required services, processors, and helpers are registered with
    /// appropriate lifetimes, and that robust, production-grade logging is available throughout the application.
    /// </para>
    /// <para>
    /// <b>Key Responsibilities:</b>
    /// <list type="bullet">
    ///   <item><description>Registers configuration, helpers, and all core business and processing services.</description></item>
    ///   <item><description>Configures logging using both Microsoft.Extensions.Logging and Serilog, supporting console and file sinks.</description></item>
    ///   <item><description>Ensures log levels (including DEBUG) are respected based on the application's debug flag.</description></item>
    ///   <item><description>Registers and configures Microsoft Semantic Kernel for AI summarization, wiring it to the application's logger factory.</description></item>
    ///   <item><description>Supports flexible, testable, and maintainable service lifetimes (singleton, scoped).</description></item>
    ///   <item><description>Centralizes all DI and logging setup to promote consistency and maintainability across the codebase.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Usage:</b>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddNotebookAutomationServices(configuration, debug: true);
    /// var provider = services.BuildServiceProvider();
    /// </code>
    /// </para>
    /// <para>
    /// <b>Exceptions:</b>
    /// <list type="bullet">
    ///   <item><description>Throws <see cref="ArgumentNullException"/> if required services or configuration are missing.</description></item>
    ///   <item><description>May throw exceptions from underlying service constructors if dependencies are not satisfied.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>See Also:</b>
    /// <list type="bullet">
    ///   <item><description><see cref="LoggingService"/></description></item>
    ///   <item><description><see cref="AppConfig"/></description></item>
    ///   <item><description><see cref="PromptTemplateService"/></description></item>
    ///   <item><description><see cref="AISummarizer"/></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Register services and build provider
    /// var services = new ServiceCollection();
    /// services.AddNotebookAutomationServices(configuration, debug: true);
    /// var provider = services.BuildServiceProvider();
    /// var summarizer = provider.GetRequiredService<AISummarizer>();
    /// </code>
    /// </example>
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
            bool debug = false,
            string? configFilePath = null)
        {
            // Register configuration
            services.AddSingleton(provider =>
            {
                var appConfig = new AppConfig(
                    configuration,
                    provider.GetRequiredService<ILogger<AppConfig>>(),
                    configFilePath,
                    debug);
                return appConfig;
            });

            // Register UserSecretsHelper
            services.AddSingleton<UserSecretsHelper>();

            // Register logging services
            services.AddLogging(builder =>
            {
                // Support both snake_case and camelCase for logging_dir
                var loggingDir = configuration.GetSection("paths:logging_dir")?.Value
                    ?? configuration.GetSection("paths:loggingDir")?.Value;
                if (string.IsNullOrWhiteSpace(loggingDir))
                {
                    loggingDir = Path.Combine(AppContext.BaseDirectory, "logs");
                }
                Directory.CreateDirectory(loggingDir);

                // Configure Serilog for both console and file
                var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Is(debug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
                    .WriteTo.Console(restrictedToMinimumLevel: debug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
                    .WriteTo.File(
                        Path.Combine(loggingDir, $"notebookautomation_cli_{DateTime.Now:yyyyMMdd}.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        restrictedToMinimumLevel: debug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information);

                // Add Serilog to the logging pipeline (do not add Microsoft console logger)
                builder.AddSerilog(loggerConfig.CreateLogger(), dispose: true);
            });
            services.AddSingleton<LoggingService>();
            services.AddSingleton<IYamlHelper, YamlHelper>();

            // Register new metadata-related services
            services.AddScoped<MetadataTemplateManager>();
            services.AddScoped<MetadataHierarchyDetector>();

            // Register core services
            services.AddScoped(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<PromptTemplateService>();
                var appConfig = provider.GetRequiredService<AppConfig>();
                return new PromptTemplateService(logger, appConfig);
            });            services.AddScoped<TagProcessor>();
            
            // Register processors with AISummarizer dependency
            services.AddScoped(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<VideoNoteProcessor>();
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                var appConfig = provider.GetRequiredService<AppConfig>();
                // Use GetService instead of GetRequiredService since OneDriveService is optional
                var oneDriveService = provider.GetService<IOneDriveService>();
                return new VideoNoteProcessor(logger, aiSummarizer, oneDriveService, appConfig);
            });

            services.AddScoped(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var batchLogger = loggerFactory.CreateLogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>();                var processorLogger = loggerFactory.CreateLogger<VideoNoteProcessor>();
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                var appConfig = provider.GetRequiredService<AppConfig>();
                // Use GetService instead of GetRequiredService since OneDriveService is optional
                var oneDriveService = provider.GetService<IOneDriveService>();
                var videoProcessor = new VideoNoteProcessor(processorLogger, aiSummarizer, oneDriveService, appConfig);
                var batchProcessor = new DocumentNoteBatchProcessor<VideoNoteProcessor>(batchLogger, videoProcessor, aiSummarizer);
                return new VideoNoteBatchProcessor(batchProcessor);
            });

            services.AddScoped(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<PdfNoteProcessor>();
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                return new PdfNoteProcessor(logger, aiSummarizer);
            });

            services.AddScoped(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>();
                var aiSummarizer = provider.GetRequiredService<AISummarizer>();
                var pdfProcessor = new PdfNoteProcessor(loggerFactory.CreateLogger<PdfNoteProcessor>(), aiSummarizer);
                var batchProcessor = new DocumentNoteBatchProcessor<PdfNoteProcessor>(logger, pdfProcessor, aiSummarizer);
                return new PdfNoteBatchProcessor(batchProcessor);
            });

            // Register OneDrive service
            services.AddScoped<IOneDriveService>(provider =>
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

            // Register prompt template service is already done above

            // Add AI services conditionally if OpenAI key is available
            var openAiKey = configuration["UserSecrets:OpenAI:ApiKey"] ??
                            Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (!string.IsNullOrEmpty(openAiKey))
            {
                // Register Semantic Kernel if API key is available
                services.AddScoped(provider =>
                {
                    var appConfig = provider.GetRequiredService<AppConfig>();
                    var model = appConfig.AiService?.Model ?? "gpt-4.1";
                    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                    // Build and configure the Semantic Kernel
                    var builder = Kernel.CreateBuilder();
                    builder.Services.AddSingleton(typeof(ILoggerFactory), loggerFactory); // Inject app logger factory
                    builder.AddOpenAIChatCompletion(model, openAiKey);

                    // Optionally, set the kernel's own logger if supported
                    // builder.WithLoggerFactory(loggerFactory); // Uncomment if Semantic Kernel supports this method

                    return builder.Build();
                });
            }
            // Register AISummarizer
            services.AddScoped(provider =>
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
                  semanticKernel!,  // Use null-forgiving operator since we've already checked for null
                  textGenService!);  // Use null-forgiving operator since we've already checked for null
            });

            return services;
        }
    }
}