using NotebookAutomation.Core.Configuration;
// Enable nullable reference types for this file
#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// A test implementation of PromptTemplateService that provides controlled templates for testing.
    /// </summary>
    public class TestPromptTemplateService : PromptTemplateService
    {
        /// <summary>
        /// Gets or sets the template text that will be returned by LoadTemplateAsync.
        /// </summary>
        public string? Template { get; set; }

        /// <summary>
        /// Gets or sets the expected substitution result that will be returned by SubstituteVariables.
        /// </summary>
        public string? ExpectedSubstitution { get; set; }

        /// <summary>
        /// Gets the name of the last template requested.
        /// </summary>
        public string? LastTemplateName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the TestPromptTemplateService class.
        /// </summary>
        public TestPromptTemplateService()
            : base(Microsoft.Extensions.Logging.Abstractions.NullLogger<PromptTemplateService>.Instance, new AppConfig())
        {
        }

        /// <summary>
        /// Returns the configured template.
        /// </summary>
        public override Task<string> LoadTemplateAsync(string templateName)
        {
            LastTemplateName = templateName;
            return Task.FromResult(Template ?? $"Default test template for {templateName}");
        }

        /// <summary>
        /// Returns the configured substitution result or applies the default substitution.
        /// </summary>
        public new string SubstituteVariables(string template, Dictionary<string, string> variables)
        {
            if (ExpectedSubstitution != null)
            {
                return ExpectedSubstitution;
            }
            // Simple implementation for tests
            string result = template;
            foreach (var kvp in variables)
            {
                result = result.Replace("{{" + kvp.Key + "}}", kvp.Value);
            }
            return result;
        }
    }
}
