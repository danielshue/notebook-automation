// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Cli.Models.Browse;
using NotebookAutomation.Cli.Services.Browse;
using NotebookAutomation.Core.Tools.Vault;

namespace NotebookAutomation.Tests.Cli.Services.Browse;

/// <summary>
/// Unit tests for <see cref="VaultBrowserSource"/>.
/// </summary>
[TestClass]
public class VaultBrowserSourceTests
{
    private Mock<IVaultBrowserService> _mockVaultService = null!;
    private VaultBrowserSource _source = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mockVaultService = new Mock<IVaultBrowserService>();
        _source = new VaultBrowserSource(_mockVaultService.Object);
    }

    [TestMethod]
    public void Constructor_NullVaultBrowserService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new VaultBrowserSource(null!));
    }

    [TestMethod]
    public void SourceName_ReturnsVault()
    {
        // Act
        var sourceName = _source.SourceName;

        // Assert
        Assert.AreEqual("Vault", sourceName);
    }

    [TestMethod]
    public async Task ListDirectoryAsync_SuccessfulListing_ReturnsDirectoryListing()
    {
        // Arrange
        var vaultListing = new VaultDirectoryListing
        {
            Path = "/test",
            Directories =
            [
                new VaultBrowserDirectoryInfo { Name = "folder1", RelativePath = "/test/folder1", ItemCount = 5 },
                new VaultBrowserDirectoryInfo { Name = "folder2", RelativePath = "/test/folder2", ItemCount = 3 }
            ],
            Files =
            [
                new VaultBrowserFileInfo
                {
                    Name = "note1.md",
                    RelativePath = "/test/note1.md",
                    SizeBytes = 1024,
                    SizeFormatted = "1.0 KB",
                    LastModified = new DateTime(2025, 1, 1)
                }
            ]
        };

        _mockVaultService
            .Setup(v => v.ListDirectory(It.IsAny<string>()))
            .Returns(VaultBrowserResult<VaultDirectoryListing>.Success(vaultListing));

        // Act
        var result = await _source.ListDirectoryAsync("/test");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("/test", result.Data.CurrentPath);
        Assert.AreEqual(3, result.Data.Items.Count);
        Assert.AreEqual(2, result.Data.Directories.Count);
        Assert.AreEqual(1, result.Data.Files.Count);
    }

    [TestMethod]
    public async Task ListDirectoryAsync_FailedListing_ReturnsFailureResult()
    {
        // Arrange
        _mockVaultService
            .Setup(v => v.ListDirectory(It.IsAny<string>()))
            .Returns(VaultBrowserResult<VaultDirectoryListing>.Failure("Directory not found"));

        // Act
        var result = await _source.ListDirectoryAsync("/nonexistent");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("Directory not found", result.ErrorMessage);
    }

    [TestMethod]
    public async Task ReadFileAsync_SuccessfulRead_ReturnsFileContent()
    {
        // Arrange
        var noteContent = new VaultNoteContent
        {
            Info = new VaultNoteInfo
            {
                Name = "test",
                FileName = "test.md",
                RelativePath = "/test.md",
                SizeBytes = 512,
                SizeFormatted = "512 B",
                LastModified = new DateTime(2025, 1, 1)
            },
            Content = "# Test Note\nThis is content",
            Frontmatter = null,
            Body = "# Test Note\nThis is content"
        };

        _mockVaultService
            .Setup(v => v.ReadNote(It.IsAny<string>()))
            .Returns(VaultBrowserResult<VaultNoteContent>.Success(noteContent));

        // Act
        var result = await _source.ReadFileAsync("/test.md");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("test", result.Data.Info.Name);
        Assert.AreEqual("# Test Note\nThis is content", result.Data.Content);
    }

    [TestMethod]
    public async Task ReadFileAsync_FailedRead_ReturnsFailureResult()
    {
        // Arrange
        _mockVaultService
            .Setup(v => v.ReadNote(It.IsAny<string>()))
            .Returns(VaultBrowserResult<VaultNoteContent>.Failure("File not found"));

        // Act
        var result = await _source.ReadFileAsync("/nonexistent.md");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("File not found", result.ErrorMessage);
    }

    [TestMethod]
    public async Task CreateFileAsync_SuccessfulCreate_ReturnsSuccess()
    {
        // Arrange
        var noteInfo = new VaultNoteInfo
        {
            Name = "newfile",
            FileName = "newfile.md",
            RelativePath = "/newfile.md",
            SizeBytes = 100,
            SizeFormatted = "100 B",
            LastModified = DateTime.Now
        };

        _mockVaultService
            .Setup(v => v.CreateNote(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(VaultBrowserResult<VaultNoteInfo>.Success(noteInfo));

        // Act
        var result = await _source.CreateFileAsync("/newfile.md", "content");

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task DeleteFileAsync_SuccessfulDelete_ReturnsSuccess()
    {
        // Arrange
        _mockVaultService
            .Setup(v => v.DeleteNote(It.IsAny<string>()))
            .Returns(VaultBrowserResult<bool>.Success(true));

        // Act
        var result = await _source.DeleteFileAsync("/test.md");

        // Assert
        Assert.IsTrue(result.IsSuccess);
    }

    [TestMethod]
    public async Task GetTagsAsync_SuccessfulGet_ReturnsTags()
    {
        // Arrange
        var metadata = new VaultNoteMetadata
        {
            Info = new VaultNoteInfo
            {
                Name = "test",
                FileName = "test.md",
                RelativePath = "/test.md",
                SizeBytes = 512,
                SizeFormatted = "512 B",
                LastModified = DateTime.Now
            },
            Frontmatter = new Dictionary<string, object>(),
            Tags = new HashSet<string> { "tag1", "tag2", "tag3" },
            Created = DateTime.Now
        };

        _mockVaultService
            .Setup(v => v.GetNoteMetadata(It.IsAny<string>()))
            .Returns(VaultBrowserResult<VaultNoteMetadata>.Success(metadata));

        // Act
        var result = await _source.GetTagsAsync("/test.md");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Contains("tag1"));
        Assert.IsTrue(result.Contains("tag2"));
        Assert.IsTrue(result.Contains("tag3"));
    }

    [TestMethod]
    public async Task GetTagsAsync_FailedGet_ReturnsEmptyList()
    {
        // Arrange
        _mockVaultService
            .Setup(v => v.GetNoteMetadata(It.IsAny<string>()))
            .Returns(VaultBrowserResult<VaultNoteMetadata>.Failure("File not found"));

        // Act
        var result = await _source.GetTagsAsync("/nonexistent.md");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }
}
