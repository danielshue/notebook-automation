// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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
/// <param name="appConfig">Optional application configuration containing vault root path for hierarchical analysis.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
public partial class CourseStructureExtractor(ILogger<CourseStructureExtractor> logger, AppConfig? appConfig = null) : ICourseStructureExtractor
{
    private readonly ILogger<CourseStructureExtractor> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AppConfig? _appConfig = appConfig;
    private static readonly Regex NumberPrefixRegex = NumberPrefixRegexPattern();

    /// <summary>
    /// Gets the vault root path for hierarchical analysis, or null if not configured.
    /// </summary>
    public string? VaultRoot => _appConfig?.Paths?.NotebookVaultFullpathRoot;

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
    /// <inheritdoc/>
    public void ExtractModuleAndLesson(string filePath, IDictionary<string, object?> metadata)
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
            string? module = null; string? lesson = null;

            // Check if this is a content file that should use number-only module extraction
            // Content files (videos, readings, instructions, etc.) get module: "01", "02", etc.
            // Non-content files get friendly titles like module: "Module 1 Introduction"
            bool isContentFile = IsContentFile(filePath);
            logger.LogDebug($"Content file detection for {filePath}: {isContentFile}");

            // For content files, extract module number by walking up parent directories
            // For non-content files, find the module directory and use friendly names
            if (isContentFile)
            {
                // Content file processing - extract module number by walking up parent directories
                // Example: file "01_01_module-4-overview_instructions" in folder "01_module-4-overview"
                // should extract "4" from "module-4" pattern in the parent directory
                module = ExtractModuleNumberFromParentDirectories(dir);

                // Check if this is a case study under a module - these should have leading zeros removed
                bool isCaseStudyUnderModule = (filePath.ToLowerInvariant().Contains("case-study") ||
                                              filePath.ToLowerInvariant().Contains("case studies")) &&
                                              IsCaseStudyUnderModule(filePath);

                // For case studies under modules, remove leading zeros from module numbers
                if (isCaseStudyUnderModule && !string.IsNullOrEmpty(module) && int.TryParse(module, out int moduleInt))
                {
                    module = moduleInt.ToString(); // This removes leading zeros: "03" -> "3"
                }

                // If we still didn't find a module, try extracting from filename
                if (module == null)
                {
                    var fileModuleNumber = ExtractModuleNumber(fileInfo.Name);
                    if (!string.IsNullOrEmpty(fileModuleNumber))
                    {
                        // For case studies under modules, remove leading zeros
                        if (isCaseStudyUnderModule && int.TryParse(fileModuleNumber, out int fileModuleInt))
                        {
                            module = fileModuleInt.ToString(); // This removes leading zeros: "03" -> "3"
                        }
                        else
                        {
                            module = fileModuleNumber;
                        }
                        logger.LogDebug($"Content file module number extraction from filename: {module}");
                    }
                }
            }
            else
            {
                // Check if this is a case study at class level - these should not extract any module information
                bool isCaseStudyAtClassLevel = (filePath.ToLowerInvariant().Contains("case-study") ||
                                               filePath.ToLowerInvariant().Contains("case studies")) &&
                                               !IsCaseStudyUnderModule(filePath);

                if (!isCaseStudyAtClassLevel)
                {
                    // Non-content file processing - use directory analysis for friendly module titles
                    var directoryPartsForNonContent = GetDirectoryParts(dir);
                    var moduleDirForNonContent = FindModuleDirectory(directoryPartsForNonContent);
                    if (moduleDirForNonContent != null)
                    {
                        module = CleanModuleOrLessonName(moduleDirForNonContent);
                        logger.LogDebug($"Non-content file: using friendly module name '{module}' from directory '{moduleDirForNonContent}'");
                    }
                }
                else
                {
                    logger.LogDebug($"Case study at class level - skipping module extraction for: {filePath}");
                }
            }

            // Find lesson information for all file types
            var directoryParts = GetDirectoryParts(dir);
            var moduleDir = isContentFile ? null : FindModuleDirectory(directoryParts);
            var lessonDir = FindLessonDirectory(directoryParts, moduleDir);
            if (lessonDir != null)
            {
                lesson = CleanModuleOrLessonName(lessonDir);
                logger.LogDebug($"Extracted lesson '{lesson}' from directory '{lessonDir}'");
            }

