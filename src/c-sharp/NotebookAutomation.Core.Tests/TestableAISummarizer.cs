#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// A testable version of AISummarizer that exposes private methods for testing.
    /// </summary>
    public class TestableAISummarizer : AISummarizer
    {
        private string _summarizeAsyncResult = "[Simulated AI summary]";

        /// <summary>
        /// Initializes a new instance of the TestableAISummarizer class.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public TestableAISummarizer(ILogger<AISummarizer> logger)
            : base(
                  logger,
                  new PromptTemplateService(
                      NullLogger<PromptTemplateService>.Instance,
                      new Configuration.AppConfig()),
                  null!,
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
                 /// Override the SummarizeTextAsync method to return the predefined result in tests.
                 /// </summary>
                 /// <param name="content">The content to summarize (ignored in test)</param>
                 /// <param name="prompt">Optional prompt to guide the summary generation</param>
                 /// <param name="promptFileName">Optional prompt file name</param>
                 /// <param name="cancellationToken">Cancellation token</param>
                 /// <returns>The predefined summary result</returns>
        public override Task<string?> SummarizeTextAsync(
            string content,
            string? prompt = null,
            string? promptFileName = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(_summarizeAsyncResult);
        }

        /// <summary>
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
            return Task.FromResult<string?>(_summarizeAsyncResult);
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

        /// <summary>
        /// Exposes the private ContainsMarkdown method for testing.
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>True if text likely contains markdown</returns>
        public bool PublicContainsMarkdown(string text)
        {
            // Use reflection to call the private method
            var methodInfo = typeof(AISummarizer).GetMethod("ContainsMarkdown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (methodInfo == null)
            {
                throw new InvalidOperationException("ContainsMarkdown method not found in AISummarizer");
            }

            var result = methodInfo.Invoke(this, new object[] { text });
            return result != null && (bool)result;
        }
    }
}
