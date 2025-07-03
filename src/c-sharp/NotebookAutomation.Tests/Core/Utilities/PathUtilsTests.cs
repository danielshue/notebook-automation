// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

namespace NotebookAutomation.Tests.Core.Utils;

/// <summary>
/// Provides unit tests for the <see cref="PathUtils"/> utility class.
/// </summary>
/// <remarks>
/// <para>
/// This test class covers all major path manipulation and resolution methods in <see cref="PathUtils"/>,
/// including normalization, directory creation, relative/absolute path handling, and unique file path generation.
/// </para>
/// <para>
/// Tests are designed to validate correct behavior on both Windows and Unix-like platforms, including:
/// <list type="bullet">
///   <item><description>Slash normalization and platform-specific separators</description></item>
///   <item><description>Combining and resolving paths with and without OneDrive roots and basepaths</description></item>
///   <item><description>Edge cases for null, empty, and whitespace input</description></item>
///   <item><description>Exception handling for invalid arguments</description></item>
/// </list>
/// </para>
/// <para>
/// The tests ensure robust, cross-platform path logic for the Notebook Automation CLI and related tools.
/// </para>
/// </remarks>
[TestClass]
public class PathUtilsTests
{
    /// <summary>
    /// Verifies that NormalizePath returns an empty string for null or whitespace input.
    /// </summary>
    [TestMethod]
    public void NormalizePath_ReturnsEmptyString_WhenInputIsNullOrWhitespace()
    {
        Assert.AreEqual(string.Empty, PathUtils.NormalizePath(null!));
        Assert.AreEqual(string.Empty, PathUtils.NormalizePath(" "));
    }

    /// <summary>
    /// Verifies that NormalizePath converts all slashes to the platform-specific separator.
    /// </summary>
    [TestMethod]
    public void NormalizePath_ConvertsSlashesToPlatformSeparator()
    {
        string input = "folder1/folder2\\file.txt";
        string expected = $"folder1{Path.DirectorySeparatorChar}folder2{Path.DirectorySeparatorChar}file.txt";
        Assert.AreEqual(expected, PathUtils.NormalizePath(input));
    }

