// <copyright file="MetadataHierarchyDetector.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Utils/MetadataHierarchyDetector.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using NotebookAutomation.Core.Configuration;

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
public class MetadataHierarchyDetector(
    ILogger<MetadataHierarchyDetector> logger,
    AppConfig appConfig,
    string? programOverride = null,
    bool verbose = false,
    string? vaultRootOverride = null)
{
    private readonly ILogger<MetadataHierarchyDetector> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string notebookVaultRoot = !string.IsNullOrEmpty(vaultRootOverride)
            ? vaultRootOverride
            : appConfig?.Paths?.NotebookVaultFullpathRoot
                ?? throw new ArgumentNullException(nameof(appConfig), "Notebook vault path is required");

    private readonly string? programOverride = programOverride;
    private readonly bool verbose = verbose;

    /// <summary>
    /// Gets the vault root path being used by this detector.
    /// </summary>
    public string VaultRoot => this.notebookVaultRoot;

    /// <summary>
    /// Finds program, course, class, and module information by analyzing the file path structure relative to the vault root.
    /// </summary>
    /// <param name="filePath">The path to the file or directory to analyze.</param>
    /// <returns>A dictionary with keys <c>program</c>, <c>course</c>, <c>class</c>, and possibly <c>module</c> containing the detected hierarchy information.</returns>
    /// <remarks>
    /// <para>
    /// This method uses purely path-based analysis to determine hierarchy levels, with no file system access needed.
    /// It assumes a standard folder structure where:
    /// - The first folder level below vault root is the program (e.g., MBA)
    /// - The second folder level is the course (e.g., Finance)
    /// - The third folder level is the class (e.g., Accounting)
    /// - The fourth folder level is the module (e.g., Week1).
    /// </para>
    /// <para>
    /// Priority is given to explicit program overrides if provided in the constructor.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var info = detector.FindHierarchyInfo(@"C:\\notebook-vault\\MBA\\Finance\\Accounting\\Week1\\file.md");
    /// // info["program"] == "MBA", info["course"] == "Finance", info["class"] == "Accounting", info["module"] == "Week1"
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
        };

        // Add program if override is provided
        if (!string.IsNullOrEmpty(this.programOverride))
        {
            info["program"] = this.programOverride;
        }

        // Validate that the path exists
        bool isFile = File.Exists(filePath);
        bool isDirectory = Directory.Exists(filePath);
        if (!isFile && !isDirectory)
        {
            this.logger.LogWarningWithPath("Path does not exist or is not accessible: {FilePath}", nameof(MetadataHierarchyDetector), filePath);
            return info;
        }

        // If program is explicitly provided via constructor parameter, use it
        if (!string.IsNullOrEmpty(this.programOverride))
        {
            if (this.verbose)
            {
                this.logger.LogInformationWithPath("Using explicit program override: {Program}", nameof(MetadataHierarchyDetector), this.programOverride);
            }
        } // Pure path-based hierarchy detection

        try
        {
            // Get the path elements between vault root and the provided file/directory
            Console.WriteLine($"DEBUG: FindHierarchyInfo - vault root: '{this.notebookVaultRoot}', filePath: '{filePath}'");
            string relativePath = GetRelativePath(this.notebookVaultRoot, filePath);
            Console.WriteLine($"DEBUG: FindHierarchyInfo - relativePath: '{relativePath}'");
            string[] pathSegments = [.. relativePath.Split(Path.DirectorySeparatorChar)
                .Where(p => !string.IsNullOrEmpty(p) && !p.StartsWith("."))];

            if (this.verbose)
            {
                this.logger.LogInformationWithPath(
                    "Path segments for hierarchy detection: {Segments}",
                    nameof(MetadataHierarchyDetector),
                    string.Join(" > ", pathSegments));
            }// Calculate the depth level - this determines which hierarchy fields to include

            int depthLevel = pathSegments.Length;

            if (this.verbose)
            {
                this.logger.LogInformationWithPath(
                    "Path depth level: {DepthLevel}",
                    nameof(MetadataHierarchyDetector), depthLevel);
            } // Hierarchy mapping based on semantic meaning:

            // Level 0: Vault root/main index - NO hierarchy metadata
            // Level 1 (e.g., Program): Program level - program only
            // Level 2 (e.g., Finance): Course level - program + course
            // Level 3 (e.g., Corporate-Finance): Class level - program + course + class
            // Level 4+: Module/content level - program + course + class + module

            // Only set program if we're at program level or deeper (depth >= 1)
            if (string.IsNullOrEmpty(this.programOverride) && depthLevel >= 1)
            {
                info["program"] = pathSegments[0];
                if (this.verbose)
                {
                    this.logger.LogInformationWithPath(
                        "Setting program from first path segment: {Program}",
                        nameof(MetadataHierarchyDetector), info["program"]);
                }
            }

            // Only set course if we're at course level or deeper (depth >= 2)
            if (depthLevel >= 2)
            {
                info["course"] = pathSegments[1];
                if (this.verbose)
                {
                    this.logger.LogInformationWithPath(
                        "Setting course from second path segment: {Course}",
                        nameof(MetadataHierarchyDetector), info["course"]);
                }

                // Only set class if we're at class level or deeper (depth >= 3)
                if (depthLevel >= 3)
                {
                    info["class"] = pathSegments[2];
                    if (this.verbose)
                    {
                        this.logger.LogInformationWithPath(
                            "Setting class from third path segment: {Class}",
                            nameof(MetadataHierarchyDetector), info["class"]);
                    }

                    // Only set module if we're at module level or deeper (depth >= 4)
                    if (depthLevel >= 4)
                    {
                        info["module"] = pathSegments[3];
                        if (this.verbose)
                        {
                            this.logger.LogInformationWithPath(
                                "Setting module from fourth path segment: {Module}",
                                nameof(MetadataHierarchyDetector), info["module"]);
                        }
                    }
                }
            }

            if (this.verbose)
            {
                this.logger.LogInformationWithPath(
                    "Path-based hierarchy detection results: program='{Program}', course='{Course}', class='{Class}', module='{Module}'",
                    nameof(MetadataHierarchyDetector),
                    info["program"],
                    info["course"],
                    info["class"],
                    info.ContainsKey("module") ? info["module"] : string.Empty);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarningWithPath(ex, "Error during path-based hierarchy detection: {Error}",
                nameof(MetadataHierarchyDetector), ex.Message);
        }

        // If program is still empty (no override and no path elements), use vault root name as fallback
        if (string.IsNullOrEmpty(info["program"]))
        {
            string vaultRootName = Path.GetFileName(Path.GetFullPath(this.notebookVaultRoot).TrimEnd(Path.DirectorySeparatorChar));

            if (!string.IsNullOrEmpty(vaultRootName))
            {
                info["program"] = vaultRootName;
                if (this.verbose)
                {
                    this.logger.LogInformationWithPath(
                        "No program from path, using vault root folder name: {Program}",
                        nameof(MetadataHierarchyDetector), vaultRootName);
                }
            }
            else
            {
                if (this.verbose)
                {
                    this.logger.LogInformationWithPath(
                        "No program could be determined from path or vault root",
                        nameof(MetadataHierarchyDetector));
                }
            }
        } // Debug logging to help understand the final hierarchy

        if (this.verbose)
        {
            this.logger.LogInformationWithPath(
                "Final metadata info: Program='{Program}', Course='{Course}', Class='{Class}'",
                nameof(MetadataHierarchyDetector),
                info.TryGetValue("program", out var program) ? program : string.Empty,
                info.TryGetValue("course", out var course) ? course : string.Empty,
                info.TryGetValue("class", out var classValue) ? classValue : string.Empty);
        }

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
    public static Dictionary<string, object> UpdateMetadataWithHierarchy(
        Dictionary<string, object> metadata,
        Dictionary<string, string> hierarchyInfo,
        string? templateType = null)
    { // Determine which levels to include based on the index type
        int maxLevel;

        Console.WriteLine($"DEBUG: UpdateMetadataWithHierarchy called with templateType='{templateType}'");
        if (string.IsNullOrEmpty(templateType) || templateType == "main-index" || templateType == "main")
        {
            // For main-index (vault root), include only program metadata
            maxLevel = 1;
            Console.WriteLine($"DEBUG: Setting maxLevel=1 for main index (templateType='{templateType}')");
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
        }// List of hierarchy levels in order (top to bottom)

        string[] hierarchyLevels = ["program", "course", "class", "module"];        // If maxLevel is 0, remove all hierarchy metadata
        if (maxLevel == 0)
        {
            foreach (var level in hierarchyLevels)
            {
                if (metadata.ContainsKey(level))
                {
                    Console.WriteLine($"DEBUG: Removing hierarchy metadata '{level}' for maxLevel=0 (templateType={templateType})");
                    metadata.Remove(level);
                }
            }

            return metadata;
        } // Track if we've broken the hierarchy chain

        bool hierarchyChainBroken = false;
        int currentLevel = 0;

        // Update each level in sequence, but only up to the maximum level for this index type
        foreach (var level in hierarchyLevels)
        {
            currentLevel++;

            // If we've exceeded the maximum level for this index type, remove any existing metadata
            if (currentLevel > maxLevel)
            {
                if (metadata.ContainsKey(level))
                {
                    Console.WriteLine($"DEBUG: Removing hierarchy metadata '{level}' for maxLevel={maxLevel} (templateType={templateType})");
                    metadata.Remove(level);
                }

                continue;
            }

            // If the hierarchy chain is broken, remove any remaining levels
            if (hierarchyChainBroken)
            {
                if (metadata.ContainsKey(level))
                {
                    Console.WriteLine($"DEBUG: Removing hierarchy metadata '{level}' due to broken chain (templateType={templateType})");
                    metadata.Remove(level);
                }

                continue;
            } // Check if this level exists in the hierarchy info

            if (hierarchyInfo.TryGetValue(level, out string? value) && !string.IsNullOrEmpty(value))
            {
                // Only update if the current value is missing or empty (non-aggressive update)
                var currentValue = metadata.ContainsKey(level) ? metadata[level]?.ToString() : null;
                if (string.IsNullOrEmpty(currentValue))
                {
                    Console.WriteLine($"DEBUG: Updating hierarchy metadata '{level}' from '{currentValue}' to '{value}' (templateType={templateType})");
                    metadata[level] = value;
                }
                else
                {
                    Console.WriteLine($"DEBUG: Keeping existing hierarchy metadata '{level}' = '{currentValue}' (templateType={templateType})");
                }
            }
            else
            {
                // If this level is missing from hierarchy info, remove it and break the chain for lower levels
                if (metadata.ContainsKey(level))
                {
                    Console.WriteLine($"DEBUG: Removing hierarchy metadata '{level}' - not found in hierarchy info (templateType={templateType})");
                    metadata.Remove(level);
                }

                hierarchyChainBroken = true;
            }
        }

        return metadata;
    }

    // Helper methods

    /// <summary>
    /// Gets a value from a dictionary by key, or returns a default if the key is missing or the value is null.
    /// </summary>
    /// <param name="dict">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is missing or null.</param>
    /// <returns>The value from the dictionary, or the default value if not found.</returns>
    private static string GetValueOrDefault(Dictionary<string, object> dict, string key, string defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            return value.ToString() ?? defaultValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the relative path from <paramref name="basePath"/> to <paramref name="fullPath"/>.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="fullPath">The full file or directory path.</param>
    /// <returns>The relative path from <paramref name="basePath"/> to <paramref name="fullPath"/>.</returns>
    private static string GetRelativePath(string basePath, string fullPath)
    {
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
}
