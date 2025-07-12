using Microsoft.Extensions.Logging;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

/// <summary>
/// Unit tests for <see cref="TranscriptResolver"/>.
/// </summary>
[TestClass]
public class TranscriptResolverTests
{
    private ILogger<TranscriptResolver> _logger;
    private TranscriptResolver _resolver;

    [TestInitialize]
    public void Initialize()
    {
        _logger = new Mock<ILogger<TranscriptResolver>>().Object;
        _resolver = new TranscriptResolver(_logger);
    }

    [TestMethod]
    public void FileType_Should_Return_Transcript()
    {
        // Act
        var fileType = _resolver.FileType;

        // Assert
        Assert.AreEqual("transcript", fileType);
    }

    [TestMethod]
    public void CanResolve_Should_Return_True_For_Supported_Fields_With_Valid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/file.mp4" };

        // Act & Assert
        Assert.IsTrue(_resolver.CanResolve("transcript-path", context));
        Assert.IsTrue(_resolver.CanResolve("transcript-exists", context));
        Assert.IsTrue(_resolver.CanResolve("transcript-format", context));
        Assert.IsTrue(_resolver.CanResolve("transcript-duration", context));
        Assert.IsTrue(_resolver.CanResolve("transcript-word-count", context));
        Assert.IsTrue(_resolver.CanResolve("transcript-content", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Unsupported_Fields()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/file.mp4" };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("unsupported-field", context));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_For_Null_Context()
    {
        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("transcript-path", null));
    }

    [TestMethod]
    public void CanResolve_Should_Return_False_Without_FilePath()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act & Assert
        Assert.IsFalse(_resolver.CanResolve("transcript-path", context));
    }

    [TestMethod]
    public void Resolve_Should_Return_True_For_Transcript_Exists_With_Explicit_Path()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world, this is a test transcript.

2
00:00:05,000 --> 00:00:10,000
This is the second line of the transcript.
";

        try
        {
            File.WriteAllText(tempFile, srtContent);
            var context = new Dictionary<string, object> 
            { 
                ["filePath"] = "/path/to/video.mp4",
                ["transcriptPath"] = tempFile
            };

            // Act
            var result = _resolver.Resolve("transcript-exists", context);

            // Assert
            Assert.AreEqual(true, result);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_False_For_Transcript_Exists_Without_File()
    {
        // Arrange
        var context = new Dictionary<string, object> 
        { 
            ["filePath"] = "/path/to/video.mp4"
        };

        // Act
        var result = _resolver.Resolve("transcript-exists", context);

        // Assert
        Assert.AreEqual(false, result);
    }

    [TestMethod]
    public void Resolve_Should_Return_Srt_Format_For_Srt_File()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".srt");
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world, this is a test transcript.
";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, srtContent);
            var context = new Dictionary<string, object> { ["filePath"] = newPath };

            // Act
            var result = _resolver.Resolve("transcript-format", context);

            // Assert
            Assert.AreEqual("srt", result);
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Count_Words_In_Transcript()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".srt");
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world this is a test transcript.

2
00:00:05,000 --> 00:00:10,000
This is the second line of the transcript.
";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, srtContent);
            var context = new Dictionary<string, object> { ["filePath"] = newPath };

            // Act
            var result = _resolver.Resolve("transcript-word-count", context);

            // Assert
            Assert.AreEqual(15, result); // Total words in both lines
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Extract_Plain_Text_From_Srt()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".srt");
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world, this is a test transcript.

2
00:00:05,000 --> 00:00:10,000
This is the second line.

";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, srtContent);
            var context = new Dictionary<string, object> { ["filePath"] = newPath };

            // Act
            var result = _resolver.Resolve("transcript-content", context) as string;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("Hello world, this is a test transcript."));
            Assert.IsTrue(result.Contains("This is the second line."));
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Calculate_Duration_From_Srt()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".srt");
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world, this is a test transcript.

2
00:00:05,000 --> 00:00:10,000
This is the second line.

";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, srtContent);
            var context = new Dictionary<string, object> { ["filePath"] = newPath };

            // Act
            var result = _resolver.Resolve("transcript-duration", context);

            // Assert
            Assert.AreEqual(10.0, result); // Duration should be 10 seconds
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Extract_Segments_From_Srt()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".srt");
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world, this is a test transcript.

