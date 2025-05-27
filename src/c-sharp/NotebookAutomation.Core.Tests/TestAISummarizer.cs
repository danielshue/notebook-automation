using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Minimal test double for AISummarizer for use in VideoNoteProcessor tests.
    /// </summary>
    internal class TestAISummarizer : AISummarizer
    {
        public TestAISummarizer()
            : base(
                NullLogger<AISummarizer>.Instance,
                null,
                null,
                null)
        {
        }

        public override Task<string> SummarizeTextAsync(string inputText, string prompt = null, string promptFileName = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("This is an AI summary of the video content.");
        }

        public override Task<string> SummarizeWithVariablesAsync(string inputText, Dictionary<string, string> variables = null, string promptFileName = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("This is an AI summary of the video content.");
        }
    }
}
