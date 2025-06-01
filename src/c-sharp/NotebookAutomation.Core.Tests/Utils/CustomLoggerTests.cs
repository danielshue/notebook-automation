using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;
using Moq;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tests.Utils
{
    /// <summary>
    /// Unit tests for the CustomLogger class.
    /// </summary>
    [TestClass]
    public class CustomLoggerTests
    {
        [TestMethod]
        public void LogInformation_DelegatesToILogger()
        {
            var mockLogger = new Mock<ILogger>();
            var customLogger = new CustomLogger(mockLogger.Object);
            customLogger.LogInformation("Info {0}", 42);
            mockLogger.Verify(l => l.Log<string>(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<string>(o => o == "Info {0}"),
                null,
                It.IsAny<Func<string, Exception, string>>()), Times.Once);
        }

        [TestMethod]
        public void LogError_DelegatesToILogger()
        {
            var mockLogger = new Mock<ILogger>();
            var customLogger = new CustomLogger(mockLogger.Object);
            customLogger.LogError("Error {0}", "fail");
            mockLogger.Verify(l => l.Log<string>(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<string>(o => o == "Error {0}"),
                null,
                It.IsAny<Func<string, Exception, string>>()), Times.Once);
        }

        [TestMethod]
        public void LogDebug_DelegatesToILogger()
        {
            var mockLogger = new Mock<ILogger>();
            var customLogger = new CustomLogger(mockLogger.Object);
            customLogger.LogDebug("Debug {0}", 3.14);
            mockLogger.Verify(l => l.Log<string>(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<string>(o => o == "Debug {0}"),
                null,
                It.IsAny<Func<string, Exception, string>>()), Times.Once);
        }

        [TestMethod]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new CustomLogger(null!));
        }
    }
}
