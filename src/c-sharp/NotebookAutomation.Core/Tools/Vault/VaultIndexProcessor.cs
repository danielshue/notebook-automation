// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Processor for generating individual vault index files.
/// </summary>
/// <remarks>
/// The <c>VaultIndexProcessor</c> class handles the generation of index files for individual
/// folders within an Obsidian vault. It detects hierarchy levels, applies appropriate templates,
/// categorizes content by type, and optionally integrates Obsidian Bases for dynamic views.
/// </remarks>
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
    private readonly MarkdownNoteBuilder _noteBuilder = noteBuilder;
    private readonly string _defaultVaultRootPath = !string.IsNullOrEmpty(vaultRootPath)
        ? vaultRootPath
        : appConfig.Paths.NotebookVaultFullpathRoot;

    /// <summary>
    /// Generates an index file for the specified folder.
    /// </summary>
    /// <param name="folderPath">Path to the folder to generate an index for.</param>
    /// <param name="vaultPath">Path to the vault root directory.</param>
    /// <param name="forceOverwrite">If true, regenerates the index even if it already exists.</param>
    /// <param name="dryRun">If true, simulates the operation without making actual changes.</param>
    /// <returns>True if an index was generated or would be generated, false otherwise.</returns>
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
    /// <summary>
    /// Determines the template type based on hierarchy level and folder name.
    /// </summary>
    /// <param name="hierarchyLevel">The hierarchy level of the folder.</param>
    /// <param name="folderName">Optional folder name to check for special cases.</param>
    /// <summary>
    /// Determines the appropriate template type based on hierarchy level and folder name.
    /// </summary>
    /// <param name="hierarchyLevel">The hierarchy level of the folder relative to vault root.</param>
    /// <param name="folderName">Optional folder name for special case detection.</param>
    /// <returns>Template type identifier (e.g., "main", "program", "course", "module", "lesson").</returns>
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
    /// Scans folder content and categorizes files.
    /// </summary>
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
    /// Analyzes a file and extracts metadata.
    /// </summary>
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
    /// Categorizes content by filename patterns.
    /// </summary>
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
    /// Generates the index content for a folder.
    /// </summary>
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
    /// Gets the icon for a content type.
    /// </summary>
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
    /// Gets the display title for a content type.
    /// </summary>
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
    /// Gets the root index filename by finding the first main index in the vault.
    /// </summary>
    /// <param name="vaultPath">Path to the vault root.</param>
    /// <returns>The filename for the root index without extension.</returns>
    private string GetRootIndexFilename(string vaultPath)
    {
        // Prefer the explicitly provided vault path over the default vault path
        // This allows for temporary overrides like test vaults
        string effectiveVaultPath = !string.IsNullOrEmpty(vaultPath)
            ? vaultPath
            : _defaultVaultRootPath;

        // Ensure the path has consistent formatting
        effectiveVaultPath = effectiveVaultPath.Replace('/', Path.DirectorySeparatorChar)
                                              .TrimEnd(Path.DirectorySeparatorChar);        // Look for the first main index file in the vault structure
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
    /// Checks if a markdown file is a main index file by examining its frontmatter.
    /// </summary>
    /// <param name="filePath">Path to the markdown file.</param>
    /// <returns>True if the file has template-type: main in its frontmatter.</returns>
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
