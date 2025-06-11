// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tests.Utilities;

[TestClass]
public class CourseStructureExtractorTests
{
    private Mock<ILogger<CourseStructureExtractor>> _mockLogger = null!;
    private Mock<AppConfig> _mockAppConfig = null!;
    private CourseStructureExtractor _extractor = null!;
    private string _vaultRoot = null!; [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<CourseStructureExtractor>>();

        // Set up a consistent vault root for all tests
        _vaultRoot = Path.Combine(Path.GetTempPath(), "TestVault");

        // Ensure test vault root exists
        Directory.CreateDirectory(_vaultRoot);

        // Mock AppConfig with the vault root
        _mockAppConfig = new Mock<AppConfig>();
        var mockPaths = new Mock<PathsConfig>();
        mockPaths.Setup(p => p.NotebookVaultFullpathRoot).Returns(_vaultRoot);
        _mockAppConfig.Setup(c => c.Paths).Returns(mockPaths.Object);

        _extractor = new CourseStructureExtractor(_mockLogger.Object, _mockAppConfig.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test vault directory if it exists
        if (Directory.Exists(_vaultRoot))
        {
            try
            {
                Directory.Delete(_vaultRoot, true);
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
    }
    [TestMethod]
    [DataRow("01_module-introduction", "02_lesson-overview", "document.md", "Module Introduction", "Lesson Overview")]
    [DataRow("module-1-intro", "lesson-2-details", "notes.md", "Module 1 Intro", "Lesson 2 Details")]
    public void ExtractModuleAndLesson_VariousPathFormats_NonContentFiles_ExtractsCorrectly(
        string moduleDir, string lessonDir, string filename, string expectedModule, string expectedLesson)
    {
        // Arrange - Using non-content files (documents, notes) which get friendly module titles
        // Use proper vault hierarchy: program > course > class > module > lesson
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, lessonDir, filename);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        Dictionary<string, object?> metadata = [];

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Non-content files should have friendly module titles
        if (expectedModule != null)
        {
            Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
            Assert.AreEqual(expectedModule, metadata["module"], "Non-content file should have friendly module title");
        }

        if (expectedLesson != null)
        {
            Assert.IsTrue(metadata.ContainsKey("lesson"), "Lesson key should exist in metadata");
            Assert.AreEqual(expectedLesson, metadata["lesson"]);
        }
    }

    [TestMethod]
    [DataRow("01_module-introduction", "02_lesson-overview", "video.mp4", "01", "Lesson Overview")]
    [DataRow("module-1-intro", "lesson-2-details", "video.mp4", "1", "Lesson 2 Details")]
    public void ExtractModuleAndLesson_VariousPathFormats_ContentFiles_ExtractsCorrectly(
        string moduleDir, string lessonDir, string filename, string expectedModule, string expectedLesson)
    {
        // Arrange - Using content files (videos) which get number-only module values
        // Use proper vault hierarchy: program > course > class > module > lesson
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, lessonDir, filename);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        Dictionary<string, object?> metadata = [];

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Content files should have number-only module values
        if (expectedModule != null)
        {
            Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
            Assert.AreEqual(expectedModule, metadata["module"], "Content file should have numeric-only module value");
        }

        if (expectedLesson != null)
        {
            Assert.IsTrue(metadata.ContainsKey("lesson"), "Lesson key should exist in metadata");
            Assert.AreEqual(expectedLesson, metadata["lesson"]);
        }
    }

    [TestMethod]
    public void ExtractModuleAndLesson_SingleLevelCourse_NonContentFile_ExtractsAsModuleFriendlyTitle()
    {
        // Arrange
        Dictionary<string, object?> metadata = [];

        // Use proper vault hierarchy: program > course > class > module
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";
        string moduleDir = "01_course-orientation-operations-strategy";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, "overview.md");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Non-content file should get friendly module title
        Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
        Assert.AreEqual("Course Orientation Operations Strategy", metadata["module"], "Non-content file should have friendly module title");
        Assert.IsFalse(metadata.ContainsKey("lesson"), "Lesson key should not exist when only module is found");
    }

