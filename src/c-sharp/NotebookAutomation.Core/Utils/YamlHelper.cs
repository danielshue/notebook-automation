// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Helper class for working with YAML content in markdown files.
/// </summary>
/// <remarks>
/// <para>
/// Provides functionality for parsing, modifying, and serializing YAML frontmatter found in markdown documents, with special consideration for preserving formatting and handling Obsidian-specific conventions.
/// </para>
/// <example>
/// <code>
/// var helper = new YamlHelper(_logger);
/// var frontmatter = helper.ExtractFrontmatter(markdown);
/// var dict = helper.ParseYamlToDictionary(frontmatter);
/// </code>
/// </example>
/// </remarks>
public partial class YamlHelper : IYamlHelper
{
    private readonly ILogger _logger;
    private readonly Regex _frontmatterRegex = FrontmatterBlockRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlHelper"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
    public YamlHelper(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extracts the YAML frontmatter from markdown content.
    /// </summary>
    /// <param name="markdown">The markdown content to parse.</param>
    /// <returns>The YAML frontmatter as a string, or null if not found.</returns>
    /// <remarks>
    /// Returns only the YAML block delimited by <c>---</c> at the start and end of the file.
    /// </remarks>
    /// <example>
    /// <code>
    /// var yaml = helper.ExtractFrontmatter(markdown);
    /// </code>
    /// </example>
    public string? ExtractFrontmatter(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            _logger.LogDebug("Empty markdown content provided for frontmatter extraction");
            return null;
        }

        // Check if the content seems to have frontmatter (starts with ---)
        if (!markdown.TrimStart().StartsWith("---"))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Content does not appear to have frontmatter. First 20 chars: {Content}",
                    markdown.Length > 20 ? markdown[..20] + "..." : markdown);
            }

            return null;
        }

        try
        {
            var match = _frontmatterRegex.Match(markdown);

            if (!match.Success)
            {                // If regex did not match but content starts with ---, we might have a malformed frontmatter
                // Log more details for debugging
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Content appears to have frontmatter but didn't match regex pattern. First 50 chars: {Content}",
                        markdown.Length > 50 ? markdown[..50] + "..." : markdown);
                }

                return null;
            }

            return match.Groups[1].Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting frontmatter from markdown");
            return null;
        }
    }

    /// <summary>
    /// Parses YAML frontmatter to a dynamic object.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <returns>The parsed object, or null if parsing failed.</returns>
    /// <remarks>
    /// Uses YamlDotNet for deserialization.
    /// </remarks>
    public object? ParseYaml(string yaml)
    {
        if (string.IsNullOrEmpty(yaml))
        {
            return null;
        }

        try
        {
            var deserializer = new DeserializerBuilder()

                // Use the default naming convention to preserve original key names
                .Build();

            return deserializer.Deserialize(yaml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse YAML content");
            return null;
        }
    }

    /// <summary>
    /// Parses YAML frontmatter to a dictionary.
    /// </summary>
    /// <param name="yaml">The YAML content to parse.</param>
    /// <returns>The parsed dictionary, or an empty dictionary if parsing failed.</returns>
    /// <remarks>
    /// Handles common formatting issues and code block wrappers.
    /// </remarks>
    public Dictionary<string, object> ParseYamlToDictionary(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            _logger.LogDebug("Empty YAML content provided for parsing");
            return [];
        }

        try
        {
            // Trim the input to remove any leading/trailing whitespace that might cause issues
            yaml = yaml.Trim();                // Handle markdown code blocks that the AI sometimes generates, with various patterns
            if (yaml.Contains("```yaml") || yaml.Contains("```yml"))
            {
                _logger.LogWarning("Detected nested YAML code block in frontmatter - attempting to fix");

                // Extract the actual YAML content from inside the code block
                var startIndex = yaml.Contains("```yaml")
                    ? yaml.IndexOf("```yaml") + 7
                    : yaml.IndexOf("```yml") + 6;
                var endIndex = yaml.LastIndexOf("```");

                if (startIndex > 6 && endIndex > startIndex)
                {
                    // Extract the content between the code block markers
                    yaml = yaml[startIndex..endIndex].Trim();
                    _logger.LogInformation("Extracted YAML content from nested code block");
                }
            }

            // Also handle plain markdown code blocks
            else if (yaml.Contains("```"))
            {
                _logger.LogWarning("Detected generic code block in frontmatter - attempting to fix");
                var startIndex = yaml.IndexOf("```") + 3;

                // Check if there's a newline after the opening ```
                if (yaml.Length > startIndex && yaml[startIndex] == '\n')
                {
                    startIndex++; // Skip the newline
                }
                else if (yaml.Length > startIndex && yaml[startIndex] == '\r' && yaml.Length > startIndex + 1 && yaml[startIndex + 1] == '\n')
                {
                    startIndex += 2; // Skip CRLF
                }

                var endIndex = yaml.LastIndexOf("```");

                if (startIndex > 3 && endIndex > startIndex)
                {
                    yaml = yaml[startIndex..endIndex].Trim();
                    _logger.LogInformation("Extracted content from generic code block");
                }
            }

            // Fix common YAML formatting issues from AI responses
            // Remove trailing spaces at end of each line (common in markdown formatting)
            yaml = string.Join(
                "\n",
                yaml.Split('\n')
                    .Select(line => line.TrimEnd()));

            // Simple validation of YAML structure
            if (!yaml.Contains(':') && !yaml.Contains('-'))
            {
                _logger.LogWarning("YAML content does not contain any key-value pairs or lists");
                return [];
            }

            var deserializer = new DeserializerBuilder()

                // Use the default naming convention to preserve original key names
                .IgnoreUnmatchedProperties() // More forgiving parsing
                .Build();

            var result = deserializer.Deserialize<Dictionary<string, object>>(yaml);
            return result ?? [];
        }
        catch (YamlDotNet.Core.YamlException yamlEx)
        {
            // More specific logging for YAML syntax errors
            _logger.LogError(yamlEx, "YAML syntax error: {ErrorMessage} at Line {Line}, Column {Column}",
                yamlEx.Message, yamlEx.Start.Line, yamlEx.Start.Column);

            // Log the problematic content for debugging
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Problematic YAML content (first 100 chars): {YamlContent}",
                    yaml.Length > 100 ? yaml[..100] + "..." : yaml);
            }

            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse YAML content to dictionary");

            // Debug log the content for easier troubleshooting
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Failed YAML content (first 100 chars): {YamlContent}",
                    yaml.Length > 100 ? yaml[..100] + "..." : yaml);
            }

            return [];
        }
    }

    /// <summary>
    /// Updates existing markdown content with new frontmatter.
    /// </summary>
    /// <param name="markdown">The original markdown content.</param>
    /// <param name="frontmatter">The new frontmatter as a dictionary.</param>
    /// <returns>The updated markdown content.</returns>
    /// <remarks>
    /// Replaces or inserts the YAML frontmatter block at the top of the markdown file.
    /// </remarks>
    public string UpdateFrontmatter(string markdown, Dictionary<string, object> frontmatter)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return CreateMarkdownWithFrontmatter(frontmatter);
        }

        var serializer = new SerializerBuilder()

            // Use the default naming convention to preserve original key names
            .Build();

        var yamlString = serializer.Serialize(frontmatter);

        // Add just one set of --- separators with proper newlines
        var newFrontmatter = $"---\n{yamlString}---\n\n";

        if (_frontmatterRegex.IsMatch(markdown))
        {
            return _frontmatterRegex.Replace(markdown, newFrontmatter);
        }
        else
        {
            return $"{newFrontmatter}{markdown}";
        }
    }

    /// <summary>
    /// Creates new markdown content with the specified frontmatter.
    /// </summary>
    /// <param name="frontmatter">The frontmatter as a dictionary.</param>
    /// <returns>The created markdown content.</returns>
    /// <remarks>
    /// Produces a markdown string with only the YAML frontmatter block.
    /// </remarks>
    public static string CreateMarkdownWithFrontmatter(Dictionary<string, object> frontmatter)
    {
        var serializer = new SerializerBuilder()

            // Use the default naming convention to preserve original key names
            .Build();

        var yamlString = serializer.Serialize(frontmatter);

        // Add just one set of --- separators with proper newlines
        return $"---\n{yamlString}---\n\n";
    }

    /// <summary>
    /// Removes YAML frontmatter from markdown content if present.
    /// </summary>
    /// <param name="markdown">The markdown content to clean.</param>
    /// <returns>The content without frontmatter.</returns>
    public string RemoveFrontmatter(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return markdown ?? string.Empty;
        }

        var match = _frontmatterRegex.Match(markdown);
        if (!match.Success)
        {
            return markdown;
        }

        // Return the content after the frontmatter block
        return markdown[match.Length..].TrimStart();
    }

    /// <summary>
    /// Parses and extracts tags from frontmatter.
    /// </summary>
    /// <param name="frontmatter">The frontmatter as a dictionary.</param>
    /// <returns>A set of tags found in the frontmatter.</returns>
    /// <remarks>
    /// Handles both string and array formats for tags.
    /// </remarks>
    public static HashSet<string> ExtractTags(Dictionary<string, object> frontmatter)
    {
        var result = new HashSet<string>();

        if (frontmatter.TryGetValue("tags", out object? tags))
        {
            // Handle different formats of tags
            if (tags is List<object> tagList)
            {
                foreach (var tag in tagList)
                {
                    result.Add(tag.ToString() ?? string.Empty);
                }
            }
            else if (tags is string tagString)
            {
                // Split by commas and trim whitespace
                foreach (var tag in tagString.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    result.Add(tag.Trim());
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Updates frontmatter with new tags.
    /// </summary>
    /// <param name="frontmatter">The frontmatter dictionary to update.</param>
    /// <param name="tags">The set of tags to set.</param>
    /// <returns>The updated frontmatter.</returns>
    public static Dictionary<string, object> UpdateTags(Dictionary<string, object> frontmatter, HashSet<string> tags)
    {
        if (tags.Count > 0)
        {
            // Store tags as string array for YAML format consistency
            frontmatter["tags"] = tags.ToArray();
        }
        else
        {
            frontmatter.Remove("tags");
        }

        return frontmatter;
    }

    /// <summary>
    /// Checks if the auto-generated-state in frontmatter indicates the content is read-only.
    /// </summary>
    /// <param name="frontmatter">The frontmatter dictionary to check.</param>
    /// <returns>True if the auto-generated-state is "read-only" or "readonly", false otherwise.</returns>
    public bool IsAutoGeneratedStateReadOnly(Dictionary<string, object> frontmatter)
    {
        if (frontmatter.TryGetValue("auto-generated-state", out var state))
        {
            var stateStr = state?.ToString()?.ToLowerInvariant();
            return stateStr == "read-only" || stateStr == "readonly";
        }

        return false;
    }

    /// <summary>
    /// Checks if a markdown file has readonly auto-generated-state in its frontmatter.
    /// </summary>
    /// <param name="filePath">Path to the markdown file to check.</param>
    /// <returns>True if the file has readonly auto-generated-state, false otherwise.</returns>
    public bool IsFileReadOnly(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var frontmatter = LoadFrontmatterFromFile(filePath);
            return IsAutoGeneratedStateReadOnly(frontmatter);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithPath(ex, "Failed to check if file is read-only: {filePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Loads frontmatter from a markdown file.
    /// </summary>
    /// <param name="filePath">Path to the markdown file.</param>
    /// <returns>The parsed frontmatter as a dictionary, or an empty dictionary on error.</returns>
    public Dictionary<string, object> LoadFrontmatterFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarningWithPath("File not found: {filePath}", filePath);
                return [];
            }

            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var frontmatter = ExtractFrontmatter(content);

            if (frontmatter == null)
            {
                return [];
            }

            return ParseYamlToDictionary(frontmatter);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithPath(ex, "Failed to load frontmatter from file: {filePath}", filePath);
            return [];
        }
    }

    /// <summary>
    /// Saves markdown with updated frontmatter to a file.
    /// </summary>
    /// <param name="filePath">Path to save the file.</param>
    /// <param name="markdown">The markdown content.</param>
    /// <param name="frontmatter">The frontmatter to update or add.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool SaveMarkdownWithFrontmatter(string filePath, string markdown, Dictionary<string, object> frontmatter)
    {
        try
        {
            var updatedContent = UpdateFrontmatter(markdown, frontmatter);
            File.WriteAllText(filePath, updatedContent, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithPath(ex, "Failed to save markdown with updated frontmatter: {filePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Serializes a dictionary to YAML.
    /// </summary>
    /// <param name="data">The dictionary to serialize.</param>
    /// <returns>The serialized YAML string.</returns>
    public static string SerializeYaml(Dictionary<string, object> data)
    {
        var serializer = new SerializerBuilder()

            // Use the default naming convention to preserve original key names
            .DisableAliases()
            .Build();

        return serializer.Serialize(data);
    }    /// <summary>
         /// Serializes a dictionary to YAML format.
         /// </summary>
         /// <param name="data">The dictionary to serialize.</param>
         /// <returns>The serialized YAML string.</returns>
    public string SerializeToYaml(Dictionary<string, object> data)
    {
        var serializer = new SerializerBuilder()
            .DisableAliases()
            .Build();
        var yaml = serializer.Serialize(data);

        return yaml;
    }

    /// <summary>
    /// Replaces the frontmatter in a markdown document with new frontmatter.
    /// </summary>
    /// <param name="markdown">The markdown content.</param>
    /// <param name="newFrontmatter">The new frontmatter as a dictionary.</param>
    /// <returns>The updated markdown content.</returns>
    public string ReplaceFrontmatter(string markdown, Dictionary<string, object> newFrontmatter)
    {
        var yamlContent = SerializeToYaml(newFrontmatter);
        return ReplaceWithYamlContent(markdown, yamlContent);
    }

    /// <summary>
    /// Replaces the frontmatter in a markdown document with raw YAML content.
    /// </summary>
    /// <param name="content">The original markdown content.</param>
    /// <param name="newFrontmatterYaml">The new frontmatter YAML content.</param>
    /// <returns>The updated markdown content.</returns>
    private string ReplaceWithYamlContent(string content, string newFrontmatterYaml)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var match = _frontmatterRegex.Match(content);
        if (!match.Success)
        {
            // If no frontmatter, add it to the beginning
            return $"---\n{newFrontmatterYaml}\n---\n\n{content}";
        }

        // Replace the existing frontmatter
        return _frontmatterRegex.Replace(content, $"---\n{newFrontmatterYaml}\n---\n");
    }

    /// <summary>
    /// Diagnoses YAML parsing issues and returns detailed information about any problems found.
    /// </summary>
    /// <param name="markdown">The markdown content to diagnose.</param>
    /// <returns>A diagnostic result containing information about YAML parsing issues.</returns>
    /// <remarks>
    /// Returns a tuple indicating success, a message, and the parsed data (if successful).
    /// </remarks>
    public (bool Success, string Message, Dictionary<string, object>? Data) DiagnoseYamlFrontmatter(string markdown)
    {
        try
        {
            // Step 1: Check for empty content
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return (false, "Markdown content is empty or whitespace only", null);
            }

            // Step 2: Extract frontmatter
            string? frontmatter = ExtractFrontmatter(markdown);
            if (frontmatter == null)
            {
                // Check if it might be malformed frontmatter
                var firstLines = string.Join("\n", markdown.Split('\n').Take(5));
                if (firstLines.Contains("---"))
                {
                    return (false, $"Frontmatter not properly formatted. First few lines: {firstLines}", null);
                }

                return (false, "No frontmatter found in the markdown content", null);
            }

            // Step 3: Validate frontmatter
            if (string.IsNullOrWhiteSpace(frontmatter))
            {
                return (false, "Extracted frontmatter is empty", null);
            }

            // Step 4: Check frontmatter structure
            if (!frontmatter.Contains(':'))
            {
                return (false, $"Frontmatter lacks key-value pairs: {frontmatter}", null);
            }

            // Step 5: Try parsing
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var result = deserializer.Deserialize<Dictionary<string, object>>(frontmatter);

                if (result == null || result.Count == 0)
                {
                    return (false, "YAML parsed successfully but resulted in empty dictionary", null);
                }

                return (true, $"Successfully parsed YAML frontmatter with {result.Count} keys", result);
            }
            catch (YamlDotNet.Core.YamlException yamlEx)
            {
                return (false, $"YAML syntax error: {yamlEx.Message} at Line {yamlEx.Start.Line}, Column {yamlEx.Start.Column}", null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error during YAML diagnosis: {ex.Message}", null);
        }
    }
    /// <summary>
    /// Gets a compiled regex that matches YAML frontmatter blocks at the start of a markdown file.
    /// </summary>
    /// <remarks>
    /// Matches a block delimited by '---' at the start and end, capturing the YAML content in between.
    /// Used for extracting and manipulating YAML frontmatter in markdown documents.
    /// </remarks>
    /// <returns>A compiled <see cref="Regex"/> for matching YAML frontmatter blocks.</returns>
    [GeneratedRegex(@"^\s*---\s*[\r\n]+(.+?)[\r\n]+\s*---\s*[\r\n]+", RegexOptions.Compiled | RegexOptions.Singleline)]
    internal static partial Regex FrontmatterBlockRegex();
}
