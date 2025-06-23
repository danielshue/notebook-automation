// Copyright (c) 2025 Daniel Shue.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Tests.Core.Tools.Vault;

/// <summary>
/// Unit tests for the MetadataEnsureProcessor class.
/// Validates metadata detection and assignment for different content types.
/// </summary>
[TestClass]
public class MetadataEnsureProcessorTests
{
    private readonly ILogger<MetadataEnsureProcessor> _logger;
    private readonly IYamlHelper _yamlHelper;
    private readonly IMetadataHierarchyDetector _metadataDetector;
    private readonly ICourseStructureExtractor _structureExtractor;
    private readonly MetadataEnsureProcessor _processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataEnsureProcessorTests"/> class.
    /// </summary>

    public MetadataEnsureProcessorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<MetadataEnsureProcessor>();

        var yamlLogger = loggerFactory.CreateLogger<YamlHelper>(); // Create logger for YamlHelper
        _yamlHelper = new YamlHelper(yamlLogger);

        var detectorLogger = loggerFactory.CreateLogger<MetadataHierarchyDetector>();
        var appConfig = new AppConfig(); // Create a mock or dummy AppConfig instance
        string? optionalParameter = null; // Provide a default value for the optional parameter
        _metadataDetector = new MetadataHierarchyDetector(detectorLogger, appConfig, optionalParameter);

        var extractorLogger = loggerFactory.CreateLogger<CourseStructureExtractor>();
        _structureExtractor = new CourseStructureExtractor(extractorLogger);

