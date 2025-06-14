// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Services;

/// <summary>
/// Service for loading prompt templates and performing variable substitution.
/// Handles different template types (e.g., chunk summary, final summary) and supports dynamic prompt file loading.
/// </summary>
/// <remarks>
/// <para>
/// This service provides functionality for:
/// <list type="bullet">
/// <item><description>Loading prompt templates from a configured directory</description></item>
/// <item><description>Substituting variables in templates</description></item>
/// <item><description>Handling default templates for chunk and final summaries</description></item>
/// <item><description>Dynamic initialization of the prompts directory</description></item>
/// </list>
/// </para>
/// <para>
/// The service integrates with application configuration to determine the prompts directory and provides
/// fallback mechanisms to locate the directory in common project structures.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var promptService = new PromptTemplateService(logger, config);
/// var template = await promptService.LoadTemplateAsync("welcome_message");
/// var prompt = promptService.SubstituteVariables(template, new Dictionary&lt;string, string&gt; { { "name", "John" } });
/// Console.WriteLine(prompt);
/// </code>
/// </example>
public partial class PromptTemplateService : IPromptService
{
    private readonly ILogger<PromptTemplateService> logger;
    private readonly IYamlHelper yamlHelper;
    private string promptsDirectory = string.Empty;    // Default templates to use as fallbacks if files are not found
    public static string DefaultChunkPrompt { get; } =
        "You are an educational content summarizer for MBA course materials. Generate a clear and insightful summary of the following chunk from the content \"{{$onedrivePath}}\", part of the course \"{{$course}}\"\n\n{{$content}}";

    public static string DefaultFinalPrompt { get; } = "You are an educational content summarizer for MBA course materials. Your task is to synthesize multiple AI-generated summaries of content into a single, cohesive summary. You will receive YAML frontmatter below as placeholder that contains existing metadata - DO NOT modify this existing frontmatter structure except for tags.\n\n{{$input}}";

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptTemplateService"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="yamlHelper">The YAML helper for processing frontmatter.</param>
    /// <param name="config">The application configuration.</param>
    /// <remarks>
    /// <para>
    /// This constructor initializes the service and sets up the prompts directory using the provided configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var promptService = new PromptTemplateService(logger, _yamlHelper, config);
    /// </code>
    /// </example>
    public PromptTemplateService(ILogger<PromptTemplateService> logger, IYamlHelper yamlHelper, Configuration.AppConfig config)
    {
        this.logger = logger;
        this.yamlHelper = yamlHelper;
        InitializePromptsDirectory(config);
    }

    /// <summary>
    /// Initializes the prompts directory using the configured path or searching in common locations.
    /// </summary>
    /// <param name="config">Optional application configuration.</param>
    /// <remarks>
    /// <para>
    /// This method attempts to locate the prompts directory using the following strategies:
    /// <list type="number">
    /// <item><description>Configured path from application settings</description></item>
    /// <item><description>Output directory of the application</description></item>
    /// <item><description>Core project directory</description></item>
    /// <item><description>Repository root directory</description></item>
    /// <item><description>Parent repository root directory</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If none of these locations contain the prompts directory, a warning is logged.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// promptService.InitializePromptsDirectory(config);
    /// </code>
    /// </example>
    private void InitializePromptsDirectory(Configuration.AppConfig config)
    {
        // First try to get the prompts directory from configuration if provided
        if (config != null && !string.IsNullOrEmpty(config.Paths.PromptsPath))
        {
            string configPromptsDir = config.Paths.PromptsPath;

            if (Directory.Exists(configPromptsDir))
            {
                promptsDirectory = configPromptsDir;
                logger.LogInformation($"Using prompts directory from config: {promptsDirectory}");
                return;
            }
            else
            {
                logger.LogWarning($"Configured prompts directory not found: {configPromptsDir}");
            }
        }

        // Find the path to the prompts directory
        string baseDirectory = AppContext.BaseDirectory;        // Try to find the prompts in the project structure

        // First look in the output directory
        string projectPromptsDir = Path.Combine(baseDirectory, "Prompts");

        if (Directory.Exists(projectPromptsDir))
        {
            promptsDirectory = projectPromptsDir;

            logger.LogInformation($"Using prompts directory from output directory: {promptsDirectory}");

            return;
        }

        // Try in the Core project directory
        string coreProjectDir = Path.GetFullPath(Path.Combine(baseDirectory, "..\\..\\.."));
        string corePromptsDir = Path.Combine(coreProjectDir, "Prompts");

        if (Directory.Exists(corePromptsDir))
        {
            promptsDirectory = corePromptsDir;
            logger.LogInformation($"Using prompts directory from Core project: {promptsDirectory}");
            return;
        }

        // Try to find the repository root
        string repoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../../.."));
        string rootPromptsDir = Path.Combine(repoRoot, "prompts");

        if (Directory.Exists(rootPromptsDir))
        {
            promptsDirectory = rootPromptsDir;
            logger.LogInformation($"Using prompts directory from repository root: {promptsDirectory}");
            return;
        }

        // Try one level higher in the repo structure
        string parentRepoRoot = Path.GetFullPath(Path.Combine(baseDirectory, "../../../../../.."));
        string parentRootPromptsDir = Path.Combine(parentRepoRoot, "prompts");

        if (Directory.Exists(parentRootPromptsDir))
        {
            promptsDirectory = parentRootPromptsDir;
            logger.LogInformation($"Using prompts directory from parent repository root: {promptsDirectory}");
            return;
        }

        // If all else fails, use the current directory
        promptsDirectory = baseDirectory;
        logger.LogWarning($"Could not find prompts directory. Using base directory: {baseDirectory}");
    }

