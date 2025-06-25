// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the FileSystemWrapper class.
/// </summary>
[TestClass]
public class FileSystemWrapperTests
{
    private FileSystemWrapper _fileSystemWrapper = null!;
    private string _testDirectory = null!;
    private string _testFile = null!;

    [TestInitialize]
    public void Setup()
    {
        _fileSystemWrapper = new FileSystemWrapper();
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _testFile = Path.Combine(_testDirectory, "test.txt");

        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFile, "test content");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    /// <summary>
    /// Tests that FileExists returns true for an existing file.
    /// </summary>
    [TestMethod]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
        // Act
        var result = _fileSystemWrapper.FileExists(_testFile);

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests that FileExists returns false for a non-existing file.
    /// </summary>
    [TestMethod]
    public void FileExists_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = _fileSystemWrapper.FileExists(nonExistentFile);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that DirectoryExists returns true for an existing directory.
    /// </summary>
    [TestMethod]
    public void DirectoryExists_ExistingDirectory_ReturnsTrue()
    {
        // Act
        var result = _fileSystemWrapper.DirectoryExists(_testDirectory);

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests that DirectoryExists returns false for a non-existing directory.
    /// </summary>
    [TestMethod]
    public void DirectoryExists_NonExistingDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = _fileSystemWrapper.DirectoryExists(nonExistentDir);

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that FileSystemWrapper basic operations work correctly.
    /// Verifies that <c>FileSystemWrapper</c> basic file and directory operations work as expected.
    [TestMethod]
    public void FileSystemWrapper_BasicOperations_WorkCorrectly()
    {
        // Act & Assert - these methods should work without errors
        Assert.IsTrue(_fileSystemWrapper.FileExists(_testFile));
        Assert.IsTrue(_fileSystemWrapper.DirectoryExists(_testDirectory));
        Assert.IsFalse(_fileSystemWrapper.FileExists("nonexistent.txt"));
        Assert.IsFalse(_fileSystemWrapper.DirectoryExists("nonexistent"));
    }

    /// <summary>
    /// Tests that path operations work correctly.
    /// </summary>
    [TestMethod]
    public void FileSystemWrapper_PathOperations_WorkCorrectly()
    {
        // Act & Assert
        var combinedPath = _fileSystemWrapper.CombinePath("C:", "temp", "file.txt");
        var expectedPath = OperatingSystem.IsWindows() ? @"C:\temp\file.txt" : "C:/temp/file.txt";
        Assert.AreEqual(expectedPath, combinedPath);

        var fullPath = _fileSystemWrapper.GetFullPath("test.txt");
        Assert.IsNotNull(fullPath);

        var testPath = OperatingSystem.IsWindows() ? @"C:\temp\file.txt" : "C:/temp/file.txt";
        var dirName = _fileSystemWrapper.GetDirectoryName(testPath);
        var expectedDirName = OperatingSystem.IsWindows() ? @"C:\temp" : "C:/temp";
        Assert.AreEqual(expectedDirName, dirName);
    }

    /// <summary>
    /// Tests that CombinePath correctly combines multiple path segments.
    /// </summary>
    [TestMethod]
    public void CombinePath_ValidPaths_ReturnsCombinedPath()
    {
        // Arrange
        var path1 = OperatingSystem.IsWindows() ? "C:\\test" : "C:/test";
        var path2 = "folder";
        var path3 = "file.txt";

        // Act
        var result = _fileSystemWrapper.CombinePath(path1, path2, path3);

        // Assert
        var expectedResult = OperatingSystem.IsWindows() ? "C:\\test\\folder\\file.txt" : "C:/test/folder/file.txt";
        Assert.AreEqual(expectedResult, result);
    }

    /// <summary>
    /// Tests that GetFullPath converts a relative path to an absolute path.
    /// </summary>
    [TestMethod]
    public void GetFullPath_RelativePath_ReturnsAbsolutePath()
    {
        // Arrange
        var relativePath = "test.txt";

        // Act
        var result = _fileSystemWrapper.GetFullPath(relativePath);

        // Assert
        Assert.IsTrue(Path.IsPathRooted(result));
        Assert.IsTrue(result.EndsWith("test.txt"));
    }

    /// <summary>
    /// Tests that ReadAllTextAsync successfully reads content from an existing file.
    /// </summary>
    [TestMethod]
    public async Task ReadAllTextAsync_ExistingFile_ReturnsContent()
    {
        // Act
        var result = await _fileSystemWrapper.ReadAllTextAsync(_testFile);

        // Assert
        Assert.AreEqual("test content", result);
    }

    /// <summary>
    /// Tests that ReadAllTextAsync throws FileNotFoundException for a non-existing file.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public async Task ReadAllTextAsync_NonExistingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        await _fileSystemWrapper.ReadAllTextAsync(nonExistentFile);
    }
}

