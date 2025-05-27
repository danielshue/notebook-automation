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
    /// </remarks>
    public class CourseStructureExtractor
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CourseStructureExtractor"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic and warning messages.</param>
        public CourseStructureExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
                    // Look for lesson folder (e.g., lesson-1-...)
                    var lessonDir = dir;
                    if (lessonDir != null && lessonDir.Name.ToLower().Contains("lesson"))
                    {
                        lesson = CleanModuleOrLessonName(lessonDir.Name);
                        // Look for module folder one level up
                        var moduleDir = lessonDir.Parent;
                        if (moduleDir != null && moduleDir.Name.ToLower().Contains("module"))
                        {
                            module = CleanModuleOrLessonName(moduleDir.Name);
                        }
                    }
                    else if (dir.Name.ToLower().Contains("module"))
                    {
                        // If current dir is module, set module only
                        module = CleanModuleOrLessonName(dir.Name);
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
                _logger.LogWarning(ex, "Failed to extract module/lesson from directory structure for file: {Path}", filePath);
            }
        }

        /// <summary>
        /// Cleans up module or lesson folder names by removing numbering prefixes and formatting properly.
        /// </summary>
        /// <param name="folderName">The raw folder name.</param>
        /// <returns>A cleaned and formatted folder name.</returns>
        public static string CleanModuleOrLessonName(string folderName)
        {
            // Remove numbering prefix (e.g., 01_, 02-, etc.), replace hyphens/underscores, title case
            string clean = Regex.Replace(folderName, @"^\d+[_-]?", "");
            clean = clean.Replace("-", " ").Replace("_", " ");
            clean = Regex.Replace(clean, @"\s+", " ").Trim();
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean);
        }
    }
}
