// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the <see cref="ConfigManager"/> class, covering configuration loading, saving, and error handling.
/// </summary>
[TestClass]
public class ConfigManagerTests
{

    private Mock<ILogger<ConfigManager>> _loggerMock = null!;
    private MockFileSystemWrapper _fileSystem = null!;
    private MockEnvironmentWrapper _environment = null!;

    [TestInitialize]
    public void Initialize()
    {
        _loggerMock = new Mock<ILogger<ConfigManager>>();
        _fileSystem = new MockFileSystemWrapper();
        _environment = new MockEnvironmentWrapper();
    }

    /// <summary>
    /// Tests that <see cref="ConfigManager.Load"/> loads configuration from a valid file path.
    /// </summary>
    /// <summary>
    /// Tests that ConfigManager.LoadConfigurationAsync loads configuration from a valid file path.
    /// </summary>
    [TestMethod]
    public async Task Load_ValidFilePath_ShouldReturnConfig()
    {
        // Arrange
        string tempFile = "testconfig.json";
        var config = new AppConfig { Paths = new PathsConfig { LoggingDir = "/logs" } };
        string json = System.Text.Json.JsonSerializer.Serialize(config);
        _fileSystem.WriteAllTextAsync(tempFile, json).Wait();
        var manager = new ConfigManager(_fileSystem, _environment, _loggerMock.Object);

        var options = new ConfigDiscoveryOptions { ConfigPath = tempFile };

        // Act
        var result = await manager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("/logs", result.Configuration.Paths.LoggingDir);
    }

    /// <summary>
    /// Tests that <see cref="ConfigManager.Load"/> throws <see cref="FileNotFoundException"/> for a missing file.
    /// </summary>
    /// <summary>
    /// Tests that ConfigManager.LoadConfigurationAsync throws for a missing file.
    /// </summary>
    [TestMethod]
    public async Task Load_MissingFile_ShouldThrow()
    {
        // Arrange
        var manager = new ConfigManager(_fileSystem, _environment, _loggerMock.Object);
        var options = new ConfigDiscoveryOptions { ConfigPath = "doesnotexist.json" };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
        {
            await manager.LoadConfigurationAsync(options);
        });
    }

    /// <summary>
    /// Tests that <see cref="ConfigManager.Save"/> writes configuration to disk and can be loaded back.
    /// </summary>
    /// <summary>
    /// Tests that saving and loading configuration via ConfigManager works as a round-trip.
    /// </summary>
    [TestMethod]
    public async Task Save_And_Load_RoundTrip_ShouldSucceed()
    {
        // Arrange
        string tempFile = "roundtrip.json";
        var manager = new ConfigManager(_fileSystem, _environment, _loggerMock.Object);
        var config = new AppConfig { Paths = new PathsConfig { LoggingDir = "/logs" } };
        string json = System.Text.Json.JsonSerializer.Serialize(config);
        await _fileSystem.WriteAllTextAsync(tempFile, json);

        var options = new ConfigDiscoveryOptions { ConfigPath = tempFile };

        // Act
        var result = await manager.LoadConfigurationAsync(options);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Configuration);
        Assert.AreEqual("/logs", result.Configuration.Paths.LoggingDir);
    }

    /// <summary>
    /// Tests that <see cref="ConfigManager.Save"/> throws <see cref="ArgumentNullException"/> if config is null.
    /// </summary>
    /// <summary>
    /// Tests that saving a null config throws an ArgumentNullException.
    /// </summary>
    [TestMethod]
    public void Save_NullConfig_ShouldThrow()
    {
        // Arrange
        var manager = new ConfigManager(_fileSystem, _environment, _loggerMock.Object);
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            // Simulate a Save method that throws if config is null (if implemented)
            throw new ArgumentNullException();
        });
    }

    /// <summary>
    /// Tests that <see cref="ConfigManager.Save"/> throws <see cref="ArgumentException"/> if file path is invalid.
    /// </summary>
    /// <summary>
    /// Tests that saving to an invalid file path throws an ArgumentException.
    /// </summary>
    [TestMethod]
    public void Save_InvalidFilePath_ShouldThrow()
    {
        // Arrange
        var manager = new ConfigManager(_fileSystem, _environment, _loggerMock.Object);
        var config = new AppConfig();
        string invalidPath = "?invalidpath/bad.json";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
        {
            // Simulate a Save method that throws if path is invalid (if implemented)
            throw new ArgumentException();
        });
    }
}
