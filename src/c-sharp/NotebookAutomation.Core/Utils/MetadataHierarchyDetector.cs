// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Detects and infers hierarchical metadata (program, course, class, module) from file paths in a notebook vault.
/// </summary>
/// <remarks>
/// <para>
/// Implements path-based hierarchy detection based on the directory structure.
/// Determines the appropriate program, course, class, and module metadata based on a file's location,
/// following the conventions used in the notebook vault.
/// </para>
/// <para> ///. <b>Expected Directory Structure:</b>
/// <code>
/// Vault Root (main-index) - NO hierarchy metadata
/// └── Program Folders (program-index) - program only
///     └── Course Folders (course-index) - program + course
///         └── Class Folders (class-index) - program + course + class
///             ├── Case Study Folders (case-study-index) - program + course + class + module
///             └── Module Folders (module-index) - program + course + class + module
///                 ├── Live Session Folder (live-session-index) - program + course + class + module
///                 └── Lesson Folders (lesson-index) - program + course + class + module
///                     └── Content Files (readings, videos, transcripts, etc.)
/// </code>
/// </para>
/// <para>
/// Features:
/// - Configurable vault root path (from config or override)
/// - Support for explicit program overrides via parameter
/// - Dynamic hierarchy detection based on folder structure
/// - Dynamic fallback to folder names when index files aren't available
/// - Robust path traversal for hierarchy detection.
/// </para>
/// <example>
/// <code>
/// var detector = new MetadataHierarchyDetector(logger, appConfig);
/// var info = detector.FindHierarchyInfo(@"C:\\notebook-vault\\MBA\\Course1\\ClassA\\Lesson1\\file.md");
/// // info["program"] == "MBA", info["course"] == "Course1", info["class"] == "ClassA"
/// </code>
/// </example>
/// </remarks>

public class MetadataHierarchyDetector : IMetadataHierarchyDetector
{
    public ILogger<MetadataHierarchyDetector> Logger { get; }
    public string? VaultRoot { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataHierarchyDetector"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="appConfig">Application configuration for vault root path.</param>
    /// <param name="vaultRootOverride">Optional override for the vault root path.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MetadataHierarchyDetector(ILogger<MetadataHierarchyDetector> logger, AppConfig appConfig, string? vaultRootOverride = null)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        VaultRoot = !string.IsNullOrEmpty(vaultRootOverride)
            ? vaultRootOverride
            : appConfig?.Paths?.NotebookVaultFullpathRoot
                ?? throw new ArgumentNullException(nameof(appConfig), "Notebook vault path is required");
    }

