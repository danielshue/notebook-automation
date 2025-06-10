// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides CLI commands for managing application configuration.
/// </summary>
/// <remarks>
/// <para>
/// This class registers the 'config' command group, including subcommands for:
/// <list type="bullet">
/// <item><description>Displaying available configuration keys</description></item>
/// <item><description>Updating configuration values</description></item>
/// </list>
/// </para>
/// <para>
/// The configuration keys include paths, Microsoft Graph API settings, video extensions,
/// and AI service provider settings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var rootCommand = new RootCommand();
/// ConfigCommands.Register(rootCommand);
/// rootCommand.Invoke("config list");
/// </code>
/// </example>
internal class ConfigCommands
{
    /// <summary>
    /// Prints the available configuration keys that can be updated.
    /// </summary>
    /// <param name="context">The invocation context for output.</param>
    /// <remarks>
    /// <para>
    /// This method displays a list of configuration keys that can be updated via the CLI.
    /// The keys are grouped by category, such as paths, Microsoft Graph API settings, and AI service settings.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// ConfigCommands.PrintAvailableConfigKeys(context);
    /// </code>
    /// </example>
    public static void PrintAvailableConfigKeys(InvocationContext context)
    {
        context.Console.WriteLine("Available configuration keys:");
        context.Console.WriteLine("  paths.onedrive_fullpath_root      - Full path to OneDrive local resources directory");
        context.Console.WriteLine("  paths.notebook_vault_fullpath_root - Full path to the notebook vault root directory");
        context.Console.WriteLine("  paths.metadata_file                - Path to the metadata file");
        context.Console.WriteLine("  paths.logging_dir                  - Directory for log files");
        context.Console.WriteLine("  paths.prompts_path                 - Directory containing prompt template files");
        context.Console.WriteLine("  paths.onedrive_resources_basepath  - Base path for OneDrive resources");
        context.Console.WriteLine("---");
        context.Console.WriteLine("  microsoft_graph.client_id          - Microsoft Graph API client ID");
        context.Console.WriteLine("  microsoft_graph.api_endpoint       - Microsoft Graph API endpoint");
        context.Console.WriteLine("  microsoft_graph.authority          - Microsoft Graph API authority");
        context.Console.WriteLine("  microsoft_graph.scopes             - Microsoft Graph API scopes (comma-separated)");
        context.Console.WriteLine("---");
        context.Console.WriteLine("  video_extensions                   - Video file extensions (comma-separated)");
        context.Console.WriteLine("---");
        context.Console.WriteLine("  aiservice.provider                 - AI provider to use (openai, azure, foundry)");
        context.Console.WriteLine("  aiservice.openai.endpoint          - OpenAI API endpoint (if using OpenAI)");
        context.Console.WriteLine("  aiservice.openai.model             - OpenAI API model (if using OpenAI)");
        context.Console.WriteLine("  aiservice.openai.key               - OpenAI API key (if using OpenAI)");
        context.Console.WriteLine("---");
        context.Console.WriteLine("  aiservice.azureopenai.endpoint     - Azure OpenAI API endpoint (if using Azure OpenAI)");
        context.Console.WriteLine("  aiservice.azureopenai.deployment   - Azure OpenAI API deployment (if using Azure OpenAI)");
        context.Console.WriteLine("  aiservice.azureopenai.model        - Azure OpenAI API model (if using Azure OpenAI)");
        context.Console.WriteLine("  aiservice.azureopenai.key          - Azure OpenAI API key (if using Azure OpenAI)");
        context.Console.WriteLine("---");
        context.Console.WriteLine("  aiservice.foundry.endpoint         - Foundry API endpoint (if using Foundry)");
        context.Console.WriteLine("  aiservice.foundry.model            - Foundry API model (if using Foundry)");
        context.Console.WriteLine(ConfigCommandsResources.FoundryKeyDescription);
    }

    /// <summary>
    /// Prints the usage/help for the 'config view' command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Displays the command-line usage syntax, description, and available options
    /// for the 'config view' command in a formatted, colorized style.
    /// </para>    ///. </remarks>
    internal static void PrintViewUsage()
    {
        var optionsText = $"  {AnsiColors.OKGREEN}--config, -c <config>{AnsiColors.ENDC}    Path to the configuration file\n" +
                          $"  {AnsiColors.OKGREEN}--debug, -d{AnsiColors.ENDC}              Enable debug output";

        AnsiConsoleHelper.WriteUsage(
            "Usage: config view [options]",
            "Shows the current configuration settings.",
            optionsText);
    }

