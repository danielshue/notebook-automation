using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.PdfProcessing;

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for processing PDF files and converting them to markdown notes.
    /// 
    /// This class registers the 'pdf-notes' command, which extracts text from PDF files,
    /// generates AI-powered summaries using OpenAI (when available), and converts the content
    /// to markdown notes with proper metadata and formatting.
    /// </summary>
    internal static class PdfCommands
    {
        /// <summary>
        /// Registers the 'pdf-notes' command with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add subcommands to.</param>
        /// <param name="configOption">The global config file option.</param>
        /// <param name="debugOption">The global debug option.</param>
        /// <param name="verboseOption">The global verbose option.</param>
        /// <param name="dryRunOption">The global dry-run option.</param>
        public static void Register(RootCommand rootCommand, Option<string> configOption, Option<bool> debugOption, Option<bool> verboseOption, Option<bool> dryRunOption)
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
                // If no options or subcommands are provided, show help
                if (context.ParseResult.Tokens.Count == 0 && context.ParseResult.UnparsedTokens.Count == 0)
                {
                    context.Console.WriteLine("No subcommand provided. Showing help:\n");
                    context.Console.WriteLine(pdfCommand.Description ?? "");
                    context.Console.WriteLine("");
                    foreach (var option in pdfCommand.Options)
                    {
                        context.Console.WriteLine($"  {string.Join(", ", option.Aliases)}\t{option.Description}");
                    }
                    return Task.CompletedTask;
                }                // If options are provided, run the main handler
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
        private static async Task ProcessPdfAsync(
            string? input,
            string? output,
            string? configPath,
            bool debug,
            bool verbose,
            bool dryRun)
        {
            try
            {
                // Initialize dependency injection if needed
                if (configPath != null)
                {
                    Program.SetupDependencyInjection(configPath, debug);
                }
                  // Get services from DI container
                var serviceProvider = Program.ServiceProvider;
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("PdfCommands");
                var appConfig = serviceProvider.GetRequiredService<AppConfig>();
                var loggingService = serviceProvider.GetRequiredService<LoggingService>();
                var failedLogger = loggingService?.FailedLogger;
                
                if (string.IsNullOrEmpty(input))
                {
                    logger.LogError("Input path is required");
                    return;
                }

                // Get PdfNoteProcessor from DI container
                var pdfProcessor = serviceProvider.GetRequiredService<PdfNoteProcessor>();
                var processed = 0;
                var failed = 0;
                var pdfFiles = new List<string>();

                if (Directory.Exists(input))
                {
                    pdfFiles.AddRange(Directory.GetFiles(input, "*.pdf", SearchOption.AllDirectories));                    logger?.LogInformation("Found {Count} PDF files in directory: {Dir}", pdfFiles.Count, input);
                }
                else if (File.Exists(input) && input.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    pdfFiles.Add(input);
                }
                else
                {
                    logger?.LogError("Input must be a PDF file or directory containing PDFs: {Input}", input);
                    return;
                }
                
                // Get OpenAI API key from config or environment
                string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrWhiteSpace(openAiApiKey) && appConfig?.OpenAi != null)
                {
                    openAiApiKey = appConfig.OpenAi.ApiKey;
                }

                foreach (var pdfPath in pdfFiles)
                {
                    try
                    {
                        logger?.LogInformation("Processing PDF: {PdfPath}", pdfPath);
                        var (pdfText, metadata) = await pdfProcessor.ExtractTextAndMetadataAsync(pdfPath);
                        string aiSummary = await pdfProcessor.GenerateAiSummaryAsync(pdfText, openAiApiKey, null, "chunk_summary_prompt.md");
                        metadata["summary"] = aiSummary;
                        string markdown = pdfProcessor.GenerateMarkdownNote(pdfText, metadata);
                        if (!dryRun)
                        {                            string outputDir = output ?? (appConfig?.Paths?.NotebookVaultRoot ?? "Generated");
                            Directory.CreateDirectory(outputDir);
                            string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(pdfPath) + ".md");
                            await File.WriteAllTextAsync(outputPath, markdown);
                            logger?.LogInformation("Markdown note saved to: {OutputPath}", outputPath);
                        }
                        else
                        {
                            logger?.LogInformation("[DRY RUN] Markdown note would be generated for: {PdfPath}", pdfPath);
                        }
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed to process PDF: {PdfPath}", pdfPath);
                        failed++;
                    }
                }

                logger?.LogInformation("PDF processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing PDF(s): {ex.Message}");
            }
        }
    }
}
