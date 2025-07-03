// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Service responsible for displaying environment and configuration information.
/// </summary>
/// <remarks>
/// This service handles the display of current environment settings, configuration file contents,
/// debug/verbose mode status, and AI service configuration with proper formatting and security.
/// </remarks>
internal class EnvironmentDisplayService
{
    private readonly ConfigurationDiscoveryService _configurationDiscoveryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentDisplayService"/> class.
    /// </summary>
    /// <param name="configurationDiscoveryService">Service for discovering configuration files.</param>
    public EnvironmentDisplayService(ConfigurationDiscoveryService configurationDiscoveryService)
    {
        _configurationDiscoveryService = configurationDiscoveryService ?? throw new ArgumentNullException(nameof(configurationDiscoveryService));
    }

    /// <summary>
    /// Displays current environment settings with appropriate colors and masking.
    /// </summary>
    /// <param name="configPath">The configuration file path.</param>
    /// <param name="isDebug">Whether debug mode is enabled.</param>
    /// <param name="args">Command line arguments for determining mode sources.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task DisplayEnvironmentSettingsAsync(string? configPath, bool isDebug, string[] args)
    {
        // Get the final configuration path for display
        var finalConfigPath = await _configurationDiscoveryService.DiscoverConfigurationForDisplayAsync(configPath);

        // Configuration file status
        if (!string.IsNullOrEmpty(finalConfigPath))
        {
            Console.WriteLine($"  Configuration:    {AnsiColors.GREY}{finalConfigPath}{AnsiColors.ENDC} {AnsiColors.OKGREEN}✓{AnsiColors.ENDC}");
        }
        else if (!string.IsNullOrEmpty(configPath))
        {
            // Show the specified path even if it doesn't exist
            Console.WriteLine($"  Configuration:    {AnsiColors.GREY}{configPath}{AnsiColors.ENDC} {AnsiColors.FAIL}✗{AnsiColors.ENDC}");
        }
        else
        {
            Console.WriteLine($"  Configuration:    {AnsiColors.WARNING}Using defaults (no config.json found) ⚠{AnsiColors.ENDC}");
        }

        // Debug mode status with source indication
        var debugEnvVar = Environment.GetEnvironmentVariable("DEBUG");
        var debugFromEnv = !string.IsNullOrEmpty(debugEnvVar) &&
                          (debugEnvVar.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                           debugEnvVar.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                           debugEnvVar.Equals("yes", StringComparison.OrdinalIgnoreCase));

        if (isDebug)
        {
            if (debugFromEnv)
            {
                Console.WriteLine($"  Debug Mode:       {AnsiColors.WARNING}Enabled via ENV ⚡{AnsiColors.ENDC}");
            }
            else
            {
                Console.WriteLine($"  Debug Mode:       {AnsiColors.WARNING}Enabled ⚡{AnsiColors.ENDC}");
            }
        }
        else
        {
            Console.WriteLine($"  Debug Mode:       {AnsiColors.GREY}Disabled{AnsiColors.ENDC}");
        }

        // Verbose mode status with source indication
        var isVerbose = CommandLineModeDetector.IsVerboseModeEnabled(args);
        var verboseEnvVar = Environment.GetEnvironmentVariable("VERBOSE");
        var verboseFromEnv = !string.IsNullOrEmpty(verboseEnvVar) &&
                            (verboseEnvVar.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                             verboseEnvVar.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                             verboseEnvVar.Equals("yes", StringComparison.OrdinalIgnoreCase));

        if (isVerbose)
        {
            if (verboseFromEnv)
            {
                Console.WriteLine($"  Verbose Mode:     {AnsiColors.OKCYAN}Enabled via ENV 📢{AnsiColors.ENDC}");
            }
            else
            {
                Console.WriteLine($"  Verbose Mode:     {AnsiColors.OKCYAN}Enabled 📢{AnsiColors.ENDC}");
            }
        }
        else
        {
            Console.WriteLine($"  Verbose Mode:     {AnsiColors.GREY}Disabled{AnsiColors.ENDC}");
        }

        // Working directory
        Console.WriteLine($"  Working Dir:      {AnsiColors.GREY}{Environment.CurrentDirectory}{AnsiColors.ENDC}");

        // .NET Runtime
        Console.WriteLine($"  .NET Runtime:     {AnsiColors.OKCYAN}{Environment.Version}{AnsiColors.ENDC}");

        // Load and display configuration settings if available
        if (!string.IsNullOrEmpty(finalConfigPath) && File.Exists(finalConfigPath))
        {
            try
            {
                await DisplayConfigurationSettingsAsync(finalConfigPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Config Status:    {AnsiColors.WARNING}Error reading config: {ex.Message}{AnsiColors.ENDC}");
            }
        }
    }


    /// <summary>
    /// Displays configuration settings from the config file, masking sensitive values.
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task DisplayConfigurationSettingsAsync(string configPath)
    {
        try
        {
            var configJson = await File.ReadAllTextAsync(configPath);
            var configDoc = JsonDocument.Parse(configJson);
            var root = configDoc.RootElement;

            // Display paths section
            if (root.TryGetProperty("paths", out var paths))
            {
                if (paths.TryGetProperty("notebook_vault_fullpath_root", out var vaultPath))
                {
                    Console.WriteLine($"  Obsidian Vault Root: {AnsiColors.GREY}{vaultPath.GetString()}{AnsiColors.ENDC}");
                }
                if (paths.TryGetProperty("onedrive_fullpath_root", out var oneDrivePath))
                {
                    Console.WriteLine($"  OneDrive Root:    {AnsiColors.GREY}{oneDrivePath.GetString()}{AnsiColors.ENDC}");
                }
                if (paths.TryGetProperty("prompts_path", out var promptsPath))
                {
                    Console.WriteLine($"  Prompts Path:     {AnsiColors.GREY}{promptsPath.GetString()}{AnsiColors.ENDC}");
                }
            }

            // Display AI service configuration
            if (root.TryGetProperty("aiservice", out var aiService))
            {
                if (aiService.TryGetProperty("provider", out var provider))
                {
                    Console.WriteLine($"  AI Provider:      {AnsiColors.OKCYAN}{provider.GetString()}{AnsiColors.ENDC}");
                }

                // Check for Azure configuration
                if (aiService.TryGetProperty("azure", out var azure))
                {
                    if (azure.TryGetProperty("endpoint", out var endpoint))
                    {
                        Console.WriteLine($"  AI Endpoint:      {AnsiColors.GREY}{endpoint.GetString()}{AnsiColors.ENDC}");
                    }
                    if (azure.TryGetProperty("deployment", out var deployment))
                    {
                        Console.WriteLine($"  AI Deployment:    {AnsiColors.OKCYAN}{deployment.GetString()}{AnsiColors.ENDC}");
                    }
                    if (azure.TryGetProperty("model", out var model))
                    {
                        Console.WriteLine($"  AI Model:         {AnsiColors.OKCYAN}{model.GetString()}{AnsiColors.ENDC}");
                    }

                    // Check for API key in config (unlikely to be there, but check anyway)
                    var hasApiKey = azure.TryGetProperty("api_key", out var apiKey) &&
                                   !string.IsNullOrWhiteSpace(apiKey.GetString());
                    if (hasApiKey)
                    {
                        Console.WriteLine($"  Azure API Key:    {AnsiColors.OKGREEN}[SET] ✓{AnsiColors.ENDC}");
                    }
                    else
                    {
                        // Check environment variable for Azure OpenAI key
                        var envKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY");
                        if (!string.IsNullOrWhiteSpace(envKey))
                        {
                            Console.WriteLine($"  Azure API Key:    {AnsiColors.OKGREEN}[SET via ENV] ✓{AnsiColors.ENDC}");
                        }
                        else
                        {
                            Console.WriteLine($"  Azure API Key:    {AnsiColors.WARNING}[NOT SET] ⚠{AnsiColors.ENDC}");
                        }
                    }
                }

                // Check for OpenAI configuration
                if (aiService.TryGetProperty("openai", out var openAi))
                {
                    if (openAi.TryGetProperty("endpoint", out var endpoint))
                    {
                        Console.WriteLine($"  OpenAI Endpoint:  {AnsiColors.GREY}{endpoint.GetString()}{AnsiColors.ENDC}");
                    }
                    if (openAi.TryGetProperty("model", out var model))
                    {
                        Console.WriteLine($"  OpenAI Model:     {AnsiColors.OKCYAN}{model.GetString()}{AnsiColors.ENDC}");
                    }

                    var hasApiKey = openAi.TryGetProperty("api_key", out var apiKey) &&
                                   !string.IsNullOrWhiteSpace(apiKey.GetString());
                    if (hasApiKey)
                    {
                        Console.WriteLine($"  OpenAI API Key:   {AnsiColors.OKGREEN}[SET] ✓{AnsiColors.ENDC}");
                    }
                    else
                    {
                        // Check environment variable for OpenAI key
                        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                        if (!string.IsNullOrWhiteSpace(envKey))
                        {
                            Console.WriteLine($"  OpenAI API Key:   {AnsiColors.OKGREEN}[SET via ENV] ✓{AnsiColors.ENDC}");
                        }
                        else
                        {
                            Console.WriteLine($"  OpenAI API Key:   {AnsiColors.WARNING}[NOT SET] ⚠{AnsiColors.ENDC}");
                        }
                    }
                }

                // Check for Foundry configuration
                if (aiService.TryGetProperty("foundry", out var foundry))
                {
                    if (foundry.TryGetProperty("endpoint", out var endpoint))
                    {
                        Console.WriteLine($"  Foundry Endpoint: {AnsiColors.GREY}{endpoint.GetString()}{AnsiColors.ENDC}");
                    }
                    if (foundry.TryGetProperty("model", out var model))
                    {
                        Console.WriteLine($"  Foundry Model:    {AnsiColors.OKCYAN}{model.GetString()}{AnsiColors.ENDC}");
                    }

                    // Check environment variable for Foundry key
                    var envKey = Environment.GetEnvironmentVariable("FOUNDRY_API_KEY");
                    if (!string.IsNullOrWhiteSpace(envKey))
                    {
                        Console.WriteLine($"  Foundry API Key:  {AnsiColors.OKGREEN}[SET via ENV] ✓{AnsiColors.ENDC}");
                    }
                    else
                    {
                        Console.WriteLine($"  Foundry API Key:  {AnsiColors.WARNING}[NOT SET] ⚠{AnsiColors.ENDC}");
                    }
                }
            }

            // Check for Microsoft Graph configuration
            if (root.TryGetProperty("microsoft_graph", out var msGraph))
            {
                if (msGraph.TryGetProperty("client_id", out var clientId))
                {
                    Console.WriteLine($"  OneDrive Graph Client ID: {AnsiColors.GREY}{clientId.GetString()}{AnsiColors.ENDC}");
                }
            }
        }
        catch (JsonException)
        {
            Console.WriteLine($"  Config Status:    {AnsiColors.WARNING}Invalid JSON format ⚠{AnsiColors.ENDC}");
        }
    }
}
