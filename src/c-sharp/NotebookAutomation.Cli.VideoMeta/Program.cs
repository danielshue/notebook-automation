using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.VideoProcessing;

namespace NotebookAutomation.Cli.VideoMeta
{
    /// <summary>
    /// Entry point for the Video Metadata CLI tool.
    /// </summary>
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("VideoMeta - Tools for creating notes from video files");

            var inputOption = new Option<string>(
                aliases: new[] { "--input", "-i" },
                description: "Path to the input video file or directory");
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Path to the output markdown file or directory");
            var configOption = new Option<string>(
                aliases: new[] { "--config", "-c" },
                description: "Path to the configuration file");
            var debugOption = new Option<bool>(
                aliases: new[] { "--debug", "-d" },
                description: "Enable debug output");
            var verboseOption = new Option<bool>(
                aliases: new[] { "--verbose", "-v" },
                description: "Enable verbose output");
            var dryRunOption = new Option<bool>(
                aliases: new[] { "--dry-run" },
                description: "Simulate actions without making changes");

            rootCommand.AddOption(inputOption);
            rootCommand.AddOption(outputOption);
            rootCommand.AddGlobalOption(configOption);
            rootCommand.AddGlobalOption(debugOption);
            rootCommand.AddGlobalOption(verboseOption);
            rootCommand.AddGlobalOption(dryRunOption);

            rootCommand.SetHandler(async (input, output, config, debug, verbose, dryRun) =>
            {
                await ProcessVideoAsync(input, output, config, debug, verbose, dryRun);
            }, inputOption, outputOption, configOption, debugOption, verboseOption, dryRunOption);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task ProcessVideoAsync(
            string? input,
            string? output,
            string? configPath,
            bool debug,
            bool verbose,
            bool dryRun)
        {
            try
            {
                var configProvider = ConfigProvider.Create(configPath, debug);
                var logger = configProvider.Logger;
                if (string.IsNullOrEmpty(input))
                {
                    logger.LogError("Input path is required");
                    return;
                }
                var videoProcessor = new VideoNoteProcessor(logger);
                var processed = 0;
                var failed = 0;
                var videoFiles = new System.Collections.Generic.List<string>();
                var videoExtensions = configProvider.AppConfig?.VideoExtensions ?? new System.Collections.Generic.List<string> { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
                if (Directory.Exists(input))
                {
                    foreach (var ext in videoExtensions)
                    {
                        videoFiles.AddRange(Directory.GetFiles(input, "*" + ext, SearchOption.AllDirectories));
                    }
                    logger.LogInformation("Found {Count} video files in directory: {Dir}", videoFiles.Count, input);
                }
                else if (File.Exists(input) && videoExtensions.Exists(ext => input.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    videoFiles.Add(input);
                }
                else
                {
                    logger.LogError("Input must be a video file or directory containing videos: {Input}", input);
                    return;
                }
                string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrWhiteSpace(openAiApiKey) && configProvider.AppConfig?.OpenAi != null)
                {
                    openAiApiKey = configProvider.AppConfig.OpenAi.ApiKey;
                }
                foreach (var videoPath in videoFiles)
                {
                    try
                    {
                        logger.LogInformation("Processing video: {VideoPath}", videoPath);
                        var metadata = await videoProcessor.ExtractMetadataAsync(videoPath);
                        // In a real implementation, extract transcript or audio text here
                        string transcriptOrText = "[Simulated transcript or extracted text]";
                        string aiSummary = await videoProcessor.GenerateAiSummaryAsync(transcriptOrText, openAiApiKey, null, "chunk_summary_prompt.md");
                        string markdown = await videoProcessor.GenerateVideoNoteAsync(videoPath, openAiApiKey, "chunk_summary_prompt.md");
                        if (!dryRun)
                        {
                            string outputDir = output ?? (configProvider.AppConfig?.Paths?.NotebookVaultRoot ?? "Generated");
                            Directory.CreateDirectory(outputDir);
                            string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(videoPath) + ".md");
                            await File.WriteAllTextAsync(outputPath, markdown);
                            logger.LogInformation("Markdown note saved to: {OutputPath}", outputPath);
                        }
                        else
                        {
                            logger.LogInformation("[DRY RUN] Markdown note would be generated for: {VideoPath}", videoPath);
                        }
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process video: {VideoPath}", videoPath);
                        failed++;
                    }
                }
                logger.LogInformation("Video processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing video(s): {ex.Message}");
            }
        }
    }
}
