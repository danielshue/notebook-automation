// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Tests.Core.Helpers;

namespace NotebookAutomation.Tests.Core.Utils;

/// <summary>
/// Tests for the module_number field functionality in MetadataHierarchyDetector.
/// Verifies that both semantic module names and numeric module numbers are correctly extracted.
/// </summary>
[TestClass]
public class ModuleNumberFieldTests
{
    private Mock<ILogger<MetadataHierarchyDetector>> _loggerMock = default!;
    private string _vaultRoot = default!;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<MetadataHierarchyDetector>>();
        _vaultRoot = Path.Combine(Path.GetTempPath(), "ModuleNumberTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_vaultRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_vaultRoot))
        {
            Directory.Delete(_vaultRoot, true);
        }
    }

    /// <summary>
    /// Helper method to create a module directory path.
    /// </summary>
    private string CreateModulePath(string moduleName)
    {
        string programDir = Path.Combine(_vaultRoot, "TestProgram");
        string courseDir = Path.Combine(programDir, "TestCourse");
        string classDir = Path.Combine(courseDir, "TestClass");
        string moduleDir = Path.Combine(classDir, moduleName);
        Directory.CreateDirectory(moduleDir);
        return moduleDir;
    }

    /// <summary>
    /// Tests that module_number field is populated for modules with numeric prefixes.
    /// </summary>
    [TestMethod]
    public void FindHierarchyInfo_ModuleWithNumericPrefix_PopulatesModuleNumber()
    {
        // Arrange
        string moduleDir = CreateModulePath("05_operations-resilience");
        string contentFilePath = Path.Combine(moduleDir, "introduction-video.mp4");
        string nonContentFilePath = Path.Combine(moduleDir, "index.md");

        File.WriteAllText(contentFilePath, "test content");
        File.WriteAllText(nonContentFilePath, "test content");

        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _vaultRoot);

        // Act - Test content file
        var contentMetadata = detector.FindHierarchyInfo(contentFilePath);

        // Act - Test non-content file
        var nonContentMetadata = detector.FindHierarchyInfo(nonContentFilePath);        // Assert - Content file should have numeric module, numeric module_number, and semantic module_name
        Assert.IsTrue(contentMetadata.ContainsKey("module_number"), "Content file should have module_number field");
        Assert.AreEqual("05", contentMetadata["module_number"], "Module number should be extracted numeric prefix");

        Assert.IsTrue(contentMetadata.ContainsKey("module"), "Content file should have module field");
        Assert.AreEqual("05", contentMetadata["module"], "Content file module should be numeric only");

        Assert.IsTrue(contentMetadata.ContainsKey("module_name"), "Content file should have module_name field");
        Assert.AreEqual("05_operations-resilience", contentMetadata["module_name"], "Module name should be full semantic name");

        // Assert - Non-content file should have semantic module and numeric module_number
        Assert.IsTrue(nonContentMetadata.ContainsKey("module_number"), "Non-content file should have module_number field");
        Assert.AreEqual("05", nonContentMetadata["module_number"], "Module number should be extracted numeric prefix");

        Assert.IsTrue(nonContentMetadata.ContainsKey("module"), "Non-content file should have module field");
        Assert.AreEqual("05_operations-resilience", nonContentMetadata["module"], "Non-content file module should be full semantic name");

        Assert.IsFalse(nonContentMetadata.ContainsKey("module_name"), "Non-content file should not have separate module_name field");
    }

    /// <summary>
    /// Tests that module_number field is not populated when module has no numeric prefix.
    /// </summary>
    [TestMethod]
    public void FindHierarchyInfo_ModuleWithoutNumericPrefix_DoesNotPopulateModuleNumber()
    {
        // Arrange
        string moduleDir = CreateModulePath("introduction-module");
        string filePath = Path.Combine(moduleDir, "content.md");
        File.WriteAllText(filePath, "test content");

        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _vaultRoot);

        // Act
        var metadata = detector.FindHierarchyInfo(filePath);

        // Assert
        Assert.IsFalse(metadata.ContainsKey("module_number"), "Module without numeric prefix should not have module_number field");
        Assert.IsTrue(metadata.ContainsKey("module"), "Module field should still be present");
        Assert.AreEqual("introduction-module", metadata["module"], "Module should be the full name");
    }

    /// <summary>
    /// Tests various numeric prefix patterns for module_number extraction.
    /// </summary>
    [TestMethod]
    [DataRow("01_introduction", "01", DisplayName = "Standard underscore pattern")]
    [DataRow("02-advanced-topics", "02", DisplayName = "Hyphen separator pattern")]
    [DataRow("module03_basics", "03", DisplayName = "Module prefix pattern")]
    [DataRow("week05_content", "05", DisplayName = "Week prefix pattern")]
    public void FindHierarchyInfo_VariousNumericPatterns_ExtractsCorrectModuleNumber(string moduleName, string expectedNumber)
    {
        // Arrange
        string moduleDir = CreateModulePath(moduleName);
        string filePath = Path.Combine(moduleDir, "test.md");
        File.WriteAllText(filePath, "test content");

        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _vaultRoot);

        // Act
        var metadata = detector.FindHierarchyInfo(filePath);

        // Assert
        Assert.IsTrue(metadata.ContainsKey("module_number"), $"Module '{moduleName}' should have module_number field");
        Assert.AreEqual(expectedNumber, metadata["module_number"], $"Module number should be '{expectedNumber}' for pattern '{moduleName}'");
    }

    /// <summary>
    /// Tests that purely numeric module names don't create a separate module_number field.
    /// </summary>
    [TestMethod]
    public void FindHierarchyInfo_PurelyNumericModule_DoesNotCreateSeparateModuleNumber()
    {
        // Arrange
        string moduleDir = CreateModulePath("12");
        string filePath = Path.Combine(moduleDir, "test.md");
        File.WriteAllText(filePath, "test content");

        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _vaultRoot);

        // Act
        var metadata = detector.FindHierarchyInfo(filePath);

        // Assert
        Assert.IsFalse(metadata.ContainsKey("module_number"), "Purely numeric module should not have separate module_number field");
        Assert.IsTrue(metadata.ContainsKey("module"), "Module field should still be present");
        Assert.AreEqual("12", metadata["module"], "Module should be the numeric value");
    }
}
