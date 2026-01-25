// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Services.Copilot;
using NotebookAutomation.Core.Tools.Vault;
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

        // Register tool management
        services.AddSingleton<INotebookTools>(sp => new NotebookTools(
            sp.GetRequiredService<ILogger<NotebookTools>>(),
            sp,
            sp.GetRequiredService<AppConfig>(),
            sp.GetService<IVaultBrowserService>(),
            sp.GetService<IVaultSearchService>()));
        services.AddSingleton<ISystemMessageBuilder, SystemMessageBuilder>();

        // Register session management (Phase 4)
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        services.AddSingleton<IGitService, GitService>();

        return services;
    }
}
