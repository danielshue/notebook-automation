// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Reflection;

using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

namespace NotebookAutomation.Core.Tests.Services;

/// <summary>
/// Comprehensive unit tests for the TokenProvider class.
/// </summary>
[TestClass]
public class TokenProviderTests
{
    private Mock<IPublicClientApplication> _msalAppMock = null!;
    private Mock<ILogger> _loggerMock = null!;
    private string[] _testScopes = null!;

    [TestInitialize]
    public void Setup()
    {
        _msalAppMock = new();
        _loggerMock = new();
        _testScopes = ["https://graph.microsoft.com/.default"];
    }

    /// <summary>
    /// Tests constructor validation for null arguments.
    /// </summary>    [TestMethod]
    public void Constructor_NullMsalApp_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.ThrowsException<TargetInvocationException>(() =>
            CreateTokenProvider(null!, _testScopes, _loggerMock.Object));
        Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentNullException));
    }

    [TestMethod]
    public void Constructor_NullScopes_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.ThrowsException<TargetInvocationException>(() =>
            CreateTokenProvider(_msalAppMock.Object, null!, _loggerMock.Object));
        Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentNullException));
    }

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.ThrowsException<TargetInvocationException>(() =>
            CreateTokenProvider(_msalAppMock.Object, _testScopes, null!));
        Assert.IsInstanceOfType(ex.InnerException, typeof(ArgumentNullException));
    }

    /// <summary>
    /// Tests successful construction with valid parameters.
    /// </summary>
    [TestMethod]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var tokenProvider = CreateTokenProvider(_msalAppMock.Object, _testScopes, _loggerMock.Object);

        // Assert
        Assert.IsNotNull(tokenProvider);
        Assert.IsInstanceOfType<IAccessTokenProvider>(tokenProvider);
    }

    /// <summary>
    /// Tests AllowedHostsValidator contains correct hosts.
    /// </summary>
    [TestMethod]
    public void AllowedHostsValidator_ContainsGraphMicrosoftCom()
    {
        // Arrange
        var tokenProvider = CreateTokenProvider(_msalAppMock.Object, _testScopes, _loggerMock.Object);

        // Act
        var validator = tokenProvider.AllowedHostsValidator;

        // Assert
        Assert.IsNotNull(validator);

        // Test that graph.microsoft.com is allowed by checking the ValidHosts static field
        var tokenProviderType = GetTokenProviderType();
        var validHostsField = tokenProviderType.GetField("ValidHosts", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(validHostsField, "ValidHosts field should exist");

        var validHosts = (string[])validHostsField.GetValue(null)!;
        Assert.IsTrue(validHosts.Contains("graph.microsoft.com"), "ValidHosts should contain graph.microsoft.com");
    }

    /// <summary>
    /// Tests CachedSerializationId property.
    /// </summary>
    [TestMethod]
    public void CachedSerializationId_ReturnsNull()
    {
        // Arrange
        var tokenProviderType = GetTokenProviderType();
        var property = tokenProviderType.GetProperty("CachedSerializationId", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(property, "CachedSerializationId property should exist");

        // Act & Assert
        var value = property.GetValue(null);
        Assert.IsNull(value, "CachedSerializationId should return null");
    }

    /// <summary>
    /// Tests GetAuthorizationTokenAsync with mock setup - basic functionality.
    /// Note: This test verifies the method signature and basic exception handling
    /// without complex MSAL mocking due to the complexity of MSAL types.
    /// </summary>
    [TestMethod]
    public async Task GetAuthorizationTokenAsync_InvalidSetup_HandlesExceptions()
    {
        // Arrange
        var tokenProvider = CreateTokenProvider(_msalAppMock.Object, _testScopes, _loggerMock.Object);
        var testUri = new Uri("https://graph.microsoft.com/v1.0/me");

        // Setup mock to throw an exception (simulating auth failure)
        _msalAppMock.Setup(m => m.GetAccountsAsync())
            .ThrowsAsync(new InvalidOperationException("Mock MSAL exception"));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => tokenProvider.GetAuthorizationTokenAsync(testUri));

        Assert.AreEqual("Mock MSAL exception", exception.Message);
        // Verify error was logged
        _loggerMock.Verify(
            l => l.Log(
                Microsoft.Extensions.Logging.LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error obtaining access token")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that GetAuthorizationTokenAsync method exists with correct signature.
    /// </summary>
    [TestMethod]
    public void GetAuthorizationTokenAsync_MethodSignature_IsCorrect()
    {
        // Arrange
        var tokenProviderType = GetTokenProviderType();

        // Act
        var method = tokenProviderType.GetMethod("GetAuthorizationTokenAsync");

        // Assert
        Assert.IsNotNull(method, "GetAuthorizationTokenAsync method should exist");

        var parameters = method.GetParameters();
        Assert.AreEqual(3, parameters.Length, "Method should have 3 parameters");
        Assert.AreEqual("uri", parameters[0].Name);
        Assert.AreEqual(typeof(Uri), parameters[0].ParameterType);
        Assert.AreEqual("additionalAuthenticationContext", parameters[1].Name);
        Assert.AreEqual("cancellationToken", parameters[2].Name);
        Assert.AreEqual(typeof(CancellationToken), parameters[2].ParameterType);

        Assert.AreEqual(typeof(Task<string>), method.ReturnType, "Method should return Task<string>");
    }

    /// <summary>
    /// Tests that ValidHosts contains expected values.
    /// </summary>
    [TestMethod]
    public void ValidHosts_ContainsExpectedValues()
    {
        // Arrange
        var tokenProviderType = GetTokenProviderType();
        var validHostsField = tokenProviderType.GetField("ValidHosts", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(validHostsField);

        // Act
        var validHosts = (string[])validHostsField.GetValue(null)!;

        // Assert
        Assert.IsNotNull(validHosts);
        Assert.AreEqual(1, validHosts.Length, "Should have exactly one valid host");
        Assert.AreEqual("graph.microsoft.com", validHosts[0]);
    }

    /// <summary>
    /// Tests that TokenProvider implements IAccessTokenProvider interface.
    /// </summary>
    [TestMethod]
    public void TokenProvider_ImplementsIAccessTokenProvider()
    {
        // Arrange
        var tokenProviderType = GetTokenProviderType();

        // Act & Assert
        Assert.IsTrue(typeof(IAccessTokenProvider).IsAssignableFrom(tokenProviderType),
            "TokenProvider should implement IAccessTokenProvider");
    }

    /// <summary>
    /// Tests field initialization and immutability.
    /// </summary>
    [TestMethod]
    public void Constructor_InitializesFieldsCorrectly()
    {
        // Arrange & Act
        var tokenProvider = CreateTokenProvider(_msalAppMock.Object, _testScopes, _loggerMock.Object);

        // Assert - Use reflection to verify private fields are set
        var tokenProviderType = GetTokenProviderType();

        var msalAppField = tokenProviderType.GetField("msalApp", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(msalAppField);
        var msalAppValue = msalAppField.GetValue(tokenProvider);
        Assert.AreEqual(_msalAppMock.Object, msalAppValue);

        var scopesField = tokenProviderType.GetField("scopes", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(scopesField);
        var scopesValue = (string[])scopesField.GetValue(tokenProvider)!;
        Assert.AreEqual(_testScopes.Length, scopesValue.Length);
        Assert.AreEqual(_testScopes[0], scopesValue[0]);

        var loggerField = tokenProviderType.GetField("logger", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(loggerField);
        var loggerValue = loggerField.GetValue(tokenProvider);
        Assert.AreEqual(_loggerMock.Object, loggerValue);
    }

    /// <summary>
    /// Helper method to create TokenProvider instances using reflection since it's internal.
    /// </summary>
    private static IAccessTokenProvider CreateTokenProvider(IPublicClientApplication msalApp, string[] scopes, ILogger logger)
    {
        var tokenProviderType = GetTokenProviderType();
        var constructor = tokenProviderType.GetConstructor([typeof(IPublicClientApplication), typeof(string[]), typeof(ILogger)]);
        Assert.IsNotNull(constructor, "TokenProvider constructor should exist");

        var instance = constructor.Invoke([msalApp, scopes, logger]);
        return (IAccessTokenProvider)instance;
    }

    /// <summary>
    /// Helper method to get the TokenProvider type using reflection.
    /// </summary>
    private static Type GetTokenProviderType()
    {
        // Look for TokenProvider in the OneDriveService assembly
        var assembly = typeof(OneDriveService).Assembly;
        var tokenProviderType = assembly.GetTypes()
            .FirstOrDefault(t => t.Name == "TokenProvider" && !t.IsPublic);

        Assert.IsNotNull(tokenProviderType, "TokenProvider type should be found in the assembly");
        return tokenProviderType;
    }
}
