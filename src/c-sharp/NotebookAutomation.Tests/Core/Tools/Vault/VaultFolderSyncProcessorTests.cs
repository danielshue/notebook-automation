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
        _processor = new VaultFolderSyncProcessor(_mockLogger.Object, _mockAppConfig.Object);

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
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, bidirectional: false);

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
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: true);

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
        var result = await _processor.SyncDirectoriesAsync(testPath, _testVaultRoot, dryRun: false, bidirectional: true);

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
}
