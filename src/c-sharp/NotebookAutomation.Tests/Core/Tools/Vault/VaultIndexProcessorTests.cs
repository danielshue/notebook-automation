// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Tools.Vault;

using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Models;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Utils;

/// <summary>
/// Unit tests for the VaultIndexProcessor class.
/// </summary>
[TestClass]
public class VaultIndexProcessorTests
{
    private Mock<ILogger<VaultIndexProcessor>> _loggerMock = null!;
    private Mock<IMetadataTemplateManager> _templateManagerMock = null!; private Mock<IMetadataHierarchyDetector> _hierarchyDetectorMock = null!;
    private Mock<ILogger<CourseStructureExtractor>> _structureLoggerMock = null!;
    private CourseStructureExtractor _structureExtractor = null!;
    private Mock<IYamlHelper> _yamlHelperMock = null!;
    private Mock<ILogger<MarkdownNoteBuilder>> _noteBuilderLoggerMock = null!;
    private MarkdownNoteBuilder _noteBuilder = null!;
    private Mock<IVaultIndexContentGenerator> _contentGeneratorMock = null!;
    private VaultIndexProcessor _processor = null!;
    private AppConfig _appConfig = null!; [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<VaultIndexProcessor>>();
        _templateManagerMock = new();
        _structureLoggerMock = new Mock<ILogger<CourseStructureExtractor>>();
        _structureExtractor = new CourseStructureExtractor(_structureLoggerMock.Object);
        _yamlHelperMock = new();
        _noteBuilderLoggerMock = new Mock<ILogger<MarkdownNoteBuilder>>();
        _noteBuilder = new MarkdownNoteBuilder(_yamlHelperMock.Object);
        _contentGeneratorMock = new();
        _appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = "/vault/root"
            }
        };
        _hierarchyDetectorMock = new();

        // Setup default content generator behavior
        _contentGeneratorMock.Setup(x => x.GenerateIndexContentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<VaultFileInfo>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<int>()))
            .ReturnsAsync("Generated index content");

        _processor = new VaultIndexProcessor(
            _loggerMock.Object,
            _templateManagerMock.Object,
            _hierarchyDetectorMock.Object,
            _structureExtractor,
            _yamlHelperMock.Object,
            _noteBuilder,
            _appConfig,
            _contentGeneratorMock.Object,
            "/vault/root");
    }

    [TestMethod]
    public async Task GenerateIndexAsync_ReturnsFalse_WhenVaultPathAndDefaultAreEmpty()
    {
        // Arrange
        var appConfig = new AppConfig { Paths = new PathsConfig { NotebookVaultFullpathRoot = string.Empty } };
        var structureLoggerMock = new Mock<ILogger<CourseStructureExtractor>>();
        var structureExtractor = new CourseStructureExtractor(structureLoggerMock.Object); var processor = new VaultIndexProcessor(
            _loggerMock.Object,
            _templateManagerMock.Object,
            _hierarchyDetectorMock.Object,
            structureExtractor,
            _yamlHelperMock.Object,
            _noteBuilder,
            appConfig,
            _contentGeneratorMock.Object,
            string.Empty);

        // Act
        var result = await processor.GenerateIndexAsync("/folder", string.Empty);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GenerateIndexAsync_ReturnsFalse_WhenTemplateNotFound()
    {        // Arrange
        _templateManagerMock.Setup(t => t.GetTemplate(It.IsAny<string>())).Returns((Dictionary<string, object>?)null);

        // Act
        var result = await _processor.GenerateIndexAsync("/folder", "/vault/root");

        // Assert
        Assert.IsFalse(result);
    }
    [TestMethod]
    public async Task GenerateIndexAsync_ReturnsFalse_WhenFileExistsAndNotForce()
    {
        // Arrange
        _templateManagerMock.Setup(t => t.GetTemplate(It.IsAny<string>())).Returns(new Dictionary<string, object>());

        // Create a controlled test directory instead of using system temp path
        var testDir = Path.Combine(Path.GetTempPath(), "VaultIndexProcessorTest", Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        var folderName = Path.GetFileName(testDir);
        var fileName = folderName + ".md";
        var indexFilePath = Path.Combine(testDir, fileName);
        File.WriteAllText(indexFilePath, "test");

        try
        {
            // Act
            var result = await _processor.GenerateIndexAsync(testDir, "/vault/root");

            // Assert
            Assert.IsFalse(result, "Should return false when file exists and forceOverwrite is false");
        }
        finally
        {
            // Clean up test directory and all its contents
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task GenerateIndexAsync_ReturnsTrue_WhenDryRun()
    {
        // Arrange
        _templateManagerMock.Setup(t => t.GetTemplate(It.IsAny<string>())).Returns(new Dictionary<string, object>());

        // Act
        var result = await _processor.GenerateIndexAsync("/folder", "/vault/root", dryRun: true);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void DetermineTemplateType_Level1_ReturnsMain()
    {
        // Arrange
        // Processor already set up in Setup()

        // Act
        string result = _processor.DetermineTemplateType(1, "TestFolder");

        // Assert
        Assert.AreEqual("main", result);
    }

    [TestMethod]
    public void DetermineTemplateType_Level2_ReturnsProgram()
    {
        // Arrange
        // Processor already set up in Setup()

        // Act
        string result = _processor.DetermineTemplateType(2, "TestFolder");

        // Assert
        Assert.AreEqual("program", result);
    }

    [TestMethod]
    public void DetermineTemplateType_Level5WithLessonFolder_ReturnsLesson()
    {
        // Arrange
        // Processor already set up in Setup()

        // Act
        string result = _processor.DetermineTemplateType(5, "Lesson 1");

        // Assert
        Assert.AreEqual("lesson", result);
    }

    [TestMethod]
    public void DetermineTemplateType_Level4WithModuleFolder_ReturnsModule()
    {
        // Arrange
        // Processor already set up in Setup()

        // Act
        string result = _processor.DetermineTemplateType(4, "Module 1");

        // Assert
        Assert.AreEqual("module", result);
    }
}
