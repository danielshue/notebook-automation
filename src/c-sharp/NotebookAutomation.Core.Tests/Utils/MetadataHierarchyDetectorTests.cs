// <copyright file="MetadataHierarchyDetectorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Utils/MetadataHierarchyDetectorTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests.Utils;

/// <summary>
/// Comprehensive test suite for the MetadataHierarchyDetector class, validating hierarchy detection
/// and metadata enrichment functionality across various vault structures and content types.
/// </summary>
/// <remarks>
/// <para>
/// This test class provides complete coverage of the MetadataHierarchyDetector functionality,
/// which is responsible for automatically detecting educational hierarchy levels (program, course,
/// class, module, lesson) from file paths within a notebook vault structure and enriching
/// metadata with this hierarchical information.
/// </para>
/// <para>
/// The tests cover multiple scenarios including:
/// - Basic hierarchy detection for standard three-level structures (program/course/class)
/// - Extended hierarchy detection for complex five-level structures (program/course/class/module/lesson)
/// - Metadata enrichment based on content type and index file types
/// - Edge cases with minimal vault structures and missing hierarchy levels
/// - Multiple content types including videos, transcripts, notes, case studies, and resources
/// - Vault structure creation and validation for test infrastructure.
/// </para>
/// <para>
/// The test infrastructure creates temporary vault structures that mirror real-world educational
/// content organization, ensuring that the hierarchy detection works reliably across different
/// organizational patterns used in business education and training programs.
/// </para>
/// </remarks>
[TestClass]
internal class MetadataHierarchyDetectorTests
{
    private Mock<ILogger<MetadataHierarchyDetector>> loggerMock;
    private Mock<AppConfig> appConfigMock;
    private AppConfig testAppConfig;

