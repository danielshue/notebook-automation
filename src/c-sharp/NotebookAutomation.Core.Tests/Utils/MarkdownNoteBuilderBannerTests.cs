// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Utils;

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
}