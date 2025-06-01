#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{

    /// <summary>
    /// A testable version of AISummarizer that exposes private methods for testing.
    /// </summary>
    public class TestableAISummarizer : AISummarizer
    {
        private string _summarizeAsyncResult = "[Simulated AI summary]";        /// <summary>
                                                                                /// Initializes a new instance of the TestableAISummarizer class.
                                                                                /// </summary>
                                                                                /// <param name="logger">The logger instance</param>
        public TestableAISummarizer(ILogger<AISummarizer> logger) : base(logger,
                  new PromptTemplateService(
                      NullLogger<PromptTemplateService>.Instance,
                      new Configuration.AppConfig()),
                  null!)
        {
        }

        /// <summary>
        /// Sets up a predefined result to be returned by SummarizeAsync method.
        /// </summary>
        /// <param name="result">The result string to return from SummarizeAsync</param>
        public void SetupSummarizeAsyncResult(string result)
        {
            _summarizeAsyncResult = result;
        }        /// <summary>
                 /// Override the SummarizeWithVariablesAsync method to return the predefined result in tests.
                 /// </summary>
                 /// <param name="inputText">The text to summarize (ignored in test)</param>
                 /// <param name="variables">Optional variables to substitute in the prompt template</param>
                 /// <param name="promptFileName">Optional prompt file name</param>
                 /// <param name="cancellationToken">Cancellation token</param>
                 /// <returns>The predefined summary result</returns>
        public override Task<string?> SummarizeWithVariablesAsync(
            string inputText,
            System.Collections.Generic.Dictionary<string, string>? variables = null,
            string? promptFileName = null,
            CancellationToken cancellationToken = default)
        {
            // Return the configured test result, optionally including some variable information
            var result = _summarizeAsyncResult;
            if (variables != null && variables.Count > 0)
            {
                // Append variable info to show substitution worked
                result += $" [Variables: {string.Join(", ", variables.Select(kvp => $"{kvp.Key}={kvp.Value}"))}]";
            }
            return Task.FromResult<string?>(result);
        }

        /// <summary>
        /// Exposes the private EstimateTokenCount method for testing.
        /// </summary>
        /// <param name="text">Text to estimate token count for</param>
        /// <returns>Estimated token count</returns>
        public int PublicEstimateTokenCount(string text)
        {
            // Use reflection to call the private method
            var methodInfo = typeof(AISummarizer).GetMethod("EstimateTokenCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (methodInfo == null)
            {
                throw new InvalidOperationException("EstimateTokenCount method not found in AISummarizer");
            }

            var result = methodInfo.Invoke(this, new object[] { text });
            return result != null ? (int)result : 0;
        }

    }
}


