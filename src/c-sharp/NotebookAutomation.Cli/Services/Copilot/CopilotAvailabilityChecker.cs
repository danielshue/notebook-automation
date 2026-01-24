// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Utility to check if AI Copilot service is available with proper configuration.
/// </summary>
public class CopilotAvailabilityChecker(
    ILogger<CopilotAvailabilityChecker> logger,
    AppConfig appConfig)
{
    private readonly ILogger<CopilotAvailabilityChecker> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AppConfig appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

    /// <summary>
    /// Check if AI Copilot service is properly configured and available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability result with details.</returns>
    public Task<CopilotAvailabilityResult> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Copilot is enabled in configuration
            if (!appConfig.Copilot.Enabled)
            {
                logger.LogDebug("Copilot is disabled in configuration");
                return Task.FromResult(new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: false,
                    IsAuthenticated: false,
                    CliVersion: null,
                    ErrorMessage: "Copilot is disabled in configuration. Set 'copilot.enabled' to true in config.json."));
            }

            // Check if AI service is configured
            if (appConfig.AiService == null)
            {
                logger.LogDebug("AI service configuration is missing");
                return Task.FromResult(new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: false,
                    IsAuthenticated: false,
                    CliVersion: null,
                    ErrorMessage: "AI service not configured. Add 'aiservice' section to config.json."));
            }

            // Check if API key is available
            var apiKey = appConfig.AiService.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("No AI API key configured");
                return Task.FromResult(new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: false,
                    IsAuthenticated: false,
                    CliVersion: null,
                    ErrorMessage: "No AI API key configured. Set AZURE_OPENAI_KEY, OPENAI_API_KEY, or FOUNDRY_API_KEY environment variable."));
            }

            // Check provider configuration
            var provider = appConfig.AiService.Provider?.ToLowerInvariant() ?? "openai";
            var version = GetProviderVersion(provider);

            logger.LogInformation("AI Copilot is available with {Provider} provider", provider);
            return Task.FromResult(new CopilotAvailabilityResult(
                IsAvailable: true,
                IsCliInstalled: true,
                IsAuthenticated: true,
                CliVersion: version,
                ErrorMessage: null));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking Copilot availability");
            return Task.FromResult(new CopilotAvailabilityResult(
                IsAvailable: false,
                IsCliInstalled: false,
                IsAuthenticated: false,
                CliVersion: null,
                ErrorMessage: $"Error checking Copilot availability: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get the provider version information.
    /// </summary>
    private string GetProviderVersion(string provider)
    {
        return provider switch
        {
            "azure" => $"Azure OpenAI ({appConfig.AiService?.Azure?.Deployment ?? "default"})",
            "openai" => $"OpenAI ({appConfig.AiService?.OpenAI?.Model ?? "gpt-4"})",
            "foundry" => $"Foundry ({appConfig.AiService?.Foundry?.Model ?? "default"})",
            _ => $"Unknown provider: {provider}"
        };
    }
}
