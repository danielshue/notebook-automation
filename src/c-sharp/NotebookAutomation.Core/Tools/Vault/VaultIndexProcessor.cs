// <copyright file="VaultIndexProcessor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Tools/Vault/VaultIndexProcessor.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Models;
using NotebookAutomation.Core.Utils;

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
    MetadataTemplateManager templateManager,
    MetadataHierarchyDetector hierarchyDetector,
    CourseStructureExtractor structureExtractor,
    IYamlHelper yamlHelper,
    MarkdownNoteBuilder noteBuilder,
    AppConfig appConfig,
    string vaultRootPath = "")
{
    private readonly ILogger<VaultIndexProcessor> logger = logger;
    private readonly MetadataTemplateManager templateManager = templateManager;
    private readonly MetadataHierarchyDetector hierarchyDetector = hierarchyDetector;
    private readonly CourseStructureExtractor structureExtractor = structureExtractor;
    private readonly IYamlHelper yamlHelper = yamlHelper;
    private readonly MarkdownNoteBuilder noteBuilder = noteBuilder;
    private readonly string defaultVaultRootPath = !string.IsNullOrEmpty(vaultRootPath)
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
            this.logger.LogDebug("Generating index for folder: {FolderPath}", folderPath);
            this.logger.LogInformation("=== GENERATING INDEX ===");
            this.logger.LogInformation("Folder Path: {FolderPath}", folderPath);
            this.logger.LogInformation("Vault Path: {VaultPath}", vaultPath);
            Console.WriteLine($"DEBUG: Starting GenerateIndexAsync - FolderPath: {folderPath}, VaultPath: {vaultPath}");

            // Validate vault path - use _defaultVaultRootPath if not provided
            if (string.IsNullOrEmpty(vaultPath))
            {
                if (string.IsNullOrEmpty(this.defaultVaultRootPath))
                {
                    this.logger.LogError("Cannot determine vault root path. Neither vaultPath parameter nor AppConfig.Paths.NotebookVaultFullpathRoot is provided.");
                    return false;
                }

                this.logger.LogDebug("No vault path provided, using default from configuration: {DefaultPath}", this.defaultVaultRootPath);
                vaultPath = this.defaultVaultRootPath;
            }

            // Calculate hierarchy level based on depth from vault root
            int hierarchyLevel = this.CalculateHierarchyLevel(folderPath, vaultPath);
            this.logger.LogInformation("Calculated hierarchy level: {Level} for folder: {Folder}", hierarchyLevel, folderPath);
            Console.WriteLine($"DEBUG: Hierarchy level {hierarchyLevel} calculated for {folderPath}");

            // Create index file name based on folder name
            string folderName = Path.GetFileName(folderPath) ?? "Index";            // Determine template type based on hierarchy level and folder name
            string templateType = this.DetermineTemplateType(hierarchyLevel, folderName);
            this.logger.LogInformation("Determined template type: {TemplateType} for folder: {Folder} at level: {Level}", templateType, folderName, hierarchyLevel);
            Console.WriteLine($"DEBUG: Template type '{templateType}' determined for '{folderName}' at level {hierarchyLevel}"); // Get template using the actual available method
            var template = this.templateManager.GetTemplate(templateType);
            if (template == null)
            {
                this.logger.LogWarning("Template not found for type: {TemplateType}", templateType);
                return false;
            }

            Console.WriteLine($"DEBUG: Template keys: {string.Join(", ", template.Keys)}");
            if (template.ContainsKey("program"))
            {
                Console.WriteLine($"DEBUG: Template contains program: {template["program"]}");
            }

            if (template.ContainsKey("course"))
            {
                Console.WriteLine($"DEBUG: Template contains course: {template["course"]}");
            }

            if (template.ContainsKey("class"))
            {
                Console.WriteLine($"DEBUG: Template contains class: {template["class"]}");
            }

            string indexFileName = $"{folderName}.md";
            string indexFilePath = Path.Combine(folderPath, indexFileName);            // Check if index already exists and force is not set
            if (File.Exists(indexFilePath) && !forceOverwrite)
            {
                this.logger.LogInformation("Skipping index file (already exists, use --force to overwrite): {IndexPath}", indexFilePath);
                return false;
            }

            if (dryRun)
            {
                this.logger.LogInformation("DRY RUN: Would generate index file: {IndexPath}", indexFilePath);
                return true;
            } // Scan folder for content

            var files = await this.ScanFolderContentAsync(folderPath, vaultPath).ConfigureAwait(false);

            // Get hierarchy info
            var hierarchyInfo = this.hierarchyDetector.FindHierarchyInfo(folderPath);            // Generate index content
            string indexContent = await this.GenerateIndexContentAsync(
                folderPath,
                vaultPath,
                template,
                files,
                hierarchyInfo,
                hierarchyLevel).ConfigureAwait(false);

            // Write index file
            await File.WriteAllTextAsync(indexFilePath, indexContent).ConfigureAwait(false);

            this.logger.LogInformation("Generated index file: {IndexPath}", indexFilePath);
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error generating index for folder: {FolderPath}", folderPath);
            return false;
        }
    }

    /// <summary>
    /// Calculates the hierarchy level based on folder depth from vault root.
    /// </summary>
    private int CalculateHierarchyLevel(string folderPath, string vaultPath)
    {
        // Prefer the explicitly provided vault path over the default vault path
        // This allows for temporary overrides like test vaults
        string effectiveVaultPath = !string.IsNullOrEmpty(vaultPath)
            ? vaultPath
            : this.defaultVaultRootPath;

        // Ensure the path has consistent formatting (normalize separators and trim trailing separator)
        effectiveVaultPath = Path.TrimEndingDirectorySeparator(effectiveVaultPath.Replace('/', Path.DirectorySeparatorChar));
        this.logger.LogDebug("Using vault root path: {VaultPath} for hierarchy calculation", effectiveVaultPath);

        var relativePath = Path.GetRelativePath(effectiveVaultPath, folderPath);
        this.logger.LogInformation("CalculateHierarchyLevel: VaultPath='{VaultPath}', FolderPath='{FolderPath}', RelativePath='{RelativePath}'", effectiveVaultPath, folderPath, relativePath);
        Console.WriteLine($"DEBUG: CalculateHierarchyLevel - VaultPath: '{effectiveVaultPath}', FolderPath: '{folderPath}', RelativePath: '{relativePath}'");

        if (relativePath == ".")
        {
            Console.WriteLine("DEBUG: Vault root detected, returning level 1");
            return 1; // Vault root starts at level 1
        }

        int level = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length + 1;
        Console.WriteLine($"DEBUG: Calculated level {level} from relative path '{relativePath}'");
        return level;
    }

    /// <summary>
    /// Determines the template type based on hierarchy level and folder name.
    /// </summary>
    /// <param name="hierarchyLevel">The hierarchy level of the folder.</param>
    /// <param name="folderName">Optional folder name to check for special cases.</param>
    private string DetermineTemplateType(int hierarchyLevel, string? folderName = null)
    {
        // Main program folders (level 1) that are identified as such get main template type
        if (hierarchyLevel == 1 && folderName != null)
        {
            this.logger.LogDebug("Main program folder detected, using main template type");
            return "main";
        }

        // Check for special folder names that override hierarchy-based template type
        if (!string.IsNullOrEmpty(folderName))
        {
            string lowerFolderName = folderName.ToLowerInvariant();              // Special folder types that can appear at any level beyond course level (4+)

            // Treat content folders like Case-Studies, Lessons, etc. as modules
            if (hierarchyLevel >= 4)
            {
                if (lowerFolderName.Contains("case-studies") || lowerFolderName.Contains("case-study") ||
                    lowerFolderName.Contains("lesson") || lowerFolderName.Contains("lessons") ||
                    lowerFolderName.Contains("module") || lowerFolderName.Contains("modules") ||
                    lowerFolderName.Contains("readings") || lowerFolderName.Contains("reading") ||
                    lowerFolderName.Contains("resources") || lowerFolderName.Contains("resource") ||
                    lowerFolderName.Contains("assignment") || lowerFolderName.Contains("assignments") ||
                    lowerFolderName.Contains("project") || lowerFolderName.Contains("projects") ||
                    lowerFolderName.Contains("live class") || lowerFolderName.Contains("live-class"))
                {
                    this.logger.LogDebug("Special content folder '{FolderName}' detected at level {Level}, treating as module", folderName, hierarchyLevel);
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
            _ => "module",           // Deep subdirectories use module template
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
            var fileInfo = await this.AnalyzeFileAsync(filePath, vaultPath).ConfigureAwait(false);
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
            var frontmatter = this.yamlHelper.ExtractFrontmatter(content);

            if (!string.IsNullOrEmpty(frontmatter))
            {
                var yamlData = this.yamlHelper.ParseYamlToDictionary(frontmatter);

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
            var metadata = new Dictionary<string, object>();
            this.structureExtractor.ExtractModuleAndLesson(filePath, metadata);

            if (metadata.TryGetValue("module", out var extractedModule))
            {
                fileInfo.Module ??= extractedModule?.ToString();
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error analyzing file: {FilePath}", filePath);
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
        Console.WriteLine($"DEBUG: Before UpdateMetadataWithHierarchy - frontmatter keys: {string.Join(", ", frontmatter.Keys)}");
        Console.WriteLine($"DEBUG: hierarchyInfo keys: {string.Join(", ", hierarchyInfo.Keys)}");
        foreach (var kvp in hierarchyInfo)
        {
            Console.WriteLine($"DEBUG: hierarchyInfo[{kvp.Key}] = '{kvp.Value}'");
        }

        if (frontmatter.ContainsKey("program"))
        {
            Console.WriteLine($"DEBUG: Before - frontmatter contains program: {frontmatter["program"]}");
        }

        if (frontmatter.ContainsKey("course"))
        {
            Console.WriteLine($"DEBUG: Before - frontmatter contains course: {frontmatter["course"]}");
        }

        if (frontmatter.ContainsKey("class"))
        {
            Console.WriteLine($"DEBUG: Before - frontmatter contains class: {frontmatter["class"]}");
        }

        frontmatter = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(frontmatter, hierarchyInfo, templateType);

        Console.WriteLine($"DEBUG: After UpdateMetadataWithHierarchy - frontmatter keys: {string.Join(", ", frontmatter.Keys)}");
        if (frontmatter.ContainsKey("program"))
        {
            Console.WriteLine($"DEBUG: After - frontmatter contains program: {frontmatter["program"]}");
        }

        if (frontmatter.ContainsKey("course"))
        {
            Console.WriteLine($"DEBUG: After - frontmatter contains course: {frontmatter["course"]}");
        }

        if (frontmatter.ContainsKey("class"))
        {
            Console.WriteLine($"DEBUG: After - frontmatter contains class: {frontmatter["class"]}");
        }

        frontmatter["type"] = "index";
        frontmatter["date-created"] = DateTime.UtcNow.ToString("yyyy-MM-dd");        // Ensure banner is present (should be from template, but just in case)
        if (!frontmatter.ContainsKey("banner"))
        {
            frontmatter["banner"] = "gies-banner.png";
        }
        else if (frontmatter["banner"] is string bannerValue)
        {
            // Remove quotes if present
            if (bannerValue.StartsWith("\"") && bannerValue.EndsWith("\""))
            {
                bannerValue = bannerValue.Substring(1, bannerValue.Length - 2);
            }

            // Remove wiki link brackets if present
            if (bannerValue.StartsWith("[[") && bannerValue.EndsWith("]]"))
            {
                bannerValue = bannerValue.Substring(2, bannerValue.Length - 4);
            }

            frontmatter["banner"] = bannerValue;
        }

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
                backLinkTarget = this.GetRootIndexFilename(vaultPath);
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
            string rootIndex = this.GetRootIndexFilename(vaultPath);

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
                Path.Combine(!string.IsNullOrEmpty(vaultPath) ? vaultPath : this.defaultVaultRootPath, "..", "config", "BaseBlockTemplate.yaml"),

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
                    this.logger.LogDebug("Found BaseBlockTemplate.yaml at: {Path}", path);
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
                        this.logger.LogDebug("Found BaseBlockTemplate.yaml by walking up from executable: {Path}", candidate);
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

        return Task.FromResult(this.noteBuilder.BuildNote(frontmatter, bodyContent));
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
            : this.defaultVaultRootPath;

        // Ensure the path has consistent formatting
        effectiveVaultPath = effectiveVaultPath.Replace('/', Path.DirectorySeparatorChar)
                                              .TrimEnd(Path.DirectorySeparatorChar);        // Look for the first main index file in the vault structure
        try
        {
            this.logger.LogDebug("Searching for main index files in vault: {VaultPath}", effectiveVaultPath);

            // Search for markdown files with main index type in the vault
            var allMarkdownFiles = Directory.GetFiles(effectiveVaultPath, "*.md", SearchOption.AllDirectories);
            this.logger.LogDebug("Found {Count} markdown files in vault", allMarkdownFiles.Length);

            var mainIndexFiles = allMarkdownFiles
                .Where(file => this.IsMainIndexFile(file))
                .OrderBy(file => file.Length) // Prefer files closer to root (shorter paths)
                .ToList();

            this.logger.LogDebug("Found {Count} main index files", mainIndexFiles.Count);
            foreach (var file in mainIndexFiles)
            {
                this.logger.LogDebug("Main index file found: {FilePath}", file);
            }

            if (mainIndexFiles.Any())
            {
                string mainIndexFile = mainIndexFiles.First();
                string fileName = Path.GetFileNameWithoutExtension(mainIndexFile);
                this.logger.LogDebug("Using main index file: {FileName} at {FilePath}", fileName, mainIndexFile);
                return fileName;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Error searching for main index file in vault");
        }

        // Fallback: use the vault root folder name
        string vaultRootFolder = Path.GetFileName(effectiveVaultPath);
        if (string.IsNullOrEmpty(vaultRootFolder))
        {
            this.logger.LogWarning("Unable to determine vault root folder name, using 'main-index' as default");
            vaultRootFolder = "main-index";
        }
        else
        {
            this.logger.LogDebug("Using vault root folder name: {FolderName} for index filename", vaultRootFolder);
        }

        return vaultRootFolder;
    }

    /// <summary>
    /// Checks if a markdown file is a main index file by examining its frontmatter.
    /// </summary>
    /// <param name="filePath">Path to the markdown file.</param>         /// <returns>True if the file has template-type: main in its frontmatter.</returns>
    private bool IsMainIndexFile(string filePath)
    {
        try
        {
            string content = File.ReadAllText(filePath);
            string? frontmatter = this.yamlHelper.ExtractFrontmatter(content);

            if (!string.IsNullOrEmpty(frontmatter))
            {
                var yamlData = this.yamlHelper.ParseYamlToDictionary(frontmatter);
                if (yamlData.TryGetValue("template-type", out var templateType))
                {
                    return templateType?.ToString() == "main";
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Error checking if file is main index: {FilePath}", filePath);
        }

        return false;
    }
}
