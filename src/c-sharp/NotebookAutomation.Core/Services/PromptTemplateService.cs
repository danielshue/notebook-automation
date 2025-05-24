// Module: PromptTemplateService.cs
// Provides advanced prompt template loading and variable substitution for AI summarization.
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Loads prompt templates and performs variable substitution.
    /// Handles different template types (chunk summary, final summary) and
    /// supports dynamic prompt file loading from prompts directory.
    /// </summary>    
    public class PromptTemplateService
    {
        private readonly ILogger _logger;
        private string _promptsDirectory = string.Empty;

        // Default templates to use as fallbacks if files are not found
        private const string DefaultChunkPrompt = @"You are an expert academic summarizer. Summarize the following content for a study note, focusing on key concepts, main arguments, and actionable insights. Use clear, concise language suitable for graduate-level students.

Content:
{{content}}";

        private const string DefaultFinalPrompt = @"You are an expert academic summarizer. Write a comprehensive summary for the following material, synthesizing the main points, arguments, and conclusions. Highlight the most important takeaways and any recommended actions or next steps.

Content:
{{content}}";

        private const string DefaultVideoFinalPrompt = @"You are an educational content summarizer for video materials. Create a comprehensive final summary structured in markdown format with the following sections:

# 🎓 Video Summary (AI Generated)

## 🧩 Topics Covered
- List 3-5 main topics covered in the video
- Be specific and use bullet points

## 📝 Key Concepts Explained
- Explain the key concepts in 3-5 paragraphs
- Focus on the most important ideas

## ⭐ Important Takeaways
- List 3-5 important takeaways as bullet points
- Focus on practical applications and insights

## 🧠 Summary
- Write a concise 1-paragraph summary of the overall video content

## 💬 Notable Quotes / Insights
- Include 1-2 significant quotes or key insights from the video
- Format as proper markdown blockquotes using '>' symbol

## ❓ Questions
- What did I learn from this video?
- What's still unclear or needs further exploration?

Content:
{{content}}";

        /// <summary>
        /// Initializes a new instance of the PromptTemplateService class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        public PromptTemplateService(ILogger logger)
        {
            _logger = logger;
            InitializePromptsDirectory();
        }

        /// <summary>
        /// Initializes a new instance of the PromptTemplateService class with config.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="config">The application configuration.</param>
        public PromptTemplateService(ILogger logger, Core.Configuration.AppConfig config)
        {
            _logger = logger;
            InitializePromptsDirectory(config);
        }

        /// <summary>
        /// Initializes the prompts directory using the configured path or searching in common locations.
        /// </summary>
        /// <param name="config">Optional application configuration.</param>
        private void InitializePromptsDirectory(Core.Configuration.AppConfig? config = null)
        {
            // First try to get the prompts directory from configuration if provided
            if (config != null && !string.IsNullOrEmpty(config.Paths.PromptsPath))
            {
                string configPromptsDir = config.Paths.PromptsPath;
                if (Directory.Exists(configPromptsDir))
                {
                    _promptsDirectory = configPromptsDir;
                    _logger.LogInformation("Using prompts directory from config: {PromptsDirectory}", _promptsDirectory);
                    return;
                }
                else
                {
                    _logger.LogWarning("Configured prompts directory not found: {PromptsDirectory}", configPromptsDir);
                }
            }

            // Find the path to the prompts directory
            string baseDirectory = AppContext.BaseDirectory;

            // Try to find the prompts in the project structure
            // First look in the output directory
            string projectPromptsDir = Path.Combine(baseDirectory, "Prompts");

            if (Directory.Exists(projectPromptsDir))
            {
                _promptsDirectory = projectPromptsDir;
                _logger.LogInformation("Using prompts directory from output directory: {PromptsDirectory}", _promptsDirectory);
                return;
            }

            // Try in the Core project directory
            string coreProjectDir = Path.GetFullPath(Path.Combine(baseDirectory, "..\\..\\.."));
            string corePromptsDir = Path.Combine(coreProjectDir, "Prompts");

            if (Directory.Exists(corePromptsDir))
            {
                _promptsDirectory = corePromptsDir;
                _logger.LogInformation("Using prompts directory from Core project: {PromptsDirectory}", _promptsDirectory);
                return;
            }

            // Try to find the repository root 
            string repoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../../.."));
            string rootPromptsDir = Path.Combine(repoRoot, "Prompts");

            if (Directory.Exists(rootPromptsDir))
            {
                _promptsDirectory = rootPromptsDir;
                _logger.LogInformation("Using prompts directory from repository root: {PromptsDirectory}", _promptsDirectory);
                return;
            }

            // Try one level higher in the repo structure
            string parentRepoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../../../.."));
            string parentRootPromptsDir = Path.Combine(parentRepoRoot, "Prompts");

            if (Directory.Exists(parentRootPromptsDir))
            {
                _promptsDirectory = parentRootPromptsDir;
                _logger.LogInformation("Using prompts directory from parent repository root: {PromptsDirectory}", _promptsDirectory);
                return;
            }

            // If all else fails, use the current directory
            _promptsDirectory = baseDirectory;
            _logger.LogWarning("Could not find prompts directory. Using base directory: {BaseDirectory}", baseDirectory);
        }

        /// <summary>
        /// Gets the path to the prompts directory.
        /// </summary>
        public string PromptsDirectory => _promptsDirectory;

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
            string result = SubstituteVariables(template, variables);
            return result;
        }

        /// <summary>
        /// Substitutes variables in a template string.
        /// </summary>
        /// <param name="template">The template string with placeholders.</param>
        /// <param name="variables">Dictionary of variable names and values.</param>
        /// <returns>The template with variables substituted.</returns>
        public string SubstituteVariables(string template, Dictionary<string, string> variables)
        {
            return Regex.Replace(template, "{{(.*?)}}", match =>
            {
                var key = match.Groups[1].Value.Trim();
                return variables.TryGetValue(key, out var value) ? value : match.Value;
            });
        }

        /// <summary>
        /// Loads a template by name from the prompts directory.
        /// </summary>
        /// <param name="templateName">Name of the prompt template (e.g., "chunk_summary_prompt").</param>
        /// <returns>The template content, or a default template if not found.</returns>
        public async Task<string> LoadTemplateAsync(string templateName)
        {
            string templatePath = Path.Combine(_promptsDirectory, $"{templateName}.md");

            try
            {
                if (File.Exists(templatePath))
                {
                    string content = await File.ReadAllTextAsync(templatePath);
                    _logger.LogInformation("Loaded template: {TemplateName}", templateName);
                    return content;
                }

                // Look in project Prompts directory too
                string projectPromptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", $"{templateName}.md");
                if (File.Exists(projectPromptPath))
                {
                    string content = await File.ReadAllTextAsync(projectPromptPath);
                    _logger.LogInformation("Loaded template from project: {TemplateName}", templateName);
                    return content;
                }

                return GetDefaultTemplate(templateName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template: {TemplateName}", templateName);
                return GetDefaultTemplate(templateName);
            }
        }

        /// <summary>
        /// Gets a default template based on the template name.
        /// </summary>
        /// <param name="templateName">Name of the template.</param>
        /// <returns>The default template content.</returns>
        private string GetDefaultTemplate(string templateName)
        {
            _logger.LogWarning("Using default template for: {TemplateName}", templateName);

            return templateName switch
            {
                "chunk_summary_prompt" => DefaultChunkPrompt,
                "final_summary_prompt" => DefaultFinalPrompt,
                "final_summary_prompt_video" => DefaultVideoFinalPrompt,
                _ => DefaultFinalPrompt
            };
        }
    }
}
