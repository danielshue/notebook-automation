using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tools.VideoProcessing
{
    /// <summary>
    /// Static entry point for video processing operations, providing a unified interface for CLI tools.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The VideoNoteProcessingEntrypoint class serves as the primary integration point between
    /// command-line interfaces and the core video processing functionality. It provides a simplified
    /// API for processing video files with configuration loading and parameter normalization.
    /// </para>
    /// <para>
    /// This class is designed to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Load and initialize configuration from files or environment</description></item>
    /// <item><description>Normalize and validate input parameters</description></item>
    /// <item><description>Set up logging based on debug/verbose flags</description></item>
    /// <item><description>Delegate actual processing to the <see cref="VideoNoteBatchProcessor"/></description></item>
    /// <item><description>Provide a clean return value with processed/failed counts</description></item>
    /// </list>
    /// <para>
    /// The entrypoint follows a common pattern used throughout the NotebookAutomation toolkit,
    /// where static entry points provide simple interfaces for CLI tools while delegating
    /// the actual work to instance-based service classes.
    /// </para>
    /// </remarks>
    public static class VideoNoteProcessingEntrypoint
    {
        /// <summary>
        /// Processes video files to markdown notes using configuration and CLI-style arguments.
        /// </summary>
        /// <param name="input">Input file or directory path containing video files to process.</param>
        /// <param name="output">Output directory where markdown notes will be saved.</param>
        /// <param name="configPath">Optional path to a configuration file.</param>
        /// <param name="debug">Whether to enable debug-level logging.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">If true, simulates processing without writing any output files.</param>
        /// <returns>
        /// A tuple containing the count of successfully processed files and the count of failures.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method serves as the main entry point for video processing operations and is designed
        /// to be called directly from command-line interfaces. It handles:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Loading configuration from the specified path or default locations</description></item>
        /// <item><description>Setting up logging based on the debug/verbose flags</description></item>
        /// <item><description>Obtaining necessary API keys from configuration or environment</description></item>
        /// <item><description>Validating input parameters</description></item>
        /// <item><description>Delegating the actual processing to a <see cref="VideoNoteBatchProcessor"/></description></item>
        /// </list>
        /// <para>
        /// The method uses the following configuration hierarchy:
        /// </para>
        /// <list type="number">
        /// <item><description>Explicitly provided parameters</description></item>
        /// <item><description>Configuration file settings</description></item>
        /// <item><description>Environment variables</description></item>
        /// <item><description>Default values</description></item>
        /// </list>
        /// <para>
        /// For video file detection, it uses the configured video extensions or falls back to
        /// a default list (.mp4, .mov, .avi, .mkv, .webm).
        /// </para>
        /// <para>
        /// For OpenAI integration, it first checks for the OPENAI_API_KEY environment variable,
        /// then falls back to the configured API key if available.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // From a CLI command handler:
        /// var result = await VideoNoteProcessingEntrypoint.ProcessVideoAsync(
        ///     args.InputPath, 
        ///     args.OutputPath,
        ///     args.ConfigFile, 
        ///     args.Debug, 
        ///     args.Verbose,
        ///     args.DryRun);
        /// 
        /// Console.WriteLine($"Successfully processed {result.processed} video files.");
        /// </code>
        /// </example>
        public static async Task<(int processed, int failed)> ProcessVideoAsync(
            string? input,
            string? output,
            string? configPath,
            bool debug,
            bool verbose,
            bool dryRun)
        {
            var configProvider = ConfigProvider.Create(configPath, debug);
            var logger = configProvider.Logger;
            if (string.IsNullOrEmpty(input))
            {
                logger.LogError("Input path is required");
                return (0, 1);
            }
            var videoExtensions = configProvider.AppConfig?.VideoExtensions ?? new List<string> { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
            string? openAiApiKey = Environment.GetEnvironmentVariable(Configuration.OpenAiConfig.OpenAiApiKeyEnvVar);
            if (string.IsNullOrWhiteSpace(openAiApiKey) && configProvider.AppConfig?.OpenAi != null)
            {
                openAiApiKey = configProvider.AppConfig.OpenAi.ApiKey;
            }
            var batchProcessor = new VideoNoteBatchProcessor(logger);
            var (processed, failed) = await batchProcessor.ProcessVideosAsync(
                input,
                output ?? (configProvider.AppConfig?.Paths?.NotebookVaultRoot ?? "Generated"),
                videoExtensions,
                openAiApiKey,
                dryRun);
            logger.LogInformation("Video processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            return (processed, failed);
        }
    }
}