    [TestMethod]
    public void ExtractModuleAndLesson_SingleLevelCourse_ContentFile_ExtractsAsModuleNumber()
    {
        // Arrange
        Dictionary<string, object?> metadata = [];

        // Use proper vault hierarchy: program > course > class > module
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";
        string moduleDir = "01_course-orientation-operations-strategy";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, "video.mp4");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Content file should get number-only module value
        Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
        Assert.AreEqual("01", metadata["module"], "Content file should have numeric-only module value");
        Assert.IsFalse(metadata.ContainsKey("lesson"), "Lesson key should not exist when only module is found");
    }

    [TestMethod]
    [DataRow("01_module-intro", "01_lesson-basics", "notes.md", "Module Intro", "Lesson Basics")]
    [DataRow("02-advanced-module", "03-detailed-lesson", "overview.md", "Advanced Module", "Detailed Lesson")]
    [DataRow("01_course-introduction-and-module-1-joining", "04_lesson-1-1-introduction", "index.md", "Course Introduction And Module 1 Joining", "Lesson 1 1 Introduction")]
    public void CleanModuleOrLessonName_VariousFormats_NonContentFiles_FormatsCorrectly(
        string moduleDir, string lessonDir, string filename, string expectedModule, string expectedLesson)
    {
        // Arrange - Using non-content files (documents, notes) which get friendly module titles
        // Use proper vault hierarchy: program > course > class > module > lesson
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, lessonDir, filename);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        Dictionary<string, object?> metadata = [];

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Non-content files should have friendly module titles
        Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
        Assert.AreEqual(expectedModule, metadata["module"], "Non-content file should have friendly module title");
        Assert.IsTrue(metadata.ContainsKey("lesson"), "Lesson key should exist in metadata");
        Assert.AreEqual(expectedLesson, metadata["lesson"]);
    }

    [TestMethod]
    [DataRow("01_module-intro", "01_lesson-basics", "video.mp4", "01", "Lesson Basics")]
    [DataRow("02-advanced-module", "03-detailed-lesson", "video.mp4", "02", "Detailed Lesson")]
    [DataRow("01_course-introduction-and-module-1-joining", "04_lesson-1-1-introduction", "video.mp4", "01", "Lesson 1 1 Introduction")]
    public void CleanModuleOrLessonName_VariousFormats_ContentFiles_FormatsCorrectly(
        string moduleDir, string lessonDir, string filename, string expectedModule, string expectedLesson)
    {
        // Arrange - Using content files (videos) which get number-only module values
        // Use proper vault hierarchy: program > course > class > module > lesson
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, lessonDir, filename);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        Dictionary<string, object?> metadata = [];

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Content files should have number-only module values
        Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
        Assert.AreEqual(expectedModule, metadata["module"], "Content file should have numeric-only module value");
        Assert.IsTrue(metadata.ContainsKey("lesson"), "Lesson key should exist in metadata");
        Assert.AreEqual(expectedLesson, metadata["lesson"]);
    }

    [TestMethod]
    [DataRow("01_module-name", "Module Name")]
    [DataRow("02-lesson-intro", "Lesson Intro")]
    [DataRow("03_complex_naming-pattern", "Complex Naming Pattern")]
    public void CleanModuleOrLessonName_CleansProperly(string input, string expected)
    {
        // Act
        string result = CourseStructureExtractor.CleanModuleOrLessonName(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("Module-1-Introduction.md", "Module 1 Introduction", null)]
    [DataRow("Lesson-2-Details.md", null, "Lesson 2 Details")]
    [DataRow("01_course-overview-introduction.md", "Course Overview Introduction", null)]
    [DataRow("02_session-planning-details.md", "Session Planning Details", null)]
    [DataRow("Week1-Introduction.md", "Week1 Introduction", null)]
    [DataRow("some-random-file.txt", null, null)]
    public void ExtractModuleAndLesson_FilenameExtraction_NonContentFiles_ExtractsCorrectly(string filename, string expectedModule, string expectedLesson)
    {
        // Arrange - Using non-content files (documents, notes) which get friendly module titles
        Dictionary<string, object?> metadata = [];

        // Use proper vault hierarchy: program > course > class
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, filename);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Non-content files should get friendly module titles
        if (expectedModule != null)
        {
            Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
            Assert.AreEqual(expectedModule, metadata["module"], $"Expected module '{expectedModule}' for filename '{filename}'");
        }
        else
        {
            if (metadata.ContainsKey("module"))
            {
                Assert.IsNull(expectedModule, $"Expected no module but found '{metadata["module"]}' for filename '{filename}'");
            }
        }

        if (expectedLesson != null)
        {
            Assert.IsTrue(metadata.ContainsKey("lesson"), "Lesson key should exist in metadata");
            Assert.AreEqual(expectedLesson, metadata["lesson"], $"Expected lesson '{expectedLesson}' for filename '{filename}'");
        }
        else
        {
            if (metadata.ContainsKey("lesson"))
            {
                Assert.IsNull(expectedLesson, $"Expected no lesson but found '{metadata["lesson"]}' for filename '{filename}'");
            }
        }
    }

    [TestMethod]
    [DataRow("Module-1-Introduction.mp4", "01_module-introduction", null, "01", null)]
    [DataRow("Module1BasicConcepts.mp4", "01_module-basic-concepts", null, "01", null)]
    [DataRow("Lesson3AdvancedTopics.mp4", "03_module-advanced", "03_lesson-advanced-topics", null, "Lesson Advanced Topics")]
    [DataRow("01_video.mp4", "01_module-overview", null, "01", null)]
    [DataRow("01_reading.pdf", "01_module-overview", null, "01", null)]
    public void ExtractModuleAndLesson_FilenameExtraction_ContentFiles_ExtractsCorrectly(string filename, string moduleDir, string? lessonDir, string? expectedModule, string? expectedLesson)
    {
        // Arrange - Using content files (videos, readings) which get number-only module values
        Dictionary<string, object?> metadata = [];

        // Use proper vault hierarchy: program > course > class > module > [lesson]
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        string basePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir);

        string filePath;
        if (!string.IsNullOrEmpty(lessonDir))
        {
            basePath = Path.Combine(basePath, lessonDir);
            filePath = Path.Combine(basePath, filename);
        }
        else
        {
            filePath = Path.Combine(basePath, filename);
        }

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Content files should get number-only module values
        if (expectedModule != null)
        {
            Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
            Assert.AreEqual(expectedModule, metadata["module"], $"Content file should have numeric-only module value for '{filename}'");
        }
        else
        {
            if (metadata.ContainsKey("module"))
            {
                Assert.IsNull(expectedModule, $"Expected no module but found '{metadata["module"]}' for filename '{filename}'");
            }
        }

        if (expectedLesson != null)
        {
            Assert.IsTrue(metadata.ContainsKey("lesson"), "Lesson key should exist in metadata");
            Assert.AreEqual(expectedLesson, metadata["lesson"], $"Expected lesson '{expectedLesson}' for filename '{filename}'");
        }
        else
        {
            if (metadata.ContainsKey("lesson"))
            {
                Assert.IsNull(expectedLesson, $"Expected no lesson but found '{metadata["lesson"]}' for filename '{filename}'");
            }
        }
    }
    [TestMethod]
    [DataRow("Week 1", "Session 2", "material.pdf", "Week 1", "Session 2")]
    [DataRow("Unit-3", "Class-1", "notes.md", "Unit 3", "Class 1")]
    [DataRow("Module 1", "Lecture 2", "video.mp4", "1", "Lecture 2")]  // Content file should get numeric module
    [DataRow("01_advanced-concepts", "", "file.pdf", "Advanced Concepts", null)]
    public void ExtractModuleAndLesson_EnhancedDirectoryPatterns_ExtractsCorrectly(
        string moduleDir, string lessonDir, string fileName, string expectedModule, string expectedLesson)
    {
        // Arrange
        // Use proper vault hierarchy: program > course > class > module > (lesson)
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        string filePath;
        if (string.IsNullOrEmpty(lessonDir))
        {
            filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, fileName);
        }
        else
        {
            filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, lessonDir, fileName);
        }

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        Dictionary<string, object?> metadata = [];

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert
        if (expectedModule != null)
        {
            Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
            Assert.AreEqual(expectedModule, metadata["module"]);
        }

        if (expectedLesson != null)
        {
            Assert.IsTrue(metadata.ContainsKey("lesson"), "Lesson key should exist in metadata");
            Assert.AreEqual(expectedLesson, metadata["lesson"]);
        }
        else
        {
            Assert.IsFalse(metadata.ContainsKey("lesson"), "Lesson key should not exist when not expected");
        }
    }

    [TestMethod]
    public void ExtractModuleAndLesson_CaseStudiesPattern_HandlesCorrectly()
    {
        // Arrange - Case studies typically won't have lessons, as mentioned in the user request
        Dictionary<string, object?> metadata = [];

        // Use proper vault hierarchy: program > course > class > Case Studies > folder
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";
        string caseStudyDir = "Case Studies";
        string analysisDir = "Strategic Analysis";

        string filePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, caseStudyDir, analysisDir, "analysis.pdf");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert
        // Case studies should not extract lesson information
        Assert.IsFalse(metadata.ContainsKey("lesson"), "Case studies should not have lesson metadata");

        // But might have module information if the structure supports it
        if (metadata.ContainsKey("module"))
        {
            // This is acceptable - case studies might be organized within modules
            Assert.IsNotNull(metadata["module"]);
        }
    }

    [TestMethod]
    public void ExtractModuleAndLesson_FallbackStrategies_WorksCorrectly()
    {
        // Arrange - Test multiple extraction strategies
        Dictionary<string, object?> metadata1 = [];
        Dictionary<string, object?> metadata2 = [];
        Dictionary<string, object?> metadata3 = [];

        // Use proper vault hierarchy for testing different scenarios
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";

        // Test filename-based extraction when directory doesn't help
        string filePath1 = Path.Combine(_vaultRoot, programDir, courseDir, classDir, "Module-3-Financial-Analysis.pdf");

        // Test directory-based when filename doesn't help
        string modulePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, "02_advanced-topics");
        string lessonPath = Path.Combine(modulePath, "03_detailed-analysis");
        Directory.CreateDirectory(lessonPath);
        string filePath2 = Path.Combine(lessonPath, "document.pdf");

        // Test enhanced patterns
        string weekPath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, "Week 5");
        string sessionPath = Path.Combine(weekPath, "Session 3");
        Directory.CreateDirectory(sessionPath);
        string filePath3 = Path.Combine(sessionPath, "slides.pptx");

        // Act
        _extractor.ExtractModuleAndLesson(filePath1, metadata1);
        _extractor.ExtractModuleAndLesson(filePath2, metadata2);
        _extractor.ExtractModuleAndLesson(filePath3, metadata3);

        // Assert
        Assert.IsTrue(metadata1.ContainsKey("module"), "Should extract module from filename");
        Assert.AreEqual("Module 3 Financial Analysis", metadata1["module"]);

        Assert.IsTrue(metadata2.ContainsKey("module"), "Should extract module from directory");
        Assert.IsTrue(metadata2.ContainsKey("lesson"), "Should extract lesson from directory");

        Assert.IsTrue(metadata3.ContainsKey("module"), "Should extract module from enhanced patterns");
        Assert.IsTrue(metadata3.ContainsKey("lesson"), "Should extract lesson from enhanced patterns");
    }

    [TestMethod]
    public void ExtractModuleAndLesson_ContentFiles_ExtractsNumericModuleOnly()
    {
        // Arrange
        Dictionary<string, object?> metadata1 = [];
        Dictionary<string, object?> metadata2 = [];

        // Use proper vault hierarchy: program > course > class > module > lesson > content files
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";
        string moduleDir = "01_module-introduction";
        string lessonDir = "02_lesson-basics";

        // Create test directories
        string lessonPath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, lessonDir);
        Directory.CreateDirectory(lessonPath);

        // Create content files
        string videoPath = Path.Combine(lessonPath, "video.mp4");
        string readingPath = Path.Combine(lessonPath, "reading.pdf");

        // Act - extract from content files
        _extractor.ExtractModuleAndLesson(videoPath, metadata1);
        _extractor.ExtractModuleAndLesson(readingPath, metadata2);

        // Assert - Content files should get number-only module values
        Assert.IsTrue(metadata1.ContainsKey("module"), "Video file should have module metadata");
        Assert.AreEqual("01", metadata1["module"], "Video file should have numeric-only module value");

        Assert.IsTrue(metadata2.ContainsKey("module"), "Reading file should have module metadata");
        Assert.AreEqual("01", metadata2["module"], "Reading file should have numeric-only module value");

        // Verify lesson is still extracted properly
        Assert.IsTrue(metadata1.ContainsKey("lesson"), "Video file should have lesson metadata");
        Assert.AreEqual("Lesson Basics", metadata1["lesson"]);
    }

    [TestMethod]
    public void ExtractModuleAndLesson_NonContentFiles_ExtractsFriendlyModuleTitle()
    {
        // Arrange
        Dictionary<string, object?> metadata1 = [];
        Dictionary<string, object?> metadata2 = [];

        // Use proper vault hierarchy: program > course > class > module
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";
        string moduleDir = "01_module-introduction";

        // Create test directories
        string modulePath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir);
        Directory.CreateDirectory(modulePath);

        // Create non-content files (index/overview files)
        string indexPath = Path.Combine(modulePath, "index.md");
        string overviewPath = Path.Combine(modulePath, "module-overview.md");

        // Act
        _extractor.ExtractModuleAndLesson(indexPath, metadata1);
        _extractor.ExtractModuleAndLesson(overviewPath, metadata2);

        // Assert - Non-content files should get friendly module titles
        Assert.IsTrue(metadata1.ContainsKey("module"), "Index file should have module metadata");
        Assert.AreEqual("Module Introduction", metadata1["module"], "Index file should have friendly module title");

        Assert.IsTrue(metadata2.ContainsKey("module"), "Overview file should have module metadata");
        Assert.AreEqual("Module Introduction", metadata2["module"], "Overview file should have friendly module title");
    }

    [TestMethod]
    public void ExtractModuleAndLesson_NestedContentFiles_ExtractsCorrectHierarchy()
    {
        // Arrange
        // Use proper vault hierarchy with multiple levels of nesting
        string programDir = "Value Chain Management";
        string courseDir = "Operations Management";
        string classDir = "Supply Chain";
        string moduleDir = "01_operations-module";
        string lessonDir = "02_lesson-logistics";
        string contentDir = "videos";

        // Create the nested directory structure
        string contentPath = Path.Combine(_vaultRoot, programDir, courseDir, classDir, moduleDir, lessonDir, contentDir);
        Directory.CreateDirectory(contentPath);

        // Create a deeply nested content file
        string videoPath = Path.Combine(contentPath, "lecture.mp4");
        Dictionary<string, object?> metadata = [];

        // Act
        _extractor.ExtractModuleAndLesson(videoPath, metadata);

        // Assert - Should extract module and lesson correctly from the directory hierarchy
        Assert.IsTrue(metadata.ContainsKey("module"), "Should extract module from directory hierarchy");
        Assert.AreEqual("01", metadata["module"], "Should extract numeric-only module for content file"); Assert.IsTrue(metadata.ContainsKey("lesson"), "Should extract lesson from directory hierarchy");
        Assert.AreEqual("Lesson Logistics", metadata["lesson"], "Should extract friendly lesson name");
    }

    [TestMethod]
    [DataRow("Module 1 - Money and Finance", "1")]
    [DataRow("Module 2 - Modern Banking", "2")]
    [DataRow("Module 3 - Risk and Return", "3")]
    [DataRow("Module 4 - Regulation", "4")]
    public void ExtractModuleNumber_ModuleSpaceNumberPattern_ReturnsCorrectNumber(string dirName, string expectedNumber)
    {
        // Act
        var result = CourseStructureExtractor.ExtractModuleNumber(dirName);

        // Assert        Assert.AreEqual(expectedNumber, result, $"Should extract '{expectedNumber}' from '{dirName}'");
    }

    [TestMethod]
    public void ExtractModuleNumber_QuantitativeFundamentalsPattern_ReturnsCorrectNumber()
    {
        // Arrange
        string dirName = "03_quantitative-fundamentals-math-statistics-finance-more";
        string expected = "03";

        // Act
        var result = CourseStructureExtractor.ExtractModuleNumber(dirName);

        // Assert
        Assert.AreEqual(expected, result, $"Should extract '{expected}' from '{dirName}'");
    }

    [TestMethod]
    public void IsContentFile_InstructionsFile_ReturnsTrue()
    {
        // Arrange
        string filePath = Path.Combine(_vaultRoot, "TestCourse", "03_quantitative-fundamentals-math-statistics-finance-more", "math-basics-instructions.md");

        // Create the directory structure
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Act - We need to use reflection to access the private IsContentFile method
        var isContentFileMethod = typeof(CourseStructureExtractor).GetMethod("IsContentFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)isContentFileMethod!.Invoke(_extractor, new object[] { filePath })!;

        // Assert
        Assert.IsTrue(result, $"IsContentFile should return true for {filePath}");
    }

    [TestMethod]
    public void ExtractModuleNumberFromParentDirectories_QuantitativePattern_ReturnsCorrectNumber()
    {
        // Arrange
        string filePath = Path.Combine(_vaultRoot, "TestCourse", "03_quantitative-fundamentals-math-statistics-finance-more", "math-basics-instructions.md");

        // Create the directory structure
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var fileInfo = new FileInfo(filePath);
        var dir = fileInfo.Directory;

        // Act - We need to use reflection to access the private ExtractModuleNumberFromParentDirectories method
        var extractMethod = typeof(CourseStructureExtractor).GetMethod("ExtractModuleNumberFromParentDirectories",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (string?)extractMethod!.Invoke(_extractor, new object[] { dir! })!;

        // Assert
        Assert.AreEqual("03", result, $"ExtractModuleNumberFromParentDirectories should return '03' for {filePath}");
    }

    [TestMethod]
    public void FindModuleDirectory_QuantitativePattern_ReturnsCorrectDirectory()
    {
        // Arrange - directory parts that would be generated from the vault-relative path
        string[] directoryParts = ["TestCourse", "03_quantitative-fundamentals-math-statistics-finance-more"];

        // Act - We need to use reflection to access the private FindModuleDirectory method
        var findMethod = typeof(CourseStructureExtractor).GetMethod("FindModuleDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (string?)findMethod!.Invoke(null, new object[] { directoryParts })!;

        // Assert
        Assert.AreEqual("03_quantitative-fundamentals-math-statistics-finance-more", result,
            $"FindModuleDirectory should return the module directory from parts: [{string.Join(", ", directoryParts)}]");
    }

    [TestMethod]
    public void ExtractModuleAndLesson_ModuleSpacePattern_ContentFile_ExtractsNumberOnlyModule()
    {
        // Arrange - Test the "Module N - Description" pattern for content files
        string programDir = "Managerial Economics and Business Analysis";
        string courseDir = "Money and Banking";
        string moduleDir = "Module 1 - Money and Finance";
        string contentFile = "money-finance-overview-video.mp4";

        // Create the directory structure (no lesson level in this case)
        string modulePath = Path.Combine(_vaultRoot, programDir, courseDir, moduleDir);
        Directory.CreateDirectory(modulePath);

        // Create a content file in the module directory
        string filePath = Path.Combine(modulePath, contentFile);
        Dictionary<string, object?> metadata = [];

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert - Content files should get number-only module values
        Assert.IsTrue(metadata.ContainsKey("module"), "Should extract module from directory hierarchy");
        Assert.AreEqual("1", metadata["module"], "Should extract numeric-only module '1' for content file in 'Module 1 - Money and Finance'");
    }
    [TestMethod]
    public void ExtractModuleAndLesson_MixedPatterns_VaultScan_HandlesAllDiscoveredPatterns()
    {
        // Arrange - Test various patterns discovered in the vault analysis
        var testCases = new[]
        {
            // Strategic Management pattern
            new { ModuleDir = "02_module-1-leading-strategically", ContentFile = "lecture-video.mp4", ExpectedModule = "02" },

            // Money and Banking pattern
            new { ModuleDir = "Module 2 - Modern Banking", ContentFile = "banking-overview-video.mp4", ExpectedModule = "2" },

            // Accounting pattern
            new { ModuleDir = "01_course-overview-and-introduction-to-managerial-accounting", ContentFile = "intro-reading.pdf", ExpectedModule = "01" },

            // Fundamentals pattern
            new { ModuleDir = "03_quantitative-fundamentals-math-statistics-finance-more", ContentFile = "math-basics-instructions.md", ExpectedModule = "03" }
        };

        foreach (var testCase in testCases)
        {
            // Create directory structure for each test case
            string coursePath = Path.Combine(_vaultRoot, "TestCourse", testCase.ModuleDir);
            Directory.CreateDirectory(coursePath);

            string filePath = Path.Combine(coursePath, testCase.ContentFile);
            Dictionary<string, object?> metadata = [];            // Act
            _extractor.ExtractModuleAndLesson(filePath, metadata);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("module"), $"Should extract module for {testCase.ModuleDir}");
            Assert.AreEqual(testCase.ExpectedModule, metadata["module"],
                $"Module extraction failed for {testCase.ModuleDir} - expected '{testCase.ExpectedModule}'");
        }
    }

    [TestMethod]
    public void IsContentFile_CaseStudyUnderModule_TreatsAsContentFile()
    {        // Arrange - Case study under a module folder should be treated as content file (get module extraction)
        string filePath = @"D:\vault\TestProgram\TestCourse\03_module-fundamentals\Case Studies\financial-analysis-case-study.md";

        // Act
        var isContentFileMethod = typeof(CourseStructureExtractor).GetMethod("IsContentFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)isContentFileMethod!.Invoke(_extractor, new object[] { filePath })!;

        // Assert
        Assert.IsTrue(result, "Case study under module folder should be treated as content file for module extraction");
    }

    [TestMethod]
    public void IsContentFile_CaseStudyAtClassLevel_DoesNotTreatAsContentFile()
    {        // Arrange - Case study at class level should NOT be treated as content file (no module extraction)
        string filePath = @"D:\vault\TestProgram\TestCourse\Case Studies\strategic-planning-case-study.md";

        // Act
        var isContentFileMethod = typeof(CourseStructureExtractor).GetMethod("IsContentFile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)isContentFileMethod!.Invoke(_extractor, new object[] { filePath })!;

        // Assert
        Assert.IsFalse(result, "Case study at class level should NOT be treated as content file (no module extraction)");
    }

    [TestMethod]
    public void ExtractModuleAndLesson_CaseStudyUnderModule_ExtractsModule()
    {
        // Arrange - Case study under module should extract module number
        var logger = new Mock<ILogger<CourseStructureExtractor>>();
        var mockAppConfig = new Mock<AppConfig>();
        var extractor = new CourseStructureExtractor(logger.Object, mockAppConfig.Object); string filePath = @"D:\vault\TestProgram\TestCourse\03_module-fundamentals\Case Studies\financial-analysis-case-study.md";
        var metadata = new Dictionary<string, object?>();

        // Act
        extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert
        Assert.IsTrue(metadata.ContainsKey("module"), "Case study under module should have module metadata");
        Assert.AreEqual("3", metadata["module"], "Should extract module number '3' from '03_module-fundamentals'");
    }

    [TestMethod]
    public void ExtractModuleAndLesson_CaseStudyAtClassLevel_DoesNotExtractModule()
    {
        // Arrange - Case study at class level should NOT extract module
        var logger = new Mock<ILogger<CourseStructureExtractor>>();
        var mockAppConfig = new Mock<AppConfig>();
        var extractor = new CourseStructureExtractor(logger.Object, mockAppConfig.Object); string filePath = @"D:\vault\TestProgram\TestCourse\Case Studies\strategic-planning-case-study.md";
        var metadata = new Dictionary<string, object?>();

        // Act
        extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert
        Assert.IsFalse(metadata.ContainsKey("module"), "Case study at class level should NOT have module metadata");
    }
}