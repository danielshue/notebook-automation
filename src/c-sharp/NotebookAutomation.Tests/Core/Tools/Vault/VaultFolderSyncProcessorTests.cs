// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Tools.Vault;

/// <summary>
/// Unit tests for VaultFolderSyncProcessor.
/// </summary>
[TestClass]
public class VaultFolderSyncProcessorTests
{
    private readonly Mock<ILogger<VaultFolderSyncProcessor>> _mockLogger = new();
    private readonly Mock<AppConfig> _mockAppConfig = new();
    private readonly Mock<IMarkdownNoteBuilder> _mockMarkdownNoteBuilder = new();
    private readonly Mock<IMetadataTemplateManager> _mockMetadataTemplateManager = new();
    private readonly PathsConfig _pathsConfig = new();
    private VaultFolderSyncProcessor _processor = null!;
    private readonly string _testOneDriveRoot = Path.Combine(Path.GetTempPath(), "TestOneDrive");
    private readonly string _testVaultRoot = Path.Combine(Path.GetTempPath(), "TestVault");


    /// <summary>
    /// Initializes test environment before each test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        // Setup paths configuration
        _pathsConfig.OnedriveFullpathRoot = _testOneDriveRoot;
        _pathsConfig.OnedriveResourcesBasepath = "Resources";
        _pathsConfig.NotebookVaultFullpathRoot = _testVaultRoot;

        // Setup AppConfig mock
        _mockAppConfig.Setup(c => c.Paths).Returns(_pathsConfig);

        // Create processor
        _processor = new VaultFolderSyncProcessor(_mockLogger.Object, _mockAppConfig.Object, _mockMarkdownNoteBuilder.Object, _mockMetadataTemplateManager.Object);

