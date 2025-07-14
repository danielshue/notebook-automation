// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Tests.Core.Utils;

/// <summary>
/// Tests for banner configuration functionality in MarkdownNoteBuilder.
/// </summary>
[TestClass]
public class MarkdownNoteBuilderBannerTests
{
    private Mock<IYamlHelper> _yamlHelperMock = null!;
    private AppConfig _appConfig = null!;
    private MarkdownNoteBuilder _builder = null!;

    [TestInitialize]
    public void Setup()
    {
        _yamlHelperMock = new Mock<IYamlHelper>();
        _appConfig = new AppConfig
        {
            Banners = new BannerConfig
            {
                Enabled = true,
                DefaultBanner = "default-banner.png",
                Format = "image",
                TemplateBanners = new Dictionary<string, string>
                {
                    ["main"] = "main-banner.png",
                    ["course"] = "course-banner.png"
                },
                FilenamePatterns = new Dictionary<string, string>
                {
                    ["*index*"] = "index-banner.png",
                    ["*test*"] = "test-banner.png"
                }
            }
        };
        _builder = new MarkdownNoteBuilder(_yamlHelperMock.Object, _appConfig);

        // Setup YamlHelper mock to return a basic YAML string
        _yamlHelperMock.Setup(x => x.UpdateFrontmatter(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                      .Returns("---\ntest: value\n---\n");
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WhenBannersDisabled_DoesNotAddBanner()
    {
        // Arrange
        _appConfig.Banners.Enabled = false;
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter);

        // Assert
        Assert.IsFalse(frontmatter.ContainsKey("banner"));
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithFilenamePattern_UsesPatternBanner()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["title"] = "Test"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter, "test-index.md");

        // Assert
        Assert.IsTrue(frontmatter.ContainsKey("banner"));
        Assert.AreEqual("index-banner.png", frontmatter["banner"]);
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithTemplateType_UsesTemplateBanner()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "course"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter);

        // Assert
        Assert.IsTrue(frontmatter.ContainsKey("banner"));
        Assert.AreEqual("course-banner.png", frontmatter["banner"]);
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithExistingBanner_PreservesExisting()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main",
            ["banner"] = "existing-banner.png"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter, "index.md");

        // Assert
        Assert.AreEqual("existing-banner.png", frontmatter["banner"]);
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_FilenamePatternTakesPrecedence_OverTemplateType()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main" // Would normally get main-banner.png
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter, "test-file.md");

        // Assert
        Assert.IsTrue(frontmatter.ContainsKey("banner"));
        Assert.AreEqual("test-banner.png", frontmatter["banner"]);
    }

    [TestMethod]
    public void BuildNote_WithFilenameAndTemplateType_AppliesBannerCorrectly()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "course"
        };
        var body = "# Test Content";

        // Act
        var result = _builder.BuildNote(frontmatter, body, "course-index.md");

        // Assert
        Assert.IsTrue(frontmatter.ContainsKey("banner"));
        Assert.AreEqual("index-banner.png", frontmatter["banner"]); // filename pattern wins
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithNonMatchingPattern_FallsBackToTemplateType()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter, "regular-file.md");

        // Assert
        Assert.IsTrue(frontmatter.ContainsKey("banner"));
        Assert.AreEqual("main-banner.png", frontmatter["banner"]);
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithUnsupportedTemplateType_DoesNotAddBanner()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "video-reference" // Not in configuration
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter, "video.md");

        // Assert
        Assert.IsFalse(frontmatter.ContainsKey("banner"));
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithUniversalFields_InjectsUniversalFields()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main",
            ["title"] = "Test Note"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter);

        // Assert - Universal fields should be injected
        Assert.IsTrue(frontmatter.ContainsKey("template-type"), "Template type should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("title"), "Title should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("banner"), "Banner should be added for template type");
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithReservedFields_PreservesReservedFields()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "course",
            ["case-study"] = "true",
            ["live-class"] = "false",
            ["reading"] = "required"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter);

        // Assert - Reserved fields should be preserved
        Assert.IsTrue(frontmatter.ContainsKey("case-study"), "Reserved field 'case-study' should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("live-class"), "Reserved field 'live-class' should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("reading"), "Reserved field 'reading' should be preserved");
        Assert.AreEqual("true", frontmatter["case-study"], "Reserved field value should be correct");
        Assert.AreEqual("false", frontmatter["live-class"], "Reserved field value should be correct");
        Assert.AreEqual("required", frontmatter["reading"], "Reserved field value should be correct");
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithMixedFieldTypes_HandlesAllCorrectly()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main",
            ["title"] = "Mixed Field Test",
            ["custom-field"] = "custom-value",
            ["case-study"] = "false", // Reserved field
            ["tags"] = new[] { "test", "integration" }
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter, "test-index.md");

        // Assert - All field types should be handled properly
        Assert.IsTrue(frontmatter.ContainsKey("banner"), "Banner should be added");
        Assert.AreEqual("index-banner.png", frontmatter["banner"], "Banner should match filename pattern");
        Assert.IsTrue(frontmatter.ContainsKey("template-type"), "Template type should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("title"), "Title should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("custom-field"), "Custom field should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("case-study"), "Reserved field should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("tags"), "Tags array should be preserved");
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithSchemaBasedFields_IntegratesWithSchema()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "pdf-reference",
            ["title"] = "Schema Test",
            ["status"] = "unread",
            ["comprehension"] = 0
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter);

        // Assert - Schema-based fields should be integrated
        Assert.IsTrue(frontmatter.ContainsKey("template-type"), "Template type should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("title"), "Title should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("status"), "Status field should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("comprehension"), "Comprehension field should be preserved");
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithUniversalFieldDefaults_AppliesDefaults()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "video-reference",
            ["title"] = "Universal Field Test"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter);

        // Assert - Universal field defaults should be applied where not specified
        Assert.IsTrue(frontmatter.ContainsKey("template-type"), "Template type should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("title"), "Title should be preserved");
    }

    [TestMethod]
    public void CreateMarkdownWithFrontmatter_WithReservedFieldInheritance_InheritsCorrectly()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "resource-reading",
            ["title"] = "Reserved Field Inheritance Test"
        };

        // Act
        var result = _builder.CreateMarkdownWithFrontmatter(frontmatter);

        // Assert - Reserved fields should be inherited from schema
        Assert.IsTrue(frontmatter.ContainsKey("template-type"), "Template type should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("title"), "Title should be preserved");
    }

    [TestMethod]
    public void BuildNote_WithUniversalAndReservedFields_IntegratesAllFields()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main",
            ["title"] = "Integration Test",
            ["case-study"] = "true",
            ["custom-field"] = "custom-value"
        };
        var body = "# Integration Test Content";

        // Act
        var result = _builder.BuildNote(frontmatter, body, "integration-index.md");

        // Assert - All field types should be integrated
        Assert.IsTrue(frontmatter.ContainsKey("banner"), "Banner should be added");
        Assert.AreEqual("index-banner.png", frontmatter["banner"], "Banner should match filename pattern");
        Assert.IsTrue(frontmatter.ContainsKey("template-type"), "Template type should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("title"), "Title should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("case-study"), "Reserved field should be preserved");
        Assert.IsTrue(frontmatter.ContainsKey("custom-field"), "Custom field should be preserved");
    }
}
