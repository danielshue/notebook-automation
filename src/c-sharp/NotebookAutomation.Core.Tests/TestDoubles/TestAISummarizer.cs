// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.TestDoubles;

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