    /// <summary>
    /// Finds program, course, class, and module information by analyzing the file path structure relative to the vault root.
    /// </summary>
    /// <param name="filePath">The path to the file or directory to analyze.</param>
    /// <returns>A dictionary with keys <c>program</c>, <c>course</c>, <c>class</c>, and possibly <c>module</c> containing the detected hierarchy information.</returns>
    /// <remarks>
    /// <para>
    /// This method uses purely path-based analysis to determine hierarchy levels, with no file system access needed.
    /// It assumes a standard folder structure where:
    /// - The first folder level below vault root is the program (e.g., Value Chain Management)
    /// - The second folder level is the course (e.g., Operations Management)
    /// - The third folder level is the class (e.g., Supply Chain Fundamentals)
    /// - The fourth folder level is the module (e.g., Week 1).
    /// </para>
    /// <para>
    /// Priority is given to explicit program overrides if provided in the constructor.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var info = detector.FindHierarchyInfo(@"C:\\notebook-vault\\MBA\\Finance\\Accounting\\Week1\\file.md");
    /// // info["program"] == "Value Chain Management", info["course"] == "Finance", info["class"] == "Accounting", info["module"] == "Week 1"
    /// </code>
    /// </example>
    public Dictionary<string, string> FindHierarchyInfo(string filePath)
    {
        // Initialize with empty values for all standard hierarchy levels
        var info = new Dictionary<string, string>
        {
            { "program", string.Empty },
            { "course", string.Empty },
            { "class", string.Empty },

            // Note: module is only added if present
        };        // Log if path doesn't exist, but continue with path-based analysis
        bool isFile = File.Exists(filePath);
        bool isDirectory = Directory.Exists(filePath);

        if (!isFile && !isDirectory)
        {
            Logger.LogDebug($"Path does not exist, but continuing with path-based analysis: {filePath}");
        }

        // Initialize tracking variables
        int depthLevel = 0;

        try
        {
            // Get the path elements between vault root and the provided file/directory
            Logger.LogDebug($"DEBUG: FindHierarchyInfo - vault root: '{VaultRoot}', filePath: '{filePath}'");
            string relativePath = GetRelativePath(VaultRoot!, filePath);

            Logger.LogDebug($"DEBUG: FindHierarchyInfo - relativePath: '{relativePath}'");
            string[] pathSegments = [.. relativePath.Split(Path.DirectorySeparatorChar).Where(p => !string.IsNullOrEmpty(p)
            && !p.StartsWith("."))];

            // Special handling for files: determine if the filename should be considered part of hierarchy
            if (isFile && pathSegments.Length > 0)
            {
                string filename = pathSegments[^1];
                string parentFolderName = pathSegments.Length > 1 ? pathSegments[^2] : string.Empty;

                // If the filename appears to be a generic content file (not a template or resource),
                // and the parent folder seems to be a course-level folder, exclude the filename
                if (pathSegments.Length == 3 && // File is at course level (program/course/file.ext)
                    !filename.Contains("template", StringComparison.OrdinalIgnoreCase) &&
                    !filename.Contains("resource", StringComparison.OrdinalIgnoreCase) &&
                    !parentFolderName.Equals("Resources", StringComparison.OrdinalIgnoreCase))
                {
                    // This appears to be a content file directly in a course folder - exclude filename
                    pathSegments = pathSegments[..^1];
                    Logger.LogDebug($"Content file in course folder detected, excluding filename '{filename}' from hierarchy calculation");
                }
                else
                {
                    // File appears to represent a meaningful hierarchy component (template, resource, etc.)
                    Logger.LogDebug($"File '{filename}' appears to represent a hierarchy component, including in calculation");
                }
            }

            Logger.LogDebug($"Path segments for hierarchy detection: {string.Join(" > ", pathSegments)}");

            // Calculate the depth level - this determines which hierarchy fields to include
            depthLevel = pathSegments.Length;

            Logger.LogDebug($"Path depth level: {depthLevel}");
            // Hierarchy mapping based on semantic meaning:
            // Depth 0: Vault root/main index - NO hierarchy metadata
            // Depth 1 (e.g., Program): Program level - program only
            // Depth 2 (e.g., Finance): Course level - program + course
            // Depth 3 (e.g., Corporate-Finance): Class level - program + course + class
            // Depth 4+: Module/content level - program + course + class + module

            // Only set program if we're at program level or deeper (depth >= 1)
            if (depthLevel >= 1)
            {
                info["program"] = pathSegments[0];
                Logger.LogDebug($"Setting program from first path segment: {info["program"]}");
            }

            // Only set course if we're at course level or deeper (depth >= 2)
            if (depthLevel >= 2)
            {
                info["course"] = pathSegments[1];
                Logger.LogDebug($"Setting course from second path segment: {info["course"]}");
            }

            // Only set class if we're at class level or deeper (depth >= 3)
            if (depthLevel >= 3)
            {
                info["class"] = pathSegments[2];
                Logger.LogDebug($"Setting class from third path segment: {info["class"]}");
            }            // Only set module if we're at module level or deeper (depth >= 4)
            if (depthLevel >= 4)
            {
                // For content files, extract just the numeric part (e.g., "05_operations-resilience" -> "05")
                if (IsPathLikelyContentFile(filePath))
                {
                    string numericModulePrefix = ExtractNumericModulePrefix(pathSegments[3]);
                    info["module"] = numericModulePrefix;
                    Logger.LogDebug($"Setting numeric-only module from fourth path segment: '{numericModulePrefix}' (original: '{pathSegments[3]}')");
                }
                else
                {
                    info["module"] = pathSegments[3];
                    Logger.LogDebug($"Setting module from fourth path segment: {info["module"]}");
                }
            }

            Logger.LogDebug($"Path-based hierarchy detection results: program='{info["program"]}', course='{info["course"]}', class='{info["class"]}', module='{(info.ContainsKey("module") ? info["module"] : string.Empty)}'");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error during path-based hierarchy detection: {ex.Message}");
        }

        // If program is still empty and we're not at vault root level (depth > 0), use vault root name as fallback
        if (string.IsNullOrEmpty(info["program"]) && depthLevel > 0 && !string.IsNullOrEmpty(VaultRoot))
        {
            string vaultRootName = Path.GetFileName(Path.GetFullPath(VaultRoot).TrimEnd(Path.DirectorySeparatorChar));

            if (!string.IsNullOrEmpty(vaultRootName))
            {
                info["program"] = vaultRootName;
                Logger.LogDebug($"No program from path, using vault root folder name: {vaultRootName}");
            }
            else
            {
                Logger.LogDebug($"No program could be determined from path or vault root");
            }
        }

        // Debug logging to help understand the final hierarchy
        var program = info.TryGetValue("program", out var programValue) ? programValue : string.Empty;
        var course = info.TryGetValue("course", out var courseValue) ? courseValue : string.Empty;
        var classValue = info.TryGetValue("class", out var classValueValue) ? classValueValue : string.Empty;
        var module = info.TryGetValue("module", out var moduleValue) ? moduleValue : string.Empty;

        Logger.LogDebug($"Final metadata info: Program='{program}', Course='{course}', Class='{classValue}', Module='{module}'");

        return info;
    }

