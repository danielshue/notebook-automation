// <copyright file="CourseStructureExtractor.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Utils/CourseStructureExtractor.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Utility class for extracting course structure information (modules, lessons) from file paths.
/// </summary>
/// <remarks>
/// <para>
/// This class provides functionality to identify module and lesson information from file paths
/// based on directory naming conventions. It uses multiple extraction strategies to handle
/// various course organization patterns commonly found in educational content.
/// </para>
/// <para>
/// The extractor supports detection of module and lesson information by analyzing:
/// <list type="bullet">
/// <item><description>Directory names containing "module" or "lesson" keywords</description></item>
/// <item><description>Numbered directory prefixes (e.g., "01_", "02-")</description></item>
/// <item><description>Filename patterns with embedded module/lesson information</description></item>
/// <item><description>Hierarchical structures with parent-child relationships</description></item>
/// </list>
/// </para>
/// <para>
/// The class provides clean formatting by removing numbering prefixes and converting names
/// to title case for consistent metadata output.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var logger = serviceProvider.GetService&lt;ILogger&lt;CourseStructureExtractor&gt;&gt;();
/// var extractor = new CourseStructureExtractor(logger);
/// var metadata = new Dictionary&lt;string, object&gt;();
///
/// extractor.ExtractModuleAndLesson("/courses/01_module-intro/02_lesson-basics/content.md", metadata);
/// // metadata now contains: { "module": "Module Intro", "lesson": "Lesson Basics" }
/// </code>
/// </example>
/// <param name="logger">Logger for diagnostic and warning messages during extraction operations.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
public partial class CourseStructureExtractor(ILogger<CourseStructureExtractor> logger)
{
    private readonly ILogger<CourseStructureExtractor> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private static readonly Regex NumberPrefixRegex = MyRegex();

    /// <summary>
    /// Extracts module and lesson information from a file path and adds it to the provided metadata dictionary.
    /// </summary>
    /// <param name="filePath">The full path to the file from which to extract course structure information.</param>
    /// <param name="metadata">The metadata dictionary to update with module/lesson information.
    /// Keys "module" and "lesson" will be added if corresponding information is found.</param>
    /// <remarks>
    /// <para>
    /// This method uses a multi-stage extraction process:
    /// <list type="number">
    /// <item><description>First attempts to extract from the filename itself</description></item>
    /// <item><description>Then looks for explicit "module" and "lesson" keywords in directory names</description></item>
    /// <item><description>Finally attempts to identify patterns from numbered directory structures</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The method logs debug information during extraction to help with troubleshooting course structure issues.
    /// Warning messages are logged if the file path is empty or if extraction fails due to exceptions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var metadata = new Dictionary&lt;string, object&gt;();
    /// extractor.ExtractModuleAndLesson(@"C:\courses\01_intro-module\03_lesson-basics\notes.md", metadata);
    ///
    /// // Result: metadata contains:
    /// // { "module": "Intro Module", "lesson": "Lesson Basics" }
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">Logged as warning if extraction fails due to invalid path structure.</exception>
    public void ExtractModuleAndLesson(string filePath, Dictionary<string, object> metadata)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            logger.LogWarning("Cannot extract module/lesson from empty file path");
            return;
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            var dir = fileInfo.Directory;
            string? module = null;
            string? lesson = null;

            // First attempt: Extract from filename itself
            (module, lesson) = ExtractFromFilename(fileInfo.Name);
            logger.LogDebugWithPath(
                "Filename extraction result - Module: {Module}, Lesson: {Lesson}",
                nameof(CourseStructureExtractor), module ?? "null", lesson ?? "null", filePath);

            if (dir != null)
            {
                // Second attempt: Look for explicit "module" and "lesson" keywords in directories
                if (module == null || lesson == null)
                {
                    var (dirModule, dirLesson) = ExtractByKeywords(dir);
                    module ??= dirModule;
                    lesson ??= dirLesson;
                    logger.LogDebugWithPath(
                        "Keyword extraction result - Module: {Module}, Lesson: {Lesson}",
                        nameof(CourseStructureExtractor), module ?? "null", lesson ?? "null", filePath);
                }

                // Third attempt: If we still don't have both, try numbered directory structure
                if (module == null || lesson == null)
                {
                    var (numModule, numLesson) = ExtractByNumberedPattern(dir);
                    module ??= numModule;
                    lesson ??= numLesson;
                    logger.LogDebugWithPath(
                        "Numbered pattern extraction result - Module: {Module}, Lesson: {Lesson}",
                        nameof(CourseStructureExtractor), module ?? "null", lesson ?? "null", filePath);
                }
            }

