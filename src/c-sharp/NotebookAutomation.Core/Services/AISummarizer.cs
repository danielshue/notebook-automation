using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;

// Enable nullable reference type annotations
#nullable enable

namespace NotebookAutomation.Core.Services
{    /// <summary>
    /// Provides AI-powered text summarization using Microsoft.SemanticKernel with Azure OpenAI integration.
    /// Implements intelligent chunking strategies for large text processing, optimized for MBA coursework content
    /// including video transcripts, PDF documents, and academic materials. Supports variable substitution
    /// for metadata augmentation and configurable prompt templates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides two main summarization strategies:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Direct summarization for smaller texts (under ~12,000 characters)</description></item>
    /// <item><description>Chunked summarization with aggregation for larger texts</description></item>
    /// </list>
    /// <para>
    /// The chunking strategy splits large texts into overlapping segments, processes each chunk independently,
    /// then aggregates the results into a cohesive final summary. This approach ensures comprehensive coverage
    /// while respecting token limits of the underlying AI models.
    /// </para>
    /// <para>
    /// Supports fallback to ITextGenerationService for testing scenarios when SemanticKernel is unavailable.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage
    /// var summarizer = new AISummarizer(logger, promptService, kernel);
    /// var summary = await summarizer.SummarizeWithVariablesAsync(longText);
    /// 
    /// // With metadata variables and custom prompt
    /// var variables = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["course"] = "MBA Strategy",
    ///     ["type"] = "video_transcript",
    ///     ["onedrivePath"] = "/courses/strategy/week1"
    /// };
    /// var summary = await summarizer.SummarizeWithVariablesAsync(
    ///     inputText, 
    ///     variables, 
    ///     "chunk_summary_prompt"
    /// );
    /// </code>
    /// </example>
    public class AISummarizer
    {
        private readonly ILogger<AISummarizer> _logger;
        private readonly PromptTemplateService? _promptService;
        private readonly Kernel? _semanticKernel;
        private readonly ITextGenerationService? _textGenerationService;        
        
        /// <summary>
        /// The maximum size for individual text chunks in characters before triggering chunked processing.
        /// Set to 8000 characters which approximates 2000 tokens using the 4:1 character-to-token ratio.
        /// </summary>
        private readonly int _maxChunkTokens = 8000; // Character-based chunks
        
        /// <summary>
        /// The number of characters to overlap between adjacent chunks to maintain context continuity.
        /// Set to 500 characters to ensure important context isn't lost at chunk boundaries.
        /// </summary>
        private readonly int _overlapTokens = 500; // Characters to overlap between chunks        

        /// <summary>
        /// Initializes a new instance of the AISummarizer class with SemanticKernel support.
        /// </summary>
        /// <param name="logger">The logger instance for tracking operations and debugging</param>
        /// <param name="promptService">Service for loading and processing prompt templates from the file system</param>
        /// <param name="semanticKernel">Microsoft.SemanticKernel instance configured with Azure OpenAI</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        /// <remarks>
        /// This constructor is the primary initialization path for production usage with Azure OpenAI.
        /// The promptService and semanticKernel can be null for testing scenarios, but functionality will be limited.
        /// </remarks>
        public AISummarizer(ILogger<AISummarizer> logger, PromptTemplateService? promptService, Kernel? semanticKernel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _promptService = promptService;
            _semanticKernel = semanticKernel;
            _textGenerationService = null;
        }        
        
