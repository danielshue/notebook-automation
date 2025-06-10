// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Processor for ensuring metadata in markdown files based on directory structure.
/// Extracts program/course/class/module/lesson metadata and updates YAML frontmatter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the MetadataEnsureProcessor class.
/// </remarks>
/// <param name="_logger">The _logger instance.</param>
/// <param name="_yamlHelper">The YAML helper for frontmatter operations.</param>
/// <param name="_metadataDetector">The metadata hierarchy detector.</param>
/// <param name="_structureExtractor">The course structure extractor.</param>
public class MetadataEnsureProcessor(
    ILogger<MetadataEnsureProcessor> _logger,
    IYamlHelper _yamlHelper,
    IMetadataHierarchyDetector _metadataDetector,
    ICourseStructureExtractor _structureExtractor)
{
    private readonly ILogger<MetadataEnsureProcessor> _logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
    private readonly IYamlHelper _yamlHelper = _yamlHelper ?? throw new ArgumentNullException(nameof(_yamlHelper));
    private readonly IMetadataHierarchyDetector _metadataDetector = _metadataDetector ?? throw new ArgumentNullException(nameof(_metadataDetector));
    private readonly ICourseStructureExtractor _structureExtractor = _structureExtractor ?? throw new ArgumentNullException(nameof(_structureExtractor));

    /// <summary>
    /// Ensures metadata is properly set in a markdown file.
    /// </summary>
    /// <param name="filePath">The path to the markdown file to process.</param>
    /// <param name="forceOverwrite">Whether to overwrite existing metadata values.</param>
    /// <param name="dryRun">Whether to simulate the operation without making changes.</param>
    /// <returns>True if the file was updated (or would be updated in dry run), false otherwise.</returns>
    public async Task<bool> EnsureMetadataAsync(string filePath, bool forceOverwrite = false, bool dryRun = false)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return false;
        }

        if (!filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug($"Skipping non-markdown file: {filePath}");
            return false;
        }

        try
        {
            _logger.LogDebug($"Processing metadata for file: {filePath}");

            // Read the existing file content
            string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

            // Extract existing frontmatter
            string? existingYaml = _yamlHelper.ExtractFrontmatter(content);

            var metadata = existingYaml != null
                ? _yamlHelper.ParseYamlToDictionary(existingYaml)
                    .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
                : new Dictionary<string, object?>();

            // Store original metadata for comparison
            var originalMetadata = new Dictionary<string, object?>(metadata);

            // Determine template type if not set
            EnsureTemplateType(metadata, filePath);

            // Extract hierarchy information (program, course, class)
            var hierarchyInfo = _metadataDetector.FindHierarchyInfo(filePath);

            // Extract course structure information (module, lesson) into metadata directly
            _structureExtractor.ExtractModuleAndLesson(filePath, metadata);            // Determine the correct template-type based on existing metadata
            string? templateType = metadata.GetValueOrDefault("template-type", string.Empty)?.ToString();

            // Update metadata with hierarchy information based on template-type
            _metadataDetector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, templateType);

            // Ensure required fields based on template type
            EnsureRequiredFields(metadata);

            // Check if any changes were made
            bool hasChanges = HasMetadataChanged(originalMetadata, metadata, forceOverwrite);

            if (!hasChanges)
            {
                _logger.LogDebug($"No metadata changes needed for: {filePath}");
                return false;
            }

            if (dryRun)
            {
                _logger.LogInformation($"DRY RUN: Would update metadata for: {filePath}");
                LogMetadataChanges(originalMetadata, metadata, filePath);
                return true;
            }

            // Update the file with new frontmatter
            string updatedContent = _yamlHelper.UpdateFrontmatter(content, metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));
            await File.WriteAllTextAsync(filePath, updatedContent).ConfigureAwait(false);

            _logger.LogInformation($"Updated metadata for: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process metadata for file: {filePath}");
            return false;
        }
    }

    /// <summary>
    /// Determines and sets the template-type based on the filename if not already set.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to update.</param>
    /// <param name="filePath">The file path to analyze.</param>
    private static void EnsureTemplateType(Dictionary<string, object?> metadata, string filePath)
    {
        // If template-type is already set, don't override it
        if (metadata.ContainsKey("template-type") && !string.IsNullOrEmpty(metadata["template-type"]?.ToString()))
        {
            return;
        }

        string fileName = Path.GetFileName(filePath);
        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;

        // Determine template type based on filename patterns
        string templateType = DetermineTemplateType(fileName, directory);

        if (!string.IsNullOrEmpty(templateType))
        {
            metadata["template-type"] = templateType;
        }
    }

    /// <summary>
    /// Determines the appropriate template type based on filename and associated files.
    /// </summary>
    /// <param name="fileName">The filename to analyze.</param>
    /// <param name="directory">The directory containing the file.</param>
    /// <returns>The appropriate template type, or empty string if cannot be determined.</returns>
    private static string DetermineTemplateType(string fileName, string directory)
    {
        // Check for specific filename patterns first
        if (fileName.EndsWith("-instructions.md", StringComparison.OrdinalIgnoreCase))
        {
            return "resource-reading";
        }

        // Check for associated file types (assuming the markdown references these files)
        string baseName = Path.GetFileNameWithoutExtension(fileName);

        // Look for associated PDF or video files in the same directory
        if (File.Exists(Path.Combine(directory, baseName + ".pdf")))
        {
            return "pdf-reference";
        }

        if (File.Exists(Path.Combine(directory, baseName + ".mp4")) ||
            File.Exists(Path.Combine(directory, baseName + ".mov")) ||
            File.Exists(Path.Combine(directory, baseName + ".avi")))
        {
            return "video-reference";
        }

        // Check for index files
        if (fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase))
        {
            // This would need more sophisticated logic to determine the index type
            // For now, return empty and let manual assignment handle it
            return string.Empty;
        }

        // Default fallback - could be a general note
        return string.Empty;
    }

    /// <summary>
    /// Ensures required fields are present based on the template type.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to update.</param>
    private void EnsureRequiredFields(Dictionary<string, object?> metadata)
    {
        string templateType = metadata.GetValueOrDefault("template-type", string.Empty)?.ToString() ?? string.Empty;

        // Set auto-generated-state if not present
        if (!metadata.ContainsKey("auto-generated-state"))
        {
            metadata["auto-generated-state"] = "writable";
        }

        // Set date-created if not present
        if (!metadata.ContainsKey("date-created"))
        {
            metadata["date-created"] = DateTime.Now.ToString("yyyy-MM-dd");
        }

        // Set publisher if not present (based on requirements)
        if (!metadata.ContainsKey("publisher"))
        {
            metadata["publisher"] = "University of Illinois at Urbana-Champaign";
        }

        // Template-specific required fields
        switch (templateType)
        {
            case "pdf-reference":
                EnsurePdfReferenceFields(metadata);
                break;
            case "video-reference":
                EnsureVideoReferenceFields(metadata);
                break;
            case "resource-reading":
                EnsureResourceReadingFields(metadata);
                break;
            default:
                EnsureBasicFields(metadata);
                break;
        }
    }

    /// <summary>
    /// Ensures required fields for PDF reference template.
    /// </summary>
    private static void EnsurePdfReferenceFields(Dictionary<string, object?> metadata)
    {
        var requiredFields = new Dictionary<string, object?>
        {
            ["type"] = "note/case-study",
            ["comprehension"] = 0,
            ["status"] = "unread",
            ["completion-date"] = string.Empty,
            ["date-modified"] = string.Empty,
            ["date-review"] = string.Empty,
            ["onedrive-shared-link"] = string.Empty,
            ["onedrive_fullpath_file_reference"] = string.Empty,
            ["pdf-size"] = string.Empty,
            ["pdf-uploaded"] = string.Empty,
            ["page-count"] = string.Empty,
            ["pages"] = string.Empty,
            ["authors"] = string.Empty,
            ["tags"] = string.Empty,
        };

        foreach (var field in requiredFields)
        {
            if (!metadata.ContainsKey(field.Key))
            {
                metadata[field.Key] = field.Value;
            }
        }
    }

    /// <summary>
    /// Ensures required fields for video reference template.
    /// </summary>
    private static void EnsureVideoReferenceFields(Dictionary<string, object?> metadata)
    {
        var requiredFields = new Dictionary<string, object?>
        {
            ["type"] = "note/video-note",
            ["comprehension"] = 0,
            ["status"] = "unwatched",
            ["completion-date"] = string.Empty,
            ["date-modified"] = string.Empty,
            ["date-review"] = string.Empty,
            ["onedrive-shared-link"] = string.Empty,
            ["onedrive_fullpath_file_reference"] = string.Empty,
            ["publication-year"] = string.Empty,
            ["video-codec"] = string.Empty,
            ["video-duration"] = "00:00:00",
            ["video-resolution"] = string.Empty,
            ["video-size"] = "0 MB",
            ["video-uploaded"] = string.Empty,
            ["author"] = string.Empty,
            ["tags"] = string.Empty,
        };

        foreach (var field in requiredFields)
        {
            if (!metadata.ContainsKey(field.Key))
            {
                metadata[field.Key] = field.Value;
            }
        }
    }

    /// <summary>
    /// Ensures required fields for resource reading template.
    /// </summary>
    private static void EnsureResourceReadingFields(Dictionary<string, object?> metadata)
    {
        var requiredFields = new Dictionary<string, object?>
        {
            ["type"] = "note/reading",
            ["comprehension"] = 0,
            ["status"] = "unread",
            ["completion-date"] = string.Empty,
            ["date-modified"] = string.Empty,
            ["date-review"] = string.Empty,
            ["onedrive-shared-link"] = string.Empty,
            ["onedrive_fullpath_file_reference"] = string.Empty,
            ["page-count"] = string.Empty,
            ["pages"] = string.Empty,
            ["authors"] = string.Empty,
            ["tags"] = string.Empty,
        };

        foreach (var field in requiredFields)
        {
            if (!metadata.ContainsKey(field.Key))
            {
                metadata[field.Key] = field.Value;
            }
        }
    }

    /// <summary>
    /// Ensures basic required fields for general templates.
    /// </summary>
    private static void EnsureBasicFields(Dictionary<string, object?> metadata)
    {
        var requiredFields = new Dictionary<string, object?>
        {
            ["tags"] = string.Empty,
        };

        foreach (var field in requiredFields)
        {
            if (!metadata.ContainsKey(field.Key))
            {
                metadata[field.Key] = field.Value;
            }
        }
    }

    /// <summary>
    /// Checks if metadata has changed, considering force overwrite option.
    /// </summary>
    /// <param name="original">Original metadata.</param>
    /// <param name="updated">Updated metadata.</param>
    /// <param name="forceOverwrite">Whether to force overwrite existing values.</param>    /// <returns>True if changes were made.</returns>
    private static bool HasMetadataChanged(Dictionary<string, object?> original, Dictionary<string, object?> updated, bool forceOverwrite)
    {
        // If force overwrite is enabled, we consider changes if any fields in updated are different
        if (forceOverwrite)
        {
            return !DictionariesEqual(original, updated);
        }

        // Check for removed fields (fields in original but not in updated)
        foreach (var kvp in original)
        {
            if (!updated.ContainsKey(kvp.Key))
            {
                return true; // Field was removed
            }
        }

        // Check for new fields or populated empty fields
        foreach (var kvp in updated)
        {
            if (!original.ContainsKey(kvp.Key))
            {
                return true; // New field added
            }

            string originalValue = original[kvp.Key]?.ToString() ?? string.Empty;
            string updatedValue = kvp.Value?.ToString() ?? string.Empty;

            // If original was empty and updated has value, that's a change
            if (string.IsNullOrWhiteSpace(originalValue) && !string.IsNullOrWhiteSpace(updatedValue))
            {
                return true;
            }

            // If values are different (including updates to existing non-empty values), that's a change
            if (originalValue != updatedValue)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if two dictionaries are equal.
    /// </summary>
    private static bool DictionariesEqual(Dictionary<string, object?> dict1, Dictionary<string, object?> dict2)
    {
        if (dict1.Count != dict2.Count)
        {
            return false;
        }

        foreach (var kvp in dict1)
        {
            if (!dict2.ContainsKey(kvp.Key))
            {
                return false;
            }

            string value1 = kvp.Value?.ToString() ?? string.Empty;
            string value2 = dict2[kvp.Key]?.ToString() ?? string.Empty;

            if (value1 != value2)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Logs the changes made to metadata for debugging.
    /// </summary>
    /// <param name="original">Original metadata.</param>
    /// <param name="updated">Updated metadata.</param>
    /// <param name="filePath">Optional file path for detailed logging.</param>
    private void LogMetadataChanges(Dictionary<string, object?> original, Dictionary<string, object?> updated, string? filePath = null)
    {
        var changes = new List<string>();
        var additions = new List<string>();
        var modifications = new List<string>();
        var deletions = new List<string>();

        // Check for added or modified fields
        foreach (var kvp in updated)
        {
            if (!original.TryGetValue(kvp.Key, out var originalValue))
            {
                // This is a new field
                additions.Add($"{kvp.Key}: '{kvp.Value}'");
                changes.Add($"ADD {kvp.Key}: '{kvp.Value}'");
            }
            else
            {
                string origValue = originalValue?.ToString() ?? string.Empty;
                string updatedValue = kvp.Value?.ToString() ?? string.Empty;

                if (origValue != updatedValue)
                {
                    // This is a modified field
                    modifications.Add($"{kvp.Key}: '{origValue}' → '{updatedValue}'");
                    changes.Add($"MODIFY {kvp.Key}: '{origValue}' → '{updatedValue}'");
                }
            }
        }

        // Check for deleted fields
        foreach (var kvp in original)
        {
            if (!updated.ContainsKey(kvp.Key))
            {
                // This field was removed
                deletions.Add($"{kvp.Key}: '{kvp.Value}'");
                changes.Add($"DELETE {kvp.Key}: '{kvp.Value}'");
            }
        }

        if (changes.Count > 0)
        {
            string fileContext = filePath != null ? $" for file {Path.GetFileName(filePath)}" : string.Empty;
            string filePath_full = filePath != null ? $"{filePath}" : "unknown file";

            // Create detailed summary of changes for this file
            var metadataSummary = new StringBuilder();
            metadataSummary.AppendLine($"--- Metadata changes for {filePath_full} ---");

            if (additions.Count > 0)
            {
                metadataSummary.AppendLine("  Fields to add:");
                foreach (var addition in additions)
                {
                    metadataSummary.AppendLine($"    + {addition}");
                }
            }

            if (modifications.Count > 0)
            {
                metadataSummary.AppendLine("  Fields to modify:");
                foreach (var modification in modifications)
                {
                    metadataSummary.AppendLine($"    * {modification}");
                }
            }

            if (deletions.Count > 0)
            {
                metadataSummary.AppendLine("  Fields to delete:");
                foreach (var deletion in deletions)
                {
                    metadataSummary.AppendLine($"    - {deletion}");
                }
            } // Log the detailed changes at debug level for the log file

            _logger.LogDebug(
                $"Metadata changes for file:\n{metadataSummary.ToString()}");

            // Log a summary line at info level - elevated from debug so it appears in console with --verbose
            _logger.LogInformation(
                $"Metadata change summary{fileContext}: +{additions.Count} ~{modifications.Count} -{deletions.Count}");

            // Keep detailed key-value changes at debug level for log file
            if (additions.Count > 0)
            {
                _logger.LogDebug(
                    $"ADD operations{fileContext}: {string.Join(", ", additions)}");
            }

            if (modifications.Count > 0)
            {
                _logger.LogDebug(
                    $"MODIFY operations{fileContext}: {string.Join(", ", modifications)}");
            }

            if (deletions.Count > 0)
            {
                _logger.LogDebug(
                    $"DELETE operations{fileContext}: {string.Join(", ", deletions)}");
            }
        }
    }

    /// <summary>    ///. <summary>
    /// Determines the hierarchy level based on the file's template-type or position in the hierarchy.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <returns>The determined hierarchy level (e.g., "main", "program", "course"), or null if it cannot be determined.</returns>
    private string? DetermineHierarchyLevelFromPath(string filePath, Dictionary<string, object?> metadata)
    {
        // First check if template-type is already set and extract hierarchy level from it
        if (metadata.TryGetValue("template-type", out var templateTypeObj) && templateTypeObj != null)
        {
            string templateType = templateTypeObj.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(templateType))
            {
                // Extract hierarchy level from template-type (e.g., "course-index" -> "course")
                string? hierarchyLevel = ExtractHierarchyLevelFromTemplateType(templateType);
                if (!string.IsNullOrEmpty(hierarchyLevel))
                {
                    return hierarchyLevel;
                }
            }
        }

        // If no template-type or it doesn't indicate hierarchy, determine from file position
        string fileName = Path.GetFileName(filePath);
        string folderName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? string.Empty);

        // Check if this is likely an index file (filename matches folder name)
        string fileBaseName = Path.GetFileNameWithoutExtension(fileName);
        bool isIndexFile = fileBaseName.Equals(folderName, StringComparison.OrdinalIgnoreCase) ||
                          fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase);

        if (!isIndexFile)
        {
            // For non-index files, don't determine hierarchy level
            return null;
        }


        // Get the vault root from the metadata detector
        string? vaultRoot = _metadataDetector.VaultRoot;
        if (string.IsNullOrWhiteSpace(vaultRoot))
        {
            // Cannot determine hierarchy without a valid vault root
            return null;
        }

        // Calculate the relative path from vault root
        string relativePath = Path.GetRelativePath(vaultRoot, filePath);

        // Count the directory depth (number of path separators)
        // Subtract 1 because the file itself doesn't count as a level
        int pathDepth = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length - 1;

        // Determine hierarchy level based on directory depth:
        // Depth 0: vault root -> main
        // Depth 1: program level -> program
        // Depth 2: course level -> course
        // Depth 3: class level -> class
        // Depth 4+: module level -> module
        return pathDepth switch
        {
            0 => "main",      // Files at vault root
            1 => "program",   // Files in program folder
            2 => "course",    // Files in course folder
            3 => "class",     // Files in class folder
            _ => "module",     // Files at module level or deeper
        };
    }

    /// <summary>
    /// Extracts the hierarchy level from a template-type string.
    /// </summary>
    /// <param name="templateType">The template-type value (e.g., "course", "video-reference-note").</param>
    /// <returns>The hierarchy level (e.g., "course") or null if not a hierarchy template.</returns>
    private static string? ExtractHierarchyLevelFromTemplateType(string templateType)
    {
        if (string.IsNullOrEmpty(templateType))
        {
            return null;
        }

        // Handle new naming convention - index templates use simple names
        return templateType switch
        {
            "main" => "main",
            "program" => "program",
            "course" => "course",
            "class" => "class",
            "module" => "module",
            "lesson" => "lesson",
            "case-studies" => "case-studies",
            "readings" => "readings",
            "resources" => "resources",
            "case-study" => "case-study",

            // Legacy support for old naming convention
            "main-index" => "main",
            "program-index" => "program",
            "course-index" => "course",
            "class-index" => "class",
            "module-index" => "module",
            "lesson-index" => "lesson",
            "case-studies-index" => "case-studies",
            "readings-index" => "readings",
            "resources-index" => "resources",
            "case-study-index" => "case-study",
            _ => null, // Non-hierarchy templates like "video-reference-note", etc.
        };
    }
}

/// <summary>
/// Extension methods for Dictionary operations.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Gets a value from dictionary or returns default if key doesn't exist.
    /// </summary>
    /// <returns></returns>
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
        where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}