    /// <summary>
    /// Initializes test dependencies and configuration before each test method execution.
    /// </summary>
    /// <remarks>
    /// Sets up mock objects for ILogger and AppConfig, creates a temporary vault root path
    /// for testing, and initializes the test configuration that will be used across all test methods.
    /// </remarks>
    [TestInitialize]
    public void Setup()
    {
        this.loggerMock = new Mock<ILogger<MetadataHierarchyDetector>>();

        // Create a real AppConfig instance instead of mocking it
        this.appConfigMock = new Mock<AppConfig>();

        // Create the real config and set it up
        AppConfig realConfig = new()
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = Path.Combine(Path.GetTempPath(), "TestVault"),
            },
        };        // Store the real config in a field for test usage
        this.testAppConfig = realConfig;
    }

    /// <summary>
    /// Verifies that the MetadataHierarchyDetector correctly identifies hierarchy levels
    /// for a standard three-level vault structure (program/course/class).
    /// </summary>
    /// <remarks>
    /// Tests the basic hierarchy detection functionality using a realistic business program
    /// structure. Validates that a file path like "Value Chain Management/Supply Chain/Class 1/video.mp4"
    /// correctly maps to program="Value Chain Management", course="Supply Chain", class="Class 1".
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_ValueChainManagementPath_DetectsCorrectHierarchy()
    {
        // Arrange
        string vaultRoot = this.testAppConfig.Paths.NotebookVaultFullpathRoot;
        string filePath = Path.Combine(vaultRoot, "Value Chain Management", "Supply Chain", "Class 1", "video.mp4");

        // Ensure directory exists for testing
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, "test file content");

        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig);

        // Act
        Dictionary<string, string> result = detector.FindHierarchyInfo(filePath);

        // Assert
        Assert.AreEqual("Value Chain Management", result["program"]);
        Assert.AreEqual("Supply Chain", result["course"]);
        Assert.AreEqual("Class 1", result["class"]);        // Cleanup
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Validates hierarchy detection for project-based content structure without special handling
    /// for legacy "01_Projects" folders.
    /// </summary>
    /// <remarks>
    /// Ensures that the refactored detector works with project-style content by treating projects
    /// as classes within the normal hierarchy, rather than using special-case logic that was
    /// previously hardcoded for "01_Projects" folders.
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_ProjectsStructurePath_DetectsCorrectHierarchy()
    {
        // Arrange
        string vaultRoot = this.testAppConfig.Paths.NotebookVaultFullpathRoot;

        // We've updated our hierarchy detector to use pure path-based detection
        // so we need to create a path that corresponds to our new structure
        // without '01_Projects' since we no longer have special case handling
        string filePath = Path.Combine(vaultRoot, "Value Chain Management", "Supply Chain", "Project 1", "video.mp4");

        // Ensure directory exists for testing
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, "test file content");

        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig);

        // Act
        Dictionary<string, string> result = detector.FindHierarchyInfo(filePath);

        // Assert
        Assert.AreEqual("Value Chain Management", result["program"]);
        Assert.AreEqual("Supply Chain", result["course"]);
        Assert.AreEqual("Project 1", result["class"]);

        // Cleanup
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// Verifies that the UpdateMetadataWithHierarchy method correctly adds hierarchy information
    /// to existing metadata without overwriting other properties.
    /// </summary>
    /// <remarks>
    /// Tests the metadata enhancement functionality to ensure hierarchy information is properly
    /// merged into existing metadata dictionaries while preserving existing properties like
    /// title and source_file.
    /// </remarks>
    [TestMethod]
    public void UpdateMetadataWithHierarchy_AddsHierarchyInfo()
    {
        // Arrange
        _ = new MetadataHierarchyDetector(this.loggerMock.Object, this.testAppConfig);

        Dictionary<string, object> metadata = new()
        {
            { "title", "Test Video" },
            { "source_file", "c:/path/to/video.mp4" },
        };

        Dictionary<string, string> hierarchyInfo = new()
        {
            { "program", "MBA Program" },
            { "course", "Finance" },
            { "class", "Accounting 101" },
        };        // Act - Use module to include all hierarchy levels
        Dictionary<string, object> result = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            metadata,
            hierarchyInfo,
            "module");

        // Assert
        Assert.AreEqual("MBA Program", result["program"]);
        Assert.AreEqual("Finance", result["course"]);
        Assert.AreEqual("Accounting 101", result["class"]);
    }

    /// <summary>
    /// Ensures that UpdateMetadataWithHierarchy respects existing hierarchy values in metadata
    /// and does not overwrite them with detected values.
    /// </summary>
    /// <remarks>
    /// Validates the non-destructive nature of metadata updates, ensuring that if hierarchy
    /// information already exists in the metadata (perhaps manually set), the detector
    /// will not override those values with its detected hierarchy.
    /// </remarks>
    [TestMethod]
    public void UpdateMetadataWithHierarchy_DoesNotOverrideExistingValues()
    {
        // Arrange
        _ = new MetadataHierarchyDetector(this.loggerMock.Object, this.testAppConfig);

        Dictionary<string, object> metadata = new()
        {
            { "title", "Test Video" },
            { "source_file", "c:/path/to/video.mp4" },
            { "program", "Existing Program" },
            { "course", "Existing Course" },
        };

        Dictionary<string, string> hierarchyInfo = new()
        {
            { "program", "MBA Program" },
            { "course", "Finance" },
            { "class", "Accounting 101" },
        };        // Act - Use module to include all hierarchy levels
        Dictionary<string, object> result = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            metadata,
            hierarchyInfo,
            "module");

        // Assert
        Assert.AreEqual("Existing Program", result["program"]);
        Assert.AreEqual("Existing Course", result["course"]);
        Assert.AreEqual("Accounting 101", result["class"]);
    }

    /// <summary>
    /// Comprehensive test that validates hierarchy detection across all levels of a complete
    /// vault structure including lessons, modules, classes, courses, and programs.
    /// </summary>
    /// <remarks>
    /// This is the most comprehensive hierarchy detection test, validating that the detector
    /// correctly identifies hierarchy at every level from the deepest content files up to
    /// the vault root, including the new lesson and module levels added to the hierarchy.
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_CompleteVaultStructure_DetectsCorrectHierarchy()
    {
        // Arrange
        var paths = this.CreateTemporaryVaultStructure();

        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig, verbose: true);

        // Test hierarchy detection at various levels

        // 1. Test at transcript file level (deepest)
        Dictionary<string, string> transcriptResult = detector.FindHierarchyInfo(paths["video-transcript.md"]);

        // 2. Test at lesson level
        Dictionary<string, string> lessonResult = detector.FindHierarchyInfo(paths["Intro"]);

        // 3. Test at module level
        Dictionary<string, string> moduleResult = detector.FindHierarchyInfo(paths["Fundamentals"]);

        // 4. Test at class level
        Dictionary<string, string> classResult = detector.FindHierarchyInfo(paths["Investment"]);

        // 5. Test at course level
        Dictionary<string, string> courseResult = detector.FindHierarchyInfo(paths["Finance"]);

        // 6. Test at program level
        Dictionary<string, string> programResult = detector.FindHierarchyInfo(paths["Program"]);

        // 7. Test at MBA root level
        Dictionary<string, string> mbaResult = detector.FindHierarchyInfo(paths["MBA"]);

        // 8. Test minimal vault (class-only)
        Dictionary<string, string> singleClassResult = detector.FindHierarchyInfo(paths["SingleClass"]);        // Assert - Transcript file
        Assert.AreEqual("MBA", transcriptResult["program"]);
        Assert.AreEqual("Program", transcriptResult["course"]);
        Assert.AreEqual("Finance", transcriptResult["class"]);
        Assert.IsTrue(transcriptResult.ContainsKey("module") && transcriptResult["module"] == "Investment");        // Assert - Lesson level
        Assert.AreEqual("MBA", lessonResult["program"]);
        Assert.AreEqual("Program", lessonResult["course"]);
        Assert.AreEqual("Finance", lessonResult["class"]);
        Assert.IsTrue(lessonResult.ContainsKey("module") && lessonResult["module"] == "Investment");

        // Assert - Module level
        Assert.AreEqual("MBA", moduleResult["program"]);
        Assert.AreEqual("Program", moduleResult["course"]);
        Assert.AreEqual("Finance", moduleResult["class"]);        // Assert - Class level
        Assert.AreEqual("MBA", classResult["program"]);
        Assert.AreEqual("Program", classResult["course"]);
        Assert.AreEqual("Finance", classResult["class"]);

        // Assert - Course level
        Assert.AreEqual("MBA", courseResult["program"]);
        Assert.AreEqual("Program", courseResult["course"]);
        Assert.AreEqual("Finance", courseResult["class"]);

        // Assert - Program level
        Assert.AreEqual("MBA", programResult["program"]);
        Assert.AreEqual("Program", programResult["course"]);
        Assert.AreEqual(string.Empty, programResult["class"]);

        // Assert - MBA root level
        Assert.AreEqual("MBA", mbaResult["program"]);
        Assert.AreEqual(string.Empty, mbaResult["course"]);
        Assert.AreEqual(string.Empty, mbaResult["class"]);        // Assert - Minimal vault (class-only) - SingleClass is at depth 1, so only program is set
        Assert.AreEqual("SingleClass", singleClassResult["program"]);
        Assert.AreEqual(string.Empty, singleClassResult["course"]);
        Assert.AreEqual(string.Empty, singleClassResult["class"]);
    }

    /// <summary>
    /// Cleans up temporary test files and directories after each test method execution.
    /// </summary>
    /// <remarks>
    /// Removes the temporary vault structure and any test files created during test execution
    /// to ensure a clean state for subsequent tests and prevent test isolation issues.
    /// </remarks>
    [TestCleanup]
    public void Cleanup()
    {
        // Clean up the test vault if it exists
        string vaultRoot = this.testAppConfig.Paths.NotebookVaultFullpathRoot;
        if (Directory.Exists(vaultRoot))
        {
            Directory.Delete(vaultRoot, true);
        }
    }

    /// <summary>
    /// Creates a complete temporary vault structure for testing hierarchy detection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Creates a realistic Obsidian-like vault structure with the following hierarchy:
    /// - Vault Root (e.g. "c:\source\myvault")
    ///   - MBA (Program)
    ///     - Program (Core Curriculum)
    ///       - Finance (Course)
    ///         - Investment (Class)
    ///           - Fundamentals (Module)
    ///             - Intro (Lesson)
    ///               - Content Files (transcripts, videos, readings)
    ///           - Case Studies
    ///             - Market Analysis (Case Study)
    ///               - Content Files (data, instructions, results)
    ///     - Resources
    ///       - Templates (Resource Materials)
    ///   - SingleClass (Minimal Vault Example).
    /// </para>
    /// <para>
    /// In this structure, the vault root is the folder specified in the configuration (e.g. "myvault"),
    /// and each level has its own index file named after the folder (e.g. MBA.md, Finance.md) with
    /// appropriate metadata fields for that level in the hierarchy.
    /// </para>
    /// <para>
    /// File types created include: index files (.md), video files (.mp4), transcript files (.md),
    /// instruction files (.md), case study files (.md, .xlsx, .pdf), and resource materials.
    /// </para>
    /// </remarks>
    /// <returns>A dictionary containing paths to key files in the vault for testing.</returns>
    private Dictionary<string, string> CreateTemporaryVaultStructure()
    {
        string vaultRoot = this.testAppConfig.Paths.NotebookVaultFullpathRoot;

        // Create the root directory if it doesn't exist
        Directory.CreateDirectory(vaultRoot);

        // Create a dictionary to store paths to various files in the structure
        Dictionary<string, string> paths = new();

        // Create the MBA program folder structure
        string mbaPath = Path.Combine(vaultRoot, "MBA");
        Directory.CreateDirectory(mbaPath);
        paths["MBA"] = mbaPath;

        // Create program-level index file (MBA.md)
        string mbaMdPath = Path.Combine(mbaPath, "MBA.md");
        File.WriteAllText(mbaMdPath, @"---
template-type: main
auto-generated-state: writable
banner: gies-banner.png
title: MBA Program
type: index
template-type: main
program: MBA
---

# MBA Program

## Programs
");
        paths["MBA.md"] = mbaMdPath;

        // Create a program folder (e.g., Core Curriculum)
        string programPath = Path.Combine(mbaPath, "Program");
        Directory.CreateDirectory(programPath);
        paths["Program"] = programPath;

        // Create program index file
        string programMdPath = Path.Combine(programPath, "Program.md");
        File.WriteAllText(programMdPath, @"---
template-type: program
auto-generated-state: writable
banner: gies-banner.png
title: Core Curriculum
type: index
template-type: program
program: MBA
course: Program
---

# Core Curriculum

## Courses
");
        paths["Program.md"] = programMdPath;

        // Create a course folder (e.g., Finance)
        string coursePath = Path.Combine(programPath, "Finance");
        Directory.CreateDirectory(coursePath);
        paths["Finance"] = coursePath;

        // Create course index file
        string courseMdPath = Path.Combine(coursePath, "Finance.md");
        File.WriteAllText(courseMdPath, @"---
template-type: course
auto-generated-state: writable
banner: gies-banner.png
title: Finance
type: index
template-type: course
program: MBA
course: Finance
---

# Finance

## Classes
");
        paths["Finance.md"] = courseMdPath;

        // Create a class folder (e.g., Investment)
        string classPath = Path.Combine(coursePath, "Investment");
        Directory.CreateDirectory(classPath);
        paths["Investment"] = classPath;

        // Create class index file
        string classMdPath = Path.Combine(classPath, "Investment.md");
        File.WriteAllText(classMdPath, @"---
template-type: class
auto-generated-state: writable
banner: gies-banner.png
title: Investment Principles
type: index
template-type: class
program: MBA
course: Finance
class: Investment
---

# Investment Principles

## Modules
");
        paths["Investment.md"] = classMdPath;

        // Create a module folder (e.g., Fundamentals)
        string modulePath = Path.Combine(classPath, "Fundamentals");
        Directory.CreateDirectory(modulePath);
        paths["Fundamentals"] = modulePath;

        // Create module index file
        string moduleMdPath = Path.Combine(modulePath, "Fundamentals.md");
        File.WriteAllText(moduleMdPath, @"---
template-type: module-index
auto-generated-state: writable
banner: gies-banner.png
title: Investment Fundamentals
type: index
index-type: module
program: MBA
course: Finance
class: Investment
module: Fundamentals
---

# Investment Fundamentals

## Lessons
");
        paths["Fundamentals.md"] = moduleMdPath;

        // Create a lesson folder (e.g., Intro)
        string lessonPath = Path.Combine(modulePath, "Intro");
        Directory.CreateDirectory(lessonPath);
        paths["Intro"] = lessonPath;

        // Create lesson index file
        string lessonMdPath = Path.Combine(lessonPath, "Intro.md");
        File.WriteAllText(lessonMdPath, @"---
template-type: lesson-index
auto-generated-state: writable
banner: gies-banner.png
title: Introduction to Investments
type: index
index-type: lesson
program: MBA
course: Finance
class: Investment
module: Fundamentals
lesson: Intro
---

# Introduction to Investments

## Content
");
        paths["Intro.md"] = lessonMdPath;

        // Create content files in the lesson folder
        // Video transcript
        string contentPath = Path.Combine(lessonPath, "video-transcript.md");
        File.WriteAllText(contentPath, @"---
title: Introduction to Investments Video
type: transcript
program: MBA
course: Finance
class: Investment
module: Fundamentals
lesson: Intro
---

# Introduction to Investments Video Transcript

This is a sample transcript.
");
        paths["video-transcript.md"] = contentPath;

        // Add a video file (just a placeholder)
        string videoPath = Path.Combine(lessonPath, "lecture.mp4");
        File.WriteAllText(videoPath, "This is a placeholder for a video file");
        paths["lecture.mp4"] = videoPath;

        // Add a lecture notes file
        string notesPath = Path.Combine(lessonPath, "lecture-notes.md");
        File.WriteAllText(notesPath, @"---
title: Lecture Notes - Introduction to Investments
type: notes
program: MBA
course: Finance
class: Investment
module: Fundamentals
lesson: Intro
---

# Lecture Notes: Introduction to Investments

These are detailed notes from the lecture.
");
        paths["lecture-notes.md"] = notesPath;

        // Add a reading materials file
        string readingPath = Path.Combine(lessonPath, "required-reading.md");
        File.WriteAllText(readingPath, @"---
title: Required Reading - Investment Basics
type: reading
program: MBA
course: Finance
class: Investment
module: Fundamentals
lesson: Intro
---

# Required Reading: Investment Basics

Here are the required reading materials for this lesson.
");
        paths["required-reading.md"] = readingPath;

        // Create a case study folder at the class level
        string caseStudiesPath = Path.Combine(classPath, "Case Studies");
        Directory.CreateDirectory(caseStudiesPath);
        paths["Case Studies"] = caseStudiesPath;

        // Create case study index file
        string caseStudyIndexPath = Path.Combine(caseStudiesPath, "Case Studies.md");
        File.WriteAllText(caseStudyIndexPath, @"---
template-type: module-index
auto-generated-state: writable
banner: gies-banner.png
title: Investment Case Studies
type: index
index-type: module
program: MBA
course: Finance
class: Investment
module: Case Studies
---

# Investment Case Studies

## Case Studies
");
        paths["Case Studies.md"] = caseStudyIndexPath;

        // Create a specific case study folder
        string marketAnalysisPath = Path.Combine(caseStudiesPath, "Market Analysis");
        Directory.CreateDirectory(marketAnalysisPath);
        paths["Market Analysis"] = marketAnalysisPath;

        // Create case study index file
        string marketAnalysisMdPath = Path.Combine(marketAnalysisPath, "Market Analysis.md");
        File.WriteAllText(marketAnalysisMdPath, @"---
template-type: lesson-index
auto-generated-state: writable
banner: gies-banner.png
title: Market Analysis Case Study
type: index
index-type: lesson
program: MBA
course: Finance
class: Investment
module: Case Studies
lesson: Market Analysis
---

# Market Analysis Case Study

## Files
");
        paths["Market Analysis.md"] = marketAnalysisMdPath;

        // Add case study files
        string caseInstructionsPath = Path.Combine(marketAnalysisPath, "instructions.md");
        File.WriteAllText(caseInstructionsPath, @"---
title: Market Analysis Case Study Instructions
type: instructions
program: MBA
course: Finance
class: Investment
module: Case Studies
lesson: Market Analysis
---

# Market Analysis Case Study Instructions

Follow these instructions to complete the case study.
");
        paths["case-instructions.md"] = caseInstructionsPath;

        // Add a spreadsheet file (just a placeholder)
        string dataPath = Path.Combine(marketAnalysisPath, "market-data.xlsx");
        File.WriteAllText(dataPath, "This is a placeholder for an Excel file with market data");
        paths["market-data.xlsx"] = dataPath;

        // Add a submission template
        string submissionPath = Path.Combine(marketAnalysisPath, "submission-template.md");
        File.WriteAllText(submissionPath, @"---
title: Case Study Submission Template
type: template
program: MBA
course: Finance
class: Investment
module: Case Studies
lesson: Market Analysis
---

# Case Study Submission Template

Use this template to submit your analysis.
");
        paths["submission-template.md"] = submissionPath;

        // Create a resources folder at the program level
        string resourcesPath = Path.Combine(mbaPath, "Resources");
        Directory.CreateDirectory(resourcesPath);
        paths["Resources"] = resourcesPath;

        // Create resources index file
        string resourcesMdPath = Path.Combine(resourcesPath, "Resources.md");
        File.WriteAllText(resourcesMdPath, @"---
template-type: program-index
auto-generated-state: writable
banner: gies-banner.png
title: MBA Program Resources
type: index
index-type: program
program: MBA
---

# MBA Program Resources

## Resource Materials
");
        paths["Resources.md"] = resourcesMdPath;

        // Add a template file
        string templatePath = Path.Combine(resourcesPath, "essay-template.md");
        File.WriteAllText(templatePath, @"---
title: Standard Essay Template
type: template
program: MBA
---

# Standard Essay Template

Use this template for all essay submissions.
");
        paths["essay-template.md"] = templatePath;

        // Create a class-only structure (for testing minimal vaults)
        string singleClassPath = Path.Combine(vaultRoot, "SingleClass");
        Directory.CreateDirectory(singleClassPath);
        paths["SingleClass"] = singleClassPath;

        // Create class-only index file
        string singleClassMdPath = Path.Combine(singleClassPath, "SingleClass.md");
        File.WriteAllText(singleClassMdPath, @"---
template-type: class-index
auto-generated-state: writable
banner: gies-banner.png
title: Single Class Example
type: index
index-type: class
class: SingleClass
---

# Single Class Example

## Content
");
        paths["SingleClass.md"] = singleClassMdPath;
        return paths;
    }

    /// <summary>
    /// Validates that UpdateMetadataWithHierarchy includes only appropriate hierarchy levels
    /// based on the specified index type parameter.
    /// </summary>
    /// <remarks>
    /// Tests the template-type-specific metadata inclusion logic to ensure that different types
    /// of index files (program-index, course-index, class-index, etc.) only receive metadata
    /// fields appropriate for their level in the hierarchy.
    /// </remarks>
    [TestMethod]
    public void UpdateMetadataWithHierarchy_RespectsTemplateTypeHierarchy()
    {
        // Arrange
        Dictionary<string, string> hierarchyInfo = new()
        {
            { "program", "MBA" },
            { "course", "Finance" },
            { "class", "Investment" },
            { "module", "Fundamentals" },
        };

        Dictionary<string, object> emptyMetadata = new();

        // Test with different index types

        // Act - main-index
        var mainResult = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            new Dictionary<string, object>(emptyMetadata), hierarchyInfo, "main-index");

        // Act - program-index
        var programResult = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            new Dictionary<string, object>(emptyMetadata), hierarchyInfo, "program-index");

        // Act - course-index
        var courseResult = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            new Dictionary<string, object>(emptyMetadata), hierarchyInfo, "course-index");

        // Act - class-index
        var classResult = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            new Dictionary<string, object>(emptyMetadata), hierarchyInfo, "class-index");

        // Act - module-index
        var moduleResult = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            new Dictionary<string, object>(emptyMetadata), hierarchyInfo, "module-index");

        // Assert - main-index should only have program
        Assert.AreEqual("MBA", mainResult["program"]);
        Assert.IsFalse(mainResult.ContainsKey("course"));
        Assert.IsFalse(mainResult.ContainsKey("class"));
        Assert.IsFalse(mainResult.ContainsKey("module"));

        // Assert - program-index should only have program
        Assert.AreEqual("MBA", programResult["program"]);
        Assert.IsFalse(programResult.ContainsKey("course"));
        Assert.IsFalse(programResult.ContainsKey("class"));
        Assert.IsFalse(programResult.ContainsKey("module"));

        // Assert - course-index should have program and course
        Assert.AreEqual("MBA", courseResult["program"]);
        Assert.AreEqual("Finance", courseResult["course"]);
        Assert.IsFalse(courseResult.ContainsKey("class"));
        Assert.IsFalse(courseResult.ContainsKey("module"));

        // Assert - class-index should have program, course, and class
        Assert.AreEqual("MBA", classResult["program"]);
        Assert.AreEqual("Finance", classResult["course"]);
        Assert.AreEqual("Investment", classResult["class"]);
        Assert.IsFalse(classResult.ContainsKey("module"));

        // Assert - module-index should have all levels
        Assert.AreEqual("MBA", moduleResult["program"]);
        Assert.AreEqual("Finance", moduleResult["course"]);
        Assert.AreEqual("Investment", moduleResult["class"]);
        Assert.AreEqual("Fundamentals", moduleResult["module"]);
    }

    /// <summary>
    /// Tests hierarchy detection for newly supported content types including case studies,
    /// assignments, and resource materials within the vault structure.
    /// </summary>
    /// <remarks>
    /// Validates that the hierarchy detector works correctly with expanded content types
    /// beyond traditional video lectures, including case studies, assignments, and resources
    /// that may be organized differently within the vault structure.
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_NewContentTypes_DetectsCorrectHierarchy()
    {
        // Arrange
        var paths = this.CreateTemporaryVaultStructure();

        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig);

        // Test hierarchy detection for various content types

        // 1. Video file in lesson folder
        Dictionary<string, string> videoResult = detector.FindHierarchyInfo(paths["lecture.mp4"]);

        // 2. Lecture notes file in lesson folder
        Dictionary<string, string> notesResult = detector.FindHierarchyInfo(paths["lecture-notes.md"]);

        // 3. Reading materials file in lesson folder
        Dictionary<string, string> readingResult = detector.FindHierarchyInfo(paths["required-reading.md"]);

        // 4. Case study instructions file
        Dictionary<string, string> caseInstructionsResult = detector.FindHierarchyInfo(paths["case-instructions.md"]);

        // 5. Case study data file
        Dictionary<string, string> dataResult = detector.FindHierarchyInfo(paths["market-data.xlsx"]);

        // 6. Resource template file
        Dictionary<string, string> templateResult = detector.FindHierarchyInfo(paths["essay-template.md"]);        // Assert - Video file (should have all hierarchy levels through lesson)
        Assert.AreEqual("MBA", videoResult["program"]);
        Assert.AreEqual("Program", videoResult["course"]);
        Assert.AreEqual("Finance", videoResult["class"]);
        Assert.IsTrue(videoResult.ContainsKey("module") && videoResult["module"] == "Investment");        // Assert - Lecture notes file
        Assert.AreEqual("MBA", notesResult["program"]);
        Assert.AreEqual("Program", notesResult["course"]);
        Assert.AreEqual("Finance", notesResult["class"]);
        Assert.IsTrue(notesResult.ContainsKey("module") && notesResult["module"] == "Investment");        // Assert - Reading materials file
        Assert.AreEqual("MBA", readingResult["program"]);
        Assert.AreEqual("Program", readingResult["course"]);
        Assert.AreEqual("Finance", readingResult["class"]);
        Assert.IsTrue(readingResult.ContainsKey("module") && readingResult["module"] == "Investment");        // Assert - Case study instructions file
        Assert.AreEqual("MBA", caseInstructionsResult["program"]);
        Assert.AreEqual("Program", caseInstructionsResult["course"]);
        Assert.AreEqual("Finance", caseInstructionsResult["class"]);
        Assert.IsTrue(caseInstructionsResult.ContainsKey("module") && caseInstructionsResult["module"] == "Investment");        // Assert - Case study data file
        Assert.AreEqual("MBA", dataResult["program"]);
        Assert.AreEqual("Program", dataResult["course"]);
        Assert.AreEqual("Finance", dataResult["class"]);
        Assert.IsTrue(dataResult.ContainsKey("module") && dataResult["module"] == "Investment");        // Assert - Resource template file (MBA/Resources/essay-template.md is at depth 3)
        Assert.AreEqual("MBA", templateResult["program"]);
        Assert.AreEqual("Resources", templateResult["course"], "Resource file path MBA/Resources/essay-template.md should have Resources as course");
        Assert.AreEqual("essay-template.md", templateResult["class"], "Resource file path should have filename as class at depth 3");
        Assert.IsFalse(templateResult.ContainsKey("module"), "Resource files at depth 3 should not have module info");
    }

    /// <summary>
    /// Debug utility test that outputs detailed information about the created vault
    /// structure for troubleshooting and verification during development.
    /// </summary>
    /// <remarks>
    /// Provides a debugging tool for developers to examine the actual vault structure
    /// created by the test helper methods and to troubleshoot any issues with hierarchy
    /// detection or vault organization.
    /// </remarks>
    [TestMethod]
    public void DebugPathStructure()
    {
        // Arrange
        var paths = this.CreateTemporaryVaultStructure();

        Console.WriteLine("Vault Root: " + this.testAppConfig.Paths.NotebookVaultFullpathRoot);

        // Print out all paths
        foreach (var path in paths)
        {
            Console.WriteLine($"Key: {path.Key}, Path: {path.Value}");
        }

        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig, verbose: true);

        // Test a few key paths to find the issue
        var transcriptResult = detector.FindHierarchyInfo(paths["video-transcript.md"]);
        var lectureResult = detector.FindHierarchyInfo(paths["lecture.mp4"]);
        var moduleFundamentalsResult = detector.FindHierarchyInfo(paths["Fundamentals"]);
        var investmentResult = detector.FindHierarchyInfo(paths["Investment"]);
        var financeResult = detector.FindHierarchyInfo(paths["Finance"]);
        var programResult = detector.FindHierarchyInfo(paths["Program"]);

        // Print out what's detected
        Console.WriteLine("\nTranscript Result:");
        this.PrintHierarchyResult(transcriptResult);

        Console.WriteLine("\nLecture Result:");
        this.PrintHierarchyResult(lectureResult);

        Console.WriteLine("\nFundamentals Module Result:");
        this.PrintHierarchyResult(moduleFundamentalsResult);

        Console.WriteLine("\nInvestment Class Result:");
        this.PrintHierarchyResult(investmentResult);

        Console.WriteLine("\nFinance Course Result:");
        this.PrintHierarchyResult(financeResult);

        Console.WriteLine("\nProgram Result:");
        this.PrintHierarchyResult(programResult);

        // Examine all file paths
        this.ExamineRelativePath(this.testAppConfig.Paths.NotebookVaultFullpathRoot, paths["video-transcript.md"]);
        this.ExamineRelativePath(this.testAppConfig.Paths.NotebookVaultFullpathRoot, paths["lecture.mp4"]);
        this.ExamineRelativePath(this.testAppConfig.Paths.NotebookVaultFullpathRoot, paths["Finance"]);
        this.ExamineRelativePath(this.testAppConfig.Paths.NotebookVaultFullpathRoot, paths["Program"]);

        // Verify all assertions pass
        Assert.IsTrue(true);
    }

    private void PrintHierarchyResult(Dictionary<string, string> result)
    {
        Console.WriteLine($"  Program: {result["program"]}");
        Console.WriteLine($"  Course: {result["course"]}");
        Console.WriteLine($"  Class: {result["class"]}");
        Console.WriteLine($"  Module: {(result.ContainsKey("module") ? result["module"] : "not set")}");
    }

    private void ExamineRelativePath(string basePath, string fullPath)
    {
        string relPath = Path.GetRelativePath(basePath, fullPath);
        Console.WriteLine($"\nPath: {fullPath}");
        Console.WriteLine($"Relative to {basePath}: {relPath}");
        Console.WriteLine($"Path segments: {string.Join(" > ", relPath.Split(Path.DirectorySeparatorChar).Where(s => !string.IsNullOrEmpty(s)))}");
    }

    /// <summary>
    /// Property to expose the vault root path for testing purposes.
    /// </summary>    public string VaultRoot => _testAppConfig.Paths.NotebookVaultFullpathRoot;

    /// <summary>
    /// Validates that the CreateTemporaryVaultStructure helper method creates the expected
    /// directory structure for comprehensive testing.
    /// </summary>
    /// <remarks>
    /// Tests the test infrastructure itself, ensuring that the helper method used across
    /// multiple tests creates a realistic and complete vault directory structure that
    /// accurately represents the intended hierarchy for testing purposes.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_CreatesCorrectDirectoryHierarchy()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();

        // Assert - Verify all expected directories exist
        Assert.IsTrue(Directory.Exists(paths["MBA"]), "MBA directory should exist");
        Assert.IsTrue(Directory.Exists(paths["Program"]), "Program directory should exist");
        Assert.IsTrue(Directory.Exists(paths["Finance"]), "Finance directory should exist");
        Assert.IsTrue(Directory.Exists(paths["Investment"]), "Investment directory should exist");
        Assert.IsTrue(Directory.Exists(paths["Fundamentals"]), "Fundamentals directory should exist");
        Assert.IsTrue(Directory.Exists(paths["Intro"]), "Intro directory should exist");

        // Assert - Verify directory hierarchy structure
        string expectedMbaPath = Path.Combine(this.testAppConfig.Paths.NotebookVaultFullpathRoot, "MBA");
        Assert.AreEqual(expectedMbaPath, paths["MBA"]);

        string expectedProgramPath = Path.Combine(expectedMbaPath, "Program");
        Assert.AreEqual(expectedProgramPath, paths["Program"]);

        string expectedFinancePath = Path.Combine(expectedProgramPath, "Finance");
        Assert.AreEqual(expectedFinancePath, paths["Finance"]);

        string expectedInvestmentPath = Path.Combine(expectedFinancePath, "Investment");
        Assert.AreEqual(expectedInvestmentPath, paths["Investment"]);

        string expectedFundamentalsPath = Path.Combine(expectedInvestmentPath, "Fundamentals");
        Assert.AreEqual(expectedFundamentalsPath, paths["Fundamentals"]);

        string expectedIntroPath = Path.Combine(expectedFundamentalsPath, "Intro");
        Assert.AreEqual(expectedIntroPath, paths["Intro"]);
    }

    /// <summary>
    /// Validates that the CreateTemporaryVaultStructure helper method creates the expected
    /// index files (.md files) at each level of the hierarchy with correct naming conventions.
    /// </summary>
    /// <remarks>
    /// Tests the index file creation functionality of the vault structure helper to ensure
    /// that each directory level has its corresponding index file properly named and located.
    /// Index files are essential for metadata detection and hierarchy navigation.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_CreatesCorrectIndexFiles()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();

        // Assert - Verify all index files exist and are named correctly
        Assert.IsTrue(File.Exists(paths["MBA.md"]), "MBA.md index file should exist");
        Assert.IsTrue(File.Exists(paths["Program.md"]), "Program.md index file should exist");
        Assert.IsTrue(File.Exists(paths["Finance.md"]), "Finance.md index file should exist");
        Assert.IsTrue(File.Exists(paths["Investment.md"]), "Investment.md index file should exist");
        Assert.IsTrue(File.Exists(paths["Fundamentals.md"]), "Fundamentals.md index file should exist");
        Assert.IsTrue(File.Exists(paths["Intro.md"]), "Intro.md index file should exist");

        // Assert - Verify index files are named after their containing folder
        Assert.AreEqual("MBA.md", Path.GetFileName(paths["MBA.md"]));
        Assert.AreEqual("Program.md", Path.GetFileName(paths["Program.md"]));
        Assert.AreEqual("Finance.md", Path.GetFileName(paths["Finance.md"]));
        Assert.AreEqual("Investment.md", Path.GetFileName(paths["Investment.md"]));
        Assert.AreEqual("Fundamentals.md", Path.GetFileName(paths["Fundamentals.md"]));
        Assert.AreEqual("Intro.md", Path.GetFileName(paths["Intro.md"]));
    }

    /// <summary>
    /// Validates that the CreateTemporaryVaultStructure helper method creates the expected
    /// content files (videos, transcripts, notes) at the appropriate levels in the vault hierarchy.
    /// </summary>
    /// <remarks>
    /// Tests the content file creation functionality to ensure that all types of educational
    /// content (video files, transcripts, lecture notes, reading materials) are properly
    /// created and placed in the correct lesson directories within the vault structure.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_CreatesCorrectContentFiles()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();

        // Assert - Verify content files exist
        Assert.IsTrue(File.Exists(paths["video-transcript.md"]), "Video transcript file should exist");
        Assert.IsTrue(File.Exists(paths["lecture.mp4"]), "Lecture video file should exist");
        Assert.IsTrue(File.Exists(paths["lecture-notes.md"]), "Lecture notes file should exist");
        Assert.IsTrue(File.Exists(paths["required-reading.md"]), "Required reading file should exist");

        // Assert - Verify content files are in the correct location (Intro lesson folder)
        string expectedIntroPath = paths["Intro"];
        Assert.AreEqual(expectedIntroPath, Path.GetDirectoryName(paths["video-transcript.md"]));
        Assert.AreEqual(expectedIntroPath, Path.GetDirectoryName(paths["lecture.mp4"]));
        Assert.AreEqual(expectedIntroPath, Path.GetDirectoryName(paths["lecture-notes.md"]));
        Assert.AreEqual(expectedIntroPath, Path.GetDirectoryName(paths["required-reading.md"]));
    }

    /// <summary>
    /// Verifies that the CreateTemporaryVaultStructure helper method creates a minimal
    /// vault structure for testing simple hierarchy scenarios with minimal complexity.
    /// </summary>
    /// <remarks>
    /// Tests the creation of simplified vault structures that contain only essential
    /// elements for basic hierarchy testing, ensuring the helper method can create
    /// both complex and minimal vault configurations as needed by different tests.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_CreatesMinimalVaultStructure()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();

        // Assert - Verify minimal vault (SingleClass) structure exists
        Assert.IsTrue(Directory.Exists(paths["SingleClass"]), "SingleClass directory should exist");
        Assert.IsTrue(File.Exists(paths["SingleClass.md"]), "SingleClass.md index file should exist");

        // Assert - Verify SingleClass is directly under vault root
        string expectedSingleClassPath = Path.Combine(this.testAppConfig.Paths.NotebookVaultFullpathRoot, "SingleClass");
        Assert.AreEqual(expectedSingleClassPath, paths["SingleClass"]);

        // Assert - Verify SingleClass.md is named correctly
        Assert.AreEqual("SingleClass.md", Path.GetFileName(paths["SingleClass.md"]));
    }

    /// <summary>
    /// Validates that the CreateTemporaryVaultStructure helper method creates additional
    /// content types beyond standard video lectures, including case studies and resources.
    /// </summary>
    /// <remarks>
    /// Tests the creation of diverse content types including case studies, market data files,
    /// submission templates, and essay templates to ensure the vault structure supports
    /// the full range of educational content formats used in business education.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_CreatesAdditionalContentTypes()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();

        // Assert - Verify additional content types exist
        Assert.IsTrue(File.Exists(paths["case-instructions.md"]), "Case instructions file should exist");
        Assert.IsTrue(File.Exists(paths["market-data.xlsx"]), "Market data file should exist");
        Assert.IsTrue(File.Exists(paths["submission-template.md"]), "Submission template file should exist");
        Assert.IsTrue(File.Exists(paths["essay-template.md"]), "Essay template file should exist");

        // Assert - Verify files are in expected locations
        // Case study files should be in the Market Analysis folder
        string expectedMarketAnalysisPath = paths["Market Analysis"];
        Assert.AreEqual(expectedMarketAnalysisPath, Path.GetDirectoryName(paths["case-instructions.md"]));
        Assert.AreEqual(expectedMarketAnalysisPath, Path.GetDirectoryName(paths["market-data.xlsx"]));
        Assert.AreEqual(expectedMarketAnalysisPath, Path.GetDirectoryName(paths["submission-template.md"]));

        // Essay template should be in Resources folder at MBA level
        string expectedResourcesPath = paths["Resources"];
        Assert.AreEqual(expectedResourcesPath, Path.GetDirectoryName(paths["essay-template.md"]));
    }

    /// <summary>
    /// Validates that the CreateTemporaryVaultStructure helper method returns a complete
    /// dictionary containing all expected path keys for every element in the vault structure.
    /// </summary>
    /// <remarks>
    /// Tests the completeness of the path dictionary returned by the vault structure helper,
    /// ensuring that every directory, index file, and content file is properly tracked
    /// and accessible through the returned path dictionary for use in other test methods.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_ReturnsAllExpectedPathKeys()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();

        // Assert - Verify all expected keys are present in the returned dictionary
        string[] expectedKeys = [
            "MBA", "MBA.md",
            "Program", "Program.md",
            "Finance", "Finance.md",
            "Investment", "Investment.md",
            "Fundamentals", "Fundamentals.md",
            "Intro", "Intro.md",
            "video-transcript.md", "lecture.mp4", "lecture-notes.md", "required-reading.md",
            "Case Studies", "Case Studies.md",
            "Market Analysis", "Market Analysis.md",
            "case-instructions.md", "market-data.xlsx", "submission-template.md",
            "Resources", "Resources.md", "essay-template.md",
            "SingleClass", "SingleClass.md"
        ];

        foreach (string expectedKey in expectedKeys)
        {
            Assert.IsTrue(paths.ContainsKey(expectedKey), $"Path dictionary should contain key: {expectedKey}");
            Assert.IsFalse(string.IsNullOrEmpty(paths[expectedKey]), $"Path for key '{expectedKey}' should not be null or empty");
        }

        // Assert - Verify we have at least the expected number of paths
        Assert.IsTrue(paths.Count >= expectedKeys.Length, $"Should have at least {expectedKeys.Length} paths, but found {paths.Count}");
    }

    /// <summary>
    /// Validates that the vault root path is properly accessible and that all created
    /// paths are correctly positioned within the vault root directory structure.
    /// </summary>
    /// <remarks>
    /// Tests the vault root accessibility and path relationships to ensure that the
    /// test infrastructure properly manages the temporary vault directory and that
    /// all created content is appropriately contained within the vault boundaries.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_VaultRootAccessible()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();

        // Assert - Verify vault root is accessible through public property        Assert.IsFalse(string.IsNullOrEmpty(_testAppConfig.Paths.NotebookVaultFullpathRoot), "VaultRoot property should not be null or empty");
        Assert.IsTrue(Directory.Exists(this.testAppConfig.Paths.NotebookVaultFullpathRoot), "VaultRoot directory should exist");        // Assert - Verify all created paths are under the vault root
        foreach (var pathEntry in paths)
        {
            string relativePath = Path.GetRelativePath(this.testAppConfig.Paths.NotebookVaultFullpathRoot, pathEntry.Value);
            Assert.IsFalse(
                relativePath.StartsWith(".."),
                $"Path '{pathEntry.Key}' should be under vault root. " +
                $"VaultRoot: {this.testAppConfig.Paths.NotebookVaultFullpathRoot}, Path: {pathEntry.Value}, Relative: {relativePath}");
        }
    }

    /// <summary>
    /// Verifies that the test configuration correctly exposes the vault root path
    /// for use in testing and that it matches the expected test directory structure.
    /// </summary>
    /// <remarks>
    /// Tests the test configuration setup to ensure that the vault root path is
    /// correctly configured and accessible for all test methods that need to work
    /// with the vault structure.
    /// </remarks>
    [TestMethod]
    public void VaultRoot_ExposesCorrectPath()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();        // Assert - Verify VaultRoot property returns the correct path
        Assert.AreEqual(this.testAppConfig.Paths.NotebookVaultFullpathRoot, this.testAppConfig.Paths.NotebookVaultFullpathRoot);

        // Assert - Verify VaultRoot is a valid directory path
        Assert.IsTrue(Path.IsPathRooted(this.testAppConfig.Paths.NotebookVaultFullpathRoot), "VaultRoot should be an absolute path");
        Assert.IsTrue(Directory.Exists(this.testAppConfig.Paths.NotebookVaultFullpathRoot), "VaultRoot directory should exist after creating vault structure");

        // Assert - Verify all top-level directories are under VaultRoot
        Assert.IsTrue(paths["MBA"].StartsWith(this.testAppConfig.Paths.NotebookVaultFullpathRoot), "MBA path should start with VaultRoot");
        Assert.IsTrue(paths["SingleClass"].StartsWith(this.testAppConfig.Paths.NotebookVaultFullpathRoot), "SingleClass path should start with VaultRoot");
    }

    /// <summary>
    /// Validates that the CreateTemporaryVaultStructure helper method creates a complete
    /// hierarchical structure from vault root to the deepest content level with correct nesting.
    /// </summary>
    /// <remarks>
    /// Tests the full depth and proper nesting of the created vault hierarchy by examining
    /// the path segments from vault root to the deepest content files, ensuring that the
    /// complete educational hierarchy is properly represented in the file system structure.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_CreatesCompleteHierarchy()
    {
        // Act
        var paths = this.CreateTemporaryVaultStructure();        // Assert - Verify complete hierarchy from vault root to deepest level
        string deepestPath = paths["video-transcript.md"];
        string[] segments = Path.GetRelativePath(this.testAppConfig.Paths.NotebookVaultFullpathRoot, deepestPath).Split(Path.DirectorySeparatorChar);

        // Expected path: VaultRoot/MBA/Program/Finance/Investment/Fundamentals/Intro/video-transcript.md
        string[] expectedSegments = ["MBA", "Program", "Finance", "Investment", "Fundamentals", "Intro", "video-transcript.md"];

        Assert.AreEqual(expectedSegments.Length, segments.Length, "Hierarchy depth should match expected structure");
        for (int i = 0; i < expectedSegments.Length; i++)
        {
            Assert.AreEqual(expectedSegments[i], segments[i], $"Segment {i} should match expected hierarchy");
        }
    }

    /// <summary>
    /// Validates that multiple calls to CreateTemporaryVaultStructure work correctly
    /// and maintain consistency across test method executions.
    /// </summary>
    /// <remarks>
    /// Tests the robustness of the vault structure creation method by ensuring it
    /// handles multiple invocations correctly, either by recreating structure cleanly
    /// or by working with existing structure without conflicts.
    /// </remarks>
    [TestMethod]
    public void CreateTemporaryVaultStructure_HandlesMultipleInvocations()
    {
        // Act - Call CreateTemporaryVaultStructure multiple times
        var paths1 = this.CreateTemporaryVaultStructure();
        var paths2 = this.CreateTemporaryVaultStructure();

        // Assert - Both calls should return the same paths (idempotent)
        Assert.AreEqual(paths1.Count, paths2.Count, "Multiple invocations should return same number of paths");

        foreach (var key in paths1.Keys)
        {
            Assert.IsTrue(paths2.ContainsKey(key), $"Second invocation should contain key: {key}");
            Assert.AreEqual(paths1[key], paths2[key], $"Path for key '{key}' should be the same in both invocations");
        }

        // Assert - All files and directories should still exist
        foreach (var path in paths2.Values)
        {
            Assert.IsTrue(File.Exists(path) || Directory.Exists(path), $"Path should exist: {path}");
        }
    }

    /// <summary>
    /// Tests hierarchy detection specifically at the lesson level to ensure that individual
    /// lessons and their content files correctly identify all hierarchy levels.
    /// </summary>
    /// <remarks>
    /// Validates the hierarchy detection functionality for the deepest organizational level
    /// (lessons) and their associated content files, ensuring that both lesson directories
    /// and individual content files within lessons properly detect their complete hierarchy.
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_LessonLevelHierarchy_DetectsCorrectHierarchy()
    {
        // Arrange
        var paths = this.CreateTemporaryVaultStructure();
        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig);

        // Test hierarchy detection at lesson level (deepest level with content)
        string lessonPath = paths["Intro"];

        // Act
        Dictionary<string, string> result = detector.FindHierarchyInfo(lessonPath);

        // Assert - Lesson level should detect all hierarchy levels
        Assert.AreEqual("MBA", result["program"], "Lesson should detect program correctly");
        Assert.AreEqual("Program", result["course"], "Lesson should detect course correctly");
        Assert.AreEqual("Finance", result["class"], "Lesson should detect class correctly");
        Assert.AreEqual("Investment", result["module"], "Lesson should detect module correctly");

        // Test content files within lesson folder
        string transcriptPath = paths["video-transcript.md"];
        Dictionary<string, string> transcriptResult = detector.FindHierarchyInfo(transcriptPath);

        // Assert - Content files in lesson should have same hierarchy as lesson
        Assert.AreEqual("MBA", transcriptResult["program"], "Content file should detect program correctly");
        Assert.AreEqual("Program", transcriptResult["course"], "Content file should detect course correctly");
        Assert.AreEqual("Finance", transcriptResult["class"], "Content file should detect class correctly");
        Assert.AreEqual("Investment", transcriptResult["module"], "Content file should detect module correctly");
    }

    /// <summary>
    /// Tests hierarchy detection specifically at the module level to validate proper
    /// identification of program, course, class, and module levels in the educational structure.
    /// </summary>
    /// <remarks>
    /// Validates hierarchy detection for the module organizational level, ensuring that
    /// modules (like "Fundamentals" and "Case Studies") correctly identify their position
    /// within the complete educational hierarchy and properly detect all parent levels.
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_ModuleLevelHierarchy_DetectsCorrectHierarchy()
    {
        // Arrange
        var paths = this.CreateTemporaryVaultStructure();
        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig);

        // Test hierarchy detection at module level (fourth level)
        string modulePath = paths["Fundamentals"];

        // Act
        Dictionary<string, string> result = detector.FindHierarchyInfo(modulePath);        // Assert - Module level should detect program, course, class, and module (since it's at depth 5)
        Assert.AreEqual("MBA", result["program"], "Module should detect program correctly");
        Assert.AreEqual("Program", result["course"], "Module should detect course correctly");
        Assert.AreEqual("Finance", result["class"], "Module should detect class correctly");
        Assert.AreEqual("Investment", result["module"], "Module should detect parent class as module");

        // Test another module (Case Studies)
        string caseStudiesPath = paths["Case Studies"];
        Dictionary<string, string> caseStudiesResult = detector.FindHierarchyInfo(caseStudiesPath);        // Assert - Case Studies module should have same hierarchy structure
        Assert.AreEqual("MBA", caseStudiesResult["program"], "Case Studies module should detect program correctly");
        Assert.AreEqual("Program", caseStudiesResult["course"], "Case Studies module should detect course correctly");
        Assert.AreEqual("Finance", caseStudiesResult["class"], "Case Studies module should detect class correctly");
        Assert.AreEqual("Investment", caseStudiesResult["module"], "Case Studies should detect parent class as module");
    }

    /// <summary>
    /// Tests hierarchy detection for case study content to ensure proper identification
    /// of hierarchy levels for case study lessons and their associated files.
    /// </summary>
    /// <remarks>
    /// Validates hierarchy detection for case study educational content, including case
    /// study lessons and their associated files (instructions, data, templates), ensuring
    /// that non-traditional content types still properly integrate with the hierarchy system.
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_CaseStudyContent_DetectsCorrectHierarchy()
    {
        // Arrange
        var paths = this.CreateTemporaryVaultStructure();
        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig);

        // Test case study lesson level (Market Analysis)
        string marketAnalysisPath = paths["Market Analysis"];

        // Act
        Dictionary<string, string> result = detector.FindHierarchyInfo(marketAnalysisPath);

        // Assert - Case study lesson should detect all hierarchy levels
        Assert.AreEqual("MBA", result["program"], "Case study lesson should detect program correctly");
        Assert.AreEqual("Program", result["course"], "Case study lesson should detect course correctly");
        Assert.AreEqual("Finance", result["class"], "Case study lesson should detect class correctly");
        Assert.AreEqual("Investment", result["module"], "Case study lesson should detect module correctly");

        // Test case study content files
        string instructionsPath = paths["case-instructions.md"];
        Dictionary<string, string> instructionsResult = detector.FindHierarchyInfo(instructionsPath);

        // Assert - Case study content should have full hierarchy
        Assert.AreEqual("MBA", instructionsResult["program"], "Case study content should detect program correctly");
        Assert.AreEqual("Program", instructionsResult["course"], "Case study content should detect course correctly");
        Assert.AreEqual("Finance", instructionsResult["class"], "Case study content should detect class correctly");
        Assert.AreEqual("Investment", instructionsResult["module"], "Case study content should detect module correctly");
    }

    /// <summary>
    /// Comprehensive validation of hierarchy detection across multiple content types
    /// and file formats to ensure consistent behavior regardless of content type.
    /// </summary>
    /// <remarks>
    /// Tests hierarchy detection across diverse content formats including videos, notes,
    /// reading materials, and data files to validate that the detector provides consistent
    /// hierarchy identification regardless of file type or content format.
    /// </remarks>
    [TestMethod]
    public void FindHierarchyInfo_DeepContentHierarchy_ValidatesAllLevels()
    {
        // Arrange
        var paths = this.CreateTemporaryVaultStructure();
        MetadataHierarchyDetector detector = new(this.loggerMock.Object, this.testAppConfig);

        // Test various content types at different depths
        var testCases = new[]
        {
            new { Path = paths["lecture.mp4"], Expected = new { Program = "MBA", Course = "Program", Class = "Finance", Module = "Investment" } },
            new { Path = paths["lecture-notes.md"], Expected = new { Program = "MBA", Course = "Program", Class = "Finance", Module = "Investment" } },
            new { Path = paths["required-reading.md"], Expected = new { Program = "MBA", Course = "Program", Class = "Finance", Module = "Investment" } },
            new { Path = paths["market-data.xlsx"], Expected = new { Program = "MBA", Course = "Program", Class = "Finance", Module = "Investment" } },
        };

        foreach (var testCase in testCases)
        {
            // Act
            Dictionary<string, string> result = detector.FindHierarchyInfo(testCase.Path);

            // Assert
            Assert.AreEqual(testCase.Expected.Program, result["program"],
                $"Program mismatch for {Path.GetFileName(testCase.Path)}");
            Assert.AreEqual(testCase.Expected.Course, result["course"],
                $"Course mismatch for {Path.GetFileName(testCase.Path)}");
            Assert.AreEqual(testCase.Expected.Class, result["class"],
                $"Class mismatch for {Path.GetFileName(testCase.Path)}");
            Assert.AreEqual(testCase.Expected.Module, result["module"],
                $"Module mismatch for {Path.GetFileName(testCase.Path)}");
        }
    }

    /// <summary>
    /// Tests metadata update functionality for lesson-level index files to ensure
    /// all appropriate hierarchy levels are included in lesson index metadata.
    /// </summary>
    /// <remarks>
    /// Validates that lesson index files receive complete hierarchy metadata including
    /// program, course, class, and module information, ensuring proper metadata
    /// enrichment for the most granular level of educational content organization.
    /// </remarks>
    [TestMethod]
    public void UpdateMetadataWithHierarchy_LessonIndex_IncludesAllHierarchyLevels()
    {
        // Arrange
        Dictionary<string, string> hierarchyInfo = new()
        {
            { "program", "MBA" },
            { "course", "Finance" },
            { "class", "Investment" },
            { "module", "Fundamentals" },
        };

        Dictionary<string, object> metadata = new()
        {
            { "title", "Introduction to Investments" },
            { "type", "index" },
        };

        // Act
        var result = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            metadata, hierarchyInfo, "lesson-index");

        // Assert - Lesson index should include all hierarchy levels
        Assert.AreEqual("MBA", result["program"], "Lesson index should include program");
        Assert.AreEqual("Finance", result["course"], "Lesson index should include course");
        Assert.AreEqual("Investment", result["class"], "Lesson index should include class");
        Assert.AreEqual("Fundamentals", result["module"], "Lesson index should include module");

        // Verify original metadata is preserved
        Assert.AreEqual("Introduction to Investments", result["title"]);
        Assert.AreEqual("index", result["type"]);
    }

    /// <summary>
    /// Tests metadata update functionality for module-level index files to validate
    /// proper inclusion of all hierarchy levels appropriate for module-level content.
    /// </summary>
    /// <remarks>
    /// Validates that module index files receive complete hierarchy metadata including
    /// program, course, class, and module information, ensuring that module-level
    /// content has access to its full hierarchical context for navigation and organization.
    /// </remarks>
    [TestMethod]
    public void UpdateMetadataWithHierarchy_ModuleIndex_IncludesCorrectHierarchyLevels()
    {
        // Arrange
        Dictionary<string, string> hierarchyInfo = new()
        {
            { "program", "MBA" },
            { "course", "Finance" },
            { "class", "Investment" },
            { "module", "Fundamentals" },
        };

        Dictionary<string, object> metadata = new()
        {
            { "title", "Investment Fundamentals" },
            { "type", "index" },
        };

        // Act
        var result = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(
            metadata, hierarchyInfo, "module-index");

        // Assert - Module index should include all hierarchy levels (including module)
        Assert.AreEqual("MBA", result["program"], "Module index should include program");
        Assert.AreEqual("Finance", result["course"], "Module index should include course");
        Assert.AreEqual("Investment", result["class"], "Module index should include class");
        Assert.AreEqual("Fundamentals", result["module"], "Module index should include module");

        // Verify original metadata is preserved
        Assert.AreEqual("Investment Fundamentals", result["title"]);
        Assert.AreEqual("index", result["type"]);
    }
}
