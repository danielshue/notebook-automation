// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides static helpers for validating configuration values for feature-specific requirements.
/// </summary>
/// <remarks>
/// <para>
/// This class includes methods for validating required paths in the application configuration.
/// It ensures that all necessary configuration keys are present and logs missing keys.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var config = new AppConfig();
/// var isValid = ConfigValidation.RequireAllPaths(config, out var missingKeys);
/// if (!isValid)
/// {
///     Console.WriteLine("Missing keys:");
///     foreach (var key in missingKeys)
///     {
///         Console.WriteLine(key);
///     }
/// }
/// </code>
/// </example>
internal static class ConfigValidation
{
    private static readonly string[] Collection = [
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
    /// <remarks>
    /// <para>
    /// This method checks the configuration for required path keys, such as:
    /// <list type="bullet">
    /// <item><description>paths.onedrive_fullpath_root</description></item>
    /// <item><description>paths.notebook_vault_fullpath_root</description></item>
    /// <item><description>paths.metadata_file</description></item>
    /// <item><description>paths.onedrive_resources_basepath</description></item>
    /// <item><description>paths.logging_dir</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new AppConfig();
    /// var isValid = ConfigValidation.RequireAllPaths(config, out var missingKeys);
    /// </code>
    /// </example>
    public static bool RequireAllPaths(AppConfig config, out List<string> missingKeys)
    {
        missingKeys = [];
        if (config.Paths == null)
        {
            missingKeys.AddRange(Collection);
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Paths.OnedriveFullpathRoot))
        {
            missingKeys.Add("paths.onedrive_fullpath_root");
        }

        if (string.IsNullOrWhiteSpace(config.Paths.NotebookVaultFullpathRoot))
        {
            missingKeys.Add("paths.notebook_vault_fullpath_root");
        }

        if (string.IsNullOrWhiteSpace(config.Paths.MetadataFile))
        {
            missingKeys.Add("paths.metadata_file");
        }

        if (string.IsNullOrWhiteSpace(config.Paths.OnedriveResourcesBasepath))
        {
            missingKeys.Add("paths.onedrive_resources_basepath");
        }

        if (string.IsNullOrWhiteSpace(config.Paths.LoggingDir))
        {
            missingKeys.Add("paths.logging_dir");
        }

        return missingKeys.Count == 0;
    }

    /// <summary>
    /// Validates that Microsoft Graph config values are present. Returns true if valid, else prints error and config.
    /// </summary>
    /// <returns></returns>
    public static async Task<bool> RequireMicrosoftGraph(AppConfig config)
    {
        if (config.MicrosoftGraph == null ||
            string.IsNullOrWhiteSpace(config.MicrosoftGraph.ClientId) ||
            string.IsNullOrWhiteSpace(config.MicrosoftGraph.ApiEndpoint) ||
            string.IsNullOrWhiteSpace(config.MicrosoftGraph.Authority) ||
            config.MicrosoftGraph.Scopes == null || config.MicrosoftGraph.Scopes.Count == 0)
        {
            AnsiConsoleHelper.WriteError("Microsoft Graph configuration is required for this feature but is missing or incomplete.");
            await ConfigCommands.PrintConfigFormatted(config);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that AI service config values are present. Returns true if valid, else prints error and config.
    /// </summary>
    /// <returns></returns>
    public static async Task<bool> RequireOpenAi(AppConfig config)
    {
        if (config.AiService == null)
        {
            AnsiConsoleHelper.WriteError("OpenAI configuration is required for this feature but is missing or incomplete.");
            await ConfigCommands.PrintConfigFormatted(config);
            return false;
        }

        var apiKey = config.AiService.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            AnsiConsoleHelper.WriteError("OpenAI configuration is required for this feature but is missing or incomplete.");
            await ConfigCommands.PrintConfigFormatted(config);
            return false;
        }

        return true;
    }
}
