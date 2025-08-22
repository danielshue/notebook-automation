using NotebookAutomation.Core.Tools;
using NotebookAutomation.Core.Tools.Resolvers;
using NotebookAutomation.Tests.Core.Helpers;

namespace NotebookAutomation.Tests.Core.Tools.Integration;

/// <summary>
/// Integration tests for OneDrive relative path resolver functionality.
/// Tests verify that the resolver can be registered and used correctly with the template system.
/// </summary>
[TestClass]
public class MetadataProcessingIntegrationTests
{
    private string _testWorkingDirectory = null!;
    private string _testOneDriveRoot = null!;
    private string _testResourcesPath = null!;
    private AppConfig _testConfig = null!;
    private MetadataTemplateManager _templateManager = null!;
    private OneDriveRelativePathResolver _oneDriveResolver = null!;

    [TestInitialize]
    public void Initialize()
    {
        // Create test directory structure
        _testWorkingDirectory = Path.Combine(Path.GetTempPath(), "MetadataIntegrationTests", Guid.NewGuid().ToString());
        _testOneDriveRoot = Path.Combine(_testWorkingDirectory, "OneDrive");
        _testResourcesPath = Path.Combine(_testOneDriveRoot, "resources");

        Directory.CreateDirectory(_testResourcesPath);

        // Setup test configuration
        _testConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = _testWorkingDirectory,
                OnedriveFullpathRoot = _testOneDriveRoot,
                OnedriveResourcesBasepath = "resources"
            },
            PdfExtractImages = false
        };

        // Setup resolver and template manager
        var resolverLogger = new Mock<ILogger<OneDriveRelativePathResolver>>();
        _oneDriveResolver = new OneDriveRelativePathResolver(resolverLogger.Object, _testConfig);

        var templateLogger = new Mock<ILogger>();
        var schemaLoaderLogger = new Mock<ILogger<MetadataSchemaLoader>>();
        var schemaLoader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader(schemaLoaderLogger.Object);

        // Register the OneDrive resolver with all its aliases
        schemaLoader.ResolverRegistry.Register("OneDriveRelativePathResolver", _oneDriveResolver);
        schemaLoader.ResolverRegistry.Register("PdfOneDriveRelativePathResolver", _oneDriveResolver);
        schemaLoader.ResolverRegistry.Register("VideoOneDriveRelativePathResolver", _oneDriveResolver);
        schemaLoader.ResolverRegistry.Register("PdfTextOneDriveRelativePathResolver", _oneDriveResolver);

        _templateManager = new MetadataTemplateManager(templateLogger.Object, schemaLoader);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testWorkingDirectory))
        {
            Directory.Delete(_testWorkingDirectory, true);
        }
    }

    #region OneDrive Resolver Direct Tests

    [TestMethod]
    public void OneDriveResolver_PdfFields_ResolveCorrectRelativePaths()
    {
        // Arrange
        var pdfPath = Path.Combine(_testResourcesPath, "test-document.pdf");
        var pdfTextPath = Path.Combine(_testResourcesPath, "test-document.txt");

        File.WriteAllText(pdfPath, "test pdf content");
        File.WriteAllText(pdfTextPath, "extracted pdf text");

        var context = new Dictionary<string, object>
        {
            ["filePath"] = pdfPath,
            ["pdftext_file"] = pdfTextPath
        };

        // Act - Use resolver directly
        var legacyResult = _oneDriveResolver.Resolve("onedrive_relative_path", context);
        var pdfResult = _oneDriveResolver.Resolve("pdf-onedrive-relative-path", context);
        var pdfTextResult = _oneDriveResolver.Resolve("pdftext-onedrive-relative-path", context);

        // Assert
        Assert.IsNotNull(legacyResult);
        Assert.IsNotNull(pdfResult);
        Assert.IsNotNull(pdfTextResult);

        Assert.AreEqual("test-document.pdf", legacyResult.ToString());
        Assert.AreEqual("test-document.pdf", pdfResult.ToString());
        Assert.AreEqual("test-document.txt", pdfTextResult.ToString());
    }

    [TestMethod]
    public void OneDriveResolver_VideoFields_ResolveCorrectRelativePaths()
    {
        // Arrange
        var videoPath = Path.Combine(_testResourcesPath, "test-video.mp4");
        var transcriptPath = Path.Combine(_testResourcesPath, "test-video.txt");

        File.WriteAllText(videoPath, "test video content");
        File.WriteAllText(transcriptPath, "video transcript content");

        var context = new Dictionary<string, object>
        {
            ["filePath"] = videoPath,
            ["transcript"] = transcriptPath
        };

        // Act - Use resolver directly
        var legacyResult = _oneDriveResolver.Resolve("onedrive_relative_path", context);
        var videoResult = _oneDriveResolver.Resolve("video-onedrive-relative-path", context);
        var transcriptResult = _oneDriveResolver.Resolve("transcript-onedrive-relative-path", context);

        // Assert
        Assert.IsNotNull(legacyResult);
        Assert.IsNotNull(videoResult);
        Assert.IsNotNull(transcriptResult);

        Assert.AreEqual("test-video.mp4", legacyResult.ToString());
        Assert.AreEqual("test-video.mp4", videoResult.ToString());
        Assert.AreEqual("test-video.txt", transcriptResult.ToString());
    }

    [TestMethod]
    public void OneDriveResolver_FilesOutsideOneDriveRoot_ReturnsAbsolutePaths()
    {
        // Arrange
        var outsideDirectory = Path.Combine(Path.GetTempPath(), "outside-onedrive");
        Directory.CreateDirectory(outsideDirectory);

        var pdfPath = Path.Combine(outsideDirectory, "outside-document.pdf");
        var pdfTextPath = Path.Combine(outsideDirectory, "outside-document.txt");

        File.WriteAllText(pdfPath, "test pdf content");
        File.WriteAllText(pdfTextPath, "extracted pdf text");

        try
        {
            var context = new Dictionary<string, object>
            {
                ["filePath"] = pdfPath,
                ["pdftext_file"] = pdfTextPath
            };

            // Act - Use resolver directly
            var pdfResult = _oneDriveResolver.Resolve("pdf-onedrive-relative-path", context);
            var pdfTextResult = _oneDriveResolver.Resolve("pdftext-onedrive-relative-path", context);

            // Assert - Should return absolute paths since file is outside OneDrive root
            Assert.IsNotNull(pdfResult);
            Assert.IsNotNull(pdfTextResult);
            Assert.AreEqual(pdfPath, pdfResult.ToString());
            Assert.AreEqual(pdfTextPath, pdfTextResult.ToString());
        }
        finally
        {
            Directory.Delete(outsideDirectory, true);
        }
    }

    #endregion

    #region Resolver Registration Tests

    [TestMethod]
    public void ResolverRegistry_CanRegisterAndRetrieveOneDriveResolver()
    {
        // Arrange - Already set up in Initialize()

        // Act - Try to get resolvers from registry
        var registry = _templateManager.SchemaLoader.ResolverRegistry;

        var oneDriveResolver = registry.Get("OneDriveRelativePathResolver");
        var pdfResolver = registry.Get("PdfOneDriveRelativePathResolver");
        var videoResolver = registry.Get("VideoOneDriveRelativePathResolver");
        var textResolver = registry.Get("PdfTextOneDriveRelativePathResolver");

        // Assert
        Assert.IsNotNull(oneDriveResolver);
        Assert.IsNotNull(pdfResolver);
        Assert.IsNotNull(videoResolver);
        Assert.IsNotNull(textResolver);

        // All should be the same instance
        Assert.AreSame(_oneDriveResolver, oneDriveResolver);
        Assert.AreSame(_oneDriveResolver, pdfResolver);
        Assert.AreSame(_oneDriveResolver, videoResolver);
        Assert.AreSame(_oneDriveResolver, textResolver);
    }

    [TestMethod]
    public void ResolverRegistry_MultipleAliases_ShareSameInstance()
    {
        // Arrange
        var registry = _templateManager.SchemaLoader.ResolverRegistry;

        // Act - Get resolvers using different aliases
        var resolver1 = registry.Get("OneDriveRelativePathResolver");
        var resolver2 = registry.Get("PdfOneDriveRelativePathResolver");
        var resolver3 = registry.Get("VideoOneDriveRelativePathResolver");
        var resolver4 = registry.Get("PdfTextOneDriveRelativePathResolver");

        // Assert - All should be the same instance
        Assert.AreSame(resolver1, resolver2);
        Assert.AreSame(resolver2, resolver3);
        Assert.AreSame(resolver3, resolver4);
        Assert.AreSame(resolver1, _oneDriveResolver);
    }

    #endregion

    #region Template Integration Tests

    [TestMethod]
    public void TemplateManager_CanLoadTemplatesWithOneDriveResolver()
    {
        // Arrange - Already set up in Initialize()

        // Act
        var templateTypes = _templateManager.GetTemplateTypes();

        // Assert
        Assert.IsTrue(templateTypes.Count > 0, "Should have at least one template type");
        Assert.IsTrue(templateTypes.Contains("pdf-reference"), "Should have pdf-reference template");
        Assert.IsTrue(templateTypes.Contains("video-reference"), "Should have video-reference template");

        // Verify we can get templates
        var pdfTemplate = _templateManager.GetTemplate("pdf-reference");
        var videoTemplate = _templateManager.GetTemplate("video-reference");

        Assert.IsNotNull(pdfTemplate);
        Assert.IsNotNull(videoTemplate);

        // Templates should have basic expected fields
        Assert.IsTrue(pdfTemplate.ContainsKey("title"));
        Assert.IsTrue(videoTemplate.ContainsKey("title"));
    }

    [TestMethod]
    public void TemplateManager_ResolveTemplateFields_WorksWithContext()
    {
        // Arrange
        var pdfPath = Path.Combine(_testResourcesPath, "template-test.pdf");
        var transcriptPath = Path.Combine(_testResourcesPath, "template-test.txt");

        File.WriteAllText(pdfPath, "test pdf content");
        File.WriteAllText(transcriptPath, "test transcript");

        var pdfContext = new Dictionary<string, object>
        {
            ["filePath"] = pdfPath
        };

        var videoContext = new Dictionary<string, object>
        {
            ["filePath"] = pdfPath, // Using PDF file as mock video
            ["transcript"] = transcriptPath
        };

        // Act
        var pdfFields = _templateManager.ResolveTemplateFields("pdf-reference", pdfContext);
        var videoFields = _templateManager.ResolveTemplateFields("video-reference", videoContext);

        // Assert - Templates should resolve without error
        Assert.IsNotNull(pdfFields);
        Assert.IsNotNull(videoFields);

        // Should have some resolved fields
        Assert.IsTrue(pdfFields.Count > 0);
        Assert.IsTrue(videoFields.Count > 0);

        // PDF template should contain OneDrive fields that are now defined in schema
        Assert.IsTrue(pdfFields.ContainsKey("pdf-onedrive-relative-path"));
        Assert.AreEqual("template-test.pdf", pdfFields["pdf-onedrive-relative-path"]);

        // Video template should contain the OneDrive fields that are defined in schema
        Assert.IsTrue(videoFields.ContainsKey("transcript-onedrive-relative-path"));
        Assert.IsTrue(videoFields.ContainsKey("video-onedrive-relative-path"));
        Assert.AreEqual("template-test.txt", videoFields["transcript-onedrive-relative-path"]);
        Assert.AreEqual("template-test.pdf", videoFields["video-onedrive-relative-path"]);  // Using PDF file as mock video
    }

    #endregion

    #region Cross-Type Consistency Tests

    [TestMethod]
    public void OneDriveResolver_DifferentFileTypes_ProduceConsistentPaths()
    {
        // Arrange - Create files in a subdirectory to test path consistency
        var subDir = Path.Combine(_testResourcesPath, "subfolder");
        Directory.CreateDirectory(subDir);

        var pdfPath = Path.Combine(subDir, "document.pdf");
        var videoPath = Path.Combine(subDir, "video.mp4");
        var transcriptPath = Path.Combine(subDir, "video.txt");
        var pdfTextPath = Path.Combine(subDir, "document.txt");

        // Create test files
        File.WriteAllText(pdfPath, "test pdf");
        File.WriteAllText(videoPath, "test video");
        File.WriteAllText(transcriptPath, "video transcript");
        File.WriteAllText(pdfTextPath, "extracted pdf text");

        var pdfContext = new Dictionary<string, object>
        {
            ["filePath"] = pdfPath,
            ["pdftext_file"] = pdfTextPath
        };

        var videoContext = new Dictionary<string, object>
        {
            ["filePath"] = videoPath,
            ["transcript"] = transcriptPath
        };

        // Act - Resolve paths with different contexts
        var pdfResult = _oneDriveResolver.Resolve("pdf-onedrive-relative-path", pdfContext);
        var pdfTextResult = _oneDriveResolver.Resolve("pdftext-onedrive-relative-path", pdfContext);
        var videoResult = _oneDriveResolver.Resolve("video-onedrive-relative-path", videoContext);
        var transcriptResult = _oneDriveResolver.Resolve("transcript-onedrive-relative-path", videoContext);

        // Assert - All should produce consistent relative paths for subfolder location
        var expectedPdfPath = Path.Combine("subfolder", "document.pdf");
        var expectedPdfTextPath = Path.Combine("subfolder", "document.txt");
        var expectedVideoPath = Path.Combine("subfolder", "video.mp4");
        var expectedTranscriptPath = Path.Combine("subfolder", "video.txt");

        Assert.AreEqual(expectedPdfPath, pdfResult.ToString());
        Assert.AreEqual(expectedPdfTextPath, pdfTextResult.ToString());
        Assert.AreEqual(expectedVideoPath, videoResult.ToString());
        Assert.AreEqual(expectedTranscriptPath, transcriptResult.ToString());

        // All should contain the subfolder in their paths
        Assert.IsTrue(pdfResult.ToString()!.Contains("subfolder"));
        Assert.IsTrue(pdfTextResult.ToString()!.Contains("subfolder"));
        Assert.IsTrue(videoResult.ToString()!.Contains("subfolder"));
        Assert.IsTrue(transcriptResult.ToString()!.Contains("subfolder"));
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public void OneDriveResolver_WithInvalidConfig_HandlesGracefully()
    {
        // Arrange - Config with invalid OneDrive root (should still be a valid path structure)
        var invalidConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = _testWorkingDirectory,
                OnedriveFullpathRoot = Path.Combine(Path.GetTempPath(), "nonexistent-onedrive-root"),
                OnedriveResourcesBasepath = "resources"
            },
            PdfExtractImages = false
        };

        var invalidResolver = new OneDriveRelativePathResolver(
            Mock.Of<ILogger<OneDriveRelativePathResolver>>(), invalidConfig);

        var testFilePath = Path.Combine(_testResourcesPath, "test-file.pdf");
        File.WriteAllText(testFilePath, "test content");

        var context = new Dictionary<string, object> { ["filePath"] = testFilePath };

        // Act
        var result = invalidResolver.Resolve("pdf-onedrive-relative-path", context);

        // Assert - Should return the absolute path when file is not under OneDrive root
        Assert.IsNotNull(result);
        Assert.AreEqual(testFilePath, result.ToString());
    }

    [TestMethod]
    public void OneDriveResolver_WithMissingContextKeys_ReturnsEmptyValues()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["someOtherKey"] = "someValue" // Missing the expected keys
        };

        // Act
        var pdfResult = _oneDriveResolver.Resolve("pdf-onedrive-relative-path", context);
        var videoResult = _oneDriveResolver.Resolve("video-onedrive-relative-path", context);
        var transcriptResult = _oneDriveResolver.Resolve("transcript-onedrive-relative-path", context);

        // Assert - Should return empty strings when context keys are missing
        Assert.IsTrue(string.IsNullOrEmpty(pdfResult?.ToString()));
        Assert.IsTrue(string.IsNullOrEmpty(videoResult?.ToString()));
        Assert.IsTrue(string.IsNullOrEmpty(transcriptResult?.ToString()));
    }

    #endregion
}
