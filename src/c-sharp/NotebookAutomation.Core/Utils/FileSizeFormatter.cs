using System;

namespace NotebookAutomation.Core.Utils
{
    /// <summary>
    /// Provides utility methods for formatting file sizes into human-readable strings.
    /// </summary>
    /// <remarks>
    /// This class is intended for converting byte counts to strings such as "1.23 MB" or "512.00 B".
    /// It is stateless and thread-safe.
    /// </remarks>
    public static class FileSizeFormatter
    {
        /// <summary>
        /// Converts a file size in bytes to a human-readable string format.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        /// <returns>A string representing the file size in a human-readable format, such as KB, MB, GB, etc.</returns>
        /// <remarks>
        /// The method uses appropriate precision based on the size of the file:
        /// <list type="bullet">
        /// <item><description>Two decimal places for sizes less than 10.</description></item>
        /// <item><description>One decimal place for sizes less than 100.</description></item>
        /// <item><description>Rounded values for sizes greater than or equal to 100.</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var readableSize = FileSizeFormatter.FormatFileSizeToString(1048576); // Returns "1 MB"
        /// </code>
        /// </example>
        public static string FormatFileSizeToString(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            if (suffixIndex == 0)
                return $"{size:0.00} {suffixes[suffixIndex]}";
            else if (size < 10)
                return $"{size:0.00} {suffixes[suffixIndex]}";
            else if (size < 100)
                return $"{size:0.0} {suffixes[suffixIndex]}";
            else
                return $"{Math.Round(size)} {suffixes[suffixIndex]}";
        }
    }
}