        /// <summary>
        /// Initializes a new instance of the AISummarizer class with additional test compatibility support.
        /// </summary>
        /// <param name="logger">The logger instance for tracking operations and debugging</param>
        /// <param name="promptService">Service for loading and processing prompt templates from the file system</param>
        /// <param name="semanticKernel">Microsoft.SemanticKernel instance configured with Azure OpenAI</param>
        /// <param name="textGenerationService">Fallback text generation service for unit testing scenarios</param>
        /// <exception cref="ArgumentNullException">Thrown when logger is null</exception>
        /// <remarks>
        /// This constructor supports testing scenarios where SemanticKernel might not be available.
        /// When semanticKernel is null, the service will attempt to use textGenerationService as a fallback.
        /// </remarks>
        public AISummarizer(ILogger<AISummarizer> logger, PromptTemplateService? promptService, Kernel? semanticKernel, ITextGenerationService? textGenerationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _promptService = promptService;
            _semanticKernel = semanticKernel;
            _textGenerationService = textGenerationService;
        }        /// <summary>
        /// Generates an AI-powered summary for the given text using the best available AI framework.
        /// Automatically selects between direct summarization and chunked processing based on text length.
        /// Supports variable substitution for metadata augmentation and custom prompt templates.
        /// </summary>
        /// <param name="inputText">The text content to summarize. Cannot be null or empty.</param>
        /// <param name="variables">Optional dictionary of variables for prompt template substitution and metadata enhancement.
        /// Common variables include: course, type, onedrivePath, yamlfrontmatter.</param>
        /// <param name="promptFileName">Optional prompt template filename (without .md extension) to customize summarization behavior.
        /// Defaults to "final_summary_prompt" if not provided.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the asynchronous operation.</param>
        /// <returns>
        /// A task that represents the asynchronous summarization operation. The task result contains:
        /// - The generated summary text for successful operations
        /// - An empty string if the operation fails but the service is available
        /// - null if no AI service is available
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when inputText is null or empty</exception>
        /// <remarks>
        /// <para>
        /// The method automatically determines the optimal summarization strategy:
        /// </para>
        /// <list type="bullet">
        /// <item><description>For texts under ~12,000 characters: Direct summarization using a single AI call</description></item>
        /// <item><description>For larger texts: Chunked summarization with intelligent aggregation</description></item>
        /// </list>
        /// <para>
        /// Variable substitution occurs when both variables and promptService are available.
        /// The prompt template is loaded from the prompts directory and variables are replaced using
        /// the format {{variableName}}.
        /// </para>
        /// <para>
        /// Fallback behavior when SemanticKernel is unavailable:
        /// 1. Attempts to use ITextGenerationService if available
        /// 2. Returns null if no AI services are configured
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic summarization
        /// var summary = await summarizer.SummarizeWithVariablesAsync("Long text content...");
        /// 
        /// // With metadata variables for MBA coursework
        /// var variables = new Dictionary&lt;string, string&gt;
        /// {
        ///     ["course"] = "MBA Strategy",
        ///     ["type"] = "video_transcript",
        ///     ["onedrivePath"] = "/courses/strategy/week1"
        /// };
        /// var summary = await summarizer.SummarizeWithVariablesAsync(
        ///     inputText, 
        ///     variables, 
        ///     "chunk_summary_prompt"
        /// );
        /// 
        /// // With cancellation support
        /// using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        /// var summary = await summarizer.SummarizeWithVariablesAsync(
        ///     inputText, 
        ///     variables, 
        ///     "chunk_summary_prompt", 
        ///     cts.Token
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
            }            // For smaller texts, use the direct approach
            try
            {
                _logger.LogDebug("Using Microsoft.SemanticKernel for direct summarization");
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
        /// <code>        /// <summary>
        /// Summarizes text using chunking to handle large inputs that exceed character limits.
        /// Uses modern Semantic Kernel approach with intelligent chunking optimized for MBA coursework.
        /// Implements a two-stage process: individual chunk summarization followed by aggregation.
        /// </summary>
        /// <param name="inputText">The text content to summarize using chunked processing</param>
        /// <param name="prompt">Optional base prompt to guide the summary generation for chunks</param>
        /// <param name="variables">Optional dictionary of variables for prompt template substitution</param>
        /// <param name="cancellationToken">Optional cancellation token for async operations</param>
        /// <returns>
        /// A task that represents the asynchronous chunked summarization operation. The task result contains:
        /// - The consolidated summary combining all chunk summaries
        /// - An empty string if the operation fails
        /// - A simulated summary if SemanticKernel is unavailable
        /// </returns>
        /// <remarks>
        /// <para>
        /// The chunking process follows these steps:
        /// </para>
        /// <list type="number">
        /// <item><description>Split the input text into overlapping chunks based on character limits</description></item>
        /// <item><description>Load chunk and final summary prompt templates from the prompt service</description></item>
        /// <item><description>Process each chunk individually with the chunk summarization prompt</description></item>
        /// <item><description>Aggregate all chunk summaries using the final summary prompt</description></item>
        /// <item><description>Return the consolidated result or fall back to combined chunks if aggregation fails</description></item>
        /// </list>
        /// <para>
        /// Chunk processing includes extensive debug logging to track the summarization pipeline.
        /// Each chunk is logged with its prompt, content, and resulting summary for troubleshooting.
        /// </para>
        /// </remarks>
        private async Task<string?> SummarizeWithChunkingAsync(string inputText, string? prompt, Dictionary<string, string>? variables, CancellationToken cancellationToken)
        {
            if (_semanticKernel == null)
            {
                _logger.LogWarning("Semantic kernel is not available for chunked summarization. Returning simulated summary.");
                return await Task.FromResult("[Simulated AI summary]");
            }

            try
            {
                _logger.LogInformation("Starting chunked summarization process");                // Split text into character-based chunks
                List<string> chunks = SplitTextIntoChunks(inputText, _maxChunkTokens, _overlapTokens);

                _logger.LogDebug("Split into {ChunkCount} chunks", chunks.Count);

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
                        MaxTokens = 2048
                    }
                );                // Process each chunk individually
                var chunkSummaries = new List<string>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    _logger.LogDebug("Processing chunk {ChunkIndex}/{TotalChunks}", i + 1, chunks.Count);
                    _logger.LogDebug("Chunk prompt being sent:\n{Prompt}", chunkSystemPrompt);
                    _logger.LogDebug("Chunk content being sent:\n{Chunk}", chunks[i]);

                    // Prepare KernelArguments with all required variables for the prompt
                    var kernelArgs = new KernelArguments
                    {
                        ["input"] = chunks[i],
                        ["content"] = chunks[i],
                        ["onedrivePath"] = variables != null && variables.ContainsKey("onedrivePath") ? variables["onedrivePath"] : string.Empty,
                        ["course"] = variables != null && variables.ContainsKey("course") ? variables["course"] : string.Empty
                    };

                    var result = await _semanticKernel.InvokeAsync(summarizeChunkFunction, kernelArgs);

                    // Log the raw result object for debugging
                    _logger.LogDebug("Raw model response for chunk {Index}: {Result}", i, result);

                    string? chunkSummary = result.GetValue<string>()?.Trim();

                    if (!string.IsNullOrWhiteSpace(chunkSummary))
                    {
                        chunkSummaries.Add(chunkSummary);
                        _logger.LogDebug("Chunk {Index} summary: {Summary}", i, chunkSummary);
                    }
                    else
                    {
                        _logger.LogWarning("Chunk {Index} returned empty or whitespace summary.", i);
                    }
                }

