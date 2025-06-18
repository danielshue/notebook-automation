// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Cli.Startup;

/// <summary>
/// Unit tests for the <see cref="ApplicationBootstrapper"/> class.
/// </summary>
[TestClass]
public class ApplicationBootstrapperTests
{
    private ApplicationBootstrapper? bootstrapper;


    /// <summary>
    /// Initializes test setup before each test method.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        bootstrapper = new ApplicationBootstrapper();
    }


    /// <summary>
    /// Tests that SetupDependencyInjection returns a valid service provider.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_ReturnsValidServiceProvider()
    {
        // Act
        var serviceProvider = bootstrapper!.SetupDependencyInjection(null, false);

        // Assert
        Assert.IsNotNull(serviceProvider, "Should return a valid service provider");
    }


    /// <summary>
    /// Tests that SetupDependencyInjection registers ILoggerFactory.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_RegistersLoggerFactory()
    {
        // Act
        var serviceProvider = bootstrapper!.SetupDependencyInjection(null, false);

        // Assert
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        Assert.IsNotNull(loggerFactory, "Should register ILoggerFactory");
    }


    /// <summary>
    /// Tests that SetupDependencyInjection registers ILogger{T}.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_RegistersLogger()
    {
        // Act
        var serviceProvider = bootstrapper!.SetupDependencyInjection(null, false);

        // Assert
        var logger = serviceProvider.GetService<ILogger<ApplicationBootstrapperTests>>();
        Assert.IsNotNull(logger, "Should register ILogger<T>");
    }


    /// <summary>
    /// Tests that SetupDependencyInjection handles null config path gracefully.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_NullConfigPath_HandlesGracefully()
    {
        // Act & Assert - Should not throw exception
        var serviceProvider = bootstrapper!.SetupDependencyInjection(null, false);
        Assert.IsNotNull(serviceProvider, "Should handle null config path gracefully");
    }


    /// <summary>
    /// Tests that SetupDependencyInjection handles non-existent config path gracefully.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_NonExistentConfigPath_HandlesGracefully()
    {
        // Arrange
        var nonExistentPath = "completely-non-existent-config-file-12345.json";

        // Act & Assert - Should not throw exception
        var serviceProvider = bootstrapper!.SetupDependencyInjection(nonExistentPath, false);
        Assert.IsNotNull(serviceProvider, "Should handle non-existent config path gracefully");
    }


    /// <summary>
    /// Tests that SetupDependencyInjection works with debug mode enabled.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_DebugModeEnabled_WorksCorrectly()
    {
        // Act
        var serviceProvider = bootstrapper!.SetupDependencyInjection(null, true);

        // Assert
        Assert.IsNotNull(serviceProvider, "Should work correctly with debug mode enabled");

        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        Assert.IsNotNull(loggerFactory, "Should register ILoggerFactory in debug mode");
    }


    /// <summary>
    /// Tests that GetServiceSetupFriendlyMessage returns a user-friendly message for common exceptions.
    /// </summary>
    [TestMethod]
    public void GetServiceSetupFriendlyMessage_CommonException_ReturnsUserFriendlyMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception message");

        // Act
        var friendlyMessage = bootstrapper!.GetServiceSetupFriendlyMessage(exception);

        // Assert
        Assert.IsNotNull(friendlyMessage, "Should return a friendly message");
        Assert.IsTrue(friendlyMessage.Length > 0, "Friendly message should not be empty");
    }


    /// <summary>
    /// Tests that GetServiceSetupFriendlyMessage handles null exception gracefully.
    /// </summary>
    [TestMethod]
    public void GetServiceSetupFriendlyMessage_NullException_HandlesGracefully()
    {
        // Act
        var friendlyMessage = bootstrapper!.GetServiceSetupFriendlyMessage(null!);

        // Assert
        Assert.IsNotNull(friendlyMessage, "Should handle null exception gracefully");
        Assert.IsTrue(friendlyMessage.Length > 0, "Should return a meaningful message for null exception");
    }


    /// <summary>
    /// Tests that multiple calls to SetupDependencyInjection return independent service providers.
    /// </summary>
    [TestMethod]
    public void SetupDependencyInjection_MultipleCalls_ReturnsIndependentProviders()
    {
        // Act
        var serviceProvider1 = bootstrapper!.SetupDependencyInjection(null, false);
        var serviceProvider2 = bootstrapper!.SetupDependencyInjection(null, false);

        // Assert
        Assert.IsNotNull(serviceProvider1, "First service provider should be valid");
        Assert.IsNotNull(serviceProvider2, "Second service provider should be valid");
        Assert.AreNotSame(serviceProvider1, serviceProvider2, "Service providers should be independent instances");
    }
}
