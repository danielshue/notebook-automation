// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Tests.Configuration;

/// <summary>
/// Unit tests for the FailedOperations static class.
/// </summary>
[TestClass]
public class FailedOperationsTests
{
    [TestMethod]
    public void RecordFailedFileOperation_WithException_LogsError()
    {
        Mock<ILogger> mockLogger = new();
        string filePath = "test.txt";
        string operation = "Read";
        InvalidOperationException exception = new("fail!");

        FailedOperations.RecordFailedFileOperation(mockLogger.Object, filePath, operation, exception);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(filePath) && v.ToString().Contains(operation) && v.ToString().Contains("fail!")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void RecordFailedFileOperation_WithCustomError_LogsError()
    {
        Mock<ILogger> mockLogger = new();
        string filePath = "test.txt";
        string operation = "Write";
        string errorMessage = "custom error";

        FailedOperations.RecordFailedFileOperation(mockLogger.Object, filePath, operation, errorMessage);

        mockLogger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(filePath) && v.ToString().Contains(operation) && v.ToString().Contains(errorMessage)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void RecordFailedFileOperation_NullLogger_Throws()
    {
        string filePath = "test.txt";
        string operation = "Delete";
        Exception exception = new("fail");
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            FailedOperations.RecordFailedFileOperation(null, filePath, operation, exception));
    }
}
