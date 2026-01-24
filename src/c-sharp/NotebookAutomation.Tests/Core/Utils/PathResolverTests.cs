// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Tools;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Tests.Core.Utils;

[TestClass]
public class PathResolverTests
{
    private AppConfig? _config;

    [TestInitialize]
    public void Setup()
    {
        _config = new AppConfig
        {
            Paths = new PathsConfig
            {
                OnedriveFullpathRoot = "/home/user/OneDrive",
                OnedriveResourcesBasepath = "Education/MBA",
                NotebookVaultFullpathRoot = "/home/user/Vault",
                NotebookVaultResourcesBasepath = "Projects/MBA"
            }
        };
    }

    [TestMethod]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => new PathResolver(null!));
    }

    [TestMethod]
    public void ResolveInputRoot_OneDriveRoot_ReturnsEffectiveOneDriveRoot()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { InputRoot = "onedrive" };

        // Act
        var result = resolver.ResolveInputRoot(pathConfig);

        // Assert
        Assert.AreEqual("/home/user/OneDrive/Education/MBA", result);
    }

    [TestMethod]
    public void ResolveInputRoot_VaultRoot_ReturnsEffectiveVaultRoot()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { InputRoot = "vault" };

        // Act
        var result = resolver.ResolveInputRoot(pathConfig);

        // Assert
        Assert.AreEqual("/home/user/Vault/Projects/MBA", result);
    }

    [TestMethod]
    public void ResolveInputRoot_CwdRoot_ReturnsCurrentDirectory()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { InputRoot = "cwd" };
        var expectedCwd = Directory.GetCurrentDirectory();

        // Act
        var result = resolver.ResolveInputRoot(pathConfig);

        // Assert
        Assert.AreEqual(expectedCwd, result);
    }

    [TestMethod]
    public void ResolveInputRoot_NullPathConfig_ReturnsOneDriveDefault()
    {
        // Arrange
        var resolver = new PathResolver(_config!);

        // Act
        var result = resolver.ResolveInputRoot(null);

        // Assert
        Assert.AreEqual("/home/user/OneDrive/Education/MBA", result);
    }

    [TestMethod]
    public void ResolveInputRoot_UnknownRoot_ReturnsOneDriveDefault()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { InputRoot = "unknown" };

        // Act
        var result = resolver.ResolveInputRoot(pathConfig);

        // Assert
        Assert.AreEqual("/home/user/OneDrive/Education/MBA", result);
    }

    [TestMethod]
    public void ResolveOutputRoot_VaultRoot_ReturnsEffectiveVaultRoot()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { OutputRoot = "vault" };

        // Act
        var result = resolver.ResolveOutputRoot(pathConfig, "/some/input/file.txt");

        // Assert
        Assert.AreEqual("/home/user/Vault/Projects/MBA", result);
    }

    [TestMethod]
    public void ResolveOutputRoot_OneDriveRoot_ReturnsEffectiveOneDriveRoot()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { OutputRoot = "onedrive" };

        // Act
        var result = resolver.ResolveOutputRoot(pathConfig, "/some/input/file.txt");

        // Assert
        Assert.AreEqual("/home/user/OneDrive/Education/MBA", result);
    }

    [TestMethod]
    public void ResolveOutputRoot_CwdRoot_ReturnsCurrentDirectory()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { OutputRoot = "cwd" };
        var expectedCwd = Directory.GetCurrentDirectory();

        // Act
        var result = resolver.ResolveOutputRoot(pathConfig, "/some/input/file.txt");

        // Assert
        Assert.AreEqual(expectedCwd, result);
    }

    [TestMethod]
    public void ResolveOutputRoot_InputRoot_ReturnsInputFileDirectory()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { OutputRoot = "input" };
        var inputPath = "/home/user/Documents/file.txt";

        // Act
        var result = resolver.ResolveOutputRoot(pathConfig, inputPath);

        // Assert
        Assert.AreEqual("/home/user/Documents", result);
    }

    [TestMethod]
    public void ResolveOutputRoot_InputRootWithNoDirectory_ReturnsCurrentDirectory()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { OutputRoot = "input" };
        var inputPath = "file.txt"; // No directory - Path.GetDirectoryName returns empty string
        var expectedCwd = Directory.GetCurrentDirectory();

        // Act
        var result = resolver.ResolveOutputRoot(pathConfig, inputPath);

        // Assert
        // When Path.GetDirectoryName returns empty/null, PathResolver should return current directory
        Assert.AreEqual(expectedCwd, result);
    }

    [TestMethod]
    public void ResolveOutputRoot_NullPathConfig_ReturnsVaultDefault()
    {
        // Arrange
        var resolver = new PathResolver(_config!);

        // Act
        var result = resolver.ResolveOutputRoot(null, "/some/input/file.txt");

        // Assert
        Assert.AreEqual("/home/user/Vault/Projects/MBA", result);
    }

    [TestMethod]
    public void ResolveOutputRoot_UnknownRoot_ReturnsVaultDefault()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { OutputRoot = "unknown" };

        // Act
        var result = resolver.ResolveOutputRoot(pathConfig, "/some/input/file.txt");

        // Assert
        Assert.AreEqual("/home/user/Vault/Projects/MBA", result);
    }

    [TestMethod]
    public void ResolveInputRoot_CaseInsensitive_OneDrive()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { InputRoot = "OneDrive" };

        // Act
        var result = resolver.ResolveInputRoot(pathConfig);

        // Assert
        Assert.AreEqual("/home/user/OneDrive/Education/MBA", result);
    }

    [TestMethod]
    public void ResolveOutputRoot_CaseInsensitive_Input()
    {
        // Arrange
        var resolver = new PathResolver(_config!);
        var pathConfig = new PathResolutionConfig { OutputRoot = "INPUT" };
        var inputPath = "/home/user/Documents/file.txt";

        // Act
        var result = resolver.ResolveOutputRoot(pathConfig, inputPath);

        // Assert
        Assert.AreEqual("/home/user/Documents", result);
    }
}
