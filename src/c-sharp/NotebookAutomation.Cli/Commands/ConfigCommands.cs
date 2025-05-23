using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Cli.Utilities;
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

                // Initialize dependency injection
                Initialize(configPath, debug);

                // Get AppConfig from DI container
                var appConfig = Program.ServiceProvider.GetRequiredService<AppConfig>();
                PrintConfigFormatted(appConfig);
            });

            // config update-key <key> <value>
            var keyArg = new Argument<string>("key", "Configuration key to update (e.g. paths.resourcesRoot)");
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
                    string value = context.ParseResult.GetValueForArgument(valueArg);                    string? configPath = context.ParseResult.GetValueForOption(configOption);
                    bool debug = context.ParseResult.GetValueForOption(debugOption);
                    
                    // Initialize dependency injection
                    Initialize(configPath, debug);
                    
                    // Get AppConfig from DI container
                    var appConfig = Program.ServiceProvider.GetRequiredService<AppConfig>();

                    if (UpdateConfigKey(appConfig, key, value))
                    {
                        appConfig.SaveToJsonFile(configPath ?? AppConfig.FindConfigFile());
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
                    context.Console.WriteLine("  <key>    Configuration key to update (e.g. paths.resourcesRoot)");
                    context.Console.WriteLine("  <value>  New value for the key");
                    context.Console.WriteLine("");
                    context.Console.WriteLine("Options:");
                    context.Console.WriteLine("  --config, -c <config>    Path to the configuration file");
                    context.Console.WriteLine("  --debug, -d              Enable debug output");
                    context.Console.WriteLine("");
                    context.Console.WriteLine("Available configuration keys:");
                    context.Console.WriteLine("  paths.resourcesRoot            - Root directory for resources");
                    context.Console.WriteLine("  paths.notebookVaultRoot        - Root directory for the notebook vault");
                    context.Console.WriteLine("  paths.metadataFile             - Path to the metadata file");
                    context.Console.WriteLine("  paths.obsidianVaultRoot        - Root directory of the Obsidian vault");
                    context.Console.WriteLine("  paths.onedriveResourcesBasepath - Base path for OneDrive resources");
                    context.Console.WriteLine("  paths.loggingDir               - Directory for log files");
                    context.Console.WriteLine("  microsoftgraph.clientId        - Microsoft Graph API client ID");
                    context.Console.WriteLine("  microsoftgraph.apiEndpoint     - Microsoft Graph API endpoint");
                    context.Console.WriteLine("  microsoftgraph.authority       - Microsoft Graph API authority");
                    context.Console.WriteLine("  microsoftgraph.scopes          - Microsoft Graph API scopes (comma-separated)");
                    context.Console.WriteLine("  openai.apiKey                  - OpenAI API key");
                    context.Console.WriteLine("  videoextensions                - Video file extensions (comma-separated)");
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
                            case "resourcesRoot": paths.ResourcesRoot = value; return true;
                            case "notebookVaultRoot": paths.NotebookVaultRoot = value; return true;
                            case "metadataFile": paths.MetadataFile = value; return true;
                            case "obsidianVaultRoot": paths.ObsidianVaultRoot = value; return true;
                            case "onedriveResourcesBasepath": paths.OnedriveResourcesBasepath = value; return true;
                            case "loggingDir": paths.LoggingDir = value; return true;
                        }
                        break;
                    case "microsoftgraph":
                        var mg = appConfig.MicrosoftGraph;
                        switch (prop)
                        {
                            case "clientId": mg.ClientId = value; return true;
                            case "apiEndpoint": mg.ApiEndpoint = value; return true;
                            case "authority": mg.Authority = value; return true;
                            case "scopes": mg.Scopes = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(); return true;
                        }
                        break;
                    case "openai":
                        if (prop == "apiKey") { appConfig.OpenAi.ApiKey = value; return true; }
                        break;
                    case "videoextensions":
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
        private static void PrintConfigFormatted(AppConfig appConfig)
        {
            Console.WriteLine($"\n{AnsiColors.BG_BLUE}{AnsiColors.BOLD}{AnsiColors.HEADER}   Notebook Automation Configuration   {AnsiColors.ENDC}\n");
            Console.WriteLine($"{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Paths =={AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}resourcesRoot         {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.Paths.ResourcesRoot}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}notebookVaultRoot     {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.Paths.NotebookVaultRoot}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}metadataFile          {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.Paths.MetadataFile}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}obsidianVaultRoot     {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.Paths.ObsidianVaultRoot}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}onedriveResourcesBasepath {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.Paths.OnedriveResourcesBasepath}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}loggingDir            {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.Paths.LoggingDir}{AnsiColors.ENDC}");

            Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Microsoft Graph API =={AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}clientId      {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.MicrosoftGraph.ClientId}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}apiEndpoint   {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.MicrosoftGraph.ApiEndpoint}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}authority     {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.MicrosoftGraph.Authority}{AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}scopes        {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{string.Join(", ", appConfig.MicrosoftGraph.Scopes)}{AnsiColors.ENDC}");

            Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== OpenAI =={AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}apiKey        {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{appConfig.OpenAi.ApiKey}{AnsiColors.ENDC}");

            Console.WriteLine($"\n{AnsiColors.OKBLUE}{AnsiColors.BOLD}== Video Extensions =={AnsiColors.ENDC}");
            Console.WriteLine($"  {AnsiColors.OKCYAN}{AnsiColors.BOLD}Extensions    {AnsiColors.ENDC}: {AnsiColors.OKGREEN}{string.Join(", ", appConfig.VideoExtensions)}{AnsiColors.ENDC}");
            Console.WriteLine($"\n{AnsiColors.GREY}Tip: Use '{AnsiColors.BOLD}config update-key <key> <value>{AnsiColors.ENDC}{AnsiColors.GREY}' to change a setting.{AnsiColors.ENDC}\n");
        }
    }
}
