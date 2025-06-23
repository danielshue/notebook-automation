// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Cli.Configuration;

/// <summary>
/// Unit tests for the <see cref="ConfigurationDiscoveryService"/> class.
/// </summary>
[TestClass]
public class ConfigurationDiscoveryServiceTests
{
    private ConfigurationDiscoveryService? configDiscoveryService;


    /// <summary>
    /// Initializes test setup before each test method.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        configDiscoveryService = new ConfigurationDiscoveryService();
    }


    /// <summary>
    /// Tests that ParseConfigPathFromArgs returns null when no config argument is present.
    /// </summary>
    [TestMethod]
    public void ParseConfigPathFromArgs_NoConfigArg_ReturnsNull()
    {
        // Arrange
        var args = new[] { "--help", "--verbose" };

        // Act
        var result = configDiscoveryService!.ParseConfigPathFromArgs(args);

        // Assert
        Assert.IsNull(result, "Should return null when no config argument is present");
    }


    /// <summary>
    /// Tests that ParseConfigPathFromArgs returns the correct path when --config is used.
    /// </summary>
    [TestMethod]
    public void ParseConfigPathFromArgs_WithLongConfigArg_ReturnsCorrectPath()
    {
        // Arrange
        var expectedPath = "test-config.json";
        var args = new[] { "--config", expectedPath, "--help" };

        // Act
        var result = configDiscoveryService!.ParseConfigPathFromArgs(args);

        // Assert
        Assert.AreEqual(expectedPath, result, "Should return the correct config path");
    }


    /// <summary>
    /// Tests that ParseConfigPathFromArgs returns the correct path when -c is used.
    /// </summary>
    [TestMethod]
    public void ParseConfigPathFromArgs_WithShortConfigArg_ReturnsCorrectPath()
    {
        // Arrange
        var expectedPath = "my-config.json";
        var args = new[] { "-c", expectedPath, "--verbose" };

        // Act
        var result = configDiscoveryService!.ParseConfigPathFromArgs(args);

        // Assert
        Assert.AreEqual(expectedPath, result, "Should return the correct config path");
    }


    /// <summary>
    /// Tests that ParseConfigPathFromArgs returns null when config argument is last without value.
    /// </summary>
    [TestMethod]
    public void ParseConfigPathFromArgs_ConfigArgWithoutValue_ReturnsNull()
    {
        // Arrange
        var args = new[] { "--help", "--config" };

        // Act
        var result = configDiscoveryService!.ParseConfigPathFromArgs(args);

        // Assert
        Assert.IsNull(result, "Should return null when config argument has no value");
    }


    /// <summary>
    /// Tests that ParseConfigPathFromArgs returns the first config path when multiple are specified.
    /// </summary>
    [TestMethod]
    public void ParseConfigPathFromArgs_MultipleConfigArgs_ReturnsFirst()
    {
        // Arrange
        var firstPath = "first-config.json";
        var secondPath = "second-config.json";
        var args = new[] { "--config", firstPath, "-c", secondPath };

        // Act
        var result = configDiscoveryService!.ParseConfigPathFromArgs(args);

        // Assert
        Assert.AreEqual(firstPath, result, "Should return the first config path when multiple are specified");
    }


    /// <summary>
    /// Tests that ParseConfigPathFromArgs handles empty args array.
    /// </summary>
    [TestMethod]
    public void ParseConfigPathFromArgs_EmptyArgs_ReturnsNull()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = configDiscoveryService!.ParseConfigPathFromArgs(args);

        // Assert
        Assert.IsNull(result, "Should return null for empty args array");
    }


    /// <summary>
    /// Tests that DiscoverConfigurationForDisplayAsync handles null path correctly.
    /// </summary>
    [TestMethod]
    public async Task DiscoverConfigurationForDisplayAsync_NullPath_HandlesGracefully()
    {
        // Act
        var result = await configDiscoveryService!.DiscoverConfigurationForDisplayAsync(null);

        // Assert
        // Should not throw exception - result can be null if no config is found
        // This test mainly ensures the method doesn't crash with null input
        Assert.IsTrue(true, "Method should handle null path without throwing exception");
    }


    /// <summary>
    /// Tests that DiscoverConfigurationForDisplayAsync handles non-existent path correctly.
    /// </summary>
    [TestMethod]
    public async Task DiscoverConfigurationForDisplayAsync_NonExistentPath_ReturnsNull()
    {
        // Arrange
        var nonExistentPath = "completely-non-existent-config-file-12345.json";

        // Act
        var result = await configDiscoveryService!.DiscoverConfigurationForDisplayAsync(nonExistentPath);

        // Assert
        Assert.IsNull(result, "Should return null for non-existent config file");
    }
}
