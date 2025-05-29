// Enable nullable reference types for this file
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Test double for PromptTemplateService for unit testing.
    /// </summary>
    public class TestPromptTemplateService : PromptTemplateService
    {
        public string? Template { get; set; }
        public string? ExpectedSubstitution { get; set; }
        public string? LastTemplateName { get; set; }
        public TestPromptTemplateService() : base(Microsoft.Extensions.Logging.Abstractions.NullLogger<PromptTemplateService>.Instance, new Configuration.AppConfig()) { }
        public override Task<string> LoadTemplateAsync(string templateName)
        {
            LastTemplateName = templateName;
            if (templateName == "chunk_summary_prompt")
                return Task.FromResult(PromptTemplateService.DefaultChunkPrompt);
            if (templateName == "final_summary_prompt")
                return Task.FromResult(PromptTemplateService.DefaultFinalPrompt);
            return Task.FromResult(Template ?? "");
        }
        public new string SubstituteVariables(string template, Dictionary<string, string> variables)
            => ExpectedSubstitution ?? template;
    }

    /// <summary>
    /// Fake implementation of ITextGenerationService for unit testing.
    /// </summary>
    public class FakeTextGenerationService : ITextGenerationService
    {
        public string? ExpectedPrompt { get; set; }
        public string? Response { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public Queue<string>? Responses { get; set; }

        // Fix the nullability issue
        public IReadOnlyDictionary<string, object?> Attributes => null!;        // For ITextGenerationService (plural) contract
        public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? settings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow != null) throw ExceptionToThrow;

            string responseText;
            if (Responses != null && Responses.Count > 0)
            {
                responseText = Responses.Dequeue();
            }
            else if (ExpectedPrompt != null && prompt == ExpectedPrompt && Response != null)
            {
                responseText = Response;
            }
            else
            {
                responseText = Response ?? string.Empty;
            }

            var textContent = new TextContent(responseText);
            return Task.FromResult<IReadOnlyList<TextContent>>(new List<TextContent> { textContent });
        }

        // For ITextGenerationService (streaming) contract
        public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? settings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task<TextContent> GetTextContentAsync(string prompt, OpenAIPromptExecutionSettings settings, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            if (Responses != null && Responses.Count > 0)
                return Task.FromResult(new TextContent(Responses.Dequeue()));
            if (ExpectedPrompt != null && prompt == ExpectedPrompt && Response != null)
                return Task.FromResult(new TextContent(Response));
            return Task.FromResult(new TextContent(Response ?? ""));
        }
    }
}
