// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Models;

/// <summary>
/// Unit tests for the VaultIndexOptions class.
/// </summary>
[TestClass]
public class VaultIndexOptionsTests
{
    /// <summary>
    /// Tests that default constructor initializes all properties correctly.
    /// </summary>
    [TestMethod]
    public void Constructor_Default_InitializesPropertiesCorrectly()
    {
        // Act
        var options = new VaultIndexOptions();

        // Assert
        Assert.IsFalse(options.DryRun);
        Assert.IsFalse(options.ForceOverwrite);
        Assert.IsNull(options.Depth);
    }

    /// <summary>
    /// Tests that DryRun property can be set and retrieved correctly.
    /// </summary>
    [TestMethod]
    public void DryRun_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var options = new VaultIndexOptions();

        // Act
        options.DryRun = true;

        // Assert
        Assert.IsTrue(options.DryRun);

        // Act
        options.DryRun = false;

        // Assert
        Assert.IsFalse(options.DryRun);
    }

    /// <summary>
    /// Tests that ForceOverwrite property can be set and retrieved correctly.
    /// </summary>
    [TestMethod]
    public void ForceOverwrite_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var options = new VaultIndexOptions();

        // Act
        options.ForceOverwrite = true;

        // Assert
        Assert.IsTrue(options.ForceOverwrite);

        // Act
        options.ForceOverwrite = false;

        // Assert
        Assert.IsFalse(options.ForceOverwrite);
    }

    /// <summary>
    /// Tests that Depth property can be set and retrieved correctly.
    /// </summary>
    [TestMethod]
    public void Depth_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var options = new VaultIndexOptions();

        // Act
        options.Depth = 5;

        // Assert
        Assert.AreEqual(5, options.Depth);

        // Act
        options.Depth = 0;

        // Assert
        Assert.AreEqual(0, options.Depth);

        // Act
        options.Depth = null;

        // Assert
        Assert.IsNull(options.Depth);
    }

    /// <summary>
    /// Tests that Depth property can handle negative values.
    /// </summary>
    [TestMethod]
    public void Depth_CanHandleNegativeValues()
    {
        // Arrange
        var options = new VaultIndexOptions();

        // Act
        options.Depth = -1;

        // Assert
        Assert.AreEqual(-1, options.Depth);
    }

    /// <summary>
    /// Tests that all properties can be set during object initialization.
    /// </summary>
    [TestMethod]
    public void ObjectInitializer_SetsPropertiesCorrectly()
    {
        // Act
        var options = new VaultIndexOptions
        {
            DryRun = true,
            ForceOverwrite = true,
            Depth = 3
        };

        // Assert
        Assert.IsTrue(options.DryRun);
        Assert.IsTrue(options.ForceOverwrite);
        Assert.AreEqual(3, options.Depth);
    }

    /// <summary>
    /// Tests that default values are correct for boolean properties.
    /// </summary>
    [TestMethod]
    public void BooleanProperties_DefaultToFalse()
    {
        // Act
        var options = new VaultIndexOptions();

        // Assert
        Assert.IsFalse(options.DryRun, "DryRun should default to false");
        Assert.IsFalse(options.ForceOverwrite, "ForceOverwrite should default to false");
    }
}
