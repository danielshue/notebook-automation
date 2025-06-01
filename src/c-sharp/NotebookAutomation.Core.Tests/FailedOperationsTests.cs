using System;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Unit tests for the FailedOperations static class.
    /// </summary>
    [TestClass]
    public class FailedOperationsTests
    {
        [TestMethod]
        public void RecordFailedFileOperation_WithException_LogsError()
        {
            var mockLogger = new Mock<ILogger>();
            var filePath = "test.txt";
            var operation = "Read";
            var exception = new InvalidOperationException("fail!");

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
            var mockLogger = new Mock<ILogger>();
            var filePath = "test.txt";
            var operation = "Write";
            var errorMessage = "custom error";

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
            var filePath = "test.txt";
            var operation = "Delete";
            var exception = new Exception("fail");
            Assert.ThrowsException<ArgumentNullException>(() =>
                FailedOperations.RecordFailedFileOperation(null, filePath, operation, exception));
        }
    }
}
