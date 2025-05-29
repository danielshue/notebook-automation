using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using System.Text;

namespace NotebookAutomation.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a ServiceCollection to handle dependency injection
            var services = new ServiceCollection();

            // Add logging with console output
            services.AddLogging(builder =>
            {
                builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Debug);
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Create a semantic kernel builder with OpenAI
            var modelName = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o";
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Error: OPENAI_API_KEY environment variable not set");
                return;
            }

            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(modelName, apiKey)
                .Build();

            services.AddSingleton(kernel);            // Register AISummarizer and PromptTemplateService
            var promptsDirectory = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "prompts");
            var absolutePromptsDir = Path.GetFullPath(promptsDirectory);
            Console.WriteLine($"Loading prompts from: {absolutePromptsDir}");
            // Create a configuration object with paths
            var pathsConfig = new PathsConfig
            {
                PromptsPath = absolutePromptsDir
            };

            // Create an AppConfig with paths using the parameterless constructor
            var appConfig = new AppConfig()
            {
                Paths = pathsConfig
            };

            // Register AppConfig
            services.AddSingleton<AppConfig>(appConfig);

            // Register PromptTemplateService
            services.AddSingleton<PromptTemplateService>();

            // Register the ITextGenerationService from the kernel
            services.AddSingleton<ITextGenerationService>(sp =>
                sp.GetRequiredService<Kernel>().GetRequiredService<ITextGenerationService>());            // Register AISummarizer with all required dependencies
            services.AddSingleton<AISummarizer>(sp =>
                new AISummarizer(
                    sp.GetRequiredService<ILoggerFactory>().CreateLogger<AISummarizer>(),
                    sp.GetRequiredService<PromptTemplateService>(),
                    sp.GetRequiredService<Kernel>()
                ));

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Get the AI summarizer
            var summarizer = serviceProvider.GetRequiredService<AISummarizer>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

            logger.LogInformation("Starting test summarization");

            // Create some test content
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Test Content for Summarization");
            sb.AppendLine();
            sb.AppendLine("This is some sample content that will be summarized by the AI system.");
            sb.AppendLine("We want to test the complete logging of the final prompt submitted to the summarizer.");
            sb.AppendLine();
            sb.AppendLine("## Section 1: Background");
            sb.AppendLine();
            sb.AppendLine("The application is designed to process educational content for MBA courses.");
            sb.AppendLine("It extracts key concepts, summarizes important points, and generates structured notes.");
            sb.AppendLine();
            sb.AppendLine("## Section 2: Methodology");
            sb.AppendLine();
            sb.AppendLine("The system uses a two-stage approach:");
            sb.AppendLine("1. First, it chunks large documents into manageable sections");
            sb.AppendLine("2. Then it summarizes each chunk individually");
            sb.AppendLine("3. Finally, it consolidates all summaries into a coherent output");

            string testContent = sb.ToString();

            // Create variables for substitution
            var variables = new Dictionary<string, string>
            {
                { "title", "Sample MBA Course Content" },
                { "yaml-frontmatter", "---\ntitle: Sample MBA Course Content\ntags:\n---" }
            };            // Call the summarizer
            var result = await summarizer.SummarizeWithVariablesAsync(testContent, variables, "final_summary_prompt");

            // Output the result
            logger.LogInformation("Summarization Result:");
            logger.LogInformation(result);

            Console.WriteLine("\nSummarization completed. Check the logs above for the final prompt.");
        }
    }
}
