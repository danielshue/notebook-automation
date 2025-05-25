using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Core.Services;
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;

namespace NotebookAutomation.Core.Tests
{

    [TestClass]
    public class OneDriveServicePathMappingTests
    {
        [TestMethod]
        public void MapLocalToOneDrivePath_MapsCorrectly()
        {
            var service = new OneDriveService(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", new string[] { });
            var localRoot = Path.Combine("C:", "Users", "Test", "Vault");
            var oneDriveRoot = "Vault";
            service.ConfigureVaultRoots(localRoot, oneDriveRoot);
            var localPath = Path.Combine(localRoot, "folder", "file.txt");
            var expected = "Vault/folder/file.txt";
            var result = service.MapLocalToOneDrivePath(localPath);
            Assert.AreEqual(expected, result);

        }

        [TestMethod]
        public void MapOneDriveToLocalPath_MapsCorrectly()
        {
            var service = new OneDriveService(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", new string[] { });
            var localRoot = Path.Combine("C:", "Users", "Test", "Vault");
            var oneDriveRoot = "Vault";
            service.ConfigureVaultRoots(localRoot, oneDriveRoot);
            var oneDrivePath = "Vault/folder/file.txt";
            var expected = Path.Combine(localRoot, "folder", "file.txt");
            var result = service.MapOneDriveToLocalPath(oneDrivePath);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MapLocalToOneDrivePath_ThrowsIfNotUnderRoot()
        {
            var service = new OneDriveService(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", new string[] { });
            var localRoot = Path.Combine("C:", "Users", "Test", "Vault");
            var oneDriveRoot = "Vault";
            service.ConfigureVaultRoots(localRoot, oneDriveRoot);
            var localPath = Path.Combine("C:", "Other", "file.txt");
            Assert.Throws<ArgumentException>(() => service.MapLocalToOneDrivePath(localPath));
        }

        [TestMethod]
        public void MapOneDriveToLocalPath_ThrowsIfNotUnderRoot()
        {
            var service = new OneDriveService(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", new string[] { });
            var localRoot = Path.Combine("C:", "Users", "Test", "Vault");
            var oneDriveRoot = "Vault";
            service.ConfigureVaultRoots(localRoot, oneDriveRoot);
            var oneDrivePath = "OtherVault/folder/file.txt";
            Assert.Throws<ArgumentException>(() => service.MapOneDriveToLocalPath(oneDrivePath));
        }
    }
}
