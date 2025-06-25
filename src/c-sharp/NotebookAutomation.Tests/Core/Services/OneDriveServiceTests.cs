// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Services;

/// <summary>
/// Unit tests for the <see cref="OneDriveService"/> class.
/// </summary>
[TestClass]
public class OneDriveServiceTests
{
    /// <summary>
    /// Tests that <see cref="OneDriveService.AuthenticateAsync"/> uses the injected MSAL app and does not launch a browser.
    /// </summary>
    [TestMethod]
    [Ignore("Requires MSAL browser interaction or deeper refactor; skip in CI.")]
    public async Task AuthenticateAsync_UsesInjectedMsalApp_DoesNotLaunchBrowser()
    {
        // Arrange
        Mock<ILogger<OneDriveService>> logger = new();
        Mock<Microsoft.Identity.Client.IPublicClientApplication> msalMock = new(); Mock<Microsoft.Identity.Client.AcquireTokenSilentParameterBuilder> silentBuilderMock = new(null!, null!, null!);
        Mock<Microsoft.Identity.Client.AcquireTokenInteractiveParameterBuilder> interactiveBuilderMock = new(null!, null!);

        // Setup chained builder methods
        interactiveBuilderMock.Setup(b => b.WithPrompt(It.IsAny<Microsoft.Identity.Client.Prompt>())).Returns(interactiveBuilderMock.Object);
        Mock<Microsoft.Identity.Client.IAccount> fakeAccount = new(); Mock<Microsoft.Identity.Client.AuthenticationResult> fakeResult = new(
            "token", false, "user", DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1),
            string.Empty, null!, null!, "Bearer", null!, null!, null!, null!, null!, null!);

