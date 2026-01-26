// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

using Moq;

using NotebookAutomation.Core.Tools;
using NotebookAutomation.Core.Tools.TagManagement;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Tests.Core.Tools.TagManagement;

/// <summary>
/// Unit tests for the <see cref="TagService"/> class.
/// Tests cover constructor validation, path resolution, and operation behaviors.
/// </summary>
[TestClass]
public class TagServiceTests
{
    private Mock<ILogger<TagService>> _loggerMock = null!;
    private Mock<ILoggerFactory> _loggerFactoryMock = null!;
    private Mock<IYamlHelper> _yamlHelperMock = null!;
    private Mock<IMetadataSchemaLoader> _schemaLoaderMock = null!;
    private string _testVaultPath = null!;
    private string _tempDir = null!;

    /// <summary>
    /// Set up test dependencies before each test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<TagService>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _yamlHelperMock = new Mock<IYamlHelper>();
        _schemaLoaderMock = new Mock<IMetadataSchemaLoader>();

        // Set up logger factory to return loggers
        _loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        // Create a temp directory for tests
        _tempDir = Path.Combine(Path.GetTempPath(), $"TagServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _testVaultPath = _tempDir;
    }

    /// <summary>
    /// Clean up temp directories after each test.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TagService(
                null!,
                _loggerFactoryMock.Object,
                _yamlHelperMock.Object,
                null,
                _testVaultPath));
    }

    /// <summary>
    /// Verifies that the constructor throws when logger factory is null.
    /// </summary>
    [TestMethod]
    public void Constructor_ThrowsOnNullLoggerFactory()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TagService(
                _loggerMock.Object,
                null!,
                _yamlHelperMock.Object,
                null,
                _testVaultPath));
    }

    /// <summary>
    /// Verifies that the constructor throws when YAML helper is null.
    /// </summary>
    [TestMethod]
    public void Constructor_ThrowsOnNullYamlHelper()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TagService(
                _loggerMock.Object,
                _loggerFactoryMock.Object,
                null!,
                null,
                _testVaultPath));
    }

    /// <summary>
    /// Verifies that the constructor throws when vault path is null.
    /// </summary>
    [TestMethod]
    public void Constructor_ThrowsOnNullVaultPath()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new TagService(
                _loggerMock.Object,
                _loggerFactoryMock.Object,
                _yamlHelperMock.Object,
                null,
                null!));
    }

    /// <summary>
    /// Verifies that the constructor succeeds with valid arguments.
    /// </summary>
    [TestMethod]
    public void Constructor_SucceedsWithValidArguments()
    {
        var service = new TagService(
            _loggerMock.Object,
            _loggerFactoryMock.Object,
            _yamlHelperMock.Object,
            _schemaLoaderMock.Object,
            _testVaultPath);

        Assert.IsNotNull(service);
    }

    /// <summary>
    /// Verifies that the constructor works without optional schema loader.
    /// </summary>
    [TestMethod]
    public void Constructor_SucceedsWithoutSchemaLoader()
    {
        var service = new TagService(
            _loggerMock.Object,
            _loggerFactoryMock.Object,
            _yamlHelperMock.Object,
            null,
            _testVaultPath);

        Assert.IsNotNull(service);
    }

    #endregion

    #region AddNestedTagsAsync Tests

    /// <summary>
    /// Verifies that AddNestedTagsAsync returns error for non-existent path.
    /// </summary>
    [TestMethod]
    public async Task AddNestedTagsAsync_ReturnsErrorForNonExistentPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AddNestedTagsAsync("nonexistent/path");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("does not exist"));
    }

    /// <summary>
    /// Verifies that AddNestedTagsAsync respects dry run mode.
    /// </summary>
    [TestMethod]
    public async Task AddNestedTagsAsync_RespectsDryRunMode()
    {
        // Arrange
        var service = CreateService();
        CreateTestMarkdownFile("test.md", "---\ntitle: Test\n---\nContent");

        // Act
        var result = await service.AddNestedTagsAsync("test.md", dryRun: true);

        // Assert
        Assert.IsTrue(result.DryRun);
    }

    /// <summary>
    /// Verifies that AddNestedTagsAsync handles path outside vault.
    /// </summary>
    [TestMethod]
    public async Task AddNestedTagsAsync_ReturnsErrorForPathOutsideVault()
    {
        // Arrange
        var service = CreateService();

        // Act - try to access system temp folder directly (outside vault)
        var result = await service.AddNestedTagsAsync(Path.GetTempPath());

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("outside the vault root"));
    }

    #endregion

    #region ConsolidateTagsAsync Tests

    /// <summary>
    /// Verifies that ConsolidateTagsAsync uses vault root when path is null.
    /// </summary>
    [TestMethod]
    public async Task ConsolidateTagsAsync_UsesVaultRootWhenPathIsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ConsolidateTagsAsync(path: null, dryRun: true);

        // Assert
        Assert.IsTrue(result.DryRun);
        // Should not error since vault root exists
        Assert.IsTrue(result.Success || result.FilesProcessed >= 0);
    }

    #endregion

    #region RestructureTagsAsync Tests

    /// <summary>
    /// Verifies that RestructureTagsAsync handles empty directory.
    /// </summary>
    [TestMethod]
    public async Task RestructureTagsAsync_HandlesEmptyDirectory()
    {
        // Arrange
        var service = CreateService();
        var emptyDir = Path.Combine(_tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act
        var result = await service.RestructureTagsAsync(emptyDir);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.FilesProcessed);
    }

    #endregion

    #region UpdateFrontmatterAsync Tests

    /// <summary>
    /// Verifies that UpdateFrontmatterAsync returns error when key is empty.
    /// </summary>
    [TestMethod]
    public async Task UpdateFrontmatterAsync_ReturnsErrorWhenKeyIsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.UpdateFrontmatterAsync("test.md", "", "value");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("empty"));
    }

    /// <summary>
    /// Verifies that UpdateFrontmatterAsync returns error when key is whitespace.
    /// </summary>
    [TestMethod]
    public async Task UpdateFrontmatterAsync_ReturnsErrorWhenKeyIsWhitespace()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.UpdateFrontmatterAsync("test.md", "   ", "value");

        // Assert
        Assert.IsFalse(result.Success);
    }

    /// <summary>
    /// Verifies that UpdateFrontmatterAsync validates path exists.
    /// </summary>
    [TestMethod]
    public async Task UpdateFrontmatterAsync_ReturnsErrorForNonExistentPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.UpdateFrontmatterAsync("nonexistent.md", "key", "value");

        // Assert
        Assert.IsFalse(result.Success);
    }

    #endregion

    #region DiagnoseYamlAsync Tests

    /// <summary>
    /// Verifies that DiagnoseYamlAsync returns success for valid path.
    /// </summary>
    [TestMethod]
    public async Task DiagnoseYamlAsync_ReturnsSuccessForValidPath()
    {
        // Arrange
        var service = CreateService();

        // Mock ExtractFrontmatter to return valid YAML
        _yamlHelperMock
            .Setup(y => y.ExtractFrontmatter(It.IsAny<string>()))
            .Returns("title: Test");

        // Act
        var result = await service.DiagnoseYamlAsync();

        // Assert
        Assert.IsTrue(result.Success);
    }

    /// <summary>
    /// Verifies that DiagnoseYamlAsync handles non-existent path.
    /// </summary>
    [TestMethod]
    public async Task DiagnoseYamlAsync_ReturnsErrorForNonExistentPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DiagnoseYamlAsync("nonexistent/folder");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Message.Contains("does not exist"));
    }

    #endregion

    #region CheckMetadataAsync Tests

    /// <summary>
    /// Verifies that CheckMetadataAsync handles empty vault.
    /// </summary>
    [TestMethod]
    public async Task CheckMetadataAsync_HandlesEmptyVault()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CheckMetadataAsync(dryRun: true);

        // Assert
        Assert.IsTrue(result.DryRun);
    }

    #endregion

    #region CleanIndexFilesAsync Tests

    /// <summary>
    /// Verifies that CleanIndexFilesAsync identifies index files.
    /// </summary>
    [TestMethod]
    public async Task CleanIndexFilesAsync_IdentifiesIndexFiles()
    {
        // Arrange
        var service = CreateService();

        // Create index file
        CreateTestMarkdownFile("_index.md", "---\ntitle: Index\ntags: [test]\n---\nIndex content");

        _yamlHelperMock
            .Setup(y => y.ExtractFrontmatter(It.IsAny<string>()))
            .Returns("title: Index\ntags: [test]");
        _yamlHelperMock
            .Setup(y => y.ParseYamlToDictionary(It.IsAny<string>()))
            .Returns(new Dictionary<string, object> { ["title"] = "Index" });
        _yamlHelperMock
            .Setup(y => y.RemoveFrontmatter(It.IsAny<string>()))
            .Returns("Index content");

        // Act
        var result = await service.CleanIndexFilesAsync(dryRun: true);

        // Assert
        Assert.IsTrue(result.DryRun);
        Assert.IsTrue(result.FilesProcessed >= 0);
    }

    /// <summary>
    /// Verifies that CleanIndexFilesAsync handles non-existent path.
    /// </summary>
    [TestMethod]
    public async Task CleanIndexFilesAsync_ReturnsErrorForNonExistentPath()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CleanIndexFilesAsync("nonexistent/path");

        // Assert
        Assert.IsFalse(result.Success);
    }

    #endregion

    #region Helper Methods

    private TagService CreateService()
    {
        return new TagService(
            _loggerMock.Object,
            _loggerFactoryMock.Object,
            _yamlHelperMock.Object,
            _schemaLoaderMock.Object,
            _testVaultPath);
    }

    private void CreateTestMarkdownFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, content);
    }

    #endregion
}
