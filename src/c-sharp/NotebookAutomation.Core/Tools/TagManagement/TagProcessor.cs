using System.Text;
using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.TagManagement
{
    /// <summary>
    /// Processes tags in markdown files and handles tag management operations.
    /// 
    /// This class provides functionality for:
    /// - Adding nested tags based on frontmatter fields
    /// - Consolidating tags across files
    /// - Restructuring tag hierarchies
    /// - Cleaning tags from index files
    /// - Restructuring tags for consistency    /// - Adding example tags for demonstration/testing
    /// - Checking and enforcing metadata consistency
    /// </summary>
    public class TagProcessor
    {
        private readonly ILogger<TagProcessor> _logger;
        private readonly ILogger _failedLogger;
        private readonly IYamlHelper _yamlHelper;
        private readonly bool _dryRun;
        private readonly bool _verbose;
        private readonly HashSet<string> _fieldsToProcess;

        /// <summary>
        /// Statistics about the processing performed.
        /// </summary>
        public Dictionary<string, int> Stats { get; private set; } = new Dictionary<string, int>
        {
            { "FilesProcessed", 0 },
            { "FilesModified", 0 },
            { "TagsAdded", 0 },
            { "IndexFilesCleared", 0 },
            { "FilesWithErrors", 0 }
        };        /// <summary>
                  /// Initializes a new instance of the TagProcessor.
                  /// </summary>
                  /// <param name="logger">Logger for general diagnostics.</param>
                  /// <param name="failedLogger">Logger for recording failed operations.</param>
                  /// <param name="yamlHelper">Helper for YAML processing.</param>
                  /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
                  /// <param name="verbose">Whether to provide verbose output.</param>
        public TagProcessor(
            ILogger<TagProcessor> logger,
            ILogger failedLogger,
            IYamlHelper yamlHelper,
            bool dryRun = false,
            bool verbose = false)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _failedLogger = failedLogger ?? throw new ArgumentNullException(nameof(failedLogger));
            _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
            _dryRun = dryRun;
            _verbose = verbose;

            // Fields to process for tags (generalized)
            _fieldsToProcess =
            [
                "course", "lecture", "topic", "subjects", "professor",
                "university", "program", "assignment", "type", "author"
            ];
        }

        /// <summary>
        /// Initializes a new instance of the TagProcessor with specified fields to process.
        /// </summary>
        /// <param name="logger">Logger for general diagnostics.</param>
        /// <param name="failedLogger">Logger for recording failed operations.</param>
        /// <param name="yamlHelper">Helper for YAML processing.</param>
        /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
        /// <param name="verbose">Whether to provide verbose output.</param>
        /// <param name="fieldsToProcess">Specific frontmatter fields to process for tag generation.</param>
        public TagProcessor(
            ILogger<TagProcessor> logger,
            ILogger failedLogger,
            IYamlHelper yamlHelper,
            bool dryRun = false,
            bool verbose = false,
            HashSet<string>? fieldsToProcess = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _failedLogger = failedLogger ?? throw new ArgumentNullException(nameof(failedLogger));
            _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
            _dryRun = dryRun;
            _verbose = verbose;

            // Use provided fields if any, otherwise use defaults
            _fieldsToProcess = fieldsToProcess ??
            [
                "course", "lecture", "topic", "subjects", "professor",
                "university", "program", "assignment", "type", "author"
            ];
        }

        /// <summary>
        /// Processes a directory recursively to add or update nested tags.
        /// </summary>
        /// <param name="directory">The directory path to process.</param>
        /// <returns>Dictionary with processing statistics.</returns>
        public async Task<Dictionary<string, int>> ProcessDirectoryAsync(string directory)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogError("Directory not found: {Directory}", directory);
                return Stats;
            }

            _logger.LogInformation("Processing directory: {Directory}", directory);

            try
            {
                var markdownFiles = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories);

                if (_verbose)
                {
                    _logger.LogInformation("Found {Count} markdown files to process", markdownFiles.Length);
                }

                foreach (var file in markdownFiles)
                {
                    await ProcessFileAsync(file);
                }

                _logger.LogInformation(
                    "Processing complete: {FilesProcessed} files processed, {FilesModified} files modified, " +
                    "{TagsAdded} tags added, {IndexFilesCleared} index files cleared, {FilesWithErrors} files with errors",
                    Stats["FilesProcessed"], Stats["FilesModified"], Stats["TagsAdded"],
                    Stats["IndexFilesCleared"], Stats["FilesWithErrors"]);

                return Stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing directory: {Directory}", directory);
                _failedLogger.LogError("Failed to process directory {Directory}: {Error}", directory, ex.Message);
                Stats["FilesWithErrors"]++;
                return Stats;
            }
        }

        /// <summary>
        /// Processes a single markdown file to add or update nested tags.
        /// </summary>
        /// <param name="filePath">Path to the markdown file.</param>
        /// <returns>True if the file was modified, false otherwise.</returns>
        public async Task<bool> ProcessFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _failedLogger.LogError("File not found: {FilePath}", filePath);
                Stats["FilesWithErrors"]++;
                return false;
            }

            try
            {
                Stats["FilesProcessed"]++;

                if (_verbose)
                {
                    _logger.LogInformation("Processing file: {FilePath}", filePath);
                }

                // Skip index files if configured to do so
                if (Path.GetFileName(filePath).StartsWith("_index") || Path.GetFileName(filePath).Equals("index.md"))
                {
                    if (_verbose)
                    {
                        _logger.LogInformation("Skipping index file: {FilePath}", filePath);
                    }
                    return false;
                }                // Read the file content
                string content = await File.ReadAllTextAsync(filePath);

                // Extract frontmatter and parse YAML with better error handling
                string? frontmatter = _yamlHelper.ExtractFrontmatter(content);
                if (string.IsNullOrEmpty(frontmatter))
                {
                    if (_verbose)
                    {
                        _logger.LogInformation("No frontmatter found in file: {FilePath}", filePath);

                        // Additional debug info if the file appears to have frontmatter but extraction failed
                        if (content.TrimStart().StartsWith("---"))
                        {
                            _logger.LogDebug("Content appears to have frontmatter but extraction failed. First 50 chars: {Content}",
                                content.Length > 50 ? content[..50] + "..." : content);
                        }
                    }
                    return false;
                }

                // Parse the frontmatter into a dictionary with enhanced error handling
                var frontmatterDict = _yamlHelper.ParseYamlToDictionary(frontmatter);
                if (frontmatterDict.Count == 0)
                {
                    if (_verbose)
                    {
                        _logger.LogInformation("Empty or invalid frontmatter in file: {FilePath}", filePath);
                        _logger.LogDebug("Frontmatter content that failed parsing: {Frontmatter}",
                            frontmatter.Length > 100 ? frontmatter[..100] + "..." : frontmatter);
                    }
                    return false;
                }

                // Process tags
                bool modified = false;
                var existingTags = GetExistingTags(frontmatterDict);
                var newTags = GenerateNestedTags(frontmatterDict, existingTags);

                if (newTags.Count > 0)
                {
                    // Update the tags in the frontmatter
                    var updatedTags = existingTags.Concat(newTags).Distinct().OrderBy(t => t).ToList();
                    frontmatterDict["tags"] = updatedTags;

                    // Update the content with the new frontmatter
                    string updatedFrontmatter = YamlHelper.SerializeYaml(frontmatterDict);
                    var updatedFrontmatterDict = _yamlHelper.ParseYamlToDictionary(updatedFrontmatter);
                    string updatedContent = _yamlHelper.ReplaceFrontmatter(content, updatedFrontmatterDict);

                    if (_dryRun)
                    {
                        if (_verbose)
                        {
                            _logger.LogInformation("[DRY RUN] Would add {Count} tags to {FilePath}", newTags.Count, filePath);
                            foreach (var tag in newTags)
                            {
                                _logger.LogInformation("[DRY RUN] New tag: {Tag}", tag);
                            }
                        }
                    }
                    else
                    {
                        // Write the updated content back to the file
                        await File.WriteAllTextAsync(filePath, updatedContent);
                        Stats["FilesModified"]++;
                        Stats["TagsAdded"] += newTags.Count;
                        modified = true;

                        if (_verbose)
                        {
                            _logger.LogInformation("Added {Count} tags to {FilePath}", newTags.Count, filePath);
                            foreach (var tag in newTags)
                            {
                                _logger.LogInformation("Added tag: {Tag}", tag);
                            }
                        }
                    }
                }
                else if (_verbose)
                {
                    _logger.LogInformation("No new tags to add for {FilePath}", filePath);
                }

                return modified;
            }
            catch (Exception ex)
            {
                _failedLogger.LogError(ex, "Error processing file: {FilePath}", filePath);
                Stats["FilesWithErrors"]++;
                return false;
            }
        }

        /// <summary>
        /// Clears tags from an index file.
        /// </summary>
        /// <param name="filePath">Path to the markdown file.</param>
        /// <param name="frontmatter">The parsed frontmatter dictionary.</param>
        /// <param name="content">The full content of the file.</param>
        /// <returns>True if the file was modified, false otherwise.</returns>
        public async Task<bool> ClearTagsFromFileAsync(
            string filePath,
            Dictionary<string, object> frontmatter,
            string content)
        {
            if (!frontmatter.ContainsKey("tags") &&
                !frontmatter.Any(kv => kv.Key.Equals("tags", StringComparison.InvariantCultureIgnoreCase)))
            {
                // No tags to clear
                return false;
            }

            // Remove tags
            frontmatter.Remove("tags");

            // Check for tags with different case
            var tagsKey = frontmatter.Keys
                .FirstOrDefault(k => k.Equals("tags", StringComparison.InvariantCultureIgnoreCase));

            if (tagsKey != null)
            {
                frontmatter.Remove(tagsKey);
            }

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would clear tags from index file: {FilePath}", filePath);
                Stats["IndexFilesCleared"]++;
                return true;
            }

            var updatedContent = _yamlHelper.UpdateFrontmatter(content, frontmatter);
            await File.WriteAllTextAsync(filePath, updatedContent, Encoding.UTF8);

            _logger.LogInformation("Cleared tags from index file: {FilePath}", filePath);
            Stats["IndexFilesCleared"]++;
            Stats["FilesModified"]++;

            return true;
        }

        /// <summary>
        /// Adds nested tags based on frontmatter fields to a file.
        /// </summary>
        /// <param name="filePath">Path to the markdown file.</param>
        /// <param name="frontmatter">The parsed frontmatter dictionary.</param>
        /// <param name="content">The full content of the file.</param>
        /// <returns>True if the file was modified, false otherwise.</returns>
        public async Task<bool> AddNestedTagsToFileAsync(
            string filePath,
            Dictionary<string, object> frontmatter,
            string content)
        {
            // Extract existing tags
            var existingTags = YamlHelper.ExtractTags(frontmatter);
            var initialTagCount = existingTags.Count;

            // Add new nested tags based on frontmatter fields
            foreach (var field in _fieldsToProcess)
            {
                if (frontmatter.TryGetValue(field, out object? value) && value != null)
                {
                    string valueStr = value.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(valueStr))
                    {
                        var nestedTag = $"#{field}/{NormalizeTagValue(valueStr)}";
                        if (!existingTags.Contains(nestedTag))
                        {
                            existingTags.Add(nestedTag);
                            if (_verbose)
                            {
                                _logger.LogInformation("Adding tag: {Tag} to {FilePath}", nestedTag, filePath);
                            }
                            Stats["TagsAdded"]++;
                        }
                    }
                }
            }

            // Check if we actually added any new tags
            if (existingTags.Count == initialTagCount)
            {
                return false;
            }

            // Update frontmatter with new tags
            frontmatter = YamlHelper.UpdateTags(frontmatter, existingTags);

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would update tags in file: {FilePath}", filePath);
                return true;
            }

            var updatedContent = _yamlHelper.UpdateFrontmatter(content, frontmatter);
            await File.WriteAllTextAsync(filePath, updatedContent, Encoding.UTF8);

            _logger.LogInformation("Updated tags in file: {FilePath}", filePath);
            Stats["FilesModified"]++;

            return true;
        }

        /// <summary>
        /// Extracts existing tags from the frontmatter.
        /// </summary>
        /// <param name="frontmatter">The frontmatter dictionary.</param>
        /// <returns>A list of existing tags.</returns>
        public static List<string> GetExistingTags(Dictionary<string, object> frontmatter)
        {
            var result = new List<string>();

            // Look for tags key
            if (frontmatter.TryGetValue("tags", out var tagsObj))
            {
                // Handle different formats of tags
                if (tagsObj is List<object> tagsList)
                {
                    // Handle tags as a list
                    foreach (var tag in tagsList)
                    {
                        if (tag != null)
                        {
                            result.Add(tag.ToString() ?? string.Empty);
                        }
                    }
                }
                else if (tagsObj is string tagsStr)
                {
                    // Handle tags as a comma-separated string
                    var tags = tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var tag in tags)
                    {
                        result.Add(tag.Trim());
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Generates nested tags based on frontmatter fields.
        /// </summary>
        /// <param name="frontmatter">The frontmatter dictionary.</param>
        /// <param name="existingTags">List of existing tags.</param>
        /// <returns>A list of new tags to add.</returns>
        public List<string> GenerateNestedTags(Dictionary<string, object> frontmatter, List<string> existingTags)
        {
            var newTags = new List<string>();

            // Create a HashSet of existing tags for quicker lookups
            var existingTagsSet = new HashSet<string>(existingTags);

            // Process each field we're interested in
            foreach (var fieldName in _fieldsToProcess)
            {
                if (frontmatter.TryGetValue(fieldName, out var fieldValue) && fieldValue != null)
                {
                    string prefix = GetTagPrefixForField(fieldName);

                    if (fieldValue is List<object> valueList)
                    {
                        // Process list-type fields
                        foreach (var val in valueList)
                        {
                            if (val != null)
                            {
                                string tagValue = NormalizeTagValue(val?.ToString() ?? string.Empty);
                                string tag = $"{prefix}/{tagValue}";

                                if (!existingTagsSet.Contains(tag))
                                {
                                    newTags.Add(tag);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Process single-value fields
                        string tagValue = NormalizeTagValue(fieldValue?.ToString() ?? string.Empty);
                        string tag = $"{prefix}/{tagValue}";

                        if (!existingTagsSet.Contains(tag))
                        {
                            newTags.Add(tag);
                        }
                    }
                }
            }

            return newTags;
        }

        /// <summary>
        /// Gets the appropriate tag prefix for a frontmatter field.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The tag prefix.</returns>
        public static string GetTagPrefixForField(string fieldName)
        {
            // Map field names to tag prefixes
            return fieldName switch
            {
                "course" => "mba/course",
                "lecture" => "mba/lecture",
                "topic" => "mba/topic",
                "subjects" => "subject",
                "professor" => "people",
                "university" => "institution",
                "program" => "program",
                "assignment" => "assignment",
                "type" => "content-type",
                "author" => "author",
                _ => fieldName  // Default to using the field name itself
            };
        }

        /// <summary>
        /// Normalizes a tag value for consistent formatting.
        /// </summary>
        /// <param name="value">The raw tag value.</param>
        /// <returns>Normalized tag value.</returns>
        public static string NormalizeTagValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Replace spaces with hyphens, trim, and convert to lowercase
            return value
                .Replace(" ", "-")
                .Replace(".", "-")
                .Replace("_", "-")
                .Replace(",", "")
                .Replace(":", "")
                .Replace(";", "")
                .Replace("\"", "")
                .Replace("'", "")
                .Trim()
                .ToLowerInvariant();
        }

        /// <summary>
        /// Restructures tags in all markdown files in a directory for consistency (lowercase, dashes, etc.).
        /// </summary>
        public async Task<Dictionary<string, int>> RestructureTagsInDirectoryAsync(string directory)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogError("Directory not found: {Directory}", directory);
                return Stats;
            }
            var markdownFiles = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories);
            foreach (var file in markdownFiles)
            {
                await RestructureTagsInFileAsync(file);
            }
            return Stats;
        }

        /// <summary>
        /// Normalizes and restructures tags in a single markdown file.
        /// </summary>
        public async Task<bool> RestructureTagsInFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            string content = await File.ReadAllTextAsync(filePath);
            string? frontmatter = _yamlHelper.ExtractFrontmatter(content);
            if (string.IsNullOrEmpty(frontmatter)) return false;
            var frontmatterDict = _yamlHelper.ParseYamlToDictionary(frontmatter);
            if (!frontmatterDict.ContainsKey("tags")) return false;
            var tags = GetExistingTags(frontmatterDict);
            var normalizedTags = tags.Select(NormalizeTagValue).Distinct().OrderBy(t => t).ToList();
            frontmatterDict["tags"] = normalizedTags;
            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would restructure tags in {FilePath}", filePath);
                return true;
            }
            var updatedFrontmatter = YamlHelper.SerializeYaml(frontmatterDict);
            var updatedFrontmatterDict = _yamlHelper.ParseYamlToDictionary(updatedFrontmatter);
            string updatedContent = _yamlHelper.ReplaceFrontmatter(content, updatedFrontmatterDict);
            await File.WriteAllTextAsync(filePath, updatedContent);
            _logger.LogInformation("Restructured tags in {FilePath}", filePath);
            Stats["FilesModified"]++;
            return true;
        }

        /// <summary>
        /// Adds example nested tags to a markdown file for demonstration/testing.
        /// </summary>
        public async Task<bool> AddExampleTagsToFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            string content = await File.ReadAllTextAsync(filePath);
            string? frontmatter = _yamlHelper.ExtractFrontmatter(content);
            var frontmatterDict = string.IsNullOrEmpty(frontmatter)
                ? []
                : _yamlHelper.ParseYamlToDictionary(frontmatter);
            var exampleTags = new List<string> { "mba/course/finance", "type/note/case-study", "subject/leadership" };
            if (frontmatterDict.ContainsKey("tags"))
            {
                var tags = GetExistingTags(frontmatterDict);
                exampleTags.AddRange(tags);
            }
            frontmatterDict["tags"] = exampleTags.Distinct().OrderBy(t => t).ToList();
            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would add example tags to {FilePath}", filePath);
                return true;
            }
            var updatedFrontmatter = YamlHelper.SerializeYaml(frontmatterDict);
            var updatedFrontmatterDict = _yamlHelper.ParseYamlToDictionary(updatedFrontmatter);
            string updatedContent = _yamlHelper.ReplaceFrontmatter(content, updatedFrontmatterDict);
            await File.WriteAllTextAsync(filePath, updatedContent);
            _logger.LogInformation("Added example tags to {FilePath}", filePath);
            Stats["FilesModified"]++;
            return true;
        }

        /// <summary>
        /// Checks and enforces metadata consistency in all markdown files in a directory.
        /// </summary>
        public async Task<Dictionary<string, int>> CheckAndEnforceMetadataConsistencyAsync(string directory)
        {
            if (!Directory.Exists(directory))
            {
                _logger.LogError("Directory not found: {Directory}", directory);
                return Stats;
            }
            var markdownFiles = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories);
            foreach (var file in markdownFiles)
            {
                await CheckAndEnforceMetadataConsistencyInFileAsync(file);
            }
            return Stats;
        }

        /// <summary>
        /// Checks and enforces metadata consistency in a single markdown file.
        /// </summary>
        public async Task<bool> CheckAndEnforceMetadataConsistencyInFileAsync(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            string content = await File.ReadAllTextAsync(filePath);
            string? frontmatter = _yamlHelper.ExtractFrontmatter(content);
            var frontmatterDict = string.IsNullOrEmpty(frontmatter)
                ? []
                : _yamlHelper.ParseYamlToDictionary(frontmatter);
            // Example: Ensure required fields exist
            var requiredFields = new[] { "title", "type", "tags" };
            bool modified = false;
            foreach (var field in requiredFields)
            {
                if (!frontmatterDict.ContainsKey(field))
                {
                    frontmatterDict[field] = "[MISSING]";
                    modified = true;
                    _logger.LogWarning("Added missing metadata field '{Field}' to {FilePath}", field, filePath);
                }
            }
            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would enforce metadata consistency in {FilePath}", filePath);
                return true;
            }
            if (modified)
            {
                var updatedFrontmatter = YamlHelper.SerializeYaml(frontmatterDict);
                var updatedFrontmatterDict = _yamlHelper.ParseYamlToDictionary(updatedFrontmatter);
                var updatedContent = _yamlHelper.ReplaceFrontmatter(content, updatedFrontmatterDict);
                await File.WriteAllTextAsync(filePath, updatedContent);
                _logger.LogInformation("Enforced metadata consistency in {FilePath}", filePath);
                Stats["FilesModified"]++;
            }
            return modified;
        }

        /// <summary>
        /// Updates or adds a specific frontmatter key-value pair in all markdown files within a directory.
        /// </summary>
        /// <param name="path">Path to a directory or file to process.</param>
        /// <param name="key">The frontmatter key to add or update.</param>
        /// <param name="value">The value to set for the key.</param>
        /// <returns>Dictionary with processing statistics.</returns>
        public async Task<Dictionary<string, int>> UpdateFrontmatterKeyAsync(string path, string key, object value)
        {
            // Reset stats for this operation
            Stats = new Dictionary<string, int>
            {
                { "FilesProcessed", 0 },
                { "FilesModified", 0 },
                { "FilesWithErrors", 0 }
            };

            if (File.Exists(path) && Path.GetExtension(path).Equals(".md", StringComparison.InvariantCultureIgnoreCase))
            {
                // Process a single file
                _ = await UpdateFrontmatterKeyInFileAsync(path, key, value);
                return Stats;
            }
            else if (Directory.Exists(path))
            {
                // Process a directory recursively
                var markdownFiles = Directory.GetFiles(path, "*.md", SearchOption.AllDirectories);

                foreach (var file in markdownFiles)
                {
                    await UpdateFrontmatterKeyInFileAsync(file, key, value);
                }

                return Stats;
            }
            else
            {
                _failedLogger.LogError("Path not found or not a markdown file: {Path}", path);
                Stats["FilesWithErrors"]++;
                return Stats;
            }
        }

        /// <summary>
        /// Updates or adds a specific frontmatter key-value pair in a markdown file.
        /// </summary>
        /// <param name="filePath">Path to the markdown file.</param>
        /// <param name="key">The frontmatter key to add or update.</param>
        /// <param name="value">The value to set for the key.</param>
        /// <returns>True if the file was modified, false otherwise.</returns>
        private async Task<bool> UpdateFrontmatterKeyInFileAsync(string filePath, string key, object value)
        {
            if (!File.Exists(filePath))
            {
                _failedLogger.LogError("File not found: {FilePath}", filePath);
                Stats["FilesWithErrors"]++;
                return false;
            }

            try
            {
                Stats["FilesProcessed"]++;

                if (_verbose)
                {
                    _logger.LogInformation("Processing file: {FilePath}", filePath);
                }

                // Read the file content
                string content = await File.ReadAllTextAsync(filePath);

                // Extract frontmatter and parse YAML
                string? frontmatter = _yamlHelper.ExtractFrontmatter(content);
                Dictionary<string, object> frontmatterDict;

                if (string.IsNullOrEmpty(frontmatter))
                {
                    if (_verbose)
                    {
                        _logger.LogInformation("No frontmatter found in file, creating new frontmatter: {FilePath}", filePath);
                    }
                    // Create new frontmatter
                    frontmatterDict = [];
                }
                else
                {
                    // Parse the existing frontmatter into a dictionary
                    frontmatterDict = _yamlHelper.ParseYamlToDictionary(frontmatter);
                }

                // Check if the key already exists with the same value
                bool needsUpdate = !frontmatterDict.ContainsKey(key) || !frontmatterDict[key]?.Equals(value) == true;

                if (needsUpdate)
                {
                    // Update or add the key-value pair
                    frontmatterDict[key] = value;

                    // Update the content with the new frontmatter
                    var updatedContent = _yamlHelper.UpdateFrontmatter(content, frontmatterDict);

                    if (_dryRun)
                    {
                        if (_verbose)
                        {
                            _logger.LogInformation("[DRY RUN] Would update '{Key}: {Value}' in {FilePath}", key, value, filePath);
                        }
                    }
                    else
                    {
                        // Write the updated content back to the file
                        await File.WriteAllTextAsync(filePath, updatedContent);
                        Stats["FilesModified"]++;

                        if (_verbose)
                        {
                            _logger.LogInformation("Updated '{Key}: {Value}' in {FilePath}", key, value, filePath);
                        }
                    }

                    return true;
                }
                else if (_verbose)
                {
                    _logger.LogInformation("Key '{Key}' already has value '{Value}' in {FilePath}", key, value, filePath);
                }

                return false;
            }
            catch (Exception ex)
            {
                _failedLogger.LogError(ex, "Error processing file: {FilePath}", filePath);
                Stats["FilesWithErrors"]++;
                return false;
            }
        }

        /// <summary>
        /// Diagnoses YAML frontmatter issues in markdown files.
        /// </summary>
        /// <param name="directory">The directory path to process.</param>
        /// <returns>A list of problematic files with their diagnostic information.</returns>
        public async Task<List<(string FilePath, string DiagnosticMessage)>> DiagnoseFrontmatterIssuesAsync(string directory)
        {
            var results = new List<(string FilePath, string DiagnosticMessage)>();

            if (!Directory.Exists(directory))
            {
                _logger.LogError("Directory not found: {Directory}", directory);
                results.Add((directory, "Directory not found"));
                return results;
            }

            try
            {
                var markdownFiles = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories);

                _logger.LogInformation("Analyzing {Count} markdown files for YAML frontmatter issues", markdownFiles.Length);

                int filesWithIssues = 0;

                foreach (var file in markdownFiles)
                {
                    string content = await File.ReadAllTextAsync(file);
                    var (Success, Message, Data) = _yamlHelper.DiagnoseYamlFrontmatter(content);

                    if (!Success)
                    {
                        filesWithIssues++;
                        results.Add((file, Message));

                        if (_verbose)
                        {
                            _logger.LogWarning("YAML issue in {FilePath}: {Message}", file, Message);
                        }
                    }
                }

                _logger.LogInformation("Found {Count} files with YAML frontmatter issues out of {Total} files",
                    filesWithIssues, markdownFiles.Length);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error diagnosing directory: {Directory}", directory);
                results.Add((directory, $"Error during diagnosis: {ex.Message}"));
                return results;
            }
        }
    }
}