    /// <summary>
    /// Ensures EnsureDirectoryExists creates the directory if missing and returns the normalized path.
    /// </summary>
    [TestMethod]
    public void EnsureDirectoryExists_CreatesAndReturnsNormalizedPath()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            string result = PathUtils.EnsureDirectoryExists(tempDir);
            Assert.IsTrue(Directory.Exists(result));
            Assert.AreEqual(PathUtils.NormalizePath(tempDir), result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Verifies that GetPathRelativeToApp returns a path relative to the application base directory.
    /// </summary>
    [TestMethod]
    public void GetPathRelativeToApp_ReturnsPathRelativeToAppBase()
    {
        string rel = "testfile.txt";
        string result = PathUtils.GetPathRelativeToApp(rel);
        StringAssert.EndsWith(result, PathUtils.NormalizePath(rel));
    }

    /// <summary>
    /// Verifies that GetPathRelativeToDirectory combines and normalizes base and relative paths.
    /// </summary>
    [TestMethod]
    public void GetPathRelativeToDirectory_ReturnsCombinedNormalizedPath()
    {
        string baseDir = Path.Combine("base", "dir");
        string rel = "file.txt";
        string expected = Path.Combine(PathUtils.NormalizePath(baseDir), PathUtils.NormalizePath(rel));
        Assert.AreEqual(expected, PathUtils.GetPathRelativeToDirectory(baseDir, rel));
    }

    /// <summary>
    /// Verifies that GenerateUniqueFilePath returns the original path if the file does not exist.
    /// </summary>
    [TestMethod]
    public void GenerateUniqueFilePath_ReturnsOriginalIfNotExists()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            Assert.AreEqual(tempFile, PathUtils.GenerateUniqueFilePath(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Verifies that GenerateUniqueFilePath appends a number if the file already exists.
    /// </summary>
    [TestMethod]
    public void GenerateUniqueFilePath_AppendsNumberIfExists()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        File.WriteAllText(tempFile, "test");
        try
        {
            string unique = PathUtils.GenerateUniqueFilePath(tempFile);
            Assert.IsFalse(File.Exists(unique));
            StringAssert.Contains(unique, "(");
            StringAssert.EndsWith(unique, ".txt");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Verifies that MakeRelative returns the correct relative path when possible.
    /// </summary>
    [TestMethod]
    public void MakeRelative_ReturnsRelativePath_WhenPossible()
    {
        string baseDir = Path.Combine("C:", "Projects", "Test");
        string fullPath = Path.Combine(baseDir, "folder", "file.txt");
        string rel = PathUtils.MakeRelative(baseDir, fullPath);
        Assert.AreEqual(Path.Combine("folder", "file.txt"), rel);
    }

    /// <summary>
    /// Verifies that MakeRelative returns the full path if it cannot be made relative.
    /// </summary>
    [TestMethod]
    public void MakeRelative_ReturnsFullPath_WhenNotRelative()
    {
        string baseDir = Path.Combine("C:", "Projects", "Test");
        string fullPath = Path.Combine("D:", "Other", "file.txt");
        Assert.AreEqual(fullPath, PathUtils.MakeRelative(baseDir, fullPath));
    }

    /// <summary>
    /// Verifies that GetCommonBasePath returns the common directory prefix for multiple paths.
    /// </summary>
    [TestMethod]
    public void GetCommonBasePath_ReturnsCommonPrefix()
    {
        string[] paths =
        [
            Path.Combine("C:", "Projects", "Test", "src", "a.txt"),
            Path.Combine("C:", "Projects", "Test", "src", "b.txt"),
            Path.Combine("C:", "Projects", "Test", "src", "sub", "c.txt")
        ];
        string expected = Path.Combine("C:", "Projects", "Test", "src") + Path.DirectorySeparatorChar;
        Assert.AreEqual(expected, PathUtils.GetCommonBasePath(paths));
    }

    /// <summary>
    /// Verifies that GetCommonBasePath returns an empty string when there is no commonality.
    /// </summary>
    [TestMethod]
    public void GetCommonBasePath_ReturnsEmpty_WhenNoCommonality()
    {
        string[] paths =
        [
            Path.Combine("C:", "A", "file1.txt"),
            Path.Combine("D:", "B", "file2.txt")
        ];
        Assert.AreEqual(string.Empty, PathUtils.GetCommonBasePath(paths));
    }

    /// <summary>
    /// Verifies that GetCommonBasePath returns an empty string for empty input.
    /// </summary>
    [TestMethod]
    public void GetCommonBasePath_ReturnsEmpty_WhenEmptyInput() => Assert.AreEqual(string.Empty, PathUtils.GetCommonBasePath([]));

    /// <summary>
    /// Verifies that GetCommonBasePath returns the directory for a single path input.
    /// </summary>
    [TestMethod]
    public void GetCommonBasePath_ReturnsDir_WhenSinglePath()
    {
        string path = Path.Combine("C:", "Projects", "Test", "src", "a.txt");
        string expected = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
        Assert.AreEqual(expected, PathUtils.GetCommonBasePath([path]));
    }

    /// <summary>
    /// Verifies that ResolveInputPath returns the absolute path unchanged if already absolute.
    /// </summary>
    [TestMethod]
    public void ResolveInputPath_AbsolutePath_ReturnsUnchanged()
    {
        // Arrange
        string absolutePath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            absolutePath = @"C:\Users\test\OneDrive\folder";
        }
        else
        {
            absolutePath = "/home/test/OneDrive/folder";
        }
        string oneDriveRoot = @"C:\Users\test\OneDrive";

        // Act
        string result = PathUtils.ResolveInputPath(absolutePath, oneDriveRoot);

        // Assert
        Assert.AreEqual(absolutePath, result);
    }

    /// <summary>
    /// Verifies that ResolveInputPath combines OneDrive root and relative path correctly.
    /// </summary>
    [TestMethod]
    public void ResolveInputPath_RelativePathWithOneDriveRoot_ReturnsCombinedPath()
    {
        // Arrange
        string relativePath = "Education/MBA-Resources";
        string oneDriveRoot;
        string expected;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            oneDriveRoot = @"C:\Users\test\OneDrive";
            expected = @"C:\Users\test\OneDrive\Education\MBA-Resources";
        }
        else
        {
            oneDriveRoot = "/home/test/OneDrive";
            expected = "/home/test/OneDrive/Education/MBA-Resources";
        }

        // Act
        string result = PathUtils.ResolveInputPath(relativePath, oneDriveRoot);

        // Assert
        Assert.AreEqual(PathUtils.NormalizePath(expected), result);
    }

    /// <summary>
    /// Verifies that ResolveInputPath returns the normalized input if OneDrive root is null.
    /// </summary>
    [TestMethod]
    public void ResolveInputPath_RelativePathWithoutOneDriveRoot_ReturnsOriginal()
    {
        // Arrange
        string relativePath = "Education/MBA-Resources";

        // Act
        string result = PathUtils.ResolveInputPath(relativePath, null);

        // Assert
        Assert.AreEqual(PathUtils.NormalizePath(relativePath), result);
    }

    /// <summary>
    /// Verifies that ResolveInputPath returns the normalized input if OneDrive root is empty.
    /// </summary>
    [TestMethod]
    public void ResolveInputPath_RelativePathWithEmptyOneDriveRoot_ReturnsOriginal()
    {
        // Arrange
        string relativePath = "Education/MBA-Resources";

        // Act
        string result = PathUtils.ResolveInputPath(relativePath, string.Empty);

        // Assert
        Assert.AreEqual(PathUtils.NormalizePath(relativePath), result);
    }

    /// <summary>
    /// Verifies that ResolveInputPath returns the normalized input if OneDrive root is whitespace.
    /// </summary>
    [TestMethod]
    public void ResolveInputPath_RelativePathWithWhitespaceOneDriveRoot_ReturnsOriginal()
    {
        // Arrange
        string relativePath = "Education/MBA-Resources";

        // Act
        string result = PathUtils.ResolveInputPath(relativePath, "   ");

        // Assert
        Assert.AreEqual(PathUtils.NormalizePath(relativePath), result);
    }

    /// <summary>
    /// Verifies that ResolveInputPath throws ArgumentNullException for null inputPath.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ResolveInputPath_NullInputPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        PathUtils.ResolveInputPath(null!, @"C:\OneDrive");
    }

    /// <summary>
    /// Verifies that ResolveInputPath throws ArgumentException for empty inputPath.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ResolveInputPath_EmptyInputPath_ThrowsArgumentException()
    {
        // Act & Assert
        PathUtils.ResolveInputPath(string.Empty, @"C:\OneDrive");
    }

    /// <summary>
    /// Verifies that ResolveInputPath throws ArgumentException for whitespace inputPath.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ResolveInputPath_WhitespaceInputPath_ThrowsArgumentException()
    {
        // Act & Assert
        PathUtils.ResolveInputPath("   ", @"C:\OneDrive");
    }

    /// <summary>
    /// Comprehensive cross-platform test for the 3-argument ResolveInputPath method, covering absolute, relative, and basepath scenarios for both Windows and Unix.
    /// </summary>
    [TestMethod]
    public void ResolveInputPath_ThreeArg_CrossPlatformBehavior()
    {
        string inputRelative = "Course/Module/Lesson.mp4";
        string oneDriveRoot;
        string resourcesBasepath = "MBA-Resources/Videos";
        string expected;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            oneDriveRoot = @"C:\\Users\\test\\OneDrive";
        }
        else
        {
            oneDriveRoot = "/home/test/OneDrive";
        }

        // Relative path with both root and basepath
        expected = Path.Combine(oneDriveRoot, "MBA-Resources", "Videos", "Course", "Module", "Lesson.mp4");
        string result = PathUtils.ResolveInputPath(inputRelative, oneDriveRoot, resourcesBasepath);
        Assert.AreEqual(PathUtils.NormalizePath(expected), result);

        // Absolute path should return normalized absolute path
        string absInput = Path.Combine(oneDriveRoot, "MBA-Resources", "Videos", "Course", "Module", "Lesson.mp4");
        string absResult = PathUtils.ResolveInputPath(absInput, oneDriveRoot, resourcesBasepath);
        Assert.AreEqual(PathUtils.NormalizePath(absInput), absResult);

        // Relative path with only root
        string expectedRootOnly = Path.Combine(oneDriveRoot, "Course", "Module", "Lesson.mp4");
        string resultRootOnly = PathUtils.ResolveInputPath(inputRelative, oneDriveRoot, null);
        Assert.AreEqual(PathUtils.NormalizePath(expectedRootOnly), resultRootOnly);

        // Relative path with only basepath (should ignore basepath if root is null)
        string resultBasepathOnly = PathUtils.ResolveInputPath(inputRelative, null, resourcesBasepath);
        Assert.AreEqual(PathUtils.NormalizePath(inputRelative), resultBasepathOnly);
    }
}
