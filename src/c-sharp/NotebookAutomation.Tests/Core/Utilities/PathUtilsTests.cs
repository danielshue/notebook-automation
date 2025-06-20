// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

namespace NotebookAutomation.Tests.Core.Utils;

/// <summary>
/// Unit tests for the <see cref="PathUtils"/> class.
/// </summary>
[TestClass]
public class PathUtilsTests
{
    [TestMethod]
    public void NormalizePath_ReturnsEmptyString_WhenInputIsNullOrWhitespace()
    {
        Assert.AreEqual(string.Empty, PathUtils.NormalizePath(null!));
        Assert.AreEqual(string.Empty, PathUtils.NormalizePath(" "));
    }

    [TestMethod]
    public void NormalizePath_ConvertsSlashesToPlatformSeparator()
    {
        string input = "folder1/folder2\\file.txt";
        string expected = $"folder1{Path.DirectorySeparatorChar}folder2{Path.DirectorySeparatorChar}file.txt";
        Assert.AreEqual(expected, PathUtils.NormalizePath(input));
    }

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

    [TestMethod]
    public void GetPathRelativeToApp_ReturnsPathRelativeToAppBase()
    {
        string rel = "testfile.txt";
        string result = PathUtils.GetPathRelativeToApp(rel);
        StringAssert.EndsWith(result, PathUtils.NormalizePath(rel));
    }

    [TestMethod]
    public void GetPathRelativeToDirectory_ReturnsCombinedNormalizedPath()
    {
        string baseDir = Path.Combine("base", "dir");
        string rel = "file.txt";
        string expected = Path.Combine(PathUtils.NormalizePath(baseDir), PathUtils.NormalizePath(rel));
        Assert.AreEqual(expected, PathUtils.GetPathRelativeToDirectory(baseDir, rel));
    }

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

    [TestMethod]
    public void MakeRelative_ReturnsRelativePath_WhenPossible()
    {
        string baseDir = Path.Combine("C:", "Projects", "Test");
        string fullPath = Path.Combine(baseDir, "folder", "file.txt");
        string rel = PathUtils.MakeRelative(baseDir, fullPath);
        Assert.AreEqual(Path.Combine("folder", "file.txt"), rel);
    }

    [TestMethod]
    public void MakeRelative_ReturnsFullPath_WhenNotRelative()
    {
        string baseDir = Path.Combine("C:", "Projects", "Test");
        string fullPath = Path.Combine("D:", "Other", "file.txt");
        Assert.AreEqual(fullPath, PathUtils.MakeRelative(baseDir, fullPath));
    }

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

    [TestMethod]
    public void GetCommonBasePath_ReturnsEmpty_WhenEmptyInput() => Assert.AreEqual(string.Empty, PathUtils.GetCommonBasePath([]));

    [TestMethod]
    public void GetCommonBasePath_ReturnsDir_WhenSinglePath()
    {
        string path = Path.Combine("C:", "Projects", "Test", "src", "a.txt");
        string expected = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
        Assert.AreEqual(expected, PathUtils.GetCommonBasePath([path]));
    }

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

    [TestMethod]
    public void ResolveInputPath_RelativePathWithoutOneDriveRoot_ReturnsOriginal()
    {
        // Arrange
        string relativePath = "Education/MBA-Resources";

        // Act
        string result = PathUtils.ResolveInputPath(relativePath, null);

        // Assert
        Assert.AreEqual(relativePath, result);
    }

    [TestMethod]
    public void ResolveInputPath_RelativePathWithEmptyOneDriveRoot_ReturnsOriginal()
    {
        // Arrange
        string relativePath = "Education/MBA-Resources";

        // Act
        string result = PathUtils.ResolveInputPath(relativePath, string.Empty);

        // Assert
        Assert.AreEqual(relativePath, result);
    }

    [TestMethod]
    public void ResolveInputPath_RelativePathWithWhitespaceOneDriveRoot_ReturnsOriginal()
    {
        // Arrange
        string relativePath = "Education/MBA-Resources";

        // Act
        string result = PathUtils.ResolveInputPath(relativePath, "   ");

        // Assert
        Assert.AreEqual(relativePath, result);
    }
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ResolveInputPath_NullInputPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        PathUtils.ResolveInputPath(null!, @"C:\OneDrive");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ResolveInputPath_EmptyInputPath_ThrowsArgumentException()
    {
        // Act & Assert
        PathUtils.ResolveInputPath(string.Empty, @"C:\OneDrive");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ResolveInputPath_WhitespaceInputPath_ThrowsArgumentException()
    {
        // Act & Assert
        PathUtils.ResolveInputPath("   ", @"C:\OneDrive");
    }
}
