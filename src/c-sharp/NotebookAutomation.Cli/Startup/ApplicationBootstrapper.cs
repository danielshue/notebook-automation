// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Startup;

/// <summary>
/// Service responsible for bootstrapping the application and setting up dependency injection.
/// </summary>
/// <remarks>
/// This service handles the initialization of the service container, configuration setup,
/// and registration of all application services with proper error handling and logging.
/// </remarks>
internal class ApplicationBootstrapper
{
    /// <summary>
    /// Sets up the dependency injection container with configuration and services.
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <param name="debug">Whether debug mode is enabled.</param>
    /// <returns>An <see cref="IServiceProvider"/> instance configured with application services.</returns>
    /// <exception cref="InvalidOperationException">Thrown when service setup fails.</exception>
    public IServiceProvider SetupDependencyInjection(string? configPath, bool debug)
    {
        // Determine environment
        string environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                             Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                             "Production";

        // Build configuration using ConfigurationSetup helper
        var configuration = ConfigurationSetup.BuildConfiguration<Program>(environment, configPath);

        // Setup service collection
        var services = new ServiceCollection();

        // Register configuration
        services.AddSingleton(configuration);

        // Add notebook automation services using ServiceRegistration
        services.AddNotebookAutomationServices(configuration, debug, configPath);

        // Register application services
        RegisterApplicationServices(services);

        // Build service provider
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Converts service setup exceptions to user-friendly messages.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <returns>User-friendly error message.</returns>
    public string GetServiceSetupFriendlyMessage(Exception? exception)
    {
        if (exception == null)
            return "An unknown error occurred during service initialization";

        return exception switch
        {
            InvalidOperationException ioe when ioe.Message.Contains("OpenAI API key is missing") =>
                "OpenAI API key is missing. Please set the OPENAI_API_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("Azure OpenAI API key is missing") =>
                "Azure OpenAI API key is missing. Please set the AZURE_OPENAI_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("Foundry API key is missing") =>
                "Foundry API key is missing. Please set the FOUNDRY_API_KEY environment variable with your API key",
            InvalidOperationException ioe when ioe.Message.Contains("endpoint is missing") =>
                "AI service endpoint configuration is missing. Please check your configuration file",
            InvalidOperationException ioe when ioe.Message.Contains("deployment name is missing") =>
                "Azure OpenAI deployment name is missing. Please check your configuration file",
            InvalidOperationException ioe when ioe.Message.Contains("Configuration") =>
                "Configuration error. Please check your config file and ensure all required settings are present",
            FileNotFoundException fnf => $"Required file not found: {fnf.FileName ?? "Unknown file"}",
            DirectoryNotFoundException => "Required directory not found",
            _ => $"Failed to initialize services: {exception.Message}"
        };
    }


    /// <summary>
    /// Registers application-specific services.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    private static void RegisterApplicationServices(IServiceCollection services)
    {
        // Register CLI-specific services
        services.AddTransient<Configuration.ConfigurationDiscoveryService>();
        services.AddTransient<UI.EnvironmentDisplayService>();
        services.AddTransient<UI.HelpDisplayService>();
        services.AddTransient<Cli.CommandLineBuilder>();
    }
}
