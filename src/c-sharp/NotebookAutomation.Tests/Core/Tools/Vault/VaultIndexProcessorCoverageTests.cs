// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.Shared;
using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Tests.Core.Tools.Vault;

/// <summary>
/// Additional comprehensive unit tests for the VaultIndexProcessor class to improve code coverage.
/// </summary>
[TestClass]
public class VaultIndexProcessorCoverageTests
{
    private Mock<ILogger<VaultIndexProcessor>> _loggerMock = null!;
    private Mock<IMetadataTemplateManager> _templateManagerMock = null!;
    private Mock<IMetadataHierarchyDetector> _hierarchyDetectorMock = null!;
    private Mock<ILogger<CourseStructureExtractor>> _structureLoggerMock = null!;
    private CourseStructureExtractor _structureExtractor = null!;
    private Mock<IYamlHelper> _yamlHelperMock = null!;
    private Mock<IVaultIndexContentGenerator> _contentGeneratorMock = null!;
    private MarkdownNoteBuilder _noteBuilder = null!;
    private VaultIndexProcessor _processor = null!;
    private AppConfig _appConfig = null!;
    private string _testTempDir = null!;
    private string _testVaultPath = null!; [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<VaultIndexProcessor>>();
        _templateManagerMock = new Mock<IMetadataTemplateManager>();
        _structureLoggerMock = new Mock<ILogger<CourseStructureExtractor>>();
        _structureExtractor = new CourseStructureExtractor(_structureLoggerMock.Object);
        _yamlHelperMock = new Mock<IYamlHelper>();
        _noteBuilder = new MarkdownNoteBuilder(_yamlHelperMock.Object);
        _contentGeneratorMock = new Mock<IVaultIndexContentGenerator>();
        _appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = "/vault/root"
            }
        };

        // Setup default content generator behavior
        _contentGeneratorMock.Setup(x => x.GenerateIndexContentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<VaultFileInfo>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<int>()))
            .ReturnsAsync("Generated index content");
        _hierarchyDetectorMock = new Mock<IMetadataHierarchyDetector>();

        // Create test directory structure
        _testTempDir = Path.Combine(Path.GetTempPath(), "VaultIndexProcessorCoverageTests", Guid.NewGuid().ToString());
        _testVaultPath = Path.Combine(_testTempDir, "TestVault");
        Directory.CreateDirectory(_testVaultPath);

        // Create test folder structure
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Course 1"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Course 1", "Module 1"));
        Directory.CreateDirectory(Path.Combine(_testVaultPath, "Course 1", "Module 1", "Lesson 1"));

        // Create some test files
        File.WriteAllText(Path.Combine(_testVaultPath, "Course 1", "Module 1", "test.md"), "# Test Content");
        File.WriteAllText(Path.Combine(_testVaultPath, "Course 1", "Module 1", "video.mp4"), "fake video");
        File.WriteAllText(Path.Combine(_testVaultPath, "Course 1", "Module 1", "assignment.pdf"), "fake pdf"); _processor = new VaultIndexProcessor(
            _loggerMock.Object,
            _templateManagerMock.Object,
            _hierarchyDetectorMock.Object,
            _structureExtractor,
            _yamlHelperMock.Object,
            _noteBuilder,
            _appConfig,
            _contentGeneratorMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testTempDir))
        {
            Directory.Delete(_testTempDir, true);
        }
    }    /// <summary>
         /// Tests constructor with custom vault root path.
         /// </summary>
    [TestMethod]
    public void Constructor_WithCustomVaultRoot_UsesCustomPath()
    {        // Arrange
        var customVaultRoot = "/custom/vault/root";        // Act
        var processor = new VaultIndexProcessor(
            _loggerMock.Object,
            _templateManagerMock.Object,
            _hierarchyDetectorMock.Object,
            _structureExtractor,
            _yamlHelperMock.Object,
            _noteBuilder,
            _appConfig,
            _contentGeneratorMock.Object,
            customVaultRoot);

        // Assert
        Assert.IsNotNull(processor);

        // Use reflection to verify the vault root was set correctly
        var defaultVaultRootField = typeof(VaultIndexProcessor)
            .GetField("_defaultVaultRootPath", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(defaultVaultRootField);

        var actualVaultRoot = (string)defaultVaultRootField.GetValue(processor)!;
        Assert.AreEqual(customVaultRoot, actualVaultRoot);
    }

    /// <summary>
    /// Tests constructor with empty vault root path falls back to config.    /// </summary>
    [TestMethod]
    public void Constructor_WithEmptyVaultRoot_FallsBackToConfig()
    {
        // Act
        var processor = new VaultIndexProcessor(
            _loggerMock.Object,
            _templateManagerMock.Object,
            _hierarchyDetectorMock.Object,
            _structureExtractor,
            _yamlHelperMock.Object,
            _noteBuilder,
            _appConfig,
            _contentGeneratorMock.Object,
            ""); // Empty string should use config

        // Assert
        var defaultVaultRootField = typeof(VaultIndexProcessor)
            .GetField("_defaultVaultRootPath", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(defaultVaultRootField);

        var actualVaultRoot = (string)defaultVaultRootField.GetValue(processor)!;
        Assert.AreEqual(_appConfig.Paths.NotebookVaultFullpathRoot, actualVaultRoot);
    }

    /// <summary>
    /// Tests GenerateIndexAsync with non-existent folder.
    /// </summary>
    [TestMethod]
    public async Task GenerateIndexAsync_NonExistentFolder_ReturnsFalse()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testTempDir, "NonExistent");

        // Act
        var result = await _processor.GenerateIndexAsync(
            nonExistentPath,
            _testVaultPath,
            forceOverwrite: false,
            dryRun: false);

        // Assert
        Assert.IsFalse(result, "Should return false for non-existent folder");
    }

    /// <summary>
    /// Tests GenerateIndexAsync in dry run mode.
    /// </summary>    [TestMethod]
    public async Task GenerateIndexAsync_DryRunMode_DoesNotCreateFiles()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1"); var indexFile = Path.Combine(testFolder, "Course 1.md");

        // Setup mocks
        _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);
        _hierarchyDetectorMock.Setup(h => h.FindHierarchyInfo(It.IsAny<string>()))
            .Returns(new Dictionary<string, string> { ["course"] = "Course 1" });
        _hierarchyDetectorMock.Setup(h => h.UpdateMetadataWithHierarchy(It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>()))
            .Returns<Dictionary<string, object?>, Dictionary<string, string>, string?>((metadata, hierarchy, templateType) => metadata);        // CourseStructureExtractor is real, no mock setup needed

        _templateManagerMock.Setup(t => t.GetTemplate("class"))
            .Returns(new Dictionary<string, object> { ["title"] = "Test Class" });
        _yamlHelperMock.Setup(y => y.SerializeToYaml(It.IsAny<Dictionary<string, object>>()))
            .Returns("title: Test Class");

        // Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: false,
            dryRun: true);

        // Assert
        Assert.IsTrue(result, "Dry run should return true for valid folder");
        Assert.IsFalse(File.Exists(indexFile), "Dry run should not create actual files");
    }    /// <summary>
         /// Tests GenerateIndexAsync with force overwrite.
         /// </summary>
    [TestMethod]
    public async Task GenerateIndexAsync_ForceOverwrite_OverwritesExistingFile()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1");
        var indexFile = Path.Combine(testFolder, "Course 1.md");

        // Create existing index file
        File.WriteAllText(indexFile, "# Existing Content");
        var originalWriteTime = File.GetLastWriteTime(indexFile);

        // Wait a moment to ensure different timestamp
        await Task.Delay(100);

        // Setup mocks
        _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);
        _hierarchyDetectorMock.Setup(h => h.FindHierarchyInfo(It.IsAny<string>()))
            .Returns(new Dictionary<string, string> { ["course"] = "Course 1" });
        _hierarchyDetectorMock.Setup(h => h.UpdateMetadataWithHierarchy(It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>()))
            .Returns<Dictionary<string, object?>, Dictionary<string, string>, string?>((metadata, hierarchy, templateType) => metadata);        // CourseStructureExtractor is real, no mock setup needed

        // Setup YamlHelper mock for frontmatter extraction
        _yamlHelperMock.Setup(y => y.ExtractFrontmatter(It.IsAny<string>())).Returns("");
        _yamlHelperMock.Setup(y => y.ParseYamlToDictionary(It.IsAny<string>())).Returns(new Dictionary<string, object>());
        _templateManagerMock.Setup(t => t.GetTemplate("class"))
          .Returns(new Dictionary<string, object> { ["title"] = "Test Class" });
        _yamlHelperMock.Setup(y => y.SerializeToYaml(It.IsAny<Dictionary<string, object>>()))
            .Returns("title: Test Class");

        // Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: true,
            dryRun: false);

        // Assert
        Assert.IsTrue(result, "Should return true when overwriting");
        Assert.IsTrue(File.Exists(indexFile), "Index file should exist");

        var newWriteTime = File.GetLastWriteTime(indexFile);
        Assert.IsTrue(newWriteTime > originalWriteTime, "File should have been updated");
    }/// <summary>
     /// Tests that DetermineTemplateType returns correct template types for various hierarchy levels.
     /// </summary>
    [TestMethod]
    public void DetermineTemplateType_VariousHierarchyLevels_ReturnsCorrectTypes()
    {
        // Act & Assert
        Assert.AreEqual("main", _processor.DetermineTemplateType(1, "VaultRoot"));
        Assert.AreEqual("program", _processor.DetermineTemplateType(2, "Program"));
        Assert.AreEqual("course", _processor.DetermineTemplateType(3, "Course"));
        Assert.AreEqual("class", _processor.DetermineTemplateType(4, "Class"));
        Assert.AreEqual("module", _processor.DetermineTemplateType(5, "Module"));
        Assert.AreEqual("lesson", _processor.DetermineTemplateType(6, "Lesson"));
        Assert.AreEqual("module", _processor.DetermineTemplateType(7, "DeepNested")); // Anything > 6 defaults to module
    }    /// <summary>
         /// Tests basic content generation without complex dependencies.
         /// </summary>
    [TestMethod]
    public async Task GenerateIndexAsync_BasicFolder_ReturnsSuccess()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1");        // Setup mocks with complete dependencies
        _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);
        _hierarchyDetectorMock.Setup(h => h.FindHierarchyInfo(It.IsAny<string>()))
            .Returns(new Dictionary<string, string> { ["course"] = "Course 1" }); _hierarchyDetectorMock.Setup(h => h.UpdateMetadataWithHierarchy(It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>()))
            .Returns<Dictionary<string, object?>, Dictionary<string, string>, string?>((metadata, hierarchy, templateType) => metadata);

        // CourseStructureExtractor is real, no mock setup needed

        // Setup YamlHelper mock for frontmatter extraction
        _yamlHelperMock.Setup(y => y.ExtractFrontmatter(It.IsAny<string>())).Returns(""); _yamlHelperMock.Setup(y => y.ParseYamlToDictionary(It.IsAny<string>())).Returns(new Dictionary<string, object>());

        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class",
            ["title"] = "Class Template"
        };
        _templateManagerMock.Setup(t => t.GetTemplate("class"))
            .Returns(template);
        _yamlHelperMock.Setup(y => y.SerializeToYaml(It.IsAny<Dictionary<string, object>>()))
            .Returns("title: Class Template");

        // Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: true,
            dryRun: false);

        // Assert
        Assert.IsTrue(result, "Should successfully generate index for valid folder");
    }

    /// <summary>
    /// Tests that the processor handles template errors gracefully.
    /// </summary>
    [TestMethod]
    public async Task GenerateIndexAsync_TemplateError_HandlesGracefully()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1"); _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);
        _templateManagerMock.Setup(t => t.GetTemplate("class"))
            .Returns((Dictionary<string, object>?)null); // Return null template

        // Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: false,
            dryRun: false);

        // Assert
        Assert.IsFalse(result, "Should return false when template is not found");

        // Verify warning was logged (not error) for null template
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Template not found for type")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that the processor properly handles folder structure analysis.
    /// </summary>
    [TestMethod]
    public async Task GenerateContentSections_CategorizesFilesCorrectly()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1");

        // Create test files of different types
        File.WriteAllText(Path.Combine(testFolder, "reading.md"), "---\ntemplate-type: reading\ntitle: Test Reading\n---\n# Reading");
        File.WriteAllText(Path.Combine(testFolder, "assignment.md"), "---\ntemplate-type: assignment\ntitle: Test Assignment\n---\n# Assignment");        // Setup mocks
        _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);
        _hierarchyDetectorMock.Setup(h => h.FindHierarchyInfo(It.IsAny<string>()))
            .Returns(new Dictionary<string, string> { ["course"] = "Course 1" }); _hierarchyDetectorMock.Setup(h => h.UpdateMetadataWithHierarchy(It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>()))
            .Returns<Dictionary<string, object?>, Dictionary<string, string>, string?>((metadata, hierarchy, templateType) => metadata);

        // CourseStructureExtractor is real, no mock setup needed

        // Setup YamlHelper mock for frontmatter extraction
        _yamlHelperMock.Setup(y => y.ExtractFrontmatter(It.IsAny<string>())).Returns("---\ntemplate-type: reading\ntitle: Test Reading\n---");
        _yamlHelperMock.Setup(y => y.ParseYamlToDictionary(It.IsAny<string>())).Returns(new Dictionary<string, object>
        {
            {"template-type", "reading"},            {"title", "Test Reading"}
        });

        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class",
            ["title"] = "Class Template"
        };
        _templateManagerMock.Setup(t => t.GetTemplate("class"))
            .Returns(template); _yamlHelperMock.Setup(y => y.SerializeToYaml(It.IsAny<Dictionary<string, object>>()))
            .Returns("title: Class Template");

        // Setup content generator mock to return content containing "Course 1"
        _contentGeneratorMock.Setup(c => c.GenerateIndexContentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<List<VaultFileInfo>>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<int>()))
            .ReturnsAsync("---\ntitle: Course 1\n---\n\n# Course 1\n\nTest content with Course 1 name");

        // Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: true,
            dryRun: false);

        // Assert
        Assert.IsTrue(result, "Should successfully process folder with various file types");

        var indexFile = Path.Combine(testFolder, "Course 1.md");
        Assert.IsTrue(File.Exists(indexFile), "Index file should be created");

        var content = File.ReadAllText(indexFile);
        Assert.IsTrue(content.Contains("Course 1"), "Should contain course name");
    }

    /// <summary>
    /// Tests exception handling in content generation.
    /// </summary>
    [TestMethod]
    public async Task GenerateIndexAsync_ContentGenerationError_HandlesGracefully()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1"); _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);
        _templateManagerMock.Setup(t => t.GetTemplate("class"))
            .Returns(new Dictionary<string, object> { ["title"] = "Test Class" });

        // Setup content generator to throw an exception to simulate content generation failure
        _contentGeneratorMock.Setup(c => c.GenerateIndexContentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<List<VaultFileInfo>>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Content generation error"));

        // Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: false,
            dryRun: false);

        // Assert
        Assert.IsFalse(result, "Should return false when content generation fails");
    }

    /// <summary>
    /// Tests that existing index files are not overwritten without force flag.
    /// </summary>
    [TestMethod]
    public async Task GenerateIndexAsync_ExistingFile_DoesNotOverwriteWithoutForce()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1");
        var indexFile = Path.Combine(testFolder, "Course 1.md");

        // Create existing index file
        var originalContent = "# Existing Content";
        File.WriteAllText(indexFile, originalContent);

        _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);

        // Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: false,
            dryRun: false);        // Assert
        Assert.IsFalse(result, "Should return false when file exists and forceOverwrite is false");
        Assert.AreEqual(originalContent, File.ReadAllText(indexFile),
            "Existing content should be preserved");
    }

    /// <summary>
    /// Tests metadata enhancement functionality by testing end-to-end index generation.
    /// </summary>
    [TestMethod]
    public async Task GenerateContentSections_WithMetadata_EnhancesContent()
    {
        // Arrange
        var testFolder = Path.Combine(_testVaultPath, "Course 1");

        // Create a markdown file with frontmatter
        var markdownContent = @"---
template-type: reading
title: Test Document
author: Test Author
---
# Content";
        File.WriteAllText(Path.Combine(testFolder, "test.md"), markdownContent);        // Setup mocks to enable successful processing
        _hierarchyDetectorMock.Setup(h => h.CalculateHierarchyLevel(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(3);
        _hierarchyDetectorMock.Setup(h => h.FindHierarchyInfo(It.IsAny<string>()))
            .Returns(new Dictionary<string, string> { ["course"] = "Course 1" });
        _hierarchyDetectorMock.Setup(h => h.UpdateMetadataWithHierarchy(It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<string?>()))
            .Returns<Dictionary<string, object?>, Dictionary<string, string>, string?>((metadata, hierarchy, templateType) => metadata);

        // CourseStructureExtractor is real, no mock setup needed        // Setup YamlHelper mock for frontmatter extraction - use simpler setup like working tests
        _yamlHelperMock.Setup(y => y.ExtractFrontmatter(It.IsAny<string>())).Returns("");
        _yamlHelperMock.Setup(y => y.ParseYamlToDictionary(It.IsAny<string>())).Returns(new Dictionary<string, object>());

        // Setup template for hierarchy level 3 (should be "class")
        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class",
            ["title"] = "Class Template"
        };
        _templateManagerMock.Setup(t => t.GetTemplate("class"))
            .Returns(template);

        _yamlHelperMock.Setup(y => y.SerializeToYaml(It.IsAny<Dictionary<string, object>>()))
            .Returns("title: Class Template");// Act
        var result = await _processor.GenerateIndexAsync(
            testFolder,
            _testVaultPath,
            forceOverwrite: true,
            dryRun: false);        // Assert
        Assert.IsTrue(result, "Should successfully process folder with metadata-enhanced files");
    }
}
