// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Processor for generating comprehensive vault index files with hierarchical navigation and dynamic content organization.
/// </summary>
/// <remarks>
/// <para>
/// The VaultIndexProcessor class is the core engine for generating index files within Obsidian vaults,
/// providing intelligent hierarchy detection, template-based content generation, and advanced navigation
/// features. It serves as a central component in the vault organization system.
/// </para>
/// <para>
/// Key Capabilities:
/// </para>
/// <list type="bullet">
/// <item><description>Automatic hierarchy level detection using vault path analysis</description></item>
/// <item><description>Template-based index generation with customizable frontmatter</description></item>
/// <item><description>Content categorization by file type and metadata analysis</description></item>
/// <item><description>Dynamic navigation link generation with back/home references</description></item>
/// <item><description>Obsidian Bases integration for advanced query capabilities</description></item>
/// <item><description>Dry run support for preview and validation scenarios</description></item>
/// </list>
/// <para>
/// Hierarchy System:
/// The processor uses a 1-based hierarchy level system for template selection:
/// </para>
/// <list type="number">
/// <item><description>Level 1: Vault root (main) - Primary entry point</description></item>
/// <item><description>Level 2: Program level - Top-level organizational structure</description></item>
/// <item><description>Level 3: Course level - Subject or domain grouping</description></item>
/// <item><description>Level 4: Class level - Specific course instances with Bases integration</description></item>
/// <item><description>Level 5: Module level - Content groupings within classes</description></item>
/// <item><description>Level 6+: Lesson level - Individual learning units</description></item>
/// </list>
/// <para>
/// Template Integration:
/// Each hierarchy level corresponds to specific templates that define the structure
/// and metadata for generated index files. Templates are resolved using the IMetadataTemplateManager
/// and can include custom fields, banners, and content organization patterns.
/// </para>
/// <para>
/// Content Analysis:
/// The processor performs intelligent content analysis by examining file metadata,
/// filename patterns, and frontmatter to categorize content into types such as readings,
/// videos, transcripts, assignments, and discussions.
/// </para>
/// <para>
/// Navigation System:
/// Generates contextual navigation with back links to parent levels, home links to
/// vault root, and dashboard/assignment shortcuts for enhanced user experience.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic index generation
/// var processor = serviceProvider.GetService&lt;VaultIndexProcessor&gt;();
/// bool success = await processor.GenerateIndexAsync(
///     @"C:\vault\MBA\Finance\Corporate-Finance",
///     @"C:\vault",
///     forceOverwrite: false,
///     dryRun: false);
///
/// // Template type determination
/// string templateType = processor.DetermineTemplateType(4, "Corporate-Finance");
/// Console.WriteLine($"Template type: {templateType}"); // Output: "class"
///
/// // Dry run for preview
/// bool wouldGenerate = await processor.GenerateIndexAsync(
///     folderPath,
///     vaultPath,
///     dryRun: true);
/// </code>
/// </example>
public class VaultIndexProcessor(
    ILogger<VaultIndexProcessor> logger,
    IMetadataTemplateManager templateManager,
    IMetadataHierarchyDetector hierarchyDetector,
    ICourseStructureExtractor structureExtractor,
    IYamlHelper yamlHelper,
    MarkdownNoteBuilder noteBuilder,
    AppConfig appConfig,
    string vaultRootPath = "") : IVaultIndexProcessor
{
    private readonly ILogger<VaultIndexProcessor> _logger = logger;
    private readonly IMetadataTemplateManager _templateManager = templateManager;
    private readonly IMetadataHierarchyDetector _hierarchyDetector = hierarchyDetector;
    private readonly ICourseStructureExtractor _structureExtractor = structureExtractor;
    private readonly IYamlHelper _yamlHelper = yamlHelper;
    private readonly MarkdownNoteBuilder _noteBuilder = noteBuilder;    private readonly string _defaultVaultRootPath = !string.IsNullOrEmpty(vaultRootPath)
        ? vaultRootPath
        : appConfig.Paths.NotebookVaultFullpathRoot;

    // Cache for root index filename to avoid expensive file system lookups
    private string? _cachedRootIndexFilename;
    private string? _cachedVaultPath;

    /// <summary>
    /// Generates a comprehensive index file for the specified folder with intelligent hierarchy detection and content organization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the primary method for generating index files within a vault structure. It orchestrates
    /// the complete index generation workflow including hierarchy analysis, template resolution,
    /// content scanning, metadata application, and file generation.
    /// </para>
    /// <para>
    /// Processing Workflow:
    /// </para>
    /// <list type="number">
    /// <item><description>Validates vault path and folder structure</description></item>
    /// <item><description>Calculates hierarchy level using path analysis</description></item>
    /// <item><description>Determines appropriate template type based on hierarchy and folder name</description></item>
    /// <item><description>Resolves template metadata and frontmatter structure</description></item>
    /// <item><description>Scans folder content and categorizes files by type</description></item>
    /// <item><description>Generates navigation links and content sections</description></item>
    /// <item><description>Applies hierarchy metadata and creates final index content</description></item>
    /// <item><description>Writes the generated index file to disk (unless dry run)</description></item>
    /// </list>
    /// <para>
    /// Hierarchy Detection:
    /// Uses IMetadataHierarchyDetector to calculate the folder's position in the vault structure,
    /// converting from 0-based (detector) to 1-based (processor) level system for template compatibility.
    /// </para>
    /// <para>
    /// Template Resolution:
    /// Templates are selected based on hierarchy level and special folder name patterns.
    /// Each template defines the frontmatter structure, banners, and content organization.
    /// </para>
    /// <para>
    /// Content Integration:
    /// For class-level indices (level 4), integrates Obsidian Bases query blocks for dynamic
    /// content discovery and filtering. Other levels use static content organization.
    /// </para>
    /// <para>
    /// Error Handling:
    /// Comprehensive error handling with detailed logging ensures graceful degradation
    /// when encountering filesystem issues, template problems, or content analysis errors.
    /// </para>
    /// </remarks>
    /// <param name="folderPath">
    /// The absolute path to the folder for which to generate an index file.
    /// Must be a valid, accessible directory within the vault structure.
    /// The index file will be created in this directory with a name matching the folder.
    /// </param>
    /// <param name="vaultPath">
    /// The absolute path to the vault root directory for hierarchy calculation.
    /// Used as the reference point for determining relative hierarchy levels.
    /// If empty or null, uses the default vault path from application configuration.
    /// </param>
    /// <param name="forceOverwrite">
    /// When true, regenerates the index file even if it already exists, overwriting existing content.
    /// When false, skips generation if an index file is already present.
    /// Default is false to preserve existing user customizations.
    /// </param>
    /// <param name="dryRun">
    /// When true, simulates the index generation process without creating actual files.
    /// Useful for previewing changes, validation, and testing scenarios.
    /// All processing steps are performed except the final file write operation.
    /// Default is false for normal operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous index generation operation.
    /// The task result contains:
    /// <list type="bullet">
    /// <item><description>true: Index was successfully generated or would be generated (dry run)</description></item>
    /// <item><description>false: Index generation was skipped (file exists, no force) or failed</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the specified folderPath does not exist or is inaccessible.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application lacks permissions to read the folder or write the index file.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when file system operations fail due to disk space, file locks, or other I/O issues.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when template resolution fails or required configuration is missing.
    /// </exception>
    /// <example>
    /// <code>
    /// // Generate index for a class folder
    /// var processor = serviceProvider.GetService&lt;VaultIndexProcessor&gt;();
    /// bool success = await processor.GenerateIndexAsync(
    ///     @"C:\vault\MBA\Finance\Corporate-Finance",
    ///     @"C:\vault");
    ///
    /// if (success)
    /// {
    ///     Console.WriteLine("Index generated successfully");
    /// }
    ///
    /// // Force regeneration of existing index
    /// await processor.GenerateIndexAsync(
    ///     folderPath,
    ///     vaultPath,
    ///     forceOverwrite: true);
    ///
    /// // Preview index generation without creating files
    /// bool wouldGenerate = await processor.GenerateIndexAsync(
    ///     folderPath,
    ///     vaultPath,
    ///     dryRun: true);
    ///
    /// Console.WriteLine($"Would generate: {wouldGenerate}");
    /// </code>
    /// </example>
    public async Task<bool> GenerateIndexAsync(
        string folderPath,
        string vaultPath,
        bool forceOverwrite = false,
        bool dryRun = false)
    {
        try
        {
            _logger.LogDebug($"Generating index for folder: {folderPath}");
            _logger.LogInformation("=== GENERATING INDEX ===");
            _logger.LogInformation($"Folder Path: {folderPath}");
            _logger.LogInformation($"Vault Path: {vaultPath}");
            _logger.LogDebug($"Starting GenerateIndexAsync - FolderPath: {folderPath}, VaultPath: {vaultPath}");

            // Validate vault path - use _defaultVaultRootPath if not provided
            if (string.IsNullOrEmpty(vaultPath))
            {
                if (string.IsNullOrEmpty(_defaultVaultRootPath))
                {
                    _logger.LogError("Cannot determine vault root path. Neither vaultPath parameter nor AppConfig.Paths.NotebookVaultFullpathRoot is provided.");
                    return false;
                }

                _logger.LogDebug($"No vault path provided, using default from configuration: {_defaultVaultRootPath}");
                vaultPath = _defaultVaultRootPath;
            }

            // Calculate hierarchy level using IMetadataHierarchyDetector
            int metadataHierarchyLevel = _hierarchyDetector.CalculateHierarchyLevel(folderPath, vaultPath);

            // Convert from 0-based (MetadataHierarchyDetector) to 1-based (legacy VaultIndexProcessor) level system
            int hierarchyLevel = metadataHierarchyLevel + 1;

            _logger.LogInformation($"Calculated hierarchy level: metadataHierarchyLevel={metadataHierarchyLevel} (0-based), adjustedLevel={hierarchyLevel} (1-based) for folder: {folderPath}");

            // Create index file name based on folder name
            string folderName = Path.GetFileName(folderPath) ?? "Index";
            // Determine template type based on hierarchy level and folder name
            string templateType = DetermineTemplateType(hierarchyLevel, folderName);
            _logger.LogInformation($"Determined template type: {templateType} for folder: {folderName} at level: {hierarchyLevel}");
            _logger.LogDebug($"Template type '{templateType}' determined for '{folderName}' at level {hierarchyLevel}"); // Get template using the actual available method
            var template = _templateManager.GetTemplate(templateType);
            if (template == null)
            {
                _logger.LogWarning($"Template not found for type: {templateType}");
                return false;
            }

            _logger.LogDebug($"Template keys: {string.Join(", ", template.Keys)}");
            if (template.ContainsKey("program"))
            {
                _logger.LogDebug($"Template contains program: {template["program"]}");
            }

            if (template.ContainsKey("course"))
            {
                _logger.LogDebug($"Template contains course: {template["course"]}");
            }

            if (template.ContainsKey("class"))
            {
                _logger.LogDebug($"Template contains class: {template["class"]}");
            }

            string indexFileName = $"{folderName}.md";
            string indexFilePath = Path.Combine(folderPath, indexFileName);

            // Debug output to understand skipping behavior
            bool fileExists = File.Exists(indexFilePath);
            _logger.LogDebug($"Checking index file: {indexFilePath}");
            _logger.LogDebug($"File exists: {fileExists}");
            _logger.LogDebug($"ForceOverwrite: {forceOverwrite}");
            _logger.LogDebug($"Will skip: {fileExists && !forceOverwrite}");

            // Check if index already exists and force is not set
            if (fileExists && !forceOverwrite)
            {
                _logger.LogInformation($"Skipping index file (already exists, use --force to overwrite): {indexFilePath}");
                _logger.LogDebug($"SKIPPING - File exists and force is false");
                return false;
            }

            if (dryRun)
            {
                _logger.LogInformation($"DRY RUN: Would generate index file: {indexFilePath}");
                return true;
            } // Scan folder for content

            var files = await ScanFolderContentAsync(folderPath, vaultPath).ConfigureAwait(false);

            // Get hierarchy info
            var hierarchyInfo = _hierarchyDetector.FindHierarchyInfo(folderPath);            // Generate index content
            string indexContent = await GenerateIndexContentAsync(
                folderPath,
                vaultPath,
                template,
                files,
                hierarchyInfo,
                hierarchyLevel).ConfigureAwait(false);

            // Write index file
            await File.WriteAllTextAsync(indexFilePath, indexContent).ConfigureAwait(false);

            _logger.LogInformation($"Generated index file: {indexFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating index for folder: {folderPath}");
            return false;
        }
    }

    /// <summary>
    /// Hierarchy levels info - important for understanding template selection
    /// </summary>
    /// <remarks>
    /// IMPORTANT NOTE: VaultIndexProcessor uses a 1-based hierarchy level system (for historical reasons):
    /// - Level 1: Vault root (main) - mapped from MetadataHierarchyDetector level 0
    /// - Level 2: Program level (program) - mapped from MetadataHierarchyDetector level 1
    /// - Level 3: Course level (course) - mapped from MetadataHierarchyDetector level 2
    /// - Level 4: Class level (class) - mapped from MetadataHierarchyDetector level 3
    /// - Level 5: Module level (module) - mapped from MetadataHierarchyDetector level 4
    /// - Level 6: Lesson level (lesson) - mapped from MetadataHierarchyDetector level 5
    ///
    /// The IMetadataHierarchyDetector uses 0-based levels (0=vault root),
    /// so we add 1 to convert from 0-based to 1-based level system.
    /// The hierarchy calculation is now performed directly via IMetadataHierarchyDetector.
    /// </remarks>
    ///
    /// <summary>
    /// Determines the appropriate template type based on hierarchy level and folder name patterns with intelligent special case handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the core template selection logic that maps vault structure to appropriate
    /// index templates. It uses a combination of hierarchy level analysis and folder name pattern matching
    /// to ensure the most suitable template is selected for each context.
    /// </para>
    /// <para>
    /// Template Selection Strategy:
    /// </para>
    /// <list type="number">
    /// <item><description>Check for main program folders at level 1 (vault root level)</description></item>
    /// <item><description>Apply special folder name pattern matching for content-specific folders</description></item>
    /// <item><description>Use standard hierarchy mapping for typical vault structures</description></item>
    /// <item><description>Default to module template for deep or unrecognized structures</description></item>
    /// </list>
    /// <para>
    /// Special Folder Recognition:
    /// The method recognizes common educational content patterns including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Lesson containers: "lesson", "lessons" → lesson template</description></item>
    /// <item><description>Module containers: "module", "modules", "readings", "resources" → module template</description></item>
    /// <item><description>Case study containers: "case-studies", "case-study" → module template</description></item>
    /// <item><description>Assignment containers: "assignment", "assignments", "project", "projects" → module template</description></item>
    /// <item><description>Live class containers: "live class", "live-class" → module template</description></item>
    /// </list>
    /// <para>
    /// Hierarchy Mapping:
    /// Uses a 1-based level system where each level corresponds to a specific organizational tier:
    /// Level 1 (main) → Level 2 (program) → Level 3 (course) → Level 4 (class) → Level 5+ (module/lesson)
    /// </para>
    /// <para>
    /// Template Types:
    /// </para>
    /// <list type="bullet">
    /// <item><description>main: Vault root indices with program listings</description></item>
    /// <item><description>program: Program-level indices with course listings</description></item>
    /// <item><description>course: Course-level indices with class listings</description></item>
    /// <item><description>class: Class-level indices with Bases integration</description></item>
    /// <item><description>module: Module-level indices with content categorization</description></item>
    /// <item><description>lesson: Lesson-level indices with focused content</description></item>
    /// </list>
    /// </remarks>
    /// <param name="hierarchyLevel">
    /// The 1-based hierarchy level of the folder relative to the vault root.
    /// Level 1 represents the vault root, with each subsequent level representing
    /// deeper organizational tiers (program, course, class, module, lesson).
    /// </param>
    /// <param name="folderName">
    /// Optional folder name for special case detection and pattern matching.
    /// Used to identify content-specific folders that may override standard hierarchy mapping.
    /// Can be null for pure hierarchy-based template selection.
    /// </param>
    /// <returns>
    /// A template type identifier string corresponding to available templates:
    /// <list type="bullet">
    /// <item><description>"main" - For vault root level indices</description></item>
    /// <item><description>"program" - For program level indices</description></item>
    /// <item><description>"course" - For course level indices</description></item>
    /// <item><description>"class" - For class level indices with Bases integration</description></item>
    /// <item><description>"module" - For module level indices and content containers</description></item>
    /// <item><description>"lesson" - For lesson level indices and focused content</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// // Standard hierarchy-based template selection
    /// string mainTemplate = processor.DetermineTemplateType(1); // Returns "main"
    /// string courseTemplate = processor.DetermineTemplateType(3); // Returns "course"
    /// string classTemplate = processor.DetermineTemplateType(4); // Returns "class"
    ///
    /// // Special folder name pattern matching
    /// string lessonTemplate = processor.DetermineTemplateType(5, "Lesson-01-Introduction");
    /// // Returns "lesson" due to "lesson" in folder name
    ///
    /// string moduleTemplate = processor.DetermineTemplateType(6, "Case-Studies");
    /// // Returns "module" due to "case-studies" pattern
    ///
    /// // Deep hierarchy defaults to module
    /// string deepTemplate = processor.DetermineTemplateType(10);
    /// // Returns "module" for unrecognized deep levels
    /// </code>
    /// </example>
    public string DetermineTemplateType(int hierarchyLevel, string? folderName = null)
    {
        // Main program folders (level 1) that are identified as such get main template type
        if (hierarchyLevel == 1 && folderName != null)
        {
            _logger.LogDebug("Main program folder detected, using main template type");
            return "main";
        }

        // Check for special folder names that override hierarchy-based template type
        if (!string.IsNullOrEmpty(folderName))
        {
            string lowerFolderName = folderName.ToLowerInvariant();
            // Special folder types that can appear at any level beyond course level (4+)            // Special folder handling for content folders
            if (hierarchyLevel >= 4)
            {
                // Check for lesson-related folders first
                if (lowerFolderName.Contains("lesson") || lowerFolderName.Contains("lessons"))
                {
                    _logger.LogDebug($"Special content folder '{folderName}' detected at level {hierarchyLevel}, treating as lesson");
                    return "lesson";
                }

                // Other content folders are treated as modules
                if (lowerFolderName.Contains("case-studies") || lowerFolderName.Contains("case-study") ||
                    lowerFolderName.Contains("module") || lowerFolderName.Contains("modules") ||
                    lowerFolderName.Contains("readings") || lowerFolderName.Contains("reading") ||
                    lowerFolderName.Contains("resources") || lowerFolderName.Contains("resource") ||
                    lowerFolderName.Contains("assignment") || lowerFolderName.Contains("assignments") ||
                    lowerFolderName.Contains("project") || lowerFolderName.Contains("projects") ||
                    lowerFolderName.Contains("live class") || lowerFolderName.Contains("live-class"))
                {
                    _logger.LogDebug($"Special content folder '{folderName}' detected at level {hierarchyLevel}, treating as module");
                    return "module";
                }
            }
        } // Standard hierarchy mapping starting from vault root (level 0)

        return hierarchyLevel switch
        {
            1 => "main",            // Vault root (e.g., MBA when passed as vault root)
            2 => "program",         // Program subdivision (e.g., Digital Program)
            3 => "course",          // Course (e.g., Finance)
            4 => "class",           // Class (e.g., Corporate-Finance)
            5 => "module",          // Module level (e.g., Week 1)
            6 => "lesson",          // Lesson level (e.g., Lesson 1)
            _ => "module",          // Deep subdirectories use module template
        };
    }

    /// <summary>
    /// Scans the specified folder for markdown content and performs comprehensive file analysis and categorization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method performs the initial content discovery phase of index generation by systematically
    /// scanning the target folder for markdown files and analyzing each file's metadata, content type,
    /// and organizational properties.
    /// </para>
    /// <para>
    /// Scanning Strategy:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Searches only the immediate directory (non-recursive)</description></item>
    /// <item><description>Filters for .md files while excluding the index file itself</description></item>
    /// <item><description>Performs detailed analysis of each discovered file</description></item>
    /// <item><description>Extracts metadata from frontmatter and filename patterns</description></item>
    /// </list>
    /// <para>
    /// File Analysis:
    /// Each discovered file undergoes comprehensive analysis including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Frontmatter parsing for template-type, title, course, and module metadata</description></item>
    /// <item><description>Filename pattern analysis for content type classification</description></item>
    /// <item><description>Course structure extraction for module and lesson identification</description></item>
    /// <item><description>Friendly title generation for display purposes</description></item>
    /// </list>
    /// <para>
    /// Content Categorization:
    /// Files are automatically categorized into types such as reading, video, transcript,
    /// assignment, discussion, and note based on frontmatter and filename patterns.
    /// </para>
    /// <para>
    /// Error Resilience:
    /// Individual file analysis errors are logged but do not stop the overall scanning process,
    /// ensuring robust operation even with corrupted or inaccessible files.
    /// </para>
    /// </remarks>
    /// <param name="folderPath">
    /// The absolute path to the folder to scan for markdown content.
    /// Must be a valid, accessible directory.
    /// </param>
    /// <param name="vaultPath">
    /// The absolute path to the vault root directory, used for calculating relative paths
    /// and providing context for content analysis.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous scanning operation.
    /// The task result contains a list of VaultFileInfo objects representing all analyzed files,
    /// with complete metadata, categorization, and organizational information.
    /// Returns an empty list if the folder doesn't exist or contains no markdown files.
    /// </returns>
    /// <example>
    /// <code>
    /// // Scan a course folder for content
    /// var files = await processor.ScanFolderContentAsync(
    ///     @"C:\vault\MBA\Finance\Corporate-Finance",
    ///     @"C:\vault");
    ///
    /// // Analyze discovered content
    /// var readingFiles = files.Where(f => f.ContentType == "reading").ToList();
    /// var videoFiles = files.Where(f => f.ContentType == "video").ToList();
    ///
    /// Console.WriteLine($"Found {readingFiles.Count} readings and {videoFiles.Count} videos");
    ///
    /// // Display file information
    /// foreach (var file in files)
    /// {
    ///     Console.WriteLine($"{file.Title} ({file.ContentType}) - Module: {file.Module}");
    /// }
    /// </code>
    /// </example>
    private async Task<List<VaultFileInfo>> ScanFolderContentAsync(string folderPath, string vaultPath)
    {
        var files = new List<VaultFileInfo>();

        if (!Directory.Exists(folderPath))
        {
            return files;
        }

        var markdownFiles = Directory.GetFiles(folderPath, "*.md", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).Equals(Path.GetFileName(folderPath) + ".md", StringComparison.OrdinalIgnoreCase));

        foreach (var filePath in markdownFiles)
        {
            var fileInfo = await AnalyzeFileAsync(filePath, vaultPath).ConfigureAwait(false);
            files.Add(fileInfo);
        }

        return files;
    }

    /// <summary>
    /// Performs comprehensive analysis of a single markdown file to extract metadata, categorize content, and determine organizational properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method represents the core file analysis engine that examines individual markdown files
    /// to extract all relevant information needed for index generation and content organization.
    /// It combines multiple analysis techniques to build a complete picture of each file's properties.
    /// </para>
    /// <para>
    /// Analysis Components:
    /// </para>
    /// <list type="number">
    /// <item><description>Basic file information extraction (name, path, friendly title)</description></item>
    /// <item><description>YAML frontmatter parsing for structured metadata</description></item>
    /// <item><description>Content type classification from template-type field or filename patterns</description></item>
    /// <item><description>Course structure analysis using CourseStructureExtractor</description></item>
    /// <item><description>Module and lesson identification for organizational hierarchy</description></item>
    /// </list>
    /// <para>
    /// Frontmatter Processing:
    /// Extracts key metadata fields including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>template-type: For content categorization (reading, video, assignment, etc.)</description></item>
    /// <item><description>title: For display names and navigation</description></item>
    /// <item><description>course: For course-level organization</description></item>
    /// <item><description>module: For module-level grouping</description></item>
    /// </list>
    /// <para>
    /// Content Type Detection:
    /// Uses a multi-stage approach:
    /// </para>
    /// <list type="number">
    /// <item><description>Primary: template-type field from frontmatter</description></item>
    /// <item><description>Fallback: Filename pattern analysis (reading, video, transcript, etc.)</description></item>
    /// <item><description>Default: "note" classification for unrecognized content</description></item>
    /// </list>
    /// <para>
    /// Course Structure Integration:
    /// Leverages CourseStructureExtractor to identify module and lesson information
    /// from filename patterns and directory structure analysis.
    /// </para>
    /// <para>
    /// Error Handling:
    /// Gracefully handles file access errors, corrupted frontmatter, and missing metadata
    /// while providing comprehensive logging for troubleshooting.
    /// </para>
    /// </remarks>
    /// <param name="filePath">
    /// The absolute path to the markdown file to analyze.
    /// Must be a valid .md file accessible for reading.
    /// </param>
    /// <param name="vaultPath">
    /// The absolute path to the vault root directory, used for calculating relative paths
    /// and providing context for the CourseStructureExtractor analysis.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous file analysis operation.
    /// The task result contains a VaultFileInfo object with complete file metadata including:
    /// <list type="bullet">
    /// <item><description>Basic file information (name, path, title)</description></item>
    /// <item><description>Content type classification</description></item>
    /// <item><description>Course and module associations</description></item>
    /// <item><description>Extracted metadata from frontmatter</description></item>
    /// </list>
    /// Returns a VaultFileInfo with default values if analysis fails.
    /// </returns>
    /// <example>
    /// <code>
    /// // Analyze a specific file
    /// var fileInfo = await processor.AnalyzeFileAsync(
    ///     @"C:\vault\MBA\Finance\Reading-Financial-Statements.md",
    ///     @"C:\vault");
    ///
    /// Console.WriteLine($"File: {fileInfo.Title}");
    /// Console.WriteLine($"Type: {fileInfo.ContentType}");
    /// Console.WriteLine($"Course: {fileInfo.Course}");
    /// Console.WriteLine($"Module: {fileInfo.Module}");
    /// Console.WriteLine($"Relative Path: {fileInfo.RelativePath}");
    ///
    /// // Check content type for categorization
    /// if (fileInfo.ContentType == "reading")
    /// {
    ///     Console.WriteLine("This file will be grouped under readings section");
    /// }
    /// </code>
    /// </example>
    private async Task<VaultFileInfo> AnalyzeFileAsync(string filePath, string vaultPath)
    {
        var fileInfo = new VaultFileInfo
        {
            FileName = Path.GetFileNameWithoutExtension(filePath),
            RelativePath = Path.GetRelativePath(vaultPath, filePath),
            FullPath = filePath,
            Title = FriendlyTitleHelper.GetFriendlyTitleFromFileName(Path.GetFileName(filePath)),
        };

        try
        {
            // Read file content and extract frontmatter
            var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            var frontmatter = _yamlHelper.ExtractFrontmatter(content);

            if (!string.IsNullOrEmpty(frontmatter))
            {
                var yamlData = _yamlHelper.ParseYamlToDictionary(frontmatter);

                // Extract content type from template-type field
                if (yamlData.TryGetValue("template-type", out var templateType))
                {
                    fileInfo.ContentType = templateType?.ToString() ?? "note";
                }

                // Extract title
                if (yamlData.TryGetValue("title", out var title))
                {
                    fileInfo.Title = title?.ToString() ?? fileInfo.Title;
                }

                // Extract course and module info
                if (yamlData.TryGetValue("course", out var course))
                {
                    fileInfo.Course = course?.ToString();
                }

                if (yamlData.TryGetValue("module", out var module))
                {
                    fileInfo.Module = module?.ToString();
                }
            }
            else
            {
                // No frontmatter, categorize by filename patterns
                fileInfo.ContentType = CategorizeByFilename(fileInfo.FileName);
            }

            // Extract module/lesson using CourseStructureExtractor
            var metadata = new Dictionary<string, object?>();
            _structureExtractor.ExtractModuleAndLesson(filePath, metadata);

            if (metadata.TryGetValue("module", out var extractedModule))
            {
                fileInfo.Module ??= extractedModule?.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error analyzing file: {filePath}");
        }

        return fileInfo;
    }

    /// <summary>
    /// Categorizes content type based on filename patterns when frontmatter metadata is not available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides fallback content categorization when files lack proper frontmatter metadata.
    /// It uses pattern matching against common educational content naming conventions to determine
    /// the most appropriate content type classification.
    /// </para>
    /// <para>
    /// Pattern Matching Strategy:
    /// The method performs case-insensitive substring matching against established patterns:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Reading materials: "reading", "article" → "reading"</description></item>
    /// <item><description>Video content: "video", "lecture" → "video"</description></item>
    /// <item><description>Transcripts: "transcript" → "transcript"</description></item>
    /// <item><description>Assignments: "assignment", "homework" → "assignment"</description></item>
    /// <item><description>Discussions: "discussion", "forum" → "discussion"</description></item>
    /// <item><description>Default: All other patterns → "note"</description></item>
    /// </list>
    /// <para>
    /// Content Type Impact:
    /// The determined content type influences:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Section grouping in generated indices</description></item>
    /// <item><description>Icon selection for visual organization</description></item>
    /// <item><description>Sorting and display order within content sections</description></item>
    /// <item><description>Integration with Obsidian Bases query filters</description></item>
    /// </list>
    /// <para>
    /// Extensibility:
    /// The pattern matching logic can be easily extended to recognize additional
    /// content types and filename conventions as needed.
    /// </para>
    /// </remarks>
    /// <param name="fileName">
    /// The filename (without extension) to analyze for content type patterns.
    /// Case-insensitive matching is performed against the entire filename.
    /// </param>
    /// <returns>
    /// A content type string indicating the categorization result:
    /// <list type="bullet">
    /// <item><description>"reading" - For reading materials and articles</description></item>
    /// <item><description>"video" - For video content and lectures</description></item>
    /// <item><description>"transcript" - For transcription files</description></item>
    /// <item><description>"assignment" - For assignments and homework</description></item>
    /// <item><description>"discussion" - For discussion and forum content</description></item>
    /// <item><description>"note" - Default for unrecognized patterns</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// // Filename pattern classification examples
    /// string type1 = CategorizeByFilename("Reading-Financial-Markets-Overview");
    /// // Returns "reading" due to "reading" prefix
    ///
    /// string type2 = CategorizeByFilename("Video-Introduction-to-Finance");
    /// // Returns "video" due to "video" prefix
    ///
    /// string type3 = CategorizeByFilename("Assignment-Portfolio-Analysis");
    /// // Returns "assignment" due to "assignment" prefix
    ///
    /// string type4 = CategorizeByFilename("Meeting-Notes-December");
    /// // Returns "note" as default for unrecognized pattern
    ///
    /// string type5 = CategorizeByFilename("LECTURE-Advanced-Concepts");
    /// // Returns "video" due to case-insensitive "lecture" matching
    /// </code>
    /// </example>
    private static string CategorizeByFilename(string fileName)
    {
        var lowerName = fileName.ToLowerInvariant();

        if (lowerName.Contains("reading") || lowerName.Contains("article"))
        {
            return "reading";
        }

        if (lowerName.Contains("video") || lowerName.Contains("lecture"))
        {
            return "video";
        }

        if (lowerName.Contains("transcript"))
        {
            return "transcript";
        }

        if (lowerName.Contains("assignment") || lowerName.Contains("homework"))
        {
            return "assignment";
        }

        if (lowerName.Contains("discussion") || lowerName.Contains("forum"))
        {
            return "discussion";
        }

        return "note";
    }

    /// <summary>
    /// Generates the complete index content including frontmatter and body sections with intelligent navigation and content organization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method orchestrates the creation of the final index file content by combining template metadata,
    /// hierarchy information, content categorization, and navigation elements into a cohesive Obsidian-compatible
    /// markdown document.
    /// </para>
    /// <para>
    /// Content Generation Workflow:
    /// </para>
    /// <list type="number">
    /// <item><description>Clone and customize template frontmatter with folder-specific information</description></item>
    /// <item><description>Apply hierarchy metadata using IMetadataHierarchyDetector integration</description></item>
    /// <item><description>Generate contextual navigation links based on hierarchy level</description></item>
    /// <item><description>Organize content into type-specific sections with appropriate icons</description></item>
    /// <item><description>Add level-specific features (Bases integration for class level)</description></item>
    /// <item><description>Combine all elements using MarkdownNoteBuilder</description></item>
    /// </list>
    /// <para>
    /// Frontmatter Processing:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Preserves template structure while adding folder-specific metadata</description></item>
    /// <item><description>Updates hierarchy fields (program, course, class, module) based on vault position</description></item>
    /// <item><description>Adds title derived from folder name with friendly formatting</description></item>
    /// <item><description>Sets type to "index" and adds creation timestamp</description></item>
    /// <item><description>Maintains banner references for visual consistency</description></item>
    /// </list>
    /// <para>
    /// Navigation System:
    /// Generates contextual navigation based on hierarchy level:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Main level: Dashboard and Classes Assignments shortcuts</description></item>
    /// <item><description>Other levels: Back link to parent + Home + Dashboard + Classes Assignments</description></item>
    /// <item><description>Intelligent back link detection using parent folder analysis</description></item>
    /// <item><description>Friendly link text generation for improved user experience</description></item>
    /// </list>
    /// <para>
    /// Content Organization:
    /// Content is organized differently based on hierarchy level:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Program level: Lists courses with folder navigation</description></item>
    /// <item><description>Course level: Lists classes with readings and case studies</description></item>
    /// <item><description>Class level: Obsidian Bases integration for dynamic content queries</description></item>
    /// <item><description>Module level: Categorized content sections (readings, videos, assignments, etc.)</description></item>
    /// </list>
    /// <para>
    /// Bases Integration:
    /// For class-level indices, generates Obsidian Bases query blocks for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Readings: Dynamic reading material discovery</description></item>
    /// <item><description>Instructions: Assignment and project instructions</description></item>
    /// <item><description>Case Studies: Case study content filtering</description></item>
    /// <item><description>Videos: Video content organization</description></item>
    /// </list>
    /// </remarks>
    /// <param name="folderPath">
    /// The absolute path to the folder for which index content is being generated.
    /// Used for title generation and hierarchy context.
    /// </param>
    /// <param name="vaultPath">
    /// The absolute path to the vault root directory for navigation and context.
    /// Used for generating relative references and back links.
    /// </param>
    /// <param name="template">
    /// The template dictionary containing frontmatter structure and default values.
    /// Provides the base metadata schema for the generated index.
    /// </param>
    /// <param name="files">
    /// The list of analyzed files in the folder for content organization.
    /// Used for generating content sections and categorized listings.
    /// </param>
    /// <param name="hierarchyInfo">
    /// The hierarchy metadata dictionary containing program, course, class, and module information.
    /// Used for applying contextual metadata and Bases query generation.
    /// </param>
    /// <param name="hierarchyLevel">
    /// The 1-based hierarchy level determining content organization strategy.
    /// Influences navigation generation, content sections, and special features.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous content generation operation.
    /// The task result contains the complete markdown content string ready for file output,
    /// including properly formatted YAML frontmatter and organized body content.
    /// </returns>
    /// <example>
    /// <code>
    /// // Generate content for a class-level index
    /// var template = templateManager.GetTemplate("class");
    /// var files = await ScanFolderContentAsync(folderPath, vaultPath);
    /// var hierarchyInfo = hierarchyDetector.FindHierarchyInfo(folderPath);
    ///
    /// string content = await GenerateIndexContentAsync(
    ///     @"C:\vault\MBA\Finance\Corporate-Finance",
    ///     @"C:\vault",
    ///     template,
    ///     files,
    ///     hierarchyInfo,
    ///     4); // Class level
    ///
    /// // The generated content includes:
    /// // - YAML frontmatter with hierarchy metadata
    /// // - Navigation links (back, home, dashboard, assignments)
    /// // - Obsidian Bases query blocks for dynamic content
    /// // - Organized content sections with appropriate icons
    /// </code>
    /// </example>
    private Task<string> GenerateIndexContentAsync(
        string folderPath,
        string vaultPath,
        Dictionary<string, object> template,
        List<VaultFileInfo> files,
        Dictionary<string, string> hierarchyInfo,
        int hierarchyLevel)
    {
        // Clone the template to avoid mutating the original
        var frontmatter = new Dictionary<string, object>(template);

        frontmatter["title"] = FriendlyTitleHelper.GetFriendlyTitleFromFileName(Path.GetFileName(folderPath) ?? "Index");        // Apply hierarchy metadata based on template type using MetadataHierarchyDetector
        string? templateType = frontmatter.GetValueOrDefault("template-type")?.ToString();
        _logger.LogDebug($"Before UpdateMetadataWithHierarchy - frontmatter keys: {string.Join(", ", frontmatter.Keys)}");
        _logger.LogDebug($"hierarchyInfo keys: {string.Join(", ", hierarchyInfo.Keys)}");
        foreach (var kvp in hierarchyInfo)
        {
            _logger.LogDebug($"hierarchyInfo[{kvp.Key}] = '{kvp.Value}'");
        }

        if (frontmatter.ContainsKey("program"))
        {
            _logger.LogDebug($"Before - frontmatter contains program: {frontmatter["program"]}");
        }

        if (frontmatter.ContainsKey("course"))
        {
            _logger.LogDebug($"Before - frontmatter contains course: {frontmatter["course"]}");
        }

        if (frontmatter.ContainsKey("class"))
        {
            _logger.LogDebug($"Before - frontmatter contains class: {frontmatter["class"]}");
        }

        var updatedFrontmatter = _hierarchyDetector.UpdateMetadataWithHierarchy(frontmatter.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value), hierarchyInfo, templateType);
        frontmatter.Clear();
        foreach (var kvp in updatedFrontmatter)
        {
            if (kvp.Value is not null)
                frontmatter[kvp.Key] = kvp.Value;
        }

        _logger.LogDebug($"After UpdateMetadataWithHierarchy - frontmatter keys: {string.Join(", ", frontmatter.Keys)}");
        if (frontmatter.ContainsKey("program"))
        {
            _logger.LogDebug($"After - frontmatter contains program: {frontmatter["program"]}");
        }

        if (frontmatter.ContainsKey("course"))
        {
            _logger.LogDebug($"After - frontmatter contains course: {frontmatter["course"]}");
        }

        if (frontmatter.ContainsKey("class"))
        {
            _logger.LogDebug($"After - frontmatter contains class: {frontmatter["class"]}");
        }

        frontmatter["type"] = "index";
        frontmatter["date-created"] = DateTime.UtcNow.ToString("yyyy-MM-dd");        // Ensure banner is present (should be from template, but just in case)
        if (!frontmatter.ContainsKey("banner"))
        {
            frontmatter["banner"] = "'[[gies-banner.png]]'";
        }
        // Note: We preserve the banner value exactly as it comes from the template
        // to maintain proper Obsidian wiki link syntax like '[[gies-banner.png]]'

        // Group files by content type
        var groupedFiles = files.GroupBy(f => f.ContentType).ToDictionary(g => g.Key, g => g.ToList());        // Generate content sections
        var contentSections = new List<string>();

        // Add the title as an H1 heading
        string headerTitle = frontmatter["title"]?.ToString() ?? "Index";
        contentSections.Add($"# {headerTitle}");
        contentSections.Add(string.Empty);

        // Special handling for main (check template-type in frontmatter)
        bool isMain = frontmatter.ContainsKey("template-type") && frontmatter["template-type"]?.ToString() == "main";

        if (isMain)
        {
            // Add just Dashboard and Classes Assignments links for the main index
            contentSections.Add("📊 [[Dashboard]] | 📝 [[Classes Assignments]]");
            contentSections.Add(string.Empty);            // Get all subfolders (courses/programs)
            var subFolders = Directory.GetDirectories(folderPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith(".")) // Exclude folders starting with a period
                .OrderBy(name => name)
                .ToList();

            if (subFolders.Any())
            {
                contentSections.Add("## Programs");
                contentSections.Add(string.Empty);

                foreach (var subFolder in subFolders)
                {
                    if (!string.IsNullOrEmpty(subFolder))
                    {
                        // Get friendly name for the subfolder
                        string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                        contentSections.Add($"- 📁 [[{subFolder}|{friendlyName}]]");
                    }
                }
            }
        }
        else
        {
            // Add top navigation bar for all non-main indices
            string backLinkTarget;

            if (hierarchyLevel == 1) // Program index
            {
                backLinkTarget = GetRootIndexFilename(vaultPath);
            }
            else if (hierarchyLevel == 2) // Course index
            {
                // Get program folder name (parent folder)
                backLinkTarget = Path.GetFileName(Path.GetDirectoryName(folderPath) ?? string.Empty);
            }
            else if (hierarchyLevel == 3) // Class index
            {
                // Get course folder name (parent folder)
                backLinkTarget = Path.GetFileName(Path.GetDirectoryName(folderPath) ?? string.Empty);
            }
            else // Module or other indices
            {
                // Get class folder name (parent folder)
                backLinkTarget = Path.GetFileName(Path.GetDirectoryName(folderPath) ?? string.Empty);
            }

            // Generate friendly back link text from the target name
            string backLinkText = FriendlyTitleHelper.GetFriendlyTitleFromFileName(backLinkTarget);

            // Get the root index filename for the Home link
            string rootIndex = GetRootIndexFilename(vaultPath);

            // Add top navigation links
            contentSections.Add($"🔙 [[{backLinkTarget}|{backLinkText}]] | 🏠 [[{rootIndex}|Home]] | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]");
            contentSections.Add(string.Empty);            // Add content based on hierarchy level
            var subFolders = Directory.GetDirectories(folderPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith(".")) // Exclude folders starting with a period
                .OrderBy(name => name)
                .ToList();

            if (hierarchyLevel == 0) // Main index - show programs
            {
                if (subFolders.Any())
                {
                    contentSections.Add("## Programs");
                    contentSections.Add(string.Empty);

                    foreach (var subFolder in subFolders)
                    {
                        if (!string.IsNullOrEmpty(subFolder))
                        {
                            string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                            contentSections.Add($"- 📁 [[{subFolder}|{friendlyName}]]");
                        }
                    }
                }
            }
            else if (hierarchyLevel == 1) // Program index - show only courses
            {
                if (subFolders.Any())
                {
                    contentSections.Add("## Courses");
                    contentSections.Add(string.Empty);

                    foreach (var subFolder in subFolders)
                    {
                        if (!string.IsNullOrEmpty(subFolder))
                        {
                            string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                            contentSections.Add($"- 📁 [[{subFolder}|{friendlyName}]]");
                        }
                    }
                }
            }
            else if (hierarchyLevel == 2) // Program index - show courses and readings/case studies
            {
                // Always show folder listing at program level (Bases integration only applies at class level)
                if (subFolders.Any())
                {
                    contentSections.Add("## Courses");
                    contentSections.Add(string.Empty);

                    foreach (var subFolder in subFolders)
                    {
                        if (!string.IsNullOrEmpty(subFolder))
                        {
                            string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                            contentSections.Add($"- 📁 [[{subFolder}|{friendlyName}]]");
                        }
                    }

                    contentSections.Add(string.Empty);
                }

                // Add content by type - focus on readings for course level
                if (groupedFiles.TryGetValue("reading", out var readings) && readings.Count > 0)
                {
                    contentSections.Add("## 📚 Readings");
                    foreach (var file in readings.OrderBy(f => f.Title))
                    {
                        contentSections.Add($"- 📄 [[{file.Title}]]");
                    }

                    contentSections.Add(string.Empty);
                }

                // Add case studies for course level
                if (groupedFiles.TryGetValue("note", out var notes) && notes.Count > 0)
                {
                    var caseStudies = notes.Where(f => f.Title?.Contains("Case", StringComparison.OrdinalIgnoreCase) == true).ToList();
                    if (caseStudies.Any())
                    {
                        contentSections.Add("## 📋 Case Studies");
                        foreach (var file in caseStudies.OrderBy(f => f.Title))
                        {
                            contentSections.Add($"- [[{file.Title}]]");
                        }

                        contentSections.Add(string.Empty);
                    }
                }
            }
            else if (hierarchyLevel == 3) // Course index - show classes
            {
                if (subFolders.Any())
                {
                    contentSections.Add("## Classes");
                    contentSections.Add(string.Empty);

                    foreach (var subFolder in subFolders)
                    {
                        if (!string.IsNullOrEmpty(subFolder))
                        {
                            string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                            contentSections.Add($"- 📁 [[{subFolder}|{friendlyName}]]");
                        }
                    }

                    contentSections.Add(string.Empty);
                }
            }
            else if (hierarchyLevel == 4) // Class level - only show Bases blocks (no modules or files)
            {
                // For class level, we only want Bases blocks - no static content
                // The Bases blocks will be added separately below
            }
            else // Module level or below - show content by type
            {
                if (subFolders.Any())
                {
                    contentSections.Add("## Modules");
                    foreach (var subFolder in subFolders)
                    {
                        if (!string.IsNullOrEmpty(subFolder))
                        {
                            string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                            contentSections.Add($"- [[{subFolder}|{friendlyName}]]");
                        }
                    }

                    contentSections.Add(string.Empty);
                }

                // Add content by type for module level and below
                foreach (var contentType in new[] { "reading", "video", "transcript", "assignment", "discussion", "note" })
                {
                    if (groupedFiles.TryGetValue(contentType, out var typeFiles) && typeFiles.Count > 0)
                    {
                        string icon = GetContentTypeIcon(contentType);
                        string title = GetContentTypeTitle(contentType);

                        contentSections.Add($"## {icon} {title}");
                        foreach (var file in typeFiles.OrderBy(f => f.Title))
                        {
                            contentSections.Add($"- [[{file.Title}]]");
                        }

                        contentSections.Add(string.Empty);
                    }
                }
            }
        }

        // Add Bases integration for class level (hierarchy level 4)
        if (hierarchyLevel == 4)
        {
            // Possible locations for the template file
            List<string> possiblePaths = new()
            {
                // 1. Check relative to the vault root path
                Path.Combine(!string.IsNullOrEmpty(vaultPath) ? vaultPath : _defaultVaultRootPath, "..", "config", "BaseBlockTemplate.yaml"),

                // 2. Check in the current directory's config folder
                Path.Combine(Directory.GetCurrentDirectory(), "config", "BaseBlockTemplate.yaml"),

                // 3. Check relative to the executable
                Path.Combine(AppContext.BaseDirectory, "config", "BaseBlockTemplate.yaml"),
            };

            // Walk up until we find the config folder
            string? configPath = null;

            // Check each possible path
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    configPath = path;
                    _logger.LogDebug("Found BaseBlockTemplate.yaml at: {Path}", path);
                    break;
                }
            }

            // If not found, try walking up directories from the executable location
            if (configPath == null)
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
                {
                    var candidate = Path.Combine(dir.FullName, "config", "BaseBlockTemplate.yaml");
                    if (File.Exists(candidate))
                    {
                        configPath = candidate;
                        _logger.LogDebug($"Found BaseBlockTemplate.yaml by walking up from executable: {candidate}");
                        break;
                    }
                }
            }

            if (configPath == null)
            {
                throw new FileNotFoundException("Base block template not found in any parent config folder.");
            }

            var course = hierarchyInfo.GetValueOrDefault("course") ?? string.Empty;
            var className = hierarchyInfo.GetValueOrDefault("class") ?? string.Empty;
            var module = hierarchyInfo.GetValueOrDefault("module") ?? string.Empty;

            // Instructions block
            string instructionsBlock = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "instructions");

            // Case Studies block
            string caseStudiesBlock = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "note/case-study");

            // Videos block
            string videosBlock = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "video-reference");

            // Readings block
            string readingsBlock = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "reading");

            // Wrap in code blocks for Obsidian
            instructionsBlock = $"```base\n{instructionsBlock}\n```";
            caseStudiesBlock = $"```base\n{caseStudiesBlock}\n```";
            videosBlock = $"```base\n{videosBlock}\n```";
            readingsBlock = $"```base\n{readingsBlock}\n```";
            contentSections.Add("## 📚 Readings");
            contentSections.Add(readingsBlock);
            contentSections.Add(string.Empty);

            contentSections.Add("## 📝 Instructions");
            contentSections.Add(instructionsBlock);
            contentSections.Add(string.Empty);

            contentSections.Add("## 📊 Case Studies");
            contentSections.Add(caseStudiesBlock);
            contentSections.Add(string.Empty);

            contentSections.Add("## 📽️ Videos");
            contentSections.Add(videosBlock);
        }

        // Generate the body content
        string bodyContent = string.Join("\n\n", contentSections);

        return Task.FromResult(_noteBuilder.BuildNote(frontmatter, bodyContent));
    }

    /// <summary>
    /// Gets the appropriate emoji icon for a content type to enhance visual organization in generated indices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides consistent visual representation for different content types throughout
    /// the vault index system. Icons help users quickly identify content categories and improve
    /// the overall navigation experience.
    /// </para>
    /// <para>
    /// Icon Selection Strategy:
    /// Icons are chosen to be universally recognizable and maintain visual consistency across
    /// different platforms and Obsidian themes. The selection prioritizes clarity and immediate
    /// content type recognition.
    /// </para>
    /// </remarks>
    /// <param name="contentType">
    /// The content type string to get an icon for. Should match values returned by
    /// content categorization methods (reading, video, transcript, assignment, discussion, note).
    /// </param>
    /// <returns>
    /// An emoji string representing the content type:
    /// <list type="bullet">
    /// <item><description>"📖" - For reading materials and articles</description></item>
    /// <item><description>"🎥" - For video content and lectures</description></item>
    /// <item><description>"📝" - For transcripts and text-based content</description></item>
    /// <item><description>"📋" - For assignments and homework</description></item>
    /// <item><description>"💬" - For discussions and forum content</description></item>
    /// <item><description>"📄" - Default for notes and unrecognized content</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// // Get icons for different content types
    /// string readingIcon = GetContentTypeIcon("reading"); // Returns "📖"
    /// string videoIcon = GetContentTypeIcon("video");     // Returns "🎥"
    /// string noteIcon = GetContentTypeIcon("note");       // Returns "📄"
    /// string unknownIcon = GetContentTypeIcon("custom");  // Returns "📄" (default)
    ///
    /// // Usage in content section generation
    /// string sectionHeader = $"## {GetContentTypeIcon("reading")} Readings";
    /// // Results in: "## 📖 Readings"
    /// </code>
    /// </example>
    private static string GetContentTypeIcon(string contentType)
    {
        return contentType switch
        {
            "reading" => "📖",
            "video" => "🎥",
            "transcript" => "📝",
            "assignment" => "📋",
            "discussion" => "💬",
            _ => "📄",
        };
    }

    /// <summary>
    /// Gets the human-readable display title for a content type to create clear section headers in generated indices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides consistent, user-friendly titles for content type sections in vault indices.
    /// The titles are designed to be clear, professional, and suitable for educational content organization.
    /// </para>
    /// <para>
    /// Title Selection Strategy:
    /// Titles use proper pluralization and professional terminology that would be familiar to
    /// educators and students in academic or corporate learning environments.
    /// </para>
    /// </remarks>
    /// <param name="contentType">
    /// The content type string to get a display title for. Should match values returned by
    /// content categorization methods (reading, video, transcript, assignment, discussion, note).
    /// </param>
    /// <returns>
    /// A human-readable title string for the content type:
    /// <list type="bullet">
    /// <item><description>"Readings" - For reading materials and articles</description></item>
    /// <item><description>"Videos" - For video content and lectures</description></item>
    /// <item><description>"Transcripts" - For transcript files</description></item>
    /// <item><description>"Assignments" - For assignments and homework</description></item>
    /// <item><description>"Discussions" - For discussion and forum content</description></item>
    /// <item><description>"Notes" - Default for notes and unrecognized content</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// // Get display titles for different content types
    /// string readingTitle = GetContentTypeTitle("reading");     // Returns "Readings"
    /// string videoTitle = GetContentTypeTitle("video");         // Returns "Videos"
    /// string assignmentTitle = GetContentTypeTitle("assignment"); // Returns "Assignments"
    /// string defaultTitle = GetContentTypeTitle("unknown");     // Returns "Notes"
    ///
    /// // Usage in section header generation
    /// string icon = GetContentTypeIcon("reading");
    /// string title = GetContentTypeTitle("reading");
    /// string sectionHeader = $"## {icon} {title}";
    /// // Results in: "## 📖 Readings"
    /// </code>
    /// </example>
    private static string GetContentTypeTitle(string contentType)
    {
        return contentType switch
        {
            "reading" => "Readings",
            "video" => "Videos",
            "transcript" => "Transcripts",
            "assignment" => "Assignments",
            "discussion" => "Discussions",
            _ => "Notes",
        };
    }

    /// <summary>
    /// Identifies and returns the filename of the main vault index for consistent home navigation across all indices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements intelligent main index discovery to support consistent navigation linking
    /// throughout the vault structure. It ensures that all generated indices can properly link back to
    /// the vault's main entry point, regardless of their location in the hierarchy.
    /// </para>
    /// <para>
    /// Discovery Strategy:
    /// </para>
    /// <list type="number">
    /// <item><description>Scans all markdown files in the vault directory tree</description></item>
    /// <item><description>Examines frontmatter for template-type: main designation</description></item>
    /// <item><description>Prioritizes files closer to vault root (shorter paths)</description></item>
    /// <item><description>Falls back to vault root folder name if no main index found</description></item>
    /// </list>
    /// <para>
    /// Main Index Identification:
    /// Files are considered main indices if they contain YAML frontmatter with:
    /// template-type: main
    /// </para>
    /// <para>
    /// Prioritization Logic:
    /// When multiple main indices exist, the method selects the one with the shortest path
    /// (closest to vault root) to ensure optimal navigation hierarchy.
    /// </para>
    /// <para>
    /// Fallback Mechanism:
    /// If no main index is found, uses the vault root directory name as the default,
    /// ensuring navigation links remain functional even in incomplete vault setups.
    /// </para>
    /// <para>
    /// Error Resilience:
    /// Handles file access errors, corrupted frontmatter, and filesystem issues gracefully
    /// with comprehensive logging and fallback strategies.
    /// </para>
    /// </remarks>
    /// <param name="vaultPath">
    /// The absolute path to the vault root directory for main index discovery.
    /// Used as the starting point for recursive file scanning and as fallback for filename generation.
    /// If empty, uses the configured default vault path.
    /// </param>
    /// <returns>
    /// The filename (without extension) of the main vault index for navigation linking.
    /// <list type="bullet">
    /// <item><description>Primary: Filename of discovered main index file</description></item>
    /// <item><description>Fallback: Vault root directory name</description></item>
    /// <item><description>Default: "main-index" if all else fails</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// // Discover main index in a structured vault
    /// string mainIndex = processor.GetRootIndexFilename(@"C:\vault");
    /// // Returns "MBA" if MBA.md has template-type: main
    ///
    /// // Use in navigation link generation
    /// string homeLink = $"🏠 [[{mainIndex}|Home]]";
    /// // Results in: "🏠 [[MBA|Home]]"
    ///
    /// // Fallback scenario with no main index
    /// string fallbackIndex = processor.GetRootIndexFilename(@"C:\MyVault");
    /// // Returns "MyVault" (directory name) if no main index found
    ///
    /// // Navigation integration
    /// var navigationLinks = new[]
    /// {
    ///     $"[[{backTarget}|Back]]",
    ///     $"[[{GetRootIndexFilename(vaultPath)}|Home]]",
    ///     "[[Dashboard]]",
    ///     "[[Classes Assignments]]"
    /// };
    /// </code>
    /// </example>    private string GetRootIndexFilename(string vaultPath)
    {
        // Prefer the explicitly provided vault path over the default vault path
        // This allows for temporary overrides like test vaults
        string effectiveVaultPath = !string.IsNullOrEmpty(vaultPath)
            ? vaultPath
            : _defaultVaultRootPath;

        // Ensure the path has consistent formatting
        effectiveVaultPath = effectiveVaultPath.Replace('/', Path.DirectorySeparatorChar)
                                              .TrimEnd(Path.DirectorySeparatorChar);

        // Check cache first to avoid expensive file system operations
        if (_cachedVaultPath == effectiveVaultPath && !string.IsNullOrEmpty(_cachedRootIndexFilename))
        {
            _logger.LogDebug($"Using cached root index filename: {_cachedRootIndexFilename} for vault: {effectiveVaultPath}");
            return _cachedRootIndexFilename;
        }

        _logger.LogDebug($"Root index filename not cached for vault: {effectiveVaultPath}, performing lookup");// Look for the first main index file in the vault structure
        try
        {
            _logger.LogDebug($"Searching for main index files in vault: {effectiveVaultPath}");

            // Search for markdown files with main index type in the vault
            var allMarkdownFiles = Directory.GetFiles(effectiveVaultPath, "*.md", SearchOption.AllDirectories);
            _logger.LogDebug($"Found {allMarkdownFiles.Length} markdown files in vault");

            var mainIndexFiles = allMarkdownFiles
                .Where(file => IsMainIndexFile(file))
                .OrderBy(file => file.Length) // Prefer files closer to root (shorter paths)
                .ToList();

            _logger.LogDebug($"Found {mainIndexFiles.Count} main index files");
            foreach (var file in mainIndexFiles)
            {
                _logger.LogDebug($"Main index file found: {file}");
            }

            if (mainIndexFiles.Any())
            {
                string mainIndexFile = mainIndexFiles.First();
                string fileName = Path.GetFileNameWithoutExtension(mainIndexFile);
                _logger.LogDebug($"Using main index file: {fileName} at {mainIndexFile}");
                return fileName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error searching for main index file in vault");
        }

        // Fallback: use the vault root folder name
        string vaultRootFolder = Path.GetFileName(effectiveVaultPath);
        if (string.IsNullOrEmpty(vaultRootFolder))
        {
            _logger.LogWarning($"Unable to determine vault root folder name, using 'main-index' as default");
            vaultRootFolder = "main-index";
        }
        else
        {
            _logger.LogDebug($"Using vault root folder name: {vaultRootFolder} for index filename");
        }

        return vaultRootFolder;
    }

    /// <summary>
    /// Determines if a markdown file is designated as a main vault index by examining its frontmatter metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides the core logic for identifying main index files within the vault structure.
    /// It performs frontmatter analysis to determine if a file serves as a primary navigation entry point
    /// for the vault system.
    /// </para>
    /// <para>
    /// Identification Criteria:
    /// A file is considered a main index if it contains YAML frontmatter with:
    /// template-type: main
    /// </para>
    /// <para>
    /// Analysis Process:
    /// </para>
    /// <list type="number">
    /// <item><description>Reads the complete file content</description></item>
    /// <item><description>Extracts YAML frontmatter using IYamlHelper</description></item>
    /// <item><description>Parses frontmatter into a structured dictionary</description></item>
    /// <item><description>Checks for template-type field with "main" value</description></item>
    /// </list>
    /// <para>
    /// Error Handling:
    /// Gracefully handles various error conditions including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>File access permissions issues</description></item>
    /// <item><description>Corrupted or malformed YAML frontmatter</description></item>
    /// <item><description>Missing frontmatter sections</description></item>
    /// <item><description>File system read errors</description></item>
    /// </list>
    /// <para>
    /// Performance Considerations:
    /// The method reads entire file contents but is optimized for the typical use case
    /// where main index files are relatively small and few in number.
    /// </para>
    /// </remarks>
    /// <param name="filePath">
    /// The absolute path to the markdown file to examine for main index designation.
    /// Must be a valid .md file path, though file accessibility is verified internally.
    /// </param>
    /// <returns>
    /// true if the file contains frontmatter with template-type: main; false otherwise.
    /// <list type="bullet">
    /// <item><description>true: File has valid frontmatter with template-type: main</description></item>
    /// <item><description>false: File lacks frontmatter, has different template-type, or analysis fails</description></item>
    /// </list>
    /// </returns>
    /// <example>
    /// <code>
    /// // Check if a file is a main index
    /// bool isMain = processor.IsMainIndexFile(@"C:\vault\MBA.md");
    /// if (isMain)
    /// {
    ///     Console.WriteLine("Found main vault index");
    /// }
    ///
    /// // Example file content that would return true:
    /// /*
    /// ---
    /// title: "MBA Program"
    /// template-type: main
    /// type: index
    /// banner: "[[gies-banner.png]]"
    /// ---
    /// # MBA Program
    /// Welcome to the MBA program vault...
    /// */
    ///
    /// // Example file content that would return false:
    /// /*
    /// ---
    /// title: "Corporate Finance"
    /// template-type: class
    /// course: "Finance"
    /// ---
    /// # Corporate Finance
    /// Course materials...
    /// */
    ///
    /// // Usage in main index discovery
    /// var mainFiles = Directory.GetFiles(vaultPath, "*.md", SearchOption.AllDirectories)
    ///     .Where(file => IsMainIndexFile(file))
    ///     .ToList();
    /// </code>
    /// </example>
    private bool IsMainIndexFile(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            string? frontmatter = _yamlHelper.ExtractFrontmatter(content);

            if (!string.IsNullOrEmpty(frontmatter))
            {
                var yamlData = _yamlHelper.ParseYamlToDictionary(frontmatter);
                if (yamlData.TryGetValue("template-type", out var templateType))
                {
                    return templateType?.ToString() == "main";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, $"Error checking if file is main index: {filePath}");
        }

        return false;
    }
}
