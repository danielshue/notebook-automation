// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.TagManagement;

/// <summary>
/// Processes tags in markdown files and handles tag management operations.
/// </summary>
/// <remarks>
/// The <c>TagProcessor</c> class provides functionality for managing tags in markdown files,
/// including adding nested tags, restructuring tag hierarchies, clearing tags from index files,
/// and enforcing metadata consistency. It supports dry-run mode for testing changes without
/// modifying files and verbose logging for detailed diagnostics.
/// </remarks>
public class TagProcessor
{
    private readonly ILogger<TagProcessor> _logger;
    private readonly ILogger _failedLogger;
    private readonly IYamlHelper _yamlHelper;
    private readonly bool _dryRun;
    private readonly bool _verbose;
    private readonly HashSet<string> _fieldsToProcess;

    /// <summary>
    /// Gets statistics about the processing performed.
    /// </summary>
    public Dictionary<string, int> Stats { get; private set; } = new Dictionary<string, int>
    {
        { "FilesProcessed", 0 },
        { "FilesModified", 0 },
        { "TagsAdded", 0 },
        { "IndexFilesCleared", 0 },
        { "FilesWithErrors", 0 },
    };    /// <summary>
          /// Initializes a new instance of the <see cref="TagProcessor"/> class with default field configuration.
          /// </summary>
          /// <param name="logger">Logger for general diagnostics and operational messages.</param>
          /// <param name="failedLogger">Dedicated logger for recording failed operations and errors.</param>
          /// <param name="yamlHelper">Helper utility for YAML frontmatter processing and manipulation.</param>
          /// <param name="dryRun">When true, simulates operations without making actual file changes. Default is false.</param>
          /// <param name="verbose">When true, provides detailed verbose output during processing. Default is false.</param>
          /// <remarks>
          /// <para>
          /// This constructor initializes the TagProcessor with a default set of frontmatter fields
          /// that will be processed for tag generation. The default fields include common academic
          /// and content metadata: course, lecture, topic, subjects, professor, university, program,
          /// assignment, type, and author.
          /// </para>
          ///
          /// <para>
          /// The processor supports both dry-run mode for testing changes and verbose mode for
          /// detailed operational logging. Statistics are automatically tracked during processing
          /// and can be accessed via the <see cref="Stats"/> property.
          /// </para>
          /// </remarks>
          /// <exception cref="ArgumentNullException">
          /// Thrown when <paramref name="logger"/>, <paramref name="failedLogger"/>,
          /// or <paramref name="yamlHelper"/> is null.
          /// </exception>
          /// <example>
          /// <code>
          /// var processor = new TagProcessor(logger, failedLogger, yamlHelper);
          ///
          /// // With dry-run mode enabled
          /// var dryRunProcessor = new TagProcessor(logger, failedLogger, yamlHelper, dryRun: true);
          ///
          /// // With verbose logging
          /// var verboseProcessor = new TagProcessor(logger, failedLogger, yamlHelper, verbose: true);
          /// </code>
          /// </example>
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
    /// Initializes a new instance of the <see cref="TagProcessor"/> class with custom field configuration.
    /// </summary>
    /// <param name="logger">Logger for general diagnostics and operational messages.</param>
    /// <param name="failedLogger">Dedicated logger for recording failed operations and errors.</param>
    /// <param name="yamlHelper">Helper utility for YAML frontmatter processing and manipulation.</param>
    /// <param name="dryRun">When true, simulates operations without making actual file changes. Default is false.</param>
    /// <param name="verbose">When true, provides detailed verbose output during processing. Default is false.</param>
    /// <param name="fieldsToProcess">
    /// Custom set of frontmatter field names to process for tag generation.
    /// If null, uses the default set of fields (course, lecture, topic, subjects, professor, university, program, assignment, type, author).
    /// </param>
    /// <remarks>
    /// <para>
    /// This constructor allows customization of which frontmatter fields are processed
    /// for automatic tag generation. This is useful when working with specific content
    /// types or when you need to focus on particular metadata fields.
    /// </para>
    ///
    /// <para>
    /// Each field in <paramref name="fieldsToProcess"/> will be mapped to a tag prefix
    /// using <see cref="GetTagPrefixForField"/> and the field's value will be normalized
    /// using <see cref="NormalizeTagValue"/> to create consistent nested tags.
    /// </para>
    ///
    /// <para>
    /// The processor maintains statistics about operations performed, accessible
    /// via the <see cref="Stats"/> property, including files processed, modified,
    /// tags added, and any errors encountered.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/>, <paramref name="failedLogger"/>,
    /// or <paramref name="yamlHelper"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// // Custom fields for academic content
    /// var academicFields = new HashSet&lt;string&gt; { "course", "professor", "university" };
    /// var processor = new TagProcessor(logger, failedLogger, yamlHelper,
    ///     fieldsToProcess: academicFields);
    ///
    /// // Custom fields for business content
    /// var businessFields = new HashSet&lt;string&gt; { "department", "project", "author" };
    /// var businessProcessor = new TagProcessor(logger, failedLogger, yamlHelper,
    ///     dryRun: true, fieldsToProcess: businessFields);
    /// </code>
    /// </example>
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
        _verbose = verbose;        // Use provided fields if any, otherwise use defaults
        _fieldsToProcess = fieldsToProcess ??
        [
            "course", "lecture", "topic", "subjects", "professor",
            "university", "program", "assignment", "type", "author"
        ];
    }

    /// <summary>
    /// Processes a directory recursively to add or update nested tags in all markdown files.
    /// </summary>
    /// <param name="directory">The directory path to process recursively for markdown files.</param>
    /// <returns>
    /// A dictionary containing processing statistics with keys: "FilesProcessed", "FilesModified",
    /// "TagsAdded", "IndexFilesCleared", and "FilesWithErrors".
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs recursive directory traversal to find all markdown files (.md extension)
    /// and processes each one using <see cref="ProcessFileAsync"/>. Index files (those starting
    /// with "_index" or named "index.md") are automatically skipped during processing.
    /// </para>
    ///
    /// <para>
    /// The method is fault-tolerant and will continue processing other files even if individual
    /// files encounter errors. All errors are logged to both the main logger and the failed
    /// operations logger for comprehensive error tracking.
    /// </para>
    ///
    /// <para>
    /// Processing statistics are maintained throughout the operation and can be accessed
    /// both from the return value and the <see cref="Stats"/> property. In dry-run mode,
    /// the method will simulate all operations without making actual file changes.
    /// </para>
    /// </remarks>
    /// <exception cref="DirectoryNotFoundException">
    /// Logged as an error if the specified directory does not exist. The method returns
    /// current statistics rather than throwing an exception.
    /// </exception>
    /// <example>
    /// <code>
    /// // Process entire directory tree
    /// var stats = await processor.ProcessDirectoryAsync("/path/to/content");
    /// Console.WriteLine($"Processed {stats["FilesProcessed"]} files");
    /// Console.WriteLine($"Modified {stats["FilesModified"]} files");
    /// Console.WriteLine($"Added {stats["TagsAdded"]} tags");
    ///
    /// // Check for errors
    /// if (stats["FilesWithErrors"] > 0)
    /// {
    ///     Console.WriteLine($"Encountered errors in {stats["FilesWithErrors"]} files");
    /// }
    /// </code>
    /// </example>
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

            _logger.LogDebug($"Found {markdownFiles.Length} markdown files to process", markdownFiles.Length);

            foreach (var file in markdownFiles)
            {
                await ProcessFileAsync(file).ConfigureAwait(false);
            }

            _logger.LogDebug(
                $"Processing complete: {Stats["FilesProcessed"]} files processed, {Stats["FilesModified"]} files modified, " +
                $"{Stats["TagsAdded"]} tags added, {Stats["IndexFilesCleared"]} index files cleared, {Stats["FilesWithErrors"]} files with errors");

            return Stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing directory: {directory}");
            _failedLogger.LogError($"Failed to process directory {directory}: {ex.Message}");
            Stats["FilesWithErrors"]++;
            return Stats;
        }
    }

    /// <summary>
    /// Processes a single markdown file to add or update nested tags based on frontmatter content.
    /// </summary>
    /// <param name="filePath">The path to the markdown file to process.</param>
    /// <returns>True if the file was modified with new tags, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive tag processing on a single markdown file:
    /// 1. Extracts and parses YAML frontmatter
    /// 2. Generates nested tags from configured frontmatter fields
    /// 3. Merges new tags with existing tags (avoiding duplicates)
    /// 4. Updates the file with the enhanced tag collection
    /// </para>
    ///
    /// <para>
    /// Index files (starting with "_index" or named "index.md") are automatically skipped
    /// to avoid interference with structural navigation files. Files without valid
    /// frontmatter are also skipped with appropriate logging.
    /// </para>
    ///
    /// <para>
    /// The method uses <see cref="GenerateNestedTags"/> to create semantic tags from
    /// frontmatter fields, applying consistent normalization and formatting. All new
    /// tags are merged with existing tags and sorted alphabetically for consistency.
    /// </para>
    ///
    /// <para>
    /// In dry-run mode, the method simulates all operations and logs what would be changed
    /// without actually modifying files. Statistics are updated regardless of dry-run status.
    /// </para>
    /// </remarks>
    /// <exception cref="FileNotFoundException">
    /// Logged as an error if the specified file does not exist. The method returns false
    /// rather than throwing an exception.
    /// </exception>
    /// <exception cref="IOException">
    /// File access errors are caught, logged to both loggers, and result in a false return value.
    /// </exception>
    /// <example>
    /// <code>
    /// // Process a single file
    /// bool modified = await processor.ProcessFileAsync("/path/to/document.md");
    /// if (modified)
    /// {
    ///     Console.WriteLine("File was updated with new tags");
    /// }
    ///
    /// // Check statistics after processing
    /// var stats = processor.Stats;
    /// Console.WriteLine($"Tags added: {stats["TagsAdded"]}");
    /// </code>
    /// </example>
    public async Task<bool> ProcessFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _failedLogger.LogError($"File not found: {filePath}");
            Stats["FilesWithErrors"]++;
            return false;
        }

        try
        {
            Stats["FilesProcessed"]++;

            _logger.LogDebug($"Processing file: {filePath}");

            // Skip index files if configured to do so
            if (Path.GetFileName(filePath).StartsWith("_index") || Path.GetFileName(filePath).Equals("index.md"))
            {
                _logger.LogDebug($"Skipping index file: {filePath}");

                return false;
            }

            // Read the file content
            string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

            // Extract frontmatter and parse YAML with better error handling
            string? frontmatter = _yamlHelper.ExtractFrontmatter(content);

            if (string.IsNullOrEmpty(frontmatter))
            {
                _logger.LogDebug($"No frontmatter found in file: {filePath}");

                // Additional debug info if the file appears to have frontmatter but extraction failed
                if (content.TrimStart().StartsWith("---"))
                {
                    _logger.LogDebug($"Content appears to have frontmatter but extraction failed. First 50 chars: {(content.Length > 50 ? content[..50] + "..." : content)}");
                }

                return false;
            }

            // Parse the frontmatter into a dictionary with enhanced error handling
            var frontmatterDict = _yamlHelper.ParseYamlToDictionary(frontmatter);
            if (frontmatterDict.Count == 0)
            {
                _logger.LogInformation($"Empty or invalid frontmatter in file: {filePath}");
                _logger.LogDebug($"Frontmatter content that failed parsing: {(frontmatter.Length > 100 ? frontmatter[..100] + "..." : frontmatter)}");

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
                    _logger.LogInformation($"[DRY RUN] Would add {newTags.Count} tags to {filePath}");
                    foreach (var tag in newTags)
                    {
                        _logger.LogInformation($"[DRY RUN] New tag: {tag}");
                    }
                }
                else
                {
                    // Write the updated content back to the file
                    await File.WriteAllTextAsync(filePath, updatedContent).ConfigureAwait(false);
                    Stats["FilesModified"]++;
                    Stats["TagsAdded"] += newTags.Count;
                    modified = true;

                    _logger.LogDebug($"Added {newTags.Count} tags to {filePath}");
                    foreach (var tag in newTags)
                    {
                        _logger.LogDebug($"Added tag: {tag}");
                    }
                }
            }
            else
            {
                _logger.LogInformation($"No new tags to add for {filePath}");
            }

            return modified;
        }
        catch (Exception ex)
        {
            _failedLogger.LogError(ex, $"Error processing file: {filePath}");
            Stats["FilesWithErrors"]++;
            return false;
        }
    }

    /// <summary>
    /// Clears all tags from a markdown file's frontmatter.
    /// </summary>
    /// <param name="filePath">The path to the markdown file to modify.</param>
    /// <param name="frontmatter">The parsed frontmatter dictionary containing current metadata.</param>
    /// <param name="content">The complete file content including frontmatter and body.</param>
    /// <returns>True if tags were found and cleared (or would be cleared in dry-run), false if no tags existed.</returns>
    /// <remarks>
    /// <para>
    /// This method removes all tag-related metadata from a file's frontmatter, including
    /// both "tags" fields and case-insensitive variations. It's particularly useful for
    /// cleaning up index files or resetting tag collections before regeneration.
    /// </para>
    ///
    /// <para>
    /// The method performs case-insensitive tag field detection to handle variations
    /// in YAML field naming (e.g., "tags", "Tags", "TAGS"). All matching fields are
    /// removed from the frontmatter dictionary.
    /// </para>
    ///
    /// <para>
    /// In dry-run mode, the method simulates the clearing operation and updates statistics
    /// without actually modifying the file. This allows for testing and verification
    /// of operations before applying changes.
    /// </para>
    ///
    /// <para>
    /// Statistics are automatically updated to track the number of index files cleared
    /// and files modified during the operation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clear tags from a specific file
    /// string content = await File.ReadAllTextAsync(filePath);
    /// var frontmatter = yamlHelper.ParseYamlToDictionary(yamlHelper.ExtractFrontmatter(content));
    /// bool cleared = await processor.ClearTagsFromFileAsync(filePath, frontmatter, content);
    ///
    /// if (cleared)
    /// {
    ///     Console.WriteLine("Tags were cleared from the file");
    /// }
    /// </code>
    /// </example>
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
            _logger.LogInformation($"[DRY RUN] Would clear tags from index file: {filePath}");
            Stats["IndexFilesCleared"]++;
            return true;
        }

        var updatedContent = _yamlHelper.UpdateFrontmatter(content, frontmatter);
        await File.WriteAllTextAsync(filePath, updatedContent, Encoding.UTF8).ConfigureAwait(false);

        _logger.LogInformation($"Cleared tags from index file: {filePath}");
        Stats["IndexFilesCleared"]++;
        Stats["FilesModified"]++;

        return true;
    }

    /// <summary>
    /// Adds nested tags to a markdown file based on frontmatter field values.
    /// </summary>
    /// <param name="filePath">The path to the markdown file to modify.</param>
    /// <param name="frontmatter">The parsed frontmatter dictionary containing metadata fields.</param>
    /// <param name="content">The complete file content including frontmatter and body.</param>
    /// <returns>True if new tags were added to the file, false if no new tags were generated.</returns>
    /// <remarks>
    /// <para>
    /// This method processes configured frontmatter fields (specified in <c>_fieldsToProcess</c>)
    /// to generate semantic nested tags. Each field value is transformed into a hierarchical
    /// tag using the pattern: #{prefix}/{normalized-value}, where the prefix is determined
    /// by <see cref="GetTagPrefixForField"/> and the value is normalized using <see cref="NormalizeTagValue"/>.
    /// </para>
    ///
    /// <para>
    /// The method preserves existing tags and only adds new ones, avoiding duplicates.
    /// New tags are merged with the existing tag collection and the updated frontmatter
    /// is written back to the file using the YAML helper.
    /// </para>
    ///
    /// <para>
    /// Tag generation supports both single-value and list-value frontmatter fields.
    /// For list fields, each item in the list generates a separate tag with the same prefix.
    /// All tag values are normalized for consistency (lowercase, hyphens for spaces, etc.).
    /// </para>
    ///
    /// <para>
    /// Statistics tracking includes counting the number of tags added and files modified.
    /// In dry-run mode, the method simulates operations without making file changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example frontmatter that would generate nested tags:
    /// // course: "Finance 101"
    /// // professor: "John Smith"
    /// // subjects: ["investment", "portfolio management"]
    ///
    /// // Generated tags:
    /// // - mba/course/finance-101
    /// // - people/john-smith
    /// // - subject/investment
    /// // - subject/portfolio-management
    ///
    /// var frontmatterDict = yamlHelper.ParseYamlToDictionary(frontmatter);
    /// bool added = await processor.AddNestedTagsToFileAsync(filePath, frontmatterDict, content);
    /// </code>
    /// </example>
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
                        _logger.LogDebug($"Adding tag: {nestedTag} to {filePath}");

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
            _logger.LogInformation($"[DRY RUN] Would update tags in file: {filePath}");
            return true;
        }

        var updatedContent = _yamlHelper.UpdateFrontmatter(content, frontmatter);
        await File.WriteAllTextAsync(filePath, updatedContent, Encoding.UTF8).ConfigureAwait(false);

        _logger.LogInformation($"Updated tags in file: {filePath}");
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
            _ => fieldName,  // Default to using the field name itself
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
            .Replace(",", string.Empty)
            .Replace(":", string.Empty)
            .Replace(";", string.Empty)
            .Replace("\"", string.Empty)
            .Replace("'", string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    /// <summary>
    /// Restructures tags in all markdown files in a directory for consistency (lowercase, dashes, etc.).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<Dictionary<string, int>> RestructureTagsInDirectoryAsync(string directory)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogError($"Directory not found: {directory}");
            return Stats;
        }

        var markdownFiles = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories);
        foreach (var file in markdownFiles)
        {
            await RestructureTagsInFileAsync(file).ConfigureAwait(false);
        }

        return Stats;
    }

    /// <summary>
    /// Normalizes and restructures tags in a single markdown file.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<bool> RestructureTagsInFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        string? frontmatter = _yamlHelper.ExtractFrontmatter(content);
        if (string.IsNullOrEmpty(frontmatter))
        {
            return false;
        }

        var frontmatterDict = _yamlHelper.ParseYamlToDictionary(frontmatter);
        if (!frontmatterDict.ContainsKey("tags"))
        {
            return false;
        }

        var tags = GetExistingTags(frontmatterDict);
        var normalizedTags = tags.Select(NormalizeTagValue).Distinct().OrderBy(t => t).ToList();
        frontmatterDict["tags"] = normalizedTags;
        if (_dryRun)
        {
            _logger.LogInformation($"[DRY RUN] Would restructure tags in {filePath}");
            return true;
        }

        var updatedFrontmatter = YamlHelper.SerializeYaml(frontmatterDict);
        var updatedFrontmatterDict = _yamlHelper.ParseYamlToDictionary(updatedFrontmatter);
        string updatedContent = _yamlHelper.ReplaceFrontmatter(content, updatedFrontmatterDict);
        await File.WriteAllTextAsync(filePath, updatedContent).ConfigureAwait(false);
        _logger.LogInformation("Restructured tags in {FilePath}", filePath);
        Stats["FilesModified"]++;
        return true;
    }

    /// <summary>
    /// Adds example nested tags to a markdown file for demonstration/testing.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<bool> AddExampleTagsToFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
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
        await File.WriteAllTextAsync(filePath, updatedContent).ConfigureAwait(false);
        _logger.LogInformation("Added example tags to {FilePath}", filePath);
        Stats["FilesModified"]++;
        return true;
    }

    /// <summary>
    /// Checks and enforces metadata consistency in all markdown files in a directory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
            await CheckAndEnforceMetadataConsistencyInFileAsync(file).ConfigureAwait(false);
        }

        return Stats;
    }

    /// <summary>
    /// Checks and enforces metadata consistency in a single markdown file.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<bool> CheckAndEnforceMetadataConsistencyInFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
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
                _logger.LogWarning($"Added missing metadata field '{field}' to {filePath}");
            }
        }

        if (_dryRun)
        {
            _logger.LogInformation($"[DRY RUN] Would enforce metadata consistency in {filePath}");
            return true;
        }

        if (modified)
        {
            var updatedFrontmatter = YamlHelper.SerializeYaml(frontmatterDict);
            var updatedFrontmatterDict = _yamlHelper.ParseYamlToDictionary(updatedFrontmatter);
            var updatedContent = _yamlHelper.ReplaceFrontmatter(content, updatedFrontmatterDict);
            await File.WriteAllTextAsync(filePath, updatedContent).ConfigureAwait(false);
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
            { "FilesWithErrors", 0 },
        };

        if (File.Exists(path) && Path.GetExtension(path).Equals(".md", StringComparison.InvariantCultureIgnoreCase))
        {
            // Process a single file
            _ = await UpdateFrontmatterKeyInFileAsync(path, key, value).ConfigureAwait(false);
            return Stats;
        }
        else if (Directory.Exists(path))
        {
            // Process a directory recursively
            var markdownFiles = Directory.GetFiles(path, "*.md", SearchOption.AllDirectories);

            foreach (var file in markdownFiles)
            {
                await UpdateFrontmatterKeyInFileAsync(file, key, value).ConfigureAwait(false);
            }

            return Stats;
        }
        else
        {
            _failedLogger.LogError($"Path not found or not a markdown file: {path}");
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
            _failedLogger.LogError($"File not found: {filePath}");
            Stats["FilesWithErrors"]++;
            return false;
        }

        try
        {
            Stats["FilesProcessed"]++;

            _logger.LogDebug($"Processing file: {filePath}");

            // Read the file content
            string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

            // Extract frontmatter and parse YAML
            string? frontmatter = _yamlHelper.ExtractFrontmatter(content);
            Dictionary<string, object> frontmatterDict;

            if (string.IsNullOrEmpty(frontmatter))
            {
                _logger.LogDebug($"No frontmatter found in file, creating new frontmatter: {filePath}");

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
                    _logger.LogDebug($"[DRY RUN] Would update '{key}: {value}' in {filePath}");
                }
                else
                {
                    // Write the updated content back to the file
                    await File.WriteAllTextAsync(filePath, updatedContent).ConfigureAwait(false);
                    Stats["FilesModified"]++;

                    _logger.LogDebug($"Updated '{key}: {value}' in {filePath}");
                }

                return true;
            }
            else
            {
                _logger.LogDebug($"Key '{key}' already has value '{value}' in {filePath}");

            }

            return false;
        }
        catch (Exception ex)
        {
            _failedLogger.LogError(ex, $"Error processing file: {filePath}");

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
            _logger.LogError($"Directory not found: {directory}");

            results.Add((directory, "Directory not found"));

            return results;
        }

        try
        {
            var markdownFiles = Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories);

            _logger.LogDebug($"Analyzing {markdownFiles.Length} markdown files for YAML frontmatter issues");

            int filesWithIssues = 0;

            foreach (var file in markdownFiles)
            {
                string content = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                var (success, message, data) = _yamlHelper.DiagnoseYamlFrontmatter(content);

                if (!success)
                {
                    filesWithIssues++;

                    results.Add((file, message));

                    _logger.LogWarning($"YAML issue in {file}: {message}");
                }
            }

            _logger.LogWarning($"Found {filesWithIssues} files with YAML frontmatter issues out of {markdownFiles.Length} files");

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error diagnosing directory: {directory}");
            results.Add((directory, $"Error during diagnosis: {ex.Message}"));
            return results;
        }
    }
}