    /// <summary>
    /// Gets the path to the prompts directory.
    /// </summary>
    public string PromptsDirectory => promptsDirectory;

    /// <summary>
    /// Loads a prompt template and substitutes variables.
    /// </summary>
    /// <param name="templatePath">Path to the prompt template file.</param>
    /// <param name="variables">Dictionary of variable names and values.</param>
    /// <returns>Prompt string with variables substituted.</returns>
    public virtual async Task<string> LoadAndSubstituteAsync(string templatePath, Dictionary<string, string> substitutionValues)
    {
        if (!File.Exists(templatePath))
        {
            logger.LogError($"Prompt template not found: {templatePath}");
            return string.Empty;
        }

        string template = await File.ReadAllTextAsync(templatePath).ConfigureAwait(false);
        string result = SubstituteVariables(template, substitutionValues);
        return result;
    }

    /// <summary>
    /// Substitutes variables in a template string.
    /// </summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="variables">Dictionary of variable names and values.</param>
    /// <returns>The template with variables substituted.</returns>
    public string SubstituteVariables(string template, Dictionary<string, string>? substitutionValues)
    {
        if (substitutionValues == null || string.IsNullOrEmpty(template))
        {
            return template;
        }

        return TemplateVariableRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return substitutionValues.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    /// <summary>
    /// Gets a prompt with variables substituted.
    /// </summary>
    /// <param name="templateName">Name of the template to load, without file extension.</param>
    /// <param name="variables">Dictionary of variables to substitute.</param>
    /// <returns>The prompt with variables substituted.</returns>
    public async Task<string> GetPromptAsync(string templateName, Dictionary<string, string>? substitutionValues)
    {
        string template = await LoadTemplateAsync(templateName).ConfigureAwait(false);
        return SubstituteVariables(template, substitutionValues);
    }

    /// <summary>
    /// Loads a template by name from the prompts directory.
    /// </summary>
    /// <param name="templateName">Name of the prompt template (e.g., "chunk_summary_prompt").</param>
    /// <returns>The template content, or a default template if not found.</returns>
    public virtual async Task<string> LoadTemplateAsync(string templateName)
    {
        string templatePath = Path.Combine(promptsDirectory, $"{templateName}.md");
        try
        {
            if (File.Exists(templatePath))
            {
                string content = await File.ReadAllTextAsync(templatePath).ConfigureAwait(false);

                // Strip frontmatter if present
                content = yamlHelper.RemoveFrontmatter(content);
                logger.LogInformation($"Loaded template '{templateName}' from: {templatePath}");
                return content;
            } // Look in project Prompts directory too

            string projectPromptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", $"{templateName}.md");
            if (File.Exists(projectPromptPath))
            {
                string content = await File.ReadAllTextAsync(projectPromptPath).ConfigureAwait(false);

                // Strip frontmatter if present
                content = yamlHelper.RemoveFrontmatter(content);
                logger.LogInformation($"Loaded template '{templateName}' from project: {projectPromptPath}");
                return content;
            }

            return GetDefaultTemplate(templateName);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading template '{templateName}': {ex.Message}");
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
        logger.LogWarning($"Using default template for: {templateName}");

        return templateName switch
        {
            "chunk_summary_prompt" => DefaultChunkPrompt,
            "final_summary_prompt" => DefaultFinalPrompt,
            _ => DefaultFinalPrompt,
        };
    }

    /// <summary>
    /// Processes template with variables asynchronously.
    /// </summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="variables">Dictionary of variable names and values.</param>
    /// <returns>The template with variables substituted.</returns>
    public Task<string> ProcessTemplateAsync(string template, Dictionary<string, string>? substitutionValues)
    {
        return Task.FromResult(SubstituteVariables(template, substitutionValues));
    }    /// <summary>
         /// Generates a regular expression to match template variables enclosed in double curly braces.
         /// </summary>
         /// <returns>
         /// A <see cref="Regex"/> instance that matches placeholders in the format {{variable_name}}.
         /// </returns>
         /// <remarks>
         /// This method uses the <see cref="GeneratedRegexAttribute"/> to define a compile-time constant regex.
         /// </remarks>
    [GeneratedRegex("{{(.*?)}}")]
    internal static partial Regex TemplateVariableRegex();
}