            // Update metadata with extracted information
            if (!string.IsNullOrEmpty(module))
            {
                metadata["module"] = module;
                logger.LogDebugWithPath("Set module metadata: {Module}", nameof(CourseStructureExtractor), module, filePath);
            }

            if (!string.IsNullOrEmpty(lesson))
            {
                metadata["lesson"] = lesson;
                logger.LogDebugWithPath("Set lesson metadata: {Lesson}", nameof(CourseStructureExtractor), lesson, filePath);
            }

            // Log summary if we found any information
            if (!string.IsNullOrEmpty(module) || !string.IsNullOrEmpty(lesson))
            {
                logger.LogDebugWithPath(
                    "Successfully extracted - Module: '{Module}', Lesson: '{Lesson}'",
                    nameof(CourseStructureExtractor), module ?? "not found", lesson ?? "not found", filePath);
            }
            else
            {
                logger.LogDebugWithPath("No module or lesson information could be extracted", nameof(CourseStructureExtractor), filePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarningWithPath(ex, "Failed to extract module/lesson from directory structure for file: {filePath}", filePath);
        }
    }

    /// <summary>
    /// Extracts module and lesson information by looking for explicit keywords in directory names.
    /// </summary>
    /// <param name="dir">The directory to analyze for module and lesson keywords.</param>
    /// <returns>A tuple containing the extracted module and lesson names, where either may be null if not found.</returns>
    /// <remarks>
    /// <para>
    /// This method searches for directories containing "module" or "lesson" keywords (case-insensitive).
    /// When a lesson directory is found, it also searches one level up for a parent module directory.
    /// </para>
    /// <para>
    /// The extraction follows this hierarchy:
    /// <list type="bullet">
    /// <item><description>If current directory contains "lesson", treat as lesson and check parent for "module"</description></item>
    /// <item><description>If current directory contains "module", treat as module only</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static (string? module, string? lesson) ExtractByKeywords(DirectoryInfo dir)
    {
        string? module = null;
        string? lesson = null;

        // Look for lesson folder (e.g., lesson-1-...)
        var lessonDir = dir;
        if (lessonDir != null && lessonDir.Name.Contains("lesson", StringComparison.CurrentCultureIgnoreCase))
        {
            lesson = CleanModuleOrLessonName(lessonDir.Name);

            // Look for module folder one level up
            var moduleDir = lessonDir.Parent;
            if (moduleDir != null && moduleDir.Name.Contains("module", StringComparison.CurrentCultureIgnoreCase))
            {
                module = CleanModuleOrLessonName(moduleDir.Name);
            }
        }
        else if (dir.Name.Contains("module", StringComparison.CurrentCultureIgnoreCase))
        {
            // If current dir is module, set module only
            module = CleanModuleOrLessonName(dir.Name);
        }

        return (module, lesson);
    }