    /// <summary>
    /// Registers the 'config' command and its subcommands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to add subcommands to.</param>
    /// <param name="configOption">The global config file option.</param>
    /// <param name="debugOption">The global debug option.</param>
    public static void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption)
    {
        // Config command group (no special AI options)
        var configCommand = new Command("config", "Configuration management commands");

        // config list-keys
        var listKeysCommand = new Command("list-keys", "List all available configuration keys that can be updated");
        listKeysCommand.SetHandler(context =>
        {
            PrintAvailableConfigKeys(context);
        });
        configCommand.AddCommand(listKeysCommand);

        // config view
        var viewCommand = new Command("view", "Show the current configuration");
        viewCommand.SetHandler(context =>
        {
            // Check if any arguments were provided
            if (context.ParseResult.Tokens.Count == 0 && context.ParseResult.UnparsedTokens.Count == 0)
            {
                PrintViewUsage();
                return;
            }

            string? configPath = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            try
            {
                // Always load config directly from JSON for display
                var configFile = configPath ?? AppConfig.FindConfigFile();
                if (!string.IsNullOrEmpty(configFile))
                {
                    Console.WriteLine($"\n{AnsiColors.OKCYAN}Using configuration file: {AnsiColors.BOLD}{configFile}{AnsiColors.ENDC}\n");
                }

                var appConfig = AppConfig.LoadFromJsonFile(configFile);
                PrintConfigFormatted(appConfig);
            }
            catch (FileNotFoundException ex)
            {
                ExceptionHandler.HandleException(ex, "Loading configuration file");
            }
        });

        // config update <key> <value>
        var keyArg = new Argument<string>("key", "Configuration key to update (e.g. paths.resources_root)");
        var valueArg = new Argument<string>("value", "New value for the key");
        var updateCommand = new Command("update", "Update a configuration key")
        {
            keyArg,
            valueArg,
        };
        updateCommand.SetHandler(context =>
        {
            try
            {
                // Try to get values for both required arguments - this will throw if they're not provided
                string key = context.ParseResult.GetValueForArgument(keyArg);
                string value = context.ParseResult.GetValueForArgument(valueArg);
                string? configPath = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);

                // Always load config directly from JSON for update
                var configFile = configPath ?? AppConfig.FindConfigFile();
                var appConfig = AppConfig.LoadFromJsonFile(configFile);
                if (UpdateConfigKey(appConfig, key, value))
                {
                    appConfig.SaveToJsonFile(configFile);
                    Console.WriteLine($"Updated '{key}' to '{value}'.");
                }
                else
                {
                    Console.WriteLine($"Failed to update key '{key}'. Key not found or invalid.");                    // Print available keys if update failed
                    ConfigCommands.PrintAvailableConfigKeys(context);
                }
            }
            catch (ArgumentException)
            {
                // This exception is thrown when arguments are missing
                context.Console.WriteLine("Usage: config update <key> <value> [options]");
                context.Console.WriteLine(string.Empty);
                context.Console.WriteLine("Updates a configuration key with the specified value.");
                context.Console.WriteLine(string.Empty);
                context.Console.WriteLine("Arguments:");
                context.Console.WriteLine("  <key>    Configuration key to update (e.g. paths.resources_root)");
                context.Console.WriteLine("  <value>  New value for the key");
                context.Console.WriteLine(string.Empty);
                context.Console.WriteLine("Options:");
                context.Console.WriteLine("  --config, -c <config>    Path to the configuration file");
                context.Console.WriteLine("  --debug, -d              Enable debug output");
                context.Console.WriteLine(string.Empty);
                ConfigCommands.PrintAvailableConfigKeys(context);
            }
        });

        // Display user secrets status
        var displaySecretsCommand = new Command("display-secrets", "Display user secrets status (no values shown)");
        configCommand.AddCommand(displaySecretsCommand);
        displaySecretsCommand.SetHandler(() =>
        {
            try
            {
                var userSecrets = Program.ServiceProvider.GetRequiredService<UserSecretsHelper>();
                DisplayUserSecrets(userSecrets);
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "Displaying user secrets");
            }
        });

        // config secrets
        var secretsCommand = new Command("secrets", "Display status of user secrets");
        configCommand.AddCommand(secretsCommand);
        secretsCommand.SetHandler(context =>
        {
            try
            {
                if (Program.ServiceProvider.GetService(typeof(UserSecretsHelper)) is not UserSecretsHelper userSecretsHelper)
                {
                    AnsiConsoleHelper.WriteError("User secrets helper is not available.");
                    return;
                }

                // Check for common secrets (don't show values, just if they exist)
                bool hasOpenAIKey = userSecretsHelper.HasSecret("OpenAI:ApiKey");
                bool hasMicrosoftClientId = userSecretsHelper.HasSecret("Microsoft:ClientId");
                bool hasMicrosoftTenantId = userSecretsHelper.HasSecret("Microsoft:TenantId");

                // Display status
                Console.WriteLine("User Secrets Status:");
                Console.WriteLine();
                Console.WriteLine($"OpenAI API Key: {(hasOpenAIKey ? "[Set]" : "[Not Set]")}");
                Console.WriteLine($"Microsoft Graph Client ID: {(hasMicrosoftClientId ? "[Set]" : "[Not Set]")}");
                Console.WriteLine($"Microsoft Graph Tenant ID: {(hasMicrosoftTenantId ? "[Set]" : "[Not Set]")}");

                // Add information about managing secrets
                Console.WriteLine();
                AnsiConsoleHelper.WriteInfo("To manage user secrets, use the following commands:");
                Console.WriteLine("  dotnet user-secrets set \"UserSecrets:OpenAI:ApiKey\" \"your-api-key\" --project src/c-sharp/NotebookAutomation.Cli");
                Console.WriteLine("  dotnet user-secrets list --project src/c-sharp/NotebookAutomation.Cli"); Console.WriteLine();
                AnsiConsoleHelper.WriteInfo("For more information, see: src/c-sharp/docs/UserSecrets.md");
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, "Displaying user secrets information");
            }
        });

        configCommand.AddCommand(viewCommand);
        configCommand.AddCommand(updateCommand);

        // Show help if no subcommand is provided for the config command
        configCommand.SetHandler(context =>
        {
            if (context.ParseResult.Tokens.Count == 0 && context.ParseResult.UnparsedTokens.Count == 0)
            {
                var options = string.Join("\n", configCommand.Children.OfType<Command>().Select(cmd => $"  {cmd.Name}\t{cmd.Description}"));
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation.exe config [command] [options]",
                    configCommand.Description ?? "Available config commands:",
                    options);
            }
            else
            {
                var options = string.Join("\n", configCommand.Children.OfType<Command>().Select(cmd => $"  {cmd.Name,-15} {cmd.Description}"));
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation.exe config [command] [options]",
                    "Please provide a valid config subcommand. Available options:",
                    options);
            }

            return Task.CompletedTask;
        });

        rootCommand.AddCommand(configCommand);
    }

    /// <summary>
    /// Initializes the configuration system with the specified configuration file path and debug settings.
    /// </summary>
    /// <param name="configPath">The path to the configuration file to use. If null, the default configuration file will be located.</param>
    /// <param name="debug">Indicates whether debug mode should be enabled.</param>
    /// <remarks>
    /// <para>
    /// This method sets up dependency injection using the specified configuration file path.
    /// If the configuration file doesn't exist at the specified path, an error message is displayed.
    /// </para>
    /// </remarks>    /// <exception cref="FileNotFoundException">Thrown when the configuration file is not found at the specified path.</exception>
    public static void Initialize(string? configPath, bool debug)
    {
        // Initialize dependency injection if needed
        if (configPath != null)
        {
            if (!File.Exists(configPath))
            {
                AnsiConsoleHelper.WriteError($"Configuration file not found: {configPath}");
                return;
            }

            Program.SetupDependencyInjection(configPath, debug);
        }
    }

    /// <summary>
    /// Updates a configuration key in the AppConfig object.
    /// </summary>
    /// <param name="appConfig">The AppConfig instance to update.</param>
    /// <param name="key">The configuration key (e.g. 'paths.resourcesRoot').</param>
    /// <param name="value">The new value to set.</param>
    /// <returns>True if the key was updated, false if the key was not found or invalid.</returns>
    /// <remarks>
    /// <para>
    /// This method supports updating configuration values using dot notation for nested properties.
    /// Supported key patterns include:
    /// <list type="bullet">
    /// <item><description>paths.* - Path configuration settings</description></item>
    /// <item><description>microsoft_graph.* - Microsoft Graph API settings</description></item>
    /// <item><description>aiservice.provider - AI service provider selection</description></item>
    /// <item><description>aiservice.{provider}.* - Provider-specific settings</description></item>
    /// <item><description>video_extensions - Video file extensions list</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// bool success = UpdateConfigKey(config, "paths.onedrive_fullpath_root", "/path/to/onedrive");
    /// bool success2 = UpdateConfigKey(config, "aiservice.openai.model", "gpt-4");
    /// </code>
    /// </example>
    private static bool UpdateConfigKey(AppConfig appConfig, string key, string value)
    {
        var parts = key.Split('.');

        // Special case: top-level key for video_extensions
        if (key.Equals("video_extensions", StringComparison.OrdinalIgnoreCase))
        {
            appConfig.SetVideoExtensions([.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]);
            return true;
        }

        if (parts.Length == 2)
        {
            var section = parts[0].ToLowerInvariant();
            var prop = parts[1];
            switch (section)
            {
                case "paths":
                    var paths = appConfig.Paths; switch (prop)
                    {
                        case "onedrive_fullpath_root": paths.OnedriveFullpathRoot = value; return true;
                        case "notebook_vault_fullpath_root": paths.NotebookVaultFullpathRoot = value; return true;
                        case "metadata_file": paths.MetadataFile = value; return true;
                        case "onedrive_resources_basepath": paths.OnedriveResourcesBasepath = value; return true;
                        case "prompts_path": paths.PromptsPath = value; return true;
                        case "logging_dir": paths.LoggingDir = value; return true;
                    }

                    break;
                case "microsoft_graph":
                    var mg = appConfig.MicrosoftGraph;
                    switch (prop)
                    {
                        case "client_id": mg.ClientId = value; return true;
                        case "api_endpoint": mg.ApiEndpoint = value; return true;
                        case "authority": mg.Authority = value; return true;
                        case "scopes": mg.Scopes = [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]; return true;
                    }

                    break;
                case "aiservice":
                    var aiService = appConfig.AiService;
                    switch (prop)
                    {
                        case "provider": aiService.Provider = value; return true;
                    }

                    break;
                case "video_extensions":
                    appConfig.SetVideoExtensions([.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]);
                    return true;
            }
        }
        else if (parts.Length == 3 && parts[0].Equals("aiservice", StringComparison.InvariantCultureIgnoreCase))
        {
            var provider = parts[1].ToLowerInvariant();
            var prop = parts[2];
            var aiService = appConfig.AiService;
            switch (provider)
            {
                case "openai":
                    aiService.OpenAI ??= new OpenAiProviderConfig();
                    switch (prop)
                    {
                        case "model": aiService.OpenAI.Model = value; return true;
                        case "endpoint": aiService.OpenAI.Endpoint = value; return true;
                    }

                    break;
                case "azure":
                    aiService.Azure ??= new AzureProviderConfig();
                    switch (prop)
                    {
                        case "model": aiService.Azure.Model = value; return true;
                        case "endpoint": aiService.Azure.Endpoint = value; return true;
                        case "deployment": aiService.Azure.Deployment = value; return true;
                    }

                    break;
                case "foundry":
                    aiService.Foundry ??= new FoundryProviderConfig();
                    switch (prop)
                    {
                        case "model": aiService.Foundry.Model = value; return true;
                        case "endpoint": aiService.Foundry.Endpoint = value; return true;
                    }

                    break;
            }
        }

        return false;
    }

    /// <summary>
    /// Prints the current configuration in a formatted, colorized style for the CLI.
    /// </summary>
    /// <param name="appConfig">The AppConfig instance to display.</param>
    public static void PrintConfigFormatted(AppConfig appConfig)
    {
        // Helper for aligned output
        static void PrintAligned(string key, string value)
        {
            const int keyWidth = 32; // Adjusted for longer keys
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}{key,-keyWidth}{AnsiColors.ENDC}: {AnsiColors.OKGREEN}{value}{AnsiColors.ENDC}");
        }

        if (appConfig == null)
        {
            Console.WriteLine("[Config is null]");
            return;
        }

        // Yellow foreground on blue background, bold, spanning the CLI width
        int width;
        try
        {
            width = Console.WindowWidth;
        }
        catch
        {
            width = 80;
        }

        string headerText = "   Notebook Automation Configuration   ";
        int padLeft = (width - headerText.Length) / 2;
        if (padLeft < 0)
        {
            padLeft = 0;
        }

        string paddedHeader = headerText.PadLeft(headerText.Length + padLeft).PadRight(width);
        Console.WriteLine();
        Console.WriteLine($"{AnsiColors.BGBLUE}{new string(' ', width)}{AnsiColors.ENDC}");
        Console.WriteLine($"{AnsiColors.BGBLUE}{AnsiColors.WARNING}{AnsiColors.BOLD}{paddedHeader}{AnsiColors.ENDC}");
        Console.WriteLine($"{AnsiColors.BGBLUE}{new string(' ', width)}{AnsiColors.ENDC}");
        Console.WriteLine($"{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Paths =={AnsiColors.ENDC}");

        if (appConfig == null)
        {
            Console.WriteLine("[Config is null]");
            return;
        }

        var paths = appConfig?.Paths;
        if (appConfig == null || paths == null)
        {
            PrintAligned("onedrive_fullpath_root", "[not set]");
            PrintAligned("notebook_vault_fullpath_root", "[not set]");
            PrintAligned("metadata_file", "[not set]");
            PrintAligned("onedrive_resources_basepath", "[not set]");
            PrintAligned("logging_dir", "[not set]");
            PrintAligned("prompts_path", "[not set]");
        }
        else
        {
            PrintAligned("onedrive_fullpath_root", paths.OnedriveFullpathRoot ?? "[not set]");
            PrintAligned("notebook_vault_fullpath_root", paths.NotebookVaultFullpathRoot ?? "[not set]");
            PrintAligned("metadata_file", paths.MetadataFile ?? "[not set]");
            PrintAligned("onedrive_resources_basepath", paths.OnedriveResourcesBasepath ?? "[not set]");
            PrintAligned("logging_dir", paths.LoggingDir ?? "[not set]");
            PrintAligned("prompts_path", paths.PromptsPath ?? "[not set]");
        }

        Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Microsoft Graph API =={AnsiColors.ENDC}");
        var graph = appConfig?.MicrosoftGraph;
        if (appConfig == null || graph == null)
        {
            PrintAligned("client_id", "[not set]");
            PrintAligned("api_endpoint", "[not set]");
            PrintAligned("authority", "[not set]");
            PrintAligned("scopes", "[not set]");
        }
        else
        {
            PrintAligned("client_id", graph.ClientId ?? "[not set]");
            PrintAligned("api_endpoint", graph.ApiEndpoint ?? "[not set]");
            PrintAligned("authority", graph.Authority ?? "[not set]");
            if (graph.Scopes == null)
            {
                PrintAligned("scopes", "[not set]");
            }
            else
            {
                PrintAligned("scopes", string.Join(", ", graph.Scopes));
            }
        }

        Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== AI Service =={AnsiColors.ENDC}");

        var ai = appConfig?.AiService;
        if (appConfig == null || ai == null)
        {
            PrintAligned("provider", "[not set]");
            PrintAligned("selected_model", "[not set]");
            PrintAligned("selected_endpoint", "[not set]");
            PrintAligned("openai:model", "[not set]");
            PrintAligned("openai:endpoint", "[not set]");
            PrintAligned("azure:model", "[not set]");
            PrintAligned("azure:endpoint", "[not set]");
            PrintAligned("azure:deployment", "[not set]");
            PrintAligned("foundry:model", "[not set]");
            PrintAligned("foundry:endpoint", "[not set]");
            PrintAligned("api_key", "[not set - set via User Secrets or ENV]");
        }
        else
        {
            var provider = ai.Provider ?? "[not set]";
            PrintAligned("provider", provider);

            // Show selected model and endpoint for the provider
            string selectedModel = "[not set]";
            string selectedEndpoint = "[not set]";
            switch (provider.ToLowerInvariant())
            {
                case "openai":
                    selectedModel = ai.OpenAI != null ? ai.OpenAI.Model ?? "[not set]" : "[not set]";
                    selectedEndpoint = ai.OpenAI != null ? ai.OpenAI.Endpoint ?? "[not set]" : "[not set]";
                    break;
                case "azure":
                    selectedModel = ai.Azure != null ? ai.Azure.Model ?? "[not set]" : "[not set]";
                    selectedEndpoint = ai.Azure != null ? ai.Azure.Endpoint ?? "[not set]" : "[not set]";
                    break;
                case "foundry":
                    selectedModel = ai.Foundry != null ? ai.Foundry.Model ?? "[not set]" : "[not set]";
                    selectedEndpoint = ai.Foundry != null ? ai.Foundry.Endpoint ?? "[not set]" : "[not set]";
                    break;
            }

            PrintAligned("selected_model", selectedModel);
            PrintAligned("selected_endpoint", selectedEndpoint);

            // Show all provider configs
            if (ai.OpenAI != null)
            {
                PrintAligned("openai:model", ai.OpenAI.Model ?? "[not set]");
                PrintAligned("openai:endpoint", ai.OpenAI.Endpoint ?? "[not set]");
            }
            else
            {
                PrintAligned("openai:model", "[not set]");
                PrintAligned("openai:endpoint", "[not set]");
            }

            if (ai.Azure != null)
            {
                PrintAligned("azure:model", ai.Azure.Model ?? "[not set]");
                PrintAligned("azure:endpoint", ai.Azure.Endpoint ?? "[not set]");
                PrintAligned("azure:deployment", ai.Azure.Deployment ?? "[not set]");
            }
            else
            {
                PrintAligned("azure:model", "[not set]");
                PrintAligned("azure:endpoint", "[not set]");
                PrintAligned("azure:deployment", "[not set]");
            }

            if (ai.Foundry != null)
            {
                PrintAligned("foundry:model", ai.Foundry.Model ?? "[not set]");
                PrintAligned("foundry:endpoint", ai.Foundry.Endpoint ?? "[not set]");
            }
            else
            {
                PrintAligned("foundry:model", "[not set]");
                PrintAligned("foundry:endpoint", "[not set]");
            }

            // Always show where the API key is sourced from
            string? apiKey;
            try
            {
                apiKey = ai.GetApiKey();
            }
            catch
            {
                apiKey = null;
            }

            string apiKeyStatus;
            if (!string.IsNullOrEmpty(apiKey))
            {
                apiKeyStatus = "[API key available] [via ENV or User Secrets]";
            }
            else
            {
                apiKeyStatus = "[not set - set via User Secrets or ENV]";
            }

            PrintAligned("api_key", apiKeyStatus);
        }

        Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Video Extensions =={AnsiColors.ENDC}");
        if (appConfig != null && appConfig.VideoExtensions != null)
        {
            PrintAligned("video_extensions", string.Join(", ", appConfig.VideoExtensions));
        }
        else
        {
            PrintAligned("video_extensions", "[not set]");
        }

        Console.WriteLine($"\n{AnsiColors.GREY}Tip: Use '{AnsiColors.BOLD}config update <key> <value>{AnsiColors.ENDC}{AnsiColors.GREY}' to change a setting.{AnsiColors.ENDC}\n");
    }

    /// <summary>
    /// Displays the status of user secrets in the configuration.
    /// </summary>
    /// <param name="userSecrets">The user secrets helper.</param>
    private static void DisplayUserSecrets(UserSecretsHelper userSecrets)
    {
        AnsiConsoleHelper.WriteHeading("User Secrets Status");

        // Check for common secrets (don't show values, just if they exist)
        bool hasOpenAIKey = userSecrets.HasSecret("OpenAI:ApiKey");
        bool hasMicrosoftClientId = userSecrets.HasSecret("Microsoft:ClientId");
        bool hasMicrosoftTenantId = userSecrets.HasSecret("Microsoft:TenantId");

        // Display status
        AnsiConsoleHelper.WriteKeyValue("OpenAI API Key", hasOpenAIKey ? "[Set]" : "[Not Set]");
        AnsiConsoleHelper.WriteKeyValue("Microsoft Graph Client ID", hasMicrosoftClientId ? "[Set]" : "[Not Set]");
        AnsiConsoleHelper.WriteKeyValue("Microsoft Graph Tenant ID", hasMicrosoftTenantId ? "[Set]" : "[Not Set]");

        // Add information about managing secrets
        Console.WriteLine();
        AnsiConsoleHelper.WriteInfo("To manage user secrets, use the following commands:");
        Console.WriteLine("  dotnet user-secrets set \"UserSecrets:OpenAI:ApiKey\" \"your-api-key\" --project src/c-sharp/NotebookAutomation.Cli");
        Console.WriteLine("  dotnet user-secrets list --project src/c-sharp/NotebookAutomation.Cli");
        Console.WriteLine();
        AnsiConsoleHelper.WriteInfo("For more information, see: src/c-sharp/docs/UserSecrets.md");
    }

    /// <summary>
    /// Masks a secret value for display.
    /// </summary>
    /// <param name="secret">The secret to mask.</param>
    /// <returns>A masked version of the secret, or "[Not Set]" if it's null or empty.</returns>
    private static string MaskSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return "[Not Set]";
        }

        // Show first 3 and last 3 characters if long enough
        if (secret.Length > 8)
        {
            return $"{secret[..3]}...{secret[^3..]}";
        }

        // Otherwise just indicate it's set
        return "[Set]";
    }
}