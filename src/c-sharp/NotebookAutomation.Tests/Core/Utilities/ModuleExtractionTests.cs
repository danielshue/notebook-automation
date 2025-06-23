// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Utils;

/// <summary>
/// Tests for numeric module prefix extraction functionality in the MetadataHierarchyDetector.
/// </summary>
/// <remarks>
/// These tests validate that the module extraction logic correctly processes module folder names
/// and extracts just the numeric part for content files.
/// </remarks>
[TestClass]
public class ModuleExtractionTests
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
    /// Tests that content files with numeric module prefixes are properly processed.
    /// </summary>
    [TestMethod]
    public void ContentFile_WithNumericModulePrefix_ExtractsNumberOnly()
    {
        // Arrange - Create test folder structure with numeric module prefixes
        string programDir = Path.Combine(_vaultRoot, "TestProgram");
        string courseDir = Path.Combine(programDir, "TestCourse");
        string classDir = Path.Combine(courseDir, "TestClass");
        string moduleDir = Path.Combine(classDir, "05_operations-resilience");
        Directory.CreateDirectory(moduleDir);

        // Create test content files
        string videoFilePath = Path.Combine(moduleDir, "video-lecture.mp4");
        File.WriteAllText(videoFilePath, "video content");

        string readingFilePath = Path.Combine(moduleDir, "reading-assignment.pdf");
        File.WriteAllText(readingFilePath, "reading content");

        // Create a detector instance
        var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        // Act
        var videoMetadata = detector.FindHierarchyInfo(videoFilePath);
        var readingMetadata = detector.FindHierarchyInfo(readingFilePath);

        // Assert
        Assert.IsTrue(videoMetadata.ContainsKey("module"), "Module should be present in video metadata");
        Assert.AreEqual("05", videoMetadata["module"], "Module for video should be numeric only (05)");

        Assert.IsTrue(readingMetadata.ContainsKey("module"), "Module should be present in reading metadata");
        Assert.AreEqual("05", readingMetadata["module"], "Module for reading should be numeric only (05)");
    }

    /// <summary>
    /// Tests that non-content files retain the full module name.
    /// </summary>
    [TestMethod]
    public void NonContentFile_WithNumericModulePrefix_RetainsFullModuleName()
    {
        // Arrange - Create test folder structure with numeric module prefixes
        string programDir = Path.Combine(_vaultRoot, "TestProgram");
        string courseDir = Path.Combine(programDir, "TestCourse");
        string classDir = Path.Combine(courseDir, "TestClass");
        string moduleDir = Path.Combine(classDir, "05_operations-resilience");
        Directory.CreateDirectory(moduleDir);

        // Create test non-content file (index, notes, etc.)
        string indexFilePath = Path.Combine(moduleDir, "module-index.md");
        File.WriteAllText(indexFilePath, "index content");

        // Create a detector instance
        var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        // Act
        var indexMetadata = detector.FindHierarchyInfo(indexFilePath);

        // Assert
        Assert.IsTrue(indexMetadata.ContainsKey("module"), "Module should be present in index metadata");
        Assert.AreEqual("05_operations-resilience", indexMetadata["module"], "Module for index should be the full name");
    }

    /// <summary>
    /// Tests that various module naming patterns are correctly processed.
    /// </summary>
    [TestMethod]
    public void ContentFile_WithVariousModuleNamingPatterns_ExtractsCorrectly()
    {
        // Arrange - Create test folder structure with various module naming patterns
        string programDir = Path.Combine(_vaultRoot, "TestProgram");
        string courseDir = Path.Combine(programDir, "TestCourse");
        string classDir = Path.Combine(courseDir, "TestClass");
        Directory.CreateDirectory(classDir);

        // Test different module naming patterns
        var modulePatterns = new Dictionary<string, string>
        {
            { "01_introduction", "01" },
            { "02-advanced-topics", "02" },
            { "module03_basics", "03" },
            { "week04-summary", "04" },
            { "5_conclusion", "5" },
            { "06", "06" }
        };

        var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        foreach (var pattern in modulePatterns)
        {
            // Create module directory
            string moduleDir = Path.Combine(classDir, pattern.Key);
            Directory.CreateDirectory(moduleDir);

            // Create content file
            string videoFilePath = Path.Combine(moduleDir, "lecture-video.mp4");
            File.WriteAllText(videoFilePath, "video content");

            // Act
            var metadata = detector.FindHierarchyInfo(videoFilePath);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("module"), $"Module should be present for pattern '{pattern.Key}'");
            Assert.AreEqual(pattern.Value, metadata["module"],
                $"Module for pattern '{pattern.Key}' should extract to '{pattern.Value}'");
        }
    }
}
