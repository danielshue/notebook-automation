// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;

namespace NotebookAutomation.Tests.Core.Utils;

[TestClass]
public class MetadataTemplateManagerTests
{
    private Mock<ILogger> _loggerMock = null!;
    private IMetadataSchemaLoader _schemaLoader = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new();
        _schemaLoader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();
        // Note: The tests use the schema loader; no temp metadata.yaml needed
    }

    [TestCleanup]
    public void Cleanup()
    {
        // No cleanup required
    }

    /// <summary>
    /// Verifies that LoadTemplates loads all template types from a valid schema.
    /// </summary>
    [TestMethod]
    public void LoadTemplates_ValidSchema_LoadsAllTemplates()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();

        // Act
        List<string> templateTypes = templateManager.GetTemplateTypes();

        // Assert
        // Updated to include new base types and generic template types
        Assert.AreEqual(9, templateTypes.Count);
        Assert.IsTrue(templateTypes.Contains("video-reference"));
        Assert.IsTrue(templateTypes.Contains("pdf-reference"));
        Assert.IsTrue(templateTypes.Contains("resource-reading"));
        Assert.IsTrue(templateTypes.Contains("note/instruction"));
        Assert.IsTrue(templateTypes.Contains("video_transcript_consolidation"));
        Assert.IsTrue(templateTypes.Contains("base-template"));
        Assert.IsTrue(templateTypes.Contains("base-generic"));
        Assert.IsTrue(templateTypes.Contains("generic-video"));
        Assert.IsTrue(templateTypes.Contains("generic-pdf"));
    }
    /// <summary>
    /// Verifies that GetTemplate returns the correct template when requested type exists.
    /// </summary>
    [TestMethod]
    public void GetTemplate_ExistingType_ReturnsTemplate()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();

        // Act
        Dictionary<string, object>? template = templateManager.GetTemplate("video-reference");

        // Assert
        Assert.IsNotNull(template);
        Assert.AreEqual("video-reference", template["template-type"]);
        // Title is now resolved via TitleResolver with context; GetTemplate without context may leave it empty
        Assert.IsTrue(template.ContainsKey("title"));
        Assert.IsInstanceOfType<string>(template["title"]);
    }
    /// <summary>
    /// Verifies that GetTemplate returns null when requested template type does not exist.
    /// </summary>
    [TestMethod]
    public void GetTemplate_NonExistentType_ReturnsNull()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();

        // Act
        Dictionary<string, object>? template = templateManager.GetTemplate("non-existent-type");

        // Assert
        Assert.IsNull(template);
    }

    /// <summary>
    /// Verifies that GetFilledTemplate correctly replaces placeholders with provided values.
    /// </summary>
    [TestMethod]
    public void GetFilledTemplate_ProvidesValues_ReplacesPlaceholders()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
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

    /// <summary>
    /// Verifies that EnhanceMetadataWithTemplate applies video-reference template for video notes.
    /// </summary>
    [TestMethod]
    public void EnhanceMetadataWithTemplate_VideoNote_AppliesVideoTemplate()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
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

    /// <summary>
    /// Verifies that EnhanceMetadataWithTemplate applies pdf-reference template for PDF notes.
    /// </summary>
    [TestMethod]
    public void EnhanceMetadataWithTemplate_PdfNote_AppliesPdfTemplate()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
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

    /// <summary>
    /// Verifies that EnhanceMetadataWithTemplate applies universal fields from schema when using schema loader.
    /// </summary>
    [TestMethod]
    public void EnhanceMetadataWithTemplate_WithSchemaLoader_AppliesUniversalFields()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        Dictionary<string, object> metadata = new()
        {
            { "title", "Test Video" },
            { "source_file", "c:/path/to/video.mp4" },
        };

        // Act
        Dictionary<string, object> enhanced = templateManager.EnhanceMetadataWithTemplate(metadata, "Video Note");

        // Assert - Universal fields should be applied
        Assert.IsTrue(enhanced.ContainsKey("auto-generated-state"), "Universal field 'auto-generated-state' should be present");
        Assert.IsTrue(enhanced.ContainsKey("date-created"), "Universal field 'date-created' should be present");
        Assert.IsTrue(enhanced.ContainsKey("publisher"), "Universal field 'publisher' should be present");
    }

    /// <summary>
    /// Verifies that EnhanceMetadataWithTemplate applies reserved tags from schema when using schema loader.
    /// </summary>
    [TestMethod]
    public void EnhanceMetadataWithTemplate_WithSchemaLoader_AppliesReservedTags()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        Dictionary<string, object> metadata = new()
        {
            { "title", "Test Video" },
            { "source_file", "c:/path/to/video.mp4" },
        };

        // Act
        Dictionary<string, object> enhanced = templateManager.EnhanceMetadataWithTemplate(metadata, "Video Note");

        // Assert - Reserved tags should be applied through schema loader
        Assert.IsTrue(enhanced.ContainsKey("tags"), "Template should include tags");
        object tagsObj = enhanced["tags"];
        Assert.IsInstanceOfType<string[]>(tagsObj, "Tags should be a string array");
        string[] tags = (string[])tagsObj;

        // Should contain the reserved tag "video"
        Assert.IsTrue(tags.Contains("video"), "Reserved tag 'video' should be present");
    }

    /// <summary>
    /// Verifies that GetTemplate returns schema-based templates when using schema loader.
    /// </summary>
    [TestMethod]
    public void GetTemplate_WithSchemaLoader_ReturnsSchemaBasedTemplate()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();

        // Act
        Dictionary<string, object>? template = templateManager.GetTemplate("pdf-reference");

        // Assert - Template should be based on schema
        Assert.IsNotNull(template, "Schema-based template should exist");
        Assert.IsTrue(template.ContainsKey("template-type"), "Schema-based template should have template-type");
        Assert.AreEqual("pdf-reference", template["template-type"], "Template-type should match schema");
    }

    /// <summary>
    /// Verifies that GetTemplateTypes returns all schema-defined template types when using schema loader.
    /// </summary>
    [TestMethod]
    public void GetTemplateTypes_WithSchemaLoader_ReturnsSchemaTypes()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();

        // Act
        List<string> templateTypes = templateManager.GetTemplateTypes();

        // Assert - Should return schema-based template types
        Assert.IsTrue(templateTypes.Contains("pdf-reference"), "Schema template type 'pdf-reference' should be available");
        Assert.IsTrue(templateTypes.Contains("video-reference"), "Schema template type 'video-reference' should be available");
        Assert.IsTrue(templateTypes.Contains("resource-reading"), "Schema template type 'resource-reading' should be available");
    }

    /// <summary>
    /// Verifies that GetFilledTemplate uses schema loader's resolver integration when filling templates.
    /// </summary>
    [TestMethod]
    public void GetFilledTemplate_WithSchemaLoader_UsesResolvers()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        Dictionary<string, string> values = new()
        {
            { "title", "Custom PDF Title" },
        };

        // Act
        Dictionary<string, object>? filledTemplate = templateManager.GetFilledTemplate("pdf-reference", values);

        // Assert - Should use schema loader's resolver integration
        Assert.IsNotNull(filledTemplate, "Filled template should exist");
        Assert.AreEqual("Custom PDF Title", filledTemplate["title"], "Title should be filled from values");
        Assert.IsTrue(filledTemplate.ContainsKey("date-created"), "Should have date-created field from schema");
    }

    /// <summary>
    /// Verifies that EnhanceMetadataWithTemplate preserves existing metadata fields when applying templates with schema loader.
    /// </summary>
    [TestMethod]
    public void EnhanceMetadataWithTemplate_WithSchemaLoader_PreservesExistingMetadata()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        Dictionary<string, object> metadata = new()
        {
            { "title", "Original Title" },
            { "source_file", "c:/path/to/video.mp4" },
            { "custom_field", "custom_value" },
        };

        // Act
        Dictionary<string, object> enhanced = templateManager.EnhanceMetadataWithTemplate(metadata, "Video Note");

        // Assert - Original metadata should be preserved
        Assert.AreEqual("Original Title", enhanced["title"], "Original title should be preserved");
        Assert.AreEqual("c:/path/to/video.mp4", enhanced["source_file"], "Original source_file should be preserved");
        Assert.AreEqual("custom_value", enhanced["custom_field"], "Custom field should be preserved");
    }

    /// <summary>
    /// Verifies that EnhanceMetadataWithTemplate correctly integrates with type mapping when using schema loader.
    /// </summary>
    [TestMethod]
    public void EnhanceMetadataWithTemplate_WithSchemaLoader_IntegratesWithTypeMapping()
    {
        // Arrange
        MetadataTemplateManager templateManager = MetadataSchemaLoaderHelper.CreateTestMetadataTemplateManager();
        Dictionary<string, object> metadata = new()
        {
            { "title", "Test PDF" },
            { "source_file", "c:/path/to/document.pdf" },
        };

        // Act
        Dictionary<string, object> enhanced = templateManager.EnhanceMetadataWithTemplate(metadata, "PDF Note");

        // Assert - Should integrate with schema loader's type mapping
        Assert.AreEqual("pdf-reference", enhanced["template-type"], "Template type should match schema mapping");
        Assert.IsTrue(enhanced.ContainsKey("comprehension"), "Should have schema-defined fields");
        Assert.IsTrue(enhanced.ContainsKey("status"), "Should have schema-defined fields");
    }
}
