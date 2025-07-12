using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

/// <summary>
/// Unit tests for <see cref="TagResolver"/>.
/// </summary>
[TestClass]
public class TagResolverTests
{
    private ILogger<TagResolver> _logger;
    private TagResolver _resolver;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new Mock<ILogger<TagResolver>>().Object;
        _resolver = new TagResolver(_logger);
    }

    [TestMethod]
    public void FileType_Should_Return_Tag()
    {
        // Act
        var fileType = _resolver.FileType;

        // Assert
        Assert.AreEqual("tag", fileType);
    }

    [TestMethod]
    public void CanResolve_Should_Return_True_For_Supported_Fields_With_Valid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["tags"] = new[] { "test", "example" } };

        // Act & Assert
        Assert.IsTrue(_resolver.CanResolve("normalized-tags", context));
        Assert.IsTrue(_resolver.CanResolve("invalid-tags", context));
        Assert.IsTrue(_resolver.CanResolve("tag-count", context));
        Assert.IsTrue(_resolver.CanResolve("hierarchical-tags", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Unsupported_Fields()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["tags"] = new[] { "test" } };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("unsupported-field", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Null_Context()
    {
        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("normalized-tags", null));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_Without_Tags()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("normalized-tags", context));
    }

    [TestMethod]
    public void Resolve_Should_Normalize_Tags()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "Machine Learning", "AI/Deep Learning", "python programming" }
        };

        // Act
        var result = _resolver.Resolve("normalized-tags", context) as List<string>;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("machine-learning"));
        Assert.IsTrue(result.Contains("ai/deep-learning"));
        Assert.IsTrue(result.Contains("python-programming"));
    }

    [TestMethod]
    public void Resolve_Should_Count_Tags()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "tag1", "tag2", "tag3" }
        };

        // Act
        var result = _resolver.Resolve("tag-count", context);

        // Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void Resolve_Should_Identify_Invalid_Tags()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "valid-tag", "reserved-tag", "another-tag" },
            ["reservedTags"] = new[] { "reserved-tag" }
        };

        // Act
        var result = _resolver.Resolve("invalid-tags", context) as List<string>;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.Contains("reserved-tag"));
    }

    [TestMethod]
    public void Resolve_Should_Create_Hierarchical_Structure()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "ai/machine-learning", "ai/deep-learning", "programming/python" }
        };

        // Act
        var result = _resolver.Resolve("hierarchical-tags", context) as Dictionary<string, object>;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.ContainsKey("ai"));
        Assert.IsTrue(result.ContainsKey("programming"));
    }

    [TestMethod]
    public void Resolve_Should_Calculate_Hierarchy_Depth()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "ai/machine-learning/supervised", "simple-tag", "category/subcategory" }
        };

        // Act
        var result = _resolver.Resolve("tag-hierarchy-depth", context);

        // Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void Resolve_Should_Return_Null_For_Invalid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act
        var result = _resolver.Resolve("normalized-tags", context);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Resolve_Should_Generate_Suggested_Tags_From_Content()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "existing-tag" },
            ["content"] = "This document discusses machine learning algorithms and artificial intelligence research in Python programming."
        };

        // Act
        var result = _resolver.Resolve("suggested-tags", context) as List<string>;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Any());
        // Should contain some suggested tags based on content
    }

    [TestMethod]
    public void ExtractMetadata_Should_Return_Comprehensive_Tag_Analysis()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "Machine Learning", "AI/Deep Learning", "python programming", "reserved-tag" },
            ["reservedTags"] = new[] { "reserved-tag" },
            ["content"] = "This document discusses machine learning and artificial intelligence."
        };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsTrue(metadata.ContainsKey("normalized-tags"));
        Assert.IsTrue(metadata.ContainsKey("tag-count"));
        Assert.IsTrue(metadata.ContainsKey("invalid-tags"));
        Assert.IsTrue(metadata.ContainsKey("hierarchical-tags"));
        Assert.IsTrue(metadata.ContainsKey("tag-validation-report"));
        
        var normalizedTags = metadata["normalized-tags"] as List<string>;
        Assert.IsNotNull(normalizedTags);
        Assert.IsTrue(normalizedTags.Count > 0);
        
        var invalidTags = metadata["invalid-tags"] as List<string>;
        Assert.IsNotNull(invalidTags);
        Assert.IsTrue(invalidTags.Contains("reserved-tag"));
    }

    [TestMethod]
    public void ExtractMetadata_Should_Return_Empty_Dictionary_For_Null_Context()
    {
        // Act
        var metadata = _resolver.ExtractMetadata(null);

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(0, metadata.Count);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Handle_Empty_Tags()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["tags"] = new string[0] };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(0, (int)metadata["tag-count"]);
        Assert.IsTrue(metadata.ContainsKey("normalized-tags"));
    }

    [TestMethod]
    public void ExtractMetadata_Should_Remove_Duplicate_Tags()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "duplicate-tag", "Duplicate Tag", "DUPLICATE-TAG", "other-tag" }
        };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        var normalizedTags = metadata["normalized-tags"] as List<string>;
        Assert.IsNotNull(normalizedTags);
        Assert.AreEqual(2, normalizedTags.Count); // Should have only 2 unique tags
        Assert.IsTrue(normalizedTags.Contains("duplicate-tag"));
        Assert.IsTrue(normalizedTags.Contains("other-tag"));
    }

    [TestMethod]
    public void ExtractMetadata_Should_Handle_Different_Tag_Separators()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "category|subcategory", "another|deep|hierarchy" },
            ["tagSeparator"] = "|"
        };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        var hierarchyDepth = (int)metadata["tag-hierarchy-depth"];
        Assert.AreEqual(3, hierarchyDepth);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Preserve_Case_When_Configured()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "CamelCase", "PascalCase" },
            ["normalizeCase"] = false
        };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        var normalizedTags = metadata["normalized-tags"] as List<string>;
        Assert.IsNotNull(normalizedTags);
        Assert.IsTrue(normalizedTags.Contains("CamelCase"));
        Assert.IsTrue(normalizedTags.Contains("PascalCase"));
    }

    [TestMethod]
    public void ExtractMetadata_Should_Skip_Reserved_Validation_When_Configured()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["tags"] = new[] { "reserved-tag", "normal-tag" },
            ["reservedTags"] = new[] { "reserved-tag" },
            ["validateReserved"] = false
        };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        var normalizedTags = metadata["normalized-tags"] as List<string>;
        Assert.IsNotNull(normalizedTags);
        Assert.IsTrue(normalizedTags.Contains("reserved-tag"));
        Assert.IsFalse(metadata.ContainsKey("invalid-tags"));
    }
}