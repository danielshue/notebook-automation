using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

using System.Collections.Generic;
using System.IO;

namespace NotebookAutomation.Core.Tests.Utils
{
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
            var realConfig = new AppConfig();
            realConfig.Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = Path.Combine(Path.GetTempPath(), "TestVault")
            };

            // Store the real config in a field for test usage
            _testAppConfig = realConfig;
        }
        [TestMethod]
        public void FindHierarchyInfo_ValueChainManagementPath_DetectsCorrectHierarchy()
        {
            // Arrange
            var vaultRoot = _testAppConfig.Paths.NotebookVaultFullpathRoot;
            var filePath = Path.Combine(vaultRoot, "Value Chain Management", "Supply Chain", "Class 1", "video.mp4");

            // Ensure directory exists for testing
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, "test file content");

            var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

            // Act
            var result = detector.FindHierarchyInfo(filePath);

            // Assert
            Assert.AreEqual("Value Chain Management", result["program"]);
            Assert.AreEqual("Supply Chain", result["course"]);
            Assert.AreEqual("Class 1", result["class"]);

            // Cleanup
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        [TestMethod]
        public void FindHierarchyInfo_ProjectsStructurePath_DetectsCorrectHierarchy()
        {
            // Arrange
            var vaultRoot = _testAppConfig.Paths.NotebookVaultFullpathRoot;
            var filePath = Path.Combine(vaultRoot, "Value Chain Management", "01_Projects", "Supply Chain", "Project 1", "video.mp4");

            // Ensure directory exists for testing
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, "test file content");

            var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

            // Act
            var result = detector.FindHierarchyInfo(filePath);

            // Assert
            Assert.AreEqual("Value Chain Management", result["program"]);
            Assert.AreEqual("Supply Chain", result["course"]);
            Assert.AreEqual("Project 1", result["class"]);

            // Cleanup
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        [TestMethod]
        public void UpdateMetadataWithHierarchy_AddsHierarchyInfo()
        {
            // Arrange
            var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

            var metadata = new Dictionary<string, object>
            {
                { "title", "Test Video" },
                { "source_file", "c:/path/to/video.mp4" }
            };

            var hierarchyInfo = new Dictionary<string, string>
            {
                { "program", "MBA Program" },
                { "course", "Finance" },
                { "class", "Accounting 101" }
            };

            // Act
            var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo);

            // Assert
            Assert.AreEqual("MBA Program", result["program"]);
            Assert.AreEqual("Finance", result["course"]);
            Assert.AreEqual("Accounting 101", result["class"]);
        }
        [TestMethod]
        public void UpdateMetadataWithHierarchy_DoesNotOverrideExistingValues()
        {
            // Arrange
            var detector = new MetadataHierarchyDetector(_loggerMock.Object, _testAppConfig);

            var metadata = new Dictionary<string, object>
            {
                { "title", "Test Video" },
                { "source_file", "c:/path/to/video.mp4" },
                { "program", "Existing Program" },
                { "course", "Existing Course" }
            };

            var hierarchyInfo = new Dictionary<string, string>
            {
                { "program", "MBA Program" },
                { "course", "Finance" },
                { "class", "Accounting 101" }
            };

            // Act
            var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo);

            // Assert
            Assert.AreEqual("Existing Program", result["program"]);
            Assert.AreEqual("Existing Course", result["course"]);
            Assert.AreEqual("Accounting 101", result["class"]);
        }
    }
}
