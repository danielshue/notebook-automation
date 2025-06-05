using System.Reflection;
using System.Runtime.InteropServices;

namespace NotebookAutomation.Cli.Utilities;
/// <summary>
/// Provides version-related utility functions for the application.
/// </summary>
public static class VersionHelper
{
    /// <summary>
    /// Gets detailed version information about the application.
    /// </summary>
    /// <returns>A dictionary containing version information.</returns>
    public static Dictionary<string, string> GetVersionInfo()
    {
        var versionInfo = new Dictionary<string, string>();

        try
        {
            // Assembly version information
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();

            versionInfo["Version"] = assemblyName.Version?.ToString() ?? "Unknown";
            versionInfo["AssemblyName"] = assemblyName.Name ?? "Unknown";

            try
            {
                // File version and product version - handle single-file apps
                string assemblyPath = GetAssemblyPath(assembly);
                var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assemblyPath);
                versionInfo["FileVersion"] = fileVersionInfo.FileVersion ?? "Unknown";
                versionInfo["ProductVersion"] = fileVersionInfo.ProductVersion ?? "Unknown";

                // Build information
                versionInfo["BuildDate"] = GetBuildDate(assemblyPath).ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                versionInfo["FileVersion"] = GetInformationalVersion() ?? "Unknown (Single-file app)";
                versionInfo["ProductVersion"] = GetInformationalVersion() ?? "Unknown (Single-file app)";
                versionInfo["BuildDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                versionInfo["ErrorDetail"] = ex.Message;
            }

            // Runtime information
            versionInfo["RuntimeVersion"] = Environment.Version.ToString();
            versionInfo["RuntimeIdentifier"] = RuntimeInformation.RuntimeIdentifier;
            versionInfo["OSDescription"] = RuntimeInformation.OSDescription;
            versionInfo["OSArchitecture"] = RuntimeInformation.OSArchitecture.ToString();
            versionInfo["ProcessArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString();
            versionInfo["FrameworkDescription"] = RuntimeInformation.FrameworkDescription;
        }
        catch (Exception ex)
        {
            versionInfo["Error"] = $"Error retrieving version info: {ex.Message}";
        }

        return versionInfo;
    }

    /// <summary>
    /// Gets the informational version from assembly attributes.
    /// </summary>
    private static string GetInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attr?.InformationalVersion ?? "Unknown";
    }        /// <summary>
             /// Gets the assembly path, handling single-file applications correctly.
             /// </summary>
             /// <param name="assembly">The assembly to get the path for.</param>
             /// <returns>The path to the assembly or the application executable.</returns>
    private static string GetAssemblyPath(Assembly assembly)
    {
        // With single-file apps, Assembly.Location will be empty
        // Use Environment.ProcessPath as the most reliable way to get the executable path
        if (Environment.ProcessPath != null)
        {
            return Environment.ProcessPath;
        }

        // Fallback option 1: Use AppContext.BaseDirectory and process name
        string baseDirectory = AppContext.BaseDirectory;
        string processName = Path.GetFileName(AppDomain.CurrentDomain.FriendlyName);

        return Path.Combine(baseDirectory, processName);
    }

    /// <summary>
    /// Gets the build date of the assembly.
    /// </summary>
    /// <param name="filePath">The file path of the assembly.</param>
    /// <returns>The build timestamp as a DateTime.</returns>
    private static DateTime GetBuildDate(string filePath)
    {
        try
        {
            // For single-file applications or when path is not accessible
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return DateTime.Now;
            }

            return GetLinkerTimestamp(filePath);
        }
        catch
        {
            return DateTime.Now;
        }
    }

    /// <summary>
    /// Gets the build timestamp from the PE header of an assembly.
    /// </summary>
    /// <param name="filePath">The file path of the assembly.</param>
    /// <returns>The build timestamp as a DateTime.</returns>
    public static DateTime GetLinkerTimestamp(string filePath)
    {
        try
        {
            const int peHeaderOffset = 60;
            const int linkerTimestampOffset = 8; // Read the linker timestamp from the PE header
            byte[] buffer = new byte[2048];
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                file.ReadExactly(buffer, 0, Math.Min(buffer.Length, (int)file.Length));
            }

            // Get the PE header location from the DOS header
            int peHeaderLocation = BitConverter.ToInt32(buffer, peHeaderOffset);

            // Get the timestamp from the PE header
            int timestampOffset = peHeaderLocation + linkerTimestampOffset;
            int timestamp = BitConverter.ToInt32(buffer, timestampOffset);

            // Convert the timestamp to a DateTime (PE timestamp is seconds since Jan 1, 1970)
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime buildDate = origin.AddSeconds(timestamp);

            return buildDate.ToLocalTime();
        }
        catch
        {
            try
            {
                // If anything goes wrong, return the file creation time as a fallback
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    return File.GetCreationTime(filePath);
                }
            }
            catch
            {
                // Ignore any exceptions from the fallback
            }

            // Ultimate fallback
            return DateTime.Now;
        }
    }
}
