using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

/// <summary>
/// Unit tests for <see cref="ResourceMetadataResolver"/>.
/// </summary>
[TestClass]
public class ResourceMetadataResolverTests
{
    private ILogger<ResourceMetadataResolver> _logger;
    private ResourceMetadataResolver _resolver;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new Mock<ILogger<ResourceMetadataResolver>>().Object;
        _resolver = new ResourceMetadataResolver(_logger);
    }

    [TestMethod]
    public void FileType_Should_Return_Resource()
    {
        // Act
        var fileType = _resolver.FileType;

        // Assert
        Assert.AreEqual("resource", fileType);
    }

    [TestMethod]
    public void CanResolve_Should_Return_True_For_Supported_Fields_With_Valid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/file.pdf" };

        // Act & Assert
        Assert.IsTrue(_resolver.CanResolve("file-name", context));
        Assert.IsTrue(_resolver.CanResolve("file-extension", context));
        Assert.IsTrue(_resolver.CanResolve("file-size", context));
        Assert.IsTrue(_resolver.CanResolve("date-created", context));
        Assert.IsTrue(_resolver.CanResolve("date-modified", context));
        Assert.IsTrue(_resolver.CanResolve("resource-type", context));
        Assert.IsTrue(_resolver.CanResolve("mime-type", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Unsupported_Fields()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/file.pdf" };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("unsupported-field", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Null_Context()
    {
        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("file-name", null));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_Without_FilePath()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("file-name", context));
    }

    [TestMethod]
    public void Resolve_Should_Return_File_Name()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var context = new Dictionary<string, object> { ["filePath"] = tempFile };

        try
        {
            // Act
            var result = _resolver.Resolve("file-name", context);

            // Assert
            Assert.AreEqual(Path.GetFileName(tempFile), result);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_File_Extension()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".pdf");
        var context = new Dictionary<string, object> { ["filePath"] = pdfFile };

        try
        {
            File.Move(tempFile, pdfFile);
            File.WriteAllText(pdfFile, "dummy content");

            // Act
            var result = _resolver.Resolve("file-extension", context);

            // Assert
            Assert.AreEqual(".pdf", result);
        }
        finally
        {
            if (File.Exists(pdfFile))
                File.Delete(pdfFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_File_Size()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var testContent = "This is test content for file size testing.";
        File.WriteAllText(tempFile, testContent);
        var context = new Dictionary<string, object> { ["filePath"] = tempFile };

        try
        {
            // Act
            var result = _resolver.Resolve("file-size", context);

            // Assert
            Assert.IsTrue((long)result > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Resource_Type_For_Pdf()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".pdf");
        var context = new Dictionary<string, object> { ["filePath"] = pdfFile };

        try
        {
            File.Move(tempFile, pdfFile);
            File.WriteAllText(pdfFile, "dummy content");

            // Act
            var result = _resolver.Resolve("resource-type", context);

            // Assert
            Assert.AreEqual("document", result);
        }
        finally
        {
            if (File.Exists(pdfFile))
                File.Delete(pdfFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Resource_Type_For_Image()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var jpgFile = Path.ChangeExtension(tempFile, ".jpg");
        var context = new Dictionary<string, object> { ["filePath"] = jpgFile };

        try
        {
            File.Move(tempFile, jpgFile);
            File.WriteAllText(jpgFile, "dummy content");

            // Act
            var result = _resolver.Resolve("resource-type", context);

            // Assert
            Assert.AreEqual("image", result);
        }
        finally
        {
            if (File.Exists(jpgFile))
                File.Delete(jpgFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Resource_Type_For_Media()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var mp4File = Path.ChangeExtension(tempFile, ".mp4");
        var context = new Dictionary<string, object> { ["filePath"] = mp4File };

        try
        {
            File.Move(tempFile, mp4File);
            File.WriteAllText(mp4File, "dummy content");

            // Act
            var result = _resolver.Resolve("resource-type", context);

            // Assert
            Assert.AreEqual("media", result);
        }
        finally
        {
            if (File.Exists(mp4File))
                File.Delete(mp4File);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Unknown_For_Unrecognized_Extension()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var unknownFile = Path.ChangeExtension(tempFile, ".unknown");
        var context = new Dictionary<string, object> { ["filePath"] = unknownFile };

        try
        {
            File.Move(tempFile, unknownFile);
            File.WriteAllText(unknownFile, "dummy content");

            // Act
            var result = _resolver.Resolve("resource-type", context);

            // Assert
            Assert.AreEqual("unknown", result);
        }
        finally
        {
            if (File.Exists(unknownFile))
                File.Delete(unknownFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Correct_Mime_Type()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".pdf");
        var context = new Dictionary<string, object> { ["filePath"] = pdfFile };

        try
        {
            File.Move(tempFile, pdfFile);
            File.WriteAllText(pdfFile, "dummy content");

            // Act
            var result = _resolver.Resolve("mime-type", context);

            // Assert
            Assert.AreEqual("application/pdf", result);
        }
        finally
        {
            if (File.Exists(pdfFile))
                File.Delete(pdfFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Dates()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var context = new Dictionary<string, object> { ["filePath"] = tempFile };

        try
        {
            // Act
            var createdDate = _resolver.Resolve("date-created", context);
            var modifiedDate = _resolver.Resolve("date-modified", context);

            // Assert
            Assert.IsNotNull(createdDate);
            Assert.IsNotNull(modifiedDate);
            Assert.IsTrue(DateTime.TryParse(createdDate.ToString(), out _));
            Assert.IsTrue(DateTime.TryParse(modifiedDate.ToString(), out _));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Null_For_Nonexistent_File()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/nonexistent/file.pdf" };

        // Act
        var result = _resolver.Resolve("file-name", context);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Resolve_Should_Return_Null_For_Invalid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act
        var result = _resolver.Resolve("file-name", context);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Return_Comprehensive_File_Analysis()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".pdf");
        var testContent = "This is test content for the PDF file.";
        var context = new Dictionary<string, object> { ["filePath"] = pdfFile };

        try
        {
            File.Move(tempFile, pdfFile);
            File.WriteAllText(pdfFile, testContent);

            // Act
            var metadata = _resolver.ExtractMetadata(context);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("file-name"));
            Assert.IsTrue(metadata.ContainsKey("file-extension"));
            Assert.IsTrue(metadata.ContainsKey("file-size"));
            Assert.IsTrue(metadata.ContainsKey("date-created"));
            Assert.IsTrue(metadata.ContainsKey("date-modified"));
            Assert.IsTrue(metadata.ContainsKey("resource-type"));
            Assert.IsTrue(metadata.ContainsKey("mime-type"));
            
            Assert.AreEqual(Path.GetFileName(pdfFile), metadata["file-name"]);
            Assert.AreEqual(".pdf", metadata["file-extension"]);
            Assert.AreEqual("document", metadata["resource-type"]);
            Assert.AreEqual("application/pdf", metadata["mime-type"]);
            Assert.IsTrue((long)metadata["file-size"] > 0);
        }
        finally
        {
            if (File.Exists(pdfFile))
                File.Delete(pdfFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
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
    public void ExtractMetadata_Should_Return_Empty_Dictionary_For_Missing_FilePath()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(0, metadata.Count);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Return_Empty_Dictionary_For_Nonexistent_File()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/nonexistent/file.pdf" };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(0, metadata.Count);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Skip_Image_Metadata_When_Configured()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var jpgFile = Path.ChangeExtension(tempFile, ".jpg");
        var context = new Dictionary<string, object> 
        { 
            ["filePath"] = jpgFile,
            ["extractImageMetadata"] = false
        };

        try
        {
            File.Move(tempFile, jpgFile);
            File.WriteAllText(jpgFile, "dummy image content");

            // Act
            var metadata = _resolver.ExtractMetadata(context);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("resource-type"));
            Assert.AreEqual("image", metadata["resource-type"]);
            Assert.IsFalse(metadata.ContainsKey("image-width"));
            Assert.IsFalse(metadata.ContainsKey("image-height"));
        }
        finally
        {
            if (File.Exists(jpgFile))
                File.Delete(jpgFile);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void ExtractMetadata_Should_Handle_Various_File_Extensions()
    {
        // Test various file extensions
        var testCases = new Dictionary<string, (string expectedType, string expectedMime)>
        {
            { ".pdf", ("document", "application/pdf") },
            { ".jpg", ("image", "image/jpeg") },
            { ".png", ("image", "image/png") },
            { ".mp4", ("media", "video/mp4") },
            { ".mp3", ("media", "audio/mpeg") },
            { ".txt", ("document", "text/plain") },
            { ".docx", ("document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document") },
            { ".unknown", ("unknown", "application/octet-stream") }
        };

        foreach (var testCase in testCases)
        {
            var tempFile = Path.GetTempFileName();
            var testFile = Path.ChangeExtension(tempFile, testCase.Key);
            var context = new Dictionary<string, object> { ["filePath"] = testFile };

            try
            {
                File.Move(tempFile, testFile);
                File.WriteAllText(testFile, "dummy content");

                // Act
                var metadata = _resolver.ExtractMetadata(context);

                // Assert
                Assert.AreEqual(testCase.Value.expectedType, metadata["resource-type"], 
                    $"Resource type mismatch for {testCase.Key}");
                Assert.AreEqual(testCase.Value.expectedMime, metadata["mime-type"], 
                    $"MIME type mismatch for {testCase.Key}");
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}