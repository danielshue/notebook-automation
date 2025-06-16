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
    IVaultIndexContentGenerator contentGenerator,
    string vaultRootPath = "") : IVaultIndexProcessor
{
    private readonly ILogger<VaultIndexProcessor> _logger = logger;
    private readonly IMetadataTemplateManager _templateManager = templateManager;
    private readonly IMetadataHierarchyDetector _hierarchyDetector = hierarchyDetector;
    private readonly ICourseStructureExtractor _structureExtractor = structureExtractor; private readonly IYamlHelper _yamlHelper = yamlHelper;
    private readonly MarkdownNoteBuilder _noteBuilder = noteBuilder;
    private readonly IVaultIndexContentGenerator _contentGenerator = contentGenerator; private readonly string _defaultVaultRootPath = !string.IsNullOrEmpty(vaultRootPath)
        ? vaultRootPath
        : appConfig.Paths.NotebookVaultFullpathRoot;

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
            _logger.LogDebug($"Template type '{templateType}' determined for '{folderName}' at level {hierarchyLevel}");

            // Get template using the actual available method
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
            var hierarchyInfo = _hierarchyDetector.FindHierarchyInfo(folderPath);

            // Generate index content using the dedicated content generator
            string indexContent = await _contentGenerator.GenerateIndexContentAsync(
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
    /// }    /// </code>
    /// </example>
    internal async Task<List<VaultFileInfo>> ScanFolderContentAsync(string folderPath, string vaultPath)
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
    /// }    /// </code>
    /// </example>
    internal async Task<VaultFileInfo> AnalyzeFileAsync(string filePath, string vaultPath)
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
    /// // Returns "video" due to case-insensitive "lecture" matching    /// </code>
    /// </example>
    internal static string CategorizeByFilename(string fileName)
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
}
