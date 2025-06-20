// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Reflection;
using System.Runtime.InteropServices;

namespace NotebookAutomation.Cli.Tests.Utilities;

/// <summary>
/// Comprehensive unit tests for the VersionHelper class.
/// </summary>
[TestClass]
public class VersionHelperTests
{
    /// <summary>
    /// Tests that GetVersionInfo returns a non-null dictionary with expected keys.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_ReturnsValidDictionary()
    {
        // Act
        var versionInfo = VersionHelper.GetVersionInfo();

        // Assert
        Assert.IsNotNull(versionInfo);
        Assert.IsTrue(versionInfo.Count > 0, "Version info dictionary should not be empty");

        // Verify required keys exist
        Assert.IsTrue(versionInfo.ContainsKey("Version"), "Should contain Version key");
        Assert.IsTrue(versionInfo.ContainsKey("AssemblyName"), "Should contain AssemblyName key");
        Assert.IsTrue(versionInfo.ContainsKey("RuntimeVersion"), "Should contain RuntimeVersion key");
        Assert.IsTrue(versionInfo.ContainsKey("RuntimeIdentifier"), "Should contain RuntimeIdentifier key");
        Assert.IsTrue(versionInfo.ContainsKey("OSDescription"), "Should contain OSDescription key");
        Assert.IsTrue(versionInfo.ContainsKey("OSArchitecture"), "Should contain OSArchitecture key");
        Assert.IsTrue(versionInfo.ContainsKey("ProcessArchitecture"), "Should contain ProcessArchitecture key");
        Assert.IsTrue(versionInfo.ContainsKey("FrameworkDescription"), "Should contain FrameworkDescription key");
    }

    /// <summary>
    /// Tests that version information contains expected values.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_ContainsExpectedValues()
    {
        // Act
        var versionInfo = VersionHelper.GetVersionInfo();

        // Assert
        // Version should not be "Unknown"
        Assert.IsNotNull(versionInfo["Version"]);
        Assert.AreNotEqual("Unknown", versionInfo["Version"], "Version should be populated");

        // Assembly name should be populated
        Assert.IsNotNull(versionInfo["AssemblyName"]);
        Assert.IsFalse(string.IsNullOrEmpty(versionInfo["AssemblyName"]), "AssemblyName should not be empty");

        // Runtime version should match current environment
        Assert.AreEqual(Environment.Version.ToString(), versionInfo["RuntimeVersion"]);

        // Runtime identifier should match current platform
        Assert.AreEqual(RuntimeInformation.RuntimeIdentifier, versionInfo["RuntimeIdentifier"]);

        // OS description should match current OS
        Assert.AreEqual(RuntimeInformation.OSDescription, versionInfo["OSDescription"]);

        // Architecture values should be valid enum strings
        Assert.IsTrue(Enum.IsDefined(typeof(Architecture), Enum.Parse<Architecture>(versionInfo["OSArchitecture"])));
        Assert.IsTrue(Enum.IsDefined(typeof(Architecture), Enum.Parse<Architecture>(versionInfo["ProcessArchitecture"])));

        // Framework description should match current framework
        Assert.AreEqual(RuntimeInformation.FrameworkDescription, versionInfo["FrameworkDescription"]);
    }

    /// <summary>
    /// Tests that build date is a valid DateTime.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_BuildDateIsValid()
    {
        // Act
        var versionInfo = VersionHelper.GetVersionInfo();

        // Assert
        Assert.IsTrue(versionInfo.ContainsKey("BuildDate"), "Should contain BuildDate key");

        var buildDateString = versionInfo["BuildDate"];
        Assert.IsTrue(DateTime.TryParse(buildDateString, out var buildDate),
            $"BuildDate should be a valid DateTime, got: {buildDateString}");

        // Build date should be reasonable (not too far in the past or future)
        var now = DateTime.Now;
        Assert.IsTrue(buildDate <= now, "Build date should not be in the future");
        Assert.IsTrue(buildDate >= now.AddYears(-10), "Build date should not be more than 10 years ago");
    }

    /// <summary>
    /// Tests GetLinkerTimestamp with a valid file path.
    /// </summary>
    [TestMethod]
    public void GetLinkerTimestamp_ValidFilePath_ReturnsValidDateTime()
    {
        // Arrange
        var currentAssembly = Assembly.GetExecutingAssembly();
        var assemblyPath = GetAssemblyPath(currentAssembly);

        // Act
        var timestamp = VersionHelper.GetLinkerTimestamp(assemblyPath);

        // Assert
        Assert.IsTrue(timestamp > DateTime.MinValue, "Timestamp should be a valid DateTime");
        Assert.IsTrue(timestamp <= DateTime.Now, "Timestamp should not be in the future");

        // Should be reasonable (not from 1970 unless it's a very old build)
        var unixEpoch = new DateTime(1970, 1, 1);
        Assert.IsTrue(timestamp >= unixEpoch, "Timestamp should be after Unix epoch");
    }

