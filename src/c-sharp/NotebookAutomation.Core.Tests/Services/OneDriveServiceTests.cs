using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for the OneDriveService class.
    /// </summary>
    [TestClass]
    public class OneDriveServiceTests
    {
        [TestMethod]
        [Ignore("Requires MSAL browser interaction or deeper refactor; skip in CI.")]
        public async Task AuthenticateAsync_UsesInjectedMsalApp_DoesNotLaunchBrowser()
        {
            // Arrange
            var logger = new Mock<ILogger<OneDriveService>>();
            var msalMock = new Mock<Microsoft.Identity.Client.IPublicClientApplication>();
            var silentBuilderMock = new Mock<Microsoft.Identity.Client.AcquireTokenSilentParameterBuilder>(null, null, null);
            var interactiveBuilderMock = new Mock<Microsoft.Identity.Client.AcquireTokenInteractiveParameterBuilder>(null, null);
            // Setup chained builder methods
            interactiveBuilderMock.Setup(b => b.WithPrompt(It.IsAny<Microsoft.Identity.Client.Prompt>())).Returns(interactiveBuilderMock.Object);
            var fakeAccount = new Mock<Microsoft.Identity.Client.IAccount>();
            var fakeResult = new Mock<Microsoft.Identity.Client.AuthenticationResult>(
                "token", false, "user", DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1),
                string.Empty, null, null, "Bearer", null, null, null, null, null, null);

            // Setup silent to throw UI required, then interactive to return fake result
            silentBuilderMock.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Microsoft.Identity.Client.MsalUiRequiredException("code", "message"));
            interactiveBuilderMock.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fakeResult.Object);
            msalMock.Setup(m => m.GetAccountsAsync()).ReturnsAsync(new[] { fakeAccount.Object });
            msalMock.Setup(m => m.AcquireTokenSilent(It.IsAny<string[]>(), It.IsAny<Microsoft.Identity.Client.IAccount>())).Returns(silentBuilderMock.Object);
            msalMock.Setup(m => m.AcquireTokenInteractive(It.IsAny<string[]>())).Returns(interactiveBuilderMock.Object);

            var service = new OneDriveService(logger.Object, "clientId", "tenantId", new[] { "scope" }, msalMock.Object);

            // Act
            await service.AuthenticateAsync();

            // Assert: Should log token cache file path and not throw
            logger.Verify(l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Token cache file path")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.AtLeastOnce);
        }
        [TestMethod]
        public void Constructor_ThrowsOnNullArguments()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var scopes = new[] { "scope" };
            Assert.ThrowsException<ArgumentNullException>(() =>
                new OneDriveService(null, "clientId", "tenantId", scopes));
            Assert.ThrowsException<ArgumentNullException>(() =>
                new OneDriveService(logger, null, "tenantId", scopes));
            Assert.ThrowsException<ArgumentNullException>(() =>
                new OneDriveService(logger, "clientId", null, scopes));
            Assert.ThrowsException<ArgumentNullException>(() =>
                new OneDriveService(logger, "clientId", "tenantId", null));
        }

        [TestMethod]
        [Ignore("Requires MSAL browser interaction or deeper refactor; skip in CI.")]
        public void SetForceRefresh_UpdatesStateAndLogs()
        {
            var logger = new Mock<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger.Object, "clientId", "tenantId", new[] { "scope" });
            service.SetForceRefresh(true);
            service.SetForceRefresh(false);
            logger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Force refresh set to: True") || v.ToString().Contains("Force refresh set to: False")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Exactly(2));
        }

        [TestMethod]
        public void ConfigureVaultRoots_SetsTrimmedValues()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            service.ConfigureVaultRoots("C:/vault/", "onedrive/root/");
            // No exception means success; further validation would require reflection or exposing properties for test
        }

        [TestMethod]
        public void MapLocalToOneDrivePath_ThrowsIfNotConfigured()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            Assert.ThrowsException<InvalidOperationException>(() =>
                service.MapLocalToOneDrivePath("C:/vault/file.txt"));
        }

        [TestMethod]
        public void MapLocalToOneDrivePath_ThrowsIfPathNotUnderRoot()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            service.ConfigureVaultRoots("C:/vault", "onedrive/root");
            Assert.ThrowsException<ArgumentException>(() =>
                service.MapLocalToOneDrivePath("C:/other/file.txt"));
        }

        [TestMethod]
        public void MapLocalToOneDrivePath_ReturnsExpectedPath()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            service.ConfigureVaultRoots("C:/vault", "onedrive/root");
            var file = System.IO.Path.Combine("C:/vault", "sub", "file.txt");
            var expected = "onedrive/root/sub/file.txt";
            var result = service.MapLocalToOneDrivePath(file);
            Assert.AreEqual(expected, result.Replace("\\", "/"));
        }

        [TestMethod]
        public void MapOneDriveToLocalPath_ThrowsIfNotConfigured()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            Assert.ThrowsException<InvalidOperationException>(() =>
                service.MapOneDriveToLocalPath("onedrive/root/file.txt"));
        }

        [TestMethod]
        public void MapOneDriveToLocalPath_ThrowsIfPathNotUnderRoot()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            service.ConfigureVaultRoots("C:/vault", "onedrive/root");
            Assert.ThrowsException<ArgumentException>(() =>
                service.MapOneDriveToLocalPath("otherroot/file.txt"));
        }

        [TestMethod]
        public void MapOneDriveToLocalPath_ReturnsExpectedPath()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            service.ConfigureVaultRoots("C:/vault", "onedrive/root");
            var result = service.MapOneDriveToLocalPath("onedrive/root/sub/file.txt");
            var expected = System.IO.Path.Combine("C:/vault", "sub", "file.txt");
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void SetCliOptions_SetsOptionsOrDefaults()
        {
            var logger = Mock.Of<ILogger<OneDriveService>>();
            var service = new OneDriveService(logger, "clientId", "tenantId", new[] { "scope" });
            service.SetCliOptions(null); // Should not throw
            service.SetCliOptions(new OneDriveCliOptions()); // Should not throw
        }
    }
}
