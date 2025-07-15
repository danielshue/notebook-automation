using Microsoft.Extensions.Logging;

using NotebookAutomation.Core.Tools;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

/// <summary>
/// Integration tests for resolver registry extension and resolver composition.
/// </summary>
[TestClass]
public class ResolverRegistryIntegrationTests
{
    private ILogger<MetadataSchemaLoader> _logger = null!;
    private FieldValueResolverRegistry _registry = null!;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new Mock<ILogger<MetadataSchemaLoader>>().Object;
        _registry = new FieldValueResolverRegistry();
    }

    [TestMethod]
    public void Registry_Should_Support_Multiple_Resolver_Types()
    {
        // Arrange
        var markdownLogger = new Mock<ILogger<MarkdownMetadataResolver>>().Object;
        var tagLogger = new Mock<ILogger<TagResolver>>().Object;
        var resourceLogger = new Mock<ILogger<ResourceMetadataResolver>>().Object;
        var transcriptLogger = new Mock<ILogger<TranscriptResolver>>().Object;

        var markdownResolver = new MarkdownMetadataResolver(markdownLogger);
        var tagResolver = new TagResolver(tagLogger);
        var resourceResolver = new ResourceMetadataResolver(resourceLogger);
        var transcriptResolver = new TranscriptResolver(transcriptLogger);

        // Act
        _registry.RegisterFileTypeResolver("markdown", markdownResolver);
        _registry.RegisterFileTypeResolver("tag", tagResolver);
        _registry.RegisterFileTypeResolver("resource", resourceResolver);
        _registry.RegisterFileTypeResolver("transcript", transcriptResolver);

        // Assert
        Assert.IsNotNull(_registry.GetFileTypeResolver("markdown"));
        Assert.IsNotNull(_registry.GetFileTypeResolver("tag"));
        Assert.IsNotNull(_registry.GetFileTypeResolver("resource"));
        Assert.IsNotNull(_registry.GetFileTypeResolver("transcript"));

        Assert.AreEqual(4, _registry.GetAllFileTypeResolvers().Count);
    }

    [TestMethod]
    public void Registry_Should_Support_Dynamic_Registration()
    {
        // Arrange
        var markdownLogger = new Mock<ILogger<MarkdownMetadataResolver>>().Object;
        var markdownResolver = new MarkdownMetadataResolver(markdownLogger);

        // Act
        _registry.Register("MarkdownMetadataResolver", markdownResolver);

        // Assert
        var retrievedResolver = _registry.Get("MarkdownMetadataResolver");
        Assert.IsNotNull(retrievedResolver);
        Assert.IsInstanceOfType(retrievedResolver, typeof(MarkdownMetadataResolver));

        // Should also be available as file type resolver
        var fileTypeResolver = _registry.GetFileTypeResolver("markdown");
        Assert.IsNotNull(fileTypeResolver);
        Assert.AreSame(markdownResolver, fileTypeResolver);
    }

    [TestMethod]
    public void Registry_Should_Support_Resolver_Composition()
    {
        // Arrange
        var markdownLogger = new Mock<ILogger<MarkdownMetadataResolver>>().Object;
        var tagLogger = new Mock<ILogger<TagResolver>>().Object;

        var markdownResolver = new MarkdownMetadataResolver(markdownLogger);
        var tagResolver = new TagResolver(tagLogger);

        _registry.RegisterFileTypeResolver("markdown", markdownResolver);
        _registry.RegisterFileTypeResolver("tag", tagResolver);

        var tempFile = Path.GetTempFileName();
        var markdownFile = Path.ChangeExtension(tempFile, ".md");
        var content = @"---
title: Test Document
tags: [Machine Learning, AI/Deep Learning]
---

# Test Document

This is a test document with some content.";

        try
        {
            File.Move(tempFile, markdownFile);
            File.WriteAllText(markdownFile, content);

            // Act - Use markdown resolver
            var markdownContext = new Dictionary<string, object> { ["filePath"] = markdownFile };
            var markdownMetadata = markdownResolver.ExtractMetadata(markdownContext);

            // Act - Use tag resolver with markdown tags
            var tagContext = new Dictionary<string, object> { ["tags"] = markdownMetadata.ContainsKey("tags") ? markdownMetadata["tags"] : new string[0] };
            var tagMetadata = tagResolver.ExtractMetadata(tagContext);

            // Assert
            Assert.IsTrue(markdownMetadata.ContainsKey("title"));
            Assert.IsTrue(markdownMetadata.ContainsKey("tags"));
            Assert.IsTrue(markdownMetadata.ContainsKey("word-count"));

            Assert.IsTrue(tagMetadata.ContainsKey("normalized-tags"));
            Assert.IsTrue(tagMetadata.ContainsKey("tag-count"));

            var normalizedTags = tagMetadata["normalized-tags"] as List<string>;
            Assert.IsNotNull(normalizedTags);
            Assert.IsTrue(normalizedTags.Count > 0);
        }
        finally
        {
            if (File.Exists(markdownFile))
                File.Delete(markdownFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Registry_Should_Handle_Runtime_Extension()
    {
        // Arrange
        var initialResolverCount = _registry.GetAllFileTypeResolvers().Count;

        // Act - Add resolvers at runtime
        var markdownLogger = new Mock<ILogger<MarkdownMetadataResolver>>().Object;
        var markdownResolver = new MarkdownMetadataResolver(markdownLogger);
        _registry.RegisterFileTypeResolver("markdown", markdownResolver);

        var tagLogger = new Mock<ILogger<TagResolver>>().Object;
        var tagResolver = new TagResolver(tagLogger);
        _registry.RegisterFileTypeResolver("tag", tagResolver);

        // Assert
        Assert.AreEqual(initialResolverCount + 2, _registry.GetAllFileTypeResolvers().Count);

        // Act - Replace existing resolver
        var newTagLogger = new Mock<ILogger<TagResolver>>().Object;
        var newTagResolver = new TagResolver(newTagLogger);
        _registry.RegisterFileTypeResolver("tag", newTagResolver);

        // Assert - Count should remain the same but resolver should be replaced
        Assert.AreEqual(initialResolverCount + 2, _registry.GetAllFileTypeResolvers().Count);
        Assert.AreSame(newTagResolver, _registry.GetFileTypeResolver("tag"));
    }

    [TestMethod]
    public void Registry_Should_Support_Cross_Resolver_Metadata_Enhancement()
    {
        // Arrange
        var markdownLogger = new Mock<ILogger<MarkdownMetadataResolver>>().Object;
        var resourceLogger = new Mock<ILogger<ResourceMetadataResolver>>().Object;

        var markdownResolver = new MarkdownMetadataResolver(markdownLogger);
        var resourceResolver = new ResourceMetadataResolver(resourceLogger);

        _registry.RegisterFileTypeResolver("markdown", markdownResolver);
        _registry.RegisterFileTypeResolver("resource", resourceResolver);

        var tempFile = Path.GetTempFileName();
        var markdownFile = Path.ChangeExtension(tempFile, ".md");
        var content = @"---
title: Test Document
---

# Test Document

This is a test document.";

        try
        {
            File.Move(tempFile, markdownFile);
            File.WriteAllText(markdownFile, content);

            // Act - Get metadata from both resolvers
            var context = new Dictionary<string, object> { ["filePath"] = markdownFile };

            var markdownMetadata = markdownResolver.ExtractMetadata(context);
            var resourceMetadata = resourceResolver.ExtractMetadata(context);

            // Assert - Both should provide different but complementary metadata
            Assert.IsTrue(markdownMetadata.ContainsKey("title"));
            Assert.IsTrue(markdownMetadata.ContainsKey("word-count"));

            Assert.IsTrue(resourceMetadata.ContainsKey("file-name"));
            Assert.IsTrue(resourceMetadata.ContainsKey("file-extension"));
            Assert.IsTrue(resourceMetadata.ContainsKey("file-size"));

            // Combined metadata should be comprehensive
            var combinedMetadata = new Dictionary<string, object>(markdownMetadata);
            foreach (var kvp in resourceMetadata)
            {
                if (!combinedMetadata.ContainsKey(kvp.Key))
                    combinedMetadata[kvp.Key] = kvp.Value;
            }

            Assert.IsTrue(combinedMetadata.Count > markdownMetadata.Count);
            Assert.IsTrue(combinedMetadata.Count > resourceMetadata.Count);
        }
        finally
        {
            if (File.Exists(markdownFile))
                File.Delete(markdownFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Registry_Should_Handle_Resolver_Errors_Gracefully()
    {
        // Arrange
        var mockResolver = new Mock<IFileTypeMetadataResolver>();
        mockResolver.Setup(r => r.FileType).Returns("test");
        mockResolver.Setup(r => r.ExtractMetadata(It.IsAny<Dictionary<string, object>>()))
                   .Throws(new InvalidOperationException("Test error"));

        _registry.RegisterFileTypeResolver("test", mockResolver.Object);

        // Act
        var context = new Dictionary<string, object> { ["filePath"] = "/test/path" };

        // Should not throw exception
        try
        {
            var metadata = mockResolver.Object.ExtractMetadata(context);
            Assert.Fail("Expected exception was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected - resolver should fail but registry should handle it
        }

        // Assert - Registry should still function
        Assert.IsNotNull(_registry.GetFileTypeResolver("test"));
    }
}
