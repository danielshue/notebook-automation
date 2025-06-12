// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Utility class for formatting file paths in log messages.
/// </summary>

public static class PathFormatter
{
    /// <summary>
    /// Maximum length for truncated paths in log messages.
    /// </summary>
    private const int MaxPathLength = 80;

    /// <summary>
    /// Formats a file path for logging based on the log level.
    /// Uses the full path for Debug or Trace levels, and a shortened path otherwise.
    /// </summary>
    /// <param name="path">The file path to format.</param>
    /// <param name="logLevel">The logging level.</param>
    /// <returns>The formatted path string.</returns>
    public static string Format(string path, LogLevel logLevel)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        // For Debug or Trace level, use the full path
        if (logLevel == LogLevel.Debug || logLevel == LogLevel.Trace)
        {
            return path;
        }

        // For other levels, show a shortened path
        return ShortenPath(path, MaxPathLength);
    }

    /// <summary>
    /// Shortens a path to not exceed the specified maximum length.
    /// Keeps the filename and as much of the path as possible.
    /// </summary>
    /// <param name="path">The path to shorten.</param>
    /// <param name="maxLength">Maximum allowed length.</param>
    /// <returns>A shortened path.</returns>
    public static string ShortenPath(string path, int maxLength)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        if (path.Length <= maxLength)
        {
            return path;
        }

        // Always include the file name
        string fileName = Path.GetFileName(path);

        // If just the filename is too long, truncate it
        if (fileName.Length >= maxLength)
        {
            return "..." + fileName[Math.Max(0, fileName.Length - maxLength + 4)..];
        }

        // Calculate how much path we can include
        int pathLength = maxLength - fileName.Length - 4; // 4 for "...\"
        if (pathLength <= 0)
        {
            return "..." + Path.DirectorySeparatorChar + fileName;
        }

        string directory = Path.GetDirectoryName(path) ?? string.Empty;
        if (directory.Length <= pathLength)
        {
            return path; // Shouldn't happen since we already checked path.Length > maxLength
        }

        // Get the end portion of the directory path
        string shortenedDirectory = directory[^pathLength..];

        // Find the first directory separator to ensure we start with a complete directory name
        int firstSeparatorIndex = shortenedDirectory.IndexOf(Path.DirectorySeparatorChar);
        if (firstSeparatorIndex > 0)
        {
            shortenedDirectory = shortenedDirectory[firstSeparatorIndex..];
        }

        return "..." + shortenedDirectory + Path.DirectorySeparatorChar + fileName;
    }
}