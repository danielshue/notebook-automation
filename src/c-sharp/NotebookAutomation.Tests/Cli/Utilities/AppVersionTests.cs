// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Cli.Utilities;

/// <summary>
/// Comprehensive unit tests for the AppVersion record.
/// </summary>
[TestClass]
public class AppVersionTests
{
    /// <summary>
    /// Tests that a valid version string can be parsed correctly.
    /// </summary>
    [TestMethod]
    public void Parse_ValidVersionString_ParsesCorrectly()
    {
        // Arrange
        string versionString = "3.9.0-6.21124.20 (db94f4cc)";

        // Act
        var version = NotebookAutomation.Cli.Utilities.AppVersion.Parse(versionString);

        // Assert
        Assert.AreEqual(3, version.Major);
        Assert.AreEqual(9, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(6, version.Branch);
        Assert.AreEqual(21124, version.DateCode);
        Assert.AreEqual(20, version.Build);
        Assert.AreEqual("db94f4cc", version.Commit);
    }


    /// <summary>
    /// Tests that BuildDateUtc property correctly converts YYDDD to DateTime.
    /// </summary>
    [TestMethod]
    public void BuildDateUtc_WithValidDateCode_ReturnsCorrectDateTime()
    {
        // Arrange
        var version = new AppVersion(3, 9, 0, 6, 21124, 20, "db94f4cc");

        // Act
        var buildDate = version.BuildDateUtc;

        // Assert
        var expectedDate = new DateTime(2021, 5, 4, 0, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(expectedDate, buildDate);
    }


    /// <summary>
    /// Tests that an invalid version string throws FormatException.
    /// </summary>
    [TestMethod]
    public void Parse_InvalidVersionString_ThrowsFormatException()
    {
        // Arrange
        string invalidVersionString = "invalid version";

        // Act & Assert
        Assert.ThrowsException<FormatException>(() => AppVersion.Parse(invalidVersionString));
    }


    /// <summary>
    /// Tests that a version string without commit hash throws FormatException.
    /// </summary>
    [TestMethod]
    public void Parse_VersionStringWithoutCommitHash_ThrowsFormatException()
    {
        // Arrange
        string versionString = "3.9.0-6.21124.20";

        // Act & Assert
        Assert.ThrowsException<FormatException>(() => AppVersion.Parse(versionString));
    }


    /// <summary>
    /// Tests that FromCurrentAssembly creates a valid Version instance.
    /// </summary>
    [TestMethod]
    public void FromCurrentAssembly_ReturnsValidVersion()
    {
        // Act
        var version = AppVersion.FromCurrentAssembly();

        // Assert
        Assert.IsNotNull(version);
        Assert.IsTrue(version.Major >= 0);
        Assert.IsTrue(version.Minor >= 0);
        Assert.IsTrue(version.Patch >= 0);
        Assert.IsNotNull(version.Commit);
    }


    /// <summary>
    /// Tests that ToDisplayString returns the expected format.
    /// </summary>
    [TestMethod]
    public void ToDisplayString_ReturnsExpectedFormat()
    {
        // Arrange
        var version = new AppVersion(3, 9, 0, 6, 21124, 20, "db94f4cc");


        // Act
        string displayString = version.ToDisplayString();

        // Assert
        Assert.AreEqual("3.9.0-6.21124.20 (db94f4cc)", displayString);
    }


    /// <summary>
    /// Tests that ToSemanticVersionString returns the expected format.
    /// </summary>
    [TestMethod]
    public void ToSemanticVersionString_ReturnsExpectedFormat()
    {
        // Arrange
        var version = new AppVersion(3, 9, 0, 6, 21124, 20, "db94f4cc");

        // Act
        string semVerString = version.ToSemanticVersionString();

        // Assert
        Assert.AreEqual("3.9.0", semVerString);
    }


    /// <summary>
    /// Tests that ToInfoDictionary returns all expected keys.
    /// </summary>
    [TestMethod]
    public void ToInfoDictionary_ReturnsAllExpectedKeys()
    {
        // Arrange
        var version = new AppVersion(3, 9, 0, 6, 21124, 20, "db94f4cc");

        // Act
        var infoDictionary = version.ToInfoDictionary();

        // Assert
        var expectedKeys = new[]
        {
            "SemanticVersion", "FullVersion", "Major", "Minor", "Patch",
            "Branch", "DateCode", "Build", "Commit", "BuildDate", "BuildDateUtc"
        };

        foreach (var key in expectedKeys)
        {
            Assert.IsTrue(infoDictionary.ContainsKey(key), $"Dictionary should contain key: {key}");
            Assert.IsFalse(string.IsNullOrEmpty(infoDictionary[key]), $"Value for key '{key}' should not be null or empty");
        }
    }


    /// <summary>
    /// Tests parsing a version with different commit hash length.
    /// </summary>
    [TestMethod]
    public void Parse_DifferentCommitHashLength_ParsesCorrectly()
    {
        // Arrange
        string versionString = "1.0.0-0.25001.1 (a1b2c3d4e5f)";

        // Act
        var version = NotebookAutomation.Cli.Utilities.AppVersion.Parse(versionString);

        // Assert
        Assert.AreEqual(1, version.Major);
        Assert.AreEqual(0, version.Minor);
        Assert.AreEqual(0, version.Patch);
        Assert.AreEqual(0, version.Branch);
        Assert.AreEqual(25001, version.DateCode);
        Assert.AreEqual(1, version.Build);
        Assert.AreEqual("a1b2c3d4e5f", version.Commit);
    }


    /// <summary>
    /// Tests that Julian date conversion works for different years.
    /// </summary>
    [TestMethod]
    public void BuildDateUtc_DifferentYears_ConvertsCorrectly()
    {
        // Test for 2025 (current year)
        var version2025 = new AppVersion(1, 0, 0, 0, 25001, 0, "test");
        var buildDate2025 = version2025.BuildDateUtc;
        var expectedDate2025 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(expectedDate2025, buildDate2025);

        // Test for 2024
        var version2024 = new AppVersion(1, 0, 0, 0, 24366, 0, "test");
        var buildDate2024 = version2024.BuildDateUtc;
        var expectedDate2024 = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc); // Day 366 of 2024 (leap year)
        Assert.AreEqual(expectedDate2024, buildDate2024);
    }


    /// <summary>
    /// Tests record equality and immutability.
    /// </summary>
    [TestMethod]
    public void Version_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var version1 = new AppVersion(1, 0, 0, 0, 25001, 1, "abc123");
        var version2 = new AppVersion(1, 0, 0, 0, 25001, 1, "abc123");
        var version3 = new AppVersion(1, 0, 1, 0, 25001, 1, "abc123");

        // Act & Assert
        Assert.AreEqual(version1, version2);
        Assert.AreNotEqual(version1, version3);
        Assert.AreEqual(version1.GetHashCode(), version2.GetHashCode());
    }
}
