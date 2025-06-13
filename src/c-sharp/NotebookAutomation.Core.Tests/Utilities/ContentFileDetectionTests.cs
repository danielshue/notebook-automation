// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Utils;

/// <summary>
/// Tests for content file detection functionality in the MetadataHierarchyDetector.
/// </summary>
/// <remarks>
/// These tests validate that the content file detection logic correctly identifies
/// different types of content files based on their names, paths, and extensions.
/// </remarks>
[TestClass]
public class ContentFileDetectionTests
{
    private Mock<ILogger<MetadataHierarchyDetector>> _loggerMock = null!;
    private AppConfig _testAppConfig = null!;
    private string _vaultRoot = string.Empty;

    /// <summary>
    /// Sets up test environment before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<MetadataHierarchyDetector>>();

        // Create a test vault root
        _vaultRoot = Path.Combine(Path.GetTempPath(), "TestVault_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_vaultRoot);

        // Set up app config
        _testAppConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = _vaultRoot
            }
        };
    }

    /// <summary>
    /// Cleans up test environment after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        // Clean up the test vault if it exists
        if (Directory.Exists(_vaultRoot))
        {
            Directory.Delete(_vaultRoot, true);
        }
    }

    /// <summary>
    /// Tests that file names with content keywords are correctly identified as content files.
    /// </summary>
    [TestMethod]
    public void FilesWithContentKeywords_AreDetectedAsContentFiles()
    {
        // Arrange - Create module structure
        string modPath = CreateModulePath("01_module");
        // Create various content files with keywords in their names
        var contentFileKeywords = new[]
        {
            "video", "reading", "instruction", "assignment", "quiz", "exercise",
            "activity", "discussion", "transcript", "slides", "presentation",
            "notes", "summary", "lecture", "content", "material"
        };

        var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        foreach (var keyword in contentFileKeywords)
        {
            // Create file with keyword in the name
            string filePath = Path.Combine(modPath, $"{keyword}-file.md");
            File.WriteAllText(filePath, $"{keyword} content");

            // Act
            var metadata = detector.FindHierarchyInfo(filePath);

            // Assert - Should extract just the numeric module part for content files
            Assert.IsTrue(metadata.ContainsKey("module"), $"Module should be present for '{keyword}' file");
            Assert.AreEqual("01", metadata["module"],
                $"Module for '{keyword}' file should be numeric only (01)");
        }
    }

    /// <summary>
    /// Tests that files with content-associated extensions are correctly identified as content files.
    /// </summary>
    [TestMethod]
    public void FilesWithContentExtensions_AreDetectedAsContentFiles()
    {
        // Arrange - Create module structure
        string modPath = CreateModulePath("02_module");

        // Create files with extensions associated with content
        var contentExtensions = new[] { ".mp4", ".pdf", ".pptx", ".docx" };

        var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        foreach (var ext in contentExtensions)
        {
            // Create file with content extension
            string filePath = Path.Combine(modPath, $"file{ext}");
            File.WriteAllText(filePath, $"content for {ext}");

            // Act
            var metadata = detector.FindHierarchyInfo(filePath);

            // Assert - Should extract just the numeric module part for content files
            Assert.IsTrue(metadata.ContainsKey("module"), $"Module should be present for '{ext}' file");
            Assert.AreEqual("02", metadata["module"],
                $"Module for '{ext}' file should be numeric only (02)");
        }
    }

    /// <summary>
    /// Tests that files in directories with content keywords are correctly identified as content files.
    /// </summary>
    [TestMethod]
    public void FilesInContentDirectories_AreDetectedAsContentFiles()
    {
        // Arrange - Create module and content subdirectories
        string modPath = CreateModulePath("03_module");

        // Create content directories with keywords
        var contentDirs = new[] { "videos", "readings", "resources", "content" };

        var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        foreach (var dir in contentDirs)
        {
            // Create content directory
            string contentDirPath = Path.Combine(modPath, dir);
            Directory.CreateDirectory(contentDirPath);

            // Create a file in that directory
            string filePath = Path.Combine(contentDirPath, "regular-file.md");
            File.WriteAllText(filePath, $"content in {dir} directory");

            // Act
            var metadata = detector.FindHierarchyInfo(filePath);

            // Assert - Should extract just the numeric module part for content files
            Assert.IsTrue(metadata.ContainsKey("module"), $"Module should be present for file in '{dir}' directory");
            Assert.AreEqual("03", metadata["module"],
                $"Module for file in '{dir}' directory should be numeric only (03)");
        }
    }

    /// <summary>
    /// Tests that non-content files retain the full module name.
    /// </summary>
    [TestMethod]
    public void NonContentFiles_RetainFullModuleName()
    {
        // Arrange - Create module structure
        string modPath = CreateModulePath("04_advanced-topics");

        // Create non-content files
        var nonContentFiles = new[]
        {
            "module-index.md",
            "README.md",
            "index.md",
            "overview.md",  // Even though "overview" could be considered a content keyword,
                           // we'll test whether other signals can override it
            "template.md"
        };

        var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        foreach (var file in nonContentFiles)
        {
            // Create non-content file
            string filePath = Path.Combine(modPath, file);
            // For files that might be considered content, add text that makes them clearly non-content
            string content = file.Contains("overview") ? "# Module Overview\n\nThis is the module index." : "non-content file";
            File.WriteAllText(filePath, content);

            // Act
            var metadata = detector.FindHierarchyInfo(filePath);

            // Assert - Should retain the full module name for non-content files
            Assert.IsTrue(metadata.ContainsKey("module"), $"Module should be present for '{file}'");
            Assert.AreEqual("04_advanced-topics", metadata["module"],
                $"Module for '{file}' should be the full module name");
        }
    }

    // Helper method to create a standard module path structure
    private string CreateModulePath(string moduleName)
    {
        string programDir = Path.Combine(_vaultRoot, "TestProgram");
        string courseDir = Path.Combine(programDir, "TestCourse");
        string classDir = Path.Combine(courseDir, "TestClass");
        string moduleDir = Path.Combine(classDir, moduleName);
        Directory.CreateDirectory(moduleDir);
        return moduleDir;
    }
}
