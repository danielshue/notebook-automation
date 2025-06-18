// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Provides utility methods for working with file and directory paths.
/// </summary>
/// <remarks>
/// <para>
/// The PathUtils class contains static methods for common path-related operations
/// needed throughout the notebook automation system, such as:
/// <list type="bullet">
///   <item><description>Finding paths relative to the application or directory</description></item>
///   <item><description>Ensuring directories exist</description></item>
///   <item><description>Normalizing paths across platforms</description></item>
///   <item><description>Generating safe file paths for new files</description></item>
/// </list>
/// </para>
/// <para>
/// This class is designed to centralize path handling logic and ensure consistent behavior
/// across different parts of the application, especially for operations that need to handle
/// cross-platform path differences.
/// </para>
/// </remarks>
public static class PathUtils
{
    /// <summary>
    /// Gets a path for a file relative to the application's base directory.
    /// </summary>
    /// <param name="relativePath">The relative path from the application directory.</param>
    /// <returns>The full path to the file.</returns>
    /// <remarks>
    /// <para>
    /// This method constructs a path relative to the application's base directory, which is
    /// the directory containing the executing assembly. This is useful for finding configuration
    /// files and other resources that ship alongside the application.
    /// </para>
    /// <para>
    /// The method handles normalization to ensure paths work correctly on all platforms.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string configPath = PathUtils.GetPathRelativeToApp("config/settings.json");
    /// </code>
    /// </example>
    public static string GetPathRelativeToApp(string relativePath)
    {
        // In single-file publish, Assembly.Location returns an empty string. Use AppContext.BaseDirectory.
        string baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, NormalizePath(relativePath));
    }

    /// <summary>
    /// Gets a path for a file relative to a specified directory.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="relativePath">The relative path from the base directory.</param>
    /// <returns>The full path to the file.</returns>
    /// <remarks>
    /// <para>
    /// This method constructs a path relative to the specified base directory. It normalizes
    /// both paths to ensure they work correctly on all platforms, and combines them using
    /// platform-appropriate path separators.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string outputPath = PathUtils.GetPathRelativeToDirectory(projectDir, "output/results.json");
    /// </code>
    /// </example>
    public static string GetPathRelativeToDirectory(string basePath, string relativePath)
    {
        return Path.Combine(NormalizePath(basePath), NormalizePath(relativePath));
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directoryPath">The directory path to ensure exists.</param>
    /// <returns>The normalized directory path.</returns>
    /// <remarks>
    /// <para>
    /// This method checks if the specified directory exists, and creates it (including any
    /// necessary parent directories) if it doesn't. It returns the normalized path to the
    /// directory, which is useful for chaining operations.
    /// </para>
    /// <para>
    /// The method normalizes the path to ensure it works correctly on all platforms.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string outputDir = PathUtils.EnsureDirectoryExists("output/reports/monthly");
    /// string filePath = Path.Combine(outputDir, "report.pdf");
    /// </code>
    /// </example>
    public static string EnsureDirectoryExists(string directoryPath)
    {
        var normalizedPath = NormalizePath(directoryPath);
        if (!Directory.Exists(normalizedPath))
        {
            Directory.CreateDirectory(normalizedPath);
        }

        return normalizedPath;
    }

    /// <summary>
    /// Normalizes a path by converting slashes to the platform-specific path separator.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    /// <remarks>
    /// <para>
    /// This method normalizes a path by replacing all forward and backward slashes with the
    /// platform-specific path separator character. This ensures that paths work correctly
    /// on all platforms, regardless of how they were originally specified.
    /// </para>
    /// <para>
    /// The method also trims any leading or trailing whitespace to prevent common errors.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Works on both Windows and Unix:
    /// string normalizedPath = PathUtils.NormalizePath("reports\\monthly/current");
    /// </code>
    /// </example>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // Replace both slash types with the OS-specific separator
        path = path.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        // If on Windows, make sure drive letters are correctly formatted
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && path.Length > 1 && path[1] == ':')
        {
            // Make sure the drive letter is uppercase
            path = char.ToUpper(path[0]) + path[1..];
        }

        return path;
    }

    /// <summary>
    /// Generates a unique file path by appending a number if the file already exists.
    /// </summary>
    /// <param name="baseFilePath">The base file path (including extension).</param>
    /// <returns>A unique file path that doesn't exist yet.</returns>
    /// <remarks>
    /// <para>
    /// This method checks if a file already exists at the specified path, and if so,
    /// generates a new path by appending a number before the extension. For example,
    /// if "report.pdf" exists, it will try "report (1).pdf", then "report (2).pdf", and so on.
    /// </para>
    /// <para>
    /// This is useful for generating output file paths that won't overwrite existing files.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string safePath = PathUtils.GenerateUniqueFilePath("output/report.pdf");
    /// File.WriteAllText(safePath, content);
    /// </code>
    /// </example>
    public static string GenerateUniqueFilePath(string baseFilePath)
    {
        if (!File.Exists(baseFilePath))
        {
            return baseFilePath;
        }

        string directory = Path.GetDirectoryName(baseFilePath) ?? string.Empty;
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(baseFilePath);
        string extension = Path.GetExtension(baseFilePath);

        int counter = 1;
        string newPath;

        do
        {
            newPath = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
            counter++;
        }
        while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// Makes a path relative to a base directory.
    /// </summary>
    /// <param name="basePath">The base directory path.</param>
    /// <param name="fullPath">The full path to make relative.</param>
    /// <returns>The relative path, or the full path if it can't be made relative.</returns>
    /// <remarks>
    /// <para>
    /// This method attempts to make a path relative to a specified base directory. This is useful
    /// for storing and displaying shorter, more readable paths that are relative to a known
    /// location like a project directory.
    /// </para>
    /// <para>
    /// If the full path doesn't start with the base path (meaning it's not actually within the
    /// base directory), the method returns the original full path.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string projectDir = "C:/Projects/MyProject";
    /// string fullPath = "C:/Projects/MyProject/src/file.cs";
    /// string relativePath = PathUtils.MakeRelative(projectDir, fullPath);
    /// // Result: "src/file.cs"
    /// </code>
    /// </example>
    public static string MakeRelative(string basePath, string fullPath)
    {
        var normalizedBase = NormalizePath(basePath);
        var normalizedFull = NormalizePath(fullPath);

        if (normalizedFull.StartsWith(normalizedBase))
        {
            var relativePath = normalizedFull[normalizedBase.Length..];

            // Remove leading directory separator if present
            if (relativePath.StartsWith(Path.DirectorySeparatorChar.ToString()))
            {
                relativePath = relativePath[1..];
            }

            return relativePath;
        }

        return fullPath; // Can't make it relative
    }

    /// <summary>
    /// Gets the common base directory shared by a collection of paths.
    /// </summary>
    /// <param name="paths">The collection of paths.</param>
    /// <returns>The common base directory, or an empty string if there is no common base.</returns>
    /// <remarks>
    /// <para>
    /// This method finds the longest common path prefix shared by all paths in the collection.
    /// This is useful for identifying a common working directory or for organizing files that
    /// are related but might be scattered across different subdirectories.
    /// </para>
    /// <para>
    /// The method returns a directory path (ending with a directory separator), or an empty
    /// string if there is no common base directory (for example, if the paths are on different drives).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string[] paths = new[] {
    ///     "C:/Projects/MyProject/src/file1.cs",
    ///     "C:/Projects/MyProject/src/Models/model.cs",
    ///     "C:/Projects/MyProject/tests/test1.cs"
    /// };
    /// string common = PathUtils.GetCommonBasePath(paths);
    /// // Result: "C:/Projects/MyProject/"
    /// </code>
    /// </example>
    public static string GetCommonBasePath(IEnumerable<string> paths)
    {
        var pathsList = paths.Select(NormalizePath).ToList();
        if (pathsList.Count == 0)
        {
            return string.Empty;
        }

        if (pathsList.Count == 1)
        {
            var dir = Path.GetDirectoryName(pathsList[0]);
            return dir == null ? string.Empty : dir + Path.DirectorySeparatorChar;
        }

        // Find the common prefix of all paths
        var firstPath = pathsList[0];
        var commonPrefix = firstPath;

        foreach (var path in pathsList.Skip(1))
        {
            int prefixLength = 0;
            int maxLength = Math.Min(commonPrefix.Length, path.Length);

            while (prefixLength < maxLength &&
                   char.ToLowerInvariant(commonPrefix[prefixLength]) ==
                   char.ToLowerInvariant(path[prefixLength]))
            {
                prefixLength++;
            }

            commonPrefix = commonPrefix[..prefixLength];
            if (string.IsNullOrEmpty(commonPrefix))
            {
                return string.Empty;
            }
        }

        // Make sure the common prefix ends at a directory separator
        var lastSeparatorIndex = commonPrefix.LastIndexOf(Path.DirectorySeparatorChar);
        if (lastSeparatorIndex >= 0)
        {
            return commonPrefix[..(lastSeparatorIndex + 1)];
        }

        return string.Empty;
    }

    /// <summary>
    /// Resolves an input path by prepending OneDrive root if the path is relative.
    /// </summary>
    /// <param name="inputPath">The input path that may be relative or absolute.</param>
    /// <param name="oneDriveRoot">The OneDrive root directory to prepend for relative paths.</param>
    /// <returns>The resolved absolute path.</returns>
    /// <remarks>
    /// <para>
    /// This method handles path resolution for CLI commands that accept both relative and absolute paths.
    /// If the input path is already absolute (rooted), it returns the path unchanged.
    /// If the input path is relative and an OneDrive root is provided, it combines them to create
    /// an absolute path.
    /// </para>
    /// <para>
    /// This is particularly useful for video-notes and pdf-notes commands where users may want to
    /// specify relative paths from their OneDrive root instead of full absolute paths.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Absolute path - returned unchanged
    /// string resolved1 = PathUtils.ResolveInputPath(@"C:\Users\user\OneDrive\folder", null);
    /// // Result: @"C:\Users\user\OneDrive\folder"
    ///
    /// // Relative path with OneDrive root
    /// string resolved2 = PathUtils.ResolveInputPath("Education/MBA", @"C:\Users\user\OneDrive");
    /// // Result: @"C:\Users\user\OneDrive\Education\MBA"
    ///
    /// // Relative path without OneDrive root - returned unchanged
    /// string resolved3 = PathUtils.ResolveInputPath("folder", null);
    /// // Result: "folder"
    /// </code>
    /// </example>
    public static string ResolveInputPath(string inputPath, string? oneDriveRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);

        // If already absolute, return as-is
        if (Path.IsPathRooted(inputPath))
        {
            return inputPath;
        }

        // If no OneDrive root configured, return relative path as-is
        if (string.IsNullOrWhiteSpace(oneDriveRoot))
        {
            return inputPath;
        }

        // Combine OneDrive root with relative path and normalize
        return NormalizePath(Path.Combine(oneDriveRoot, inputPath));
    }
}
