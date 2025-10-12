// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

/// <summary>
/// Unit tests for the <see cref="OneDriveShareLinkResolver"/> class.
/// </summary>
[TestClass]
public class OneDriveShareLinkResolverTests
{
    private Mock<ILogger<OneDriveShareLinkResolver>> _loggerMock = null!;
    private Mock<IOneDriveService> _oneDriveServiceMock = null!;
    private OneDriveShareLinkResolver _resolver = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<OneDriveShareLinkResolver>>();
        _oneDriveServiceMock = new Mock<IOneDriveService>();
        _resolver = new OneDriveShareLinkResolver(_loggerMock.Object, _oneDriveServiceMock.Object);
    }

    [TestMethod]
    public void Resolve_WithNullContext_ReturnsNull()
    {
        // Act
        var result = _resolver.Resolve("share-link", null);

        // Assert
        Assert.IsNull(result);
        _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that Resolve returns null when the context does not contain a filePath.
    /// </summary>
    [TestMethod]
    public void Resolve_WithMissingFilePath_ReturnsNull()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["other_key"] = "value"
        };

        // Act
        var result = _resolver.Resolve("share-link", context);

        // Assert
        Assert.IsNull(result);
        _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that Resolve returns empty string when skip_onedrive_share_link flag is set to true.
    /// </summary>
    [TestMethod]
    public void Resolve_WithSkipFlag_ReturnsEmpty()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["filePath"] = @"C:\test\file.mp4",
            ["skip_onedrive_share_link"] = true
        };

        // Act
        var result = _resolver.Resolve("share-link", context);

        // Assert
        Assert.AreEqual(string.Empty, result);
        _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that Resolve calls OneDrive service when skip_onedrive_share_link flag is explicitly set to false.
    /// </summary>
    [TestMethod]
    public void Resolve_WithSkipFlagFalse_CallsOneDriveService()
    {
        // Arrange
        var filePath = @"C:\test\file.mp4";
        var expectedLink = "https://example.com/share-link";
        var context = new Dictionary<string, object>
        {
            ["filePath"] = filePath,
            ["skip_onedrive_share_link"] = false
        };

        _oneDriveServiceMock.Setup(x => x.CreateShareLinkAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLink);

        // Act
        var result = _resolver.Resolve("share-link", context);

        // Assert
        Assert.AreEqual(expectedLink, result);
        _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(filePath, "view", "anonymous", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that Resolve calls OneDrive service when skip_onedrive_share_link flag is not present in context.
    /// </summary>
    [TestMethod]
    public void Resolve_WithoutSkipFlag_CallsOneDriveService()
    {
        // Arrange
        var filePath = @"C:\test\file.mp4";
        var expectedLink = "https://example.com/share-link";
        var context = new Dictionary<string, object>
        {
            ["filePath"] = filePath
        };

        _oneDriveServiceMock.Setup(x => x.CreateShareLinkAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLink);

        // Act
        var result = _resolver.Resolve("share-link", context);

        // Assert
        Assert.AreEqual(expectedLink, result);
        _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(filePath, "view", "anonymous", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that Resolve returns empty string when OneDrive service throws an exception.
    /// </summary>
    [TestMethod]
    public void Resolve_WithOneDriveServiceFailure_ReturnsEmpty()
    {
        // Arrange
        var filePath = @"C:\test\file.mp4";
        var context = new Dictionary<string, object>
        {
            ["filePath"] = filePath
        };

        _oneDriveServiceMock.Setup(x => x.CreateShareLinkAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("OneDrive service failure"));

        // Act
        var result = _resolver.Resolve("share-link", context);

        // Assert
        Assert.AreEqual(string.Empty, result);
        _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(filePath, "view", "anonymous", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that Resolve returns empty string when OneDrive service returns null.
    /// </summary>
    [TestMethod]
    public void Resolve_WithOneDriveServiceReturningNull_ReturnsEmpty()
    {
        // Arrange
        var filePath = @"C:\test\file.mp4";
        var context = new Dictionary<string, object>
        {
            ["filePath"] = filePath
        };

        _oneDriveServiceMock.Setup(x => x.CreateShareLinkAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = _resolver.Resolve("share-link", context);

        // Assert
        Assert.AreEqual(string.Empty, result);
        _oneDriveServiceMock.Verify(x => x.CreateShareLinkAsync(filePath, "view", "anonymous", It.IsAny<CancellationToken>()), Times.Once);
    }
}
