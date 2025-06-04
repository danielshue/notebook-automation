using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Detects hierarchical metadata (program, course, class) from file paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements path-based hierarchy detection similar to the Python version's ensure_metadata.py.
    /// It determines the appropriate program, course, and class metadata based on a file's location
    /// in the directory structure, following the conventions used in the notebook vault.
    /// </para>
    /// <para>
    /// Directory Structure Expected:
    /// - Root (main-index)
    ///   - Program Folders (program-index)
    ///     - Course Folders (course-index)
    ///       - Class Folders (class-index)
    ///         - Case Study Folders (case-study-index)
    ///         - Module Folders (module-index)
    ///           - Live Session Folder (live-session-index)
    ///           - Lesson Folders (lesson-index)
    ///             - Content Files (readings, videos, transcripts, etc.)
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MetadataHierarchyDetector"/> class.    /// </remarks>
    /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="programOverride">Optional explicit program name override.</param>
    /// <param name="verbose">Whether to output verbose logging information.</param>
    public class MetadataHierarchyDetector(
        ILogger<MetadataHierarchyDetector> logger,
        AppConfig appConfig,
        string? programOverride = null,
        bool verbose = false)
    {
        private readonly ILogger<MetadataHierarchyDetector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly string _notebookVaultRoot = appConfig?.Paths?.NotebookVaultFullpathRoot
                ?? throw new ArgumentNullException(nameof(appConfig), "Notebook vault path is required");
        private readonly string? _programOverride = programOverride;
        private readonly YamlHelper _yamlHelper = new(logger);
        private readonly bool _verbose = verbose;

        /// <summary>
        /// Finds program, course, and class information by analyzing the file path and scanning parent directories.
        /// </summary>
        /// <param name="filePath">Path to the file to analyze.</param>
        /// <returns>Dictionary with program, course, and class information.</returns>
        public Dictionary<string, string> FindHierarchyInfo(string filePath)
        {
            var info = new Dictionary<string, string>
            {
                { "program", _programOverride ?? string.Empty },
                { "course", string.Empty },
                { "class", string.Empty }
            };

            if (!File.Exists(filePath))
            {
                _logger.LogWarningWithPath("File does not exist: {FilePath}", nameof(MetadataHierarchyDetector), filePath);
                return info;
            }

            // If program is explicitly provided, we don't need special path handling for program
            if (!string.IsNullOrEmpty(_programOverride))
            {
                if (_verbose)
                {
                    _logger.LogInformationWithPath("Using explicit program override: {Program}", nameof(MetadataHierarchyDetector), _programOverride);
                }
            }

            // Special case handling for Value Chain Management
            string pathStr = filePath;

            // Detect Value Chain Management in the path, prioritizing it over other detection methods
            if (string.IsNullOrEmpty(_programOverride) && pathStr.Contains("Value Chain Management"))
            {
                // Set the program explicitly
                info["program"] = "Value Chain Management";
                if (_verbose)
                {
                    _logger.LogInformationWithPath("Found 'Value Chain Management' in path, using it as program name", nameof(MetadataHierarchyDetector));
                }

                // Extract course and class info from the path structure
                var parts = pathStr.Split(Path.DirectorySeparatorChar);

                int vcmIdx = Array.FindIndex(parts, p => p == "Value Chain Management");
                if (vcmIdx >= 0)
                {
                    // Check if this is the "01_Projects" structure which has additional levels
                    if (vcmIdx + 1 < parts.Length && parts[vcmIdx + 1] == "01_Projects")
                    {
                        // Skip the "01_Projects" level and use the next one for course
                        if (vcmIdx + 2 < parts.Length)
                        {
                            info["course"] = parts[vcmIdx + 2];
                            if (_verbose)
                            {
                                _logger.LogInformationWithPath("Found course in Value Chain Management path: {Course}", nameof(MetadataHierarchyDetector), info["course"]);
                            }

                            // Class would be the next level
                            if (vcmIdx + 3 < parts.Length)
                            {
                                info["class"] = parts[vcmIdx + 3];
                                if (_verbose)
                                {
                                    _logger.LogInformationWithPath("Found class in Value Chain Management path: {Class}", nameof(MetadataHierarchyDetector), info["class"]);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Normal case - course is directly after VCM in the path
                        if (vcmIdx + 1 < parts.Length)
                        {
                            info["course"] = parts[vcmIdx + 1];
                            if (_verbose)
                            {
                                _logger.LogInformationWithPath("Found course in Value Chain Management path: {Course}", nameof(MetadataHierarchyDetector), info["course"]);
                            }

                            // Class would be the next level
                            if (vcmIdx + 2 < parts.Length)
                            {
                                info["class"] = parts[vcmIdx + 2];
                                if (_verbose)
                                {
                                    _logger.LogInformationWithPath("Found class in Value Chain Management path: {Class}", nameof(MetadataHierarchyDetector), info["class"]);
                                }
                            }
                        }
                    }
                }

                if (_verbose)
                {
                    _logger.LogInformationWithPath("Value Chain Management path analysis: program='{Program}', course='{Course}', class='{Class}'", nameof(MetadataHierarchyDetector), info["program"], info["course"], info["class"]);
                }

                // Return early since we've determined the hierarchy for VCM
                return info;
            }

            // Start from the file's directory and move up the tree
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? string.Empty);
            var rootPath = new DirectoryInfo(_notebookVaultRoot);

            // IMPORTANT: For programs, we want the highest level (closest to root)
            // For courses and classes, we want the closest to the file (deepest level)
            DirectoryInfo? highestProgramDir = null;  // Track the highest directory with program-index
            int courseLevel = -1;
            int classLevel = -1;
            int currentLevel = 0;

            // Look through index files in parent directories
            while (currentDir != null && IsSubdirectoryOf(currentDir, rootPath))
            {
                // Look for index files in the current directory
                foreach (var indexFile in currentDir.GetFiles("*.md"))
                {
                    try
                    {
                        var content = File.ReadAllText(indexFile.FullName);
                        var frontmatter = _yamlHelper.ExtractFrontmatter(content);

                        if (!string.IsNullOrEmpty(frontmatter))
                        {
                            var frontmatterDict = _yamlHelper.ParseYamlToDictionary(frontmatter);

                            if (frontmatterDict.TryGetValue("index-type", out object? value))
                            {
                                string indexType = value.ToString() ?? string.Empty;
                                string dirName = currentDir.Name;

                                // For program, we want the highest level (closest to root)
                                // But don't override existing Value Chain Management setting
                                if (indexType == "program-index" && string.IsNullOrEmpty(info["program"]))
                                {
                                    // Only update if we haven't found a program yet, or if this is higher in the hierarchy
                                    if (highestProgramDir == null || GetPathDepth(currentDir) < GetPathDepth(highestProgramDir))
                                    {
                                        highestProgramDir = currentDir;
                                        info["program"] = GetValueOrDefault(frontmatterDict, "title", dirName);
                                        if (_verbose)
                                        {
                                            _logger.LogInformationWithPath("Found program: {Program} at {Dir}", nameof(MetadataHierarchyDetector), info["program"], currentDir.FullName);
                                        }
                                    }
                                }

                                // For course and class, we want the deepest level (closest to the file)
                                else if (indexType == "course-index" && currentLevel > courseLevel)
                                {
                                    courseLevel = currentLevel;
                                    info["course"] = GetValueOrDefault(frontmatterDict, "title", dirName);
                                    if (_verbose)
                                    {
                                        _logger.LogInformationWithPath("Found course: {Course} at {Dir}", nameof(MetadataHierarchyDetector), info["course"], currentDir.FullName);
                                    }
                                }

                                else if (indexType == "class-index" && currentLevel > classLevel)
                                {
                                    classLevel = currentLevel;
                                    info["class"] = GetValueOrDefault(frontmatterDict, "title", dirName);
                                    if (_verbose)
                                    {
                                        _logger.LogInformationWithPath("Found class: {Class} at {Dir}", nameof(MetadataHierarchyDetector), info["class"], currentDir.FullName);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarningWithPath(ex, "Error processing index file {File}: {Error}", nameof(MetadataHierarchyDetector), indexFile.FullName, ex.Message);
                    }
                }

                // Move up to the parent directory
                currentDir = currentDir.Parent;
                currentLevel++;
            }

            // If program is still null, try to determine from path structure
            if (string.IsNullOrEmpty(info["program"]) && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    // Get the relevant parts of the path between vault root and file
                    string relativePath = GetRelativePath(_notebookVaultRoot, filePath);
                    string[] relevant = [.. relativePath.Split(Path.DirectorySeparatorChar).Where(p => !string.IsNullOrEmpty(p) && p != "01_Projects")];

                    // Walk up the directory tree from the file's parent, looking for program-index.md
                    string? programTitle = null;
                    var searchDir = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? string.Empty);
                    var rootDir = new DirectoryInfo(_notebookVaultRoot);

                    while (searchDir != null && IsSubdirectoryOf(searchDir, rootDir))
                    {
                        var programIndex = Path.Combine(searchDir.FullName, "program-index.md");
                        if (File.Exists(programIndex))
                        {
                            try
                            {
                                var content = File.ReadAllText(programIndex);
                                var frontmatter = _yamlHelper.ExtractFrontmatter(content);
                                if (!string.IsNullOrEmpty(frontmatter))
                                {
                                    var frontmatterDict = _yamlHelper.ParseYamlToDictionary(frontmatter);
                                    if (frontmatterDict.TryGetValue("title", out object? value))
                                    {
                                        programTitle = value.ToString();
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // Continue searching if we encounter an error
                            }
                        }
                        searchDir = searchDir.Parent;
                    }

                    if (programTitle != null)
                    {
                        info["program"] = programTitle;
                    }
                    // Fallback to directory structure
                    else if (relevant.Length >= 1)
                    {
                        info["program"] = info["program"] ?? relevant[0];
                    }

                    // Always assign course and class as the next two folders after program, if available
                    if (relevant.Length >= 2 && string.IsNullOrEmpty(info["course"]))
                    {
                        info["course"] = relevant[1];
                    }
                    if (relevant.Length >= 3 && string.IsNullOrEmpty(info["class"]))
                    {
                        info["class"] = relevant[2];
                    }

                    if (_verbose)
                    {
                        _logger.LogInformationWithPath("Path fallback: program='{Program}', course='{Course}', class='{Class}'", nameof(MetadataHierarchyDetector), info["program"], info["course"], info["class"]);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarningWithPath(ex, "Error during path-based fallback: {Error}", nameof(MetadataHierarchyDetector), ex.Message);
                }

                // If we still don't have a program, use a default
                if (string.IsNullOrEmpty(info["program"]))
                {
                    info["program"] = "MBA Program";
                    if (_verbose)
                    {
                        _logger.LogInformationWithPath("No program identifier found, using default: {Program}", nameof(MetadataHierarchyDetector), info["program"]);
                    }
                }
            }

            // Debug logging to help understand the final hierarchy
            if (_verbose)
            {
                _logger.LogInformationWithPath("Final metadata info: Program='{Program}', Course='{Course}', Class='{Class}'", nameof(MetadataHierarchyDetector), info["program"], info["course"], info["class"]);
            }

            return info;
        }

        /// <summary>
        /// Updates metadata dictionary with program, course, and class information.
        /// </summary>
        /// <param name="metadata">The existing metadata dictionary to update.</param>
        /// <param name="hierarchyInfo">The hierarchy information to apply.</param>
        /// <returns>Updated metadata dictionary.</returns>
        public static Dictionary<string, object> UpdateMetadataWithHierarchy(
            Dictionary<string, object> metadata,
            Dictionary<string, string> hierarchyInfo)
        {
            // Update program if needed and if we found program info
            if (!string.IsNullOrEmpty(hierarchyInfo["program"]) &&
                (!metadata.ContainsKey("program") ||
                 string.IsNullOrEmpty(metadata["program"]?.ToString())))
            {
                metadata["program"] = hierarchyInfo["program"];
            }

            // Update course if needed and if we found course info
            if (!string.IsNullOrEmpty(hierarchyInfo["course"]) &&
                (!metadata.ContainsKey("course") ||
                 string.IsNullOrEmpty(metadata["course"]?.ToString())))
            {
                metadata["course"] = hierarchyInfo["course"];
            }

            // Update class if needed and if we found class info
            if (!string.IsNullOrEmpty(hierarchyInfo["class"]) &&
                (!metadata.ContainsKey("class") ||
                 string.IsNullOrEmpty(metadata["class"]?.ToString())))
            {
                metadata["class"] = hierarchyInfo["class"];
            }

            return metadata;
        }

        // Helper methods
        private static string GetValueOrDefault(Dictionary<string, object> dict, string key, string defaultValue)
        {
            if (dict.TryGetValue(key, out var value) && value != null)
            {
                return value.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        private static bool IsSubdirectoryOf(DirectoryInfo child, DirectoryInfo parent)
        {
            if (child == null || parent == null)
                return false;

            var childPath = child.FullName;
            var parentPath = parent.FullName;

            return childPath.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetPathDepth(DirectoryInfo dir)
        {
            return dir.FullName.Split(Path.DirectorySeparatorChar).Length;
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            // Ensure trailing directory separator for proper relative path calculation
            if (!basePath.EndsWith(Path.DirectorySeparatorChar))
                basePath += Path.DirectorySeparatorChar;

            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath[basePath.Length..];
            }

            // If not a subdirectory, return the full path (though this shouldn't happen)
            return fullPath;
        }
    }
}
