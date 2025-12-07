// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.VideoTranscriptProcessing;

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Command group for consolidating video transcripts into a single markdown file.
/// </summary>
internal class VideoTranscriptCommands
{
    private readonly ILogger<VideoTranscriptCommands> _logger;
    private readonly IServiceProvider _serviceProvider;

    public VideoTranscriptCommands(ILogger<VideoTranscriptCommands> logger, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public void Register(
        RootCommand rootCommand,
        Option<string> configOption,
        Option<bool> debugOption,
        Option<bool> verboseOption,
        Option<bool> dryRunOption)
    {
        ArgumentNullException.ThrowIfNull(rootCommand);

        var pathOption = new Option<string>(
            aliases: ["--path", "-p"],
            description: "Path to the class or module folder in OneDrive or relative to configuration.")
        {
            IsRequired = true,
        };

        var recursiveOption = new Option<bool>(
            aliases: ["--recursive"],
            description: "Process all nested folders recursively.");

        var forceOption = new Option<bool>(
            aliases: ["--force"],
            description: "Overwrite existing consolidated markdown even if the transcript list is unchanged.");

        var consolidateCommand = new Command("consolidate", "Create a consolidated video transcript markdown file for the specified folder.");
        consolidateCommand.AddOption(pathOption);
        consolidateCommand.AddOption(recursiveOption);
        consolidateCommand.AddOption(forceOption);

        consolidateCommand.SetHandler(async context =>
        {
            string? inputPath = context.ParseResult.GetValueForOption(pathOption);
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                AnsiConsoleHelper.WriteUsage(
                    "Usage: notebookautomation video-transcripts consolidate --path <folder> [--recursive]",
                    consolidateCommand.Description ?? string.Empty,
                    string.Join("\n", consolidateCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}")));
                return;
            }

            string? config = context.ParseResult.GetValueForOption(configOption);
            bool debug = context.ParseResult.GetValueForOption(debugOption);
            bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            bool recursive = context.ParseResult.GetValueForOption(recursiveOption);
            bool force = context.ParseResult.GetValueForOption(forceOption);

            if (Program.ServiceProvider == null)
            {
                Program.SetupDependencyInjection(config, debug);
            }

            var provider = Program.ServiceProvider;
            if (provider == null)
            {
                AnsiConsoleHelper.WriteError("Failed to initialize services for transcript consolidation.");
                context.ExitCode = 1;
                return;
            }

            using var scope = provider.CreateScope();
            var scopedServices = scope.ServiceProvider;

            try
            {
                var service = scopedServices.GetRequiredService<VideoTranscriptConsolidationService>();
                var request = new VideoTranscriptConsolidationRequest(inputPath!, recursive, force, dryRun);
                var result = await service.ConsolidateAsync(request, context.GetCancellationToken()).ConfigureAwait(false);

                if (result.AggregatedCount == 0)
                {
                    AnsiConsoleHelper.WriteWarning("No transcripts were discovered for consolidation.");
                }

                AnsiConsoleHelper.WriteSuccess($"Consolidated {result.AggregatedCount} transcript(s); skipped {result.SkippedCount}.\nOutput: {result.OutputPath}");

                if (!result.WasWritten)
                {
                    AnsiConsoleHelper.WriteInfo("Existing consolidated note already matches the discovered transcripts. No changes written.");
                }

                if (dryRun)
                {
                    AnsiConsoleHelper.WriteInfo("Dry run mode enabled; no files were written.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consolidating transcripts");
                ExceptionHandler.HandleException(ex, "consolidating transcripts");
                context.ExitCode = 1;
            }
        });

        var root = new Command("video-transcripts", "Utilities for consolidating existing video transcripts into markdown notes.");
        root.AddCommand(consolidateCommand);

        rootCommand.AddCommand(root);
    }
}
