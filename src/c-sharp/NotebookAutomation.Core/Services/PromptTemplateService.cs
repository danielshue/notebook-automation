// Module: PromptTemplateService.cs
// Provides advanced prompt variable substitution for AI summarization.
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Loads prompt templates and performs variable substitution.
    /// </summary>
    public class PromptTemplateService
    {
        private readonly ILogger _logger;
        public PromptTemplateService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads a prompt template and substitutes variables.
        /// </summary>
        /// <param name="templatePath">Path to the prompt template file.</param>
        /// <param name="variables">Dictionary of variable names and values.</param>
        /// <returns>Prompt string with variables substituted.</returns>
        public async Task<string> LoadAndSubstituteAsync(string templatePath, Dictionary<string, string> variables)
        {
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Prompt template not found: {TemplatePath}", templatePath);
                return string.Empty;
            }
            string template = await File.ReadAllTextAsync(templatePath);
            string result = Regex.Replace(template, "{{(.*?)}}", match =>
            {
                var key = match.Groups[1].Value.Trim();
                return variables.TryGetValue(key, out var value) ? value : match.Value;
            });
            return result;
        }
    }
}