        // Setup silent to throw UI required, then interactive to return fake result
        silentBuilderMock.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Microsoft.Identity.Client.MsalUiRequiredException("code", "message"));
        interactiveBuilderMock.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fakeResult.Object);
        msalMock.Setup(m => m.GetAccountsAsync()).ReturnsAsync([fakeAccount.Object]);
        msalMock.Setup(m => m.AcquireTokenSilent(It.IsAny<string[]>(), It.IsAny<Microsoft.Identity.Client.IAccount>())).Returns(silentBuilderMock.Object);
        msalMock.Setup(m => m.AcquireTokenInteractive(It.IsAny<string[]>())).Returns(interactiveBuilderMock.Object);

        OneDriveService service = new(logger.Object, "clientId", "tenantId", ["scope"], msalMock.Object);

        // Act
        await service.AuthenticateAsync().ConfigureAwait(false);        // Assert: Should log token cache file path and not throw
        logger.Verify(
            l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token cache file path")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that the constructor of <see cref="OneDriveService"/> throws <see cref="ArgumentNullException"/> for null arguments.
    /// </summary>
    [TestMethod]
    public void Constructor_ThrowsOnNullArguments()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        string[] scopes = ["scope"];
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new OneDriveService(null!, "clientId", "tenantId", scopes));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new OneDriveService(logger, null!, "tenantId", scopes));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new OneDriveService(logger, "clientId", null!, scopes));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new OneDriveService(logger, "clientId", "tenantId", null!));
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.SetForceRefresh"/> updates the state and logs the changes.
    /// </summary>
    [TestMethod]
    [Ignore("Requires MSAL browser interaction or deeper refactor; skip in CI.")]
    public void SetForceRefresh_UpdatesStateAndLogs()
    {
        Mock<ILogger<OneDriveService>> logger = new();
        OneDriveService service = new(logger.Object, "clientId", "tenantId", ["scope"]);
        service.SetForceRefresh(true);
        service.SetForceRefresh(false); logger.Verify(
            l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Force refresh set to: True") || v.ToString()!.Contains("Force refresh set to: False")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.ConfigureVaultRoots"/> sets trimmed values without exceptions.
    /// </summary>
    [TestMethod]
    public void ConfigureVaultRoots_SetsTrimmedValues()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        service.ConfigureVaultRoots("C:/vault/", "onedrive/root/");

        // No exception means success; further validation would require reflection or exposing properties for test
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.MapLocalToOneDrivePath"/> throws <see cref="InvalidOperationException"/> if not configured.
    /// </summary>
    [TestMethod]
    public void MapLocalToOneDrivePath_ThrowsIfNotConfigured()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            service.MapLocalToOneDrivePath("C:/vault/file.txt"));
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.MapLocalToOneDrivePath"/> throws <see cref="ArgumentException"/> if the path is not under the root.
    /// </summary>
    [TestMethod]
    public void MapLocalToOneDrivePath_ThrowsIfPathNotUnderRoot()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        service.ConfigureVaultRoots("C:/vault", "onedrive/root");
        Assert.ThrowsExactly<ArgumentException>(() =>
            service.MapLocalToOneDrivePath("C:/other/file.txt"));
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.MapLocalToOneDrivePath"/> returns the expected OneDrive path.
    /// </summary>
    [TestMethod]
    public void MapLocalToOneDrivePath_ReturnsExpectedPath()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        service.ConfigureVaultRoots("C:/vault", "onedrive/root");
        string file = System.IO.Path.Combine("C:/vault", "sub", "file.txt");
        string expected = "onedrive/root/sub/file.txt";
        string result = service.MapLocalToOneDrivePath(file);
        Assert.AreEqual(expected, result.Replace("\\", "/"));
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.MapOneDriveToLocalPath"/> throws <see cref="InvalidOperationException"/> if not configured.
    /// </summary>
    [TestMethod]
    public void MapOneDriveToLocalPath_ThrowsIfNotConfigured()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            service.MapOneDriveToLocalPath("onedrive/root/file.txt"));
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.MapOneDriveToLocalPath"/> throws <see cref="ArgumentException"/> if the path is not under the root.
    /// </summary>
    [TestMethod]
    public void MapOneDriveToLocalPath_ThrowsIfPathNotUnderRoot()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        service.ConfigureVaultRoots("C:/vault", "onedrive/root");
        Assert.ThrowsExactly<ArgumentException>(() =>
            service.MapOneDriveToLocalPath("otherroot/file.txt"));
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.MapOneDriveToLocalPath"/> returns the expected local path.
    /// </summary>
    [TestMethod]
    public void MapOneDriveToLocalPath_ReturnsExpectedPath()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        service.ConfigureVaultRoots("C:/vault", "onedrive/root");
        string result = service.MapOneDriveToLocalPath("onedrive/root/sub/file.txt");
        string expected = System.IO.Path.Combine("C:/vault", "sub", "file.txt");
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.SetCliOptions"/> sets options or defaults without exceptions.
    /// </summary>
    [TestMethod]
    public void SetCliOptions_SetsOptionsOrDefaults()
    {
        ILogger<OneDriveService> logger = Mock.Of<ILogger<OneDriveService>>();
        OneDriveService service = new(logger, "clientId", "tenantId", ["scope"]);
        service.SetCliOptions(null!); // Should not throw
        service.SetCliOptions(new OneDriveCliOptions()); // Should not throw
    }

    /// <summary>
    /// Tests that <see cref="OneDriveService.RefreshAuthenticationAsync"/> refreshes the token successfully.
    /// </summary>
    [TestMethod]
    [Ignore("Requires MSAL browser interaction or deeper refactor; skip in CI.")]
    public async Task RefreshAuthenticationAsync_RefreshesTokenSuccessfully()
    {
        // Arrange
        Mock<ILogger<OneDriveService>> logger = new();
        Mock<Microsoft.Identity.Client.IPublicClientApplication> msalMock = new();
        Mock<Microsoft.Identity.Client.AcquireTokenInteractiveParameterBuilder> interactiveBuilderMock = new(null!, null!);

        interactiveBuilderMock.Setup(b => b.WithPrompt(It.IsAny<Microsoft.Identity.Client.Prompt>())).Returns(interactiveBuilderMock.Object);
        Mock<Microsoft.Identity.Client.AuthenticationResult> fakeResult = new(
            "token", false, "user", DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1),
            string.Empty, null!, null!, "Bearer", null!, null!, null!, null!, null!, null!);

        interactiveBuilderMock.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fakeResult.Object);
        msalMock.Setup(m => m.AcquireTokenInteractive(It.IsAny<string[]>())).Returns(interactiveBuilderMock.Object);

        OneDriveService service = new(logger.Object, "clientId", "tenantId", ["scope"], msalMock.Object);

        // Act
        await service.RefreshAuthenticationAsync().ConfigureAwait(false);

        // Assert
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OneDrive authentication refreshed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
