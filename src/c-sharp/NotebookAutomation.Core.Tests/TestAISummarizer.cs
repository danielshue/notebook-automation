using Microsoft.Extensions.Logging.Abstractions;

using NotebookAutomation.Core.Services;

#nullable enable

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// Minimal test double for AISummarizer for use in VideoNoteProcessor tests.
/// </summary>
internal class TestAISummarizer : AISummarizer
{
    public TestAISummarizer()
        : base(
            NullLogger<AISummarizer>.Instance,
            null,
            null)
    {
    }
    public override Task<string?> SummarizeWithVariablesAsync(string inputText, Dictionary<string, string>? variables = null, string? promptFileName = null, CancellationToken cancellationToken = default) => Task.FromResult<string?>("This is an AI summary of the video content.");
}

