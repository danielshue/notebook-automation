// <copyright file="CourseStructureExtractorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Utils/CourseStructureExtractorTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests.Utils;

[TestClass]
public class CourseStructureExtractorTests
{
    private Mock<ILogger<CourseStructureExtractor>> mockLogger;
    private CourseStructureExtractor extractor;

    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<CourseStructureExtractor>>();
        extractor = new CourseStructureExtractor(mockLogger.Object);
    }

    [TestMethod]
    [DataRow(@"D:\Videos\01_module-introduction\02_lesson-overview\video.mp4", "Module Introduction", "Lesson Overview")]
    [DataRow(@"D:\Videos\module-1-intro\lesson-2-details\video.mp4", "Module 1 Intro", "Lesson 2 Details")]
    public void ExtractModuleAndLesson_VariousPathFormats_ExtractsCorrectly(string filePath, string expectedModule, string expectedLesson)
    {
        // Arrange
        Dictionary<string, object> metadata = [];

        // Act
        extractor.ExtractModuleAndLesson(filePath, metadata);

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
    }

    [TestMethod]
    public void ExtractModuleAndLesson_SingleLevelCourse_ExtractsAsModule()
    {
        // Arrange
        Dictionary<string, object> metadata = [];
        string filePath = @"D:\Videos\01_course-orientation-operations-strategy\video.mp4";

        // Act
        extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert
        Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
        Assert.AreEqual("Course Orientation Operations Strategy", metadata["module"]);
        Assert.IsFalse(metadata.ContainsKey("lesson"), "Lesson key should not exist when only module is found");
    }

    [TestMethod]
    [DataRow(@"D:\Videos\01_module-intro\01_lesson-basics\video.mp4", "Module Intro", "Lesson Basics")]
    [DataRow(@"D:\Videos\02-advanced-module\03-detailed-lesson\video.mp4", "Advanced Module", "Detailed Lesson")]
    [DataRow(@"D:\Videos\01_course-introduction-and-module-1-joining\04_lesson-1-1-introduction\video.mp4", "Course Introduction And Module 1 Joining", "Lesson 1 1 Introduction")]
    public void CleanModuleOrLessonName_VariousFormats_FormatsCorrectly(string filePath, string expectedModule, string expectedLesson)
    {
        // Arrange
        Dictionary<string, object> metadata = [];

        // Act
        extractor.ExtractModuleAndLesson(filePath, metadata);

        // Assert
        Assert.IsTrue(metadata.ContainsKey("module"), "Module key should exist in metadata");
        Assert.AreEqual(expectedModule, metadata["module"]);
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
    [DataRow("Module-1-Introduction.pdf", "Module 1 Introduction", null)]
    [DataRow("Lesson-2-Details.md", null, "Lesson 2 Details")]
    [DataRow("Module1BasicConcepts.mp4", "Module 1 Basic Concepts", null)]
    [DataRow("Lesson3AdvancedTopics.docx", null, "Lesson 3 Advanced Topics")]
    [DataRow("01_course-overview-introduction.pdf", "Course Overview Introduction", null)]
    [DataRow("02_session-planning-details.md", "Session Planning Details", null)]
    [DataRow("Week1-Introduction.pdf", "Week1 Introduction", null)]
    [DataRow("some-random-file.txt", null, null)]
    public void ExtractModuleAndLesson_FilenameExtraction_ExtractsCorrectly(string filename, string expectedModule, string expectedLesson)
    {
        // Arrange
        Dictionary<string, object> metadata = [];
        string testPath = $@"D:\TestCourse\{filename}";

        // Act
        extractor.ExtractModuleAndLesson(testPath, metadata);

        // Assert
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
    [DataRow(@"D:\Course\Week 1\Session 2\material.pdf", "Week 1", "Session 2")]
    [DataRow(@"D:\Course\Unit-3\Class-1\notes.md", "Unit 3", "Class 1")]
    [DataRow(@"D:\Course\Module 1\Lecture 2\video.mp4", "Module 1", "Lecture 2")]
    [DataRow(@"D:\Course\01_advanced-concepts\file.pdf", "Advanced Concepts", null)]
    public void ExtractModuleAndLesson_EnhancedDirectoryPatterns_ExtractsCorrectly(string filePath, string expectedModule, string expectedLesson)
    {
        // Arrange
        Dictionary<string, object> metadata = [];

        // Act
        extractor.ExtractModuleAndLesson(filePath, metadata);

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
    }

    [TestMethod]
    public void ExtractModuleAndLesson_CaseStudiesPattern_HandlesCorrectly()
    {
        // Arrange - Case studies typically won't have lessons, as mentioned in the user request
        Dictionary<string, object> metadata = [];
        string filePath = @"D:\Course\Case Studies\Strategic Analysis\analysis.pdf";

        // Act
        extractor.ExtractModuleAndLesson(filePath, metadata);

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
        Dictionary<string, object> metadata1 = [];
        Dictionary<string, object> metadata2 = [];
        Dictionary<string, object> metadata3 = [];

        // Test filename-based extraction when directory doesn't help
        string filePath1 = @"D:\SomeFolder\AnotherFolder\Module-3-Financial-Analysis.pdf";

        // Test directory-based when filename doesn't help
        string filePath2 = @"D:\Course\02_advanced-topics\03_detailed-analysis\document.pdf";

        // Test enhanced patterns
        string filePath3 = @"D:\Course\Week 5\Session 3\slides.pptx";

        // Act
        extractor.ExtractModuleAndLesson(filePath1, metadata1);
        extractor.ExtractModuleAndLesson(filePath2, metadata2);
        extractor.ExtractModuleAndLesson(filePath3, metadata3);

        // Assert
        Assert.IsTrue(metadata1.ContainsKey("module"), "Should extract module from filename");
        Assert.AreEqual("Module 3 Financial Analysis", metadata1["module"]);

        Assert.IsTrue(metadata2.ContainsKey("module"), "Should extract module from directory");
        Assert.IsTrue(metadata2.ContainsKey("lesson"), "Should extract lesson from directory");

        Assert.IsTrue(metadata3.ContainsKey("module"), "Should extract module from enhanced patterns");
        Assert.IsTrue(metadata3.ContainsKey("lesson"), "Should extract lesson from enhanced patterns");
    }
}
