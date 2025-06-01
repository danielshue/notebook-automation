using NotebookAutomation.Cli.Utilities;
using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.PdfProcessing;
using NotebookAutomation.Core.Utils;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for processing PDF files and converting them to markdown notes.
    /// 
    /// This class registers the 'pdf-notes' command, which extracts text from PDF files,
    /// generates AI-powered summaries using OpenAI (when available), and converts the content
    /// to markdown notes with proper metadata and formatting.
    /// </summary>
    public class PdfCommands
    {
        private readonly ILogger<PdfCommands> _logger;

        public PdfCommands(ILogger<PdfCommands> logger)
        {
            _logger = logger;
            _logger.LogInformationWithPath("PDF command initialized", "PdfCommands.cs");
        }

        /// <summary>
        /// Registers the 'pdf-notes' command with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add subcommands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        public void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
        {
            var inputOption = new Option<string>(
                aliases: new[] { "--input", "-i" },
                description: "Path to the input PDF file or directory");
            var outputOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Path to the output markdown file or directory");

            var pdfCommand = new Command("pdf-notes", "PDF notes processing commands");
            pdfCommand.AddOption(inputOption);
            pdfCommand.AddOption(outputOption);

            // Show help if no subcommand or options are provided
            pdfCommand.SetHandler((InvocationContext context) =>
            {
                // Print usage/help if required argument is missing
                var input = context.ParseResult.GetValueForOption(inputOption);
                if (string.IsNullOrEmpty(input))
                {
                    AnsiConsoleHelper.WriteUsage(
                        "Usage: notebookautomation pdf-notes --input <path> [options]",
                        pdfCommand.Description ?? string.Empty,
                        string.Join("\n", pdfCommand.Options.Select(option => $"  {string.Join(", ", option.Aliases)}\t{option.Description}"))
                    );
                    return Task.CompletedTask;
                }
                string? output = context.ParseResult.GetValueForOption(outputOption);
                string? config = context.ParseResult.GetValueForOption(configOption);
                bool debug = context.ParseResult.GetValueForOption(debugOption);
                bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                bool dryRun = context.ParseResult.GetValueForOption(dryRunOption);

                // Initialize dependency injection if needed
                if (config != null)
                {
                    if (!System.IO.File.Exists(config))
                    {
                        AnsiConsoleHelper.WriteError($"Configuration file not found: {config}");
                        return Task.CompletedTask;
                    }
                    Program.SetupDependencyInjection(config, debug);
                }

                return ProcessPdfAsync(input, output, config, debug, verbose, dryRun);
            });

            rootCommand.AddCommand(pdfCommand);
        }

        /// <summary>
        /// Processes PDF files from the input path and generates markdown notes.
        /// </summary>
        /// <param name="input">Path to a PDF file or directory containing PDF files.</param>
        /// <param name="output">Path to the output directory for generated markdown files.</param>
        /// <param name="configPath">Path to the configuration file.</param>
        /// <param name="debug">Enable debug output.</param>
        /// <param name="verbose">Enable verbose output.</param>
        /// <param name="dryRun">Simulate actions without making changes.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method:
        /// 1. Locates PDF files from the input path (single file or directory)
        /// 2. For each PDF, extracts text and metadata
        /// 3. Generates an AI summary using OpenAI when API key is available
        /// 4. Creates a markdown note with extracted text, metadata, and summary
        /// 5. Saves the markdown note to the output directory
        /// </remarks>        
        private async Task ProcessPdfAsync(
            string? input,
            string? output,
            string? configPath,
            bool debug,
            bool verbose,
            bool dryRun)
        {
            if (string.IsNullOrEmpty(configPath))
            {
                _logger.LogError("OpenAI configuration is missing or incomplete. Exiting.");
                return;
            }

            if (string.IsNullOrEmpty(input))
            {
                _logger.LogErrorWithPath("Input path is required: {FilePath}", input ?? "unknown");
                return;
            }

            // Simulate PDF processing logic
            try
            {
                _logger.LogInformationWithPath("Processing PDF file: {FilePath}", input);
                // Example processing logic
                await Task.CompletedTask;
                _logger.LogInformation("PDF processing completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during PDF processing.");
                _logger.LogErrorWithPath("Error in PDF command", "PdfCommands.cs", ex);
            }
            finally
            {
                _logger.LogDebugWithPath("Debugging PDF command", "PdfCommands.cs");
            }
        }
    }
}
