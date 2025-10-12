// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Services;

/// <summary>
/// Unit tests for OneDriveService path mapping functionality.
/// </summary>
[TestClass]
public class OneDriveServicePathMappingTests
{
    /// <summary>
    /// Verifies that MapLocalToOneDrivePath correctly maps local file paths to OneDrive relative paths.
    /// </summary>
    [TestMethod]
    public void MapLocalToOneDrivePath_MapsCorrectly()
    {
        OneDriveService service = new(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", []);
        string localRoot = Path.Combine("C:", "Users", "Test", "Vault");
        string oneDriveRoot = "Vault";
        service.ConfigureVaultRoots(localRoot, oneDriveRoot);
        string localPath = Path.Combine(localRoot, "folder", "file.txt");
        string expected = "Vault/folder/file.txt";
        string result = service.MapLocalToOneDrivePath(localPath);
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that MapOneDriveToLocalPath correctly maps OneDrive relative paths to local file system paths.
    /// </summary>
    [TestMethod]
    public void MapOneDriveToLocalPath_MapsCorrectly()
    {
        OneDriveService service = new(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", []);
        string localRoot = Path.Combine("C:", "Users", "Test", "Vault");
        string oneDriveRoot = "Vault";
        service.ConfigureVaultRoots(localRoot, oneDriveRoot);
        string oneDrivePath = "Vault/folder/file.txt";
        string expected = Path.Combine(localRoot, "folder", "file.txt");
        string result = service.MapOneDriveToLocalPath(oneDrivePath);
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that MapLocalToOneDrivePath throws ArgumentException when the local path is not under the configured vault root.
    /// </summary>
    [TestMethod]
    public void MapLocalToOneDrivePath_ThrowsIfNotUnderRoot()
    {
        OneDriveService service = new(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", []);
        string localRoot = Path.Combine("C:", "Users", "Test", "Vault");
        string oneDriveRoot = "Vault";
        service.ConfigureVaultRoots(localRoot, oneDriveRoot);
        string localPath = Path.Combine("C:", "Other", "file.txt");
        Assert.Throws<ArgumentException>(() => service.MapLocalToOneDrivePath(localPath));
    }

    /// <summary>
    /// Verifies that MapOneDriveToLocalPath throws ArgumentException when the OneDrive path is not under the configured vault root.
    /// </summary>
    [TestMethod]
    public void MapOneDriveToLocalPath_ThrowsIfNotUnderRoot()
    {
        OneDriveService service = new(Mock.Of<ILogger<OneDriveService>>(), "client", "tenant", []);
        string localRoot = Path.Combine("C:", "Users", "Test", "Vault");
        string oneDriveRoot = "Vault";
        service.ConfigureVaultRoots(localRoot, oneDriveRoot);
        string oneDrivePath = "OtherVault/folder/file.txt";
        Assert.Throws<ArgumentException>(() => service.MapOneDriveToLocalPath(oneDrivePath));
    }
}
