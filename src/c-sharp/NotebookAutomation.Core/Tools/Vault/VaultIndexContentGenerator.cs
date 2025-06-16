// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tools.Vault;

/// <summary>
/// Generates comprehensive index content for vault files with intelligent hierarchy-based organization and dynamic content structuring.
/// </summary>
/// <remarks>
/// <para>
/// The VaultIndexContentGenerator is a specialized component responsible for creating rich, structured index content
/// that adapts to different hierarchy levels within an Obsidian vault. It serves as the content generation engine
/// for the vault indexing system, handling frontmatter generation, navigation creation, content organization,
/// and integration with advanced features like Obsidian Bases.
/// </para>
/// <para>
/// Architecture and Design:
/// This class follows the Single Responsibility Principle by focusing exclusively on content generation,
/// separated from file I/O operations and higher-level processing logic. It uses dependency injection
/// for its core dependencies and provides internal methods for comprehensive testability.
/// </para>
/// <para>
/// Key Capabilities:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Hierarchy-Aware Content Generation</strong>: Adapts output based on vault hierarchy level (1-6+)</description></item>
/// <item><description><strong>Dynamic Navigation</strong>: Generates contextual back/home links and dashboard shortcuts</description></item>
/// <item><description><strong>Content Type Categorization</strong>: Organizes files by type (readings, videos, assignments, etc.)</description></item>
/// <item><description><strong>Obsidian Bases Integration</strong>: Adds advanced query blocks at class level for dynamic content</description></item>
/// <item><description><strong>Template Processing</strong>: Integrates with metadata hierarchy detection for consistent frontmatter</description></item>
/// <item><description><strong>Visual Organization</strong>: Uses icons, sections, and consistent markdown formatting</description></item>
/// </list>
/// <para>
/// Hierarchy System:
/// The generator operates on a 1-based hierarchy level system that determines content structure and features:
/// </para>
/// <list type="number">
/// <item><description><strong>Level 1: Vault Root (Main)</strong> - Primary entry point with program listings</description></item>
/// <item><description><strong>Level 2: Program Level</strong> - Course listings within academic programs</description></item>
/// <item><description><strong>Level 3: Course Level</strong> - Class and content organization within courses</description></item>
/// <item><description><strong>Level 4: Class Level</strong> - Module organization with Obsidian Bases integration</description></item>
/// <item><description><strong>Level 5: Module Level</strong> - Lesson and content groupings</description></item>
/// <item><description><strong>Level 6+: Lesson Level</strong> - Individual content and resource organization</description></item>
/// </list>
/// <para>
/// Content Organization Strategy:
/// The generator employs intelligent content organization by analyzing file metadata, categorizing content types,
/// and applying hierarchy-appropriate templates. It ensures consistent navigation patterns while adapting to
/// the specific needs of each hierarchy level.
/// </para>
/// <para>
/// Performance Considerations:
/// The class includes caching mechanisms for expensive operations like root index discovery and uses
/// efficient string building techniques for large content generation scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage with dependency injection
/// var generator = serviceProvider.GetService&lt;IVaultIndexContentGenerator&gt;();
///
/// // Generate content for a class-level index
/// var template = await templateManager.GetTemplateAsync("class");
/// var files = await fileAnalyzer.AnalyzeFolderAsync(classPath);
/// var hierarchyInfo = hierarchyDetector.FindHierarchyInfo(classPath);
///
/// string indexContent = await generator.GenerateIndexContentAsync(
///     classPath,
///     vaultPath,
///     template,
///     files,
///     hierarchyInfo,
///     4); // Class level
///
/// // Result includes:
/// // - Structured YAML frontmatter
/// // - Navigation with back/home links
/// // - Categorized content sections
/// // - Obsidian Bases query integration
/// // - Visual hierarchy with icons
///
/// // Testing individual components
/// var frontmatter = generator.PrepareFrontmatter(template, folderPath, hierarchyInfo);
/// var sections = generator.GenerateContentSections(folderPath, vaultPath, frontmatter, files, hierarchyInfo, level);
/// </code>
/// </example>
public class VaultIndexContentGenerator(
    ILogger<VaultIndexContentGenerator> logger,
    IMetadataHierarchyDetector hierarchyDetector,
    MarkdownNoteBuilder noteBuilder,
    AppConfig appConfig) : IVaultIndexContentGenerator
{
    private readonly ILogger<VaultIndexContentGenerator> _logger = logger;
    private readonly IMetadataHierarchyDetector _hierarchyDetector = hierarchyDetector;
    private readonly MarkdownNoteBuilder _noteBuilder = noteBuilder;
    private readonly string _defaultVaultRootPath = appConfig.Paths.NotebookVaultFullpathRoot;

    // Cache for discovered root index filenames to avoid expensive file system lookups
    private readonly Dictionary<string, string> _discoveredIndexFilenames = new();

    /// <summary>
    /// Generates comprehensive index content for a vault folder with intelligent hierarchy detection and content organization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method serves as the primary entry point for generating index content within vault folders.
    /// It orchestrates the entire content generation process, from frontmatter preparation to final
    /// markdown assembly, ensuring consistency and hierarchy-appropriate organization.
    /// </para>
    /// <para>
    /// Content Generation Process:
    /// </para>
    /// <list type="number">
    /// <item><description>Prepares frontmatter with hierarchy-specific metadata</description></item>
    /// <item><description>Generates content sections based on hierarchy level and file types</description></item>
    /// <item><description>Assembles final markdown content using MarkdownNoteBuilder</description></item>
    /// </list>
    /// <para>
    /// Hierarchy-Specific Behavior:
    /// The generator adapts its output based on the hierarchy level, providing appropriate navigation,
    /// content organization, and special features like Obsidian Bases integration at class level (level 4).
    /// </para>
    /// </remarks>
    /// <param name="folderPath">The absolute path to the folder for which to generate index content.</param>
    /// <param name="vaultPath">The absolute path to the vault root directory.</param>
    /// <param name="template">The template dictionary containing base frontmatter and configuration.</param>
    /// <param name="files">Collection of analyzed vault file information for content categorization.</param>
    /// <param name="hierarchyInfo">Dictionary containing hierarchy-specific metadata (course, class, etc.).</param>
    /// <param name="hierarchyLevel">The hierarchy level (1-6+) determining content structure and features.</param>
    /// <returns>
    /// A Task containing the complete markdown content string with frontmatter and body sections.
    /// The content includes navigation, organized file listings, and hierarchy-appropriate features.
    /// </returns>
    /// <example>
    /// <code>
    /// var template = new Dictionary&lt;string, object&gt;
    /// {
    ///     ["template-type"] = "class",
    ///     ["title"] = "Course Template",
    ///     ["banner"] = "'[[banner.png]]'"
    /// };
    ///
    /// var files = new List&lt;VaultFileInfo&gt;
    /// {
    ///     new() { FileName = "reading1", ContentType = "reading", Title = "Introduction" },
    ///     new() { FileName = "assignment1", ContentType = "assignment", Title = "Assignment 1" }
    /// };
    ///
    /// var hierarchyInfo = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["course"] = "Finance 101",
    ///     ["class"] = "Corporate Finance"
    /// };
    ///
    /// string content = await generator.GenerateIndexContentAsync(
    ///     @"C:\vault\Finance\Corporate-Finance",
    ///     @"C:\vault",
    ///     template,
    ///     files,
    ///     hierarchyInfo,
    ///     4); // Class level
    ///
    /// // Generated content includes:
    /// // - YAML frontmatter with hierarchy metadata
    /// // - Navigation links (back, home, dashboard)
    /// // - Organized content sections by type
    /// // - Obsidian Bases integration (at class level)
    /// </code>
    /// </example>
    public Task<string> GenerateIndexContentAsync(
        string folderPath,
        string vaultPath,
        Dictionary<string, object> template,
        List<VaultFileInfo> files,
        Dictionary<string, string> hierarchyInfo,
        int hierarchyLevel)
    {
        var frontmatter = PrepareFrontmatter(template, folderPath, hierarchyInfo);
        var contentSections = GenerateContentSections(folderPath, vaultPath, frontmatter, files, hierarchyInfo, hierarchyLevel);
        var bodyContent = string.Join("\n\n", contentSections);

        return Task.FromResult(_noteBuilder.BuildNote(frontmatter, bodyContent));
    }

    /// <summary>
    /// Prepares and enhances frontmatter with hierarchy-specific metadata and template processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates the YAML frontmatter for the index file by combining the provided template
    /// with hierarchy-specific metadata and standardized fields. It ensures consistent frontmatter
    /// structure while preserving template-specific customizations.
    /// </para>
    /// <para>
    /// Processing Steps:
    /// </para>
    /// <list type="number">
    /// <item><description>Clones the template to avoid mutation</description></item>
    /// <item><description>Sets the title from folder name using friendly formatting</description></item>
    /// <item><description>Applies hierarchy metadata through MetadataHierarchyDetector</description></item>
    /// <item><description>Adds standard fields (type, date-created)</description></item>
    /// <item><description>Ensures required fields like banner are present</description></item>
    /// </list>
    /// </remarks>
    /// <param name="template">The base template dictionary containing initial frontmatter configuration.</param>
    /// <param name="folderPath">The folder path used to generate the title and context.</param>
    /// <param name="hierarchyInfo">Dictionary containing hierarchy metadata to be applied.</param>
    /// <returns>
    /// A dictionary containing the complete frontmatter with template fields, hierarchy metadata,
    /// and standardized index fields ready for YAML serialization.
    /// </returns>
    internal Dictionary<string, object> PrepareFrontmatter(
        Dictionary<string, object> template,
        string folderPath,
        Dictionary<string, string> hierarchyInfo)
    {
        // Clone the template to avoid mutating the original
        var frontmatter = new Dictionary<string, object>(template);

        frontmatter["title"] = FriendlyTitleHelper.GetFriendlyTitleFromFileName(Path.GetFileName(folderPath) ?? "Index");

        // Apply hierarchy metadata based on template type using MetadataHierarchyDetector
        string? templateType = frontmatter.GetValueOrDefault("template-type")?.ToString();
        _logger.LogDebug($"Before UpdateMetadataWithHierarchy - frontmatter keys: {string.Join(", ", frontmatter.Keys)}");
        _logger.LogDebug($"hierarchyInfo keys: {string.Join(", ", hierarchyInfo.Keys)}");

        foreach (var kvp in hierarchyInfo)
        {
            _logger.LogDebug($"hierarchyInfo[{kvp.Key}] = '{kvp.Value}'");
        }

        LogFrontmatterDebugInfo("Before", frontmatter);

        var updatedFrontmatter = _hierarchyDetector.UpdateMetadataWithHierarchy(
            frontmatter.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value),
            hierarchyInfo,
            templateType);

        frontmatter.Clear();
        foreach (var kvp in updatedFrontmatter)
        {
            if (kvp.Value is not null)
                frontmatter[kvp.Key] = kvp.Value;
        }

        LogFrontmatterDebugInfo("After", frontmatter);

        frontmatter["type"] = "index";
        frontmatter["date-created"] = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Ensure banner is present (should be from template, but just in case)
        if (!frontmatter.ContainsKey("banner"))
        {
            frontmatter["banner"] = "'[[gies-banner.png]]'";
        }

        return frontmatter;
    }

    /// <summary>
    /// Generates all content sections including navigation, hierarchy-specific content, and special integrations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates the structured body content for the index file by orchestrating multiple
    /// content generation strategies. It adapts the content structure based on hierarchy level and
    /// integrates special features like Obsidian Bases for enhanced functionality.
    /// </para>
    /// <para>
    /// Content Structure:
    /// </para>
    /// <list type="number">
    /// <item><description><strong>Title Section</strong>: H1 heading from frontmatter title</description></item>
    /// <item><description><strong>Navigation</strong>: Context-aware back/home links and shortcuts</description></item>
    /// <item><description><strong>Main Content</strong>: Hierarchy-specific organization (programs, courses, etc.)</description></item>
    /// <item><description><strong>Special Features</strong>: Obsidian Bases integration at class level</description></item>
    /// </list>
    /// <para>
    /// Hierarchy Adaptations:
    /// The method dynamically adjusts content based on hierarchy level, providing appropriate
    /// organizational structures from program listings at the root to detailed content organization
    /// at lesson levels.
    /// </para>
    /// </remarks>
    /// <param name="folderPath">The folder path for content generation context.</param>
    /// <param name="vaultPath">The vault root path for navigation and relative linking.</param>
    /// <param name="frontmatter">The prepared frontmatter containing metadata and configuration.</param>
    /// <param name="files">Collection of analyzed files for content categorization and listing.</param>
    /// <param name="hierarchyInfo">Hierarchy metadata for contextual content generation.</param>
    /// <param name="hierarchyLevel">The hierarchy level determining content structure and features.</param>
    /// <returns>
    /// A list of content section strings that will be joined to form the complete markdown body.
    /// Each string represents a distinct content section (navigation, listings, etc.).
    /// </returns>
    internal List<string> GenerateContentSections(
        string folderPath,
        string vaultPath,
        Dictionary<string, object> frontmatter,
        List<VaultFileInfo> files,
        Dictionary<string, string> hierarchyInfo,
        int hierarchyLevel)
    {
        var contentSections = new List<string>();

        // Add the title as an H1 heading
        string headerTitle = frontmatter.TryGetValue("title", out var titleValue) ? titleValue?.ToString() ?? "Index" : "Index";
        contentSections.Add($"# {headerTitle}");
        contentSections.Add(string.Empty);

        // Special handling for main (check template-type in frontmatter)
        bool isMain = frontmatter.ContainsKey("template-type") && frontmatter["template-type"]?.ToString() == "main";

        AddNavigationSection(contentSections, vaultPath, folderPath, hierarchyLevel, isMain);

        var groupedFiles = files.GroupBy(f => f.ContentType).ToDictionary(g => g.Key, g => g.ToList());

        if (isMain)
        {
            AddMainIndexContent(contentSections, folderPath);
        }
        else
        {
            AddHierarchySpecificContent(contentSections, folderPath, hierarchyLevel, groupedFiles);
            if (hierarchyLevel == 4) // Class level - add Bases integration
            {
                AddBasesIntegration(contentSections, hierarchyInfo, vaultPath);
            }
        }

        return contentSections;
    }

    /// <summary>
    /// Adds appropriate navigation section based on hierarchy level and main index status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates contextual navigation links that adapt to the current hierarchy level
    /// and vault structure. It provides consistent navigation patterns while respecting the
    /// specific needs of main indices versus sub-level indices.
    /// </para>
    /// <para>
    /// Navigation Patterns:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Main Index</strong>: Dashboard and Classes Assignments links only</description></item>
    /// <item><description><strong>Program Level</strong>: Home, Dashboard, and Classes Assignments (no back link)</description></item>
    /// <item><description><strong>Sub-levels</strong>: Back link, Home, Dashboard, and Classes Assignments</description></item>
    /// </list>
    /// <para>
    /// The navigation uses Obsidian's internal linking format with appropriate icons for
    /// visual consistency and user experience enhancement.
    /// </para>
    /// </remarks>
    /// <param name="contentSections">The content sections list to which navigation will be added.</param>
    /// <param name="vaultPath">The vault root path for determining home link target.</param>
    /// <param name="folderPath">The current folder path for generating back links.</param>
    /// <param name="hierarchyLevel">The hierarchy level determining navigation structure.</param>
    /// <param name="isMain">Whether this is the main vault index, affecting navigation style.</param>
    internal void AddNavigationSection(List<string> contentSections, string vaultPath, string folderPath, int hierarchyLevel, bool isMain)
    {
        if (isMain)
        {
            // Add just Dashboard and Classes Assignments links for the main index
            contentSections.Add("📊 [[Dashboard]] | 📝 [[Classes Assignments]]");
            contentSections.Add(string.Empty);
        }
        else
        {
            // Get the root index filename for the Home link
            string rootIndex = GetRootIndexFilename(vaultPath, folderPath); if (hierarchyLevel == 1) // Program index - only show Home and other navigation, no back link
            {
                contentSections.Add($"🏠 [Home]({rootIndex}) | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]");
            }
            else // Course, Class, Module, and other indices - show full navigation with back link
            {
                string backLinkTarget = GetBackLinkTarget(folderPath, hierarchyLevel);
                string backLinkText = FriendlyTitleHelper.GetFriendlyTitleFromFileName(backLinkTarget);
                contentSections.Add($"🔙 [[{backLinkTarget}|{backLinkText}]] | 🏠 [Home]({rootIndex}) | 📊 [[Dashboard]] | 📝 [[Classes Assignments]]");
            }

            contentSections.Add(string.Empty);
        }
    }

    /// <summary>
    /// Adds content sections for the main vault index showing programs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates the content structure for the main vault index (hierarchy level 1),
    /// which serves as the primary entry point for the entire vault. It focuses on listing
    /// all available programs in an organized, user-friendly format.
    /// </para>
    /// <para>
    /// Content Structure:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Programs Section</strong>: H2 heading with folder icon</description></item>
    /// <item><description><strong>Program Listings</strong>: Each subfolder as a linked item with friendly names</description></item>
    /// <item><description><strong>Visual Organization</strong>: Uses folder icons and consistent formatting</description></item>
    /// </list>
    /// <para>
    /// The method automatically discovers subdirectories in the vault root and converts
    /// them into organized navigation links using friendly title formatting.
    /// </para>
    /// </remarks>
    /// <param name="contentSections">The content sections list to which main index content will be added.</param>
    /// <param name="folderPath">The vault root folder path to scan for program subdirectories.</param>
    internal void AddMainIndexContent(List<string> contentSections, string folderPath)
    {
        var subFolders = GetOrderedSubfolders(folderPath);

        if (subFolders.Any())
        {
            contentSections.Add("## Programs");
            contentSections.Add(string.Empty);

            foreach (var subFolder in subFolders)
            {
                string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                contentSections.Add($"- 📁 [[{subFolder}|{friendlyName}]]");
            }
        }
    }

    /// <summary>
    /// Adds hierarchy-specific content sections based on the current level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements the core hierarchy-aware content generation strategy by selecting
    /// appropriate content structures based on the vault hierarchy level. Each level has distinct
    /// organizational patterns and content requirements tailored to its specific purpose.
    /// </para>
    /// <para>
    /// Hierarchy Level Behaviors:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Level 0 (Main)</strong>: Programs listing only</description></item>
    /// <item><description><strong>Level 1 (Program)</strong>: Courses listing only</description></item>
    /// <item><description><strong>Level 2 (Course)</strong>: Courses + course-specific content</description></item>
    /// <item><description><strong>Level 3+ (Class/Module)</strong>: Module-specific organization with content categorization</description></item>
    /// </list>
    /// <para>
    /// The method uses a switch statement to ensure optimal performance and clear separation
    /// of hierarchy-specific logic, making it easy to maintain and extend.
    /// </para>
    /// </remarks>
    /// <param name="contentSections">The content sections list to which hierarchy-specific content will be added.</param>
    /// <param name="folderPath">The current folder path for content discovery and organization.</param>
    /// <param name="hierarchyLevel">The hierarchy level (0-6+) determining content structure.</param>
    /// <param name="groupedFiles">Dictionary of files grouped by content type for organized presentation.</param>
    internal void AddHierarchySpecificContent(List<string> contentSections, string folderPath, int hierarchyLevel, Dictionary<string, List<VaultFileInfo>> groupedFiles)
    {
        var subFolders = GetOrderedSubfolders(folderPath);

        switch (hierarchyLevel)
        {
            case 0: // Main index - show programs
                AddSubfolderListing(contentSections, subFolders, "Programs");
                break;

            case 1: // Program index - show only courses
                AddSubfolderListing(contentSections, subFolders, "Courses");
                break;

            case 2: // Course index - show courses and content
                AddSubfolderListing(contentSections, subFolders, "Courses");
                AddCourseSpecificContent(contentSections, groupedFiles);
                break;

            case 3: // Class index - show classes
                AddSubfolderListing(contentSections, subFolders, "Classes");
                break;

            case 4: // Class level - only show Bases blocks (no modules or files)
                // Bases integration will be added separately
                break;

            default: // Module level or below - show content by type
                AddModuleLevelContent(contentSections, subFolders, groupedFiles);
                break;
        }
    }

    /// <summary>
    /// Adds course-specific content sections for readings and case studies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates specialized content sections tailored for course-level indices
    /// (hierarchy level 2). It focuses on academic content types that are most relevant
    /// at the course level, particularly readings and case studies.
    /// </para>
    /// <para>
    /// Content Organization:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Readings Section</strong>: Organized list of reading materials with book icons</description></item>
    /// <item><description><strong>Case Studies</strong>: Notes and case study materials with document icons</description></item>
    /// <item><description><strong>Alphabetical Ordering</strong>: Content sorted by title for easy navigation</description></item>
    /// </list>
    /// <para>
    /// The method uses content type filtering to ensure only relevant academic materials
    /// are displayed at the course level, providing a clean, focused view for students
    /// and instructors.
    /// </para>
    /// </remarks>
    /// <param name="contentSections">The content sections list to which course-specific content will be added.</param>
    /// <param name="groupedFiles">Dictionary of files grouped by content type, filtered for course-relevant materials.</param>
    internal void AddCourseSpecificContent(List<string> contentSections, Dictionary<string, List<VaultFileInfo>> groupedFiles)
    {
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

    /// <summary>
    /// Adds module-level content with subfolders and content by type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates comprehensive content organization for module-level indices
    /// (hierarchy level 3+). It combines structural navigation (subfolders) with detailed
    /// content categorization to provide both hierarchical and topical organization.
    /// </para>
    /// <para>
    /// Content Structure:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Modules Section</strong>: Lists all subfolder modules with friendly names</description></item>
    /// <item><description><strong>Content by Type</strong>: Organized sections for readings, videos, assignments, etc.</description></item>
    /// <item><description><strong>Comprehensive Coverage</strong>: Includes all major academic content types</description></item>
    /// </list>
    /// <para>
    /// The method serves as the primary organization strategy for class and module levels,
    /// providing both structural navigation and detailed content access in a single view.
    /// </para>
    /// </remarks>
    /// <param name="contentSections">The content sections list to which module-level content will be added.</param>
    /// <param name="subFolders">List of subfolder names to be included in the modules navigation.</param>
    /// <param name="groupedFiles">Dictionary of files grouped by content type for detailed organization.</param>
    internal void AddModuleLevelContent(List<string> contentSections, List<string> subFolders, Dictionary<string, List<VaultFileInfo>> groupedFiles)
    {
        if (subFolders.Any())
        {
            contentSections.Add("## Modules");
            foreach (var subFolder in subFolders)
            {
                string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                contentSections.Add($"- [[{subFolder}|{friendlyName}]]");
            }
            contentSections.Add(string.Empty);
        }

        // Add content by type for module level and below
        AddContentByType(contentSections, groupedFiles, ["reading", "video", "transcript", "assignment", "discussion", "note"]);
    }

    /// <summary>
    /// Adds Obsidian Bases integration blocks for class-level indices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method integrates advanced Obsidian Bases functionality specifically for class-level
    /// indices (hierarchy level 4). Obsidian Bases provides dynamic content querying and
    /// organization capabilities that enhance the vault's intelligence and automation.
    /// </para>
    /// <para>
    /// Bases Integration Features:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Dynamic Content Blocks</strong>: Auto-updating content based on metadata queries</description></item>
    /// <item><description><strong>Content Type Filtering</strong>: Separate blocks for readings, instructions, case studies, and videos</description></item>
    /// <item><description><strong>Hierarchy Context</strong>: Uses course, class, and module information for targeted queries</description></item>
    /// <item><description><strong>Visual Organization</strong>: Each content type has distinct icons and headers</description></item>
    /// </list>
    /// <para>
    /// The method requires a properly configured BaseBlockTemplate.yaml file and uses
    /// the BaseBlockGenerator to create Obsidian-compatible query blocks that automatically
    /// populate with relevant content based on metadata filtering.
    /// </para>
    /// </remarks>
    /// <param name="contentSections">The content sections list to which Bases integration blocks will be added.</param>
    /// <param name="hierarchyInfo">Dictionary containing hierarchy context (course, class, module) for query targeting.</param>
    /// <param name="vaultPath">The vault root path used to locate the BaseBlockTemplate configuration.</param>
    internal void AddBasesIntegration(List<string> contentSections, Dictionary<string, string> hierarchyInfo, string vaultPath)
    {
        try
        {
            string configPath = FindBaseBlockTemplatePath(vaultPath);

            var course = hierarchyInfo.GetValueOrDefault("course") ?? string.Empty;
            var className = hierarchyInfo.GetValueOrDefault("class") ?? string.Empty;
            var module = hierarchyInfo.GetValueOrDefault("module") ?? string.Empty;

            // Generate Bases blocks
            var blocks = new Dictionary<string, string>
            {
                ["readings"] = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "reading"),
                ["instructions"] = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "instructions"),
                ["caseStudies"] = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "note/case-study"),
                ["videos"] = BaseBlockGenerator.GenerateBaseBlock(configPath, course, className, module, "video-reference")
            };

            // Add sections in order
            var sections = new[]
            {
                ("## 📚 Readings", blocks["readings"]),
                ("## 📝 Instructions", blocks["instructions"]),
                ("## 📊 Case Studies", blocks["caseStudies"]),
                ("## 📽️ Videos", blocks["videos"])
            };

            foreach (var (title, block) in sections)
            {
                contentSections.Add(title);
                contentSections.Add($"```base\n{block}\n```");
                contentSections.Add(string.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Bases integration blocks");
            contentSections.Add("## Content");
            contentSections.Add("*Error loading dynamic content blocks*");
            contentSections.Add(string.Empty);
        }
    }

    /// <summary>
    /// Adds a hierarchical subfolder listing section with consistent Obsidian-compatible markdown formatting and intelligent title conversion.
    /// </summary>
    /// <param name="contentSections">The content sections list to append the subfolder listing to. Must not be null.</param>
    /// <param name="subFolders">Collection of subfolder names to include in the listing. Empty collections are gracefully handled.</param>
    /// <param name="sectionTitle">The display title for the subfolder section (e.g., "Programs", "Courses", "Classes").</param>
    /// <param name="icon">Optional emoji icon to display next to each subfolder link. Defaults to "📁" (folder icon).</param>
    /// <remarks>
    /// <para>
    /// This method creates a structured markdown section that organizes subfolders into a navigable list using Obsidian's
    /// wikilink syntax. Each subfolder entry is converted from a filesystem name to a friendly display title using
    /// the FriendlyTitleHelper utility, ensuring consistent presentation across the vault.
    /// </para>
    /// <para>
    /// The method follows a consistent formatting pattern:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Section header with the provided title (## Section Title)</description></item>
    /// <item><description>Blank line for readability</description></item>
    /// <item><description>Bulleted list of folder links with icons</description></item>
    /// <item><description>Trailing blank line for section separation</description></item>
    /// </list>
    /// <para>
    /// Example output:
    /// <code>
    /// ## Programs
    ///
    /// - 📁 [[computer-science|Computer Science]]
    /// - 📁 [[data-analytics|Data Analytics]]
    ///
    /// </code>
    /// </para>
    /// <para>
    /// The method safely handles empty subfolder collections by performing no operation, ensuring clean content
    /// generation without unnecessary empty sections.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var contentSections = new List&lt;string&gt;();
    /// var subFolders = new List&lt;string&gt; { "intro-to-programming", "data-structures" };
    /// generator.AddSubfolderListing(contentSections, subFolders, "Courses", "📚");
    /// // Results in formatted course listing with book icons
    /// </code>
    /// </example>
    internal void AddSubfolderListing(List<string> contentSections, List<string> subFolders, string sectionTitle, string icon = "📁")
    {
        if (subFolders.Any())
        {
            contentSections.Add($"## {sectionTitle}");
            contentSections.Add(string.Empty);

            foreach (var subFolder in subFolders)
            {
                string friendlyName = FriendlyTitleHelper.GetFriendlyTitleFromFileName(subFolder);
                contentSections.Add($"- {icon} [[{subFolder}|{friendlyName}]]");
            }
            contentSections.Add(string.Empty);
        }
    }

    /// <summary>
    /// Organizes and adds content sections grouped by content type with contextual icons, titles, and alphabetical sorting.
    /// </summary>
    /// <param name="contentSections">The content sections list to append organized content to. Must not be null.</param>
    /// <param name="groupedFiles">Dictionary mapping content types to their associated vault files. Keys should match supported content types.</param>
    /// <param name="contentTypes">Array of content type keys to process in the specified order. Determines section ordering in the output.</param>
    /// <remarks>
    /// <para>
    /// This method processes content files by type, creating organized sections that enhance navigation and content discovery
    /// within the Obsidian vault. Each content type is rendered as a distinct section with appropriate visual indicators
    /// and consistent formatting.
    /// </para>
    /// <para>
    /// Content Processing Strategy:
    /// </para>
    /// <list type="bullet">
    /// <item><description><strong>Type-Based Organization</strong>: Groups related content together (readings, videos, assignments, etc.)</description></item>
    /// <item><description><strong>Visual Distinction</strong>: Uses type-specific emoji icons for quick identification</description></item>
    /// <item><description><strong>Alphabetical Sorting</strong>: Orders files within each type by title for predictable navigation</description></item>
    /// <item><description><strong>Obsidian Compatibility</strong>: Generates wikilink syntax for seamless vault integration</description></item>
    /// <item><description><strong>Empty Handling</strong>: Gracefully skips content types with no associated files</description></item>
    /// </list>
    /// <para>
    /// The method relies on GetContentTypeIcon() and GetContentTypeTitle() to provide consistent visual presentation
    /// and human-readable labels for each content type. This ensures uniform formatting across the entire vault.
    /// </para>
    /// <para>
    /// Example output structure:
    /// <code>
    /// ## 📖 Readings
    /// - [[Advanced Algorithms]]
    /// - [[Data Structures Overview]]
    ///
    /// ## 🎥 Videos
    /// - [[Binary Search Tutorial]]
    /// - [[Sorting Algorithms Explained]]
    ///
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var contentSections = new List&lt;string&gt;();
    /// var groupedFiles = new Dictionary&lt;string, List&lt;VaultFileInfo&gt;&gt;
    /// {
    ///     ["reading"] = new List&lt;VaultFileInfo&gt; { new VaultFileInfo { Title = "Chapter 1" } },
    ///     ["video"] = new List&lt;VaultFileInfo&gt; { new VaultFileInfo { Title = "Intro Video" } }
    /// };
    /// var contentTypes = new[] { "reading", "video", "assignment" };
    /// generator.AddContentByType(contentSections, groupedFiles, contentTypes);
    /// // Creates organized sections for each content type with files
    /// </code>
    /// </example>
    internal void AddContentByType(List<string> contentSections, Dictionary<string, List<VaultFileInfo>> groupedFiles, string[] contentTypes)
    {
        foreach (var contentType in contentTypes)
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
    }    /// <summary>
         /// Gets ordered list of subfolders, excluding hidden directories.
         /// </summary>
    protected virtual List<string> GetOrderedSubfolders(string folderPath)
    {
        return Directory.GetDirectories(folderPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name) && !name.StartsWith("."))
            .OrderBy(name => name)
            .ToList()!;
    }

    /// <summary>
    /// Determines the back link target based on hierarchy level.
    /// </summary>
    internal static string GetBackLinkTarget(string folderPath, int hierarchyLevel)
    {
        return hierarchyLevel switch
        {
            2 or 3 => Path.GetFileName(Path.GetDirectoryName(folderPath) ?? string.Empty), // Course or Class index
            _ => Path.GetFileName(Path.GetDirectoryName(folderPath) ?? string.Empty) // Module or other indices
        };
    }

    /// <summary>
    /// Locates the BaseBlockTemplate.yaml configuration file required for Obsidian Bases integration using a comprehensive search strategy.
    /// </summary>
    /// <param name="vaultPath">The root path of the Obsidian vault. Used as the primary search location anchor.</param>
    /// <returns>
    /// The absolute file path to the BaseBlockTemplate.yaml file if found; otherwise, an empty string.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method implements a robust file discovery algorithm that searches multiple potential locations for the
    /// BaseBlockTemplate.yaml configuration file, which is essential for Obsidian Bases functionality. The search
    /// strategy is designed to work across different deployment scenarios and development environments.
    /// </para>
    /// <para>
    /// Search Strategy (in order of priority):
    /// </para>
    /// <list type="number">
    /// <item><description><strong>Vault-Relative Path</strong>: ../config/BaseBlockTemplate.yaml relative to vault root</description></item>
    /// <item><description><strong>Working Directory</strong>: config/BaseBlockTemplate.yaml in current working directory</description></item>
    /// <item><description><strong>Application Directory</strong>: config/BaseBlockTemplate.yaml in application base directory</description></item>
    /// <item><description><strong>Directory Traversal</strong>: Walks up to 8 levels from application directory searching for config/BaseBlockTemplate.yaml</description></item>
    /// </list>
    /// <para>
    /// The method includes comprehensive debug logging to aid in troubleshooting configuration issues during
    /// development and deployment. Each search location is logged, and successful discovery is clearly documented.
    /// </para>
    /// <para>
    /// Obsidian Bases Integration:
    /// The BaseBlockTemplate.yaml file contains configuration templates for dynamic content blocks that are
    /// inserted into class-level index files. These blocks enable advanced querying and content organization
    /// features within the Obsidian vault ecosystem.
    /// </para>
    /// <para>
    /// Error Handling:
    /// The method gracefully handles missing files and invalid paths by returning an empty string, allowing
    /// calling code to implement appropriate fallback behavior for missing configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string vaultPath = @"C:\Users\Student\Documents\MyVault";
    /// string templatePath = generator.FindBaseBlockTemplatePath(vaultPath);
    /// if (!string.IsNullOrEmpty(templatePath))
    /// {
    ///     // Use template for Bases integration
    ///     var template = File.ReadAllText(templatePath);
    /// }
    /// </code>
    /// </example>
    internal string FindBaseBlockTemplatePath(string vaultPath)
    {
        // Possible locations for the template file
        List<string> possiblePaths = new()
        {
            Path.Combine(!string.IsNullOrEmpty(vaultPath) ? vaultPath : _defaultVaultRootPath, "..", "config", "BaseBlockTemplate.yaml"),
            Path.Combine(Directory.GetCurrentDirectory(), "config", "BaseBlockTemplate.yaml"),
            Path.Combine(AppContext.BaseDirectory, "config", "BaseBlockTemplate.yaml"),
        };

        // Check each possible path
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _logger.LogDebug("Found BaseBlockTemplate.yaml at: {Path}", path);
                return path;
            }
        }

        // If not found, try walking up directories from the executable location
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "config", "BaseBlockTemplate.yaml");
            if (File.Exists(candidate))
            {
                _logger.LogDebug($"Found BaseBlockTemplate.yaml by walking up from executable: {candidate}");
                return candidate;
            }
        }
        throw new FileNotFoundException("Base block template not found in any parent config folder.");
    }

    /// <summary>
    /// Identifies and returns the relative path to the main vault index from the current folder for consistent home navigation across all indices.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method implements intelligent main index discovery to support consistent navigation linking
    /// throughout the vault structure. It calculates the relative path from the current folder to the
    /// vault's main entry point, enabling proper Obsidian wikilink navigation regardless of hierarchy level.
    /// </para>
    /// <para>
    /// Discovery Strategy:
    /// </para>
    /// <list type="number">
    /// <item><description>Uses the configured vault root path as the search location</description></item>
    /// <item><description>Searches for prioritized filenames (index.md, Index.md, README.md, Home.md, Main.md)</description></item>
    /// <item><description>Falls back to scanning for files with template-type: main in frontmatter</description></item>
    /// <item><description>Calculates relative path from current folder to found index file</description></item>
    /// </list>
    /// <para>
    /// Relative Path Calculation:
    /// The method determines the relative path that allows Obsidian wikilinks to properly navigate
    /// from the current folder to the root index, supporting nested folder structures at any depth.
    /// </para>
    /// </remarks>
    /// <param name="vaultPath">
    /// The absolute path to the vault root directory for main index discovery.
    /// If empty, uses the configured default vault path.
    /// </param>
    /// <param name="currentFolderPath">
    /// The absolute path to the current folder where the index is being generated.
    /// Used to calculate the relative path to the root index.
    /// </param>
    /// <returns>
    /// The relative path from the current folder to the main vault index file (without extension).
    /// Returns "index" as fallback if no main index is found.
    /// </returns>
    /// <example>
    /// <code>
    /// // From a nested folder to root index
    /// string relativePath = processor.GetRootIndexFilename(@"C:\vault", @"C:\vault\program\course\class");
    /// // Returns: "../../../MBA.md" if MBA.md is the main index
    ///
    /// // From program level to root index
    /// string relativePath = processor.GetRootIndexFilename(@"C:\vault", @"C:\vault\program");
    /// // Returns: "../index.md" if index.md is the main index
    /// </code>
    /// </example>
    protected virtual string GetRootIndexFilename(string vaultPath, string currentFolderPath)
    {
        // Use the vault root path or default if not provided
        var searchPath = !string.IsNullOrEmpty(vaultPath) ? vaultPath : _defaultVaultRootPath;        // Check cache first to avoid expensive file system operations
        if (!_discoveredIndexFilenames.TryGetValue(searchPath, out var discoveredFilename))
        {
            // Perform the expensive discovery operation only once per vault
            discoveredFilename = DiscoverRootIndexFilename(searchPath);
            _discoveredIndexFilenames[searchPath] = discoveredFilename;
            _logger.LogDebug("Discovered and cached root index filename: '{Filename}' for path: {Path}", discoveredFilename, searchPath);
        }
        else
        {
            _logger.LogDebug("Using cached root index filename: '{Filename}' for path: {Path}", discoveredFilename, searchPath);
        }

        // Only calculate the relative path (this is what varies per call)
        var result = CalculateRelativePath(currentFolderPath, searchPath, discoveredFilename);
        _logger.LogDebug("GetRootIndexFilename result: '{Result}' (current: {Current}, vault: {Vault}, filename: {Filename})",
            result, currentFolderPath, searchPath, discoveredFilename);
        return result;
    }    /// <summary>
         /// Discovers the root index filename in the specified vault path through file system scanning.
         /// </summary>
         /// <param name="searchPath">The vault root path to search for index files.</param>
         /// <returns>The discovered index filename with extension, or the expected filename based on folder name.</returns>
         /// <remarks>
         /// This method performs the expensive file system operations to identify the main index file.
         /// It should only be called once per vault path, with results cached for subsequent calls.
         /// When no existing index file is found, it returns the expected filename based on the folder name
         /// to support initial index creation scenarios.
         /// </remarks>
    private string DiscoverRootIndexFilename(string searchPath)
    {
        // Determine expected folder-named file first (even if it doesn't exist yet)
        var folderName = Path.GetFileName(searchPath);
        var expectedFolderNamedFile = !string.IsNullOrEmpty(folderName) ? $"{folderName}.md" : "index.md";

        // First priority: Check for file named after the folder (e.g., MBA/MBA.md)
        if (!string.IsNullOrEmpty(folderName))
        {
            var fullPath = Path.Combine(searchPath, expectedFolderNamedFile);
            if (File.Exists(fullPath))
            {
                _logger.LogDebug($"Found folder-named root index file: {expectedFolderNamedFile} in {searchPath}");
                return expectedFolderNamedFile;
            }
        }

        // Second priority: Look for standard prioritized filenames
        var prioritizedFilenames = new[] { "index.md", "Index.md", "README.md", "Home.md", "Main.md" };
        foreach (var filename in prioritizedFilenames)
        {
            var fullPath = Path.Combine(searchPath, filename);
            if (File.Exists(fullPath))
            {
                _logger.LogDebug($"Found prioritized root index file: {filename} in {searchPath}");
                return filename;
            }
        }

        // Third priority: Look for any .md file with template-type: main
        if (Directory.Exists(searchPath))
        {
            var mdFiles = Directory.GetFiles(searchPath, "*.md");
            foreach (var mdFile in mdFiles)
            {
                try
                {
                    var content = File.ReadAllText(mdFile);
                    if (content.Contains("template-type: main", StringComparison.OrdinalIgnoreCase))
                    {
                        var filename = Path.GetFileName(mdFile);
                        _logger.LogDebug($"Found template-type: main root index file: {filename} in {searchPath}");
                        return filename;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error reading file {mdFile} while searching for main index");
                }
            }
        }

        // Final decision: Return expected folder-named file (even if it doesn't exist yet)
        // This supports initial index creation scenarios where we need to know what the filename SHOULD be
        _logger.LogDebug($"No existing root index file found in {searchPath}, returning expected filename: {expectedFolderNamedFile}");
        return expectedFolderNamedFile;
    }

    /// <summary>
    /// Calculates the relative path from the current folder to the root index file for markdown link navigation.
    /// </summary>
    /// <param name="currentFolderPath">The absolute path to the current folder.</param>
    /// <param name="vaultRootPath">The absolute path to the vault root.</param>
    /// <param name="indexFilename">The filename (with extension) of the index file.</param>
    /// <returns>The relative path that can be used in markdown links.</returns>
    private string CalculateRelativePath(string currentFolderPath, string vaultRootPath, string indexFilename)
    {
        _logger.LogDebug($"CalculateRelativePath called with: current='{currentFolderPath}', vault='{vaultRootPath}', filename='{indexFilename}'");

        try
        {
            // Normalize paths to handle different path separators
            var normalizedCurrentPath = Path.GetFullPath(currentFolderPath);
            var normalizedVaultPath = Path.GetFullPath(vaultRootPath);

            // If current folder is the vault root, just return the index filename
            if (string.Equals(normalizedCurrentPath, normalizedVaultPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Current folder is vault root, returning filename directly: '{Filename}'", indexFilename);
                return indexFilename;
            }

            // Calculate relative path using Uri class for reliable path calculation
            var currentUri = new Uri(normalizedCurrentPath + Path.DirectorySeparatorChar);
            var vaultUri = new Uri(normalizedVaultPath + Path.DirectorySeparatorChar);
            var relativeUri = currentUri.MakeRelativeUri(vaultUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            // Ensure forward slashes for markdown compatibility (don't convert to Windows separators)
            // Remove trailing slash if present
            if (relativePath.EndsWith("/"))
            {
                relativePath = relativePath.TrimEnd('/');
            }

            // Combine relative path with index filename using forward slash
            var result = string.IsNullOrEmpty(relativePath) ? indexFilename : $"{relativePath}/{indexFilename}";
            _logger.LogDebug("CalculateRelativePath result: '{Result}' (relativePath: '{RelativePath}', filename: '{Filename}')",
                result, relativePath, indexFilename);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating relative path from {CurrentPath} to {VaultPath}, using fallback",
                currentFolderPath, vaultRootPath);

            // Fallback: just return the index filename
            return indexFilename;
        }
    }

    /// <summary>
    /// Logs frontmatter debug information for troubleshooting.
    /// </summary>
    private void LogFrontmatterDebugInfo(string stage, Dictionary<string, object> frontmatter)
    {
        _logger.LogDebug($"{stage} UpdateMetadataWithHierarchy - frontmatter keys: {string.Join(", ", frontmatter.Keys)}");

        var fieldsToLog = new[] { "program", "course", "class" };
        foreach (var field in fieldsToLog)
        {
            if (frontmatter.ContainsKey(field))
            {
                _logger.LogDebug($"{stage} - frontmatter contains {field}: {frontmatter[field]}");
            }
        }
    }

    /// <summary>
    /// Provides standardized emoji icons for content types to ensure consistent visual representation across the vault.
    /// </summary>
    /// <param name="contentType">The content type identifier (e.g., "reading", "video", "assignment"). Case-sensitive.</param>
    /// <returns>
    /// A Unicode emoji string representing the content type. Returns "📄" (document icon) for unrecognized types.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method maintains a centralized mapping of content types to their visual representations, ensuring
    /// consistent iconography throughout the vault index system. The emoji icons provide immediate visual
    /// recognition and improve the user experience when navigating content.
    /// </para>
    /// <para>
    /// Supported Content Types and Icons:
    /// </para>
    /// <list type="table">
    /// <item>
    /// <term>reading</term>
    /// <description>📖 (Open Book) - Used for reading materials, articles, and textbook chapters</description>
    /// </item>
    /// <item>
    /// <term>video</term>
    /// <description>🎥 (Movie Camera) - Used for video lectures, tutorials, and recorded content</description>
    /// </item>
    /// <item>
    /// <term>transcript</term>
    /// <description>📝 (Memo) - Used for video transcripts and lecture notes</description>
    /// </item>
    /// <item>
    /// <term>assignment</term>
    /// <description>📋 (Clipboard) - Used for homework, projects, and assessment materials</description>
    /// </item>
    /// <item>
    /// <term>discussion</term>
    /// <description>💬 (Speech Balloon) - Used for discussion forums and collaborative content</description>
    /// </item>
    /// <item>
    /// <term>other</term>
    /// <description>📄 (Document) - Default icon for unrecognized or miscellaneous content</description>
    /// </item>
    /// </list>
    /// <para>
    /// Design Considerations:
    /// The method uses static implementation for performance efficiency, as icon mappings are immutable
    /// and frequently accessed during content generation. The switch expression provides optimal
    /// performance for the small, fixed set of content types.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string readingIcon = VaultIndexContentGenerator.GetContentTypeIcon("reading");
    /// // Returns: "📖"
    ///
    /// string unknownIcon = VaultIndexContentGenerator.GetContentTypeIcon("custom-type");
    /// // Returns: "📄"
    /// </code>
    /// </example>
    internal static string GetContentTypeIcon(string contentType)
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
    /// Provides standardized human-readable display titles for content types, ensuring consistent terminology across the vault interface.
    /// </summary>
    /// <param name="contentType">The content type identifier (e.g., "reading", "video", "assignment"). Case-sensitive.</param>
    /// <returns>
    /// A properly formatted, pluralized display title for the content type. Returns "Notes" for unrecognized types.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method maintains centralized control over content type labeling, ensuring professional and consistent
    /// presentation throughout the vault's user interface. The titles are designed to be intuitive and follow
    /// standard academic terminology conventions.
    /// </para>
    /// <para>
    /// Content Type Mappings:
    /// </para>
    /// <list type="table">
    /// <item>
    /// <term>reading</term>
    /// <description>"Readings" - Academic reading materials, articles, and textbook assignments</description>
    /// </item>
    /// <item>
    /// <term>video</term>
    /// <description>"Videos" - Video lectures, tutorials, and multimedia content</description>
    /// </item>
    /// <item>
    /// <term>transcript</term>
    /// <description>"Transcripts" - Written transcriptions of audio/video content</description>
    /// </item>
    /// <item>
    /// <term>assignment</term>
    /// <description>"Assignments" - Homework, projects, and assessments</description>
    /// </item>
    /// <item>
    /// <term>discussion</term>
    /// <description>"Discussions" - Forum posts, group discussions, and collaborative activities</description>
    /// </item>
    /// <item>
    /// <term>other</term>
    /// <description>"Notes" - General notes and miscellaneous content</description>
    /// </item>
    /// </list>
    /// <para>
    /// Design Philosophy:
    /// The method uses pluralized forms to accurately represent sections that typically contain multiple items.
    /// This aligns with standard academic and organizational conventions where section headers describe
    /// collections of related content.
    /// </para>
    /// <para>
    /// Performance Optimization:
    /// Implemented as a static method using switch expressions for optimal performance during frequent
    /// content generation operations. The method requires no external dependencies and provides
    /// deterministic, fast lookups.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string readingTitle = VaultIndexContentGenerator.GetContentTypeTitle("reading");
    /// // Returns: "Readings"
    ///
    /// string sectionHeader = $"## {GetContentTypeIcon("video")} {GetContentTypeTitle("video")}";
    /// // Results in: "## 🎥 Videos"
    ///
    /// string unknownTitle = VaultIndexContentGenerator.GetContentTypeTitle("custom-type");
    /// // Returns: "Notes"
    /// </code>
    /// </example>
    internal static string GetContentTypeTitle(string contentType)
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
}
