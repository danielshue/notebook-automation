using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// A simple implementation of ITextGenerationService for testing
    /// </summary>
    public class TestTextGenerationService : ITextGenerationService
    {
        public IReadOnlyDictionary<string, object> Attributes => new Dictionary<string, object>();

        public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
            string prompt,
            PromptExecutionSettings executionSettings = null,

            Kernel kernel = null,
            CancellationToken cancellationToken = default)
        {
            var result = new List<TextContent> { new TextContent("Mock summary") };
            return Task.FromResult<IReadOnlyList<TextContent>>(result);
        }

        public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
            string prompt,
            PromptExecutionSettings executionSettings = null,
            Kernel kernel = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Streaming is not used in this test");
        }

        // This is the method that AISummarizer actually uses
        public Task<TextContent> GetTextContentAsync(
            string prompt,
            OpenAIPromptExecutionSettings settings,
            Kernel kernel = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TextContent("Mock summary"));
        }

        public Task<TextContent> GetTextContentAsync(
            string prompt,
            PromptExecutionSettings settings = null,
            Kernel kernel = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TextContent("Mock summary"));
        }
    }

    /// <summary>
    /// Tests for verifying that prompt logging works correctly in AISummarizer.
    /// </summary>
    [TestClass]
    public class TestSummarizerTests
    {
        private readonly Mock<ILogger<AISummarizer>> _loggerMock;
        private readonly PromptTemplateService _promptTemplateService;
        private readonly AISummarizer _summarizer;

        public TestSummarizerTests()
        {
            _loggerMock = new Mock<ILogger<AISummarizer>>();

            // Set up paths for prompt templates
            string projectDir = Directory.GetCurrentDirectory();
            string promptsPath = Path.GetFullPath(Path.Combine(projectDir, "..", "..", "..", "..", "..", "prompts"));

            // Create AppConfig with paths
            var pathsConfig = new PathsConfig
            {
                PromptsPath = promptsPath
            };

            var appConfig = new AppConfig()
            {
                Paths = pathsConfig
            };

            // Create a real PromptTemplateService with the actual prompts directory
            _promptTemplateService = new PromptTemplateService(
                Mock.Of<ILogger<PromptTemplateService>>(),
                appConfig);

            // Create a test implementation of ITextGenerationService
            var textGenerationService = new TestTextGenerationService();

            // Create AISummarizer with dependencies
            _summarizer = new AISummarizer(
                _loggerMock.Object,
                _promptTemplateService,
                null);
        }
    }
}

