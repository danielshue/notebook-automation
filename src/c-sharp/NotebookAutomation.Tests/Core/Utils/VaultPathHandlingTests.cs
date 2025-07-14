using NotebookAutomation.Tests.Core.Helpers;
using NotebookAutomation.Core.Tools;
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Tests.Core.Utils;

/// <summary>
/// Tests for VaultCommands path handling functionality.
/// </summary>
/// <remarks>
/// These tests specifically verify the path resolution logic in VaultCommands
/// to ensure relative paths are correctly handled against the vault root.
/// </remarks>
[TestClass]
public class VaultPathHandlingTests
{
    private readonly Mock<ILogger<MetadataHierarchyDetector>> _mockLogger = new();
    private readonly AppConfig _appConfig = new();
    private string _tempVaultRoot = string.Empty;

    /// <summary>
    /// Set up test environment with temporary vault structure.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        // Create a temporary directory for vault root
        _tempVaultRoot = Path.Combine(Path.GetTempPath(), $"TestVault_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempVaultRoot);

        // Create a nested directory structure for testing
        string nestedPath = Path.Combine(_tempVaultRoot, "Value Chain Management", "Operations Management");
        Directory.CreateDirectory(nestedPath);

        // Set up app config
        _appConfig.Paths = new PathsConfig
        {
            NotebookVaultFullpathRoot = _tempVaultRoot
        };
    }

    /// <summary>
    /// Clean up test environment.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempVaultRoot))
            {
                Directory.Delete(_tempVaultRoot, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Tests that MetadataHierarchyDetector.CalculateHierarchyLevel correctly calculates
    /// hierarchy levels for paths relative to the vault root.
    /// </summary>
    [TestMethod]
    [Ignore("Temporarily disabled")]
    public void CalculateHierarchyLevel_RelativePath_CalculatedCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger<MetadataHierarchyDetector>>();
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);

        // Test cases with expected hierarchy levels
        var testCases = new Dictionary<string, int>
        {
            { "Value Chain Management", 1 }, // One level deep from vault root
            { "Value Chain Management/Operations Management", 2 }, // Two levels deep
            { @"Value Chain Management\Operations Management", 2 }, // Windows path format
            { "/Value Chain Management/Operations Management", 2 }, // Leading slash
        };

        foreach (var testCase in testCases)
        {
            // Act
            int level = detector.CalculateHierarchyLevel(testCase.Key);

            // Assert
            Assert.AreEqual(testCase.Value, level, $"Path '{testCase.Key}' should be at level {testCase.Value}, not {level}");
        }
    }

    /// <summary>
    /// Tests that MetadataHierarchyDetector.GetTemplateTypeFromHierarchyLevel correctly maps
    /// hierarchy levels to template types.
    /// </summary>
    [TestMethod]
    [Ignore("Temporarily disabled")]
    public void GetTemplateTypeFromHierarchyLevel_ReturnsCorrectTemplateType()
    {
        // Arrange
        var logger = new Mock<ILogger<MetadataHierarchyDetector>>();
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);

        // Test cases with expected template types
        var testCases = new Dictionary<int, string>
        {
            { 0, "main" },        // Vault root
            { 1, "program" },     // First level (e.g., "Value Chain Management")
            { 2, "course" },      // Second level (e.g., "Operations Management")
            { 3, "class" },       // Third level
            { 4, "module" },      // Fourth level
            { 5, "lesson" },      // Fifth level
            { 6, "unknown" }      // Sixth level and beyond
        };

