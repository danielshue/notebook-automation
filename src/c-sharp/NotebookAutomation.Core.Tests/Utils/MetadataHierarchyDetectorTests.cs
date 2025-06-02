using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Utils;

[TestClass]
public class MetadataHierarchyDetectorTests
{
    private Mock<ILogger> _loggerMock;
    private Mock<AppConfig> _appConfigMock;
    private AppConfig _testAppConfig; [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger>();

        // Create a real AppConfig instance instead of mocking it
        _appConfigMock = new Mock<AppConfig>();

        // Create the real config and set it up
        AppConfig realConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = Path.Combine(Path.GetTempPath(), "TestVault")
            }
        };

        // Store the real config in a field for test usage
        _testAppConfig = realConfig;
    }
    [TestMethod]
    public void FindHierarchyInfo_ValueChainManagementPath_DetectsCorrectHierarchy()
    {
        // Arrange
        string vaultRoot = _testAppConfig.Paths.NotebookVaultFullpathRoot;
        string filePath = Path.Combine(vaultRoot, "Value Chain Management", "Supply Chain", "Class 1", "video.mp4");

        // Ensure directory exists for testing
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, "test file content");

        MetadataHierarchyDetector detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        // Act
        Dictionary<string, string> result = detector.FindHierarchyInfo(filePath);

        // Assert
        Assert.AreEqual("Value Chain Management", result["program"]);
        Assert.AreEqual("Supply Chain", result["course"]);
        Assert.AreEqual("Class 1", result["class"]);

        // Cleanup
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
    [TestMethod]
    public void FindHierarchyInfo_ProjectsStructurePath_DetectsCorrectHierarchy()
    {
        // Arrange
        string vaultRoot = _testAppConfig.Paths.NotebookVaultFullpathRoot;
        string filePath = Path.Combine(vaultRoot, "Value Chain Management", "01_Projects", "Supply Chain", "Project 1", "video.mp4");

        // Ensure directory exists for testing
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, "test file content");

        MetadataHierarchyDetector detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

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
    [TestMethod]
    public void UpdateMetadataWithHierarchy_AddsHierarchyInfo()
    {
        // Arrange
        MetadataHierarchyDetector detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "title", "Test Video" },
            { "source_file", "c:/path/to/video.mp4" }
        };

        Dictionary<string, string> hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "MBA Program" },
            { "course", "Finance" },
            { "class", "Accounting 101" }
        };

        // Act
        Dictionary<string, object> result = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo);

        // Assert
        Assert.AreEqual("MBA Program", result["program"]);
        Assert.AreEqual("Finance", result["course"]);
        Assert.AreEqual("Accounting 101", result["class"]);
    }
    [TestMethod]
    public void UpdateMetadataWithHierarchy_DoesNotOverrideExistingValues()
    {
        // Arrange
        MetadataHierarchyDetector detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "title", "Test Video" },
            { "source_file", "c:/path/to/video.mp4" },
            { "program", "Existing Program" },
            { "course", "Existing Course" }
        };

        Dictionary<string, string> hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "MBA Program" },
            { "course", "Finance" },
            { "class", "Accounting 101" }
        };

        // Act
        Dictionary<string, object> result = MetadataHierarchyDetector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo);

        // Assert
        Assert.AreEqual("Existing Program", result["program"]);
        Assert.AreEqual("Existing Course", result["course"]);
        Assert.AreEqual("Accounting 101", result["class"]);
    }
}
