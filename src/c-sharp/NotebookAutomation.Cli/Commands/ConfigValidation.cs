using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Cli.Utilities;

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides static helpers for config validation for feature-specific requirements.
    /// </summary>
    public static class ConfigValidation
    {
        private static readonly string[] collection = [
                    "paths.onedrive_fullpath_root",
                    "paths.notebook_vault_fullpath_root",
                    "paths.metadata_file",
                    "paths.onedrive_resources_basepath",
                    "paths.logging_dir"
                ];

        /// <summary>
        /// Validates that all required path values are present in the configuration.
        /// </summary>
        /// <param name="config">The AppConfig instance to validate.</param>
        /// <param name="missingKeys">A list of missing required path keys, if any.</param>
        /// <returns>True if all required paths are present; otherwise, false.</returns>
        /// <summary>
        /// Validates that all required path values are present in the configuration.
        /// </summary>
        /// <param name="config">The AppConfig instance to validate.</param>
        /// <param name="missingKeys">A list of missing required path keys, if any.</param>
        /// <returns>True if all required paths are present; otherwise, false.</returns>
        public static bool RequireAllPaths(AppConfig config, out List<string> missingKeys)
        {
            missingKeys = [];
            if (config.Paths == null)
            {
                missingKeys.AddRange(collection);
                return false;
            }
            if (string.IsNullOrWhiteSpace(config.Paths.OnedriveFullpathRoot))
                missingKeys.Add("paths.onedrive_fullpath_root");
            if (string.IsNullOrWhiteSpace(config.Paths.NotebookVaultFullpathRoot))
                missingKeys.Add("paths.notebook_vault_fullpath_root");
            if (string.IsNullOrWhiteSpace(config.Paths.MetadataFile))
                missingKeys.Add("paths.metadata_file");
            if (string.IsNullOrWhiteSpace(config.Paths.OnedriveResourcesBasepath))
                missingKeys.Add("paths.onedrive_resources_basepath");
            if (string.IsNullOrWhiteSpace(config.Paths.LoggingDir))
                missingKeys.Add("paths.logging_dir");
            return missingKeys.Count == 0;
        }
        /// <summary>
        /// Validates that Microsoft Graph config values are present. Returns true if valid, else prints error and config.
        /// </summary>
        public static bool RequireMicrosoftGraph(AppConfig config)
        {
            if (config.MicrosoftGraph == null ||
                string.IsNullOrWhiteSpace(config.MicrosoftGraph.ClientId) ||
                string.IsNullOrWhiteSpace(config.MicrosoftGraph.ApiEndpoint) ||
                string.IsNullOrWhiteSpace(config.MicrosoftGraph.Authority) ||
                config.MicrosoftGraph.Scopes == null || config.MicrosoftGraph.Scopes.Count == 0)
            {
                AnsiConsoleHelper.WriteError("Microsoft Graph configuration is required for this feature but is missing or incomplete.");
                ConfigCommands.PrintConfigFormatted(config);
                return false;
            }
            return true;
        }        /// <summary>
                 /// Validates that AI service config values are present. Returns true if valid, else prints error and config.
                 /// </summary>
        public static bool RequireOpenAi(AppConfig config)
        {
            if (config.AiService == null)
            {
                AnsiConsoleHelper.WriteError("OpenAI configuration is required for this feature but is missing or incomplete.");
                ConfigCommands.PrintConfigFormatted(config);
                return false;
            }
            var apiKey = config.AiService.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                AnsiConsoleHelper.WriteError("OpenAI configuration is required for this feature but is missing or incomplete.");
                ConfigCommands.PrintConfigFormatted(config);
                return false;
            }
            return true;
        }
    }
}
