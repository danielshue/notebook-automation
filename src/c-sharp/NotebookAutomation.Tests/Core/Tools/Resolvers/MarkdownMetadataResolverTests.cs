using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

/// <summary>
/// Unit tests for <see cref="MarkdownMetadataResolver"/>.
/// </summary>
[TestClass]
public class MarkdownMetadataResolverTests
{
    private ILogger<MarkdownMetadataResolver> _logger;
    private MarkdownMetadataResolver _resolver;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new Mock<ILogger<MarkdownMetadataResolver>>().Object;
        _resolver = new MarkdownMetadataResolver(_logger);
    }

    [TestMethod]
    public void FileType_Should_Return_Markdown()
    {
        // Act
        var fileType = _resolver.FileType;

        // Assert
        Assert.AreEqual("markdown", fileType);
    }

    [TestMethod]
    public void CanResolve_Should_Return_True_For_Supported_Fields_With_Valid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/file.md" };

        // Act & Assert
        Assert.IsTrue(_resolver.CanResolve("title", context));
        Assert.IsTrue(_resolver.CanResolve("tags", context));
        Assert.IsTrue(_resolver.CanResolve("date-created", context));
        Assert.IsTrue(_resolver.CanResolve("word-count", context));
        Assert.IsTrue(_resolver.CanResolve("heading-count", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Unsupported_Fields()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/file.md" };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("unsupported-field", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Null_Context()
    {
        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("title", null));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_Without_FilePath_Or_Content()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("title", context));
    }

    [TestMethod]
    public void Resolve_Should_Extract_Title_From_Frontmatter()
    {
        // Arrange
        var content = @"---
title: My Test Document
tags: [test, markdown]
---

# Content Header

This is the body content.";
        var context = new Dictionary<string, object> { ["content"] = content };

        // Act
        var result = _resolver.Resolve("title", context);

        // Assert
        Assert.AreEqual("My Test Document", result);
    }

    [TestMethod]
    public void Resolve_Should_Extract_Title_From_First_Heading_When_No_Frontmatter()
    {
        // Arrange
        var content = @"# Main Title

This is the body content.

## Sub Header";
        var context = new Dictionary<string, object> { ["content"] = content };

        // Act
        var result = _resolver.Resolve("title", context);

        // Assert
        Assert.AreEqual("Main Title", result);
    }

    [TestMethod]
    public void Resolve_Should_Count_Words_Excluding_Frontmatter()
    {
        // Arrange
        var content = @"---
title: Test Document
tags: [test]
---

This is a test document with exactly ten words here.";
        var context = new Dictionary<string, object> { ["content"] = content };

        // Act
        var result = _resolver.Resolve("word-count", context);

        // Assert
        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public void Resolve_Should_Count_Headings()
    {
        // Arrange
        var content = @"# Main Title

## Sub Title 1

### Sub Sub Title

## Sub Title 2

Content here.";
        var context = new Dictionary<string, object> { ["content"] = content };

        // Act
        var result = _resolver.Resolve("heading-count", context);

        // Assert
        Assert.AreEqual(4, result);
    }

    [TestMethod]
    public void Resolve_Should_Return_Null_For_Invalid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act
        var result = _resolver.Resolve("title", context);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Return_Comprehensive_Metadata()
    {
        // Arrange
        var content = @"---
title: Test Document
tags: [test, markdown]
author: Test Author
---

# Main Header

This is test content with some words.

## Sub Header

More content here.";
        var context = new Dictionary<string, object> { ["content"] = content };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsTrue(metadata.ContainsKey("title"));
        Assert.IsTrue(metadata.ContainsKey("tags"));
        Assert.IsTrue(metadata.ContainsKey("author"));
        Assert.IsTrue(metadata.ContainsKey("word-count"));
        Assert.IsTrue(metadata.ContainsKey("heading-count"));
        
        Assert.AreEqual("Test Document", metadata["title"]);
        Assert.AreEqual(2, metadata["heading-count"]);
        Assert.IsTrue((int)metadata["word-count"] > 0);
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
    public void ExtractMetadata_Should_Return_Empty_Dictionary_For_Empty_Content()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["content"] = "" };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(0, metadata.Count);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Handle_Malformed_Frontmatter()
    {
        // Arrange
        var content = @"---
title: Test Document
tags: [test, markdown
invalid yaml here
---

# Main Header

Content here.";
        var context = new Dictionary<string, object> { ["content"] = content };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsNotNull(metadata);
        // Should still extract content-based metadata even if frontmatter is malformed
        Assert.IsTrue(metadata.ContainsKey("word-count"));
        Assert.IsTrue(metadata.ContainsKey("heading-count"));
    }

    [TestMethod]
    public void ExtractMetadata_Should_Include_File_System_Metadata_When_FilePath_Provided()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = @"# Test Document

This is test content.";
        
        try
        {
            File.WriteAllText(tempFile, content);
            var context = new Dictionary<string, object> { ["filePath"] = tempFile };

            // Act
            var metadata = _resolver.ExtractMetadata(context);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("date-created"));
            Assert.IsTrue(metadata.ContainsKey("date-modified"));
            Assert.IsTrue(metadata.ContainsKey("file-size"));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}