            // Check if this is a case study at class level to prevent any module extraction in fallback logic
            bool skipModuleExtractionForClassLevelCaseStudy = (filePath.ToLowerInvariant().Contains("case-study") ||
                                           filePath.ToLowerInvariant().Contains("case studies")) &&
                                           !IsCaseStudyUnderModule(filePath);

            // Fallback extraction if primary methods failed (but skip for class-level case studies)
            if (module == null && !skipModuleExtractionForClassLevelCaseStudy)
            {
                // Try filename-based extraction
                (string? fileModule, string? fileLesson) = ExtractFromFilename(fileInfo.Name);

                if (fileModule != null)
                {
                    if (isContentFile)
                    {
                        // For content files, try to extract only the number
                        var moduleNumber = ExtractModuleNumber(fileModule);
                        if (!string.IsNullOrEmpty(moduleNumber))
                        {
                            module = moduleNumber;
                        }
                        else
                        {
                            module = fileModule;
                        }
                    }
                    else
                    {
                        module = fileModule;
                    }
                    logger.LogDebug($"Filename extraction result - Module: {module} for file: {filePath}");
                }

                if (lesson == null && fileLesson != null)
                {
                    lesson = fileLesson;
                    logger.LogDebug($"Filename extraction result - Lesson: {lesson} for file: {filePath}");
                }
            }            // Last resort: look for additional context in directory structure (but skip for class-level case studies)
            if (dir != null && (module == null || lesson == null) && !skipModuleExtractionForClassLevelCaseStudy)
            {
                // Only try keyword extraction for non-content files or if we still don't have a module
                if (!isContentFile || module == null)
                {
                    var (dirModule, dirLesson) = ExtractByKeywords(dir);

                    if (module == null && dirModule != null)
                    {
                        if (isContentFile)
                        {
                            // For content files, try to extract only the number
                            var moduleNumber = ExtractModuleNumber(dirModule);
                            if (!string.IsNullOrEmpty(moduleNumber))
                            {
                                module = moduleNumber;
                            }
                            else
                            {
                                module = dirModule;
                            }
                        }
                        else
                        {
                            module = dirModule;
                        }
                    }

                    if (lesson == null)
                    {
                        lesson = dirLesson;
                    }
                }

                // Still nothing? Try numbered directory structure as last resort
                if (module == null || lesson == null)
                {
                    var (numModule, numLesson) = ExtractByNumberedPattern(dir);

                    if (module == null && numModule != null)
                    {
                        if (isContentFile)
                        {
                            // For content files, try to extract only the number
                            var moduleNumber = ExtractModuleNumber(numModule);
                            if (!string.IsNullOrEmpty(moduleNumber))
                            {
                                module = moduleNumber;
                            }
                            else
                            {
                                module = numModule;
                            }
                        }
                        else
                        {
                            module = numModule;
                        }
                    }

                    if (lesson == null)
                    {
                        lesson = numLesson;
                    }
                }
            }

            // Update metadata with extracted information
            if (!string.IsNullOrEmpty(module))
            {
                metadata["module"] = module;
                logger.LogDebug($"Set module metadata: {module} for file: {filePath}");
            }

            if (!string.IsNullOrEmpty(lesson))
            {
                metadata["lesson"] = lesson;
                logger.LogDebug($"Set lesson metadata: {lesson} for file: {filePath}");
            }

