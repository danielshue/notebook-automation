using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Utility class for extracting course structure information (modules, lessons) from file paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides functionality to identify module and lesson information from file paths
    /// based on directory naming conventions.
    /// </para>
    /// <para>
    /// It supports detection of module and lesson information by analyzing directory names that contain
    /// "module" or "lesson" within them, and provides clean formatting by removing numbering prefixes
    /// and converting to title case.
    /// </para>
    /// <para>
    /// It also handles directory structures where numbering prefixes (like "01_") indicate module or lesson
    /// hierarchy, even when the directory names don't explicitly contain "module" or "lesson" keywords.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="CourseStructureExtractor"/> class.
    /// </remarks>    /// <param name="logger">Logger for diagnostic and warning messages.</param>
    public partial class CourseStructureExtractor(ILogger<CourseStructureExtractor> logger)
    {
        private readonly ILogger<CourseStructureExtractor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private static readonly Regex NumberPrefixRegex = MyRegex();

        /// <summary>
        /// Extracts module and lesson information from a file path and adds it to the provided metadata dictionary.
        /// </summary>
        /// <param name="filePath">The full path to the file.</param>
        /// <param name="metadata">The metadata dictionary to update with module/lesson information.</param>
        public void ExtractModuleAndLesson(string filePath, Dictionary<string, object> metadata)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("Cannot extract module/lesson from empty file path");
                return;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var dir = fileInfo.Directory;
                string? module = null;
                string? lesson = null;

                if (dir != null)
                {
                    // First attempt: Look for explicit "module" and "lesson" keywords
                    (module, lesson) = ExtractByKeywords(dir);

                    // Second attempt: If the first approach didn't yield both module and lesson,
                    // try to extract from numbered directory structure (01_something, 02_something)
                    if (module == null || lesson == null)
                    {
                        (module, lesson) = ExtractByNumberedPattern(dir);
                    }
                }

                // Update metadata with extracted information
                if (!string.IsNullOrEmpty(module))
                {
                    metadata["module"] = module;
                }
                if (!string.IsNullOrEmpty(lesson))
                {
                    metadata["lesson"] = lesson;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarningWithPath(ex, "Failed to extract module/lesson from directory structure for file: {filePath}", filePath);
            }
        }

        /// <summary>
        /// Extracts module and lesson information by looking for explicit keywords in directory names.
        /// </summary>
        /// <param name="dir">The directory to analyze.</param>
        /// <returns>A tuple with (module, lesson) information, where either may be null.</returns>
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
        /// Extracts module and lesson information by analyzing numbered directory patterns.        /// </summary>
        /// <param name="dir">Starting directory to analyze.</param>
        /// <returns>A tuple with (module, lesson) information, where either may be null.</returns>
        private static (string? module, string? lesson) ExtractByNumberedPattern(DirectoryInfo dir)
        {
            string? module = null;
            string? lesson = null;

            var currentDir = dir;
            var parentDir = currentDir?.Parent;
            var grandParentDir = parentDir?.Parent;

            // First check if current directory looks like a lesson or module (numbered prefix)
            if (currentDir != null && HasNumberPrefix(currentDir.Name))
            {
                // Check if it contains "course" in the name - if so, treat as a module
                if (currentDir.Name.Contains("course", StringComparison.CurrentCultureIgnoreCase))
                {
                    module = CleanModuleOrLessonName(currentDir.Name);
                }
                else
                {
                    lesson = CleanModuleOrLessonName(currentDir.Name);

                    // Then check if parent directory looks like a module (numbered prefix)
                    if (parentDir != null && HasNumberPrefix(parentDir.Name))
                    {
                        module = CleanModuleOrLessonName(parentDir.Name);
                    }
                }
            }
            // If we didn't find lesson in current dir but parent dir has number prefix
            else if (parentDir != null && HasNumberPrefix(parentDir.Name))
            {
                // Check if parent dir contains "course" - if so, treat as a module
                if (parentDir.Name.Contains("course", StringComparison.CurrentCultureIgnoreCase))
                {
                    module = CleanModuleOrLessonName(parentDir.Name);
                }
                else
                {
                    // Use parent as lesson
                    lesson = CleanModuleOrLessonName(parentDir.Name);

                    // And grandparent as module if available
                    if (grandParentDir != null && HasNumberPrefix(grandParentDir.Name))
                    {
                        module = CleanModuleOrLessonName(grandParentDir.Name);
                    }
                }
            }

            // If we still have no module or lesson, but current directory has a number prefix,
            // treat the current directory as a module (common case with single-level directories)
            if (module == null && lesson == null && currentDir != null && HasNumberPrefix(currentDir.Name))
            {
                module = CleanModuleOrLessonName(currentDir.Name);
            }

            return (module, lesson);
        }

        /// <summary>
        /// Determines if a directory name has a numeric prefix like "01_" or "02-".
        /// </summary>
        /// <param name="dirName">Name of the directory to check.</param>
        /// <returns>True if the directory name starts with a numeric prefix.</returns>
        private static bool HasNumberPrefix(string dirName)
        {
            return NumberPrefixRegex.IsMatch(dirName);
        }

        /// <summary>
        /// Cleans up module or lesson folder names by removing numbering prefixes and formatting properly.
        /// </summary>
        /// <param name="folderName">The raw folder name.</param>
        /// <returns>A cleaned and formatted folder name.</returns>
        public static string CleanModuleOrLessonName(string folderName)
        {
            // Remove numbering prefix (e.g., 01_, 02-, etc.), replace hyphens/underscores, title case
            string clean = MyRegex1().Replace(folderName, "");
            clean = clean.Replace("-", " ").Replace("_", " ");
            clean = MyRegex2().Replace(clean, " ").Trim();
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean);
        }

        [GeneratedRegex(@"^(\d+)[_-]", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"^\d+[_-]?")]
        private static partial Regex MyRegex1();
        [GeneratedRegex(@"\s+")]
        private static partial Regex MyRegex2();
    }
}
