// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Interface for extracting course structure information from files and metadata.
/// </summary>

public interface ICourseStructureExtractor
{
    /// <summary>
    /// Extracts module and lesson information from a file and updates the provided metadata dictionary.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="metadata">The metadata dictionary to update.</param>
    void ExtractModuleAndLesson(string filePath, IDictionary<string, object?> metadata);
}