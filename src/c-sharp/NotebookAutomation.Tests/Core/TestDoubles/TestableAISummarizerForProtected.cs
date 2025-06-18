// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Tests.Core.TestDoubles;

namespace NotebookAutomation.Tests.Core.TestDoubles;

/// <summary>
/// Test subclass that exposes protected virtual methods for direct testing.
/// This implementation overrides the protected methods to ensure consistent test results.
/// </summary>

internal class TestableAISummarizerForProtected : AISummarizer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestableAISummarizerForProtected"/> class.
    /// Initializes a new instance for testing protected methods.
    /// </summary>

    public TestableAISummarizerForProtected(
        ILogger<AISummarizer> logger,
        IPromptService? promptService,
        Kernel? semanticKernel)
        : base(logger, promptService, semanticKernel)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestableAISummarizerForProtected"/> class.
    /// Initializes a new instance for testing protected methods with a custom ITextChunkingService.
    /// </summary>

    public TestableAISummarizerForProtected(
        ILogger<AISummarizer> logger,
        IPromptService? promptService,
        Kernel? semanticKernel,
        ITextChunkingService textChunkingService)
        : base(logger, promptService, semanticKernel, textChunkingService)
    {
    }

    /// <summary>
    /// Exposes the protected SummarizeWithChunkingAsync method for testing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<string?> CallSummarizeWithChunkingAsync(
        string inputText,
        string? prompt,
        Dictionary<string, string>? variables,
        CancellationToken cancellationToken) => await SummarizeWithChunkingAsync(inputText, prompt, variables, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Exposes the protected LoadChunkPromptAsync method for testing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<string?> CallLoadChunkPromptAsync() => await LoadChunkPromptAsync().ConfigureAwait(false);

    /// <summary>
    /// Exposes the protected LoadFinalPromptAsync method for testing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<string?> CallLoadFinalPromptAsync() => await LoadFinalPromptAsync().ConfigureAwait(false);

    /// <summary>
    /// Exposes the protected ProcessPromptTemplateAsync method for testing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<(string? processedPrompt, string processedInputText)> CallProcessPromptTemplateAsync(
        string inputText,
        string? prompt,
        string promptFileName) => await ProcessPromptTemplateAsync(inputText, prompt ?? string.Empty, promptFileName).ConfigureAwait(false);

    /// <summary>
    /// Exposes the protected SummarizeWithSemanticKernelAsync method for testing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<string?> CallSummarizeWithSemanticKernelAsync(
        string inputText, string prompt,
        CancellationToken cancellationToken) => await SummarizeWithSemanticKernelAsync(inputText, prompt, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Override to always return [Simulated AI summary] for tests
    /// </summary>
    internal override async Task<string?> SummarizeWithChunkingAsync(
        string inputText,
        string? prompt,
        Dictionary<string, string>? variables,
        CancellationToken cancellationToken)
    {
        // Check for cancellation first to ensure cancellation tests work correctly
        cancellationToken.ThrowIfCancellationRequested();

        // Always call the chunking service for test verification
        ITextChunkingService chunkingService = GetTextChunkingService();

        // Add an await to make it properly async
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        _ = chunkingService.SplitTextIntoChunks(inputText, 8000, 500);
        return "[Simulated AI summary]";
    }

    // Helper to get the internal chunking service
    private ITextChunkingService GetTextChunkingService()
    {
        // Using reflection to get the _chunkingService field from the base class
        System.Reflection.FieldInfo field = typeof(AISummarizer).GetField(
            "_chunkingService",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ?? throw new InvalidOperationException("Could not find _chunkingService field in AISummarizer");
        object value = field.GetValue(this) ?? throw new InvalidOperationException("_chunkingService is null in AISummarizer");
        return (ITextChunkingService)value;
    }
}
