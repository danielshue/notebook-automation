using System;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;
using NotebookAutomation.Core.Services;

#nullable enable

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// A testable version of AISummarizer that exposes private methods for testing.
    /// </summary>
    public class TestableAISummarizer : AISummarizer
    {        
        /// <summary>
        /// Initializes a new instance of the TestableAISummarizer class.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="promptService">Optional prompt template service for loading templates</param>
        /// <param name="semanticKernel">Optional Microsoft.SemanticKernel kernel</param>
        /// <param name="textGenerationService">Optional Microsoft.SemanticKernel text generation service</param>
        public TestableAISummarizer(
            ILogger logger,
            PromptTemplateService? promptService = null,
            Kernel? semanticKernel = null,
            ITextGenerationService? textGenerationService = null)
            : base(logger, promptService, semanticKernel, textGenerationService)
        {
        }

        /// <summary>
        /// Backward compatibility constructor for TestableAISummarizer class.
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="apiKey">OpenAI API key (no longer used with DI)</param>
        /// <param name="model">OpenAI model to use (no longer used with DI)</param>
        /// <param name="promptService">Optional prompt template service for loading templates</param>
        /// <param name="semanticKernel">Optional Microsoft.SemanticKernel kernel</param>
        /// <param name="textGenerationService">Optional Microsoft.SemanticKernel text generation service</param>
        public TestableAISummarizer(
            ILogger logger, 
            string apiKey, 
            string model = "gpt-4.1", 
            PromptTemplateService? promptService = null,
            Kernel? semanticKernel = null,
            ITextGenerationService? textGenerationService = null)
            : base(logger, promptService, semanticKernel, textGenerationService)
        {
            // API Key and model are now configured via DI, so we ignore them here
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