        _processor = new MetadataEnsureProcessor(_logger, _yamlHelper, _metadataDetector, _structureExtractor);
    }

    /// <summary>
    /// Tests that reading files get the correct type metadata.
    /// </summary>
    [TestMethod]
    [DataRow("required-reading.md", "note/reading")]
    [DataRow("chapter-1-reading.md", "note/reading")]
    [DataRow("reading-assignment.md", "note/reading")]
    public void DetermineTemplateType_ReadingFiles_SetsCorrectType(string fileName, string expectedType)
    {
        // Arrange
        string tempDir = Path.GetTempPath();
        string filePath = Path.Combine(tempDir, fileName);

        // Create temporary file
        File.WriteAllText(filePath, "---\ntags: ''\n---\nContent");

        try
        {
            // Act
            bool result = _processor.EnsureMetadataAsync(filePath, forceOverwrite: true).Result;

            // Assert
            Assert.IsTrue(result, "File processing should succeed");

            // Read back the metadata
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains($"type: {expectedType}"), $"File should have type: {expectedType}");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// Tests that instruction files get the correct type metadata.
    /// </summary>
    [TestMethod]
    [DataRow("assignment-instructions.md", "note/instruction")]
    [DataRow("project-instructions.md", "note/instruction")]
    [DataRow("case-study-instructions.md", "note/instruction")]
    [DataRow("instruction-guide.md", "note/instruction")]
    public void DetermineTemplateType_InstructionFiles_SetsCorrectType(string fileName, string expectedType)
    {
        // Arrange
        string tempDir = Path.GetTempPath();
        string filePath = Path.Combine(tempDir, fileName);

        // Create temporary file
        File.WriteAllText(filePath, "---\ntags: ''\n---\nContent");

        try
        {
            // Act
            bool result = _processor.EnsureMetadataAsync(filePath, forceOverwrite: true).Result;

            // Assert
            Assert.IsTrue(result, "File processing should succeed");

            // Read back the metadata
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains($"type: {expectedType}"), $"File should have type: {expectedType}");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// Tests that case study files get the correct type metadata based on directory structure.
    /// </summary>
    [TestMethod]
    public void DetermineTemplateType_CaseStudyDirectory_SetsCorrectType()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), "case-studies");
        Directory.CreateDirectory(tempDir);
        string filePath = Path.Combine(tempDir, "amazon-case-study.md");

        // Create temporary file
        File.WriteAllText(filePath, "---\ntags: ''\n---\nContent");

        try
        {
            // Act
            bool result = _processor.EnsureMetadataAsync(filePath, forceOverwrite: true).Result;

            // Assert
            Assert.IsTrue(result, "File processing should succeed");

            // Read back the metadata
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("type: note/case-study"), "File should have type: note/case-study");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests that files in reading directories get the correct type metadata.
    /// </summary>
    [TestMethod]
    public void DetermineTemplateType_ReadingDirectory_SetsCorrectType()
    {
        // Arrange
        string tempDir = Path.Combine(Path.GetTempPath(), "readings");
        Directory.CreateDirectory(tempDir);
        string filePath = Path.Combine(tempDir, "chapter-1.md");

        // Create temporary file
        File.WriteAllText(filePath, "---\ntags: ''\n---\nContent");

        try
        {
            // Act
            bool result = _processor.EnsureMetadataAsync(filePath, forceOverwrite: true).Result;

            // Assert
            Assert.IsTrue(result, "File processing should succeed");

            // Read back the metadata
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("type: note/reading"), "File should have type: note/reading");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests that files with associated PDF get the correct type metadata.
    /// </summary>
    [TestMethod]
    public void DetermineTemplateType_WithAssociatedPdf_SetsCorrectType()
    {
        // Arrange
        string tempDir = Path.GetTempPath();
        string mdFilePath = Path.Combine(tempDir, "case-study.md");
        string pdfFilePath = Path.Combine(tempDir, "case-study.pdf");

        // Create temporary files
        File.WriteAllText(mdFilePath, "---\ntags: ''\n---\nContent");
        File.WriteAllText(pdfFilePath, "dummy pdf content");

        try
        {
            // Act
            bool result = _processor.EnsureMetadataAsync(mdFilePath, forceOverwrite: true).Result;

            // Assert
            Assert.IsTrue(result, "File processing should succeed");

            // Read back the metadata
            string content = File.ReadAllText(mdFilePath);
            Assert.IsTrue(content.Contains("type: note/case-study"), "File should have type: note/case-study");
        }
        finally
        {
            // Cleanup
            if (File.Exists(mdFilePath))
            {
                File.Delete(mdFilePath);
            }
            if (File.Exists(pdfFilePath))
            {
                File.Delete(pdfFilePath);
            }
        }
    }

    /// <summary>
    /// Tests that files with associated video get the correct type metadata.
    /// </summary>
    [TestMethod]
    public void DetermineTemplateType_WithAssociatedVideo_SetsCorrectType()
    {
        // Arrange
        string tempDir = Path.GetTempPath();
        string mdFilePath = Path.Combine(tempDir, "lecture-video.md");
        string videoFilePath = Path.Combine(tempDir, "lecture-video.mp4");

        // Create temporary files
        File.WriteAllText(mdFilePath, "---\ntags: ''\n---\nContent");
        File.WriteAllText(videoFilePath, "dummy video content");

        try
        {
            // Act
            bool result = _processor.EnsureMetadataAsync(mdFilePath, forceOverwrite: true).Result;

            // Assert
            Assert.IsTrue(result, "File processing should succeed");

            // Read back the metadata
            string content = File.ReadAllText(mdFilePath);
            Assert.IsTrue(content.Contains("type: note/video-note"), "File should have type: note/video-note");
        }
        finally
        {
            // Cleanup
            if (File.Exists(mdFilePath))
            {
                File.Delete(mdFilePath);
            }
            if (File.Exists(videoFilePath))
            {
                File.Delete(videoFilePath);
            }
        }
    }

    /// <summary>
    /// Tests that existing metadata is preserved when forceOverwrite is false.
    /// </summary>
    [TestMethod]
    public void ProcessFile_ExistingMetadata_PreservesWhenNotForced()
    {
        // Arrange
        string tempDir = Path.GetTempPath();
        string filePath = Path.Combine(tempDir, "existing-metadata.md");

        // Create file with existing metadata
        string existingContent = @"---
type: note/custom
status: completed
tags: existing-tag
---
Content";
        File.WriteAllText(filePath, existingContent);

        try
        {
            // Act
            bool result = _processor.EnsureMetadataAsync(filePath, forceOverwrite: false).Result;

            // Assert
            Assert.IsTrue(result, "File processing should succeed");

            // Read back the metadata
            string content = File.ReadAllText(filePath);
            Assert.IsTrue(content.Contains("type: note/custom"), "Should preserve existing type");
            Assert.IsTrue(content.Contains("status: completed"), "Should preserve existing status");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