    /// <summary>
    /// Extracts module and lesson information by analyzing numbered directory patterns.
    /// </summary>
    /// <param name="dir">Starting directory to analyze for numbered patterns and hierarchical structure.</param>
    /// <returns>A tuple containing the extracted module and lesson names, where either may be null if not found.</returns>
    /// <remarks>
    /// <para>
    /// This method performs pattern recognition on directory structures that use numbering or
    /// specific keywords to indicate course hierarchy. It analyzes multiple directory levels
    /// to understand the relationship between modules and lessons.
    /// </para>
    /// <para>
    /// The method recognizes patterns such as:
    /// <list type="bullet">
    /// <item><description>Numbered prefixes: "01_", "02-", etc.</description></item>
    /// <item><description>Course keywords: "module", "course", "week", "unit"</description></item>
    /// <item><description>Lesson keywords: "lesson", "session", "lecture", "class"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The extraction process uses hierarchical analysis to determine parent-child relationships
    /// between modules and lessons based on directory nesting and naming patterns.
    /// </para>
    /// </remarks>
    private static (string? module, string? lesson) ExtractByNumberedPattern(DirectoryInfo dir)
    {
        string? module = null;
        string? lesson = null;

        var currentDir = dir;
        var parentDir = currentDir?.Parent;
        var grandParentDir = parentDir?.Parent;

        // Enhanced pattern detection: Check multiple directory levels for common patterns
        var directories = new List<DirectoryInfo?> { currentDir, parentDir, grandParentDir };

        // First pass: Look for explicit module/lesson indicators in any directory level
        foreach (var directory in directories.Where(d => d != null))
        {
            var dirName = directory!.Name.ToLowerInvariant();

            // Check for module indicators
            if (module == null && (HasNumberPrefix(directory.Name) ||
                dirName.Contains("module") ||
                dirName.Contains("course") ||
                dirName.Contains("week") ||
                dirName.Contains("unit")))
            {
                // Additional patterns: "Week 1", "Unit 2", etc.
                if (dirName.Contains("week") || dirName.Contains("unit") || dirName.Contains("course"))
                {
                    module = CleanModuleOrLessonName(directory.Name);
                }
                else if (HasNumberPrefix(directory.Name))
                {
                    // For numbered directories, decide based on hierarchy
                    if (directory == currentDir && parentDir != null && HasNumberPrefix(parentDir.Name))
                    {
                        // Current is lesson-like, parent is module-like
                        lesson ??= CleanModuleOrLessonName(directory.Name);
                    }
                    else
                    {
                        module = CleanModuleOrLessonName(directory.Name);
                    }
                }
            }

            // Check for lesson indicators
            if (lesson == null && (HasNumberPrefix(directory.Name) ||
                dirName.Contains("lesson") ||
                dirName.Contains("session") ||
                dirName.Contains("lecture") ||
                dirName.Contains("class")))
            {
                if (dirName.Contains("lesson") || dirName.Contains("session") ||
                    dirName.Contains("lecture") || dirName.Contains("class"))
                {
                    lesson = CleanModuleOrLessonName(directory.Name);
                }
            }
        } // Second pass: Hierarchical fallback - use directory structure to infer relationships

        if (currentDir != null && parentDir != null)
        {
            // Only treat current directory as lesson if parent looks like a module container
            if (lesson == null && HasNumberPrefix(currentDir.Name) &&
                (parentDir.Name.ToLowerInvariant().Contains("module") ||
                 parentDir.Name.ToLowerInvariant().Contains("course") ||
                 parentDir.Name.ToLowerInvariant().Contains("week") ||
                 parentDir.Name.ToLowerInvariant().Contains("unit") ||
                 HasNumberPrefix(parentDir.Name)))
            {
                lesson = CleanModuleOrLessonName(currentDir.Name);
            }

            if (module == null && HasNumberPrefix(parentDir.Name))
            {
                // Use parent as module if we found a lesson, or if parent contains module indicators
                if (lesson != null ||
                    parentDir.Name.ToLowerInvariant().Contains("module") ||
                    parentDir.Name.ToLowerInvariant().Contains("course") ||
                    parentDir.Name.ToLowerInvariant().Contains("week") ||
                    parentDir.Name.ToLowerInvariant().Contains("unit"))
                {
                    module = CleanModuleOrLessonName(parentDir.Name);
                }
            }
        }

        // Final fallback: If we still have nothing but have a numbered current directory,
        // treat it as a module (common case for single-level course structures)
        if (module == null && lesson == null && currentDir != null && HasNumberPrefix(currentDir.Name))
        {
            module = CleanModuleOrLessonName(currentDir.Name);
        }

        return (module, lesson);
    }