    /// <summary>
    /// Updates a metadata dictionary with program, course, class, and module information appropriate for a specific hierarchy level.
    /// </summary>
    /// <param name="metadata">The existing metadata dictionary to update (will be mutated).</param>
    /// <param name="hierarchyInfo">The hierarchy information to apply (should contain keys for hierarchical levels).</param>
    /// <param name="templateType">Optional template type to determine which hierarchy levels to include. Defaults to including all detected levels.</param>
    /// <returns>The updated metadata dictionary with hierarchy fields set if missing or empty.</returns>
    /// <remarks>
    /// <para>
    /// Only updates fields that are missing or empty in the original metadata.
    /// The method will look for the following keys in the hierarchyInfo dictionary:
    /// - program: The program name (top level of the hierarchy) - included for all index types
    /// - course: The course name (second level of the hierarchy) - included for course, class and module index types
    /// - class: The class name (third level of the hierarchy) - included for class and module index types
    /// - module: The module name (fourth level of the hierarchy) - included only for module index types.
    /// </para>
    /// <para>
    /// Each level only includes metadata appropriate for its level in the hierarchy.
    /// For example, a program-level index will only include program metadata, while
    /// a class-level index will include program, course, and class metadata.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For a program-level index
    /// var updated = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(metadata, info, "program-index");
    /// // Only includes program metadata
    ///
    /// // For a class-level index
    /// var updated = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(metadata, info, "class-index");
    /// // Includes program, course, and class metadata
    /// </code>
    /// </example>
    public Dictionary<string, object?> UpdateMetadataWithHierarchy(Dictionary<string, object?> metadata, Dictionary<string, string> hierarchyInfo, string? templateType = null)
    {

        // Determine which levels to include based on the index type
        int maxLevel;

        Logger.LogDebug($"UpdateMetadataWithHierarchy called with templateType='{templateType}'");

        if (string.IsNullOrEmpty(templateType) || templateType == "main-index" || templateType == "main")
        {
            // For main-index (vault root), include only program metadata
            maxLevel = 1;
            Logger.LogDebug($"Setting maxLevel=1 for main index (templateType='{templateType}')");
        }
        else if (templateType == "program-index" || templateType == "program")
        {
            // For program index, only include program
            maxLevel = 1;
        }
        else if (templateType == "course-index" || templateType == "course")
        {
            // For course index, include program and course
            maxLevel = 2;
        }
        else if (templateType == "class-index" || templateType == "class")
        {
            // For class index, include program, course, and class
            maxLevel = 3;
        }
        else if (templateType == "module-index" || templateType == "module" || templateType == "lesson-index" || templateType == "lesson")
        {
            // For module/lesson indices, include all levels
            maxLevel = 4;
        }
        else if (templateType?.EndsWith("-note") == true)
        {
            // For content templates (ending with -note), include all hierarchy levels
            maxLevel = 4;
        }
        else
        {
            // For unknown types, include all levels as a fallback
            maxLevel = 4;


        }

        // List of hierarchy levels in order (top to bottom)
        string[] hierarchyLevels = ["program", "course", "class", "module"];        // Special handling for content files - preserve module value regardless of template type
        bool isContentFile = false;

        // Method 1: Check templateType field for content keywords
        if (metadata.ContainsKey("templateType"))
        {
            string? templateValue = metadata["templateType"]?.ToString();
            if (!string.IsNullOrEmpty(templateValue))
            {
                // Check if this is a content-related template
                string[] contentTemplatePatterns = { "video", "reading", "instruction", "resource", "content" };
                foreach (var pattern in contentTemplatePatterns)
                {
                    if (templateValue.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        isContentFile = true;
                        Logger.LogDebug($"Detected content file by templateType '{templateValue}', will preserve module value");
                        break;
                    }
                }
            }
        }

        // Method 2: Check if module value is numeric-only (indicating a content file)
        if (!isContentFile && metadata.ContainsKey("module"))
        {
            string? moduleValue = metadata["module"]?.ToString();
            if (!string.IsNullOrEmpty(moduleValue) && Regex.IsMatch(moduleValue, @"^\d{1,2}$"))
            {
                isContentFile = true;
                Logger.LogDebug($"Detected content file by numeric-only module value '{moduleValue}', will preserve module value");
            }
        }

        // If maxLevel is 0, remove all hierarchy metadata (except module for content files)
        if (maxLevel == 0)
        {
            foreach (var level in hierarchyLevels)
            {
                // Special handling for module field on content files - preserve it
                if (level == "module" && isContentFile && metadata.ContainsKey("module"))
                {
                    Logger.LogDebug($"Preserving content file module value '{metadata["module"]}' despite maxLevel=0");
                    continue;
                }

                if (metadata.ContainsKey(level))
                {
                    Logger.LogDebug($"Removing hierarchy metadata '{level}' for maxLevel=0 (templateType={templateType})");
                    metadata.Remove(level);
                }
            }

            return metadata;
        }

        // Track if we've broken the hierarchy chain

        bool hierarchyChainBroken = false;
        int currentLevel = 0;

        // Update each level in sequence, but only up to the maximum level for this index type
        foreach (var level in hierarchyLevels)
        {
            currentLevel++;            // If we've exceeded the maximum level for this index type, remove any existing metadata
            // But preserve module value for content files regardless of maxLevel
            if (currentLevel > maxLevel)
            {
                // Special handling for module field on content files
                if (level == "module" && isContentFile && metadata.ContainsKey("module"))
                {
                    Logger.LogDebug($"Preserving content file module value '{metadata["module"]}' despite maxLevel={maxLevel}");
                    continue;
                }

                if (metadata.ContainsKey(level))
                {
                    Logger.LogDebug($"Removing hierarchy metadata '{level}' for maxLevel={maxLevel} (templateType={templateType})");
                    metadata.Remove(level);
                }

                continue;
            }

            // If the hierarchy chain is broken, remove any remaining levels
            if (hierarchyChainBroken)
            {
                if (metadata.ContainsKey(level))
                {
                    Logger.LogDebug($"Removing hierarchy metadata '{level}' due to broken chain (templateType={templateType})");
                    metadata.Remove(level);
                }

                continue;
            }

            // Check if this level exists in the hierarchy info

            if (hierarchyInfo.TryGetValue(level, out string? value) && !string.IsNullOrEmpty(value))
            {
                // Only update if the current value is missing or empty (non-aggressive update)
                var currentValue = metadata.ContainsKey(level) ? metadata[level]?.ToString() : null;
                if (string.IsNullOrEmpty(currentValue))
                {
                    Logger.LogDebug($"Updating hierarchy metadata '{level}' from '{currentValue}' to '{value}' (templateType={templateType})");
                    metadata[level] = value;
                }
                else
                {
                    Logger.LogDebug($"Keeping existing hierarchy metadata '{level}' = '{currentValue}' (templateType={templateType})");
                }
            }
            else
            {
                // If this level is missing from hierarchy info, remove it and break the chain for lower levels
                if (metadata.ContainsKey(level))
                {
                    Logger.LogDebug($"Removing hierarchy metadata '{level}' - not found in hierarchy info (templateType={templateType})");
                    metadata.Remove(level);
                }

                hierarchyChainBroken = true;
            }
        }

        return metadata;
    }

