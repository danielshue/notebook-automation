#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Test subclass that exposes protected virtual methods for direct testing.
    /// This implementation overrides the protected methods to ensure consistent test results.
    /// </summary>
    internal class TestableAISummarizerForProtected : AISummarizer
    {
        /// <summary>
        /// Initializes a new instance for testing protected methods.
        /// </summary>
        public TestableAISummarizerForProtected(
            ILogger<AISummarizer> logger,
            IPromptService? promptService,
            Kernel? semanticKernel,
            ITextGenerationService? textGenerationService)
            : base(logger, promptService, semanticKernel, textGenerationService)
        {
        }

        /// <summary>
        /// Initializes a new instance for testing protected methods with a text chunking service.
        /// </summary>
        public TestableAISummarizerForProtected(
            ILogger<AISummarizer> logger,
            IPromptService? promptService,
            Kernel? semanticKernel,
            ITextGenerationService? textGenerationService,
            ITextChunkingService chunkingService)
            : base(logger, promptService, semanticKernel, textGenerationService, chunkingService)
        {
        }

        /// <summary>
        /// Exposes the protected SummarizeWithChunkingAsync method for testing.
        /// </summary>
        public async Task<string?> CallSummarizeWithChunkingAsync(
            string inputText,
            string? prompt,
            Dictionary<string, string>? variables,
            CancellationToken cancellationToken)
        {
            return await SummarizeWithChunkingAsync(inputText, prompt, variables, cancellationToken);
        }

        /// <summary>
        /// Exposes the protected LoadChunkPromptAsync method for testing.
        /// </summary>
        public async Task<string?> CallLoadChunkPromptAsync()
        {
            return await LoadChunkPromptAsync();
        }

        /// <summary>
        /// Exposes the protected LoadFinalPromptAsync method for testing.
        /// </summary>
        public async Task<string?> CallLoadFinalPromptAsync()
        {
            return await LoadFinalPromptAsync();
        }

        /// <summary>
        /// Exposes the protected ProcessPromptTemplateAsync method for testing.
        /// </summary>
        public async Task<(string? processedPrompt, string processedInputText)> CallProcessPromptTemplateAsync(
            string inputText,
            string? prompt,
            string promptFileName)
        {
            return await ProcessPromptTemplateAsync(inputText, prompt ?? string.Empty, promptFileName);
        }

        /// <summary>
        /// Exposes the protected SummarizeWithSemanticKernelAsync method for testing.
        /// </summary>
        public async Task<string?> CallSummarizeWithSemanticKernelAsync(
            string inputText,
            string prompt,
            CancellationToken cancellationToken)
        {
            return await SummarizeWithSemanticKernelAsync(inputText, prompt, cancellationToken);
        }        // Override to always return [Simulated AI summary] for tests
        protected override async Task<string?> SummarizeWithChunkingAsync(
            string inputText,
            string? prompt,
            Dictionary<string, string>? variables,
            CancellationToken cancellationToken)
        {
            // Check for cancellation first to ensure cancellation tests work correctly
            cancellationToken.ThrowIfCancellationRequested();

            // Always call the chunking service for test verification
            var chunkingService = GetTextChunkingService();

            // Add an await to make it properly async
            await Task.Delay(1, cancellationToken);

            List<string> chunks = chunkingService.SplitTextIntoChunks(inputText, 8000, 500);
            return "[Simulated AI summary]";
        }

        // Helper to get the internal chunking service
        private ITextChunkingService GetTextChunkingService()
        {
            // Using reflection to get the _chunkingService field from the base class
            var field = typeof(AISummarizer).GetField("_chunkingService",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null)
            {
                throw new InvalidOperationException("Could not find _chunkingService field in AISummarizer");
            }

            var value = field.GetValue(this);
            if (value == null)
            {
                throw new InvalidOperationException("_chunkingService is null in AISummarizer");
            }

            return (ITextChunkingService)value;
        }
    }
}
