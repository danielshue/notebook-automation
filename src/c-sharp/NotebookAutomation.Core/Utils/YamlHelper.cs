using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Helper class for working with YAML content in markdown files.
    /// 
    /// This class provides functionality for parsing, modifying, and serializing YAML
    /// frontmatter found in markdown documents, with special consideration for
    /// preserving formatting and handling Obsidian-specific conventions.
    /// </summary>
    public class YamlHelper
    {
        private readonly ILogger? _logger;
        private readonly Regex _frontmatterRegex;
        
        /// <summary>
        /// Initializes a new instance of the YamlHelper class.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostics.</param>
        public YamlHelper(ILogger? logger = null)
        {
            _logger = logger;
            _frontmatterRegex = new Regex(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline);
        }
        
        /// <summary>
        /// Extracts the YAML frontmatter from markdown content.
        /// </summary>
        /// <param name="markdown">The markdown content to parse.</param>
        /// <returns>The YAML frontmatter as a string, or null if not found.</returns>
        public string? ExtractFrontmatter(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return null;
            }
            
            var match = _frontmatterRegex.Match(markdown);
            return match.Success ? match.Groups[1].Value : null;
        }
        
        /// <summary>
        /// Parses YAML frontmatter to a dynamic object.
        /// </summary>
        /// <param name="yaml">The YAML content to parse.</param>
        /// <returns>The parsed object, or null if parsing failed.</returns>
        public object? ParseYaml(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return null;
            }
            
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                    
                return deserializer.Deserialize(yaml);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse YAML content");
                return null;
            }
        }
        
        /// <summary>
        /// Parses YAML frontmatter to a dictionary.
        /// </summary>
        /// <param name="yaml">The YAML content to parse.</param>
        /// <returns>The parsed dictionary, or an empty dictionary if parsing failed.</returns>
        public Dictionary<string, object> ParseYamlToDictionary(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return new Dictionary<string, object>();
            }
            
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                    
                return deserializer.Deserialize<Dictionary<string, object>>(yaml);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse YAML content to dictionary");
                return new Dictionary<string, object>();
            }
        }
        
        /// <summary>
        /// Updates existing markdown content with new frontmatter.
        /// </summary>
        /// <param name="markdown">The original markdown content.</param>
        /// <param name="frontmatter">The new frontmatter as a dictionary.</param>
        /// <returns>The updated markdown content.</returns>
        public string UpdateFrontmatter(string markdown, Dictionary<string, object> frontmatter)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return CreateMarkdownWithFrontmatter(frontmatter);
            }
            
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
                
            var yamlString = serializer.Serialize(frontmatter);
            var newFrontmatter = $"---\n{yamlString}---\n";
            
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
        public string CreateMarkdownWithFrontmatter(Dictionary<string, object> frontmatter)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
                
            var yamlString = serializer.Serialize(frontmatter);
            return $"---\n{yamlString}---\n\n";
        }
        
        /// <summary>
        /// Parses and extracts tags from frontmatter.
        /// </summary>
        /// <param name="frontmatter">The frontmatter as a dictionary.</param>
        /// <returns>A set of tags found in the frontmatter.</returns>
        public HashSet<string> ExtractTags(Dictionary<string, object> frontmatter)
        {
            var result = new HashSet<string>();
            
            if (frontmatter.ContainsKey("tags"))
            {
                var tags = frontmatter["tags"];
                
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
        public Dictionary<string, object> UpdateTags(Dictionary<string, object> frontmatter, HashSet<string> tags)
        {
            if (tags.Count > 0)
            {
                var tagList = new List<string>(tags);
                frontmatter["tags"] = tagList;
            }
            else if (frontmatter.ContainsKey("tags"))
            {
                frontmatter.Remove("tags");
            }
            
            return frontmatter;
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
                    _logger?.LogWarning("File not found: {FilePath}", filePath);
                    return new Dictionary<string, object>();
                }
                
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                var frontmatter = ExtractFrontmatter(content);
                
                if (frontmatter == null)
                {
                    return new Dictionary<string, object>();
                }
                
                return ParseYamlToDictionary(frontmatter);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load frontmatter from file: {FilePath}", filePath);
                return new Dictionary<string, object>();
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
                _logger?.LogError(ex, "Failed to save markdown with updated frontmatter: {FilePath}", filePath);
                return false;
            }
        }
        
        /// <summary>
        /// Serializes a dictionary to YAML.
        /// </summary>
        /// <param name="data">The dictionary to serialize.</param>
        /// <returns>The serialized YAML string.</returns>
        public string SerializeYaml(Dictionary<string, object> data)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .DisableAliases()
                .Build();
                
            return serializer.Serialize(data);
        }
        
        /// <summary>
        /// Replaces the frontmatter in a markdown document.
        /// </summary>
        /// <param name="content">The original markdown content.</param>
        /// <param name="newFrontmatter">The new frontmatter to insert.</param>
        /// <returns>The updated markdown content.</returns>
        public string ReplaceFrontmatter(string content, string newFrontmatter)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }
            
            var match = _frontmatterRegex.Match(content);
            if (!match.Success)
            {
                // If no frontmatter, add it to the beginning
                return $"---\n{newFrontmatter}\n---\n\n{content}";
            }
            
            // Replace the existing frontmatter
            return _frontmatterRegex.Replace(content, $"---\n{newFrontmatter}\n---\n");
        }
    }
}
