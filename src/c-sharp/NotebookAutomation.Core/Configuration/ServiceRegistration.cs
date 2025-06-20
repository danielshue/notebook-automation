// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NotebookAutomation.Core.Configuration;

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
    /// <param name="configFilePath">Optional path to the configuration file.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddNotebookAutomationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool debug = false,
        string? configFilePath = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register and configure services by category
        RegisterLoggingServices(services, configuration, debug);
        RegisterConfigurationServices(services, configuration, configFilePath, debug);
        RegisterMetadataServices(services);
        RegisterCloudServices(services);
        RegisterAIServices(services);
        RegisterDocumentProcessors(services);

        return services;
    }

    /// <summary>
    /// Adds core notebook automation services to the dependency injection container using the new ConfigManager.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">Configuration discovery options.</param>
    /// <param name="debug">Whether debug mode is enabled.</param>
    /// <returns>The configured service collection.</returns>
    public static async Task<IServiceCollection> AddNotebookAutomationServicesAsync(
        this IServiceCollection services,
        ConfigDiscoveryOptions options,
        bool debug = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        // First register the basic infrastructure services
        services.AddSingleton<IFileSystemWrapper, FileSystemWrapper>();
        services.AddSingleton<IEnvironmentWrapper, EnvironmentWrapper>();

        // Add basic logging (we'll enhance it later)
        services.AddLogging(builder => builder.AddConsole());

        // Create a temporary service provider to get the ConfigManager
        using var tempProvider = services.BuildServiceProvider();
        var logger = tempProvider.GetRequiredService<ILogger<ConfigManager>>();
        var fileSystem = tempProvider.GetRequiredService<IFileSystemWrapper>();
        var environment = tempProvider.GetRequiredService<IEnvironmentWrapper>();

        var configManager = new ConfigManager(fileSystem, environment, logger);
        services.AddSingleton<IConfigManager>(configManager);

        // Use ConfigManager to discover and load configuration
        var configResult = await configManager.LoadConfigurationAsync(options);
        if (!configResult.IsSuccess || configResult.Configuration == null)
        {
            var exception = configResult.Exception != null
                ? new ConfigurationException($"Failed to load configuration: {configResult.ErrorMessage}", configResult.Exception)
                : new ConfigurationException($"Failed to load configuration: {configResult.ErrorMessage}");
            throw exception;
        }

        // Build IConfiguration from the discovered AppConfig
        var configurationBuilder = new ConfigurationBuilder();        // If we have a discovered config file, add it
        if (configResult.IsSuccess && !string.IsNullOrEmpty(configResult.ConfigurationPath))
        {
            configurationBuilder.AddJsonFile(configResult.ConfigurationPath, optional: false, reloadOnChange: true);
        }

        // Add environment variables
        configurationBuilder.AddEnvironmentVariables();

        // Add user secrets in development
        if (environment.IsDevelopment())
        {
            // This would need to be enhanced based on the specific assembly requirements
            // configurationBuilder.AddUserSecrets<Program>();
        }

        var configuration = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Register the discovered AppConfig
        services.AddSingleton(configResult.Configuration);

        // Register remaining services
        RegisterLoggingServices(services, configuration, debug);
        RegisterMetadataServices(services);
        RegisterCloudServices(services);
        RegisterAIServices(services);
        RegisterDocumentProcessors(services);

        return services;
    }

    /// <summary>
    /// Registers AI-related services including Semantic Kernel and AISummarizer.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    private static IServiceCollection RegisterAIServices(IServiceCollection services)
    {
        // Register prompt template service
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<PromptTemplateService>();
            var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            return new PromptTemplateService(logger, yamlHelper, appConfig);
        });

        // Register Semantic Kernel
        services.AddScoped(provider =>
        {
            var appConfig = provider.GetRequiredService<AppConfig>();
            var aiConfig = appConfig.AiService;
            var loggingService = provider.GetRequiredService<LoggingService>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>(); var skbuilder = Kernel.CreateBuilder();            // add logging to Semantic Kernel - configure logging first with SK-specific filtering
            skbuilder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConsole().SetMinimumLevel(LogLevel.Debug);
                // Add specific filters for SK components
                loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
                loggingBuilder.AddFilter("Microsoft.SemanticKernel", LogLevel.Debug);
                loggingBuilder.AddFilter("Microsoft.SemanticKernel.KernelFunction", LogLevel.Trace);
                loggingBuilder.AddFilter("Microsoft.SemanticKernel.KernelPromptTemplate", LogLevel.Trace);
            });

            // Add the existing logger factory from DI
            skbuilder.Services.AddSingleton(loggerFactory); var providerType = aiConfig.Provider?.ToLowerInvariant() ?? "openai";
            if (providerType == "openai" && aiConfig.OpenAI != null)
            {
                var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                var endpoint = aiConfig.OpenAI.Endpoint ?? "https://api.openai.com/v1/chat/completions";
                var model = aiConfig.OpenAI.Model ?? "gpt-4o";

                // Validate OpenAI configuration before attempting to register
                if (string.IsNullOrWhiteSpace(openAiKey))
                {
                    throw new InvalidOperationException("OpenAI API key is missing. Please set the OPENAI_API_KEY environment variable.");
                }

                skbuilder.AddOpenAIChatCompletion(model, openAiKey, endpoint);
            }
            else if (providerType == "azure" && aiConfig.Azure != null)
            {
                var azureKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ?? string.Empty;
                var endpoint = aiConfig.Azure.Endpoint ?? string.Empty;
                var deployment = aiConfig.Azure.Deployment ?? string.Empty;
                var model = aiConfig.Azure.Model ?? string.Empty;

                // Validate Azure configuration before attempting to register
                if (string.IsNullOrWhiteSpace(azureKey))
                {
                    throw new InvalidOperationException("Azure OpenAI API key is missing. Please set the AZURE_OPENAI_KEY environment variable.");
                }
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    throw new InvalidOperationException("Azure OpenAI endpoint is missing. Please check your configuration file.");
                }
                if (string.IsNullOrWhiteSpace(deployment))
                {
                    throw new InvalidOperationException("Azure OpenAI deployment name is missing. Please check your configuration file.");
                }

                skbuilder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey: azureKey, null, modelId: model ?? string.Empty);
            }
            else if (providerType == "foundry" && aiConfig.Foundry != null)
            {
                var foundryKey = Environment.GetEnvironmentVariable("FOUNDRY_API_KEY") ?? string.Empty;
                var endpoint = aiConfig.Foundry.Endpoint ?? string.Empty;
                var model = aiConfig.Foundry.Model ?? string.Empty;

                // Validate Foundry configuration before attempting to register
                if (string.IsNullOrWhiteSpace(foundryKey))
                {
                    throw new InvalidOperationException("Foundry API key is missing. Please set the FOUNDRY_API_KEY environment variable.");
                }
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    throw new InvalidOperationException("Foundry endpoint is missing. Please check your configuration file.");
                }
                skbuilder.AddOpenAIChatCompletion(model, foundryKey, endpoint);
            }

            // Build the kernel
            var kernel = skbuilder.Build();

            // Get a logger to verify SK configuration
            var skLogger = kernel.GetRequiredService<ILoggerFactory>().CreateLogger("SemanticKernel");
            skLogger.LogInformation("Semantic Kernel configured successfully with {Provider} provider", providerType);

            return kernel;
        });        // Register AISummarizer
        services.AddScoped<IAISummarizer>(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<AISummarizer>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            var promptService = provider.GetRequiredService<PromptTemplateService>();
            var semanticKernel = provider.GetRequiredService<Kernel>();

            // Get timeout configuration from AI service config
            var timeoutConfig = appConfig.AiService.Timeout ?? new TimeoutConfig();

            return new AISummarizer(
              logger,
              promptService,
              semanticKernel,
              null, // chunkingService - use default
              timeoutConfig);
        });

        return services;
    }

    /// <summary>
    /// Registers document processing services such as PDF and Video processors.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    private static IServiceCollection RegisterDocumentProcessors(IServiceCollection services)
    {        // Register Video processor
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<VideoNoteProcessor>(); var aiSummarizer = provider.GetRequiredService<IAISummarizer>();
            var appConfig = provider.GetRequiredService<AppConfig>();

            // Use GetService instead of GetRequiredService since OneDriveService is optional
            var oneDriveService = provider.GetService<IOneDriveService>(); var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            var templateManager = provider.GetRequiredService<IMetadataTemplateManager>();
            var courseStructureExtractor = provider.GetRequiredService<ICourseStructureExtractor>();
            var markdownBuilder = provider.GetRequiredService<MarkdownNoteBuilder>();

            // Pass parameters in correct order: logger, aiSummarizer, yamlHelper, hierarchyDetector, templateManager, courseStructureExtractor, markdownBuilder, oneDriveService, appConfig
            return new VideoNoteProcessor(logger, aiSummarizer, yamlHelper, hierarchyDetector, templateManager, courseStructureExtractor, markdownBuilder, oneDriveService, appConfig);
        });

        // Register VideoNoteBatchProcessor
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var batchLogger = loggingService.GetLogger<DocumentNoteBatchProcessor<VideoNoteProcessor>>();
            var processorLogger = loggingService.GetLogger<VideoNoteProcessor>();
            var aiSummarizer = provider.GetRequiredService<IAISummarizer>();
            var appConfig = provider.GetRequiredService<AppConfig>(); var oneDriveService = provider.GetService<IOneDriveService>();
            var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            var templateManager = provider.GetRequiredService<IMetadataTemplateManager>();
            var courseStructureExtractor = provider.GetRequiredService<ICourseStructureExtractor>();
            var markdownBuilder = provider.GetRequiredService<MarkdownNoteBuilder>();

            var videoProcessor = new VideoNoteProcessor(processorLogger, aiSummarizer, yamlHelper, hierarchyDetector, templateManager, courseStructureExtractor, markdownBuilder, oneDriveService, appConfig);
            var batchProcessor = new DocumentNoteBatchProcessor<VideoNoteProcessor>(batchLogger, videoProcessor, aiSummarizer);
            return new VideoNoteBatchProcessor(batchProcessor);
        });

        // Register PDF processor
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<PdfNoteProcessor>();
            var aiSummarizer = provider.GetRequiredService<IAISummarizer>();
            var appConfig = provider.GetRequiredService<AppConfig>(); var oneDriveService = provider.GetService<IOneDriveService>();
            var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            var templateManager = provider.GetRequiredService<IMetadataTemplateManager>();
            var courseStructureExtractor = provider.GetRequiredService<ICourseStructureExtractor>();
            var markdownBuilder = provider.GetRequiredService<MarkdownNoteBuilder>();

            return new PdfNoteProcessor(logger, aiSummarizer, yamlHelper, hierarchyDetector, templateManager, courseStructureExtractor, markdownBuilder, oneDriveService, appConfig, appConfig.PdfExtractImages);
        });

        // Register PDF Batch Processor
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var batchLogger = loggingService.GetLogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>();
            var processorLogger = loggingService.GetLogger<PdfNoteProcessor>(); var aiSummarizer = provider.GetRequiredService<IAISummarizer>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            var oneDriveService = provider.GetService<IOneDriveService>();
            var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            var templateManager = provider.GetRequiredService<IMetadataTemplateManager>();
            var courseStructureExtractor = provider.GetRequiredService<ICourseStructureExtractor>();
            var markdownBuilder = provider.GetRequiredService<MarkdownNoteBuilder>();

            var pdfProcessor = new PdfNoteProcessor(processorLogger, aiSummarizer, yamlHelper, hierarchyDetector, templateManager, courseStructureExtractor, markdownBuilder, oneDriveService, appConfig, appConfig.PdfExtractImages);
            var batchProcessor = new DocumentNoteBatchProcessor<PdfNoteProcessor>(batchLogger, pdfProcessor, aiSummarizer);
            return new PdfNoteBatchProcessor(batchProcessor);
        });

        // Register Markdown processor
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<MarkdownNoteProcessor>();
            var aiSummarizer = provider.GetRequiredService<AISummarizer>(); var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            var markdownNoteBuilder = provider.GetRequiredService<MarkdownNoteBuilder>();

            return new MarkdownNoteProcessor(logger, aiSummarizer, (MetadataHierarchyDetector)hierarchyDetector, markdownNoteBuilder, appConfig);
        });

        // Register Vault Metadata processors
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<MetadataEnsureProcessor>();
            var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            var metadataDetector = provider.GetRequiredService<MetadataHierarchyDetector>();
            var structureExtractor = provider.GetRequiredService<ICourseStructureExtractor>();
            return new MetadataEnsureProcessor(logger, yamlHelper, metadataDetector, structureExtractor);
        });

        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<MetadataEnsureBatchProcessor>();
            var metadataProcessor = provider.GetRequiredService<MetadataEnsureProcessor>();
            return new MetadataEnsureBatchProcessor(logger, metadataProcessor);
        });        // Register Vault Index Content Generator
        services.AddScoped<IVaultIndexContentGenerator, VaultIndexContentGenerator>(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<VaultIndexContentGenerator>();
            var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            var noteBuilder = provider.GetRequiredService<MarkdownNoteBuilder>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            return new VaultIndexContentGenerator(logger, hierarchyDetector, noteBuilder, appConfig);
        });        // Register Vault Index processors
        services.AddScoped<IVaultIndexProcessor, VaultIndexProcessor>(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<VaultIndexProcessor>();
            var templateManager = provider.GetRequiredService<IMetadataTemplateManager>();
            var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            var structureExtractor = provider.GetRequiredService<ICourseStructureExtractor>();
            var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            var noteBuilder = provider.GetRequiredService<MarkdownNoteBuilder>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            var contentGenerator = provider.GetRequiredService<IVaultIndexContentGenerator>();
            return new VaultIndexProcessor(logger, templateManager, hierarchyDetector, structureExtractor, yamlHelper, noteBuilder, appConfig, contentGenerator);
        }); services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<VaultIndexBatchProcessor>();
            var indexProcessor = provider.GetRequiredService<IVaultIndexProcessor>();
            var hierarchyDetector = provider.GetRequiredService<IMetadataHierarchyDetector>();
            return new VaultIndexBatchProcessor(logger, indexProcessor, hierarchyDetector);
        });

        return services;
    }

    /// <summary>
    /// Registers cloud-related services like OneDriveService.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    private static IServiceCollection RegisterCloudServices(IServiceCollection services)
    {
        // Register OneDrive service
        services.AddScoped<IOneDriveService>(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<OneDriveService>();
            var microsoftGraph = provider.GetRequiredService<AppConfig>().MicrosoftGraph;
            return new OneDriveService(
                logger,
                microsoftGraph?.ClientId ?? string.Empty,
                microsoftGraph?.TenantId ?? string.Empty,
                microsoftGraph?.Scopes?.ToArray() ?? []);
        });

        return services;
    }

    /// <summary>
    /// Registers and configures logging services for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="debug">Whether debug mode is enabled.</param>
    /// <returns>The configured service collection.</returns>
    private static IServiceCollection RegisterLoggingServices(IServiceCollection services, IConfiguration configuration, bool debug)
    {
        // Determine logging directory from configuration
        var loggingDir = configuration.GetSection("paths:logging_dir")?.Value ?? configuration.GetSection("paths:loggingDir")?.Value;
        if (string.IsNullOrWhiteSpace(loggingDir))
        {
            loggingDir = Path.Combine(AppContext.BaseDirectory, "logs");
        }

        // Register the LoggingService as a singleton early, before any other components
        services.AddSingleton<ILoggingService>(_ => new LoggingService(loggingDir, debug));
        services.AddSingleton(provider => (LoggingService)provider.GetRequiredService<ILoggingService>());        // Now configure Microsoft.Extensions.Logging to use our LoggingService
        services.AddLogging(builder =>
        {
            var provider = services.BuildServiceProvider();
            var loggingService = provider.GetRequiredService<ILoggingService>();
            loggingService.ConfigureLogging(builder);
        });        // Configure OpenTelemetry for enhanced tracing and debugging
        services.AddOpenTelemetry()
            .WithTracing(traceBuilder =>
            {
                traceBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("NotebookAutomation.Core", "1.0.0"))
                    .AddSource("NotebookAutomation.Core") // Our custom ActivitySource
                    .AddSource("Microsoft.SemanticKernel*") // SK internal traces (corrected pattern)
                    .AddHttpClientInstrumentation() // Trace HTTP calls to AI providers
                    .AddConsoleExporter(); // Export traces to console for debugging

                // Enable more detailed tracing in debug mode
                if (debug)
                {
                    traceBuilder.SetSampler(new AlwaysOnSampler());
                }
            });

        return services;
    }

    /// <summary>
    /// Registers configuration services for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configFilePath">Optional path to the configuration file.</param>
    /// <param name="debug">Whether debug mode is enabled.</param>
    /// <returns>The configured service collection.</returns>
    private static IServiceCollection RegisterConfigurationServices(
        IServiceCollection services,
        IConfiguration configuration,
        string? configFilePath,
        bool debug)
    {
        // Register new configuration management interfaces and implementations
        services.AddSingleton<IFileSystemWrapper, FileSystemWrapper>();
        services.AddSingleton<IEnvironmentWrapper, EnvironmentWrapper>();
        services.AddSingleton<IConfigManager, ConfigManager>();

        // Register the configuration as a singleton
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

        // Register VaultRootContextService as scoped for vault root overrides
        services.AddScoped<VaultRootContextService>();

        // Register YamlHelper with proper logging from LoggingService
        services.AddSingleton<IYamlHelper>(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<YamlHelper>();
            return new YamlHelper(logger);
        });

        return services;
    }

    /// <summary>
    /// Registers metadata-related services for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    private static IServiceCollection RegisterMetadataServices(IServiceCollection services)
    {        // Register metadata-related services
        services.AddScoped<IMetadataTemplateManager>(provider =>
        {
            var logger = provider.GetRequiredService<ILoggingService>().GetLogger<MetadataTemplateManager>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            var yamlHelper = provider.GetRequiredService<IYamlHelper>();
            return new MetadataTemplateManager(logger, appConfig, yamlHelper);
        });

        // Register IMetadataHierarchyDetector with factory
        services.AddScoped<IMetadataHierarchyDetector>(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<MetadataHierarchyDetector>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            var vaultRootContext = provider.GetRequiredService<VaultRootContextService>();            // Use vault root override if available, otherwise use config
            string? vaultRootOverride = vaultRootContext.HasVaultRootOverride
                ? vaultRootContext.VaultRootOverride
                : null;

            return new MetadataHierarchyDetector(
                logger,
                appConfig,
                vaultRootOverride
            );
        });

        // Register concrete MetadataHierarchyDetector for backward compatibility
        services.AddScoped<MetadataHierarchyDetector>(provider =>
            (MetadataHierarchyDetector)provider.GetRequiredService<IMetadataHierarchyDetector>());        // Register ICourseStructureExtractor with factory
        services.AddScoped<ICourseStructureExtractor>(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<CourseStructureExtractor>();
            var appConfig = provider.GetRequiredService<AppConfig>();
            return new CourseStructureExtractor(logger, appConfig);
        });

        // Register CourseStructureExtractor (concrete class) for processors that expect it
        services.AddScoped<CourseStructureExtractor>(provider =>
            (CourseStructureExtractor)provider.GetRequiredService<ICourseStructureExtractor>());

        // Register MarkdownNoteBuilder with factory
        services.AddScoped(provider =>
        {
            var loggingService = provider.GetRequiredService<ILoggingService>();
            var logger = loggingService.GetLogger<MarkdownNoteBuilder>();
            var yaml = provider.GetRequiredService<IYamlHelper>();
            var appConfig = provider.GetRequiredService<AppConfig>();

            return new MarkdownNoteBuilder(yaml, appConfig);
        });

        services.AddScoped<TagProcessor>();

        return services;
    }
}
