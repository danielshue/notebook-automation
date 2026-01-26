// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Tests.Core.Services;

/// <summary>
/// Unit tests for the <see cref="OneDriveToolService"/> class.
/// Tests cover constructor validation, token refresh, and status operations.
/// </summary>
[TestClass]
public class OneDriveToolServiceTests
{
    private Mock<ILogger<OneDriveToolService>> _loggerMock = null!;
    private Mock<IOneDriveService> _oneDriveServiceMock = null!;
    private AppConfig _appConfig = null!;

    /// <summary>
    /// Set up test dependencies before each test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<OneDriveToolService>>();
        _oneDriveServiceMock = new Mock<IOneDriveService>();

        // Create AppConfig with test values
        _appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = "C:/test/vault",
                OnedriveFullpathRoot = "C:/test/onedrive"
            }
        };
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws when logger is null.
    /// </summary>
    [TestMethod]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new OneDriveToolService(
                null!,
                _oneDriveServiceMock.Object,
                _appConfig));
    }

    /// <summary>
    /// Verifies that the constructor throws when app config is null.
    /// </summary>
    [TestMethod]
    public void Constructor_ThrowsOnNullAppConfig()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new OneDriveToolService(
                _loggerMock.Object,
                _oneDriveServiceMock.Object,
                null!));
    }

    /// <summary>
    /// Verifies that the constructor succeeds with null OneDrive service.
    /// </summary>
    [TestMethod]
    public void Constructor_SucceedsWithNullOneDriveService()
    {
        // OneDrive service can be null if not configured
        var service = new OneDriveToolService(
            _loggerMock.Object,
            null,
            _appConfig);

        Assert.IsNotNull(service);
    }

    /// <summary>
    /// Verifies that the constructor succeeds with valid arguments.
    /// </summary>
    [TestMethod]
    public void Constructor_SucceedsWithValidArguments()
    {
        var service = new OneDriveToolService(
            _loggerMock.Object,
            _oneDriveServiceMock.Object,
            _appConfig);

        Assert.IsNotNull(service);
    }

    #endregion

    #region Interface Tests

    /// <summary>
    /// Verifies that IOneDriveToolService interface is properly implemented.
    /// </summary>
    [TestMethod]
    public void OneDriveToolService_ImplementsIOneDriveToolService()
    {
        var service = CreateService();
        Assert.IsInstanceOfType<IOneDriveToolService>(service);
    }

    #endregion

    #region RefreshTokenAsync Tests

    /// <summary>
    /// Verifies that RefreshTokenAsync returns error when OneDrive is not configured.
    /// </summary>
    [TestMethod]
    public async Task RefreshTokenAsync_ReturnsErrorWhenNotConfigured()
    {
        // Arrange
        var service = new OneDriveToolService(
            _loggerMock.Object,
            null,  // OneDrive not configured
            _appConfig);

        // Act
        var result = await service.RefreshTokenAsync();

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.TokenValid);
        Assert.IsTrue(result.Message.Contains("not configured"));
    }

    /// <summary>
    /// Verifies that RefreshTokenAsync returns success on successful refresh.
    /// </summary>
    [TestMethod]
    public async Task RefreshTokenAsync_ReturnsSuccessOnSuccessfulRefresh()
    {
        // Arrange
        _oneDriveServiceMock
            .Setup(s => s.RefreshAuthenticationAsync())
            .Returns(Task.CompletedTask);
        _oneDriveServiceMock
            .Setup(s => s.IsTokenValidAsync())
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.RefreshTokenAsync();

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.TokenValid);
        Assert.IsTrue(result.Message.Contains("successfully"));
    }

    /// <summary>
    /// Verifies that RefreshTokenAsync returns failure when token validation fails.
    /// </summary>
    [TestMethod]
    public async Task RefreshTokenAsync_ReturnsFailureWhenTokenValidationFails()
    {
        // Arrange
        _oneDriveServiceMock
            .Setup(s => s.RefreshAuthenticationAsync())
            .Returns(Task.CompletedTask);
        _oneDriveServiceMock
            .Setup(s => s.IsTokenValidAsync())
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.RefreshTokenAsync();

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.TokenValid);
    }

    /// <summary>
    /// Verifies that RefreshTokenAsync handles exceptions gracefully.
    /// </summary>
    [TestMethod]
    public async Task RefreshTokenAsync_HandlesExceptionGracefully()
    {
        // Arrange
        _oneDriveServiceMock
            .Setup(s => s.RefreshAuthenticationAsync())
            .ThrowsAsync(new InvalidOperationException("Token refresh failed"));

        var service = CreateService();

        // Act
        var result = await service.RefreshTokenAsync();

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.TokenValid);
        Assert.IsTrue(result.Message.Contains("Failed"));
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion

    #region GetStatusAsync Tests

    /// <summary>
    /// Verifies that GetStatusAsync returns not configured when OneDrive root is null.
    /// </summary>
    [TestMethod]
    public async Task GetStatusAsync_ReturnsNotConfiguredWhenRootIsNull()
    {
        // Arrange
        _appConfig.Paths.OnedriveFullpathRoot = null;
        var service = CreateService();

        // Act
        var result = await service.GetStatusAsync();

        // Assert
        Assert.IsFalse(result.IsConfigured);
        Assert.IsFalse(result.TokenValid);
    }

    /// <summary>
    /// Verifies that GetStatusAsync returns not configured when OneDrive service is null.
    /// </summary>
    [TestMethod]
    public async Task GetStatusAsync_ReturnsNotConfiguredWhenServiceIsNull()
    {
        // Arrange
        var service = new OneDriveToolService(
            _loggerMock.Object,
            null,
            _appConfig);

        // Act
        var result = await service.GetStatusAsync();

        // Assert
        Assert.IsFalse(result.IsConfigured);
        Assert.IsFalse(result.TokenValid);
    }

    /// <summary>
    /// Verifies that GetStatusAsync returns configured and authenticated.
    /// </summary>
    [TestMethod]
    public async Task GetStatusAsync_ReturnsConfiguredAndAuthenticated()
    {
        // Arrange
        _oneDriveServiceMock
            .Setup(s => s.IsTokenValidAsync())
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.GetStatusAsync();

        // Assert
        Assert.IsTrue(result.IsConfigured);
        Assert.IsTrue(result.TokenValid);
        Assert.AreEqual("C:/test/onedrive", result.OneDriveRoot);
        Assert.IsTrue(result.Message.Contains("authenticated"));
    }

    /// <summary>
    /// Verifies that GetStatusAsync returns configured but not authenticated.
    /// </summary>
    [TestMethod]
    public async Task GetStatusAsync_ReturnsConfiguredButNotAuthenticated()
    {
        // Arrange
        _oneDriveServiceMock
            .Setup(s => s.IsTokenValidAsync())
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.GetStatusAsync();

        // Assert
        Assert.IsTrue(result.IsConfigured);
        Assert.IsFalse(result.TokenValid);
        Assert.IsTrue(result.Message.Contains("invalid") || result.Message.Contains("expired"));
    }

    /// <summary>
    /// Verifies that GetStatusAsync handles exceptions gracefully.
    /// </summary>
    [TestMethod]
    public async Task GetStatusAsync_HandlesExceptionGracefully()
    {
        // Arrange
        _oneDriveServiceMock
            .Setup(s => s.IsTokenValidAsync())
            .ThrowsAsync(new InvalidOperationException("Status check failed"));

        var service = CreateService();

        // Act
        var result = await service.GetStatusAsync();

        // Assert
        Assert.IsTrue(result.IsConfigured);
        Assert.IsFalse(result.TokenValid);
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion

    #region OneDriveTokenResult Tests

    /// <summary>
    /// Verifies OneDriveTokenResult can represent successful refresh.
    /// </summary>
    [TestMethod]
    public void OneDriveTokenResult_CanRepresentSuccessfulRefresh()
    {
        var result = new OneDriveTokenResult
        {
            Success = true,
            Message = "Token refreshed successfully",
            TokenValid = true
        };

        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.TokenValid);
        Assert.IsNull(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies OneDriveTokenResult can represent failed refresh.
    /// </summary>
    [TestMethod]
    public void OneDriveTokenResult_CanRepresentFailedRefresh()
    {
        var result = new OneDriveTokenResult
        {
            Success = false,
            Message = "Token refresh failed",
            TokenValid = false,
            ErrorMessage = "Authentication error"
        };

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.TokenValid);
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion

    #region OneDriveStatusResult Tests

    /// <summary>
    /// Verifies OneDriveStatusResult can represent fully configured state.
    /// </summary>
    [TestMethod]
    public void OneDriveStatusResult_CanRepresentFullyConfigured()
    {
        var result = new OneDriveStatusResult
        {
            IsConfigured = true,
            TokenValid = true,
            Message = "OneDrive is configured and authenticated",
            OneDriveRoot = "/Users/test/OneDrive"
        };

        Assert.IsTrue(result.IsConfigured);
        Assert.IsTrue(result.TokenValid);
        Assert.IsNotNull(result.OneDriveRoot);
        Assert.IsNull(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies OneDriveStatusResult can represent unconfigured state.
    /// </summary>
    [TestMethod]
    public void OneDriveStatusResult_CanRepresentUnconfigured()
    {
        var result = new OneDriveStatusResult
        {
            IsConfigured = false,
            TokenValid = false,
            Message = "OneDrive is not configured",
            OneDriveRoot = null,
            ErrorMessage = "OneDrive root path not set"
        };

        Assert.IsFalse(result.IsConfigured);
        Assert.IsFalse(result.TokenValid);
        Assert.IsNull(result.OneDriveRoot);
        Assert.IsNotNull(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies OneDriveStatusResult can represent configured but expired state.
    /// </summary>
    [TestMethod]
    public void OneDriveStatusResult_CanRepresentConfiguredButExpired()
    {
        var result = new OneDriveStatusResult
        {
            IsConfigured = true,
            TokenValid = false,
            Message = "OneDrive configured but token expired",
            OneDriveRoot = "/Users/test/OneDrive"
        };

        Assert.IsTrue(result.IsConfigured);
        Assert.IsFalse(result.TokenValid);
        Assert.IsNotNull(result.OneDriveRoot);
    }

    #endregion

    #region Helper Methods

    private OneDriveToolService CreateService()
    {
        return new OneDriveToolService(
            _loggerMock.Object,
            _oneDriveServiceMock.Object,
            _appConfig);
    }

    #endregion
}
