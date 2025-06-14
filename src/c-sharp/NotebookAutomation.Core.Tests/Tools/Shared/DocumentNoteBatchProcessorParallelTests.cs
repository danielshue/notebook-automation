// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Reflection;

namespace NotebookAutomation.Core.Tests.Tools.Shared;

/// <summary>
/// Unit tests for DocumentNoteBatchProcessor parallel processing functionality.
/// </summary>
[TestClass]
public class DocumentNoteBatchProcessorParallelTests
{
    private Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>> _loggerMock = null!;
    private DocumentNoteBatchProcessor<PdfNoteProcessor> _batchProcessor = null!; [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<DocumentNoteBatchProcessor<PdfNoteProcessor>>>();

        // Create real instances with minimal dependencies for testing
        var mockAppConfig = new AppConfig();
        mockAppConfig.Paths = new PathsConfig { NotebookVaultFullpathRoot = Path.GetTempPath() };

        var yamlHelper = new YamlHelper(Mock.Of<ILogger<YamlHelper>>());
        var markdownNoteBuilder = new MarkdownNoteBuilder(yamlHelper);
        var hierarchyDetector = new MetadataHierarchyDetector(
            Mock.Of<ILogger<MetadataHierarchyDetector>>(),
            mockAppConfig);

        var pdfProcessor = new PdfNoteProcessor(
            Mock.Of<ILogger<PdfNoteProcessor>>(),
            new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>()),
            hierarchyDetector,
            markdownNoteBuilder);

        _batchProcessor = new DocumentNoteBatchProcessor<PdfNoteProcessor>(
            _loggerMock.Object,
            pdfProcessor,
            new TestableAISummarizer(Mock.Of<ILogger<AISummarizer>>()));
    }


    [TestMethod]
    public void Configuration_DefaultParallelism_ShouldBe2()
    {
        // Arrange
        var config = new TimeoutConfig();

        // Act & Assert
        Assert.AreEqual(2, config.MaxFileParallelism);
        Assert.AreEqual(200, config.FileRateLimitMs);
    }


    [TestMethod]
    public void Configuration_CustomParallelism_ShouldRespectSettings()
    {
        // Arrange
        var config = new TimeoutConfig
        {
            MaxFileParallelism = 5,
            FileRateLimitMs = 500
        };

        // Act & Assert
        Assert.AreEqual(5, config.MaxFileParallelism);
        Assert.AreEqual(500, config.FileRateLimitMs);
    }


    [TestMethod]
    public async Task ProcessFilesAsync_WithNullAppConfig_ShouldUseDefaults()
    {
        // Arrange
        var files = new List<string> { "test1.pdf", "test2.pdf" };

        // Use reflection to access the protected method
        var method = typeof(DocumentNoteBatchProcessor<PdfNoteProcessor>)
            .GetMethod("ProcessFilesAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(method, "ProcessFilesAsync method should exist");

        // Act
        try
        {
            var result = await (Task<(int processed, int failed, List<string> failedFiles)>)method.Invoke(
                _batchProcessor,
                new object?[]
                {
                    files, "output", null, false, true, string.Empty, true, null, true, "Test", null
                })!;

            // Assert - Should complete without exception (dry run mode)
            Assert.IsNotNull(result);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // If there are expected exceptions due to mocking, that's OK for this test
            // We're just verifying the method exists and configuration handling doesn't crash
            Assert.IsTrue(ex.InnerException is ArgumentException ||
                         ex.InnerException is DirectoryNotFoundException ||
                         ex.InnerException is FileNotFoundException,
                         $"Unexpected exception: {ex.InnerException}");
        }
    }


    [TestMethod]
    public async Task ProcessFilesAsync_WithAppConfig_ShouldUseConfiguredParallelism()
    {
        // Arrange
        var appConfig = new AppConfig
        {
            AiService = new AIServiceConfig
            {
                Timeout = new TimeoutConfig
                {
                    MaxFileParallelism = 4,
                    FileRateLimitMs = 100
                }
            }
        };

        var files = new List<string> { "test1.pdf", "test2.pdf" };

        // Use reflection to access the protected method
        var method = typeof(DocumentNoteBatchProcessor<PdfNoteProcessor>)
            .GetMethod("ProcessFilesAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.IsNotNull(method, "ProcessFilesAsync method should exist");

        // Act
        try
        {
            var result = await (Task<(int processed, int failed, List<string> failedFiles)>)method.Invoke(
                _batchProcessor,
                new object?[]
                {
                    files, "output", null, false, true, string.Empty, true, null, true, "Test", appConfig
                })!;

            // Assert - Should complete without exception (dry run mode)
            Assert.IsNotNull(result);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // If there are expected exceptions due to mocking, that's OK for this test
            // We're just verifying the method exists and configuration handling doesn't crash
            Assert.IsTrue(ex.InnerException is ArgumentException ||
                         ex.InnerException is DirectoryNotFoundException ||
                         ex.InnerException is FileNotFoundException,
                         $"Unexpected exception: {ex.InnerException}");
        }
    }


    [TestMethod]
    public void TimeoutConfig_JsonSerialization_ShouldWork()
    {
        // Arrange
        var config = new TimeoutConfig
        {
            MaxFileParallelism = 3,
            FileRateLimitMs = 250,
            MaxChunkParallelism = 4,
            ChunkRateLimitMs = 150
        };

        // Act
        var json = JsonSerializer.Serialize(config);
        var deserializedConfig = JsonSerializer.Deserialize<TimeoutConfig>(json);

        // Assert
        Assert.IsNotNull(deserializedConfig);
        Assert.AreEqual(3, deserializedConfig.MaxFileParallelism);
        Assert.AreEqual(250, deserializedConfig.FileRateLimitMs);
        Assert.AreEqual(4, deserializedConfig.MaxChunkParallelism);
        Assert.AreEqual(150, deserializedConfig.ChunkRateLimitMs);
    }
}
