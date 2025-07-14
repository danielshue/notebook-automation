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
}
