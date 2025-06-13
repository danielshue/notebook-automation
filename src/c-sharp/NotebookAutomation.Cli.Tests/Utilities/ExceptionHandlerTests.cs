// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Tests.Utilities;

/// <summary>
/// Unit tests for the ExceptionHandler utility class.
/// </summary>
/// <remarks>
/// Tests the centralized exception handling functionality including:
/// - Debug mode vs normal mode behavior
/// - User-friendly message conversion
/// - Logging integration
/// - Exception execution wrappers
/// </remarks>
[TestClass]
public class ExceptionHandlerTests
{
    private Mock<ILogger> mockLogger = null!;
    private StringWriter consoleOutput = null!;
    private StringWriter errorOutput = null!;
    private TextWriter originalOut = null!;
    private TextWriter originalError = null!;

    /// <summary>
    /// Initializes test dependencies before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger>();

        // Capture console output for testing
        originalOut = Console.Out;
        originalError = Console.Error;
        consoleOutput = new StringWriter();
        errorOutput = new StringWriter();
        Console.SetOut(consoleOutput);
        Console.SetError(errorOutput);

        // Reset the static state of ExceptionHandler
        // Note: Since ExceptionHandler uses static fields, we need to initialize it fresh for each test
    }

    /// <summary>
    /// Cleans up test resources after each test.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        Console.SetOut(originalOut);
        Console.SetError(originalError);
        consoleOutput?.Dispose();
        errorOutput?.Dispose();
    }

    /// <summary>
    /// Tests that Initialize sets up the exception handler correctly.
    /// </summary>
    [TestMethod]
    public void Initialize_SetsLoggerAndDebugMode()
    {
        // Arrange & Act
        ExceptionHandler.Initialize(mockLogger.Object, true);

        // Assert
        Assert.IsTrue(ExceptionHandler.IsInitialized, "ExceptionHandler should be initialized");
    }

    /// <summary>
    /// Tests that IsInitialized returns false before initialization.
    /// </summary>
    [TestMethod]
    public void IsInitialized_ReturnsFalseBeforeInitialization()
    {
        // Arrange - Reset static state by initializing with null
        ExceptionHandler.Initialize(null!, false);

        // Assert
        Assert.IsFalse(ExceptionHandler.IsInitialized, "ExceptionHandler should not be initialized with null logger");
    }

    /// <summary>
    /// Tests exception handling in debug mode shows full details.
    /// </summary>
    [TestMethod]
    public void HandleException_DebugMode_ShowsFullDetails()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: true);
        var exception = new InvalidOperationException("Test error message");
        var operation = "test operation";

        // Act
        var exitCode = ExceptionHandler.HandleException(exception, operation);

        // Assert
        Assert.AreEqual(1, exitCode, "Should return default exit code of 1");

        var output = consoleOutput.ToString();
        StringAssert.Contains(output, "Error in test operation:", "Should contain operation context");
        StringAssert.Contains(output, "Message: Test error message", "Should contain exception message");
        StringAssert.Contains(output, "Type: System.InvalidOperationException", "Should contain exception type");
        StringAssert.Contains(output, "Stack Trace:", "Should contain stack trace header");

        // Verify logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to execute test operation")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log the exception");
    }

    /// <summary>
    /// Tests exception handling in normal mode shows user-friendly message.
    /// </summary>
    [TestMethod]
    public void HandleException_NormalMode_ShowsUserFriendlyMessage()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = new FileNotFoundException("Config file not found", "config.json");
        var operation = "loading configuration";

        // Act
        var exitCode = ExceptionHandler.HandleException(exception, operation);

        // Assert
        Assert.AreEqual(1, exitCode, "Should return default exit code of 1");

        var output = consoleOutput.ToString();
        StringAssert.Contains(output, "Error: Required file not found: config.json", "Should contain user-friendly message");
        StringAssert.Contains(output, "Run with --debug flag for detailed error information", "Should suggest debug flag");

        // Should NOT contain debug details
        Assert.IsFalse(output.Contains("Stack Trace:"), "Should not contain stack trace in normal mode");
        Assert.IsFalse(output.Contains("Type: System.IO.FileNotFoundException"), "Should not contain exception type in normal mode");

        // Verify logging
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to execute loading configuration")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Should log the exception");
    }

    /// <summary>
    /// Tests custom exit code parameter.
    /// </summary>
    [TestMethod]
    public void HandleException_CustomExitCode_ReturnsSpecifiedCode()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = new ArgumentException("Invalid argument");
        var operation = "validation";
        const int customExitCode = 42;

        // Act
        var exitCode = ExceptionHandler.HandleException(exception, operation, customExitCode);

        // Assert
        Assert.AreEqual(customExitCode, exitCode, "Should return custom exit code");
    }

    /// <summary>
    /// Tests that HandleException throws ArgumentNullException for null exception.
    /// </summary>
    [TestMethod]
    public void HandleException_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => ExceptionHandler.HandleException(null!, "test operation"),
            "Should throw ArgumentNullException for null exception");
    }

    /// <summary>
    /// Tests that HandleException throws ArgumentException for null operation.
    /// </summary>
    [TestMethod]
    public void HandleException_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(
            () => ExceptionHandler.HandleException(exception, null!),
            "Should throw ArgumentNullException for null operation");
    }

    /// <summary>
    /// Tests that HandleException throws ArgumentException for empty operation.
    /// </summary>
    [TestMethod]
    public void HandleException_EmptyOperation_ThrowsArgumentException()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => ExceptionHandler.HandleException(exception, ""),
            "Should throw ArgumentException for empty operation");
    }

    /// <summary>
    /// Tests HandleException with whitespace-only operation.
    /// </summary>
    [TestMethod]
    public void HandleException_WhitespaceOperation_ThrowsArgumentException()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(
            () => ExceptionHandler.HandleException(exception, "   "),
            "Should throw ArgumentException for whitespace-only operation");
    }

    /// <summary>
    /// Tests ExecuteWithHandling for successful operation.
    /// </summary>
    [TestMethod]
    public async Task ExecuteWithHandling_SuccessfulOperation_ReturnsZero()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var executed = false;

        async Task TestOperation()
        {
            await Task.Delay(1);
            executed = true;
        }

        // Act
        var result = await ExceptionHandler.ExecuteWithHandling(TestOperation, "test operation");

        // Assert
        Assert.AreEqual(0, result, "Should return 0 for successful operation");
        Assert.IsTrue(executed, "Operation should have been executed");
    }

    /// <summary>
    /// Tests ExecuteWithHandling for operation that throws exception.
    /// </summary>
    [TestMethod]
    public async Task ExecuteWithHandling_ExceptionThrown_ReturnsOne()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);

        async Task FailingOperation()
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Test failure");
        }

        // Act
        var result = await ExceptionHandler.ExecuteWithHandling(FailingOperation, "failing operation");

        // Assert
        Assert.AreEqual(1, result, "Should return 1 for failed operation");

        var output = consoleOutput.ToString();
        StringAssert.Contains(output, "Error:", "Should contain error message");
    }

    /// <summary>
    /// Tests ExecuteWithHandling generic version for successful operation.
    /// </summary>
    [TestMethod]
    public async Task ExecuteWithHandling_Generic_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        const string expectedResult = "success";

        async Task<string> TestOperation()
        {
            await Task.Delay(1);
            return expectedResult;
        }

        // Act
        var result = await ExceptionHandler.ExecuteWithHandling(TestOperation, "test operation", "default");

        // Assert
        Assert.AreEqual(expectedResult, result, "Should return operation result");
    }

    /// <summary>
    /// Tests ExecuteWithHandling generic version for operation that throws exception.
    /// </summary>
    [TestMethod]
    public async Task ExecuteWithHandling_Generic_ExceptionThrown_ReturnsDefault()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        const string defaultValue = "default";

        async Task<string> FailingOperation()
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Test failure");
        }

        // Act
        var result = await ExceptionHandler.ExecuteWithHandling(FailingOperation, "failing operation", defaultValue);

        // Assert
        Assert.AreEqual(defaultValue, result, "Should return default value on exception");

        var output = consoleOutput.ToString();
        StringAssert.Contains(output, "Error:", "Should contain error message");
    }

    /// <summary>
    /// Tests user-friendly message conversion for different exception types.
    /// </summary>
    [TestMethod]
    [DataRow(typeof(FileNotFoundException), "config.json", "Required file not found: config.json")]
    [DataRow(typeof(DirectoryNotFoundException), "", "Required directory not found")]
    [DataRow(typeof(UnauthorizedAccessException), "", "Access denied. Check file permissions or run as administrator")]
    [DataRow(typeof(ArgumentException), "Invalid parameter", "Invalid argument: Invalid parameter")]
    [DataRow(typeof(TimeoutException), "", "Operation timed out. Please try again")]
    [DataRow(typeof(TaskCanceledException), "", "Operation was cancelled")]
    [DataRow(typeof(NotImplementedException), "", "This feature is not yet implemented")]
    public void HandleException_DifferentExceptionTypes_ShowsAppropriateMessages(Type exceptionType, string message, string expectedOutput)
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = CreateException(exceptionType, message);
        var operation = "test operation";

        // Act
        ExceptionHandler.HandleException(exception, operation);

        // Assert
        var output = consoleOutput.ToString();
        StringAssert.Contains(output, expectedOutput, $"Should contain appropriate message for {exceptionType.Name}");
    }

    /// <summary>
    /// Tests handling of exceptions with inner exceptions in debug mode.
    /// </summary>
    [TestMethod]
    public void HandleException_ExceptionWithInnerException_ShowsInnerException()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: true);
        var innerException = new ArgumentException("Inner error");
        var outerException = new InvalidOperationException("Outer error", innerException);
        var operation = "test operation";

        // Act
        ExceptionHandler.HandleException(outerException, operation);

        // Assert
        var output = consoleOutput.ToString();
        StringAssert.Contains(output, "Message: Outer error", "Should contain outer exception message");
        StringAssert.Contains(output, "Inner Exception: Inner error", "Should contain inner exception message");
    }

    /// <summary>
    /// Tests that configuration-related InvalidOperationExceptions get special handling.
    /// </summary>
    [TestMethod]
    public void HandleException_ConfigurationError_ShowsConfigurationMessage()
    {
        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = new InvalidOperationException("Configuration validation failed");
        var operation = "loading settings";

        // Act
        ExceptionHandler.HandleException(exception, operation);

        // Assert
        var output = consoleOutput.ToString();
        StringAssert.Contains(output, "Configuration error. Please check your config file", "Should show configuration-specific message");
    }

    /// <summary>
    /// Tests that service-related InvalidOperationExceptions get special handling.
    /// </summary>
    [TestMethod]
    public void HandleException_ServiceError_ShowsServiceMessage()
    {        // Arrange
        ExceptionHandler.Initialize(mockLogger.Object, debugMode: false);
        var exception = new InvalidOperationException("service initialization failed");
        var operation = "starting application";

        // Act
        ExceptionHandler.HandleException(exception, operation);        // Assert
        var output = consoleOutput.ToString();
        StringAssert.Contains(output, "Internal service configuration error", "Should show service-specific message");
    }

    /// <summary>
    /// Helper method to create exceptions of different types for testing.
    /// </summary>

    private static Exception CreateException(Type exceptionType, string message)
    {
        return exceptionType.Name switch
        {
            nameof(FileNotFoundException) => new FileNotFoundException("File not found", message),
            nameof(DirectoryNotFoundException) => new DirectoryNotFoundException(message),
            nameof(UnauthorizedAccessException) => new UnauthorizedAccessException(message),
            nameof(ArgumentException) => new ArgumentException(message),
            nameof(TimeoutException) => new TimeoutException(message),
            nameof(TaskCanceledException) => new TaskCanceledException(message),
            nameof(NotImplementedException) => new NotImplementedException(message),
            _ => new InvalidOperationException(message)
        };
    }
}