    /// <summary>
    /// Tests GetLinkerTimestamp with an invalid file path.
    /// </summary>
    [TestMethod]
    public void GetLinkerTimestamp_InvalidFilePath_ReturnsFallbackDateTime()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), "nonexistent_file.exe");

        // Act
        var timestamp = VersionHelper.GetLinkerTimestamp(invalidPath);

        // Assert
        Assert.IsTrue(timestamp > DateTime.MinValue, "Should return a valid DateTime even for invalid paths");

        // Should be close to current time (fallback behavior)
        var now = DateTime.Now;
        var timeDifference = Math.Abs((timestamp - now).TotalMinutes);
        Assert.IsTrue(timeDifference < 5, "Fallback timestamp should be close to current time");
    }

    /// <summary>
    /// Tests GetLinkerTimestamp with null or empty path.
    /// </summary>
    [TestMethod]
    public void GetLinkerTimestamp_NullOrEmptyPath_ReturnsFallbackDateTime()
    {
        // Act
        var timestampNull = VersionHelper.GetLinkerTimestamp(null!);
        var timestampEmpty = VersionHelper.GetLinkerTimestamp(string.Empty);

        // Assert
        Assert.IsTrue(timestampNull > DateTime.MinValue, "Should handle null path gracefully");
        Assert.IsTrue(timestampEmpty > DateTime.MinValue, "Should handle empty path gracefully");

        // Both should be close to current time
        var now = DateTime.Now;
        Assert.IsTrue(Math.Abs((timestampNull - now).TotalMinutes) < 5,
            "Null path should return time close to now");
        Assert.IsTrue(Math.Abs((timestampEmpty - now).TotalMinutes) < 5,
            "Empty path should return time close to now");
    }

    /// <summary>
    /// Tests that GetVersionInfo handles exceptions gracefully.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_HandlesExceptionsGracefully()
    {
        // Act - This should not throw even if there are issues with file access
        var versionInfo = VersionHelper.GetVersionInfo();

        // Assert
        Assert.IsNotNull(versionInfo, "Should return a dictionary even if errors occur");

        // Should contain at least basic information
        Assert.IsTrue(versionInfo.ContainsKey("Version") || versionInfo.ContainsKey("Error"),
            "Should contain either version info or error information");
    }

    /// <summary>
    /// Tests that GetVersionInfo handles single-file application scenarios.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_SingleFileApp_HandledCorrectly()
    {
        // Act
        var versionInfo = VersionHelper.GetVersionInfo();

        // Assert
        // If this is a single-file app, should still provide version info
        Assert.IsNotNull(versionInfo);

        // Check if single-file specific handling is indicated
        if (versionInfo.ContainsKey("FileVersion") &&
            versionInfo["FileVersion"].Contains("Single-file app"))
        {
            Assert.IsTrue(versionInfo.ContainsKey("ErrorDetail"),
                "Single-file app handling should include error details");
        }
    }

    /// <summary>
    /// Tests version info consistency across multiple calls.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_ConsistentAcrossMultipleCalls()
    {
        // Act
        var versionInfo1 = VersionHelper.GetVersionInfo();
        var versionInfo2 = VersionHelper.GetVersionInfo();

        // Assert
        Assert.AreEqual(versionInfo1.Count, versionInfo2.Count,
            "Version info should be consistent across calls");

        foreach (var key in versionInfo1.Keys)
        {
            if (key != "BuildDate") // BuildDate might vary slightly due to timing
            {
                Assert.AreEqual(versionInfo1[key], versionInfo2[key],
                    $"Value for key '{key}' should be consistent");
            }
        }
    }

    /// <summary>
    /// Tests that all returned strings are non-null.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_AllValuesAreNonNull()
    {
        // Act
        var versionInfo = VersionHelper.GetVersionInfo();

        // Assert
        foreach (var kvp in versionInfo)
        {
            Assert.IsNotNull(kvp.Value, $"Value for key '{kvp.Key}' should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(kvp.Value),
                $"Value for key '{kvp.Key}' should not be empty");
        }
    }

    /// <summary>
    /// Tests the private GetAssemblyPath method indirectly through GetVersionInfo.
    /// </summary>
    [TestMethod]
    public void GetVersionInfo_AssemblyPathResolution_WorksCorrectly()
    {
        // Act
        var versionInfo = VersionHelper.GetVersionInfo();

        // Assert
        // If we get valid file version info, the assembly path resolution worked
        if (versionInfo.ContainsKey("FileVersion") &&
            !versionInfo["FileVersion"].Contains("Unknown") &&
            !versionInfo["FileVersion"].Contains("Single-file app"))
        {
            Assert.IsTrue(versionInfo.ContainsKey("ProductVersion"),
                "Product version should be available when file version is resolved");
        }
    }

    /// <summary>
    /// Helper method to get assembly path similar to the private method in VersionHelper.
    /// </summary>
    private static string GetAssemblyPath(Assembly assembly)
    {
        if (Environment.ProcessPath != null)
        {
            return Environment.ProcessPath;
        }

        string baseDirectory = AppContext.BaseDirectory;
        string processName = Path.GetFileName(AppDomain.CurrentDomain.FriendlyName);
        return Path.Combine(baseDirectory, processName);
    }
}
