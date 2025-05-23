using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.VideoProcessing;

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for processing video files and generating markdown notes.
    /// 
    /// This class registers the 'video-meta' command for processing video files to extract 
    /// metadata, generate markdown notes with appropriate frontmatter, and optionally 
    /// include references to the original video file.
    /// </summary>
    /// <remarks>
    /// The video processing functionality utilizes the <see cref="VideoNoteProcessingEntrypoint"/>
    /// from the Core library to handle the actual processing of video files. The supported 
    /// video formats are defined in the application configuration and typically include
    /// MP4, MOV, AVI, MKV, WEBM, and others.
    /// </remarks>
    internal class VideoCommands
    {
        /// <summary>
        /// Registers the 'video-meta' command with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add video processing commands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        /// <param name="verboseOption">The global verbose output option.</param>
        /// <param name="dryRunOption">The global dry run option to simulate actions without making changes.</param>
        public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var inputOption = new Option<string>(
                aliases: ["--input", "-i"],
                description: "Path to the input video file or directory");

            var outputOption = new Option<string>(
                aliases: ["--output", "-o"],
                description: "Path to the output markdown file or directory");

            var videoCommand = new Command("video-meta", "Video metadata commands");
            videoCommand.AddOption(inputOption);
            videoCommand.AddOption(outputOption);
            
            videoCommand.SetHandler(async context =>
            {
                string? input = context.ParseResult.GetValueForOption(inputOption);
                string? output = context.ParseResult.GetValueForOption(outputOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                // Initialize dependency injection if needed
                if (config != null)
                {
                    Program.SetupDependencyInjection(config, debug);
                }

                // Use DI container to get services
                var serviceProvider = Program.ServiceProvider;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("VideoCommands");
                var loggingService = serviceProvider.GetRequiredService<LoggingService>();

                var appConfig = serviceProvider.GetRequiredService<AppConfig>();
                var batchProcessor = serviceProvider.GetRequiredService<VideoNoteBatchProcessor>();

                // Get video extensions from config
                var videoExtensions = appConfig.VideoExtensions ?? new List<string> { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

                // Get OpenAI API key from environment or config
                string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrWhiteSpace(openAiApiKey) && appConfig.OpenAi != null)
                {
                    openAiApiKey = appConfig.OpenAi.ApiKey;
                }

                // Process videos
                if (string.IsNullOrEmpty(input))
                {
                    logger.LogError("Input path is required");
                    return;
                }

                var (processed, failed) = await batchProcessor.ProcessVideosAsync(
                    input,
                    output ?? appConfig.Paths?.NotebookVaultRoot ?? "Generated",
                    videoExtensions,
                    openAiApiKey,
                    dryRun);

                logger.LogInformation("Video processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            });

            rootCommand.AddCommand(videoCommand);
        }
    }
}
