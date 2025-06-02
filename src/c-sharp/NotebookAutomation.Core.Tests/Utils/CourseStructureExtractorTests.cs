using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Utils;

[TestClass]
public class CourseStructureExtractorTests
{
    private Mock<ILogger> _mockLogger;
    private CourseStructureExtractor _extractor;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _extractor = new CourseStructureExtractor(_mockLogger.Object);
    }

    [TestMethod]
    [DataRow(@"D:\Videos\01_module-introduction\02_lesson-overview\video.mp4", "Module Introduction", "Lesson Overview")]
    [DataRow(@"D:\Videos\module-1-intro\lesson-2-details\video.mp4", "Module 1 Intro", "Lesson 2 Details")]
    public void ExtractModuleAndLesson_VariousPathFormats_ExtractsCorrectly(string filePath, string expectedModule, string expectedLesson)
    {
        // Arrange
        Dictionary<string, object> metadata = [];

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
    }

    [TestMethod]
    public void ExtractModuleAndLesson_SingleLevelCourse_ExtractsAsModule()
    {
        // Arrange
        Dictionary<string, object> metadata = [];
        string filePath = @"D:\Videos\01_course-orientation-operations-strategy\video.mp4";

        // Act
        _extractor.ExtractModuleAndLesson(filePath, metadata);

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
        _extractor.ExtractModuleAndLesson(filePath, metadata);

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
}