    /// <summary>
    /// Determines if a directory name has a numeric prefix or structured numbering pattern commonly used in course organization.
    /// </summary>
    /// <param name="dirName">Name of the directory to check for numbering patterns.</param>
    /// <returns><c>true</c> if the directory name contains structured numbering patterns; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// This method recognizes various numbering patterns used in educational content organization:
    /// <list type="bullet">
    /// <item><description>Numeric prefixes: "01_", "02-", "03.", etc.</description></item>
    /// <item><description>Week/Unit patterns: "Week 1", "Week-1", "Unit 2", etc.</description></item>
    /// <item><description>Module/Lesson patterns: "Module 1", "Lesson 2", etc.</description></item>
    /// <item><description>Session/Class patterns: "Session 1", "Class 3", etc.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The method uses compiled regular expressions for efficient pattern matching and is case-insensitive
    /// for keyword-based patterns.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// bool result1 = HasNumberPrefix("01_introduction");     // Returns true
    /// bool result2 = HasNumberPrefix("Week 1");              // Returns true
    /// bool result3 = HasNumberPrefix("Module 2");            // Returns true
    /// bool result4 = HasNumberPrefix("random-folder");       // Returns false
    /// </code>
    /// </example>
    private static bool HasNumberPrefix(string dirName)
    {
        if (string.IsNullOrEmpty(dirName))
        {
            return false;
        }

        // Original numbered prefix pattern (01_, 02-, etc.)
        if (NumberPrefixRegex.IsMatch(dirName))
        {
            return true;
        }

        // Additional patterns for course structures
        string lowerName = dirName.ToLowerInvariant();

        // Week/Unit patterns: "Week 1", "Week-1", "Unit 2", etc.
        if (WeekUnitRegex().IsMatch(lowerName))
        {
            return true;
        }

        // Module/Lesson patterns: "Module 1", "Lesson 2", etc.
        if (ModuleLessonNumberRegex().IsMatch(lowerName))
        {
            return true;
        }

        // Session/Class patterns: "Session 1", "Class 3", etc.
        if (SessionClassNumberRegex().IsMatch(lowerName))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cleans up module or lesson folder names by removing numbering prefixes and formatting for consistent display.
    /// </summary>
    /// <param name="folderName">The raw folder name to clean and format.</param>
    /// <returns>A cleaned and formatted folder name in title case with proper spacing.</returns>
    /// <remarks>
    /// <para>
    /// This method performs several transformations to create user-friendly module and lesson names:
    /// <list type="number">
    /// <item><description>Removes numeric prefixes (e.g., "01_", "02-", "03.")</description></item>
    /// <item><description>Converts camelCase to spaced words</description></item>
    /// <item><description>Replaces hyphens and underscores with spaces</description></item>
    /// <item><description>Normalizes multiple spaces to single spaces</description></item>
    /// <item><description>Converts to title case using current culture</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The method is culture-aware and uses the current culture's title case rules for proper formatting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string result1 = CleanModuleOrLessonName("01_module-introduction");     // Returns "Module Introduction"
    /// string result2 = CleanModuleOrLessonName("02-lesson_basics");          // Returns "Lesson Basics"
    /// string result3 = CleanModuleOrLessonName("sessionPlanningDetails");    // Returns "Session Planning Details"
    /// string result4 = CleanModuleOrLessonName("Week-1-Overview");           // Returns "Week Overview"
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="folderName"/> is null.</exception>
    public static string CleanModuleOrLessonName(string folderName)
    {
        // Remove numbering prefix (e.g., 01_, 02-, etc.), replace hyphens/underscores, title case
        string clean = MyRegex1().Replace(folderName, string.Empty);

        // Convert camelCase to spaced words before other processing
        clean = CamelCaseRegex().Replace(clean, " ");

        clean = clean.Replace("-", " ").Replace("_", " ");
        clean = MyRegex2().Replace(clean, " ").Trim();
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean);
    }

    /// <summary>
    /// Extracts module and lesson information from a filename using various naming patterns.
    /// </summary>
    /// <param name="filename">The filename to analyze (without directory path).</param>
    /// <returns>A tuple containing the extracted module and lesson names, where either may be null if not found.</returns>
    /// <remarks>
    /// <para>
    /// This method uses multiple regex patterns to identify module and lesson information embedded in filenames:
    /// <list type="bullet">
    /// <item><description>"Module-X-Name" or "module_X_name" format</description></item>
    /// <item><description>"Lesson-X-Name" or "lesson_X_name" format</description></item>
    /// <item><description>"Week-X-Name", "Unit-X-Name", "Session-X-Name", or "Class-X-Name" format</description></item>
    /// <item><description>Compact patterns like "ModuleX" or "LessonX"</description></item>
    /// <item><description>Numbered prefix patterns with content words</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The method removes file extensions before analysis and applies cleaning rules to create user-friendly names.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = ExtractFromFilename("Module-1-Introduction.md");
    /// // Returns: ("Module 1 Introduction", null)
    ///
    /// var result = ExtractFromFilename("Lesson-2-Basics.txt");
    /// // Returns: (null, "Lesson 2 Basics")
    /// </code>
    /// </example>
    private static (string? module, string? lesson) ExtractFromFilename(string filename)
    {
        string? module = null;
        string? lesson = null;

        // Remove file extension for analysis
        string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);

        // Pattern 1: "Module-X-Name" or "module_X_name" format
        var moduleMatch = ModuleFilenameRegex().Match(nameWithoutExt);
        if (moduleMatch.Success && moduleMatch.Groups.Count >= 3)
        {
            var number = moduleMatch.Groups[1].Value;
            var title = moduleMatch.Groups[2].Value;
            module = CleanModuleOrLessonName($"Module {number} {title}");
        }

