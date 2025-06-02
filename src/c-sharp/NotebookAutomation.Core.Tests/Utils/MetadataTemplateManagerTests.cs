using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Utils;

[TestClass]
public class MetadataTemplateManagerTests
{
    private Mock<ILogger> _loggerMock;
    private Mock<AppConfig> _appConfigMock;
    private AppConfig _testAppConfig;
    private string _testMetadataFile;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger>();

        // Create a temporary metadata.yaml file for testing
        _testMetadataFile = Path.Combine(Path.GetTempPath(), "test_metadata.yaml");

        // Create test metadata content
        string testMetadata = @"---
template-type: video-reference
auto-generated-state: writable
template-description: Template for video reference notes.
title: Video Note
type: video-reference
tags:
  - video
  - reference
date-created: 2025-04-19
---
template-type: pdf-reference
auto-generated-state: writable
template-description: Template for PDF reference notes.
title: PDF Note
type: pdf-reference
tags:
  - pdf
  - reference
date-created: 2025-04-19";
        File.WriteAllText(_testMetadataFile, testMetadata);

        // Create a real AppConfig instance instead of mocking it
        _appConfigMock = new Mock<AppConfig>();

        // Create the real config and set it up
        AppConfig realConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                MetadataFile = _testMetadataFile
            }
        };

        // Store the real config in a field for test usage
        _testAppConfig = realConfig;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Delete temporary test file
        if (File.Exists(_testMetadataFile))
        {
            File.Delete(_testMetadataFile);
        }
    }
    [TestMethod]
    public void LoadTemplates_ValidMetadataFile_LoadsAllTemplates()
    {
        // Arrange
        MetadataTemplateManager templateManager = new MetadataTemplateManager(_loggerMock.Object, _testAppConfig);

        // Act
        List<string> templateTypes = templateManager.GetTemplateTypes();

        // Assert
        Assert.AreEqual(2, templateTypes.Count);
        Assert.IsTrue(templateTypes.Contains("video-reference"));
        Assert.IsTrue(templateTypes.Contains("pdf-reference"));
    }
    [TestMethod]
    public void GetTemplate_ExistingType_ReturnsTemplate()
    {
        // Arrange
        MetadataTemplateManager templateManager = new MetadataTemplateManager(_loggerMock.Object, _testAppConfig);

        // Act
        Dictionary<string, object> template = templateManager.GetTemplate("video-reference");

        // Assert
        Assert.IsNotNull(template);
        Assert.AreEqual("video-reference", template["template-type"]);
        Assert.AreEqual("Video Note", template["title"]);
    }
    [TestMethod]
    public void GetTemplate_NonExistentType_ReturnsNull()
    {
        // Arrange
        MetadataTemplateManager templateManager = new MetadataTemplateManager(_loggerMock.Object, _testAppConfig);

        // Act
        Dictionary<string, object> template = templateManager.GetTemplate("non-existent-type");

        // Assert
        Assert.IsNull(template);
    }
    [TestMethod]
    public void GetFilledTemplate_ProvidesValues_ReplacesPlaceholders()
    {
        // Arrange
        MetadataTemplateManager templateManager = new MetadataTemplateManager(_loggerMock.Object, _testAppConfig);
        Dictionary<string, string> values = new Dictionary<string, string>
        {
            { "title", "Custom Video Title" },
            { "date-created", "2025-05-25" }
        };

        // Act
        Dictionary<string, object> filledTemplate = templateManager.GetFilledTemplate("video-reference", values);

        // Assert
        Assert.IsNotNull(filledTemplate);
        Assert.AreEqual("Custom Video Title", filledTemplate["title"]);
        Assert.AreEqual("2025-05-25", filledTemplate["date-created"]);
        Assert.AreEqual("video-reference", filledTemplate["template-type"]);
    }
    [TestMethod]
    public void EnhanceMetadataWithTemplate_VideoNote_AppliesVideoTemplate()
    {
        // Arrange
        MetadataTemplateManager templateManager = new MetadataTemplateManager(_loggerMock.Object, _testAppConfig);
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "title", "Custom Video Title" },
            { "source_file", "c:/path/to/video.mp4" }
        };            // Act
        Dictionary<string, object> enhanced = templateManager.EnhanceMetadataWithTemplate(metadata, "Video Note");

        // Assert
        Assert.IsNotNull(enhanced);
        Assert.AreEqual("Custom Video Title", enhanced["title"]);
        Assert.AreEqual("video-reference", enhanced["template-type"]);            // Verify template tags are included
        object tagsObj = enhanced["tags"];
        Assert.IsNotNull(tagsObj);

        // Tags should be a string array
        Assert.IsInstanceOfType<string[]>(tagsObj, "Tags should be a string array");
        string[] tags = (string[])tagsObj;

        Assert.AreEqual(2, tags.Length);
        Assert.AreEqual("video", tags[0]);
        Assert.AreEqual("reference", tags[1]);
    }
    [TestMethod]
    public void EnhanceMetadataWithTemplate_PdfNote_AppliesPdfTemplate()
    {
        // Arrange
        MetadataTemplateManager templateManager = new MetadataTemplateManager(_loggerMock.Object, _testAppConfig);
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            { "title", "Custom PDF Title" },
            { "source_file", "c:/path/to/document.pdf" }
        };

        // Act
        Dictionary<string, object> enhanced = templateManager.EnhanceMetadataWithTemplate(metadata, "PDF Note");            // Assert
        Assert.IsNotNull(enhanced);
        Assert.AreEqual("Custom PDF Title", enhanced["title"]);
        Assert.AreEqual("pdf-reference", enhanced["template-type"]);

        // Verify template tags are included
        object tagsObj = enhanced["tags"];
        Assert.IsNotNull(tagsObj);

        // Tags should be a string array
        Assert.IsInstanceOfType<string[]>(tagsObj, "Tags should be a string array");
        string[] tags = (string[])tagsObj;

        Assert.AreEqual(2, tags.Length);
        Assert.AreEqual("pdf", tags[0]);
        Assert.AreEqual("reference", tags[1]);
    }
}
