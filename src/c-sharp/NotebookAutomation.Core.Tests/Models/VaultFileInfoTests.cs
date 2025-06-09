// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Models;

/// <summary>
/// Unit tests for the VaultFileInfo class.
/// </summary>
[TestClass]
public class VaultFileInfoTests
{
    /// <summary>
    /// Tests that default constructor initializes all properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_Default_InitializesPropertiesCorrectly()
    {
        // Act
        var fileInfo = new VaultFileInfo();

        // Assert
        Assert.AreEqual(string.Empty, fileInfo.FileName);
        Assert.AreEqual(string.Empty, fileInfo.RelativePath);
        Assert.AreEqual(string.Empty, fileInfo.Title);
        Assert.AreEqual("note", fileInfo.ContentType);
        Assert.IsNull(fileInfo.Course);
        Assert.IsNull(fileInfo.Module);
        Assert.AreEqual(string.Empty, fileInfo.FullPath);
    }

    /// <summary>
    /// Tests that all properties can be set and retrieved correctly.
    /// </summary>
    [TestMethod]
    public void Properties_SetAndGet_WorkCorrectly()
    {
        // Arrange
        var fileInfo = new VaultFileInfo();
        const string fileName = "test-file.md";
        const string relativePath = "course/module/test-file.md";
        const string title = "Test File";
        const string contentType = "reading";
        const string course = "Test Course";
        const string module = "Test Module";
        const string fullPath = "/vault/course/module/test-file.md";

        // Act
        fileInfo.FileName = fileName;
        fileInfo.RelativePath = relativePath;
        fileInfo.Title = title;
        fileInfo.ContentType = contentType;
        fileInfo.Course = course;
        fileInfo.Module = module;
        fileInfo.FullPath = fullPath;

        // Assert
        Assert.AreEqual(fileName, fileInfo.FileName);
        Assert.AreEqual(relativePath, fileInfo.RelativePath);
        Assert.AreEqual(title, fileInfo.Title);
        Assert.AreEqual(contentType, fileInfo.ContentType);
        Assert.AreEqual(course, fileInfo.Course);
        Assert.AreEqual(module, fileInfo.Module);
        Assert.AreEqual(fullPath, fileInfo.FullPath);
    }

    /// <summary>
    /// Tests that Course and Module properties can be set to null.
    /// </summary>
    [TestMethod]
    public void NullableProperties_CanBeSetToNull()
    {
        // Arrange
        var fileInfo = new VaultFileInfo
        {
            Course = "Initial Course",
            Module = "Initial Module"
        };

        // Act
        fileInfo.Course = null;
        fileInfo.Module = null;

        // Assert
        Assert.IsNull(fileInfo.Course);
        Assert.IsNull(fileInfo.Module);
    }

    /// <summary>
    /// Tests that ContentType has correct default value.
    /// </summary>
    [TestMethod]
    public void ContentType_DefaultValue_IsNote()
    {
        // Act
        var fileInfo = new VaultFileInfo();

        // Assert
        Assert.AreEqual("note", fileInfo.ContentType);
    }

    /// <summary>
    /// Tests that all string properties can handle empty strings.
    /// </summary>
    [TestMethod]
    public void StringProperties_CanHandleEmptyStrings()
    {
        // Arrange
        var fileInfo = new VaultFileInfo();

        // Act
        fileInfo.FileName = string.Empty;
        fileInfo.RelativePath = string.Empty;
        fileInfo.Title = string.Empty;
        fileInfo.ContentType = string.Empty;
        fileInfo.FullPath = string.Empty;

        // Assert
        Assert.AreEqual(string.Empty, fileInfo.FileName);
        Assert.AreEqual(string.Empty, fileInfo.RelativePath);
        Assert.AreEqual(string.Empty, fileInfo.Title);
        Assert.AreEqual(string.Empty, fileInfo.ContentType);
        Assert.AreEqual(string.Empty, fileInfo.FullPath);
    }
}