// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;

namespace NotebookAutomation.Tests.Core.Utils;

[TestClass]
public class MetadataTemplateManagerTests
{
    private Mock<ILogger> _loggerMock = null!;
    private IMetadataSchemaLoader _schemaLoader = null!;
    private string _testMetadataFile = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new();
        _schemaLoader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

        // Create a unique temporary metadata.yaml file for testing to prevent parallel test conflicts
        string uniqueId = Guid.NewGuid().ToString("N")[..8]; // Short unique ID
        _testMetadataFile = Path.Combine(Path.GetTempPath(), $"test_metadata_{uniqueId}.yaml");

        // Note: The test now uses the schema loader, so we don't need to create metadata files
        // The helper will create the necessary test schema
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Delete temporary test file if it exists
        if (File.Exists(_testMetadataFile))
        {
            File.Delete(_testMetadataFile);
        }
    }

    [TestMethod]
    public void LoadTemplates_ValidMetadataFile_LoadsAllTemplates()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager(_loggerMock.Object);

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
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager(_loggerMock.Object);

        // Act
        Dictionary<string, object>? template = templateManager.GetTemplate("video-reference");

        // Assert
        Assert.IsNotNull(template);
        Assert.AreEqual("video-reference", template["template-type"]);
        Assert.AreEqual("Video Note", template["title"]);
    }
    [TestMethod]
    public void GetTemplate_NonExistentType_ReturnsNull()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager(_loggerMock.Object);

        // Act
        Dictionary<string, object>? template = templateManager.GetTemplate("non-existent-type");

        // Assert
        Assert.IsNull(template);
    }

    [TestMethod]
    public void GetFilledTemplate_ProvidesValues_ReplacesPlaceholders()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager(_loggerMock.Object);
        Dictionary<string, string> values = new()
        {
            { "title", "Custom Video Title" },
            { "date-created", "2025-05-25" },
        };

        // Act
        Dictionary<string, object>? filledTemplate = templateManager.GetFilledTemplate("video-reference", values);

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
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager(_loggerMock.Object);
        Dictionary<string, object> metadata = new()
        {
            { "title", "Custom Video Title" },
            { "source_file", "c:/path/to/video.mp4" },
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
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager(_loggerMock.Object);
        Dictionary<string, object> metadata = new()
        {
            { "title", "Custom PDF Title" },
            { "source_file", "c:/path/to/document.pdf" },
        };

        // Act
        Dictionary<string, object> enhanced = templateManager.EnhanceMetadataWithTemplate(metadata, "PDF Note");// Assert
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