2
00:00:05,000 --> 00:00:10,000
This is the second line.

";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, srtContent);
            var context = new Dictionary<string, object> { ["filePath"] = newPath };

            // Act
            var result = _resolver.Resolve("transcript-segments", context) as List<object>;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void Resolve_Should_Return_Null_For_Invalid_Context()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["other"] = "value" };

        // Act
        var result = _resolver.Resolve("transcript-path", context);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractMetadata_Should_Return_Comprehensive_Transcript_Analysis()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".srt");
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world, this is a test transcript.

2
00:00:05,000 --> 00:00:10,000
This is the second line of the transcript.

";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, srtContent);
            var context = new Dictionary<string, object> { ["filePath"] = newPath };

            // Act
            var metadata = _resolver.ExtractMetadata(context);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("transcript-exists"));
            Assert.IsTrue(metadata.ContainsKey("transcript-path"));
            Assert.IsTrue(metadata.ContainsKey("transcript-format"));
            Assert.IsTrue(metadata.ContainsKey("transcript-word-count"));
            Assert.IsTrue(metadata.ContainsKey("transcript-duration"));
            Assert.IsTrue(metadata.ContainsKey("transcript-content"));
            Assert.IsTrue(metadata.ContainsKey("transcript-segments"));
            
            Assert.AreEqual(true, metadata["transcript-exists"]);
            Assert.AreEqual(newPath, metadata["transcript-path"]);
            Assert.AreEqual("srt", metadata["transcript-format"]);
            Assert.AreEqual(15, metadata["transcript-word-count"]);
            Assert.AreEqual(10.0, metadata["transcript-duration"]);
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
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
    public void ExtractMetadata_Should_Handle_Missing_Transcript_File()
    {
        // Arrange
        var context = new Dictionary<string, object> { ["filePath"] = "/path/to/nonexistent.mp4" };

        // Act
        var metadata = _resolver.ExtractMetadata(context);

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(false, metadata["transcript-exists"]);
        Assert.AreEqual(1, metadata.Count); // Should only contain transcript-exists
    }

    [TestMethod]
    public void ExtractMetadata_Should_Handle_Plain_Text_Transcript()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".txt");
        var textContent = "This is a plain text transcript with multiple words to count.";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, textContent);
            var context = new Dictionary<string, object> { ["filePath"] = newPath };

            // Act
            var metadata = _resolver.ExtractMetadata(context);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("transcript-exists"));
            Assert.IsTrue(metadata.ContainsKey("transcript-format"));
            Assert.IsTrue(metadata.ContainsKey("transcript-word-count"));
            Assert.IsTrue(metadata.ContainsKey("transcript-content"));
            
            Assert.AreEqual(true, metadata["transcript-exists"]);
            Assert.AreEqual("txt", metadata["transcript-format"]);
            Assert.AreEqual(11, metadata["transcript-word-count"]);
            Assert.AreEqual(textContent, metadata["transcript-content"]);
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void ExtractMetadata_Should_Skip_Content_When_Configured()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".txt");
        var textContent = "This is a plain text transcript.";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, textContent);
            var context = new Dictionary<string, object> 
            { 
                ["filePath"] = newPath,
                ["extractContent"] = false
            };

            // Act
            var metadata = _resolver.ExtractMetadata(context);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("transcript-exists"));
            Assert.IsFalse(metadata.ContainsKey("transcript-content"));
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void ExtractMetadata_Should_Skip_Timing_When_Configured()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var newPath = Path.ChangeExtension(tempFile, ".srt");
        var srtContent = @"1
00:00:01,000 --> 00:00:05,000
Hello world, this is a test transcript.

";

        try
        {
            File.Move(tempFile, newPath);
            File.WriteAllText(newPath, srtContent);
            var context = new Dictionary<string, object> 
            { 
                ["filePath"] = newPath,
                ["extractTimings"] = false
            };

            // Act
            var metadata = _resolver.ExtractMetadata(context);

            // Assert
            Assert.IsTrue(metadata.ContainsKey("transcript-exists"));
            Assert.IsFalse(metadata.ContainsKey("transcript-segments"));
        }
        finally
        {
            if (File.Exists(newPath))
                File.Delete(newPath);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}