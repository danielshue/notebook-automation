#nullable enable
namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Interface for services that manage and process prompt templates.
    /// </summary>
    public interface IPromptService
    {
        /// <summary>
        /// Loads a template from the configured prompts directory.
        /// </summary>
        /// <param name="templateName">Name of the template to load, without file extension.</param>
        /// <returns>The template content as a string.</returns>
        Task<string> LoadTemplateAsync(string templateName);

        /// <summary>
        /// Substitutes variables in a template string.
        /// </summary>
        /// <param name="template">Template with placeholders in the format {{variable_name}}.</param>
        /// <param name="variables">Dictionary of variables to substitute.</param>
        /// <returns>The template with variables substituted.</returns>
        string SubstituteVariables(string template, Dictionary<string, string>? variables);

        /// <summary>
        /// Gets a prompt with variables substituted.
        /// </summary>
        /// <param name="templateName">Name of the template to load, without file extension.</param>
        /// <param name="variables">Dictionary of variables to substitute.</param>
        /// <returns>The prompt with variables substituted.</returns>
        Task<string> GetPromptAsync(string templateName, Dictionary<string, string>? variables);
    }
}
