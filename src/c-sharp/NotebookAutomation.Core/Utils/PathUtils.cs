// -----------------------------------------------------------------------------
// PathUtils.cs
// Utilities for cross-platform file path normalization and directory management.
//
// Example:
//     string normalized = PathUtils.NormalizePath("C:/Users/Example/file.md");
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Utilities for handling file paths across different platforms.
    /// 
    /// This class provides methods for path normalization, WSL path conversion,
    /// and universal path handling for Windows and Unix-like systems.
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Determines if the current application is running in WSL (Windows Subsystem for Linux).
        ///
        /// Returns:
        ///     bool: True if running in WSL, false otherwise.
        ///
        /// Example:
        ///     bool isWsl = PathUtils.IsRunningInWsl();
        /// </summary>
        public static bool IsRunningInWsl()
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return false;
                }
                // Check for WSL by reading /proc/version
                if (File.Exists("/proc/version"))
                {
                    string versionContent = File.ReadAllText("/proc/version");
                    return versionContent.Contains("Microsoft") || versionContent.Contains("WSL");
                }
                return false;
            }
            catch (Exception ex)
            {
                // Log error if logger is available in future refactor
                return false;
            }
        }

        /// <summary>
        /// Normalizes a path between Windows and WSL formats.
        ///
        /// Args:
        ///     path (string): The path to normalize.
        ///     logger (ILogger, optional): Logger for reporting issues.
        ///
        /// Returns:
        ///     string: The normalized path.
        ///
        /// Example:
        ///     string norm = PathUtils.NormalizePath("C:/Users/Example/file.md");
        /// </summary>
        public static string NormalizePath(string path, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            try
            {
                if (IsRunningInWsl())
                {
                    return NormalizeWslPath(path, logger);
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return NormalizeWindowsPath(path);
                }
                else
                {
                    return NormalizeUnixPath(path);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error normalizing path: {Path}", path);
                return path; // Return original path if normalization fails
            }
        }

        /// <summary>
        /// Normalizes a Windows path.
        ///
        /// Args:
        ///     path (string): The path to normalize.
        ///
        /// Returns:
        ///     string: The normalized Windows path.
        /// </summary>
        private static string NormalizeWindowsPath(string path)
        {
            // Convert forward slashes to backslashes
            path = path.Replace('/', '\\');
            // Handle UNC paths
            if (path.StartsWith("\\\\"))
            {
                return path;
            }
            // Convert WSL paths to Windows
            if (path.StartsWith("/mnt/"))
            {
                char driveLetter = path[5];
                return $"{driveLetter}:{path.Substring(6).Replace('/', '\\')}";
            }
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Normalizes a Unix path.
        ///
        /// Args:
        ///     path (string): The path to normalize.
        ///
        /// Returns:
        ///     string: The normalized Unix path.
        /// </summary>
        private static string NormalizeUnixPath(string path)
        {
            // Convert backslashes to forward slashes
            path = path.Replace('\\', '/');
            // Convert Windows drive paths to Unix
            if (path.Length >= 2 && path[1] == ':')
            {
                char driveLetter = char.ToLowerInvariant(path[0]);
                return $"/mnt/{driveLetter}{path.Substring(2).Replace('\\', '/')}";
            }
            // Make sure it's an absolute path
            if (!path.StartsWith("/"))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), path)
                    .Replace('\\', '/');
            }
            return path;
        }

        /// <summary>
        /// Normalizes a path specifically for WSL environments.
        ///
        /// Args:
        ///     path (string): The path to normalize.
        ///     logger (ILogger, optional): Logger for reporting issues.
        ///
        /// Returns:
        ///     string: The normalized WSL path.
        /// </summary>
        private static string NormalizeWslPath(string path, ILogger? logger = null)
        {
            // If already a WSL path, return as is
            if (path.StartsWith("/mnt/"))
            {
                return path;
            }
            try
            {
                // Handle Windows path format
                if (path.Length >= 2 && path[1] == ':')
                {
                    char driveLetter = char.ToLowerInvariant(path[0]);
                    return $"/mnt/{driveLetter}{path.Substring(2).Replace('\\', '/')}";
                }
                // Try to get the full path for relative paths
                if (!path.StartsWith("/"))
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), path)
                        .Replace('\\', '/');
                    // If we now have a drive letter, convert to /mnt/x format
                    if (path.Length >= 2 && path[1] == ':')
                    {
                        char driveLetter = char.ToLowerInvariant(path[0]);
                        return $"/mnt/{driveLetter}{path.Substring(2).Replace('\\', '/')}";
                    }
                }
                return path;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error normalizing WSL path: {Path}", path);
                return path;
            }
        }
    }
}