                if (chunkSummaries.Count == 0)
                {
                    _logger.LogWarning("No chunk summaries were generated");
                    return string.Empty;
                }                // If we only have one chunk summary, return it directly
                if (chunkSummaries.Count == 1)
                {
                    _logger.LogDebug("Only one chunk was processed, returning its summary directly");
                    return chunkSummaries[0];
                }

                // Define the aggregation function for final MBA summary
                string finalSystemPrompt = !string.IsNullOrEmpty(finalPromptTemplate)
                    ? finalPromptTemplate
                    : "You are an academic editor specializing in MBA coursework. Combine multiple partial summaries into one cohesive summary that emphasizes overarching themes, strategic frameworks, and actionable insights for students.";

                // this is the final prompt that will be used to aggregate the summaries
                var aggregateSummariesFunction = _semanticKernel.CreateFunctionFromPrompt(
                    finalSystemPrompt + "\n{{$input}}",
                    new OpenAIPromptExecutionSettings
                    {
                        MaxTokens = 2048
                    }
                );                // Combine and finalize
                string allSummaries = string.Join("\n\n", chunkSummaries);
                _logger.LogDebug("Aggregating {SummaryCount} summaries", chunkSummaries.Count);

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
                        finalKernelArgs[kvp.Key] = kvp.Value;                    }
                }

                _logger.LogDebug("Final aggregateSummariesFunction: {aggregateSummariesFunction}", aggregateSummariesFunction);
                _logger.LogDebug("Final kernel arguments: {finalKernelArgs}", finalKernelArgs);

                var finalResult = await _semanticKernel.InvokeAsync(
                    aggregateSummariesFunction, finalKernelArgs);

                _logger.LogDebug("finalResult: {finalResult}", finalResult);

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
        /// Loads the chunk prompt template from the prompt service for individual chunk processing.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous load operation. The task result contains:
        /// - The chunk prompt template content if successfully loaded
        /// - null if the prompt service is unavailable or loading fails
        /// </returns>
        /// <remarks>
        /// This method attempts to load the "chunk_summary_prompt.md" template file from the prompts directory.
        /// The chunk prompt is specifically designed for processing individual text segments before aggregation.
        /// Failures are logged as warnings but do not throw exceptions, allowing the system to fall back to default prompts.
        /// </remarks>
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
        }        
        
        /// <summary>
        /// Loads the final prompt template from the prompt service for summary aggregation.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous load operation. The task result contains:
        /// - The final summary prompt template content if successfully loaded  
        /// - null if the prompt service is unavailable or loading fails
        /// </returns>
        /// <remarks>
        /// This method attempts to load the "final_summary_prompt.md" template file from the prompts directory.
        /// The final prompt is specifically designed for aggregating multiple chunk summaries into a cohesive result.
        /// Failures are logged as warnings but do not throw exceptions, allowing the system to fall back to default prompts.
        /// </remarks>
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
        /// Processes and prepares the prompt template for use in summarization operations.
        /// Handles loading of prompt templates when not already provided and validates input text.
        /// </summary>
        /// <param name="inputText">The input text to be summarized</param>
        /// <param name="prompt">The prompt string that may need processing or loading</param>
        /// <param name="promptFileName">The prompt template filename to load if prompt is empty</param>
        /// <returns>
        /// A task that represents the asynchronous processing operation. The task result contains:
        /// - A tuple with the processed prompt (or null if loading failed) and the input text
        /// - The processed prompt will be loaded from promptFileName if the prompt parameter is empty
        /// </returns>
        /// <remarks>
        /// This method serves as a preparation step for both direct and chunked summarization.
        /// It attempts to load the specified prompt template file when no prompt is provided directly.
        /// Loading failures are logged as warnings but do not prevent the operation from continuing.
        /// </remarks>
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
        /// Uses approximately 4 characters per token as a rough estimate for English text.
        /// </summary>
        /// <param name="text">The text to estimate tokens for</param>
        /// <returns>
        /// The estimated token count based on character length, or 0 if the text is null or whitespace.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is a simplified estimation method that provides reasonable approximations for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>English academic text (typical in MBA coursework)</description></item>
        /// <item><description>Mixed alphanumeric content</description></item>
        /// <item><description>Standard punctuation and formatting</description></item>
        /// </list>
        /// <para>
        /// The 4:1 character-to-token ratio is a conservative estimate that works well for OpenAI models.
        /// Actual token counts may vary based on text complexity, language, and specific tokenizer implementation.
        /// </para>
        /// </remarks>
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
        /// Summarizes text using Microsoft SemanticKernel with the specified prompt for smaller texts that don't require chunking.
        /// </summary>
        /// <param name="inputText">The text content to summarize</param>
        /// <param name="prompt">The prompt to guide the summarization process</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>
        /// A task that represents the asynchronous summarization operation. The task result contains:
        /// - The generated summary text if successful
        /// - null if the operation fails or SemanticKernel is unavailable
        /// - A simulated summary string for testing scenarios
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is used for direct summarization of smaller texts (typically under 12,000 characters).
        /// It creates a semantic function from the provided prompt and processes the entire text in a single operation.
        /// </para>
        /// <para>
        /// The method uses OpenAI prompt execution settings configured for balanced performance:
        /// </para>
        /// <list type="bullet">
        /// <item><description>MaxTokens: 4000 (allowing comprehensive summaries)</description></item>
        /// <item><description>Temperature: 1.0 (balanced creativity and consistency)</description></item>
        /// <item><description>TopP: 1.0 (full vocabulary consideration)</description></item>
        /// </list>
        /// <para>
        /// Falls back to a default summarization prompt if none is provided.
        /// All errors are logged and handled gracefully by returning null.
        /// </para>
        /// </remarks>
        /// <exception cref="Exception">
        /// Various exceptions may be thrown by the underlying SemanticKernel operations, 
        /// all of which are caught, logged, and result in a null return value.
        /// </exception>
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
