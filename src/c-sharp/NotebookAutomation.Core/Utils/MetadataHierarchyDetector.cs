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
public class MetadataHierarchyDetector()
{
    public required ILogger<MetadataHierarchyDetector> Logger { get; init; }

    /// <summary>
    /// Gets the root path of the notebook vault.
    /// </summary>
    /// <remarks>
    /// The vault root is determined either from the application configuration or an override provided during initialization.
    /// </remarks>
    public string? VaultRoot { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataHierarchyDetector"/> class.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="appConfig"></param>
    /// <param name="programOverride"></param>
    /// <param name="verbose"></param>
    /// <param name="vaultRootOverride"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public MetadataHierarchyDetector(ILogger<MetadataHierarchyDetector> logger, AppConfig appConfig, string? vaultRootOverride = null) : this()
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        VaultRoot = !string.IsNullOrEmpty(vaultRootOverride) ? vaultRootOverride : appConfig?.Paths?.NotebookVaultFullpathRoot ?? throw new ArgumentNullException(nameof(appConfig), "Notebook vault path is required");
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

        // Validate that the path exists
        bool isFile = File.Exists(filePath);
        bool isDirectory = Directory.Exists(filePath);

        if (!isFile && !isDirectory)
        {
            Logger.LogWarning($"Path does not exist or is not accessible: {filePath}");
            return info;
        }

        try
        {
            // Get the path elements between vault root and the provided file/directory
            Logger.LogDebug($"DEBUG: FindHierarchyInfo - vault root: '{VaultRoot}', filePath: '{filePath}'");
            string relativePath = GetRelativePath(VaultRoot, filePath);

            Logger.LogDebug($"DEBUG: FindHierarchyInfo - relativePath: '{relativePath}'");
            string[] pathSegments = [.. relativePath.Split(Path.DirectorySeparatorChar).Where(p => !string.IsNullOrEmpty(p)
            && !p.StartsWith("."))];

            Logger.LogDebug($"Path segments for hierarchy detection: {string.Join(" > ", pathSegments)}");

            // Calculate the depth level - this determines which hierarchy fields to include
            int depthLevel = pathSegments.Length;

            Logger.LogDebug($"Path depth level: {depthLevel}");

            // Hierarchy mapping based on semantic meaning:
            // Level 0: Vault root/main index - NO hierarchy metadata
            // Level 1 (e.g., Program): Program level - program only
            // Level 2 (e.g., Finance): Course level - program + course
            // Level 3 (e.g., Corporate-Finance): Class level - program + course + class
            // Level 4+: Module/content level - program + course + class + module

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

                // Only set class if we're at class level or deeper (depth >= 3)
                if (depthLevel >= 3)
                {
                    info["class"] = pathSegments[2];
                    Logger.LogDebug($"Setting class from third path segment: {info["class"]}");

                    // Only set module if we're at module level or deeper (depth >= 4)
                    if (depthLevel >= 4)
                    {
                        info["module"] = pathSegments[3];
                        Logger.LogDebug($"Setting module from fourth path segment: {info["module"]}");
                    }
                }
            }

            Logger.LogDebug($"Path-based hierarchy detection results: program='{info["program"]}', course='{info["course"]}', class='{info["class"]}', module='{(info.ContainsKey("module") ? info["module"] : string.Empty)}'");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error during path-based hierarchy detection: {ex.Message}");
        }

        // If program is still empty (no override and no path elements), use vault root name as fallback
        if (string.IsNullOrEmpty(info["program"]))
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


            var program = info.TryGetValue("program", out var programValue) ? programValue : string.Empty;
            var course = info.TryGetValue("course", out var courseValue) ? courseValue : string.Empty;
            var classValue = info.TryGetValue("class", out var classValueValue) ? classValueValue : string.Empty;

            // Debug logging to help understand the final hierarchy
            Logger.LogDebug($"Final metadata info: Program='{program}', Course='{course}', Class='{classValue}'");
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
    public Dictionary<string, object> UpdateMetadataWithHierarchy(Dictionary<string, object> metadata, Dictionary<string, string> hierarchyInfo, string? templateType = null)
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
        string[] hierarchyLevels = ["program", "course", "class", "module"];        // If maxLevel is 0, remove all hierarchy metadata
        if (maxLevel == 0)
        {
            foreach (var level in hierarchyLevels)
            {
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
            currentLevel++;

            // If we've exceeded the maximum level for this index type, remove any existing metadata
            if (currentLevel > maxLevel)
            {
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