        // Clean up and create test directories
        CleanupTestDirectories();
        CreateTestDirectories();
    }


    /// <summary>
    /// Cleans up test environment after each test.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        CleanupTestDirectories();
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync throws argument exception for null OneDrive path.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_ThrowsArgumentException_WhenOneDrivePathIsNull()
    {
        // Act
        var result = await _processor.SyncDirectoriesAsync(null!, _testVaultRoot);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage!.Contains("OneDrive path cannot be null or empty"));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync throws argument exception for empty OneDrive path.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_ThrowsArgumentException_WhenOneDrivePathIsEmpty()
    {
        // Act
        var result = await _processor.SyncDirectoriesAsync(string.Empty, _testVaultRoot);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage!.Contains("OneDrive path cannot be null or empty"));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync uses default vault path from config when vault path is null.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_UsesDefaultVaultPath_WhenVaultPathIsNull()
    {
        // Arrange
        var testOneDriveSubPath = "MBA/Finance";
        CreateTestOneDriveStructure(testOneDriveSubPath);

        // Act
        var result = await _processor.SyncDirectoriesAsync(testOneDriveSubPath, null);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.TotalFolders >= 0);
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync fails when OneDrive source directory does not exist.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_Fails_WhenOneDriveSourceDoesNotExist()
    {
        // Arrange
        var nonExistentPath = "NonExistent/Path";

        // Act
        var result = await _processor.SyncDirectoriesAsync(nonExistentPath, _testVaultRoot);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage!.Contains("OneDrive source directory does not exist"));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync successfully creates missing directories.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_CreatesDirectories_WhenTheyDontExist()
    {
        // Arrange
        var testPath = "MBA/Finance";
        CreateTestOneDriveStructure(testPath);

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.CreatedVaultFolders > 0);
        Assert.AreEqual(result.CreatedVaultFolders, result.SynchronizedFolders);
        Assert.AreEqual(0, result.SkippedFolders);
        Assert.AreEqual(0, result.FailedFolders);

        // Verify directories were actually created
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1", "Module1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course2")));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync skips existing directories.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_SkipsExistingDirectories_WhenTheyAlreadyExist()
    {
        // Arrange
        var testPath = "MBA/Finance";
        CreateTestOneDriveStructure(testPath);

        // Create some directories in vault first
        var existingDir = Path.Combine(_testVaultRoot, "Course1");
        Directory.CreateDirectory(existingDir);

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.SkippedFolders > 0);
        Assert.IsTrue(result.CreatedVaultFolders < result.TotalFolders);
        Assert.AreEqual(result.CreatedVaultFolders + result.SkippedFolders, result.SynchronizedFolders);
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync dry run mode doesn't create directories.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_DryRun_DoesNotCreateDirectories()
    {
        // Arrange
        var testPath = "MBA/Finance";
        CreateTestOneDriveStructure(testPath);

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.CreatedVaultFolders > 0); // Should report what would be created
        Assert.IsTrue(result.SynchronizedFolders > 0);

        // Verify directories were NOT actually created
        Assert.IsFalse(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_testVaultRoot, "Course2")));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync handles missing configuration gracefully.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_Fails_WhenOneDriveRootNotConfigured()
    {
        // Arrange
        _pathsConfig.OnedriveFullpathRoot = string.Empty;
        var testPath = "MBA/Finance";

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage!.Contains("OneDrive root path not configured"));
    }


    /// <summary>
    /// Creates test directory structure in OneDrive.
    /// </summary>
    /// <param name="relativePath">The relative path within OneDrive to create structure.</param>
    private void CreateTestOneDriveStructure(string relativePath)
    {
        var basePath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, relativePath);

        // Create test directory structure
        Directory.CreateDirectory(Path.Combine(basePath, "Course1", "Module1"));
        Directory.CreateDirectory(Path.Combine(basePath, "Course1", "Module2"));
        Directory.CreateDirectory(Path.Combine(basePath, "Course2"));
        Directory.CreateDirectory(Path.Combine(basePath, "Course2", "Resources"));
    }


    /// <summary>
    /// Creates test directories.
    /// </summary>

    private void CreateTestDirectories()
    {
        Directory.CreateDirectory(_testOneDriveRoot);
        Directory.CreateDirectory(_testVaultRoot);
    }


    /// <summary>
    /// Cleans up test directories.
    /// </summary>

    private void CleanupTestDirectories()
    {
        try
        {
            if (Directory.Exists(_testOneDriveRoot))
            {
                Directory.Delete(_testOneDriveRoot, recursive: true);
            }
            if (Directory.Exists(_testVaultRoot))
            {
                Directory.Delete(_testVaultRoot, recursive: true);
            }
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync creates directories in both directions when bidirectional is enabled.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_CreatesBidirectional_WhenBidirectionalEnabled()
    {
        // Arrange
        var testPath = "MBA/Finance";
        CreateTestOneDriveStructure(testPath);

        // Create some vault-only directories
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "VaultOnly"));
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "VaultOnly", "SubFolder"));

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: true, recursive: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.CreatedVaultFolders > 0); // OneDrive folders created in vault
        Assert.IsTrue(result.CreatedOneDriveFolders > 0); // Vault folders created in OneDrive
        Assert.IsTrue(result.SynchronizedFolders > 0);

        // Verify OneDrive directories were created in vault
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1", "Module1")));

        // Verify vault directories were created in OneDrive
        var oneDriveTargetPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Assert.IsTrue(Directory.Exists(Path.Combine(oneDriveTargetPath, "VaultOnly")));
        Assert.IsTrue(Directory.Exists(Path.Combine(oneDriveTargetPath, "VaultOnly", "SubFolder")));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync dry run mode reports bidirectional changes without creating directories.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_BidirectionalDryRun_DoesNotCreateDirectories()
    {
        // Arrange
        var testPath = "MBA/Finance";
        CreateTestOneDriveStructure(testPath);

        // Create some vault-only directories
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "VaultOnly"));

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: true, bidirectional: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.CreatedVaultFolders > 0); // Should report what would be created in vault
        Assert.IsTrue(result.CreatedOneDriveFolders > 0); // Should report what would be created in OneDrive
        Assert.IsTrue(result.SynchronizedFolders > 0);

        // Verify directories were NOT actually created
        var oneDriveTargetPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Assert.IsFalse(Directory.Exists(Path.Combine(oneDriveTargetPath, "VaultOnly")), "VaultOnly should NOT be created in OneDrive during dry run");

        // OneDrive directories should also not be created in vault during dry run
        Assert.IsFalse(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")), "Course1 should NOT be created in vault during dry run");
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync unidirectional mode only creates vault directories.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_UnidirectionalMode_OnlyCreatesVaultDirectories()
    {
        // Arrange
        var testPath = "MBA/Finance";
        CreateTestOneDriveStructure(testPath);

        // Create some vault-only directories
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "VaultOnly"));

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.CreatedVaultFolders > 0); // OneDrive folders created in vault
        Assert.AreEqual(0, result.CreatedOneDriveFolders); // No vault folders should be created in OneDrive
        Assert.IsTrue(result.SynchronizedFolders > 0);

        // Verify OneDrive directories were created in vault
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")));

        // Verify vault directories were NOT created in OneDrive
        var oneDriveTargetPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Assert.IsFalse(Directory.Exists(Path.Combine(oneDriveTargetPath, "VaultOnly")), "VaultOnly should NOT be created in OneDrive in unidirectional mode");
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync handles mixed scenarios in bidirectional mode.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_BidirectionalMode_HandlesMixedScenarios()
    {
        // Arrange
        var testPath = "MBA/Finance";
        CreateTestOneDriveStructure(testPath);

        // Create some directories that exist in both locations
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "Course1")); // Exists in both

        // Create vault-only directory
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "VaultExclusive"));

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: true, recursive: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.CreatedVaultFolders >= 0); // Some OneDrive folders created in vault
        Assert.IsTrue(result.CreatedOneDriveFolders > 0); // VaultExclusive created in OneDrive
        Assert.IsTrue(result.SkippedFolders > 0); // Course1 should be skipped as it exists in both
        Assert.IsTrue(result.SynchronizedFolders > 0);
        Assert.AreEqual(0, result.FailedFolders);

        // Verify mixed scenario results
        var oneDriveTargetPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Assert.IsTrue(Directory.Exists(Path.Combine(oneDriveTargetPath, "VaultExclusive")), "VaultExclusive should be created in OneDrive");
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")), "Course1 should still exist in vault");
        Assert.IsTrue(Directory.Exists(Path.Combine(oneDriveTargetPath, "Course1")), "Course1 should still exist in OneDrive");
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync with recursive=false processes only immediate children.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_NonRecursive_ProcessesOnlyImmediateChildren()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Directory.CreateDirectory(oneDriveTestPath);

        // Create nested directory structure
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course1"));
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course2"));
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course1", "Module1")); // Nested - should not be processed
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course1", "Module2")); // Nested - should not be processed

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.CreatedVaultFolders); // Only Course1 and Course2, not the nested modules
        Assert.AreEqual(2, result.SynchronizedFolders);
        Assert.AreEqual(0, result.SkippedFolders);
        Assert.AreEqual(0, result.FailedFolders);

        // Verify only immediate children were created
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course2")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_testVaultRoot, "Course1", "Module1")), "Nested Module1 should NOT be created in non-recursive mode");
        Assert.IsFalse(Directory.Exists(Path.Combine(_testVaultRoot, "Course1", "Module2")), "Nested Module2 should NOT be created in non-recursive mode");
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync with recursive=true processes entire directory tree.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_Recursive_ProcessesEntireDirectoryTree()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Directory.CreateDirectory(oneDriveTestPath);

        // Create nested directory structure
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course1"));
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course2"));
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course1", "Module1"));
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course1", "Module2"));
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "Course1", "Module1", "Lesson1"));

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(5, result.CreatedVaultFolders); // Course1, Course2, Module1, Module2, Lesson1
        Assert.AreEqual(5, result.SynchronizedFolders);
        Assert.AreEqual(0, result.SkippedFolders);
        Assert.AreEqual(0, result.FailedFolders);

        // Verify entire directory tree was created
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course2")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1", "Module1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1", "Module2")));
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "Course1", "Module1", "Lesson1")));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync with bidirectional=true and recursive=false processes only immediate children in both directions.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_BidirectionalNonRecursive_ProcessesOnlyImmediateChildren()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Directory.CreateDirectory(oneDriveTestPath);

        // Create OneDrive structure with nested directories
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "OneDriveCourse1"));
        Directory.CreateDirectory(Path.Combine(oneDriveTestPath, "OneDriveCourse1", "Module1")); // Nested - should not be processed

        // Create vault structure with nested directories
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "VaultCourse1"));
        Directory.CreateDirectory(Path.Combine(_testVaultRoot, "VaultCourse1", "Module1")); // Nested - should not be processed

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: true, recursive: false);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.CreatedVaultFolders); // Only OneDriveCourse1, not the nested module
        Assert.AreEqual(1, result.CreatedOneDriveFolders); // Only VaultCourse1, not the nested module
        Assert.AreEqual(2, result.SynchronizedFolders);
        Assert.AreEqual(0, result.SkippedFolders);
        Assert.AreEqual(0, result.FailedFolders);

        // Verify only immediate children were created in both directions
        Assert.IsTrue(Directory.Exists(Path.Combine(_testVaultRoot, "OneDriveCourse1")));
        Assert.IsTrue(Directory.Exists(Path.Combine(oneDriveTestPath, "VaultCourse1")));
        Assert.IsFalse(Directory.Exists(Path.Combine(_testVaultRoot, "OneDriveCourse1", "Module1")), "Nested OneDrive module should NOT be created in non-recursive mode");
        Assert.IsFalse(Directory.Exists(Path.Combine(oneDriveTestPath, "VaultCourse1", "Module1")), "Nested vault module should NOT be created in non-recursive mode");
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync creates placeholder markdown files for document types.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_CreatesPlaceholderFiles_WhenDocumentTypesSpecified()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath, "Course1");
        Directory.CreateDirectory(oneDriveTestPath);

        // Create test document files
        File.WriteAllText(Path.Combine(oneDriveTestPath, "lecture.mp4"), "test video content");
        File.WriteAllText(Path.Combine(oneDriveTestPath, "slides.pdf"), "test pdf content");
        File.WriteAllText(Path.Combine(oneDriveTestPath, "reading.html"), "test html content");

        // Setup template manager mock
        var templateMetadata = new Dictionary<string, object>
        {
            ["template-type"] = "video-reference",
            ["type"] = "note/video-note",
            ["title"] = "test",
            ["status"] = "unread"
        };

        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("video-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("pdf-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("resource-reading"))
            .Returns(templateMetadata);

        _mockMetadataTemplateManager.Setup(m => m.ResolveTemplateFields(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _mockMarkdownNoteBuilder.Setup(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
            .Returns("---\ntitle: test\ntemplate-type: video-reference\ntype: note/video-note\n---");

        var documentTypes = new List<string> { "videos", "pdf", "html" };

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: true, documentTypes: documentTypes);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(3, result.CreatedPlaceholderFiles);

        // Verify placeholder files were created
        var vaultCoursePath = Path.Combine(_testVaultRoot, "Course1");
        Assert.IsTrue(File.Exists(Path.Combine(vaultCoursePath, "lecture.md")));
        Assert.IsTrue(File.Exists(Path.Combine(vaultCoursePath, "slides.md")));
        Assert.IsTrue(File.Exists(Path.Combine(vaultCoursePath, "reading.md")));

        // Verify template manager was called for each document type
        _mockMarkdownNoteBuilder.Verify(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()), Times.Exactly(3));
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync dry run mode reports placeholder files without creating them.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_DryRun_ReportsPlaceholderFilesWithoutCreating()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath, "Course1");
        Directory.CreateDirectory(oneDriveTestPath);

        // Create test document files
        File.WriteAllText(Path.Combine(oneDriveTestPath, "lecture.mp4"), "test video content");
        File.WriteAllText(Path.Combine(oneDriveTestPath, "slides.pdf"), "test pdf content");

        // Setup template manager mock
        var templateMetadata = new Dictionary<string, object>
        {
            ["template-type"] = "video-reference",
            ["type"] = "note/video-note",
            ["title"] = "test",
            ["status"] = "unread"
        };

        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("video-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("pdf-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.ResolveTemplateFields(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        var documentTypes = new List<string> { "videos", "pdf" };

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: true, bidirectional: false, recursive: true, documentTypes: documentTypes);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.CreatedPlaceholderFiles); // Should report what would be created

        // Verify placeholder files were NOT actually created
        var vaultCoursePath = Path.Combine(_testVaultRoot, "Course1");
        Directory.CreateDirectory(vaultCoursePath); // Create directory to check files
        Assert.IsFalse(File.Exists(Path.Combine(vaultCoursePath, "lecture.md")));
        Assert.IsFalse(File.Exists(Path.Combine(vaultCoursePath, "slides.md")));

        // Verify template manager was not called in dry run
        _mockMarkdownNoteBuilder.Verify(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()), Times.Never);
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync skips existing markdown files instead of creating conflicts.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_SkipsExistingMarkdownFiles()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath, "Course1");
        Directory.CreateDirectory(oneDriveTestPath);

        var vaultCoursePath = Path.Combine(_testVaultRoot, "Course1");
        Directory.CreateDirectory(vaultCoursePath);

        // Create a document file
        File.WriteAllText(Path.Combine(oneDriveTestPath, "lecture.mp4"), "test video content");

        // Create an existing markdown file with the same name
        File.WriteAllText(Path.Combine(vaultCoursePath, "lecture.md"), "existing content");

        // Setup template manager mock
        var templateMetadata = new Dictionary<string, object>
        {
            ["template-type"] = "video-reference",
            ["type"] = "note/video-note",
            ["title"] = "lecture",
            ["status"] = "unread"
        };

        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("video-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.ResolveTemplateFields(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _mockMarkdownNoteBuilder.Setup(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
            .Returns("---\ntitle: lecture\ntemplate-type: video-reference\ntype: note/video-note\n---");

        var documentTypes = new List<string> { "videos" };

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: true, documentTypes: documentTypes);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.CreatedPlaceholderFiles); // Should be 0 because file already exists

        // Verify original file still exists and no new files were created
        Assert.IsTrue(File.Exists(Path.Combine(vaultCoursePath, "lecture.md")));
        Assert.IsFalse(File.Exists(Path.Combine(vaultCoursePath, "lecture-1.md")));

        // Verify original content is preserved
        var originalContent = File.ReadAllText(Path.Combine(vaultCoursePath, "lecture.md"));
        Assert.AreEqual("existing content", originalContent);

        // Verify template manager was not called since file was skipped
        _mockMarkdownNoteBuilder.Verify(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()), Times.Never);
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync skips placeholder creation when no document types specified.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_SkipsPlaceholderCreation_WhenNoDocumentTypesSpecified()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath, "Course1");
        Directory.CreateDirectory(oneDriveTestPath);

        // Create test document files
        File.WriteAllText(Path.Combine(oneDriveTestPath, "lecture.mp4"), "test video content");
        File.WriteAllText(Path.Combine(oneDriveTestPath, "slides.pdf"), "test pdf content");

        // Act - no document types specified
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.CreatedPlaceholderFiles);

        // Verify no placeholder files were created
        var vaultCoursePath = Path.Combine(_testVaultRoot, "Course1");
        if (Directory.Exists(vaultCoursePath))
        {
            Assert.IsFalse(File.Exists(Path.Combine(vaultCoursePath, "lecture.md")));
            Assert.IsFalse(File.Exists(Path.Combine(vaultCoursePath, "slides.md")));
        }

        // Verify template manager was not called
        _mockMarkdownNoteBuilder.Verify(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()), Times.Never);
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync handles unknown document types gracefully.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_HandlesUnknownDocumentTypes_Gracefully()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath, "Course1");
        Directory.CreateDirectory(oneDriveTestPath);

        // Create test document files
        File.WriteAllText(Path.Combine(oneDriveTestPath, "lecture.mp4"), "test video content");
        File.WriteAllText(Path.Combine(oneDriveTestPath, "unknown.xyz"), "unknown file type");

        // Setup template manager mock for known type
        var templateMetadata = new Dictionary<string, object>
        {
            ["template-type"] = "video-reference",
            ["type"] = "note/video-note",
            ["title"] = "lecture",
            ["status"] = "unread"
        };

        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("video-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.ResolveTemplateFields(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _mockMarkdownNoteBuilder.Setup(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
            .Returns("---\ntitle: lecture\ntemplate-type: video-reference\ntype: note/video-note\n---");

        var documentTypes = new List<string> { "videos", "unknown-type" };

        // Act
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: true, documentTypes: documentTypes);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.CreatedPlaceholderFiles); // Only the video file should create a placeholder

        // Verify only the known document type created a placeholder
        var vaultCoursePath = Path.Combine(_testVaultRoot, "Course1");
        Assert.IsTrue(File.Exists(Path.Combine(vaultCoursePath, "lecture.md")));
        Assert.IsFalse(File.Exists(Path.Combine(vaultCoursePath, "unknown.md")));

        // Verify template manager was called only for the known type
        _mockMarkdownNoteBuilder.Verify(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()), Times.Once);
    }


    /// <summary>
    /// Tests that SyncDirectoriesAsync creates placeholders only in recursive mode when files are in subdirectories.
    /// </summary>
    [TestMethod]
    public async Task SyncDirectoriesAsync_NonRecursive_DoesNotCreatePlaceholdersInSubdirectories()
    {
        // Arrange
        var testPath = "MBA/Finance";
        var oneDriveTestPath = Path.Combine(_testOneDriveRoot, _pathsConfig.OnedriveResourcesBasepath, testPath);
        Directory.CreateDirectory(oneDriveTestPath);

        // Create subdirectory with document file
        var courseDir = Path.Combine(oneDriveTestPath, "Course1");
        Directory.CreateDirectory(courseDir);
        File.WriteAllText(Path.Combine(courseDir, "lecture.mp4"), "test video content");

        // Create document file in root level
        File.WriteAllText(Path.Combine(oneDriveTestPath, "overview.pdf"), "test pdf content");

        // Setup template manager mock
        var templateMetadata = new Dictionary<string, object>
        {
            ["template-type"] = "pdf-reference",
            ["type"] = "note/case-study",
            ["title"] = "overview",
            ["status"] = "unread"
        };

        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("pdf-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.GetTemplate("video-reference"))
            .Returns(templateMetadata);
        _mockMetadataTemplateManager.Setup(m => m.ResolveTemplateFields(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _mockMarkdownNoteBuilder.Setup(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()))
            .Returns("---\ntitle: overview\ntemplate-type: pdf-reference\ntype: note/case-study\n---");

        var documentTypes = new List<string> { "videos", "pdf" };

        // Act - non-recursive mode
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: false, recursive: false, documentTypes: documentTypes);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.CreatedPlaceholderFiles); // Only the root-level PDF should create a placeholder

        // Verify only root-level document created a placeholder
        Assert.IsTrue(File.Exists(Path.Combine(_testVaultRoot, "overview.md")));

        // Verify subdirectory document did not create a placeholder
        var vaultCoursePath = Path.Combine(_testVaultRoot, "Course1");
        if (Directory.Exists(vaultCoursePath))
        {
            Assert.IsFalse(File.Exists(Path.Combine(vaultCoursePath, "lecture.md")));
        }

        // Verify template manager was called only for the root-level file
        _mockMarkdownNoteBuilder.Verify(m => m.CreateMarkdownWithFrontmatter(It.IsAny<Dictionary<string, object>>(), It.IsAny<string>()), Times.Once);
    }
}
