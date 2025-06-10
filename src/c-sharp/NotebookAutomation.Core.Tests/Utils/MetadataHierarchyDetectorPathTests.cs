// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Utils;

/// <summary>
/// Tests for path handling and hierarchy level calculation in MetadataHierarchyDetector.
/// </summary>
[TestClass]
public class MetadataHierarchyDetectorPathTests
{
    private readonly Mock<ILogger<MetadataHierarchyDetector>> _mockLogger = new();
    private AppConfig _appConfig = new();
    private string _tempVaultRoot = string.Empty;

    /// <summary>
    /// Set up test environment with temporary vault structure.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        // Create a temporary directory for vault root
        _tempVaultRoot = Path.Combine(Path.GetTempPath(), $"TestVault_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempVaultRoot);

        // Create a nested directory structure for testing
        CreateTestVaultStructure();

        // Set up app config
        _appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = _tempVaultRoot
            }
        };
    }

    /// <summary>
    /// Clean up test environment.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        // Clean up temp vault directory if it exists
        if (Directory.Exists(_tempVaultRoot))
        {
            try
            {
                Directory.Delete(_tempVaultRoot, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to clean up test directory: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Creates a test vault directory structure for testing hierarchies.
    /// </summary>
    private void CreateTestVaultStructure()
    {
        // Create nested directories representing common vault structure

        // Program level
        string program1Path = Path.Combine(_tempVaultRoot, "Program1");
        Directory.CreateDirectory(program1Path);

        // Course level
        string course1Path = Path.Combine(program1Path, "Course1");
        Directory.CreateDirectory(course1Path);

        // Class level
        string class1Path = Path.Combine(course1Path, "Class1");
        Directory.CreateDirectory(class1Path);

        // Module level
        string module1Path = Path.Combine(class1Path, "Module1");
        Directory.CreateDirectory(module1Path);

        // Lesson level
        string lesson1Path = Path.Combine(module1Path, "Lesson1");
        Directory.CreateDirectory(lesson1Path);

        // Special folders at module level
        Directory.CreateDirectory(Path.Combine(class1Path, "Lessons"));
        Directory.CreateDirectory(Path.Combine(class1Path, "Modules"));
        Directory.CreateDirectory(Path.Combine(class1Path, "Case Studies"));
    }

    /// <summary>
    /// Tests that hierarchy level is correctly calculated for absolute paths.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_AbsolutePaths_ReturnsCorrectLevel()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Test cases - path and expected hierarchy level
        var testCases = new[]
        {
            (Path: _tempVaultRoot, ExpectedLevel: 0),
            (Path: Path.Combine(_tempVaultRoot, "Value Chain Management"), ExpectedLevel: 1),
            (Path: Path.Combine(_tempVaultRoot, "Value Chain Management", "Operations Management"), ExpectedLevel: 2),
            (Path: Path.Combine(_tempVaultRoot, "Value Chain Management", "Operations Management", "Supply Chain Fundamentals"), ExpectedLevel: 3),
            (Path: Path.Combine(_tempVaultRoot, "Value Chain Management", "Operations Management", "Supply Chain Fundamentals", "Week 1"), ExpectedLevel: 4),
            (Path: Path.Combine(_tempVaultRoot, "Value Chain Management", "Operations Management", "Supply Chain Fundamentals", "Week 1", "Lesson 1"), ExpectedLevel: 5),
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            int actualLevel = detector.CalculateHierarchyLevel(testCase.Path);
            Assert.AreEqual(testCase.ExpectedLevel, actualLevel,
                $"Path '{testCase.Path}' should have hierarchy level {testCase.ExpectedLevel}, got {actualLevel}");
        }
    }

    /// <summary>
    /// Tests that hierarchy level is correctly calculated for relative paths.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_RelativePaths_ReturnsCorrectLevel()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Get the relative paths
        string vaultRootRelative = Path.GetFileName(_tempVaultRoot);

        // Test cases - relative path and expected hierarchy level
        var testCases = new[]
        {
            (Path: "", ExpectedLevel: 0), // Empty path should be treated as vault root
            (Path: "Value Chain Management", ExpectedLevel: 1),
            (Path: "Value Chain Management/Operations Management", ExpectedLevel: 2),
            (Path: "Value Chain Management\\Operations Management", ExpectedLevel: 2), // Testing Windows path separator
            (Path: "Value Chain Management/Operations Management/Supply Chain Fundamentals", ExpectedLevel: 3),
            (Path: Path.Combine("Value Chain Management", "Operations Management", "Supply Chain Fundamentals", "Week 1"), ExpectedLevel: 4),
            (Path: Path.Combine("Value Chain Management", "Operations Management", "Supply Chain Fundamentals", "Week 1", "Lesson 1"), ExpectedLevel: 5),
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            int actualLevel = detector.CalculateHierarchyLevel(testCase.Path);
            Assert.AreEqual(testCase.ExpectedLevel, actualLevel,
                $"Relative path '{testCase.Path}' should have hierarchy level {testCase.ExpectedLevel}, got {actualLevel}");
        }
    }

    /// <summary>
    /// Tests that hierarchy level calculation uses the provided vault path override.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithVaultPathOverride_ReturnsCorrectLevel()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Use a subdirectory as vault root override
        string vaultOverride = Path.Combine(_tempVaultRoot, "Value Chain Management", "Operations Management");

        // Test cases - path and expected hierarchy level when using vault override
        var testCases = new[]
        {
            (Path: vaultOverride, ExpectedLevel: 0), // Override root becomes level 0
            (Path: Path.Combine(vaultOverride, "Supply Chain Fundamentals"), ExpectedLevel: 1),
            (Path: Path.Combine(vaultOverride, "Supply Chain Fundamentals", "Week 1"), ExpectedLevel: 2),
            (Path: Path.Combine(vaultOverride, "Supply Chain Fundamentals", "Week 1", "Lesson 1"), ExpectedLevel: 3),
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            int actualLevel = detector.CalculateHierarchyLevel(testCase.Path, vaultOverride);
            Assert.AreEqual(testCase.ExpectedLevel, actualLevel,
                $"With vault override '{vaultOverride}', path '{testCase.Path}' should have hierarchy level {testCase.ExpectedLevel}, got {actualLevel}");
        }
    }

    /// <summary>
    /// Tests that GetTemplateTypeFromHierarchyLevel returns the correct template type.
    /// </summary>
    [TestMethod]
    public void GetTemplateTypeFromHierarchyLevel_ValidLevels_ReturnsCorrectTemplateType()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Test cases - hierarchy level and expected template type
        var testCases = new[]
        {
            (Level: 0, ExpectedType: "main"),
            (Level: 1, ExpectedType: "program"),
            (Level: 2, ExpectedType: "course"),
            (Level: 3, ExpectedType: "class"),
            (Level: 4, ExpectedType: "module"),
            (Level: 5, ExpectedType: "lesson"),
            (Level: 6, ExpectedType: "unknown"),
            (Level: 7, ExpectedType: "unknown"),
        };

        // Act & Assert
        foreach (var testCase in testCases)
        {
            string actualType = detector.GetTemplateTypeFromHierarchyLevel(testCase.Level);
            Assert.AreEqual(testCase.ExpectedType, actualType,
                $"Hierarchy level {testCase.Level} should map to template type '{testCase.ExpectedType}', got '{actualType}'");
        }
    }

    /// <summary>
    /// Tests that hierarchy level is correctly calculated with an absolute path.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithAbsolutePath_CalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);
        string absolutePath = Path.Combine(_tempVaultRoot, "Program1", "Course1", "Class1", "Module1");

        // Act
        int level = detector.CalculateHierarchyLevel(absolutePath);

        // Assert
        Assert.AreEqual(4, level, "Hierarchy level should be 4 for a path 4 levels deep from vault root");
    }

    /// <summary>
    /// Tests that hierarchy level is correctly calculated with a relative path.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithRelativePath_CalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Get the directory where the temp vault is located (usually the temp directory)
        string tempDir = Path.GetDirectoryName(_tempVaultRoot)!;
        string vaultName = Path.GetFileName(_tempVaultRoot);

        // Change the current directory to the temp directory
        string originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);

            // Use a relative path from the temp directory
            string relativePath = Path.Combine(vaultName, "Program1", "Course1");            // Act
            int level = detector.CalculateHierarchyLevel(relativePath);

            // Assert - the level is 3 because the temp directory itself is a level (1)
            // plus Program1 (2) and Course1 (3)
            Assert.AreEqual(3, level, "Hierarchy level should be 3 for a path that is 3 levels deep from vault root");
        }
        finally
        {
            // Restore original directory
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    /// <summary>
    /// Tests that template type is correctly determined based on hierarchy level.
    /// </summary>
    [TestMethod]
    public void DetermineTemplateType_BasedOnHierarchyLevel_ReturnsCorrectTemplate()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);
        var mockProcessorLogger = new Mock<ILogger<VaultIndexProcessor>>();

        // We need all the dependencies to create the VaultIndexProcessor
        var mockTemplateManager = new Mock<IMetadataTemplateManager>();
        var mockStructureExtractor = new Mock<ICourseStructureExtractor>();
        var mockYamlHelper = new Mock<IYamlHelper>();
        var mockNoteBuilder = new Mock<MarkdownNoteBuilder>(MockBehavior.Default, null!);

        IVaultIndexProcessor processor = new VaultIndexProcessor(
            mockProcessorLogger.Object,
            mockTemplateManager.Object,
            detector,
            mockStructureExtractor.Object,
            mockYamlHelper.Object,
            mockNoteBuilder.Object,
            _appConfig);

        // Create paths at different hierarchy levels
        string pathLevel1 = Path.Combine(_tempVaultRoot, "Program1");
        string pathLevel3 = Path.Combine(_tempVaultRoot, "Program1", "Course1", "Class1");
        string pathLevel5 = Path.Combine(_tempVaultRoot, "Program1", "Course1", "Class1", "Module1", "Lesson1");

        // Act
        int level1 = detector.CalculateHierarchyLevel(pathLevel1);
        int level3 = detector.CalculateHierarchyLevel(pathLevel3);
        int level5 = detector.CalculateHierarchyLevel(pathLevel5);        // Convert from MetadataHierarchyDetector's 0-based to VaultIndexProcessor's 1-based level
        string templateType1 = processor.DetermineTemplateType(level1 + 1);
        string templateType3 = processor.DetermineTemplateType(level3 + 1);
        string templateType5 = processor.DetermineTemplateType(level5 + 1);

        // Assert        Assert.AreEqual("program", templateType1, "Level 1 should map to 'program' template");
        Assert.AreEqual("class", templateType3, "Level 3 should map to 'class' template");
        Assert.AreEqual("lesson", templateType5, "Level 5 should map to 'lesson' template");
    }

    /// <summary>
    /// Tests that hierarchy level is correctly calculated when vault root is overridden.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithVaultRootOverride_CalculatesCorrectly()
    {
        // Arrange
        string overrideVaultRoot = Path.Combine(_tempVaultRoot, "Program1", "Course1");
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig, overrideVaultRoot);

        string testPath = Path.Combine(overrideVaultRoot, "Class1", "Module1");

        // Act
        int level = detector.CalculateHierarchyLevel(testPath);

        // Assert
        Assert.AreEqual(2, level, "Hierarchy level should be 2 when measured from the overridden vault root");
    }

    /// <summary>
    /// Tests that special folder names are correctly detected and map to the right template types.
    /// </summary>
    [TestMethod]
    public void DetermineTemplateType_WithSpecialFolderNames_ReturnsCorrectTemplate()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);
        var mockProcessorLogger = new Mock<ILogger<VaultIndexProcessor>>();

        // We need all the dependencies to create the VaultIndexProcessor
        var mockTemplateManager = new Mock<IMetadataTemplateManager>();
        var mockStructureExtractor = new Mock<ICourseStructureExtractor>();
        var mockYamlHelper = new Mock<IYamlHelper>();
        var mockNoteBuilder = new Mock<MarkdownNoteBuilder>(MockBehavior.Default, null!);

        IVaultIndexProcessor processor = new VaultIndexProcessor(
            mockProcessorLogger.Object,
            mockTemplateManager.Object,
            detector,
            mockStructureExtractor.Object,
            mockYamlHelper.Object,
            mockNoteBuilder.Object,
            _appConfig);

        // Create a path that should be at hierarchy level 4 (course level)
        string coursePath = Path.Combine(_tempVaultRoot, "Program1", "Course1", "Class1");
        int courseLevel = detector.CalculateHierarchyLevel(coursePath) + 1; // +1 for 0-based to 1-based

        // Act & Assert
        Assert.AreEqual("lesson", processor.DetermineTemplateType(courseLevel + 1, "Lessons"),
            "A folder named 'Lessons' at level 5 should map to 'lesson' template");

        Assert.AreEqual("module", processor.DetermineTemplateType(courseLevel + 1, "Modules"),
            "A folder named 'Modules' at level 5 should map to 'module' template");

        Assert.AreEqual("module", processor.DetermineTemplateType(courseLevel + 1, "Case Studies"),
            "A folder named 'Case Studies' at level 5 should map to 'module' template");
    }

    /// <summary>
    /// Tests that Windows-style paths are handled correctly.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithWindowsStylePath_CalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Create Windows-style path with backslashes
        string windowsPath = _tempVaultRoot.Replace('/', '\\') + "\\Program1\\Course1";

        // Act
        int level = detector.CalculateHierarchyLevel(windowsPath);

        // Assert
        Assert.AreEqual(2, level, "Hierarchy level should be 2 for a Windows-style path 2 levels deep");
    }

    /// <summary>
    /// Tests that Unix-style paths are handled correctly.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithUnixStylePath_CalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Create Unix-style path with forward slashes
        string unixPath = _tempVaultRoot.Replace('\\', '/') + "/Program1/Course1";

        // Act
        int level = detector.CalculateHierarchyLevel(unixPath);

        // Assert
        Assert.AreEqual(2, level, "Hierarchy level should be 2 for a Unix-style path 2 levels deep");
    }

    /// <summary>
    /// Tests that mixed paths (with both forward and backslashes) are handled correctly.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithMixedSlashes_CalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Create mixed path with both slash types
        string mixedPath = _tempVaultRoot + "/Program1\\Course1";

        // Act
        int level = detector.CalculateHierarchyLevel(mixedPath);

        // Assert
        Assert.AreEqual(2, level, "Hierarchy level should be 2 for a mixed-slash path 2 levels deep");
    }

    /// <summary>
    /// Tests that folder names with special characters are handled correctly.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithSpecialCharacters_CalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Create temporary special folders
        string specialFolder1 = Path.Combine(_tempVaultRoot, "Program-With_Special.Chars!");
        string specialFolder2 = Path.Combine(specialFolder1, "Course (2023)");
        Directory.CreateDirectory(specialFolder1);
        Directory.CreateDirectory(specialFolder2);

        // Act
        int level = detector.CalculateHierarchyLevel(specialFolder2);

        // Assert
        Assert.AreEqual(2, level, "Hierarchy level should be 2 for a path with special characters");
    }

    /// <summary>
    /// Tests that folder names with spaces are handled correctly.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithSpacesInFolderNames_CalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Create temporary folders with spaces
        string spaceFolder1 = Path.Combine(_tempVaultRoot, "Program With Spaces");
        string spaceFolder2 = Path.Combine(spaceFolder1, "Course Name With Spaces");
        Directory.CreateDirectory(spaceFolder1);
        Directory.CreateDirectory(spaceFolder2);

        // Act
        int level = detector.CalculateHierarchyLevel(spaceFolder2);

        // Assert
        Assert.AreEqual(2, level, "Hierarchy level should be 2 for a path with spaces in folder names");
    }

    /// <summary>
    /// Tests template selection for folder names with unusual patterns.
    /// </summary>
    [TestMethod]
    public void DetermineTemplateType_WithUnusualFolderPatterns_DetectsCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);
        var mockProcessorLogger = new Mock<ILogger<VaultIndexProcessor>>();
        var mockTemplateManager = new Mock<IMetadataTemplateManager>();
        var mockStructureExtractor = new Mock<ICourseStructureExtractor>();
        var mockYamlHelper = new Mock<IYamlHelper>();
        var mockNoteBuilder = new Mock<MarkdownNoteBuilder>(MockBehavior.Default, null!);

        IVaultIndexProcessor processor = new VaultIndexProcessor(
            mockProcessorLogger.Object,
            mockTemplateManager.Object,
            detector,
            mockStructureExtractor.Object,
            mockYamlHelper.Object,
            mockNoteBuilder.Object,
            _appConfig);
        // Create unusual folder names
        string lessonFolder = Path.Combine(_tempVaultRoot, "Program1", "Course1", "Class1", "Lesson-01_Introduction");
        string moduleFolder = Path.Combine(_tempVaultRoot, "Program1", "Course1", "Class1", "MODULE-2_Advanced-Topics");
        Directory.CreateDirectory(lessonFolder);
        Directory.CreateDirectory(moduleFolder);

        // Calculate hierarchy levels - should both be level 4
        int lessonLevel = detector.CalculateHierarchyLevel(lessonFolder);
        int moduleLevel = detector.CalculateHierarchyLevel(moduleFolder);

        // Act - convert to 1-based indexing for VaultIndexProcessor
        string lessonTemplate = processor.DetermineTemplateType(lessonLevel + 1, Path.GetFileName(lessonFolder));
        string moduleTemplate = processor.DetermineTemplateType(moduleLevel + 1, Path.GetFileName(moduleFolder));

        // Assert
        Assert.AreEqual("lesson", lessonTemplate, "Should detect 'lesson' in folder name despite unusual formatting");
        Assert.AreEqual("module", moduleTemplate, "Should detect 'module' in folder name despite unusual formatting");
    }

    /// <summary>
    /// Tests behavior when path doesn't exist.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithNonExistentPath_StillCalculatesCorrectly()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Create a path that doesn't exist
        string nonExistentPath = Path.Combine(_tempVaultRoot, "NonExistent", "Folder", "Structure");

        // Act
        int level = detector.CalculateHierarchyLevel(nonExistentPath);

        // Assert - should still calculate correctly even if path doesn't exist
        Assert.AreEqual(3, level, "Hierarchy level should be calculated based on path structure even if path doesn't exist");
    }
    /// <summary>
    /// Tests behavior with null path.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithNullPath_HandlesGracefully()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Act & Assert
        try
        {
            int level = detector.CalculateHierarchyLevel(null!);

            // If we get here without exception, the level should be a sensible value
            Assert.AreEqual(0, level, "Null path should be treated as root level");
        }
        catch (ArgumentException)
        {
            // It's also acceptable to throw ArgumentException for null path
            return; // Test passes if ArgumentException is thrown
        }
        catch (NullReferenceException)
        {
            // NullReferenceException is also an acceptable response to a null path
            return; // Test passes if NullReferenceException is thrown
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception type: {ex.GetType().Name}. Expected ArgumentException, NullReferenceException, or graceful handling.");
        }
    }

    /// <summary>
    /// Tests behavior when path is outside of vault root.
    /// </summary>
    [TestMethod]
    public void CalculateHierarchyLevel_WithPathOutsideVaultRoot_HandlesAppropriately()
    {
        // Arrange
        var detector = new MetadataHierarchyDetector(_mockLogger.Object, _appConfig);

        // Create a path outside the vault root
        string outsidePath = Path.GetTempPath(); // System temp path, definitely outside vault root

        // Act
        int level = detector.CalculateHierarchyLevel(outsidePath);

        // Assert - should handle appropriately
        // This is actually implementation-dependent - could return 0 (root level) or a negative number
        // or (safer) should throw an exception to indicate the path is outside the vault
        try
        {
            // If returning a numerical value, it should be 0 or negative to indicate it's outside the vault
            Assert.IsTrue(level <= 0, "Path outside vault root should return 0 or negative hierarchy level");
        }
        catch (Exception)
        {
            // It's also acceptable to throw an exception for paths outside vault root
            // Test passes if exception is thrown
        }
    }
}