        // Pattern 2: "Lesson-X-Name" or "lesson_X_name" format
        var lessonMatch = LessonFilenameRegex().Match(nameWithoutExt);
        if (lessonMatch.Success && lessonMatch.Groups.Count >= 3)
        {
            var number = lessonMatch.Groups[1].Value;
            var title = lessonMatch.Groups[2].Value;
            lesson = CleanModuleOrLessonName($"Lesson {number} {title}");
        } // Pattern 3: "Week-X-Name", "Unit-X-Name", "Session-X-Name", or "Class-X-Name" format

        if (module == null)
        {
            var weekUnitMatch = WeekUnitFilenameRegex().Match(nameWithoutExt);
            if (weekUnitMatch.Success && weekUnitMatch.Groups.Count >= 4)
            {
                var type = weekUnitMatch.Groups[1].Value;
                var number = weekUnitMatch.Groups[2].Value;
                var title = weekUnitMatch.Groups[3].Value;
                module = CleanModuleOrLessonName($"{type}{number} {title}");
            }
        }

        // Pattern 4: Look for "ModuleX" or "LessonX" patterns (without separator)
        if (module == null)
        {
            var compactModuleMatch = CompactModuleRegex().Match(nameWithoutExt);
            if (compactModuleMatch.Success && compactModuleMatch.Groups.Count >= 3)
            {
                var number = compactModuleMatch.Groups[1].Value;
                var title = compactModuleMatch.Groups[2].Value;
                module = CleanModuleOrLessonName($"Module {number} {title}");
            }
        }

        if (lesson == null)
        {
            var compactLessonMatch = CompactLessonRegex().Match(nameWithoutExt);
            if (compactLessonMatch.Success && compactLessonMatch.Groups.Count >= 3)
            {
                var number = compactLessonMatch.Groups[1].Value;
                var title = compactLessonMatch.Groups[2].Value;
                lesson = CleanModuleOrLessonName($"Lesson {number} {title}");
            }
        }

        // Pattern 5: Look for numbered prefix with content words for general patterns like "02_session-planning-details"
        if (module == null && lesson == null)
        {
            var numberedMatch = NumberedContentRegex().Match(nameWithoutExt);
            if (numberedMatch.Success && numberedMatch.Groups.Count >= 3)
            {
                var number = numberedMatch.Groups[1].Value;
                var content = numberedMatch.Groups[2].Value;

                // Determine if this looks more like a module or lesson based on content
                if (content.Contains("course", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("introduction", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("overview", StringComparison.OrdinalIgnoreCase))
                {
                    module = CleanModuleOrLessonName($"{number} {content}");
                }
                else
                {
                    // For things like "02_session-planning-details", treat as module
                    module = CleanModuleOrLessonName(content);
                }
            }
        }

        return (module, lesson);
    }

    [GeneratedRegex(@"^(\d+)[_-]", RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"^\d+[_-]?")]
    private static partial Regex MyRegex1();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex2();        // Filename-based extraction patterns

    [GeneratedRegex(@"(?i)module\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex ModuleFilenameRegex();

    [GeneratedRegex(@"(?i)lesson\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex LessonFilenameRegex();

    [GeneratedRegex(@"(?i)(week|unit|session|class)\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex WeekUnitFilenameRegex();

    [GeneratedRegex(@"(?i)module(\d+)([a-zA-Z]+.*)", RegexOptions.IgnoreCase)]
    private static partial Regex CompactModuleRegex();

    [GeneratedRegex(@"(?i)lesson(\d+)([a-zA-Z]+.*)", RegexOptions.IgnoreCase)]
    private static partial Regex CompactLessonRegex();

    [GeneratedRegex(@"(?i)module(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex ModuleNumberSeparatorRegex();

    [GeneratedRegex(@"(?i)lesson(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex LessonNumberSeparatorRegex();

    [GeneratedRegex(@"^(\d+)[_-](.+)", RegexOptions.IgnoreCase)]
    private static partial Regex NumberedContentRegex();

    // Enhanced directory pattern recognition
    [GeneratedRegex(@"(week|unit)[_\s-]*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex WeekUnitRegex();

    [GeneratedRegex(@"(module|lesson)[_\s-]*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex ModuleLessonNumberRegex();

    [GeneratedRegex(@"(session|class)[_\s-]*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex SessionClassNumberRegex();

    // Pattern for camelCase to space conversion
    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
    private static partial Regex CamelCaseRegex();
}
