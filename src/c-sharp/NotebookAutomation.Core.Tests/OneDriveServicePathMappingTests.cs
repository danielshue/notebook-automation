// <copyright file="OneDriveServicePathMappingTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/OneDriveServicePathMappingTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests;

[TestClass]
public class OneDriveServicePathMappingTests
{
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
