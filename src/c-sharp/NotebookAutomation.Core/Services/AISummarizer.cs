using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Text;
using Microsoft.SemanticKernel.TextGeneration;

// Suppress warning about experimental TextChunker API
#pragma warning disable SKEXP0001

// Enable nullable reference type annotations
#nullable enable

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Provides AI-powered summarization using Microsoft.SemanticKernel.
    /// and direct API integration for maximum flexibility.
    /// </summary>
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only
    public class AISummarizer
    {
        protected readonly ILogger<AISummarizer> _logger;
        protected readonly PromptTemplateService? _promptService;
        protected readonly Kernel? _semanticKernel;
        protected readonly ITextGenerationService? _textGenService;
        protected readonly int _maxChunkTokens = 3000; // Maximum tokens per chunk
        protected readonly int _overlapTokens = 500; // Tokens to overlap between chunks

        /// <summary>
        /// Initializes a new instance of the AISummarizer class.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="promptService">Optional prompt template service for loading templates</param>
        /// <param name="semanticKernel">Optional Microsoft.SemanticKernel kernel</param>
        /// <param name="textGenerationService">Optional Microsoft.SemanticKernel text generation service</param>
        public AISummarizer(ILogger<AISummarizer> logger, PromptTemplateService promptService, Kernel semanticKernel, ITextGenerationService textGenerationService)
        {
            _logger = logger;
            _promptService = promptService;
            _semanticKernel = semanticKernel;
            _textGenService = textGenerationService;
        }        /// <summary>
                 /// Generates a summary for the given text using the best available AI framework.
                 /// Text will be chunked if it exceeds token limits.
                 /// </summary>
                 /// <param name="inputText">The text to summarize.</param>
                 /// <param name="prompt">Optional prompt to guide the summary.</param>
                 /// <param name="promptFileName">Optional prompt template name (e.g., "chunk_summary_prompt") without extension.</param>
                 /// <param name="cancellationToken">Optional cancellation token.</param>
                 /// <returns>The summary text, or null if failed.</returns>
        [Obsolete("Use SummarizeTextAsync or SummarizeWithVariablesAsync instead")]
        internal virtual async Task<string?> SummarizeAsync(string inputText, string? prompt = null, string? promptFileName = null, CancellationToken cancellationToken = default)
        {
            if (_semanticKernel == null && _textGenService == null)
            {
                _logger?.LogError("No AI service is available. Please provide an API key or configure a semantic kernel service.");

                return null;
            }

            // If no prompt and no promptFileName are provided, use final_summary_prompt.md as default
            string? effectivePromptFileName = promptFileName;
            string? effectivePrompt = prompt;
            if (string.IsNullOrWhiteSpace(prompt) && string.IsNullOrWhiteSpace(promptFileName))
            {
                effectivePromptFileName = "final_summary_prompt";
                _logger?.LogDebug("No prompt or promptFileName provided. Using default: final_summary_prompt.md");
            }

            // Process the prompt template
            (string? processedPrompt, string processedInputText) = await ProcessPromptTemplateAsync(
                inputText,
                effectivePrompt ?? string.Empty,
                effectivePromptFileName ?? string.Empty);

            // Check if input likely exceeds token limits and needs chunking
            if (EstimateTokenCount(processedInputText) > _maxChunkTokens * 1.5)
            {
                _logger?.LogInformation("Input text is large. Using chunking strategy for summarization.");

                return await SummarizeWithChunkingAsync(processedInputText, processedPrompt, cancellationToken);
            }

            // For smaller texts, use the direct approach
            // Try to use the available AI framework in order of preference
            try
            {
                // First try Semantic Kernel if available
                if (_textGenService != null || _semanticKernel != null)
                {
                    _logger?.LogInformation("Using Microsoft.SemanticKernel for summarization");

                    return await SummarizeWithSemanticKernelAsync(
                        processedInputText,
                        processedPrompt ?? string.Empty,
                        cancellationToken);
                }
                _logger?.LogError("No valid AI service configuration available");

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate summary");

                // If Semantic Kernel fails, fall back to text generation service if available
                return null;
            }
        }        /// <summary>
                 /// Public method for summarizing text without template variables.
                 /// </summary>
                 /// <param name="inputText">The text to summarize.</param>
                 /// <param name="prompt">Optional prompt to guide the summary.</param>
                 /// <param name="promptFileName">Optional prompt template name.</param>
                 /// <param name="cancellationToken">Optional cancellation token.</param>
                 /// <returns>The summary text, or null if failed.</returns>
        public virtual async Task<string?> SummarizeTextAsync(string inputText, string? prompt = null, string? promptFileName = null, CancellationToken cancellationToken = default)
        {
            // Forward to the internal implementation
#pragma warning disable CS0618 // Suppress obsolete warning since this is our wrapper
            return await SummarizeAsync(inputText, prompt, promptFileName, cancellationToken);
#pragma warning restore CS0618
        }/// <summary>
         /// Generates a summary for the given text using the best available AI framework, with variable substitution.
         /// Text will be chunked if it exceeds token limits.
         /// </summary>
         /// <param name="inputText">The text to summarize.</param>
         /// <param name="variables">Dictionary of variables to substitute in the prompt template.</param>
         /// <param name="promptFileName">Optional prompt template name (e.g., "chunk_summary_prompt") without extension.</param>
         /// <param name="cancellationToken">Optional cancellation token.</param>
         /// <returns>The summary text, or null if failed.</returns>
        public virtual async Task<string?> SummarizeWithVariablesAsync(string inputText, Dictionary<string, string>? variables = null, string? promptFileName = null, CancellationToken cancellationToken = default)
        {            // If no promptFileName is provided, use final_summary_prompt.md as default
            string effectivePromptFileName = promptFileName ?? "final_summary_prompt";
            if (string.IsNullOrEmpty(promptFileName))
            {
                _logger?.LogDebug("No promptFileName provided. Using default: final_summary_prompt.md");
            }

            string? prompt = null;
            if (_promptService != null)
            {
                // Load the prompt template and substitute variables
                prompt = await _promptService.LoadTemplateAsync(effectivePromptFileName);
                if (variables != null && variables.Count > 0 && !string.IsNullOrEmpty(prompt))
                {
                    prompt = _promptService.SubstituteVariables(prompt, variables);
                    _logger?.LogDebug("Substituted variables in prompt template");
                }
            }

#pragma warning disable CS0618 // Suppress obsolete warning since this is our wrapper
            return await SummarizeAsync(inputText, prompt, null, cancellationToken);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Summarizes text using chunking to handle large inputs that exceed token limits.
        /// </summary>
        /// <param name="inputText">Text to summarize</param>
        /// <param name="prompt">Optional prompt to guide the summary generation</param>
        /// <param name="cancellationToken">Optional cancellation token for async operations</param>
        /// <returns>The consolidated summary, or null if failed</returns>
        /// <remarks>
        /// This method implements a sophisticated multi-stage chunking and summarization workflow:
        /// 1. Detects if the content contains markdown and uses appropriate chunking strategy
        /// 2. Splits the text into overlapping chunks while preserving meaning
        /// 3. Processes each chunk with position awareness (beginning, middle, end)
        /// 4. Consolidates the individual summaries into a coherent final summary
        /// 
        /// The implementation uses Microsoft.SemanticKernel.Text.TextChunker for intelligent
        /// content splitting that respects semantic boundaries when possible.
        /// </remarks>
        private async Task<string?> SummarizeWithChunkingAsync(string inputText, string? prompt, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting chunked summarization process");

                // Split text into chunks using SemanticKernel's TextChunker
                List<string> chunks;

                // Using TextChunker with appropriate warning suppression since it's marked as experimental API
#pragma warning disable SKEXP0001, SKEXP0050 // Type is for evaluation purposes only
                if (ContainsMarkdown(inputText))
                {
                    var lines = TextChunker.SplitMarkDownLines(inputText, _maxChunkTokens);

                    chunks = TextChunker.SplitMarkdownParagraphs(lines, _maxChunkTokens, _overlapTokens);
                }
                else
                {
                    var lines = TextChunker.SplitPlainTextLines(inputText, _maxChunkTokens);
                    chunks = TextChunker.SplitPlainTextParagraphs(lines, _maxChunkTokens, _overlapTokens);
                }
#pragma warning restore SKEXP0001, SKEXP0050

                _logger.LogInformation($"Text split into {chunks.Count} chunks for processing");

                if (chunks.Count == 0)
                {
                    _logger.LogWarning("No valid chunks were generated");
                    return null;
                }

                // Load chunk and final summary prompts from PromptTemplateService if available
                string? chunkPromptTemplate = prompt;
                string? finalPromptTemplate = null;
                if (_promptService != null)
                {
                    chunkPromptTemplate = await _promptService.LoadTemplateAsync("chunk_summary_prompt");
                    _logger.LogDebug($"Loaded chunk prompt template from chunk_summary_prompt.md: {{First100=}}{chunkPromptTemplate?.Substring(0, Math.Min(100, chunkPromptTemplate.Length)) ?? "null"}...");
                    finalPromptTemplate = await _promptService.LoadTemplateAsync("final_summary_prompt");
                    _logger.LogDebug($"Loaded final prompt template from final_summary_prompt.md: {{First100=}}{finalPromptTemplate?.Substring(0, Math.Min(100, finalPromptTemplate.Length)) ?? "null"}...");
                }

                // Process each chunk individually with position awareness
                var chunkSummaries = new List<string>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    _logger.LogInformation($"Processing chunk {i + 1}/{chunks.Count}");

                    // Add positional context to help maintain document flow
                    string chunkContext = string.Empty;
                    if (chunks.Count > 1)
                    {
                        chunkContext = $"This is part {i + 1} of {chunks.Count}. ";
                        bool isFirstChunk = i == 0;
                        bool isLastChunk = i == chunks.Count - 1;
                        if (isFirstChunk)
                        {
                            chunkContext += "This is the beginning of the document. ";
                        }
                        else if (isLastChunk)
                        {
                            chunkContext += "This is the end of the document. ";
                        }
                        else
                        {
                            chunkContext += "This is a middle section of the document. ";
                        }
                    }

                    // Substitute variables for chunk prompt
                    string chunkPrompt = chunkPromptTemplate ?? string.Empty;
                    if (_promptService != null)
                    {
                        var variables = new Dictionary<string, string>
                        {
                            { "content", chunks[i] },
                            { "chunk_context", chunkContext },
                            { "chunk_num", (i + 1).ToString() },
                            { "total_chunks", chunks.Count.ToString() }
                        };
                        if (chunkPromptTemplate != null)
                        {
                            chunkPrompt = _promptService.SubstituteVariables(chunkPromptTemplate, variables);
                        }
                        _logger.LogDebug($"Using chunk prompt for chunk {i + 1}: {{First100=}}{chunkPrompt?[..Math.Min(100, chunkPrompt.Length)] ?? "null"}...");
                    }

                    string? chunkSummary = await SummarizeWithSemanticKernelAsync(
                        chunks[i],
                        chunkPrompt ?? string.Empty,
                        cancellationToken);

                    if (!string.IsNullOrWhiteSpace(chunkSummary))
                    {
                        chunkSummaries.Add(chunkSummary);
                    }
                }
                if (chunkSummaries.Count == 0)
                {
                    _logger.LogWarning("No chunk summaries were generated");
                    return null;
                }

                // If we only have one chunk summary, return it directly
                if (chunkSummaries.Count == 1)
                {
                    _logger.LogInformation("Only one chunk was processed, returning its summary directly");
                    return chunkSummaries[0];
                }

                // Format the combined chunk summaries with clear section markers
                var formattedChunkSummaries = new List<string>(chunkSummaries.Count);
                for (int i = 0; i < chunkSummaries.Count; i++)
                {
                    formattedChunkSummaries.Add(
                        $"--- CHUNK {i + 1}/{chunkSummaries.Count} SUMMARY ---\n{chunkSummaries[i]}");
                }

                // Use final_summary_prompt.md for the final summary
                string consolidationText = string.Join("\n\n", formattedChunkSummaries);
                string finalPrompt = finalPromptTemplate ?? "Create a coherent, comprehensive summary from these section summaries. Maintain the logical flow of ideas from the original document. Ensure the final summary is well-structured and reads as a unified piece:";
                if (_promptService != null)
                {
                    var variables = new Dictionary<string, string>
                    {
                        { "content", consolidationText }
                    };
                    finalPrompt = _promptService.SubstituteVariables(finalPrompt, variables);
                    _logger.LogDebug($"Using final summary prompt: {{First200=}}{finalPrompt?.Substring(0, Math.Min(200, finalPrompt.Length)) ?? "null"}...");
                }

                _logger.LogInformation($"Generating final consolidated summary from {chunkSummaries.Count} chunk summaries");
                string? finalSummary = await SummarizeWithSemanticKernelAsync(
                    string.Empty, // content is already in the prompt
                    finalPrompt ?? string.Empty,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(finalSummary))
                {
                    _logger.LogWarning("Final consolidation returned empty summary. Falling back to combined chunk summaries.");
                    return string.Join("\n\n", chunkSummaries);
                }

                return finalSummary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chunked summarization process");
                return null;
            }
        }
        /// <summary>
        /// Estimates token count in a string using a simple heuristic.
        /// </summary>
        /// <param name="text">Text to estimate token count for</param>
        /// <returns>Estimated token count</returns>
        /// <remarks>
        /// This method provides a rough estimation of token count for use in determining
        /// if text needs chunking. It uses several heuristics to improve accuracy over
        /// a simple character count, accounting for common token patterns in various languages.
        /// For more precise token counting, a proper tokenizer should be used.
        /// </remarks>
        private int EstimateTokenCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            // Split by whitespace to get a rough word count
            string[] words = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

            // Count punctuation and special characters separately as they often become individual tokens
            int punctuationCount = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

            // Calculate a weighted token count:
            // - Most words become ~1 token
            // - Numbers and short words (<3 chars) are often fractional tokens
            // - Special characters and punctuation often become separate tokens
            int shortWordCount = words.Count(w => w.Length < 3);
            int normalWordCount = words.Length - shortWordCount;

            // Apply weightings to different elements (these weights are approximations)
            double estimatedTokens = (normalWordCount * 1.0) + (shortWordCount * 0.5) + (punctuationCount * 0.5);

            // Apply a safety factor of 1.2 to avoid underestimation
            return (int)(estimatedTokens * 1.2);
        }

        /// <summary>
        /// Checks if text likely contains markdown formatting.
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>True if text likely contains markdown</returns>
        /// <remarks>
        /// This method detects common markdown patterns to determine if the Markdown-specific
        /// chunking methods should be used. It looks for headers, lists, code blocks, links,
        /// and other markdown formatting elements.
        /// </remarks>
        private bool ContainsMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // Sample the first 5000 characters to avoid scanning extremely large texts
            string sample = text.Length > 5000 ? text.Substring(0, 5000) : text;

            // Define regex patterns for common markdown elements
            // Using simple string contains for performance, could use regex for more accuracy

            // Check for headers (both # style and === or --- style)
            bool hasHeaders = sample.Contains("\n# ") || sample.Contains("\n## ") ||
                              sample.Contains("\n=====") || sample.Contains("\n---");

            // Check for lists
            bool hasLists = sample.Contains("\n- ") || sample.Contains("\n* ") ||
                            sample.Contains("\n1. ") || sample.Contains("\n+ ");

            // Check for code blocks and formatting
            bool hasCodeElements = sample.Contains("```") || sample.Contains("`") ||
                                  sample.Contains("    ") && sample.Contains("\n    ");

            // Check for links, images and other formatting
            bool hasOtherFormatting = (sample.Contains("[") && sample.Contains("](")) ||
                                     sample.Contains("**") || sample.Contains("__") ||
                                     sample.Contains(">") && sample.Contains("\n>");

            // Check for tables
            bool hasTables = sample.Contains("|") && sample.Contains("\n|") &&
                            sample.Contains("|--") || sample.Contains("--|");

            // Return true if any markdown patterns are found
            return hasHeaders || hasLists || hasCodeElements || hasOtherFormatting || hasTables;
        }

        /// <summary>
        /// Processes prompt templates and variables.
        /// </summary>
        /// <param name="inputText">The original input text</param>
        /// <param name="prompt">Direct prompt text</param>
        /// <param name="promptFileName">Template file name</param>
        /// <returns>A tuple with the processed prompt and input text</returns>
        private async Task<(string? prompt, string inputText)> ProcessPromptTemplateAsync(string inputText, string prompt, string promptFileName)
        {
            string? promptText = prompt;
            string processedInputText = inputText;

            _logger.LogDebug("Processing prompt template with file name: {PromptFileName}", promptFileName);

            // Try to load the prompt from PromptTemplateService if available
            if (_promptService != null && !string.IsNullOrWhiteSpace(promptFileName))
            {
                try
                {
                    // Load the prompt template
                    promptText = await _promptService.LoadTemplateAsync(promptFileName);

                    // Substitute content variable if template was loaded
                    if (!string.IsNullOrEmpty(promptText))
                    {
                        var variables = new Dictionary<string, string>
                        {
                            { "content", inputText }
                        };

                        // Replace inputText with an empty string since we'll include it in the prompt
                        processedInputText = string.Empty;
                        promptText = _promptService.SubstituteVariables(promptText, variables);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading prompt template: {PromptFileName}", promptFileName);

                    // Fall back to the provided prompt
                    promptText = prompt;

                    processedInputText = inputText;
                }
            }

            return (promptText, processedInputText);
        }

        /// <summary>
        /// Summarizes text using Microsoft.SemanticKernel
        /// </summary>
        /// <param name="inputText">Text to summarize</param>
        /// <param name="prompt">Optional prompt</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The summary, or null if failed</returns>
        private async Task<string?> SummarizeWithSemanticKernelAsync(string inputText, string? prompt, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Generating summary with Semantic Kernel");
                _logger.LogDebug("Input text length: {Length}, Prompt: {Prompt}", inputText.Length, prompt ?? "null");

                // Get text generation service - either from constructor or kernel
                ITextGenerationService? textGeneration = _textGenService;

                if (textGeneration == null && _semanticKernel != null)
                {
                    try
                    {
                        textGeneration = _semanticKernel.GetRequiredService<ITextGenerationService>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get text generation service from kernel");
                        return null;
                    }
                }

                if (textGeneration == null)
                {
                    throw new InvalidOperationException("No text generation service available");
                }

                // Prepare the full prompt
                string fullPrompt;
                if (!string.IsNullOrEmpty(prompt))
                {
                    fullPrompt = string.IsNullOrEmpty(inputText) ? prompt : $"{prompt}\n\n{inputText}";
                }
                else
                {
                    fullPrompt = $"Summarize the following text:\n\n{inputText}";
                }

                // Configure generation settings
                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 1000,
                    Temperature = 0.3,
                    TopP = 0.9,
                    FrequencyPenalty = 0,
                    PresencePenalty = 0
                };

                // Generate summary
                var result = await textGeneration.GetTextContentAsync(
                    fullPrompt,
                    executionSettings,
                    kernel: _semanticKernel,
                    cancellationToken);

                return result.Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating summary with Semantic Kernel");
                return null;
            }
        }
    }
}
