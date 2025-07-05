// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Processor for ensuring metadata in markdown files based on directory structure and file characteristics.
/// This class automatically analyzes markdown files to extract and populate YAML frontmatter metadata
/// including hierarchy information (program/course/class/module/lesson), template types, and required fields.
/// </summary>
/// <remarks>
/// <para>
/// The MetadataEnsureProcessor is designed to intelligently analyze markdown files within a structured
/// educational content directory hierarchy and ensure they contain appropriate metadata. It performs
/// several key operations:
/// </para>
/// <list type="bullet">
/// <item><description>Automatic template type detection based on filename patterns and directory structure</description></item>
/// <item><description>Hierarchy extraction from directory paths (program, course, class, module, lesson)</description></item>
/// <item><description>Population of template-specific required fields</description></item>
/// <item><description>Smart change detection to avoid unnecessary file modifications</description></item>
/// <item><description>Comprehensive logging for debugging and auditing</description></item>
/// </list>
/// <para>
/// The processor supports various template types including PDF references, video references,
/// reading materials, and instruction files. Each template type has specific metadata requirements
/// that are automatically enforced.
/// </para>
/// <para>
/// Template Type Detection Patterns:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Pattern</term>
/// <description>Template Type</description>
/// </listheader>
/// <item>
/// <term>Files ending with "-instructions.md"</term>
/// <description>note/instruction</description>
/// </item>
/// <item>
/// <term>Files in "reading" directories or containing "reading"</term>
/// <description>resource-reading</description>
/// </item>
/// <item>
/// <term>Files in "case studies" directories or case study patterns</term>
/// <description>pdf-reference</description>
/// </item>
/// <item>
/// <term>Files with associated PDF files</term>
/// <description>pdf-reference</description>
/// </item>
/// <item>
/// <term>Files with associated video files (.mp4, .mov, .avi)</term>
/// <description>video-reference</description>
/// </item>
/// </list>
/// <para>
/// Change Detection Logic:
/// When forceOverwrite is false (default), the processor only updates empty or missing fields,
/// preserving existing user-entered values. When forceOverwrite is true, all fields are updated
/// to match template requirements, potentially overwriting existing values.
/// </para>
/// <para>
/// Error Handling:
/// The processor includes comprehensive error handling with graceful degradation. Failures in
/// processing individual files are logged but do not prevent processing of other files.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage with dependency injection
/// var processor = serviceProvider.GetService&lt;MetadataEnsureProcessor&gt;();
///
/// // Process a single file
/// bool wasUpdated = await processor.EnsureMetadataAsync("/path/to/file.md");
///
/// // Dry run to preview changes
/// bool wouldUpdate = await processor.EnsureMetadataAsync("/path/to/file.md", dryRun: true);
///
/// // Force overwrite existing metadata
/// bool wasUpdated = await processor.EnsureMetadataAsync("/path/to/file.md", forceOverwrite: true);
/// </code>
/// </example>
/// <param name="_logger">Logger instance for diagnostic and error reporting.</param>
/// <param name="_yamlHelper">YAML helper service for parsing and manipulating frontmatter.</param>
/// <param name="_metadataDetector">Metadata hierarchy detector for extracting directory structure information.</param>
/// <param name="_structureExtractor">Course structure extractor for module and lesson identification.</param>
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
    /// Ensures metadata is properly set in a markdown file based on its location and characteristics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs a comprehensive analysis of the specified markdown file to ensure it contains
    /// appropriate YAML frontmatter metadata. The process includes:
    /// </para>
    /// <list type="number">
    /// <item><description>File validation (exists, is markdown file)</description></item>
    /// <item><description>Reading existing content and extracting current frontmatter</description></item>
    /// <item><description>Template type determination based on filename and directory patterns</description></item>
    /// <item><description>Hierarchy information extraction (program, course, class, module, lesson)</description></item>
    /// <item><description>Population of template-specific required fields</description></item>
    /// <item><description>Change detection and conditional file updates</description></item>
    /// </list>
    /// <para>
    /// The method respects existing metadata values unless forceOverwrite is true. In normal operation,
    /// only empty or missing fields are populated, preserving any user-entered content.
    /// </para>
    /// <para>
    /// Dry run mode allows previewing changes without modifying files, which is useful for validation
    /// and bulk operation planning.
    /// </para>
    /// </remarks>
    /// <param name="filePath">
    /// The absolute path to the markdown file to process. The file must exist and have a .md extension.
    /// The path is used for hierarchy extraction and file validation.
    /// </param>
    /// <param name="forceOverwrite">
    /// When false (default), only empty or missing metadata fields are populated, preserving existing values.
    /// When true, all metadata fields are updated to match template requirements, potentially overwriting
    /// existing user content. Use with caution in automated scenarios.
    /// </param>
    /// <param name="dryRun">
    /// When true, the method simulates the operation without making actual file changes. This mode is
    /// useful for previewing changes, validation, and determining which files would be modified in
    /// a bulk operation. All logging and change detection still occurs.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// <list type="bullet">
    /// <item><description>true if the file was updated (or would be updated in dry run mode)</description></item>
    /// <item><description>false if no changes were needed or if an error occurred</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when filePath is null, empty, or points to a non-markdown file.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified file does not exist.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when file I/O operations fail (insufficient permissions, file locked, etc.).
    /// </exception>
    /// <exception cref="YamlException">
    /// Thrown when existing YAML frontmatter is malformed and cannot be parsed.
    /// </exception>
    /// <example>
    /// <code>
    /// // Basic processing
    /// bool updated = await processor.EnsureMetadataAsync(@"C:\course\module1\lesson.md");
    ///
    /// // Dry run to preview changes
    /// bool wouldUpdate = await processor.EnsureMetadataAsync(
    ///     @"C:\course\module1\lesson.md",
    ///     dryRun: true);
    ///
    /// // Force update all metadata
    /// bool updated = await processor.EnsureMetadataAsync(
    ///     @"C:\course\module1\lesson.md",
    ///     forceOverwrite: true);
    ///
    /// // Error handling
    /// try
    /// {
    ///     bool result = await processor.EnsureMetadataAsync(filePath);
    ///     if (result)
    ///     {
    ///         Console.WriteLine("File updated successfully");
    ///     }
    /// }
    /// catch (FileNotFoundException)
    /// {
    ///     Console.WriteLine("File not found");
    /// }
    /// catch (IOException ex)
    /// {
    ///     Console.WriteLine($"I/O error: {ex.Message}");
    /// }
    /// </code>
    /// </example>
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
                _logger.LogDebug($"DRY RUN: Would update metadata for: {filePath}");
                LogMetadataChanges(originalMetadata, metadata, filePath);
                return true;
            }

            // Update the file with new frontmatter
            string updatedContent = _yamlHelper.UpdateFrontmatter(content, metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));
            await File.WriteAllTextAsync(filePath, updatedContent).ConfigureAwait(false);

            _logger.LogDebug($"Updated metadata for: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process metadata for file: {filePath}");
            return false;
        }
    }

    /// <summary>
    /// Determines and sets the template-type metadata field based on filename patterns and directory structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method analyzes the file path to automatically determine the appropriate template type if one
    /// is not already set. It respects existing template-type values and only sets the field when it's
    /// empty or missing.
    /// </para>
    /// <para>
    /// The template type determination affects which metadata fields are considered required and how
    /// the content should be structured. Different template types have different metadata schemas.
    /// </para>
    /// </remarks>
    /// <param name="metadata">
    /// The metadata dictionary to update. If the dictionary already contains a non-empty "template-type"
    /// key, no changes are made. Otherwise, the determined template type is added to this dictionary.
    /// </param>
    /// <param name="filePath">
    /// The full file path to analyze. Both the filename and directory structure are examined to
    /// determine the appropriate template type based on established patterns.
    /// </param>
    /// <example>
    /// <code>
    /// var metadata = new Dictionary&lt;string, object?&gt;();
    /// processor.EnsureTemplateType(metadata, @"C:\course\readings\chapter1.md");
    /// // Result: metadata["template-type"] = "resource-reading"
    ///
    /// var existingMetadata = new Dictionary&lt;string, object?&gt;
    /// {
    ///     ["template-type"] = "custom-type"
    /// };
    /// processor.EnsureTemplateType(existingMetadata, @"C:\course\readings\chapter1.md");
    /// // Result: metadata["template-type"] remains "custom-type" (no change)
    /// </code>
    /// </example>
    private void EnsureTemplateType(Dictionary<string, object?> metadata, string filePath)
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
    /// Determines the appropriate template type based on filename patterns and directory structure analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the core logic for automatic template type detection. It analyzes both
    /// the filename and directory path to identify patterns that indicate specific content types.
    /// The detection is performed in priority order, with more specific patterns checked first.
    /// </para>
    /// <para>
    /// Detection Priority Order:
    /// </para>
    /// <list type="number">
    /// <item><description>Instruction files (highest priority)</description></item>
    /// <item><description>Reading materials</description></item>
    /// <item><description>Case studies</description></item>
    /// <item><description>Associated file types (PDF, video)</description></item>
    /// <item><description>Index files (require manual classification)</description></item>
    /// </list>
    /// <para>
    /// The method performs case-insensitive pattern matching and checks for file associations
    /// in the same directory (e.g., looking for PDF files with the same base name).
    /// </para>
    /// </remarks>
    /// <param name="fileName">
    /// The filename (without path) to analyze for patterns. Common patterns include suffixes
    /// like "-instructions.md" or keywords like "reading" or "case-study".
    /// </param>
    /// <param name="directory">
    /// The directory path containing the file. Directory names are analyzed for keywords
    /// like "readings", "case studies", etc. that indicate content organization.
    /// </param>
    /// <returns>
    /// The determined template type string, or empty string if no specific pattern is detected.
    /// Common return values include:
    /// <list type="bullet">
    /// <item><description>"note/instruction" - for instruction files</description></item>
    /// <item><description>"resource-reading" - for reading materials</description></item>
    /// <item><description>"pdf-reference" - for PDF-associated content or case studies</description></item>
    /// <item><description>"video-reference" - for video-associated content</description></item>
    /// <item><description>Empty string - for files requiring manual classification</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// // Instruction file detection
    /// string type1 = processor.DetermineTemplateType("setup-instructions.md", @"C:\course\module1");
    /// // Returns: "note/instruction"
    ///
    /// // Reading material detection
    /// string type2 = processor.DetermineTemplateType("chapter1.md", @"C:\course\readings");
    /// // Returns: "resource-reading"
    ///
    /// // Case study detection
    /// string type3 = processor.DetermineTemplateType("analysis.md", @"C:\course\case studies");
    /// // Returns: "pdf-reference"
    ///
    /// // PDF association detection (requires PDF file to exist)
    /// string type4 = processor.DetermineTemplateType("report.md", @"C:\course\docs");
    /// // Returns: "pdf-reference" if "report.pdf" exists in same directory
    ///
    /// // No pattern detected
    /// string type5 = processor.DetermineTemplateType("notes.md", @"C:\course\misc");
    /// // Returns: "" (empty string)
    /// </code>
    /// </example>
    private string DetermineTemplateType(string fileName, string directory)
    {
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string lowerFileName = fileName.ToLowerInvariant();
        string lowerDirectory = directory.ToLowerInvariant();

        _logger.LogDebug($"DetermineTemplateType - fileName: '{fileName}', directory: '{directory}', lowerDirectory: '{lowerDirectory}'");
        // Check for specific filename patterns first
        if (lowerFileName.EndsWith("-instructions.md", StringComparison.OrdinalIgnoreCase) ||
            lowerFileName.Contains("instruction", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug($"Detected instruction file: {fileName}");
            return "note/instruction";
        }

        // Check for reading materials
        if (lowerFileName.Contains("reading", StringComparison.OrdinalIgnoreCase) ||
            lowerDirectory.Contains("reading", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug($"Detected reading file: {fileName}");
            return "resource-reading";
        }

        // Check for case studies based on directory or filename
        if (lowerDirectory.Contains("case studies") ||
            lowerDirectory.Contains("case-studies") ||
            lowerDirectory.Contains("case_studies") ||
            lowerFileName.Contains("case-stud", StringComparison.OrdinalIgnoreCase) ||
            lowerFileName.Contains("case_stud", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug($"Detected case study file: {fileName}");
            return "pdf-reference"; // Case studies are typically PDF references
        }

        // Check for associated file types (assuming the markdown references these files)
        // Look for associated PDF or video files in the same directory
        if (File.Exists(Path.Combine(directory, baseName + ".pdf")))
        {
            _logger.LogDebug($"Detected PDF reference file: {fileName}");
            return "pdf-reference";
        }

        if (File.Exists(Path.Combine(directory, baseName + ".mp4")) ||
            File.Exists(Path.Combine(directory, baseName + ".mov")) ||
            File.Exists(Path.Combine(directory, baseName + ".avi")))
        {
            _logger.LogDebug($"Detected video reference file: {fileName}");
            return "video-reference";
        }

        // Check for index files
        if (fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase))
        {
            // This would need more sophisticated logic to determine the index type
            // For now, return empty and let manual assignment handle it
            _logger.LogDebug($"Detected index file, returning empty: {fileName}");
            return string.Empty;
        }

        // Default fallback - could be a general note
        _logger.LogDebug($"No specific template type detected for: {fileName}");
        return string.Empty;
    }

    /// <summary>
    /// Ensures required metadata fields are present based on the template type and system requirements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method populates missing metadata fields according to template-specific requirements and
    /// system-wide standards. It operates in two phases:
    /// </para>
    /// <list type="number">
    /// <item><description>Universal fields - applied to all template types</description></item>
    /// <item><description>Template-specific fields - based on the template-type value</description></item>
    /// </list>
    /// <para>
    /// Universal Fields:
    /// </para>
    /// <list type="bullet">
    /// <item><description>auto-generated-state: "writable"</description></item>
    /// <item><description>date-created: Current date in YYYY-MM-DD format</description></item>
    /// <item><description>publisher: "University of Illinois at Urbana-Champaign"</description></item>
    /// </list>
    /// <para>
    /// Template-specific field sets are applied based on the template-type value. Each template
    /// type has a predefined schema that ensures consistency across similar content types.
    /// Fields are only added if they don't already exist, preserving any existing values.
    /// </para>
    /// </remarks>
    /// <param name="metadata">
    /// The metadata dictionary to update with required fields. Existing fields are preserved
    /// and only missing fields are added. The dictionary is modified in place.
    /// </param>
    /// <example>
    /// <code>
    /// var metadata = new Dictionary&lt;string, object?&gt;
    /// {
    ///     ["template-type"] = "pdf-reference",
    ///     ["title"] = "Existing Title" // This will be preserved
    /// };
    ///
    /// processor.EnsureRequiredFields(metadata);
    ///
    /// // Result: metadata now contains all PDF reference required fields
    /// // plus the existing title, auto-generated-state, date-created, etc.
    /// </code>
    /// </example>
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
    /// Ensures required fields for PDF reference template are present in the metadata dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method adds PDF-specific metadata fields that are required for proper PDF reference handling.
    /// The fields include tracking information for reading status, OneDrive integration, and PDF properties.
    /// </para>
    /// <para>
    /// Fields added by this method include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>type: "note/case-study" - Categorizes as case study content</description></item>
    /// <item><description>comprehension: 0 - Initial comprehension rating</description></item>
    /// <item><description>status: "unread" - Reading status tracking</description></item>
    /// <item><description>completion-date, date-modified, date-review: Date tracking fields</description></item>
    /// <item><description>onedrive-shared-link, onedrive_fullpath_file_reference: OneDrive integration</description></item>
    /// <item><description>pdf-size, pdf-uploaded, page-count, pages: PDF metadata</description></item>
    /// <item><description>authors, tags: Content classification fields</description></item>
    /// </list>
    /// </remarks>
    /// <param name="metadata">
    /// The metadata dictionary to update. Only missing fields are added; existing values are preserved.
    /// </param>
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
    /// Ensures required fields for video reference template are present in the metadata dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method adds video-specific metadata fields that are required for proper video reference handling.
    /// The fields include tracking information for viewing status, OneDrive integration, and video properties.
    /// </para>
    /// <para>
    /// Fields added by this method include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>type: "note/video-note" - Categorizes as video content</description></item>
    /// <item><description>comprehension: 0 - Initial comprehension rating</description></item>
    /// <item><description>status: "unwatched" - Viewing status tracking</description></item>
    /// <item><description>completion-date, date-modified, date-review: Date tracking fields</description></item>
    /// <item><description>onedrive-shared-link, onedrive_fullpath_file_reference: OneDrive integration</description></item>
    /// <item><description>video-duration: "00:00:00" - Video length in HH:MM:SS format</description></item>
    /// <item><description>video-codec, video-resolution, video-size: Technical video properties</description></item>
    /// <item><description>publication-year, video-uploaded: Publication metadata</description></item>
    /// <item><description>author, tags: Content classification fields</description></item>
    /// </list>
    /// </remarks>
    /// <param name="metadata">
    /// The metadata dictionary to update. Only missing fields are added; existing values are preserved.
    /// </param>
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
    /// Ensures required fields for resource reading template are present in the metadata dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method adds reading-specific metadata fields that are required for proper reading material handling.
    /// The fields include tracking information for reading status, OneDrive integration, and document properties.
    /// </para>
    /// <para>
    /// Fields added by this method include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>type: "note/reading" - Categorizes as reading material</description></item>
    /// <item><description>comprehension: 0 - Initial comprehension rating</description></item>
    /// <item><description>status: "unread" - Reading status tracking</description></item>
    /// <item><description>completion-date, date-modified, date-review: Date tracking fields</description></item>
    /// <item><description>onedrive-shared-link, onedrive_fullpath_file_reference: OneDrive integration</description></item>
    /// <item><description>page-count, pages: Document structure information</description></item>
    /// <item><description>authors, tags: Content classification fields</description></item>
    /// </list>
    /// </remarks>
    /// <param name="metadata">
    /// The metadata dictionary to update. Only missing fields are added; existing values are preserved.
    /// </param>
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
    /// Ensures basic required fields for general templates that don't have specific field requirements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides minimal field requirements for templates that don't fall into specific
    /// categories like PDF reference, video reference, or resource reading. It serves as a fallback
    /// to ensure even generic content has basic metadata structure.
    /// </para>
    /// <para>
    /// Currently only adds the "tags" field, which is considered essential for content organization
    /// and searchability across all template types.
    /// </para>
    /// </remarks>
    /// <param name="metadata">
    /// The metadata dictionary to update. Only missing fields are added; existing values are preserved.
    /// </param>
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
    /// Determines whether metadata has changed by comparing original and updated dictionaries,
    /// considering the force overwrite option for change detection logic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements intelligent change detection that respects the forceOverwrite parameter:
    /// </para>
    /// <list type="bullet">
    /// <item><description>When forceOverwrite is true: Any difference between dictionaries is considered a change</description></item>
    /// <item><description>When forceOverwrite is false: Only new fields or population of empty fields counts as changes</description></item>
    /// </list>
    /// <para>
    /// The method performs comprehensive comparison including:
    /// </para>
    /// <list type="number">
    /// <item><description>Field additions (new keys in updated dictionary)</description></item>
    /// <item><description>Field deletions (keys present in original but missing in updated)</description></item>
    /// <item><description>Value changes (different values for existing keys)</description></item>
    /// <item><description>Empty field population (original empty/null, updated has value)</description></item>
    /// </list>
    /// </remarks>
    /// <param name="original">
    /// The original metadata dictionary before processing. Used as baseline for comparison.
    /// </param>
    /// <param name="updated">
    /// The updated metadata dictionary after processing. Compared against original to detect changes.
    /// </param>
    /// <param name="forceOverwrite">
    /// When true, all differences are considered changes. When false, only additions and
    /// empty field population are considered changes, preserving existing user values.
    /// </param>
    /// <returns>
    /// true if changes were detected according to the specified logic; false if no changes detected.
    /// </returns>

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
    /// Performs deep equality comparison between two metadata dictionaries by comparing all key-value pairs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides comprehensive dictionary comparison that:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Compares dictionary sizes first for quick inequality detection</description></item>
    /// <item><description>Validates all keys are present in both dictionaries</description></item>
    /// <item><description>Compares string representations of values for content equality</description></item>
    /// <item><description>Handles null values safely by converting to empty strings</description></item>
    /// </list>
    /// <para>
    /// The comparison is performed using string representation to handle various object types
    /// consistently and avoid type-specific comparison issues.
    /// </para>
    /// </remarks>
    /// <param name="dict1">First dictionary to compare.</param>
    /// <param name="dict2">Second dictionary to compare.</param>
    /// <returns>
    /// true if dictionaries contain exactly the same keys with identical string representations of values;
    /// false otherwise.
    /// </returns>

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
    /// Logs detailed information about metadata changes for debugging and audit purposes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides comprehensive change logging that categorizes modifications into:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Additions: New fields added to metadata</description></item>
    /// <item><description>Modifications: Existing fields with changed values</description></item>
    /// <item><description>Deletions: Fields removed from metadata</description></item>
    /// </list>
    /// <para>
    /// The logging occurs at multiple levels:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Debug level: Detailed change information for log files</description></item>
    /// <item><description>Information level: Summary counts for console output with --verbose</description></item>
    /// </list>
    /// <para>
    /// The method formats changes in a human-readable format showing old → new values
    /// for modifications and clear indicators (+, *, -) for different operation types.
    /// </para>
    /// </remarks>
    /// <param name="original">
    /// The original metadata dictionary before changes. Used to detect deletions and modifications.
    /// </param>
    /// <param name="updated">
    /// The updated metadata dictionary after changes. Used to detect additions and modifications.
    /// </param>
    /// <param name="filePath">
    /// Optional file path for contextual logging. When provided, includes filename in log messages
    /// for easier identification in bulk operations. Can be null for generic change logging.
    /// </param>
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
}

/// <summary>
/// Extension methods for Dictionary operations that provide additional functionality for metadata processing.
/// </summary>
/// <remarks>
/// These extension methods provide commonly used dictionary operations that are not available in the
/// standard Dictionary class, particularly for safe value retrieval with fallback defaults.
/// </remarks>
public static class DictionaryExtensions
{
    /// <summary>
    /// Safely retrieves a value from the dictionary or returns a default value if the key doesn't exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides safe dictionary access that prevents KeyNotFoundException when accessing
    /// potentially missing keys. It's particularly useful when working with metadata dictionaries
    /// where fields may or may not be present.
    /// </para>
    /// <para>
    /// The method uses the TryGetValue pattern internally for optimal performance and returns
    /// the specified default value (or type default) when the key is not found.
    /// </para>
    /// </remarks>
    /// <typeparam name="TKey">The type of keys in the dictionary. Must be non-null.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to search in.</param>
    /// <param name="key">The key to look for in the dictionary.</param>
    /// <param name="defaultValue">
    /// The value to return if the key is not found. If not specified, uses the default value for TValue.
    /// </param>
    /// <returns>
    /// The value associated with the key if found; otherwise the specified defaultValue or type default.
    /// </returns>
    /// <example>
    /// <code>
    /// var metadata = new Dictionary&lt;string, object?&gt;
    /// {
    ///     ["title"] = "Example Title",
    ///     ["count"] = 42
    /// };
    ///
    /// // Get existing value
    /// string title = metadata.GetValueOrDefault("title", "No Title")?.ToString() ?? "No Title";
    /// // Result: "Example Title"
    ///
    /// // Get missing value with default
    /// string description = metadata.GetValueOrDefault("description", "No Description")?.ToString() ?? "No Description";
    /// // Result: "No Description"
    ///
    /// // Get missing value with type default
    /// object? status = metadata.GetValueOrDefault("status");
    /// // Result: null (default for object?)
    /// </code>
    /// </example>
    public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)
        where TKey : notnull
    {
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
