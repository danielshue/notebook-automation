using System.CommandLine;
using System.CommandLine.Invocation;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Cli.Utilities;

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for managing application configuration.
    /// 
    /// This class registers the 'config' command group, including subcommands for
    /// displaying and updating configuration values in the Notebook Automation CLI.
    /// </summary>
    internal class ConfigCommands
    {
        /// <summary>
        /// Prints the usage/help for the 'config show' command.
        /// </summary>
        internal void PrintShowUsage()
        {
            AnsiConsoleHelper.WriteUsage(
                "Usage: config show [options]",
                "Shows the current configuration settings.",
                $"  {AnsiColors.OKGREEN}--config, -c <config>{AnsiColors.ENDC}    Path to the configuration file\n" +
                $"  {AnsiColors.OKGREEN}--debug, -d{AnsiColors.ENDC}              Enable debug output"
            );
        }
        /// <summary>
        /// Registers the 'config' command and its subcommands with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add subcommands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption)
        {
            var configCommand = new Command("config", "Configuration management commands");

            // config show
            var showCommand = new Command("show", "Show the current configuration");
            showCommand.SetHandler((InvocationContext context) =>
            {
                // Check if any arguments were provided
                if (context.ParseResult.Tokens.Count == 0 && context.ParseResult.UnparsedTokens.Count == 0)
                {
                    PrintShowUsage();
                    return;
                }
                string? configPath = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);

                try
                {
                    // Always load config directly from JSON for display
                    var configFile = configPath ?? AppConfig.FindConfigFile();
                    var appConfig = AppConfig.LoadFromJsonFile(configFile);
                    PrintConfigFormatted(appConfig);
                }
                catch (System.IO.FileNotFoundException ex)
                {
                    AnsiConsoleHelper.WriteError($"Configuration file not found: {ex.FileName ?? ex.Message}");
                }
            });

            // config update-key <key> <value>
            var keyArg = new Argument<string>("key", "Configuration key to update (e.g. paths.resources_root)");
            var valueArg = new Argument<string>("value", "New value for the key");
            var updateKeyCommand = new Command("update-key", "Update a configuration key")
            {
                keyArg,
                valueArg
            };
            updateKeyCommand.SetHandler((InvocationContext context) =>
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
                        Console.WriteLine($"Failed to update key '{key}'. Key not found or invalid.");
                    }
                }
                catch (ArgumentException)
                {
                    // This exception is thrown when arguments are missing
                    context.Console.WriteLine("Usage: config update-key <key> <value> [options]");
                    context.Console.WriteLine("");
                    context.Console.WriteLine("Updates a configuration key with the specified value.");
                    context.Console.WriteLine("");
                    context.Console.WriteLine("Arguments:");
                    context.Console.WriteLine("  <key>    Configuration key to update (e.g. paths.resources_root)");
                    context.Console.WriteLine("  <value>  New value for the key");
                    context.Console.WriteLine("");
                    context.Console.WriteLine("Options:");
                    context.Console.WriteLine("  --config, -c <config>    Path to the configuration file");
                    context.Console.WriteLine("  --debug, -d              Enable debug output");
                    context.Console.WriteLine("");
                    context.Console.WriteLine("Available configuration keys:");
                    context.Console.WriteLine("  paths.resources_root            - Root directory for resources");
                    context.Console.WriteLine("  paths.notebook_vault_root        - Root directory for the notebook vault");
                    context.Console.WriteLine("  paths.metadata_file             - Path to the metadata file");
                    context.Console.WriteLine("  paths.obsidian_vault_root        - Root directory of the Obsidian vault");
                    context.Console.WriteLine("  paths.onedrive_resources_basepath - Base path for OneDrive resources");
                    context.Console.WriteLine("  paths.logging_dir               - Directory for log files");
                    context.Console.WriteLine("  microsoft_graph.client_id        - Microsoft Graph API client ID");
                    context.Console.WriteLine("  microsoft_graph.api_endpoint     - Microsoft Graph API endpoint");
                    context.Console.WriteLine("  microsoft_graph.authority       - Microsoft Graph API authority");
                    context.Console.WriteLine("  microsoft_graph.scopes          - Microsoft Graph API scopes (comma-separated)");
                    context.Console.WriteLine("  openai.api_key                  - OpenAI API key");
                    context.Console.WriteLine("  video_extensions                - Video file extensions (comma-separated)");
                }
            });

            configCommand.AddCommand(showCommand);
            configCommand.AddCommand(updateKeyCommand);

            // Show help if no subcommand is provided for the config command
            configCommand.SetHandler((InvocationContext context) =>
            {
                if (context.ParseResult.Tokens.Count == 0 && context.ParseResult.UnparsedTokens.Count == 0)
                {
                    var options = string.Join("\n", configCommand.Children.OfType<Command>().Select(cmd => $"  {cmd.Name}\t{cmd.Description}"));
                    AnsiConsoleHelper.WriteUsage(
                        "Usage: notebookautomation config [command] [options]",
                        configCommand.Description ?? "Available config commands:",
                        options
                    );
                }
                else
                {
                    var options = string.Join("\n", configCommand.Children.OfType<Command>().Select(cmd => $"  {cmd.Name,-15} {cmd.Description}"));
                    AnsiConsoleHelper.WriteUsage(
                        "Usage: notebookautomation config [command] [options]",
                        "Please provide a valid config subcommand. Available options:",
                        options
                    );
                }
                return Task.CompletedTask;
            });

            rootCommand.AddCommand(configCommand);
        }        public static void Initialize(string? configPath, bool debug)
        {
            // Initialize dependency injection if needed
            if (configPath != null)
            {
                if (!System.IO.File.Exists(configPath))
                {
                    AnsiConsoleHelper.WriteError($"Configuration file not found: {configPath}");
                    return;
                }
                Program.SetupDependencyInjection(configPath, debug);
            }
        }


        /// Updates a configuration key in the AppConfig object.
        /// </summary>
        /// <param name="appConfig">The AppConfig instance to update.</param>
        /// <param name="key">The configuration key (e.g. 'paths.resourcesRoot').</param>
        /// <param name="value">The new value to set.</param>
        /// <returns>True if the key was updated, false if the key was not found or invalid.</returns>
        private static bool UpdateConfigKey(AppConfig appConfig, string key, string value)
        {
            var parts = key.Split('.');
            if (parts.Length == 2)
            {
                var section = parts[0].ToLowerInvariant();
                var prop = parts[1];
                switch (section)
                {
                    case "paths":
                        var paths = appConfig.Paths;
                        switch (prop)
                        {
                            case "resources_root": paths.ResourcesRoot = value; return true;
                            case "notebook_vault_root": paths.NotebookVaultRoot = value; return true;
                            case "metadata_file": paths.MetadataFile = value; return true;
                            case "obsidian_vault_root": paths.ObsidianVaultRoot = value; return true;
                            case "onedrive_resources_basepath": paths.OnedriveResourcesBasepath = value; return true;
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
                            case "scopes": mg.Scopes = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(); return true;
                        }
                        break;
                    case "openai":
                        if (prop == "api_key") {
                            // Do not allow updating/storing the OpenAI API key in config for security
                            AnsiConsoleHelper.WriteWarning("OpenAI API key must be set via the OPENAI_API_KEY environment variable. It will not be stored in the config file.");
                            return false;
                        }
                        break;
                    case "video_extensions":
                        appConfig.SetVideoExtensions(value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
                        return true;
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
            void PrintAligned(string key, string value)
            {
                const int keyWidth = 32; // Adjusted for longer keys
                Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}{key,-keyWidth}{AnsiColors.ENDC}: {AnsiColors.OKGREEN}{value}{AnsiColors.ENDC}");
            }

            // Yellow foreground on blue background, bold, spanning the CLI width
            int width = 0;
            try { width = Console.WindowWidth; } catch { width = 80; }
            string headerText = "   Notebook Automation Configuration   ";
            int padLeft = (width - headerText.Length) / 2;
            if (padLeft < 0) padLeft = 0;
            string paddedHeader = headerText.PadLeft(headerText.Length + padLeft).PadRight(width);
            Console.WriteLine();
            Console.WriteLine($"{AnsiColors.BG_BLUE}{new string(' ', width)}{AnsiColors.ENDC}");
            Console.WriteLine($"{AnsiColors.BG_BLUE}{AnsiColors.WARNING}{AnsiColors.BOLD}{paddedHeader}{AnsiColors.ENDC}");
            Console.WriteLine($"{AnsiColors.BG_BLUE}{new string(' ', width)}{AnsiColors.ENDC}");
            Console.WriteLine($"{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Paths =={AnsiColors.ENDC}");
            PrintAligned("resources_root", appConfig.Paths.ResourcesRoot);
            PrintAligned("notebook_vault_root", appConfig.Paths.NotebookVaultRoot);
            PrintAligned("metadata_file", appConfig.Paths.MetadataFile);
            PrintAligned("obsidian_vault_root", appConfig.Paths.ObsidianVaultRoot);
            PrintAligned("onedrive_resources_basepath", appConfig.Paths.OnedriveResourcesBasepath);
            PrintAligned("logging_dir", appConfig.Paths.LoggingDir);

            Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Microsoft Graph API =={AnsiColors.ENDC}");
            PrintAligned("client_id", appConfig.MicrosoftGraph.ClientId);
            PrintAligned("api_endpoint", appConfig.MicrosoftGraph.ApiEndpoint);
            PrintAligned("authority", appConfig.MicrosoftGraph.Authority);
            PrintAligned("scopes", string.Join(", ", appConfig.MicrosoftGraph.Scopes));

            Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== OpenAI =={AnsiColors.ENDC}");
            // Always show where the OpenAI API key is sourced from
            string openAiKey = Environment.GetEnvironmentVariable(OpenAiConfig.OpenAiApiKeyEnvVar) != null
                ? $"[set via ENV:{OpenAiConfig.OpenAiApiKeyEnvVar}]"
                : $"[not set - must set ENV:{OpenAiConfig.OpenAiApiKeyEnvVar}]";
            PrintAligned("api_key", openAiKey);
            PrintAligned("model", appConfig.OpenAi.Model ?? "");

            Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Video Extensions =={AnsiColors.ENDC}");
            PrintAligned("video_extensions", string.Join(", ", appConfig.VideoExtensions));
            Console.WriteLine($"\n{AnsiColors.GREY}Tip: Use '{AnsiColors.BOLD}config update-key <key> <value>{AnsiColors.ENDC}{AnsiColors.GREY}' to change a setting.{AnsiColors.ENDC}\n");
        }
    }
}
