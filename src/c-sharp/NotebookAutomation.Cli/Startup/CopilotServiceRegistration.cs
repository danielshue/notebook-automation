// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Services.Copilot;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Configuration.Validation;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools;
using NotebookAutomation.Core.Tools.MarkdownGeneration;
using NotebookAutomation.Core.Tools.PdfProcessing;
using NotebookAutomation.Core.Tools.TagManagement;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Tools.VideoProcessing;
using NotebookAutomation.Core.Tools.VideoTranscriptProcessing;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Cli.Startup;

/// <summary>
/// Extension methods for registering Copilot-related services.
/// </summary>
public static class CopilotServiceRegistration
{
    /// <summary>
    /// Adds GitHub Copilot SDK services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCopilotServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register availability checker
        services.AddSingleton<CopilotAvailabilityChecker>();

        // Register vault browser and search services
        services.AddSingleton<IVaultBrowserService>(sp =>
        {
            var appConfig = sp.GetRequiredService<AppConfig>();
            var vaultPath = appConfig.Paths?.NotebookVaultFullpathRoot;
            if (string.IsNullOrEmpty(vaultPath))
            {
                // Return null if vault not configured - NotebookTools will handle this gracefully
                return null!;
            }

            return new VaultBrowserService(
                sp.GetRequiredService<ILogger<VaultBrowserService>>(),
                sp.GetRequiredService<IYamlHelper>(),
                vaultPath);
        });

        services.AddSingleton<IVaultSearchService>(sp =>
        {
            var vaultBrowser = sp.GetService<IVaultBrowserService>();
            if (vaultBrowser == null)
            {
                return null!;
            }

            return new VaultSearchService(
                sp.GetRequiredService<ILogger<VaultSearchService>>(),
                vaultBrowser,
                sp.GetRequiredService<IYamlHelper>());
        });

        // Register main Copilot service with all dependencies
        services.AddSingleton<ICopilotService>(sp => new CopilotService(
            sp.GetRequiredService<ILogger<CopilotService>>(),
            sp.GetRequiredService<CopilotAvailabilityChecker>(),
            sp.GetRequiredService<ISessionManager>(),
            sp.GetRequiredService<INotebookTools>(),
            sp.GetRequiredService<ISystemMessageBuilder>(),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<AppConfig>()));

        // Register built-in commands handler
        services.AddSingleton<ChatBuiltInCommands>();

        // Register tag management service
        services.AddSingleton<ITagService>(sp =>
        {
            var appConfig = sp.GetRequiredService<AppConfig>();
            var vaultPath = appConfig.Paths?.NotebookVaultFullpathRoot;
            if (string.IsNullOrEmpty(vaultPath))
            {
                return null!;
            }

            return new TagService(
                sp.GetRequiredService<ILogger<TagService>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IYamlHelper>(),
                sp.GetService<IMetadataSchemaLoader>(),
                vaultPath);
        });

        // Register configuration service
        services.AddSingleton<IConfigService>(sp =>
        {
            return new ConfigService(
                sp.GetRequiredService<AppConfig>(),
                sp.GetRequiredService<UserSecretsHelper>(),
                sp.GetRequiredService<IConfigurationValidationService>(),
                sp.GetRequiredService<ILogger<ConfigService>>());
        });

        // Register video processing service
        services.AddSingleton<IVideoService>(sp =>
        {
            var videoBatchProcessor = sp.GetService<VideoNoteBatchProcessor>();
            var consolidationService = sp.GetService<VideoTranscriptConsolidationService>();
            if (videoBatchProcessor == null || consolidationService == null)
            {
                return null!;
            }

            return new VideoService(
                sp.GetRequiredService<ILogger<VideoService>>(),
                videoBatchProcessor,
                consolidationService,
                sp.GetRequiredService<AppConfig>(),
                sp.GetRequiredService<UserSecretsHelper>());
        });

        // Register PDF processing service
        services.AddSingleton<IPdfService>(sp =>
        {
            var pdfBatchProcessor = sp.GetService<PdfNoteBatchProcessor>();
            if (pdfBatchProcessor == null)
            {
                return null!;
            }

            return new PdfService(
                sp.GetRequiredService<ILogger<PdfService>>(),
                pdfBatchProcessor,
                sp.GetRequiredService<AppConfig>(),
                sp.GetRequiredService<UserSecretsHelper>());
        });

        // Register markdown generation service
        services.AddSingleton<IMarkdownService>(sp =>
        {
            var markdownBatchProcessor = sp.GetService<MarkdownNoteBatchProcessor>();
            if (markdownBatchProcessor == null)
            {
                return null!;
            }

            return new MarkdownService(
                sp.GetRequiredService<ILogger<MarkdownService>>(),
                markdownBatchProcessor,
                sp.GetRequiredService<AppConfig>(),
                sp.GetRequiredService<UserSecretsHelper>());
        });

        // Register OneDrive tool service
        services.AddSingleton<IOneDriveToolService>(sp =>
        {
            return new OneDriveToolService(
                sp.GetRequiredService<ILogger<OneDriveToolService>>(),
                sp.GetService<IOneDriveService>(),
                sp.GetRequiredService<AppConfig>());
        });

        // Register tool management
        services.AddSingleton<INotebookTools>(sp => new NotebookTools(
            sp.GetRequiredService<ILogger<NotebookTools>>(),
            sp,
            sp.GetRequiredService<AppConfig>(),
            sp.GetService<IVaultBrowserService>(),
            sp.GetService<IVaultSearchService>(),
            sp.GetService<ITagService>(),
            sp.GetService<IConfigService>(),
            sp.GetService<IVideoService>(),
            sp.GetService<IPdfService>(),
            sp.GetService<IMarkdownService>(),
            sp.GetService<IOneDriveToolService>()));
        services.AddSingleton<ISystemMessageBuilder, SystemMessageBuilder>();

        // Register session management (Phase 4)
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        services.AddSingleton<IGitService, GitService>();

        return services;
    }
}
