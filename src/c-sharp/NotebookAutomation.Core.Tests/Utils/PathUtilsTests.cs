using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Utils
{
    /// <summary>
    /// Unit tests for the <see cref="PathUtils"/> class.
    /// </summary>
    [TestClass]
    public class PathUtilsTests
    {
        [TestMethod]
        public void NormalizePath_ReturnsEmptyString_WhenInputIsNullOrWhitespace()
        {
            Assert.AreEqual(string.Empty, PathUtils.NormalizePath(null));
            Assert.AreEqual(string.Empty, PathUtils.NormalizePath(" "));
        }

        [TestMethod]
        public void NormalizePath_ConvertsSlashesToPlatformSeparator()
        {
            var input = "folder1/folder2\\file.txt";
            var expected = $"folder1{Path.DirectorySeparatorChar}folder2{Path.DirectorySeparatorChar}file.txt";
            Assert.AreEqual(expected, PathUtils.NormalizePath(input));
        }

        [TestMethod]
        public void EnsureDirectoryExists_CreatesAndReturnsNormalizedPath()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                var result = PathUtils.EnsureDirectoryExists(tempDir);
                Assert.IsTrue(Directory.Exists(result));
                Assert.AreEqual(PathUtils.NormalizePath(tempDir), result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [TestMethod]
        public void GetPathRelativeToApp_ReturnsPathRelativeToAppBase()
        {
            var rel = "testfile.txt";
            var result = PathUtils.GetPathRelativeToApp(rel);
            StringAssert.EndsWith(result, PathUtils.NormalizePath(rel));
        }

        [TestMethod]
        public void GetPathRelativeToDirectory_ReturnsCombinedNormalizedPath()
        {
            var baseDir = Path.Combine("base", "dir");
            var rel = "file.txt";
            var expected = Path.Combine(PathUtils.NormalizePath(baseDir), PathUtils.NormalizePath(rel));
            Assert.AreEqual(expected, PathUtils.GetPathRelativeToDirectory(baseDir, rel));
        }

        [TestMethod]
        public void GenerateUniqueFilePath_ReturnsOriginalIfNotExists()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            try
            {
                Assert.AreEqual(tempFile, PathUtils.GenerateUniqueFilePath(tempFile));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void GenerateUniqueFilePath_AppendsNumberIfExists()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            File.WriteAllText(tempFile, "test");
            try
            {
                var unique = PathUtils.GenerateUniqueFilePath(tempFile);
                Assert.IsFalse(File.Exists(unique));
                StringAssert.Contains(unique, "(");
                StringAssert.EndsWith(unique, ".txt");
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [TestMethod]
        public void MakeRelative_ReturnsRelativePath_WhenPossible()
        {
            var baseDir = Path.Combine("C:", "Projects", "Test");
            var fullPath = Path.Combine(baseDir, "folder", "file.txt");
            var rel = PathUtils.MakeRelative(baseDir, fullPath);
            Assert.AreEqual(Path.Combine("folder", "file.txt"), rel);
        }

        [TestMethod]
        public void MakeRelative_ReturnsFullPath_WhenNotRelative()
        {
            var baseDir = Path.Combine("C:", "Projects", "Test");
            var fullPath = Path.Combine("D:", "Other", "file.txt");
            Assert.AreEqual(fullPath, PathUtils.MakeRelative(baseDir, fullPath));
        }

        [TestMethod]
        public void GetCommonBasePath_ReturnsCommonPrefix()
        {
            var paths = new[]
            {
                Path.Combine("C:", "Projects", "Test", "src", "a.txt"),
                Path.Combine("C:", "Projects", "Test", "src", "b.txt"),
                Path.Combine("C:", "Projects", "Test", "src", "sub", "c.txt")
            };
            var expected = Path.Combine("C:", "Projects", "Test", "src") + Path.DirectorySeparatorChar;
            Assert.AreEqual(expected, PathUtils.GetCommonBasePath(paths));
        }

        [TestMethod]
        public void GetCommonBasePath_ReturnsEmpty_WhenNoCommonality()
        {
            var paths = new[]
            {
                Path.Combine("C:", "A", "file1.txt"),
                Path.Combine("D:", "B", "file2.txt")
            };
            Assert.AreEqual(string.Empty, PathUtils.GetCommonBasePath(paths));
        }

        [TestMethod]
        public void GetCommonBasePath_ReturnsEmpty_WhenEmptyInput()
        {
            Assert.AreEqual(string.Empty, PathUtils.GetCommonBasePath(Array.Empty<string>()));
        }

        [TestMethod]
        public void GetCommonBasePath_ReturnsDir_WhenSinglePath()
        {
            var path = Path.Combine("C:", "Projects", "Test", "src", "a.txt");
            var expected = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
            Assert.AreEqual(expected, PathUtils.GetCommonBasePath(new[] { path }));
        }
    }
}