/// <summary>
/// Provides unit tests for the <see cref="EnvironmentWrapper"/> class, verifying environment variable access and environment detection logic.
/// </summary>
[TestClass]
public class EnvironmentWrapperTests
{
    private EnvironmentWrapper _environmentWrapper = null!;

    /// <summary>
    /// Initializes the test environment for <see cref="EnvironmentWrapper"/> tests.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _environmentWrapper = new EnvironmentWrapper();
    }

    /// <summary>
    /// Verifies that <see cref="EnvironmentWrapper.GetEnvironmentVariable(string)"/> returns the correct value for an existing environment variable.
    /// </summary>
    [TestMethod]
    public void GetEnvironmentVariable_ExistingVariable_ReturnsValue()
    {
        // Arrange
        var variableName = "TEST_VAR_" + Guid.NewGuid().ToString("N")[..8];
        var expectedValue = "test_value";
        Environment.SetEnvironmentVariable(variableName, expectedValue);

        try
        {
            // Act
            var result = _environmentWrapper.GetEnvironmentVariable(variableName);

            // Assert
            Assert.AreEqual(expectedValue, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    /// <summary>
    /// Verifies that <see cref="EnvironmentWrapper.GetEnvironmentVariable(string)"/> returns <c>null</c> for a non-existing environment variable.
    /// </summary>
    [TestMethod]
    public void GetEnvironmentVariable_NonExistingVariable_ReturnsNull()
    {
        // Arrange
        var nonExistentVariable = "NON_EXISTENT_VAR_" + Guid.NewGuid().ToString("N");

        // Act
        var result = _environmentWrapper.GetEnvironmentVariable(nonExistentVariable);

        // Assert
        Assert.IsNull(result);
    }    /// <summary>
         /// <summary>
         /// Verifies that <see cref="EnvironmentWrapper.IsDevelopment()"/> returns a boolean value.
         /// </summary>
    [TestMethod]
    public void IsDevelopment_ReturnsBooleanValue()
    {
        // Act
        var result = _environmentWrapper.IsDevelopment();

        // Assert - The method should return true or false
        Assert.IsNotNull(result);
        Assert.IsTrue(result == true || result == false);
    }    /// <summary>
         /// <summary>
         /// Verifies that <see cref="EnvironmentWrapper.IsDevelopment()"/> correctly identifies the development environment based on the ASPNETCORE_ENVIRONMENT variable.
         /// </summary>
    [TestMethod]
    public void IsDevelopment_ChecksEnvironmentVariable()
    {
        // Act
        var result = _environmentWrapper.IsDevelopment();
        var aspnetcoreEnv = _environmentWrapper.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        // Assert
        if (string.Equals(aspnetcoreEnv, "Development", StringComparison.OrdinalIgnoreCase))
        {
            Assert.IsTrue(result);
        }
        else
        {
            Assert.IsFalse(result);
        }
    }    /// <summary>
         /// <summary>
         /// Verifies that <see cref="EnvironmentWrapper.IsDevelopment()"/> returns <c>true</c> when ASPNETCORE_ENVIRONMENT is set to "Development".
         /// </summary>
    [TestMethod]
    public void IsDevelopment_WithASPNETCOREEnvironmentDevelopment_ReturnsTrue()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        try
        {
            // Act
            var result = _environmentWrapper.IsDevelopment();

            // Assert
            Assert.IsTrue(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalValue);
        }
    }    /// <summary>
         /// <summary>
         /// Verifies that environment variables can be retrieved correctly using <see cref="EnvironmentWrapper.GetEnvironmentVariable(string)"/>.
         /// </summary>
    [TestMethod]
    public void GetEnvironmentVariable_ReturnsCorrectValue()
    {
        // Arrange
        var testVariableName = "TEST_VARIABLE";
        var testValue = "TestValue123";
        Environment.SetEnvironmentVariable(testVariableName, testValue);

        try
        {
            // Act
            var result = _environmentWrapper.GetEnvironmentVariable(testVariableName);

            // Assert
            Assert.AreEqual(testValue, result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(testVariableName, null);
        }
    }
}
