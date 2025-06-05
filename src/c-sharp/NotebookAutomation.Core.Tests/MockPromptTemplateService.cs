#nullable enable
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// A mock implementation of PromptTemplateService for testing AISummarizer.
/// </summary>
/// <remarks>
/// This class allows tests to directly control what templates are returned without
/// requiring file system access. It doesn't use the real configuration and doesn't
/// need to be mocked with Moq.
/// </remarks>
public class MockPromptTemplateService : IPromptService
{
    /// <summary>
    /// Gets or sets the template text that will be returned by LoadTemplateAsync.
    /// </summary>
    public string Template { get; set; } = "Default mock template";

    /// <summary>
    /// Gets or sets the expected substitution result that will be returned by SubstituteVariables.
    /// </summary>
    public string? ExpectedSubstitution { get; set; }

    /// <summary>
    /// Gets or sets whether LoadTemplateAsync should throw an exception.
    /// </summary>
    public bool ThrowExceptionOnLoad { get; set; } = false;

    /// <summary>
    /// Gets or sets the exception to throw, if ThrowExceptionOnLoad is true.
    /// </summary>
    public Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// Gets the name of the last template requested.
    /// </summary>
    public string? LastTemplateName { get; private set; }

    /// <summary>
    /// Gets the last variables dictionary passed to SubstituteVariables.
    /// </summary>
    public Dictionary<string, string>? LastVariables { get; private set; }

    /// <summary>
    /// Initializes a new instance of the MockPromptTemplateService class.
    /// </summary>
    public MockPromptTemplateService()
    {
        // No dependencies required in constructor
    }

    /// <summary>
    /// Returns the configured template or throws the configured exception.
    /// </summary>
    /// <param name="templateName">Name of the template to load.</param>
    /// <returns>The configured template text.</returns>
    /// <exception cref="Exception">Throws the configured exception if ThrowExceptionOnLoad is true.</exception>
    public Task<string> LoadTemplateAsync(string templateName)
    {
        LastTemplateName = templateName;

        if (ThrowExceptionOnLoad)
        {
            throw ExceptionToThrow ?? new InvalidOperationException($"Failed to load template {templateName}");
        }

        return Task.FromResult(Template);
    }

    /// <summary>
    /// Substitutes variables in a template string.
    /// </summary>
    /// <param name="template">Template with placeholders.</param>
    /// <param name="variables">Dictionary of variables to substitute.</param>
    /// <returns>The processed template with variables substituted.</returns>
    public string SubstituteVariables(string template, Dictionary<string, string>? variables)
    {
        LastVariables = variables != null ? new Dictionary<string, string>(variables) : null;

        // If ExpectedSubstitution is set, return that directly for testing
        if (!string.IsNullOrEmpty(ExpectedSubstitution))
        {
            return ExpectedSubstitution;
        }

        // Simple placeholder substitution logic
        if (string.IsNullOrEmpty(template) || variables == null)
        {
            return template;
        }

        string result = template;
        foreach (KeyValuePair<string, string> kvp in variables)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }

        return result;
    }        /// <summary>
             /// Gets a prompt with variables substituted.
             /// </summary>
             /// <param name="templateName">Name of the template to load.</param>
             /// <param name="variables">Dictionary of variables to substitute.</param>
             /// <returns>The processed template with variables substituted.</returns>
    public async Task<string> GetPromptAsync(string templateName, Dictionary<string, string>? variables)
    {
        string template = await LoadTemplateAsync(templateName);
        return SubstituteVariables(template, variables);
    }    /// <summary>
         /// Processes template with variables for compatibility with AISummarizer tests.
         /// </summary>
         /// <param name="template">The template string with placeholders.</param>
         /// <param name="variables">Dictionary of variable names and values.</param>
         /// <returns>The template with variables substituted.</returns>
    public Task<string> ProcessTemplateAsync(string template, Dictionary<string, string>? variables) => Task.FromResult(SubstituteVariables(template, variables));

    /// <summary>
    /// Verifies that a specific variable was substituted in the template.
    /// </summary>
    /// <param name="variableName">Name of the variable to check.</param>
    /// <param name="expectedValue">Expected value after substitution.</param>
    /// <returns>True if the variable was substituted with the expected value, false otherwise.</returns>
    /// <remarks>This method is used to verify YAML frontmatter substitution in tests.</remarks>
    public bool VerifySubstitution(string variableName, string expectedValue)
    {
        if (LastVariables != null && LastVariables.TryGetValue(variableName, out var actualValue))
        {
            return actualValue == expectedValue;
        }
        return false;
    }
}
