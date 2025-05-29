using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;

// Enable nullable reference type annotations
#nullable enable

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Provides AI-powered summarization using Microsoft.SemanticKernel with modern function-based approaches.
    /// Uses semantic functions for chunk processing and final aggregation, following modern SK patterns.
    /// Optimized for MBA coursework content including video transcripts and PDF processing.
    /// </summary>
    /// <example>
    /// <code>
    /// var summarizer = new AISummarizer(logger, promptService, kernel);
    /// var summary = await summarizer.SummarizeTextAsync(longText, null, "chunk_summary_prompt");
    /// </code>
    /// </example>
    public class AISummarizer
    {
        private readonly ILogger<AISummarizer> _logger;
        private readonly PromptTemplateService? _promptService;
        private readonly Kernel? _semanticKernel;
        private readonly ITextGenerationService? _textGenerationService;
        private readonly int _maxChunkTokens = 8000; // Character-based chunks
        private readonly int _overlapTokens = 500; // Characters to overlap between chunks        /// <summary>
        /// Initializes a new instance of the AISummarizer class.
        /// </summary>
        /// <param name="logger">The logger instance for tracking operations</param>
        /// <param name="promptService">Prompt template service for loading templates</param>
        /// <param name="semanticKernel">Microsoft.SemanticKernel kernel instance</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public AISummarizer(ILogger<AISummarizer> logger, PromptTemplateService? promptService, Kernel? semanticKernel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _promptService = promptService;
            _semanticKernel = semanticKernel;
            _textGenerationService = null;
        }

        /// <summary>
        /// Initializes a new instance of the AISummarizer class.
        /// </summary>
        /// <param name="logger">The logger instance for tracking operations</param>
        /// <param name="promptService">Prompt template service for loading templates</param>
        /// <param name="semanticKernel">Microsoft.SemanticKernel kernel instance</param>
        /// <param name="textGenerationService">Text generation service for test compatibility</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        public AISummarizer(ILogger<AISummarizer> logger, PromptTemplateService? promptService, Kernel? semanticKernel, ITextGenerationService? textGenerationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _promptService = promptService;
            _semanticKernel = semanticKernel;
            _textGenerationService = textGenerationService;
        }

        /// <summary>
        /// Generates a summary for the given text using the best available AI framework.
        /// Text will be chunked if it exceeds token limits. Supports variable substitution for metadata augmentation.
        /// </summary>
        /// <param name="inputText">The text to summarize.</param>
        /// <param name="variables">Optional dictionary of variables to substitute in the prompt template for metadata augmentation.</param>
        /// <param name="promptFileName">Optional prompt template name (e.g., "chunk_summary_prompt") without extension.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The summary text, or null if failed.</returns>
        /// <exception cref="ArgumentException">Thrown when inputText is null or empty</exception>
        /// <example>
        /// <code>
        /// var variables = new Dictionary&lt;string, string&gt; { ["course"] = "MBA Strategy", ["type"] = "video_transcript" };
        /// var summary = await summarizer.SummarizeWithVariablesAsync(
        ///     "Long text content...", 
        ///     variables, 
        ///     "chunk_summary_prompt"
        /// );
        /// </code>
        /// </example>
        public virtual async Task<string?> SummarizeWithVariablesAsync(string inputText, Dictionary<string, string>? variables = null, string? promptFileName = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                _logger.LogWarning("Input text is null or empty");
                return string.Empty;
            }

            // If no promptFileName is provided, use final_summary_prompt.md as default
            string effectivePromptFileName = promptFileName ?? "final_summary_prompt";
            if (string.IsNullOrEmpty(promptFileName))
            {
                _logger.LogDebug("No promptFileName provided. Using default: final_summary_prompt.md");
            }

            // Load and process the prompt template
            string? prompt = null;
            if (_promptService != null)
            {
                // Load the prompt template and substitute variables
                prompt = await _promptService.LoadTemplateAsync(effectivePromptFileName);
                if (variables != null && variables.Count > 0 && !string.IsNullOrEmpty(prompt))
                {
                    prompt = _promptService.SubstituteVariables(prompt, variables);
                    _logger.LogDebug("Substituted variables in prompt template");
                }
            }

            if (_semanticKernel == null)
            {
                // Fall back to direct ITextGenerationService if available
                if (_textGenerationService != null)
                {
                    _logger.LogDebug("Using ITextGenerationService fallback for summarization");

                    // Process the prompt template
                    (string? fallbackProcessedPrompt, string fallbackProcessedInputText) = await ProcessPromptTemplateAsync(
                        inputText,
                        prompt ?? string.Empty,
                        effectivePromptFileName);

                    // Use the processed prompt or fall back to a default
                    string finalPrompt = fallbackProcessedPrompt ?? $"Please summarize the following text:\n\n{fallbackProcessedInputText}";

                    try
                    {
                        var textContents = await _textGenerationService.GetTextContentsAsync(finalPrompt, null, null, cancellationToken);
                        var firstContent = textContents?.FirstOrDefault();
                        return firstContent?.Text;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate summary using ITextGenerationService");
                        return string.Empty;
                    }
                }

                _logger.LogWarning("No AI service is available. Returning null.");
                return null;
            }

            // Process the prompt template
            (string? processedPrompt, string processedInputText) = await ProcessPromptTemplateAsync(
                inputText,
                prompt ?? string.Empty,
                effectivePromptFileName);

            // Check if input likely exceeds character limits and needs chunking
            if (processedInputText.Length > _maxChunkTokens * 1.5)
            {
                _logger.LogInformation("Input text is large ({Length} characters). Using chunking strategy for summarization.", processedInputText.Length);
                return await SummarizeWithChunkingAsync(processedInputText, processedPrompt, variables, cancellationToken);
            }

            // For smaller texts, use the direct approach
            try
            {
                _logger.LogInformation("Using Microsoft.SemanticKernel for summarization");
                return await SummarizeWithSemanticKernelAsync(
                    processedInputText,
                    processedPrompt ?? string.Empty,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate summary");
                return string.Empty;
            }
        }

        /// <summary>
        /// Generates a summary for the given text using the best available AI framework, with variable substitution.
        /// Text will be chunked if it exceeds token limits.
        /// </summary>
        /// <param name="inputText">The text to summarize.</param>
        /// <param name="variables">Dictionary of variables to substitute in the prompt template.</param>
        /// <param name="promptFileName">Optional prompt template name (e.g., "chunk_summary_prompt") without extension.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The summary text, or null if failed.</returns>
        /// <example>
        /// <code>
        /// var variables = new Dictionary&lt;string, string&gt; { ["course"] = "MBA Strategy" };        /// <summary>
        /// Summarizes text using chunking to handle large inputs that exceed character limits.
        /// Uses modern Semantic Kernel approach with intelligent chunking optimized for MBA coursework.
        /// </summary>
        /// <param name="inputText">Text to summarize</param>
        /// <param name="prompt">Optional prompt to guide the summary generation</param>
        /// <param name="cancellationToken">Optional cancellation token for async operations</param>
        /// <returns>The consolidated summary, or null if failed</returns>
        private async Task<string?> SummarizeWithChunkingAsync(string inputText, string? prompt, Dictionary<string, string>? variables, CancellationToken cancellationToken)
        {
            if (_semanticKernel == null)
            {
                _logger.LogWarning("Semantic kernel is not available for chunked summarization. Returning simulated summary.");
                return await Task.FromResult("[Simulated AI summary]");
            }

            try
            {
                _logger.LogInformation("Starting chunked summarization process");

                // Split text into character-based chunks
                List<string> chunks = SplitTextIntoChunks(inputText, _maxChunkTokens, _overlapTokens);

                _logger.LogInformation("Split into {ChunkCount} chunks", chunks.Count);

                if (chunks.Count == 0)
                {
                    _logger.LogWarning("No valid chunks were generated");
                    return string.Empty;
                }

                // Load chunk and final summary prompts from PromptTemplateService if available
                string? chunkPromptTemplate = await LoadChunkPromptAsync();
                string? finalPromptTemplate = await LoadFinalPromptAsync();

                // Define the chunk summarizer function for MBA/coursework content
                string chunkSystemPrompt = !string.IsNullOrEmpty(chunkPromptTemplate)
                    ? chunkPromptTemplate
                    : "You are an expert MBA instructor. Summarize the following content from video transcripts and course PDFs, highlighting key concepts, frameworks, and real-world applications relevant to MBA studies.";

                var summarizeChunkFunction = _semanticKernel.CreateFunctionFromPrompt(
                    chunkSystemPrompt + "\n{{$input}}",
                    new OpenAIPromptExecutionSettings
                    {
                        MaxTokens = 2000,
                        Temperature = 0.2f,
                        TopP = 0.95f,
                        StopSequences = ["\n\n"]
                    });

                // Process each chunk individually
                var chunkSummaries = new List<string>();
                for (int i = 0; i < chunks.Count; i++)
                {

                    _logger.LogInformation("Processing chunk {ChunkIndex}/{TotalChunks}", i + 1, chunks.Count);

                    // Prepare KernelArguments with all required variables for the prompt
                    var kernelArgs = new KernelArguments
                    {
                        ["input"] = chunks[i],
                        ["content"] = chunks[i],
                        ["onedrivePath"] = variables != null && variables.ContainsKey("onedrivePath") ? variables["onedrivePath"] : string.Empty,
                        ["course"] = variables != null && variables.ContainsKey("course") ? variables["course"] : string.Empty
                    };

                    var result = await _semanticKernel.InvokeAsync(summarizeChunkFunction, kernelArgs);

                    string? chunkSummary = result.GetValue<string>()?.Trim();

                    if (!string.IsNullOrWhiteSpace(chunkSummary))
                    {
                        chunkSummaries.Add(chunkSummary);
                        _logger.LogDebug("Chunk {Index} summary: {Summary}", i, chunkSummary);
                    }
                }

                if (chunkSummaries.Count == 0)
                {
                    _logger.LogWarning("No chunk summaries were generated");
                    return string.Empty;
                }

                // If we only have one chunk summary, return it directly
                if (chunkSummaries.Count == 1)
                {
                    _logger.LogInformation("Only one chunk was processed, returning its summary directly");
                    return chunkSummaries[0];
                }

                // Define the aggregation function for final MBA summary
                string finalSystemPrompt = !string.IsNullOrEmpty(finalPromptTemplate)
                    ? finalPromptTemplate
                    : "You are an academic editor specializing in MBA coursework. Combine multiple partial summaries into one cohesive summary that emphasizes overarching themes, strategic frameworks, and actionable insights for students.";

                var aggregateSummariesFunction = _semanticKernel.CreateFunctionFromPrompt(
                    finalSystemPrompt + "\n{{$input}}",
                    new OpenAIPromptExecutionSettings
                    {
                        MaxTokens = 4000,
                        Temperature = 1.0f,
                        TopP = 1.0f
                    });

                // Combine and finalize
                string allSummaries = string.Join("\n\n", chunkSummaries);
                _logger.LogInformation("Aggregating {SummaryCount} summaries", chunkSummaries.Count);

                // Ensure yamlfrontmatter is present in variables
                var finalVariables = variables != null ? new Dictionary<string, string>(variables) : new Dictionary<string, string>();
                if (!finalVariables.ContainsKey("yamlfrontmatter"))
                {
                    finalVariables["yamlfrontmatter"] = string.Empty; // Or provide actual YAML frontmatter if available
                }

                var finalKernelArgs = new KernelArguments { ["input"] = allSummaries };
                foreach (var kvp in finalVariables)
                {
                    if (!finalKernelArgs.ContainsKey(kvp.Key))
                    {
                        finalKernelArgs[kvp.Key] = kvp.Value;
                    }
                }

                _logger.LogInformation("Final aggregateSummariesFunction: {aggregateSummariesFunction}", aggregateSummariesFunction);
                _logger.LogInformation("Final kernel arguments: {finalKernelArgs}", finalKernelArgs);

                var finalResult = await _semanticKernel.InvokeAsync(
                    aggregateSummariesFunction, finalKernelArgs);

                _logger.LogInformation("finalResult: {finalResult}", finalResult);

                string? finalSummary = finalResult.GetValue<string>()?.Trim();

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
                return string.Empty;
            }
        }

        /// <summary>
        /// Helper method to split text into chunks with overlap for optimal processing.
        /// Uses character-based chunking with intelligent boundary detection.
        /// </summary>
        /// <param name="text">The text to split</param>
        /// <param name="chunkSize">Maximum size of each chunk in characters</param>
        /// <param name="overlap">Number of characters to overlap between chunks</param>
        /// <returns>List of text chunks</returns>
        private static List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap)
        {
            List<string> chunks = new List<string>();
            int textLength = text.Length;
            int position = 0;

            while (position < textLength)
            {
                int length = Math.Min(chunkSize, textLength - position);
                string chunk = text.Substring(position, length);
                chunks.Add(chunk);

                // Move position forward, accounting for overlap
                position += (chunkSize - overlap);
            }

            return chunks;
        }

        /// <summary>
        /// Loads the chunk prompt template from the prompt service.
        /// </summary>
        /// <returns>The chunk prompt template or null if not available</returns>
        private async Task<string?> LoadChunkPromptAsync()
        {
            if (_promptService == null) return null;

            try
            {
                string? chunkPrompt = await _promptService.LoadTemplateAsync("chunk_summary_prompt");
                _logger.LogDebug("Loaded chunk prompt template: {PromptPreview}...",
                    chunkPrompt?.Substring(0, Math.Min(100, chunkPrompt.Length)) ?? "null");
                return chunkPrompt;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load chunk prompt template");
                return null;
            }
        }        /// <summary>
                 /// Loads the final prompt template from the prompt service.
                 /// </summary>
                 /// <returns>The final prompt template or null if not available</returns>
        private async Task<string?> LoadFinalPromptAsync()
        {
            if (_promptService == null) return null;

            try
            {
                string? finalPrompt = await _promptService.LoadTemplateAsync("final_summary_prompt");
                _logger.LogDebug("Loaded final prompt template: {PromptPreview}...",
                    finalPrompt?.Substring(0, Math.Min(100, finalPrompt.Length)) ?? "null");
                return finalPrompt;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load final prompt template");
                return null;
            }
        }

        /// <summary>
        /// Processes and prepares the prompt template for use.
        /// </summary>
        /// <param name="inputText">The input text to be summarized</param>
        /// <param name="prompt">The prompt string</param>
        /// <param name="promptFileName">The prompt template filename</param>
        /// <returns>A tuple containing the processed prompt and input text</returns>
        private async Task<(string? processedPrompt, string processedInputText)> ProcessPromptTemplateAsync(
            string inputText, string prompt, string promptFileName)
        {
            string? processedPrompt = prompt;
            string processedInputText = inputText;

            // If we have a prompt file name but no prompt, try to load the template
            if (string.IsNullOrEmpty(prompt) && !string.IsNullOrEmpty(promptFileName) && _promptService != null)
            {
                try
                {
                    processedPrompt = await _promptService.LoadTemplateAsync(promptFileName);
                    _logger.LogDebug("Loaded prompt template from file: {FileName}", promptFileName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load prompt template from file: {FileName}", promptFileName);
                }
            }

            return (processedPrompt, processedInputText);
        }

        /// <summary>
        /// Estimates the token count for the given text using a character-based heuristic.
        /// Uses approximately 4 characters per token as a rough estimate.
        /// </summary>
        /// <param name="text">The text to estimate tokens for</param>
        /// <returns>Estimated token count</returns>
        private int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            // Rough heuristic: ~4 characters per token for English text
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        /// <summary>
        /// Summarizes text using Microsoft SemanticKernel with the specified prompt.
        /// </summary>
        /// <param name="inputText">Text to summarize</param>
        /// <param name="prompt">Prompt to guide the summarization</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Summary text or null if failed</returns>
        private async Task<string?> SummarizeWithSemanticKernelAsync(string inputText, string prompt, CancellationToken cancellationToken)
        {
            if (_semanticKernel == null)
            {
                _logger.LogWarning("Semantic kernel is not available. Returning simulated summary.");
                return await Task.FromResult("[Simulated AI summary]");
            }

            try
            {
                // Use a default prompt if none provided
                string effectivePrompt = string.IsNullOrWhiteSpace(prompt)
                    ? "Provide a concise summary of the following text, highlighting key points and main ideas:"
                    : prompt;

                // Create a semantic function for summarization
                var function = _semanticKernel.CreateFunctionFromPrompt(
                    effectivePrompt + "\n{{$input}}",
                    new OpenAIPromptExecutionSettings
                    {
                        MaxTokens = 4000,
                        Temperature = 1.0f,
                        TopP = 1.0f
                    });

                // Execute the function
                var result = await _semanticKernel.InvokeAsync(function,
                    new KernelArguments { ["input"] = inputText });

                string? summary = result.GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(summary))
                {
                    _logger.LogWarning("SemanticKernel returned empty summary");
                    return null;
                }

                _logger.LogDebug("Generated summary with SemanticKernel: {SummaryLength} characters", summary.Length);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SemanticKernel summarization");
                return null;
            }
        }
    }
}