    /// <summary>
    /// Gets the relative path from the base directory to the target path.
    /// </summary>
    /// <param name="basePath">The base directory path to calculate from.</param>
    /// <param name="fullPath">The full file or directory path to calculate to.</param>
    /// <returns>The relative path from <paramref name="basePath"/> to <paramref name="fullPath"/>.</returns>
    /// <remarks>
    /// This method provides robust relative path calculation with proper normalization and edge case handling:
    /// - Handles null or empty inputs gracefully
    /// - Normalizes paths using <see cref="Path.GetFullPath"/> for consistent comparison
    /// - Returns empty string when the paths are identical (vault root case)
    /// - Ensures proper directory separator handling for cross-platform compatibility
    /// - Returns the full path if it's not a subdirectory of the base path
    ///
    /// This is primarily used for calculating hierarchy levels by determining the path segments
    /// between the vault root and a target file or directory.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic relative path calculation
    /// string relative = GetRelativePath("/vault", "/vault/Program/Course");
    /// // Returns: "Program/Course"
    ///
    /// // Same path handling
    /// string relative = GetRelativePath("/vault", "/vault");
    /// // Returns: "" (empty string)
    ///
    /// // Cross-platform path handling
    /// string relative = GetRelativePath("C:\\vault", "C:\\vault\\Program\\Course");
    /// // Returns: "Program\\Course" (on Windows)
    /// </code>
    /// </example>
    private static string GetRelativePath(string basePath, string fullPath)
    {
        // Handle null or empty base path
        if (string.IsNullOrEmpty(basePath))
        {
            return fullPath ?? string.Empty;
        }

        // Handle null or empty full path
        if (string.IsNullOrEmpty(fullPath))
        {
            return string.Empty;
        }

        // Normalize paths for comparison
        string normalizedBasePath = Path.GetFullPath(basePath);
        string normalizedFullPath = Path.GetFullPath(fullPath);

        // If the paths are the same (vault root case), return empty string
        if (string.Equals(normalizedBasePath, normalizedFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        // Ensure trailing directory separator for proper relative path calculation
        if (!normalizedBasePath.EndsWith(Path.DirectorySeparatorChar))
        {
            normalizedBasePath += Path.DirectorySeparatorChar;
        }

        if (normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedFullPath[normalizedBasePath.Length..];
        }

        // If not a subdirectory, return the full path (though this shouldn't happen)
        return normalizedFullPath;
    }

    /// <summary>
    /// Calculates the hierarchy level of a folder relative to the vault root.
    /// </summary>
    /// <param name="folderPath">The folder path to analyze.</param>
    /// <param name="vaultPath">The vault root path. If null, uses the instance VaultRoot.</param>
    /// <returns>The hierarchy level (0 = vault root, 1 = program, 2 = course, 3 = class, 4 = module, 5 = lesson, etc.).</returns>
    /// <remarks>
    /// Hierarchy levels:
    /// - Level 0: Vault root
    /// - Level 1: Program level (e.g., "Value Chain Management")
    /// - Level 2: Course level (e.g., "Operations Management")
    /// - Level 3: Class level (e.g., "Supply Chain Fundamentals")
    /// - Level 4: Module level (e.g., "Week 1")
    /// - Level 5: Lesson level (e.g., "Lesson 1")
    /// - Level 6+: Content level (e.g., subdirectories within lessons)
    /// </remarks>
    public int CalculateHierarchyLevel(string folderPath, string? vaultPath = null)
    {
        // Use provided vault path or fall back to instance VaultRoot
        string effectiveVaultPath = vaultPath ?? VaultRoot ?? throw new InvalidOperationException("Vault root path is required");

        // Normalize and get full paths for consistent comparison
        string fullVaultPath = Path.GetFullPath(effectiveVaultPath);

        // Handle paths consistently - paths with leading slash are treated as relative to vault root
        string normalizedFolderPath = folderPath;
        if (folderPath.StartsWith('/') || folderPath.StartsWith('\\'))
        {
            // Remove leading slash and treat as relative path
            normalizedFolderPath = folderPath.TrimStart('/', '\\');
            Logger.LogDebug("CalculateHierarchyLevel - removed leading slash from '{Original}' -> '{Normalized}'", folderPath, normalizedFolderPath);
        }

        // Determine if the path should be treated as absolute or relative
        string fullFolderPath;
        if (Path.IsPathRooted(normalizedFolderPath) && !normalizedFolderPath.StartsWith('/') && !normalizedFolderPath.StartsWith('\\'))
        {
            // True absolute path (e.g., C:\path\to\folder)
            fullFolderPath = Path.GetFullPath(normalizedFolderPath);
            Logger.LogDebug("CalculateHierarchyLevel - treating as absolute path: '{NormalizedPath}' -> '{FullPath}'", normalizedFolderPath, fullFolderPath);
        }
        else
        {
            // Relative path - combine with vault root
            fullFolderPath = Path.GetFullPath(Path.Combine(effectiveVaultPath, normalizedFolderPath));
            Logger.LogDebug("CalculateHierarchyLevel - treating as relative path, combining with vault root: '{VaultRoot}' + '{RelPath}' -> '{FullPath}'",
                effectiveVaultPath, normalizedFolderPath, fullFolderPath);
        }

        // Platform-appropriate path comparison strategy
        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        Logger.LogDebug("CalculateHierarchyLevel - folderPath: '{FolderPath}' -> normalized: '{NormalizedPath}' -> fullPath: '{FullFolderPath}'", folderPath, normalizedFolderPath, fullFolderPath);
        Logger.LogDebug("CalculateHierarchyLevel - vaultPath: '{VaultPath}' -> fullPath: '{FullVaultPath}'", effectiveVaultPath, fullVaultPath);
        Logger.LogDebug("CalculateHierarchyLevel - using {PathComparison} for path comparison", pathComparison);

        // Check if folder is the vault root
        if (string.Equals(fullFolderPath, fullVaultPath, pathComparison))
        {
            Logger.LogDebug("Folder equals vault root, returning level 0");
            return 0; // Vault root
        }

        // Verify that the folder path is actually within the vault root
        string relativePath = Path.GetRelativePath(fullVaultPath, fullFolderPath);
        if (relativePath.StartsWith("..") || Path.IsPathRooted(relativePath))
        {
            Logger.LogWarning("Folder path '{FullFolderPath}' is not within vault root '{FullVaultPath}'. Relative path: '{RelativePath}'", fullFolderPath, fullVaultPath, relativePath);
            return -1; // Invalid - not within vault
        }

        // Split path into segments and calculate hierarchy level
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        int level = segments.Length;

        Logger.LogDebug("relativePath: '{RelativePath}'", relativePath);
        Logger.LogDebug("segments: [{Segments}]", string.Join(", ", segments));
        Logger.LogDebug("calculated level: {Level}", level);

        return level;
    }

    /// <summary>
    /// Maps hierarchy level to the corresponding template type string.
    /// </summary>
    /// <param name="level">The hierarchy level to convert to a template type.</param>
    /// <returns>The corresponding template type string for the given hierarchy level.</returns>
    /// <remarks>
    /// <para>
    /// This method provides the canonical mapping between numerical hierarchy levels
    /// and their semantic template types. The mapping aligns with the standard
    /// vault structure and template naming conventions used throughout the system.
    /// </para>
    ///
    /// <para>
    /// <b>Hierarchy Level Mapping:</b>
    /// - Level 0: "main" - Vault root (typically not used for index generation)
    /// - Level 1: "program" - Program level (e.g., "Value Chain Management", "MBA")
    /// - Level 2: "course" - Course level (e.g., "Operations Management", "Finance")
    /// - Level 3: "class" - Class level (e.g., "Supply Chain Fundamentals", "Accounting")
    /// - Level 4: "module" - Module level (e.g., "Week 1", "Module 3")
    /// - Level 5: "lesson" - Lesson level (e.g., "Lesson 1" with Readings, Transcripts, Notes)
    /// - Level 6+: "unknown" - Content level (subdirectories within lessons)
    /// </para>
    ///
    /// <para>
    /// These template types are used by the template engine to select appropriate
    /// templates for index generation, ensuring consistent formatting and structure
    /// across different levels of the educational content hierarchy.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Standard hierarchy mappings
    /// string type1 = GetTemplateTypeFromHierarchyLevel(1); // Returns: "program"
    /// string type2 = GetTemplateTypeFromHierarchyLevel(2); // Returns: "course"
    /// string type3 = GetTemplateTypeFromHierarchyLevel(3); // Returns: "class"
    /// string type4 = GetTemplateTypeFromHierarchyLevel(4); // Returns: "module"
    /// string type5 = GetTemplateTypeFromHierarchyLevel(5); // Returns: "lesson"
    ///
    /// // Edge cases
    /// string type0 = GetTemplateTypeFromHierarchyLevel(0); // Returns: "main"
    /// string type6 = GetTemplateTypeFromHierarchyLevel(6); // Returns: "unknown"
    /// string type99 = GetTemplateTypeFromHierarchyLevel(99); // Returns: "unknown"
    /// </code>
    /// </example>
    public string GetTemplateTypeFromHierarchyLevel(int level)
    {
        return level switch
        {
            0 => "main",      // Vault root (should not occur in practice)
            1 => "program",   // Program level (e.g., Value Chain Management)
            2 => "course",    // Course level (e.g., Operations Management)
            3 => "class",     // Class level (e.g., Supply Chain Fundamentals)
            4 => "module",    // Module level (e.g., Week 1)
            5 => "lesson",    // Lesson subdirectory (with Readings, Transcripts, Notes)
            _ => "unknown",   // Content level (Level 6+)
        };
    }

    /// <summary>
    /// Calculates the hierarchy level of a folder relative to a base path, with optional level offset.
    /// </summary>
    /// <param name="folderPath">The folder path to analyze for hierarchy level calculation.</param>
    /// <param name="basePath">The base path to calculate relative positioning from.</param>
    /// <param name="baseHierarchyLevel">The hierarchy level of the base path (default: 0 for vault root).</param>
    /// <returns>The adjusted hierarchy level accounting for the base path's position in the hierarchy.</returns>
    /// <remarks>
    /// <para>
    /// This method is particularly useful when using --override-vault-root functionality,
    /// where processing starts from a subdirectory but hierarchy levels need to be calculated
    /// relative to the true vault structure. It maintains correct hierarchy relationships
    /// even when the processing context changes.
    /// </para>
    ///
    /// <para>
    /// The method performs the following operations:
    /// 1. Normalizes both paths for consistent comparison
    /// 2. Checks if the folder path is under the base path
    /// 3. Calculates the relative depth between the paths
    /// 4. Adds the relative depth to the base hierarchy level
    /// </para>
    ///
    /// <para>
    /// This ensures that a lesson directory maintains its level 5 designation even when
    /// processing starts from a module directory (level 4), preserving the semantic
    /// meaning of hierarchy levels across different processing contexts.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Standard usage: lesson content relative to lesson directory
    /// var levelOffset = detector.CalculateHierarchyLevelWithOffset(
    ///     "/vault/program/course/class/module/lesson/readings",
    ///     "/vault/program/course/class/module/lesson",
    ///     5); // Returns 6 (lesson content level)
    ///
    /// // Override scenario: maintain hierarchy when starting from module
    /// var levelOffset = detector.CalculateHierarchyLevelWithOffset(
    ///     "/vault/program/course/class/module/lesson",
    ///     "/vault/program/course/class/module",
    ///     4); // Returns 5 (lesson level)
    ///
    /// // Same path scenario
    /// var levelOffset = detector.CalculateHierarchyLevelWithOffset(
    ///     "/vault/program/course",
    ///     "/vault/program/course",
    ///     2); // Returns 2 (same as base level)
    /// </code>
    /// </example>
    public int CalculateHierarchyLevelWithOffset(string folderPath, string basePath, int baseHierarchyLevel = 0)
    {
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(basePath))
        {
            Logger.LogWarning("CalculateHierarchyLevelWithOffset: Null or empty paths provided. Folder: '{FolderPath}', Base: '{BasePath}'",
                folderPath ?? "null", basePath ?? "null");
            return 0;
        }

        string normalizedFolderPath = Path.GetFullPath(folderPath).Replace('\\', '/');
        string normalizedBasePath = Path.GetFullPath(basePath).Replace('\\', '/');

        Logger.LogDebug("CalculateHierarchyLevelWithOffset: Folder='{FolderPath}', Base='{BasePath}', BaseLevel={BaseLevel}",
            normalizedFolderPath, normalizedBasePath, baseHierarchyLevel);

        // If folder path is the same as base path, return the base hierarchy level
        if (string.Equals(normalizedFolderPath, normalizedBasePath, StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogDebug("CalculateHierarchyLevelWithOffset: Paths are equal, returning base level {BaseLevel}", baseHierarchyLevel);
            return baseHierarchyLevel;
        }

        // If folder path is not under base path, calculate relative to base path
        if (!normalizedFolderPath.StartsWith(normalizedBasePath + "/", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning("CalculateHierarchyLevelWithOffset: Folder '{FolderPath}' is not under base '{BasePath}', returning base level",
                normalizedFolderPath, normalizedBasePath);
            return baseHierarchyLevel;
        }

        // Calculate the relative depth from base path
        string relativePath = normalizedFolderPath.Substring(normalizedBasePath.Length + 1); // +1 to skip the separator
        int relativeDepth = relativePath.Split('/').Length;
        int adjustedLevel = baseHierarchyLevel + relativeDepth;

        Logger.LogDebug("CalculateHierarchyLevelWithOffset: RelativePath='{RelativePath}', RelativeDepth={RelativeDepth}, AdjustedLevel={AdjustedLevel}",
            relativePath, relativeDepth, adjustedLevel);

        return adjustedLevel;
    }

    /// <summary>
    /// Detects if a file path is likely to be a content file based on filename and directory patterns.
    /// </summary>
    /// <param name="filePath">The file path to analyze for content file characteristics.</param>
    /// <returns>True if the path appears to be a content file, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method uses multiple heuristics to identify content files versus structural files:
    /// </para>
    ///
    /// <para>
    /// <b>Non-Content Patterns (return false):</b>
    /// - Files containing "index", "readme", "template", "module-" in the name
    /// - Standalone "overview.md" files (typically index files)
    /// </para>
    ///
    /// <para>
    /// <b>Content Patterns (return true):</b>
    /// - Filename keywords: video, reading, instruction, assignment, quiz, exercise, activity, discussion, transcript, slides, presentation, notes, summary, lecture, content, material
    /// - Directory keywords: Same list applied to parent directory names
    /// - File extensions: .mp4, .pdf, .pptx, .docx
    /// - Path keywords: video, reading, resource, content, material anywhere in the full path
    /// </para>
    ///
    /// <para>
    /// This classification affects how module metadata is handled during hierarchy detection.
    /// Content files may have their module values preserved or extracted differently to
    /// maintain appropriate organizational context.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Content files (return true)
    /// IsPathLikelyContentFile("/vault/course/module/video-introduction.mp4");
    /// IsPathLikelyContentFile("/vault/course/module/readings/chapter1.pdf");
    /// IsPathLikelyContentFile("/vault/course/module/assignments/homework1.md");
    ///
    /// // Non-content files (return false)
    /// IsPathLikelyContentFile("/vault/course/module/index.md");
    /// IsPathLikelyContentFile("/vault/course/module/overview.md");
    /// IsPathLikelyContentFile("/vault/course/module/module-template.md");
    /// </code>
    /// </example>
    private bool IsPathLikelyContentFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        // Get filename and extension
        string fileName = Path.GetFileName(filePath).ToLowerInvariant();
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        string directory = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? string.Empty;

        // Special cases: Common index or template files should never be considered content
        string[] nonContentPatterns = { "index", "readme", "template", "module-" };
        foreach (var pattern in nonContentPatterns)
        {
            if (fileNameWithoutExt.Contains(pattern))
            {
                Logger.LogDebug($"Detected non-content file by pattern '{pattern}': {filePath}");
                return false;
            }
        }

        // Standalone overview.md files are typically index files, not content files
        if (fileNameWithoutExt == "overview")
        {
            Logger.LogDebug($"Detected standalone overview file (likely an index): {filePath}");
            return false;
        }

        // Keywords that identify content files (same list as in CourseStructureExtractor)
        var contentKeywords = new[]
        {
            "video", "reading", "instruction", "assignment", "quiz", "exercise",
            "activity", "discussion", "transcript", "slides", "presentation",
            "notes", "summary", "lecture", "content", "material"
        };

        // Check filename keywords
        foreach (var keyword in contentKeywords)
        {
            if (fileNameWithoutExt.Contains(keyword))
            {
                Logger.LogDebug($"Detected content file by filename keyword '{keyword}': {filePath}");
                return true;
            }
        }

        // Check last directory name for keywords
        if (!string.IsNullOrEmpty(directory))
        {
            string lastDirName = Path.GetFileName(directory);
            foreach (var keyword in contentKeywords)
            {
                if (lastDirName.Contains(keyword))
                {
                    Logger.LogDebug($"Detected content file by directory keyword '{keyword}': {filePath}");
                    return true;
                }
            }
        }

        // Check file extensions associated with content files
        var contentExtensions = new[] { ".mp4", ".pdf", ".pptx", ".docx" };
        if (contentExtensions.Contains(extension))
        {
            Logger.LogDebug($"Detected content file by extension '{extension}': {filePath}");
            return true;
        }

        // Check for indicators in the full path
        var fullPath = filePath.ToLowerInvariant();
        foreach (var keyword in new[] { "video", "reading", "resource", "content", "material" })
        {
            if (fullPath.Contains(keyword))
            {
                Logger.LogDebug($"Detected content file by path keyword '{keyword}': {filePath}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts the numeric prefix from a module string for content file organization.
    /// </summary>
    /// <param name="moduleString">The module string to extract the numeric prefix from.</param>
    /// <returns>The numeric prefix if found, or the original string if no numeric prefix is detected.</returns>
    /// <remarks>
    /// <para>
    /// This method is specifically designed for content files where module organization
    /// follows numeric prefixing conventions. It handles common module naming patterns
    /// used in educational content management:
    /// </para>
    ///
    /// <para>
    /// <b>Supported Patterns:</b>
    /// - "01_module-name" → "01"
    /// - "02-module-name" → "02"
    /// - "module03_name" → "03"
    /// - "week05_content" → "05"
    /// - "03" → "03"
    /// </para>
    ///
    /// <para>
    /// The extraction uses regex patterns to identify numeric prefixes (1-2 digits)
    /// with common separators (underscore, hyphen) or alphanumeric prefixes.
    /// This enables consistent module organization for content files while preserving
    /// semantic module names for structural elements.
    /// </para>
    ///
    /// <para>
    /// If no numeric prefix is found, the original string is returned unchanged,
    /// ensuring graceful handling of non-standard naming conventions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Standard patterns
    /// string result1 = ExtractNumericModulePrefix("05_operations-resilience");
    /// // Returns: "05"
    ///
    /// string result2 = ExtractNumericModulePrefix("module03_supply-chain");
    /// // Returns: "03"
    ///
    /// string result3 = ExtractNumericModulePrefix("week07-logistics");
    /// // Returns: "07"
    ///
    /// // Edge cases
    /// string result4 = ExtractNumericModulePrefix("12");
    /// // Returns: "12"
    ///
    /// string result5 = ExtractNumericModulePrefix("introduction");
    /// // Returns: "introduction" (no numeric prefix found)
    /// </code>
    /// </example>
    private string ExtractNumericModulePrefix(string moduleString)
    {
        if (string.IsNullOrEmpty(moduleString))
            return moduleString;

        // Look for common patterns like "01_xyz", "02-xyz", "module03_xyz"
        var match = Regex.Match(moduleString, @"^(?:[a-zA-Z_-]*)?(\d{1,2})[\-_]");
        if (match.Success)
        {
            string numericPart = match.Groups[1].Value;
            Logger.LogDebug($"Extracted numeric module prefix: '{numericPart}' from '{moduleString}'");
            return numericPart;
        }

        // Try numbers with no separator
        match = Regex.Match(moduleString, @"^(\d{1,2})");
        if (match.Success)
        {
            string numericPart = match.Groups[1].Value;
            Logger.LogDebug($"Extracted numeric module prefix: '{numericPart}' from '{moduleString}'");
            return numericPart;
        }

        // If no pattern matches, return the original string
        return moduleString;
    }
}
