// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Services;

/// <summary>
/// Unit tests for the VaultRootContextService class.
/// </summary>
[TestClass]
public class VaultRootContextServiceTests
{
    private VaultRootContextService _service = null!;

    /// <summary>
    /// Initializes test setup before each test method.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _service = new VaultRootContextService();
    }

    /// <summary>
    /// Tests that VaultRootOverride property can be set and retrieved.
    /// </summary>
    [TestMethod]
    public void VaultRootOverride_SetAndGet_ReturnsCorrectValue()
    {
        // Arrange
        const string testPath = "/test/vault/path";

        // Act
        _service.VaultRootOverride = testPath;

        // Assert
        Assert.AreEqual(testPath, _service.VaultRootOverride);
    }

    /// <summary>
    /// Tests that VaultRootOverride can be set to null.
    /// </summary>
    [TestMethod]
    public void VaultRootOverride_SetToNull_ReturnsNull()
    {
        // Arrange
        _service.VaultRootOverride = "/some/path";

        // Act
        _service.VaultRootOverride = null;

        // Assert
        Assert.IsNull(_service.VaultRootOverride);
    }

    /// <summary>
    /// Tests that HasVaultRootOverride returns false when VaultRootOverride is null.
    /// </summary>
    [TestMethod]
    public void HasVaultRootOverride_WhenNull_ReturnsFalse()
    {
        // Arrange
        _service.VaultRootOverride = null;

        // Act
        bool result = _service.HasVaultRootOverride;

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that HasVaultRootOverride returns false when VaultRootOverride is empty string.
    /// </summary>
    [TestMethod]
    public void HasVaultRootOverride_WhenEmpty_ReturnsFalse()
    {
        // Arrange
        _service.VaultRootOverride = string.Empty;

        // Act
        bool result = _service.HasVaultRootOverride;

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that HasVaultRootOverride returns false when VaultRootOverride is whitespace.
    /// </summary>
    [TestMethod]
    public void HasVaultRootOverride_WhenWhitespace_ReturnsFalse()
    {
        // Arrange
        _service.VaultRootOverride = "   ";

        // Act
        bool result = _service.HasVaultRootOverride;

        // Assert
        Assert.IsFalse(result);
    }

    /// <summary>
    /// Tests that HasVaultRootOverride returns true when VaultRootOverride has a valid path.
    /// </summary>
    [TestMethod]
    public void HasVaultRootOverride_WhenValidPath_ReturnsTrue()
    {
        // Arrange
        _service.VaultRootOverride = "/valid/vault/path";

        // Act
        bool result = _service.HasVaultRootOverride;

        // Assert
        Assert.IsTrue(result);
    }

    /// <summary>
    /// Tests initial state has no vault root override.
    /// </summary>
    [TestMethod]
    public void InitialState_HasNoVaultRootOverride()
    {
        // Act & Assert
        Assert.IsNull(_service.VaultRootOverride);
        Assert.IsFalse(_service.HasVaultRootOverride);
    }

    /// <summary>
    /// Tests that setting VaultRootOverride multiple times works correctly.
    /// </summary>
    [TestMethod]
    public void VaultRootOverride_MultipleSets_WorksCorrectly()
    {
        // Arrange & Act
        _service.VaultRootOverride = "/first/path";
        Assert.AreEqual("/first/path", _service.VaultRootOverride);
        Assert.IsTrue(_service.HasVaultRootOverride);

        _service.VaultRootOverride = "/second/path";
        Assert.AreEqual("/second/path", _service.VaultRootOverride);
        Assert.IsTrue(_service.HasVaultRootOverride);

        _service.VaultRootOverride = null;
        Assert.IsNull(_service.VaultRootOverride);
        Assert.IsFalse(_service.HasVaultRootOverride);
    }
}
