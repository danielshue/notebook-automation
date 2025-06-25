// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the <c>FailedOperations</c> static class.
/// <para>
/// These tests verify that failed file operations are logged correctly and that null logger arguments are handled as expected.
/// </para>
/// </summary>
[TestClass]
public class FailedOperationsTests
{
    /// <summary>
    /// Verifies that <c>RecordFailedFileOperation</c> logs an error with exception details for a failed file operation.
    /// </summary>
    [TestMethod]
    public void RecordFailedFileOperation_WithException_LogsError()
    {
        Mock<ILogger> mockLogger = new();
        string filePath = "test.txt";
        string operation = "Read";
        InvalidOperationException exception = new("fail!");

        FailedOperations.RecordFailedFileOperation(mockLogger.Object, filePath, operation, exception); mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(filePath) && v.ToString()!.Contains(operation) && v.ToString()!.Contains("fail!")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that <c>RecordFailedFileOperation</c> logs an error with a custom error message for a failed file operation.
    /// </summary>
    [TestMethod]
    public void RecordFailedFileOperation_WithCustomError_LogsError()
    {
        Mock<ILogger> mockLogger = new();
        string filePath = "test.txt";
        string operation = "Write";
        string errorMessage = "custom error";

        FailedOperations.RecordFailedFileOperation(mockLogger.Object, filePath, operation, errorMessage); mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(filePath) && v.ToString()!.Contains(operation) && v.ToString()!.Contains(errorMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that <c>RecordFailedFileOperation</c> throws <see cref="ArgumentNullException"/> when the logger is null.
    /// </summary>
    [TestMethod]
    public void RecordFailedFileOperation_NullLogger_Throws()
    {
        string filePath = "test.txt";
        string operation = "Delete";
        Exception exception = new("fail"); Assert.ThrowsExactly<ArgumentNullException>(() =>
            FailedOperations.RecordFailedFileOperation(null!, filePath, operation, exception));
    }
}
