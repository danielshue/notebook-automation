// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Services.Copilot;

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

        // Register main Copilot service
        services.AddSingleton<ICopilotService, CopilotService>();

        // Register built-in commands handler
        services.AddSingleton<ChatBuiltInCommands>();

        // Register tool management
        services.AddSingleton<INotebookTools, NotebookTools>();
        services.AddSingleton<ISystemMessageBuilder, SystemMessageBuilder>();

        // Register session management (Phase 4)
        services.AddSingleton<ISessionManager, SessionManager>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        services.AddSingleton<IGitService, GitService>();

        return services;
    }
}
