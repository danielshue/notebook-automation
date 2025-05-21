using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.PdfProcessing;

namespace NotebookAutomation.Cli.PdfNotes
{
    /// <summary>
    /// Entry point for the PDF Notes CLI tool.
    /// 
    /// This program provides functionality for creating markdown notes from PDF files,
    /// extracting text, and organizing the resulting notes in the vault.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Exit code (0 for success, non-zero for error).</returns>
        public static async Task<int> Main(string[] args)
        {
            // Create the root command with description
            var rootCommand = new RootCommand(
                description: "PDF Notes - Tools for creating notes from PDF files");
                
            // Common options
            var inputOption = new Option<string>(
                aliases: new[] { "--input", "-i" },
                description: "Path to the input PDF file or directory");
                
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
                
            // Add options to the root command
            rootCommand.AddOption(inputOption);
            rootCommand.AddOption(outputOption);
            rootCommand.AddGlobalOption(configOption);
            rootCommand.AddGlobalOption(debugOption);
            rootCommand.AddGlobalOption(verboseOption);
            rootCommand.AddGlobalOption(dryRunOption);
            
            // Set the handler for the root command
            rootCommand.SetHandler(async (input, output, config, debug, verbose, dryRun) =>
            {
                await ProcessPdfAsync(input, output, config, debug, verbose, dryRun);
            }, inputOption, outputOption, configOption, debugOption, verboseOption, dryRunOption);
            
            // Execute the command
            return await rootCommand.InvokeAsync(args);
        }
        
        /// <summary>
        /// Processes PDF files to generate markdown notes.
        /// </summary>
        /// <param name="input">Input PDF file or directory.</param>
        /// <param name="output">Output markdown file or directory.</param>
        /// <param name="configPath">Optional configuration file path.</param>
        /// <param name="debug">Whether to enable debug output.</param>
        /// <param name="verbose">Whether to enable verbose output.</param>
        /// <param name="dryRun">Whether to perform a dry run.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
                var configProvider = ConfigProvider.Create(configPath, debug);
                var logger = configProvider.Logger;

                if (string.IsNullOrEmpty(input))
                {
                    logger.LogError("Input path is required");
                    return;
                }

                var pdfProcessor = new PdfNoteProcessor(logger);
                var processed = 0;
                var failed = 0;
                var pdfFiles = new List<string>();

                if (Directory.Exists(input))
                {
                    pdfFiles.AddRange(Directory.GetFiles(input, "*.pdf", SearchOption.AllDirectories));
                    logger.LogInformation("Found {Count} PDF files in directory: {Dir}", pdfFiles.Count, input);
                }
                else if (File.Exists(input) && input.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    pdfFiles.Add(input);
                }
                else
                {
                    logger.LogError("Input must be a PDF file or directory containing PDFs: {Input}", input);
                    return;
                }

                // Get OpenAI API key from config or environment
                string? openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (string.IsNullOrWhiteSpace(openAiApiKey) && configProvider.AppConfig?.OpenAi != null)
                {
                    openAiApiKey = configProvider.AppConfig.OpenAi.ApiKey;
                }

                foreach (var pdfPath in pdfFiles)
                {
                    try
                    {
                        logger.LogInformation("Processing PDF: {PdfPath}", pdfPath);
                        var (pdfText, metadata) = await pdfProcessor.ExtractTextAndMetadataAsync(pdfPath);
                        string aiSummary = await pdfProcessor.GenerateAiSummaryAsync(pdfText, openAiApiKey, null, "chunk_summary_prompt.md");
                        metadata["summary"] = aiSummary;
                        string markdown = pdfProcessor.GenerateMarkdownNote(pdfText, metadata);
                        if (!dryRun)
                        {
                            string outputDir = output ?? (configProvider.AppConfig?.Paths?.NotebookVaultRoot ?? "Generated");
                            Directory.CreateDirectory(outputDir);
                            string outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(pdfPath) + ".md");
                            await File.WriteAllTextAsync(outputPath, markdown);
                            logger.LogInformation("Markdown note saved to: {OutputPath}", outputPath);
                        }
                        else
                        {
                            logger.LogInformation("[DRY RUN] Markdown note would be generated for: {PdfPath}", pdfPath);
                        }
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process PDF: {PdfPath}", pdfPath);
                        failed++;
                    }
                }

                logger.LogInformation("PDF processing completed. Success: {Processed}, Failed: {Failed}", processed, failed);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing PDF(s): {ex.Message}");
            }
        }
    }
}