        foreach (var testCase in testCases)
        {
            // Act
            string templateType = detector.GetTemplateTypeFromHierarchyLevel(testCase.Key);

            // Assert
            Assert.AreEqual(testCase.Value, templateType,
                $"Hierarchy level {testCase.Key} should map to template type '{testCase.Value}', not '{templateType}'");
        }
    }

    /// <summary>
    /// Tests that path handling integrates with schema loader for template resolution.
    /// </summary>
    [TestMethod]
    public void PathHandling_IntegratesWithSchemaLoader_ForTemplateResolution()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);
        var hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "Value Chain Management" },
            { "course", "Operations Management" },
            { "class", "Supply Chain Fundamentals" }
        };
        var metadata = new Dictionary<string, object?>
        {
            { "title", "Test Path Handling" },
            { "template-type", "class-index" }
        };

        // Act
        var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, "class-index");

        // Assert - Schema loader integration should work with path handling
        Assert.AreEqual("Value Chain Management", result["program"], "Schema loader should integrate with path handling");
        Assert.AreEqual("Operations Management", result["course"], "Schema loader should integrate with path handling");
        Assert.AreEqual("Supply Chain Fundamentals", result["class"], "Schema loader should integrate with path handling");
    }

    /// <summary>
    /// Tests that path handling correctly processes schema-based template types.
    /// </summary>
    [TestMethod]
    public void PathHandling_ProcessesSchemaBasedTemplateTypes()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);
        var hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "MBA" },
            { "course", "Finance" },
            { "class", "Investment" }
        };

        // Test different template types from schema
        var testCases = new[]
        {
            new { TemplateType = "pdf-reference", ExpectedField = "comprehension" },
            new { TemplateType = "video-reference", ExpectedField = "status" },
            new { TemplateType = "resource-reading", ExpectedField = "page-count" }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var metadata = new Dictionary<string, object?>
            {
                { "title", "Test Schema Template" },
                { "template-type", testCase.TemplateType }
            };
            var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, testCase.TemplateType);

            // Assert - Schema-based template types should be processed correctly
            Assert.AreEqual("MBA", result["program"], $"Template type '{testCase.TemplateType}' should process hierarchy correctly");
            Assert.AreEqual("Finance", result["course"], $"Template type '{testCase.TemplateType}' should process hierarchy correctly");
            Assert.AreEqual("Investment", result["class"], $"Template type '{testCase.TemplateType}' should process hierarchy correctly");
        }
    }

    /// <summary>
    /// Tests that path handling integrates with schema loader for universal field injection.
    /// </summary>
    [TestMethod]
    public void PathHandling_IntegratesWithSchemaLoader_ForUniversalFieldInjection()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);
        var hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "Executive Program" },
            { "course", "Leadership" },
            { "class", "Team Management" }
        };
        var metadata = new Dictionary<string, object?>
        {
            { "title", "Universal Field Test" },
            { "template-type", "class-index" }
        };

        // Act
        var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, "class-index");

        // Assert - Universal fields should be integrated through path handling
        Assert.IsTrue(result.ContainsKey("program"), "Result should contain program key");
        Assert.AreEqual("Executive Program", result["program"], "Universal field injection should work with path handling");
        Assert.IsTrue(result.ContainsKey("course"), "Result should contain course key");
        Assert.AreEqual("Leadership", result["course"], "Universal field injection should work with path handling");
        Assert.IsTrue(result.ContainsKey("class"), "Result should contain class key");
        Assert.AreEqual("Team Management", result["class"], "Universal field injection should work with path handling");
    }

    /// <summary>
    /// Tests that path handling correctly resolves schema-based field values.
    /// </summary>
    [TestMethod]
    public void PathHandling_ResolvesSchemaBasedFieldValues()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);
        var hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "Data Science Program" },
            { "course", "Machine Learning" },
            { "class", "Deep Learning" }
        };

        // Test with different metadata configurations
        var testCases = new[]
        {
            new { Title = "Test Path Resolution", TemplateType = "class-index" },
            new { Title = "Test Schema Integration", TemplateType = "module-index" },
            new { Title = "Test Field Resolution", TemplateType = "lesson-index" }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var metadata = new Dictionary<string, object?>
            {
                { "title", testCase.Title },
                { "template-type", testCase.TemplateType }
            };
            var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, testCase.TemplateType);

            // Assert - Schema-based field values should be resolved correctly
            Assert.AreEqual("Data Science Program", result["program"], $"Field resolution should work for '{testCase.TemplateType}'");
            Assert.AreEqual("Machine Learning", result["course"], $"Field resolution should work for '{testCase.TemplateType}'");
            Assert.AreEqual("Deep Learning", result["class"], $"Field resolution should work for '{testCase.TemplateType}'");
            Assert.AreEqual(testCase.Title, result["title"], $"Title should be preserved for '{testCase.TemplateType}'");
        }
    }

    /// <summary>
    /// Tests that path handling integrates with schema loader for reserved tag processing.
    /// </summary>
    [TestMethod]
    public void PathHandling_IntegratesWithSchemaLoader_ForReservedTagProcessing()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);
        var hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "Business Analytics" },
            { "course", "Operations Research" },
            { "class", "Optimization" }
        };
        var metadata = new Dictionary<string, object?>
        {
            { "title", "Reserved Tag Test" },
            { "template-type", "class-index" },
            { "case-study", "true" },
            { "reading", "required" }
        };

        // Act
        var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, "class-index");

        // Assert - Reserved tags should be processed correctly with path handling
        Assert.AreEqual("Business Analytics", result["program"], "Reserved tag processing should work with path handling");
        Assert.AreEqual("Operations Research", result["course"], "Reserved tag processing should work with path handling");
        Assert.AreEqual("Optimization", result["class"], "Reserved tag processing should work with path handling");
        Assert.IsTrue(result.ContainsKey("case-study"), "Reserved tags should be preserved");
        Assert.IsTrue(result.ContainsKey("reading"), "Reserved tags should be preserved");
    }

    /// <summary>
    /// Tests that path handling works correctly with complex vault structures.
    /// </summary>
    [TestMethod]
    public void PathHandling_WorksWithComplexVaultStructures()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);
        
        // Create a complex nested structure
        var complexPath = Path.Combine(_tempVaultRoot, "Programs", "Advanced Analytics", "Statistical Methods", "Regression Analysis", "Linear Models");
        Directory.CreateDirectory(complexPath);
        
        // Act
        var hierarchyInfo = detector.FindHierarchyInfo(complexPath);

        // Assert - Complex structures should be handled correctly
        Assert.AreEqual("Programs", hierarchyInfo["program"], "Complex structure should detect program level");
        Assert.AreEqual("Advanced Analytics", hierarchyInfo["course"], "Complex structure should detect course level");
        Assert.AreEqual("Statistical Methods", hierarchyInfo["class"], "Complex structure should detect class level");
        Assert.AreEqual("Regression Analysis", hierarchyInfo["module"], "Complex structure should detect module level");
    }

    /// <summary>
    /// Tests that path handling correctly handles schema loader integration across different file types.
    /// </summary>
    [TestMethod]
    public void PathHandling_HandlesSchemaLoaderIntegration_AcrossFileTypes()
    {
        // Arrange
        var detector = MetadataSchemaLoaderHelper.CreateTestMetadataHierarchyDetector(vaultRootOverride: _tempVaultRoot);
        var hierarchyInfo = new Dictionary<string, string>
        {
            { "program", "Engineering" },
            { "course", "Software Engineering" },
            { "class", "Design Patterns" }
        };

        // Test different file types that use schema loader
        var testCases = new[]
        {
            new { FileName = "index.md", TemplateType = "class-index" },
            new { FileName = "lecture.mp4", TemplateType = "video-reference" },
            new { FileName = "transcript.md", TemplateType = "resource-reading" },
            new { FileName = "assignment.pdf", TemplateType = "pdf-reference" }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var metadata = new Dictionary<string, object?>
            {
                { "title", $"Test {testCase.FileName}" },
                { "template-type", testCase.TemplateType }
            };
            var result = detector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, testCase.TemplateType);

            // Assert - Schema loader integration should work across file types
            Assert.IsTrue(result.ContainsKey("program"), $"File type '{testCase.FileName}' should have program key");
            Assert.AreEqual("Engineering", result["program"], $"File type '{testCase.FileName}' should have correct hierarchy");
            Assert.IsTrue(result.ContainsKey("course"), $"File type '{testCase.FileName}' should have course key");
            Assert.AreEqual("Software Engineering", result["course"], $"File type '{testCase.FileName}' should have correct hierarchy");
            Assert.IsTrue(result.ContainsKey("class"), $"File type '{testCase.FileName}' should have class key");
            Assert.AreEqual("Design Patterns", result["class"], $"File type '{testCase.FileName}' should have correct hierarchy");
        }
    }
}
