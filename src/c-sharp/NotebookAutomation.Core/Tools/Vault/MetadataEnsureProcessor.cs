using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Vault
{
    /// <summary>
    /// Processor for ensuring metadata in markdown files based on directory structure.
    /// Extracts program/course/class/module/lesson metadata and updates YAML frontmatter.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the MetadataEnsureProcessor class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="yamlHelper">The YAML helper for frontmatter operations.</param>
    /// <param name="metadataDetector">The metadata hierarchy detector.</param>
    /// <param name="structureExtractor">The course structure extractor.</param>
    public class MetadataEnsureProcessor(
        ILogger<MetadataEnsureProcessor> logger,
        AppConfig appConfig,
        IYamlHelper yamlHelper,
        MetadataHierarchyDetector metadataDetector,
        CourseStructureExtractor structureExtractor)
    {
        private readonly ILogger<MetadataEnsureProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly AppConfig _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
        private readonly IYamlHelper _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
        private readonly MetadataHierarchyDetector _metadataDetector = metadataDetector ?? throw new ArgumentNullException(nameof(metadataDetector));
        private readonly CourseStructureExtractor _structureExtractor = structureExtractor ?? throw new ArgumentNullException(nameof(structureExtractor));

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
                _logger.LogWarningWithPath("File not found: {FilePath}", nameof(MetadataEnsureProcessor), filePath);
                return false;
            }

            if (!filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebugWithPath("Skipping non-markdown file: {FilePath}", nameof(MetadataEnsureProcessor), filePath);
                return false;
            }

            try
            {
                _logger.LogDebugWithPath("Processing metadata for file: {FilePath}", nameof(MetadataEnsureProcessor), filePath);

                // Read the existing file content
                string content = await File.ReadAllTextAsync(filePath);

                // Extract existing frontmatter
                string? existingYaml = _yamlHelper.ExtractFrontmatter(content);
                var metadata = existingYaml != null
                    ? _yamlHelper.ParseYamlToDictionary(existingYaml)
                    : new Dictionary<string, object>();

                // Store original metadata for comparison
                var originalMetadata = new Dictionary<string, object>(metadata);

                // Determine template type if not set
                EnsureTemplateType(metadata, filePath);

                // Extract hierarchy information (program, course, class)
                var hierarchyInfo = _metadataDetector.FindHierarchyInfo(filePath);

                // Extract course structure information (module, lesson) into metadata directly
                _structureExtractor.ExtractModuleAndLesson(filePath, metadata);

                // Update metadata with hierarchy information
                MetadataHierarchyDetector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo);

                // Ensure required fields based on template type
                EnsureRequiredFields(metadata);

                // Check if any changes were made
                bool hasChanges = HasMetadataChanged(originalMetadata, metadata, forceOverwrite);

                if (!hasChanges)
                {
                    _logger.LogDebugWithPath("No metadata changes needed for: {FilePath}", nameof(MetadataEnsureProcessor), filePath);
                    return false;
                }                if (dryRun)
                {
                    _logger.LogInformationWithPath("DRY RUN: Would update metadata for: {FilePath}", nameof(MetadataEnsureProcessor), filePath);
                    LogMetadataChanges(originalMetadata, metadata, filePath);
                    return true;
                }

                // Update the file with new frontmatter
                string updatedContent = _yamlHelper.UpdateFrontmatter(content, metadata);
                await File.WriteAllTextAsync(filePath, updatedContent);

                _logger.LogInformationWithPath("Updated metadata for: {FilePath}", nameof(MetadataEnsureProcessor), filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogErrorWithPath(ex, "Failed to process metadata for file: {FilePath}", nameof(MetadataEnsureProcessor), filePath);
                return false;
            }
        }

        /// <summary>
        /// Determines and sets the template-type based on the filename if not already set.
        /// </summary>
        /// <param name="metadata">The metadata dictionary to update.</param>
        /// <param name="filePath">The file path to analyze.</param>
        private static void EnsureTemplateType(Dictionary<string, object> metadata, string filePath)
        {
            // If template-type is already set, don't override it
            if (metadata.ContainsKey("template-type") && !string.IsNullOrEmpty(metadata["template-type"]?.ToString()))
            {
                return;
            }

            string fileName = Path.GetFileName(filePath);
            string directory = Path.GetDirectoryName(filePath) ?? "";

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
                return "";
            }

            // Default fallback - could be a general note
            return "";
        }

        /// <summary>
        /// Ensures required fields are present based on the template type.
        /// </summary>
        /// <param name="metadata">The metadata dictionary to update.</param>
        private void EnsureRequiredFields(Dictionary<string, object> metadata)
        {
            string templateType = metadata.GetValueOrDefault("template-type", "")?.ToString() ?? "";

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
        private static void EnsurePdfReferenceFields(Dictionary<string, object> metadata)
        {
            var requiredFields = new Dictionary<string, object>
            {
                ["type"] = "note/case-study",
                ["comprehension"] = 0,
                ["status"] = "unread",
                ["completion-date"] = "",
                ["date-modified"] = "",
                ["date-review"] = "",
                ["onedrive-shared-link"] = "",
                ["onedrive_fullpath_file_reference"] = "",
                ["pdf-size"] = "",
                ["pdf-uploaded"] = "",
                ["page-count"] = "",
                ["pages"] = "",
                ["authors"] = "",
                ["tags"] = ""
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
        private static void EnsureVideoReferenceFields(Dictionary<string, object> metadata)
        {
            var requiredFields = new Dictionary<string, object>
            {
                ["type"] = "note/video-note",
                ["comprehension"] = 0,
                ["status"] = "unwatched",
                ["completion-date"] = "",
                ["date-modified"] = "",
                ["date-review"] = "",
                ["onedrive-shared-link"] = "",
                ["onedrive_fullpath_file_reference"] = "",
                ["publication-year"] = "",
                ["video-codec"] = "",
                ["video-duration"] = "00:00:00",
                ["video-resolution"] = "",
                ["video-size"] = "0 MB",
                ["video-uploaded"] = "",
                ["author"] = "",
                ["tags"] = ""
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
        private static void EnsureResourceReadingFields(Dictionary<string, object> metadata)
        {
            var requiredFields = new Dictionary<string, object>
            {
                ["type"] = "note/reading",
                ["comprehension"] = 0,
                ["status"] = "unread",
                ["completion-date"] = "",
                ["date-modified"] = "",
                ["date-review"] = "",
                ["onedrive-shared-link"] = "",
                ["onedrive_fullpath_file_reference"] = "",
                ["page-count"] = "",
                ["pages"] = "",
                ["authors"] = "",
                ["tags"] = ""
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
        private static void EnsureBasicFields(Dictionary<string, object> metadata)
        {
            var requiredFields = new Dictionary<string, object>
            {
                ["tags"] = ""
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
        /// <param name="forceOverwrite">Whether to force overwrite existing values.</param>
        /// <returns>True if changes were made.</returns>
        private static bool HasMetadataChanged(Dictionary<string, object> original, Dictionary<string, object> updated, bool forceOverwrite)
        {
            // If force overwrite is enabled, we consider changes if any fields in updated are different
            if (forceOverwrite)
            {
                return !DictionariesEqual(original, updated);
            }

            // Otherwise, only check if new fields were added or empty fields were populated
            foreach (var kvp in updated)
            {
                if (!original.ContainsKey(kvp.Key))
                {
                    return true; // New field added
                }

                string originalValue = original[kvp.Key]?.ToString() ?? "";
                string updatedValue = kvp.Value?.ToString() ?? "";

                // If original was empty and updated has value, that's a change
                if (string.IsNullOrWhiteSpace(originalValue) && !string.IsNullOrWhiteSpace(updatedValue))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if two dictionaries are equal.
        /// </summary>
        private static bool DictionariesEqual(Dictionary<string, object> dict1, Dictionary<string, object> dict2)
        {
            if (dict1.Count != dict2.Count) return false;

            foreach (var kvp in dict1)
            {
                if (!dict2.ContainsKey(kvp.Key)) return false;

                string value1 = kvp.Value?.ToString() ?? "";
                string value2 = dict2[kvp.Key]?.ToString() ?? "";

                if (value1 != value2) return false;
            }

            return true;
        }        /// <summary>
        /// Logs the changes made to metadata for debugging.
        /// </summary>
        /// <param name="original">Original metadata.</param>        
        /// <param name="updated">Updated metadata.</param>
        /// <param name="filePath">Optional file path for detailed logging.</param>
        private void LogMetadataChanges(Dictionary<string, object> original, Dictionary<string, object> updated, string? filePath = null)
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
                    string origValue = originalValue?.ToString() ?? "";
                    string updatedValue = kvp.Value?.ToString() ?? "";

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
                string fileContext = filePath != null ? $" for file {Path.GetFileName(filePath)}" : "";
                string filePath_full = filePath != null ? $"{filePath}" : "unknown file";
                
                // Create detailed summary of changes for this file
                var metadataSummary = new System.Text.StringBuilder();
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
                }                // Log the detailed changes at debug level for the log file
                _logger.LogDebugFormatted("Metadata changes for file:\n{DetailedChanges}", 
                    metadataSummary.ToString());
                
                // Log a summary line at info level - elevated from debug so it appears in console with --verbose
                _logger.LogInformationFormatted("Metadata change summary{FileContext}: +{Adds} ~{Modifies} -{Deletes}", 
                    fileContext,
                    additions.Count,
                    modifications.Count,
                    deletions.Count);
                
                // Keep detailed key-value changes at debug level for log file
                if (additions.Count > 0)
                {
                    _logger.LogDebugFormatted("ADD operations{FileContext}: {AddedFields}", 
                        fileContext,
                        string.Join(", ", additions));
                }
                
                if (modifications.Count > 0)
                {
                    _logger.LogDebugFormatted("MODIFY operations{FileContext}: {ModifiedFields}", 
                        fileContext,
                        string.Join(", ", modifications));
                }
                
                if (deletions.Count > 0)
                {
                    _logger.LogDebugFormatted("DELETE operations{FileContext}: {DeletedFields}", 
                        fileContext,
                        string.Join(", ", deletions));
                }
            }
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
        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
            where TKey : notnull
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
