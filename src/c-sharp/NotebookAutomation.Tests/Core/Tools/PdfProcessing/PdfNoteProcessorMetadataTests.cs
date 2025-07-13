using NotebookAutomation.Tests.Core.Helpers;
using NotebookAutomation.Core.Tools;
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Tools.PdfProcessing;

/// <summary>
/// Unit tests for PdfNoteProcessor metadata extraction functionality.
/// These tests focus on preventing regression of the metadata hierarchy bug
/// where course and class fields were missing from PDF note metadata.
/// </summary>
[TestClass]
public class PdfNoteProcessorMetadataTests
{
    private AppConfig _testAppConfig = null!;

    [TestInitialize]
    public void Setup()
    {
        _testAppConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = Path.GetTempPath()
            }
        };
    }

    /// <summary>
    /// Verifies that the MetadataHierarchyDetector with pdf-reference template type
    /// includes all necessary hierarchy levels (program, course, class) for PDF metadata.
    /// This test prevents regression of the bug where course and class fields were missing.
    /// </summary>
    [TestMethod]
    public void UpdateMetadataWithHierarchy_WithPdfReferenceTemplate_IncludesAllHierarchyLevels()
    {
        // Arrange - Create a real MetadataHierarchyDetector to test the actual logic
        var realDetector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            _testAppConfig);

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["program"] = "MBA Program",
            ["course"] = "Operations Management",
            ["class"] = "Quality and Supply Chain",
            ["module"] = "Test Module"
        };

        var metadata = new Dictionary<string, object?>();

        // Act - Test with pdf-reference template type (should include all levels up to class)
        var result = realDetector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, "pdf-reference");        // Assert - With pdf-reference template, we should get program, course, class, AND module
        Assert.IsTrue(result.ContainsKey("program"), "Metadata should contain program field");
        Assert.IsTrue(result.ContainsKey("course"), "Metadata should contain course field");
        Assert.IsTrue(result.ContainsKey("class"), "Metadata should contain class field");

        Assert.AreEqual("MBA Program", result["program"]);
        Assert.AreEqual("Operations Management", result["course"]);
        Assert.AreEqual("Quality and Supply Chain", result["class"]);

        // pdf-reference is an unknown template type, so it gets maxLevel=4 (includes module)
        Assert.IsTrue(result.ContainsKey("module"), "Metadata should contain module field for pdf-reference template (maxLevel=4)");
        Assert.AreEqual("Test Module", result["module"]);
    }

    /// <summary>
    /// Verifies that calling UpdateMetadataWithHierarchy without a template type (the bug scenario)
    /// would result in missing hierarchy fields compared to using the correct template type.
    /// This test demonstrates the exact bug we fixed.
    /// </summary>
    [TestMethod]
    public void UpdateMetadataWithHierarchy_WithoutTemplateType_LimitsHierarchyLevels()
    {
        // Arrange - Create a real MetadataHierarchyDetector to test the actual logic
        var realDetector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            _testAppConfig);

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["program"] = "MBA Program",
            ["course"] = "Operations Management",
            ["class"] = "Quality and Supply Chain"
        };

        var metadata1 = new Dictionary<string, object?>();
        var metadata2 = new Dictionary<string, object?>();

        // Act - Compare results with and without template type
        var resultWithoutTemplate = realDetector.UpdateMetadataWithHierarchy(metadata1, hierarchyInfo, null); // Bug scenario
        var resultWithTemplate = realDetector.UpdateMetadataWithHierarchy(metadata2, hierarchyInfo, "pdf-reference"); // Fixed scenario

        // Assert - Without template type, we should only get program (maxLevel=1) - THIS WAS THE BUG
        Assert.IsTrue(resultWithoutTemplate.ContainsKey("program"), "Should contain program field");
        Assert.IsFalse(resultWithoutTemplate.ContainsKey("course"), "Should NOT contain course field without template - this was the bug!");
        Assert.IsFalse(resultWithoutTemplate.ContainsKey("class"), "Should NOT contain class field without template - this was the bug!");

        // With pdf-reference template, we should get all levels - THIS IS THE FIX
        Assert.IsTrue(resultWithTemplate.ContainsKey("program"), "Should contain program field");
        Assert.IsTrue(resultWithTemplate.ContainsKey("course"), "Should contain course field with pdf-reference template - this is the fix!");
        Assert.IsTrue(resultWithTemplate.ContainsKey("class"), "Should contain class field with pdf-reference template - this is the fix!");
    }

    /// <summary>
    /// Integration test that verifies the pdf-reference template type provides the correct hierarchy
    /// levels for typical MBA course structure (program > course > class).
    /// </summary>
    [TestMethod]
    public void PdfReferenceTemplate_ProvidesMbaHierarchyLevels()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            _testAppConfig);

        var mbaHierarchy = new Dictionary<string, string>
        {
            ["program"] = "MBA",
            ["course"] = "Value Chain Management",
            ["class"] = "Operations Management",
            ["module"] = "Quality and Supply Chain"
        };

        var metadata = new Dictionary<string, object?>();

        // Act
        var result = detector.UpdateMetadataWithHierarchy(metadata, mbaHierarchy, "pdf-reference");        // Assert - PDF references should get all levels including module (pdf-reference is unknown type, gets maxLevel=4)
        Assert.AreEqual("MBA", result["program"]);
        Assert.AreEqual("Value Chain Management", result["course"]);
        Assert.AreEqual("Operations Management", result["class"]);
        Assert.AreEqual("Quality and Supply Chain", result["module"], "PDF references should include module level (unknown template type gets maxLevel=4)");
    }
}
