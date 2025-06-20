// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Tests.Core.TestDoubles;

namespace NotebookAutomation.Tests.Core.TestDoubles;

/// <summary>
/// A test implementation of PromptTemplateService that provides controlled templates for testing.
/// </summary>

internal class TestPromptTemplateService : PromptTemplateService
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
    /// Initializes a new instance of the <see _cref="TestPromptTemplateService"/> class.    ///.</summary>
    public TestPromptTemplateService()
        : base(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PromptTemplateService>.Instance,
            new YamlHelper(Microsoft.Extensions.Logging.Abstractions.NullLogger<YamlHelper>.Instance),
            new AppConfig())
    {
    }

    /// <summary>
    /// Returns the configured template.
    /// </summary>
    /// <returns>A <see _cref="Task"/> representing the asynchronous operation.</returns>
    public override Task<string> LoadTemplateAsync(string templateName)
    {
        LastTemplateName = templateName;
        return Task.FromResult(Template ?? $"Default test template for {templateName}");
    }

    /// <summary>
    /// Returns the configured substitution result or applies the default substitution.
    /// </summary>
    /// <returns></returns>
    public new string SubstituteVariables(string template, Dictionary<string, string>? variables)
    {
        if (ExpectedSubstitution != null)
        {
            return ExpectedSubstitution;
        }

        // Simple implementation for tests
        string result = template;
        if (variables != null)
        {
            foreach (KeyValuePair<string, string> kvp in variables)
            {
                result = result.Replace("{{" + kvp.Key + "}}", kvp.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// Processes template with variables for tests.
    /// </summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="variables">Dictionary of variable names and values.</param>
    /// <returns>The template with variables substituted.</returns>
    public new Task<string> ProcessTemplateAsync(string template, Dictionary<string, string>? variables) => Task.FromResult(SubstituteVariables(template, variables ?? []));
}