            // Log summary if we found any information
            if (!string.IsNullOrEmpty(module) || !string.IsNullOrEmpty(lesson))
            {
                logger.LogDebug(
                    $"Successfully extracted - Module: '{module ?? "not found"}', Lesson: '{lesson ?? "not found"}' for file: {filePath}");
            }
            else
            {
                logger.LogDebug($"No module or lesson information could be extracted for file: {filePath}");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Failed to extract module/lesson from directory structure for file: {filePath}");
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
    /// string result4 = CleanModuleOrLessonName("Week-1-Overview");           // Returns "Week Overview"    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="folderName"/> is null.</exception>
    public static string CleanModuleOrLessonName(string folderName)
    {
        // Remove numbering prefix (e.g., 01_, 02-, etc.), replace hyphens/underscores, title case
        string clean = LeadingNumberOptionalSeparatorRegexPattern().Replace(folderName, string.Empty);

        // Convert camelCase to spaced words before other processing, but avoid breaking number-letter combinations
        // like "Week1" -> "Week 1" (which should stay as "Week1")
        clean = SmartCamelCaseRegex().Replace(clean, " ");

        clean = clean.Replace("-", " ").Replace("_", " ");
        clean = WhitespaceRegexPattern().Replace(clean, " ").Trim();
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
    ///    /// <example>
    /// <code>
    /// var result = ExtractFromFilename("Module-1-Introduction.txt");
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
        bool isContentFile = IsContentFileFromName(filename);

        // Pattern 1: "Module-X-Name" or "module_X_name" format
        var moduleMatch = ModuleFilenameRegex().Match(nameWithoutExt);
        if (moduleMatch.Success && moduleMatch.Groups.Count >= 3)
        {
            var number = moduleMatch.Groups[1].Value;
            var title = moduleMatch.Groups[2].Value;

            // For content files, use just the number; for non-content, use friendly title
            module = isContentFile ? number : CleanModuleOrLessonName($"Module {number} {title}");
        }

        // Pattern 2: "Lesson-X-Name" or "lesson_X_name" format
        var lessonMatch = LessonFilenameRegex().Match(nameWithoutExt);
        if (lessonMatch.Success && lessonMatch.Groups.Count >= 3)
        {
            var number = lessonMatch.Groups[1].Value;
            var title = lessonMatch.Groups[2].Value;
            lesson = CleanModuleOrLessonName($"Lesson {number} {title}");
        }        // Pattern 3: "Week-X-Name", "Unit-X-Name", "Session-X-Name", or "Class-X-Name" format
        if (module == null)
        {
            var weekUnitMatch = WeekUnitFilenameRegex().Match(nameWithoutExt);
            if (weekUnitMatch.Success && weekUnitMatch.Groups.Count >= 4)
            {
                var type = weekUnitMatch.Groups[1].Value;
                var number = weekUnitMatch.Groups[2].Value;
                var title = weekUnitMatch.Groups[3].Value;

                // For content files, use just the number; for non-content, use friendly title
                // For non-content files, preserve the original format (e.g., "Week1 Introduction" not "Week 1 Introduction")
                module = isContentFile ? number : CleanModuleOrLessonName($"{type}{number} {title}");
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

                // For content files, use just the number; for non-content, use friendly title
                module = isContentFile ? number : CleanModuleOrLessonName($"Module {number} {title}");
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

                // For content files, just use the number
                if (isContentFile)
                {
                    module = number;
                }
                // For non-content files, use friendly titles
                else if (content.Contains("course", StringComparison.OrdinalIgnoreCase) ||
                         content.Contains("introduction", StringComparison.OrdinalIgnoreCase) ||
                         content.Contains("overview", StringComparison.OrdinalIgnoreCase))
                {
                    module = CleanModuleOrLessonName($"{content}");
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
    internal static partial Regex NumberPrefixRegexPattern();

    [GeneratedRegex(@"^\d+[_-]?")]
    internal static partial Regex LeadingNumberOptionalSeparatorRegexPattern();

    [GeneratedRegex(@"\s+")]
    internal static partial Regex WhitespaceRegexPattern();        // Filename-based extraction patterns

    [GeneratedRegex(@"(?i)module\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$", RegexOptions.IgnoreCase)]
    internal static partial Regex ModuleFilenameRegex();

    [GeneratedRegex(@"(?i)lesson\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$", RegexOptions.IgnoreCase)]
    internal static partial Regex LessonFilenameRegex();

    [GeneratedRegex(@"(?i)(week|unit|session|class)\s*[_-]?\s*(\d+)[_-]?\s*(.+?)(?:\.\w+)?$", RegexOptions.IgnoreCase)]
    internal static partial Regex WeekUnitFilenameRegex();

    [GeneratedRegex(@"(?i)module(\d+)([a-zA-Z]+.*)", RegexOptions.IgnoreCase)]
    internal static partial Regex CompactModuleRegex();

    [GeneratedRegex(@"(?i)lesson(\d+)([a-zA-Z]+.*)", RegexOptions.IgnoreCase)]
    internal static partial Regex CompactLessonRegex();

    [GeneratedRegex(@"(?i)module[_\s-]*(\d+)", RegexOptions.IgnoreCase)]
    internal static partial Regex ModuleNumberSeparatorRegex();

    [GeneratedRegex(@"(?i)lesson[_\s-]*(\d+)", RegexOptions.IgnoreCase)]
    internal static partial Regex LessonNumberSeparatorRegex();

    [GeneratedRegex(@"^(\d+)[_-](.+)", RegexOptions.IgnoreCase)]
    internal static partial Regex NumberedContentRegex();

    // Enhanced directory pattern recognition
    [GeneratedRegex(@"(week|unit)[_\s-]*\d+", RegexOptions.IgnoreCase)]
    internal static partial Regex WeekUnitRegex();

    [GeneratedRegex(@"(module|lesson)[_\s-]*\d+", RegexOptions.IgnoreCase)]
    internal static partial Regex ModuleLessonNumberRegex();

    [GeneratedRegex(@"(session|class)[_\s-]*\d+", RegexOptions.IgnoreCase)]
    internal static partial Regex SessionClassNumberRegex();

    // Pattern for camelCase to space conversion (but avoid breaking number-letter combinations like Week1)
    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])")]
    internal static partial Regex CamelCaseRegex();

    // Smart camelCase conversion that doesn't break number-letter combinations (e.g., Week1 stays Week1)
    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])(?!\d)")]
    internal static partial Regex SmartCamelCaseRegex();

    /// <summary>
    /// Extracts just the numeric module identifier from a directory or file name.
    /// Used specifically for content files to populate YAML frontmatter with number-only module values.
    /// </summary>
    /// <param name="name">The directory or file name to extract the module number from.</param>
    /// <returns>The numeric module identifier (e.g., "01", "02") or null if no number is found.</returns>
    /// <remarks>
    /// <para>
    /// This method is designed for content files (videos, readings, instructions, etc.) that need
    /// consistent numerical module references in their YAML frontmatter, as opposed to friendly
    /// display titles generated by CleanModuleOrLessonName.
    /// </para>
    /// <para>
    /// Content files get: module: "01" (number only)
    /// Regular files get: module: "Module 1 Introduction" (friendly title via CleanModuleOrLessonName)
    /// </para>
    /// <para>
    /// The method recognizes patterns such as:
    /// <list type="bullet">
    /// <item><description>Leading numbers with separators: "01_", "02-", "03."</description></item>
    /// <item><description>Module/lesson with numbers: "module01", "lesson02"</description></item>
    /// <item><description>Week/unit with numbers: "week01", "unit02"</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For content files (videos, readings, instructions):
    /// string result1 = ExtractModuleNumber("01_video-content");        // Returns "01" -> module: "01"
    /// string result2 = ExtractModuleNumber("module02_readings");       // Returns "02" -> module: "02"
    /// string result3 = ExtractModuleNumber("lesson-03-instructions");  // Returns "03" -> module: "03"
    /// string result4 = ExtractModuleNumber("week1-overview");          // Returns "1"  -> module: "1"
    /// </code>
    /// </example>
    public static string? ExtractModuleNumber(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Extract just the numeric identifier, no cleaning or title formatting
        // This ensures content files get simple module numbers like "01", "02" in YAML frontmatter
        var lowerName = name.ToLowerInvariant();

        // Pattern 1: Leading number with separator (01_, 02-, 03.) -> "01", "02", "03"
        var leadingNumberMatch = NumberPrefixRegexPattern().Match(name);
        if (leadingNumberMatch.Success)
        {
            return leadingNumberMatch.Groups[1].Value; // Return number as-is (e.g., "01")
        }

        // Pattern 2: "Module N -" format (Module 1 - Money and Finance) -> "1"
        var moduleSpaceMatch = ModuleSpaceNumberRegex().Match(name);
        if (moduleSpaceMatch.Success)
        {
            return moduleSpaceMatch.Groups[1].Value; // Return number as-is (e.g., "1")
        }

        // Pattern 3: Module/Lesson with number (module01, lesson02, module-4) -> "01", "02", "4"
        var moduleMatch = ModuleNumberSeparatorRegex().Match(lowerName);
        if (moduleMatch.Success)
        {
            return moduleMatch.Groups[1].Value; // Return number as-is (e.g., "01")
        }
        var lessonMatch = LessonNumberSeparatorRegex().Match(lowerName);
        if (lessonMatch.Success)
        {
            return lessonMatch.Groups[1].Value; // Return number as-is (e.g., "02")
        }

        // Pattern 4: Week/Unit with number (week1, unit2) -> "1", "2"
        var weekUnitMatch = WeekUnitNumberExtractRegex().Match(lowerName);
        if (weekUnitMatch.Success)
        {
            return weekUnitMatch.Groups[2].Value; // Return number as-is (e.g., "1")
        }

        // Pattern 5: Session/Class with number (session1, class2) -> "1", "2"
        var sessionClassMatch = SessionClassNumberExtractRegex().Match(lowerName);
        if (sessionClassMatch.Success)
        {
            return sessionClassMatch.Groups[2].Value; // Return number as-is (e.g., "1")
        }

        // No numeric module identifier found
        return null;
    }    // Regex patterns for extracting just the numbers from various naming conventions
    [GeneratedRegex(@"(week|unit)[_\s-]*(\d+)", RegexOptions.IgnoreCase)]
    internal static partial Regex WeekUnitNumberExtractRegex();

    [GeneratedRegex(@"(session|class)[_\s-]*(\d+)", RegexOptions.IgnoreCase)]
    internal static partial Regex SessionClassNumberExtractRegex();

    // Pattern for "Module N -" format (e.g., "Module 1 - Money and Finance")
    [GeneratedRegex(@"(?i)module\s+(\d+)\s*-", RegexOptions.IgnoreCase)]
    internal static partial Regex ModuleSpaceNumberRegex();

    /// <summary>
    /// Determines if a file is content-related (video, reading, instruction, etc.) that should use number-only module extraction.
    /// Content files get number-only module values in YAML frontmatter for consistency.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is content-related, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method distinguishes between two types of files for module metadata population:
    /// </para>
    /// <para>
    /// <strong>Content Files (returns true):</strong> Videos, readings, instructions, assignments, quizzes, etc.
    /// These get simple numeric module values like module: "01", module: "02" in YAML frontmatter.
    /// </para>
    /// <para>
    /// <strong>Regular Files (returns false):</strong> Course structure files, documentation, etc.
    /// These get friendly module titles like module: "Module 1 Introduction" via CleanModuleOrLessonName.
    /// </para>
    /// <para>
    /// <summary>
    /// Determines if a file is content-related (video, reading, instruction, etc.) that should use number-only module extraction.
    /// Content files get number-only module values in YAML frontmatter for consistency.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is content-related, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method distinguishes between two types of files for module metadata population:
    /// </para>
    /// <para>
    /// <strong>Content Files (returns true):</strong> Videos, readings, instructions, assignments, quizzes, etc.
    /// These get simple numeric module values like module: "01", module: "02" in YAML frontmatter.
    /// </para>
    /// <para>
    /// <strong>Regular Files (returns false):</strong> Course structure files, documentation, etc.
    /// These get friendly module titles like module: "Module 1 Introduction" via CleanModuleOrLessonName.
    /// </para>
    /// <para>
    /// The distinction ensures content materials have consistent numerical references while
    /// course structure files have descriptive, human-readable module names.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// IsContentFile("/course/01_module/videos/introduction.mp4");     // Returns true  -> module: "01"
    /// IsContentFile("/course/01_module/readings/chapter1.pdf");       // Returns true  -> module: "01"
    /// IsContentFile("/course/01_module/01_module-overview.md");       // Returns false -> module: "Module 1 Overview"
    /// </code>
    /// </example>
    internal bool IsContentFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var directoryName = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? string.Empty;

        // File extensions that typically indicate content files
        var contentExtensions = new[]
        {
            ".mp4", ".mp3", ".pptx", ".xlsx", ".docx", ".png", ".jpg", ".jpeg", ".gif"
        };

        // Keywords that identify content files requiring number-only module extraction
        // These files get module: "01", "02" instead of module: "Module 1 Introduction"
        var contentKeywords = new[]
        {
            "video", "reading", "instruction", "assignment", "quiz", "exercise",
            "activity", "discussion", "transcript", "slides", "presentation",
            "lecture", "case-study", "project"
        };

        // Directory-based keywords that identify content files
        var contentDirKeywords = new[]
        {
            "video", "reading", "content", "material", "transcript", "slide", "assignment"
        };

        // Check for content file extensions first (most reliable)
        if (contentExtensions.Contains(extension))
        {
            return true;
        }

        // For PDF files, be more specific - only treat as content if they have content keywords
        if (extension == ".pdf")
        {
            // Check if the filename or directory has content-specific keywords
            if (fileName.Contains("instruction") || fileName.EndsWith("-instructions") ||
                fileName.Contains("reading") || fileName.Contains("assignment") ||
                fileName.Contains("exercise") || fileName.Contains("quiz") ||
                fileName.Contains("activity"))
            {
                return true;
            }

            // Check if in a content-specific directory
            if (contentDirKeywords.Any(keyword => directoryName.Contains(keyword)))
            {
                return true;
            }

            // Otherwise, treat PDFs as non-content files (e.g., course materials, notes)
            return false;
        }

        // Special handling for case studies: only treat as content file if under a module
        if (fileName.Contains("case-study") || directoryName.Contains("case-stud"))
        {
            return IsCaseStudyUnderModule(filePath);
        }

        // Check for instruction files explicitly
        if (fileName.Contains("instruction") || fileName.Contains("instructions"))
        {
            return true;
        }

        // Check if the filename contains content-related keywords (excluding case-study which is handled above)
        var nonCaseStudyKeywords = new[]
        {
            "video", "reading", "instruction", "assignment", "quiz", "exercise",
            "activity", "discussion", "transcript", "slides", "presentation",
            "lecture", "project"
        };

        if (nonCaseStudyKeywords.Any(keyword => fileName.Contains(keyword)))
        {
            return true;
        }        // Check if any parent directory is explicitly for content
        if (contentDirKeywords.Any(keyword => directoryName.Contains(keyword)))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Simple static method to determine if a filename suggests it's a content file.
    /// Used for filename parsing where we don't have full path context.
    /// </summary>
    /// <param name="filename">The filename to check.</param>
    /// <returns>True if the filename suggests it's a content file.</returns>
    private static bool IsContentFileFromName(string filename)
    {
        var fileNameLower = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
        var extension = Path.GetExtension(filename).ToLowerInvariant();

        // File extensions that typically indicate content files
        var contentExtensions = new[]
        {
            ".mp4", ".mp3", ".pptx", ".xlsx", ".docx", ".png", ".jpg", ".jpeg", ".gif"
        };

        // Keywords that identify content files
        var contentKeywords = new[]
        {
            "video", "reading", "instruction", "assignment", "quiz", "exercise",
            "activity", "discussion", "transcript", "slides", "presentation",
            "lecture", "project"
        };

        // Check for content file extensions
        if (contentExtensions.Contains(extension))
        {
            return true;
        }

        // Check for instruction files explicitly
        if (fileNameLower.Contains("instruction") || fileNameLower.Contains("instructions"))
        {
            return true;
        }

        // Check if the filename contains content-related keywords
        if (contentKeywords.Any(keyword => fileNameLower.Contains(keyword)))
        {
            return true;
        }

        // For case studies, default to treating as content file for filename parsing
        // The full path analysis will handle the proper logic
        if (fileNameLower.Contains("case-study") || fileNameLower.Contains("case_study"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a case study file is located under a module directory.
    /// Case studies should only get module extraction if they're under a module folder.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="vaultRoot">The vault root path for hierarchical validation.</param>
    /// <returns>True if the case study is under a module directory, false if it's at class level.</returns>
    /// <remarks>
    /// <para>
    /// This method walks up the directory hierarchy to determine if the case study is:
    /// <list type="bullet">
    /// <item><description>Under a module folder: Should get module extraction (content file behavior)</description></item>
    /// <item><description>At class level: Should NOT get module extraction (non-content file behavior)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For case studies under a module, this method goes up two parent folders to the class level    /// and uses the vault root to validate the hierarchical level before extracting module information.
    /// </para>
    /// </remarks>
    private bool IsCaseStudyUnderModule(string filePath)
    {
        try
        {
            // Use path manipulation instead of FileInfo for non-existent paths
            string? directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return false;
            }

            string currentDirName = Path.GetFileName(directoryPath);

            // Go up to Case Studies folder (if we're in it)
            var directoryNameLower = currentDirName.ToLowerInvariant();
            if (directoryNameLower.Contains("case") || directoryNameLower.Contains("stud"))
            {
                directoryPath = Path.GetDirectoryName(directoryPath); // Now at Module or Class level
                if (string.IsNullOrEmpty(directoryPath))
                {
                    return false;
                }
                currentDirName = Path.GetFileName(directoryPath);
            }

            // Check if we're at a module level by looking for module patterns
            if (HasNumberPrefix(currentDirName) ||
                currentDirName.ToLowerInvariant().Contains("module") ||
                currentDirName.ToLowerInvariant().Contains("week") ||
                currentDirName.ToLowerInvariant().Contains("unit"))
            {
                // We're at module level - case study is under a module
                return true;
            }

            return false; // Case study is at class level, don't treat as content file
        }
        catch (Exception)
        {
            return false; // Default to class level (no module extraction) on error
        }
    }

    /// <summary>
    /// Extracts module number for content files by walking up parent directories to find the first directory with a module pattern.
    /// Uses intelligent directory analysis to distinguish between module and lesson directories.
    /// </summary>
    /// <param name="directory">The starting directory to examine.</param>
    /// <returns>The module number (e.g., "4", "01", "02") if found, otherwise null.</returns>
    private string? ExtractModuleNumberFromParentDirectories(DirectoryInfo? directory)
    {
        if (directory == null)
        {
            return null;
        }

        // Use VaultRoot to determine the relative hierarchy level for better module detection
        string? vaultRoot = VaultRoot;
        DirectoryInfo? rootDirectory = null;

        if (!string.IsNullOrEmpty(vaultRoot) && Directory.Exists(vaultRoot))
        {
            rootDirectory = new DirectoryInfo(vaultRoot);
            logger.LogDebug($"Using vault root for hierarchical analysis: {vaultRoot}");
        }

        // Use the existing directory analysis logic to find the module directory intelligently
        var directoryParts = GetDirectoryParts(directory);

        // If we have vault root context, filter directory parts to only include those relative to vault
        if (rootDirectory != null && directory.FullName.StartsWith(rootDirectory.FullName, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = Path.GetRelativePath(rootDirectory.FullName, directory.FullName);
            directoryParts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            logger.LogDebug($"Using vault-relative directory parts for analysis: [{string.Join(", ", directoryParts)}]");
        }

        var moduleDir = FindModuleDirectory(directoryParts);

        if (moduleDir != null)
        {
            logger.LogDebug($"Found module directory for content file: {moduleDir}");

            // Extract the number from the identified module directory
            var moduleNumber = ExtractModuleNumber(moduleDir);
            if (!string.IsNullOrEmpty(moduleNumber))
            {
                logger.LogDebug($"Content file module number extracted: {moduleNumber} from module directory: {moduleDir}");
                return moduleNumber;
            }
        }        // Fallback: If FindModuleDirectory didn't work, try the old approach but be more careful
        var currentDir = directory;
        var searchRoot = rootDirectory ?? directory; // Limit search to vault root if available
        while (currentDir != null && (rootDirectory == null || currentDir.FullName.StartsWith(rootDirectory.FullName, StringComparison.OrdinalIgnoreCase)))
        {
            var dirName = currentDir.Name;
            logger.LogDebug($"Examining directory for content file module extraction: {dirName}");

            // Only consider directories that have clear module indicators (not lesson directories)
            if (IsModuleDirectoryForContentFiles(dirName))
            {
                var moduleNumber = ExtractModuleNumber(dirName);
                if (!string.IsNullOrEmpty(moduleNumber))
                {
                    logger.LogDebug($"Content file module number extracted: {moduleNumber} from directory: {dirName}");
                    return moduleNumber;
                }
            }

            // Move up to parent directory
            currentDir = currentDir.Parent;
        }

        logger.LogDebug("No module number found in parent directory hierarchy");
        return null;
    }

    /// <summary>
    /// Determines if a directory name is likely a module name for content file processing.
    /// More restrictive than general module detection to avoid lesson directories.
    /// </summary>
    /// <param name="dirName">The directory name to check.</param>
    /// <returns>True if the directory name is likely a module name.</returns>
    private static bool IsModuleDirectoryForContentFiles(string dirName)
    {
        if (!HasNumberPrefix(dirName))
        {
            return false;
        }

        var lowerName = dirName.ToLowerInvariant();

        // Strong module indicators
        if (lowerName.Contains("module") ||
            lowerName.Contains("course") ||
            lowerName.Contains("week") ||
            lowerName.Contains("unit"))
        {
            return true;
        }

        // Operations Management specific module patterns
        if (lowerName.Contains("orientation") ||
            lowerName.Contains("configuration") ||
            lowerName.Contains("concept") ||
            lowerName.Contains("application") ||
            lowerName.Contains("strategy"))
        {
            return true;
        }

        // Avoid lesson-like patterns
        if (lowerName.Contains("lesson") ||
            lowerName.Contains("about") ||
            lowerName.Contains("defining") ||
            lowerName.Contains("transcript"))
        {
            return false;
        }

        // For numbered directories, assume it's a module if it's higher in the hierarchy
        // This is handled by the FindModuleDirectory logic above
        return true; // If it has a number prefix and isn't explicitly a lesson, treat as module
    }

    /// <summary>
    /// Finds the most likely module directory from a list of directory parts.
    /// Prioritizes numbered directories over course directories to better identify modules.
    /// </summary>
    /// <param name="directoryParts">The directory parts to analyze.</param>
    /// <returns>The name of the directory that most likely represents a module, or null if none found.</returns>
    private static string? FindModuleDirectory(string[] directoryParts)
    {
        if (directoryParts.Length == 0)
        {
            return null;
        }

        // First pass: look for explicit module keywords
        for (int i = directoryParts.Length - 1; i >= 0; i--)
        {
            var dirName = directoryParts[i].ToLowerInvariant();

            if (dirName.Contains("module") ||
                dirName.Contains("week") ||
                dirName.Contains("unit"))
            {
                return directoryParts[i]; // Found an explicit module directory
            }
        }

        // Second pass: look for numbered directories that might be modules (higher priority than course)
        for (int i = directoryParts.Length - 1; i >= 0; i--)
        {
            // Check for numbered prefix patterns typical of module directories
            if (NumberPrefixRegexPattern().IsMatch(directoryParts[i]))
            {
                return directoryParts[i]; // Found a numbered directory - prioritize this over course
            }
        }

        // Third pass: look for course directories (lower priority than numbered directories)
        for (int i = directoryParts.Length - 1; i >= 0; i--)
        {
            var dirName = directoryParts[i].ToLowerInvariant();

            if (dirName.Contains("course"))
            {
                return directoryParts[i]; // Found a course directory
            }
        }

        return null;
    }

    /// <summary>
    /// Gets directory parts from a DirectoryInfo, filtering out empty parts.
    /// </summary>
    /// <param name="directory">The directory to get parts from.</param>
    /// <returns>Array of directory part names.</returns>
    private static string[] GetDirectoryParts(DirectoryInfo? directory)
    {
        if (directory == null)
            return [];

        var parts = new List<string>();
        var current = directory;

        while (current != null)
        {
            if (!string.IsNullOrEmpty(current.Name))
            {
                parts.Insert(0, current.Name);
            }
            current = current.Parent;
        }

        return [.. parts];
    }

    /// <summary>
    /// Finds the most likely lesson directory from a list of directory parts.
    /// </summary>
    /// <param name="directoryParts">The directory parts to analyze.</param>
    /// <param name="moduleDir">The identified module directory to help context.</param>
    /// <returns>The name of the directory that most likely represents a lesson, or null if none found.</returns>
    private static string? FindLessonDirectory(string[] directoryParts, string? moduleDir)
    {
        if (directoryParts.Length == 0)
            return null;

        // Look for directories with lesson-like names
        for (int i = directoryParts.Length - 1; i >= 0; i--)
        {
            var dirName = directoryParts[i].ToLowerInvariant();

            if (dirName.Contains("lesson") ||
                dirName.Contains("session") ||
                dirName.Contains("lecture") ||
                dirName.Contains("class"))
            {
                return directoryParts[i];
            }
        }
        return null;
    }
}
